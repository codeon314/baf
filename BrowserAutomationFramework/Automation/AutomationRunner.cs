using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.ReverseEngineering;
using BrowserAutomationFramework.Scraping;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.Automation
{
    /// <summary>
    /// Modular execution engine for running <see cref="AutomationProject"/>, <see cref="AutomationSequence"/>, and <see cref="AutomationStep"/> workflows.
    /// Allows developers to register custom step handlers and subscribe to execution lifecycle events.
    /// </summary>
    public class AutomationRunner
    {
        /// <summary>
        /// Represents an asynchronous handler that executes a single <see cref="AutomationStep"/> against a connected <see cref="BrowserClient"/>.
        /// </summary>
        /// <param name="client">The target browser client.</param>
        /// <param name="step">The automation step to execute.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the optional <see cref="CommandResult"/>.</returns>
        public delegate Task<CommandResult?> StepHandlerDelegate(BrowserClient client, AutomationStep step, CancellationToken cancellationToken);

        private readonly ConcurrentDictionary<AutomationActionType, StepHandlerDelegate> _stepHandlers = new();
        private readonly ConcurrentDictionary<string, StepHandlerDelegate> _customCommandHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ScrapeDefinition> _registeredScrapes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Raised when an <see cref="AutomationSequence"/> begins execution.
        /// </summary>
        public event EventHandler<AutomationSequenceEventArgs>? OnSequenceStarted;

        /// <summary>
        /// Raised when an <see cref="AutomationSequence"/> finishes executing all of its steps.
        /// </summary>
        public event EventHandler<AutomationSequenceEventArgs>? OnSequenceCompleted;

        /// <summary>
        /// Raised immediately before an individual <see cref="AutomationStep"/> is executed.
        /// </summary>
        public event EventHandler<AutomationStepEventArgs>? OnStepStarted;

        /// <summary>
        /// Raised when an individual <see cref="AutomationStep"/> completes successfully.
        /// </summary>
        public event EventHandler<AutomationStepEventArgs>? OnStepCompleted;

        /// <summary>
        /// Raised when an individual <see cref="AutomationStep"/> throws an exception during execution.
        /// </summary>
        public event EventHandler<AutomationStepEventArgs>? OnStepFailed;

        /// <summary>
        /// Raised when an <see cref="AutomationActionType.Scrape"/> step finishes extracting data.
        /// </summary>
        public event EventHandler<ScrapeEventArgs>? OnScrapeCompleted;

        /// <summary>
        /// Raised when an <see cref="AutomationActionType.DumpSite"/> step finishes dumping site resources.
        /// </summary>
        public event EventHandler<SiteDumpEventArgs>? OnSiteDumpCompleted;

        /// <summary>
        /// Gets the <see cref="DataScraper"/> instance used by this runner when executing <see cref="AutomationActionType.Scrape"/> steps.
        /// </summary>
        public DataScraper Scraper { get; }

        /// <summary>
        /// Gets the <see cref="SiteDumper"/> instance used by this runner when executing <see cref="AutomationActionType.DumpSite"/> steps.
        /// </summary>
        public SiteDumper SiteDumper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationRunner"/> class with all built-in step handlers registered.
        /// </summary>
        /// <param name="scraper">An optional <see cref="DataScraper"/> instance to share with this runner.</param>
        /// <param name="siteDumper">An optional <see cref="SiteDumper"/> instance to share with this runner.</param>
        public AutomationRunner(DataScraper? scraper = null, SiteDumper? siteDumper = null)
        {
            Scraper = scraper ?? new DataScraper(this);
            SiteDumper = siteDumper ?? new SiteDumper();
            Scraper.OnScrapeCompleted += (s, e) => OnScrapeCompleted?.Invoke(this, e);
            SiteDumper.OnDumpCompleted += (s, e) => OnSiteDumpCompleted?.Invoke(this, e);
            RegisterDefaultHandlers();
        }

        private void RegisterDefaultHandlers()
        {
            RegisterStepHandler(AutomationActionType.Click, async (client, step, ct) =>
            {
                string? beforeLoadId = client.CurrentPageLoadId;
                var res = await client.ClickReactElementAsync(step.Selector, ct);
                if (res.Output != null && res.Output.StartsWith("Link click executed", StringComparison.OrdinalIgnoreCase))
                {
                    await client.WaitForPageTransitionAsync(beforeLoadId, expectNavigation: true, cancellationToken: ct);
                }
                return res;
            });

            RegisterStepHandler(AutomationActionType.SetValue, (client, step, ct) =>
                client.SetReactInputValueAsync(step.Selector, step.Value, step.Overwrite, ct)!);

            RegisterStepHandler(AutomationActionType.Navigate, (client, step, ct) =>
            {
                string targetUrl = !string.IsNullOrWhiteSpace(step.Value) ? step.Value.Trim() : step.Selector.Trim();
                return client.NavigateToUrlAsync(targetUrl, cancellationToken: ct)!;
            });

            RegisterStepHandler(AutomationActionType.EvaluateScript, (client, step, ct) =>
                client.EvaluateJavaScriptAsync(step.Value, ct)!);

            RegisterStepHandler(AutomationActionType.Scrape, async (client, step, ct) =>
            {
                string targetScrapeName = !string.IsNullOrWhiteSpace(step.Value) ? step.Value.Trim() : step.CustomCommandType.Trim();

                if (!string.IsNullOrWhiteSpace(targetScrapeName) &&
                    _registeredScrapes.TryGetValue(targetScrapeName, out var scrapeDef))
                {
                    var scrapeRes = await Scraper.RunScrapeAsync(client, scrapeDef, null, ct);
                    return new CommandResult
                    {
                        Status = "completed",
                        Output = scrapeRes.ToJson(false, true)
                    };
                }

                var adHocDef = new ScrapeDefinition
                {
                    Name = step.Name,
                    WaitBeforeScrapeMs = 0,
                    Fields = new List<ScrapeField>
                    {
                        new ScrapeField
                        {
                            Name = !string.IsNullOrWhiteSpace(step.Value) ? step.Value : step.Name,
                            Selector = step.Selector,
                            ExtractionType = Enum.TryParse<ScrapeExtractionType>(step.CustomCommandType, true, out var parsedType)
                                ? parsedType
                                : ScrapeExtractionType.InnerText,
                            MatchAllElements = !step.Overwrite
                        }
                    }
                };

                var result = await Scraper.RunScrapeAsync(client, adHocDef, null, ct);
                return new CommandResult
                {
                    Status = "completed",
                    Output = result.ToJson(false, true)
                };
            });

            RegisterStepHandler(AutomationActionType.Custom, async (client, step, ct) =>
            {
                if (!string.IsNullOrWhiteSpace(step.CustomCommandType) &&
                    _customCommandHandlers.TryGetValue(step.CustomCommandType, out var customHandler))
                {
                    return await customHandler(client, step, ct);
                }

                string cmdType = !string.IsNullOrWhiteSpace(step.CustomCommandType) ? step.CustomCommandType : step.Name;
                return await client.SendRawCommandAsync(cmdType, step.Value, ct);
            });

            RegisterStepHandler(AutomationActionType.DumpSite, async (client, step, ct) =>
            {
                var dumpType = Enum.TryParse<SiteDumpType>(step.CustomCommandType, true, out var parsedType)
                    ? parsedType
                    : SiteDumpType.All;

                var dumpResult = await SiteDumper.DumpSiteAsync(client, dumpType, null, ct);
                if (!string.IsNullOrWhiteSpace(step.Value))
                {
                    await dumpResult.SaveToDirectoryAsync(step.Value.Trim(), cancellationToken: ct);
                }

                return new CommandResult
                {
                    Status = "completed",
                    Output = $"Dumped {dumpResult.TotalResourceCount} resource(s) ({dumpResult.GetFormattedTotalSize()})"
                };
            });
        }

        /// <summary>
        /// Registers or overrides the execution handler for a given <see cref="AutomationActionType"/>.
        /// </summary>
        public void RegisterStepHandler(AutomationActionType actionType, StepHandlerDelegate handler)
        {
            _stepHandlers[actionType] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Registers a custom step handler keyed by <see cref="AutomationStep.CustomCommandType"/>.
        /// </summary>
        public void RegisterCustomActionHandler(string customCommandType, StepHandlerDelegate handler)
        {
            if (string.IsNullOrWhiteSpace(customCommandType))
                throw new ArgumentException("Custom command type cannot be empty.", nameof(customCommandType));

            _customCommandHandlers[customCommandType] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Registers a <see cref="ScrapeDefinition"/> so it can be invoked by name from an <see cref="AutomationActionType.Scrape"/> step.
        /// </summary>
        public void RegisterScrapeDefinition(ScrapeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _registeredScrapes[definition.Name] = definition;
        }

        /// <summary>
        /// Replaces the registered <see cref="ScrapeDefinition"/> lookup table.
        /// </summary>
        public void SetScrapeDefinitions(IEnumerable<ScrapeDefinition> definitions)
        {
            _registeredScrapes.Clear();
            if (definitions == null) return;
            foreach (var def in definitions)
            {
                if (!string.IsNullOrWhiteSpace(def.Name))
                {
                    _registeredScrapes[def.Name] = def;
                }
            }
        }

        /// <summary>
        /// Runs all active sequences in an <see cref="AutomationProject"/>.
        /// </summary>
        public Task RunProjectAsync(BrowserClient client, AutomationProject project, CancellationToken cancellationToken = default)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            SetScrapeDefinitions(project.Scrapes);
            var activeSequences = project.Sequences.Where(s => s.IsActive);
            return RunSequencesAsync(client, activeSequences, cancellationToken);
        }

        /// <summary>
        /// Runs a collection of <see cref="AutomationSequence"/> instances sequentially.
        /// </summary>
        public async Task RunSequencesAsync(BrowserClient client, IEnumerable<AutomationSequence> sequences, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (sequences == null) throw new ArgumentNullException(nameof(sequences));

            foreach (var sequence in sequences)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RunSequenceAsync(client, sequence, cancellationToken);
            }
        }

        /// <summary>
        /// Runs all steps in a single <see cref="AutomationSequence"/>.
        /// </summary>
        public async Task RunSequenceAsync(BrowserClient client, AutomationSequence sequence, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));

            OnSequenceStarted?.Invoke(this, new AutomationSequenceEventArgs(sequence));

            foreach (var step in sequence.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteStepInternalAsync(client, sequence, step, cancellationToken);

                if (step.DelayAfterMs > 0)
                {
                    await Task.Delay(step.DelayAfterMs, cancellationToken);
                }
            }

            OnSequenceCompleted?.Invoke(this, new AutomationSequenceEventArgs(sequence));
        }

        /// <summary>
        /// Executes a single <see cref="AutomationStep"/> against the specified client.
        /// </summary>
        public Task<CommandResult?> RunStepAsync(BrowserClient client, AutomationStep step, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (step == null) throw new ArgumentNullException(nameof(step));

            return ExecuteStepInternalAsync(client, null, step, cancellationToken);
        }

        private async Task<CommandResult?> ExecuteStepInternalAsync(BrowserClient client, AutomationSequence? sequence, AutomationStep step, CancellationToken cancellationToken)
        {
            OnStepStarted?.Invoke(this, new AutomationStepEventArgs(sequence, step));

            try
            {
                if (!_stepHandlers.TryGetValue(step.ActionType, out var handler))
                {
                    throw new NotSupportedException($"No handler registered for action type '{step.ActionType}'.");
                }

                var result = await handler(client, step, cancellationToken);
                OnStepCompleted?.Invoke(this, new AutomationStepEventArgs(sequence, step, result));
                return result;
            }
            catch (Exception ex)
            {
                OnStepFailed?.Invoke(this, new AutomationStepEventArgs(sequence, step, null, ex));
                throw;
            }
        }
    }
}