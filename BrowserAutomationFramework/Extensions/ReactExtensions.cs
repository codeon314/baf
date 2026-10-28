using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.ReverseEngineering;
using BrowserAutomationFramework.Scraping;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.Extensions
{
    /// <summary>
    /// Provides high-level extension methods on <see cref="BrowserClient"/> for React-compatible DOM interaction,
    /// page navigation, DOM tree snapshots, element highlighting, data scraping, automation workflows, and reverse-engineering site dumps.
    /// </summary>
    public static class ReactExtensions
    {
        private static readonly JsonSerializerOptions DomJsonSerializerOptions = new()
        {
            MaxDepth = 1024
        };

        private static readonly JsonDocumentOptions HighDepthJsonDocumentOptions = new()
        {
            MaxDepth = 1024
        };

        /// <summary>
        /// Locates an element via CSS selector and simulates a trusted React or native click event.
        /// </summary>
        public static Task<CommandResult> ClickReactElementAsync(this BrowserClient client, string selector, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("REACT_CLICK", new { selector }, cancellationToken);
        }

        /// <summary>
        /// Simulates a trusted React or native click event on the element represented by <paramref name="domNode"/>.
        /// </summary>
        public static Task<CommandResult> ClickReactElementAsync(this BrowserClient client, DomNode domNode, CancellationToken cancellationToken = default)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            if (string.IsNullOrWhiteSpace(domNode.Path))
                throw new ArgumentException("The specified DomNode does not have a valid CSS path.", nameof(domNode));

            return client.ClickReactElementAsync(domNode.Path, cancellationToken);
        }

        /// <summary>
        /// Navigates the browser tab to the specified URL and waits until the new page has completely loaded.
        /// </summary>
        public static async Task<CommandResult> NavigateToUrlAsync(
            this BrowserClient client,
            string url,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty.", nameof(url));

            string? beforeLoadId = client.CurrentPageLoadId;
            var navResult = await client.SendCommandAsync("NAVIGATE", new { url }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(navResult.Output))
            {
                try
                {
                    using var doc = JsonDocument.Parse(navResult.Output, HighDepthJsonDocumentOptions);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("previousPageLoadId", out var prevProp) &&
                        prevProp.ValueKind == JsonValueKind.String)
                    {
                        beforeLoadId = prevProp.GetString() ?? beforeLoadId;
                    }
                }
                catch { }
            }

            await client.WaitForPageTransitionAsync(beforeLoadId, expectNavigation: true, timeoutMs: timeoutMs, cancellationToken: cancellationToken);
            return navResult;
        }

        /// <summary>
        /// Reloads the current page in the browser tab and waits until the reloaded page has completely loaded.
        /// </summary>
        public static async Task<CommandResult> ReloadPageAsync(
            this BrowserClient client,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            string? beforeLoadId = client.CurrentPageLoadId;
            var reloadResult = await client.SendCommandAsync("RELOAD_PAGE", null, cancellationToken);

            if (!string.IsNullOrWhiteSpace(reloadResult.Output))
            {
                try
                {
                    using var doc = JsonDocument.Parse(reloadResult.Output, HighDepthJsonDocumentOptions);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("previousPageLoadId", out var prevProp) &&
                        prevProp.ValueKind == JsonValueKind.String)
                    {
                        beforeLoadId = prevProp.GetString() ?? beforeLoadId;
                    }
                }
                catch { }
            }

            await client.WaitForPageTransitionAsync(beforeLoadId, expectNavigation: true, timeoutMs: timeoutMs, cancellationToken: cancellationToken);
            return reloadResult;
        }

        /// <summary>
        /// Waits until the current page reports <c>document.readyState === 'complete'</c>.
        /// </summary>
        public static async Task<CommandResult> WaitForPageLoadAsync(
            this BrowserClient client,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            var res = await client.SendCommandAsync("WAIT_FOR_PAGE_LOAD", new { timeoutMs }, cancellationToken);
            UpdateClientPageStateFromOutput(client, res.Output);
            return res;
        }

        /// <summary>
        /// Waits for a page navigation or in-page transition to complete and ensures the entire page is loaded before returning.
        /// </summary>
        public static async Task WaitForPageTransitionAsync(
            this BrowserClient client,
            string? previousPageLoadId,
            bool expectNavigation = true,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            var pageLoadedTcs = new TaskCompletionSource<PageLoadedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnLoaded(object? sender, PageLoadedEventArgs e)
            {
                if (string.IsNullOrEmpty(previousPageLoadId) || !string.Equals(e.PageLoadId, previousPageLoadId, StringComparison.Ordinal))
                {
                    pageLoadedTcs.TrySetResult(e);
                }
            }

            client.OnPageLoaded += OnLoaded;
            try
            {
                // If safeNavigate already completed the transition in-place, return immediately
                if (!string.IsNullOrEmpty(previousPageLoadId) &&
                    !string.IsNullOrEmpty(client.CurrentPageLoadId) &&
                    !string.Equals(client.CurrentPageLoadId, previousPageLoadId, StringComparison.Ordinal))
                {
                    await Task.Delay(50, cancellationToken);
                    return;
                }

                if (expectNavigation)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeoutMs);

                    while (!timeoutCts.IsCancellationRequested)
                    {
                        if (pageLoadedTcs.Task.IsCompleted)
                            break;

                        if (!string.IsNullOrEmpty(client.CurrentPageLoadId) &&
                            !string.IsNullOrEmpty(previousPageLoadId) &&
                            !string.Equals(client.CurrentPageLoadId, previousPageLoadId, StringComparison.Ordinal))
                        {
                            break;
                        }

                        var completed = await Task.WhenAny(pageLoadedTcs.Task, Task.Delay(2000, timeoutCts.Token));
                        if (completed == pageLoadedTcs.Task)
                            break;

                        try
                        {
                            await client.WaitForPageLoadAsync(timeoutMs, timeoutCts.Token);
                            if (!string.IsNullOrEmpty(client.CurrentPageLoadId) &&
                                (string.IsNullOrEmpty(previousPageLoadId) ||
                                 !string.Equals(client.CurrentPageLoadId, previousPageLoadId, StringComparison.Ordinal)))
                            {
                                break;
                            }
                        }
                        catch
                        {
                            // Page is still unloading/reconnecting
                        }
                    }
                }
                else
                {
                    await Task.Delay(150, cancellationToken);
                    await client.WaitForPageLoadAsync(timeoutMs, cancellationToken);
                }

                await Task.Delay(50, cancellationToken);
            }
            finally
            {
                client.OnPageLoaded -= OnLoaded;
            }
        }

        private static void UpdateClientPageStateFromOutput(BrowserClient client, string? output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return;

            try
            {
                using var doc = JsonDocument.Parse(output, HighDepthJsonDocumentOptions);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    string pageLoadId = doc.RootElement.TryGetProperty("pageLoadId", out var idProp) ? (idProp.GetString() ?? string.Empty) : string.Empty;
                    string url = doc.RootElement.TryGetProperty("url", out var urlProp) ? (urlProp.GetString() ?? string.Empty) : string.Empty;
                    string readyState = doc.RootElement.TryGetProperty("readyState", out var rsProp) ? (rsProp.GetString() ?? "complete") : "complete";

                    if (!string.IsNullOrEmpty(pageLoadId) || !string.IsNullOrEmpty(url))
                    {
                        client.HandleEvent(new EventMessage
                        {
                            Event = "PAGE_LOADED",
                            Payload = doc.RootElement.Clone()
                        });
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Locates an element via CSS selector and sets its value, bypassing React's native value tracking.
        /// </summary>
        public static Task<CommandResult> SetReactInputValueAsync(this BrowserClient client, string selector, string value, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("SET_VALUE", new { selector, value, overwrite }, cancellationToken);
        }

        /// <summary>
        /// Sets the input value of the element represented by <paramref name="domNode"/>, bypassing React's native value tracking.
        /// </summary>
        public static Task<CommandResult> SetReactInputValueAsync(this BrowserClient client, DomNode domNode, string value, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            if (string.IsNullOrWhiteSpace(domNode.Path))
                throw new ArgumentException("The specified DomNode does not have a valid CSS path.", nameof(domNode));

            return client.SetReactInputValueAsync(domNode.Path, value, overwrite, cancellationToken);
        }

        /// <summary>
        /// Evaluates raw JavaScript in the context of the page.
        /// </summary>
        public static Task<CommandResult> EvaluateJavaScriptAsync(this BrowserClient client, string code, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("EVAL", new { code }, cancellationToken);
        }

        /// <summary>
        /// Retrieves a serialized representation of the DOM.
        /// </summary>
        public static Task<CommandResult> GetDomAsync(this BrowserClient client, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("GET_DOM", null, cancellationToken);
        }

        /// <summary>
        /// Retrieves and deserializes the page DOM into a <see cref="DomNode"/> tree on a background thread,
        /// caching the result in <see cref="BrowserClient.CurrentDomRoot"/>.
        /// </summary>
        public static async Task<DomNode?> GetDomNodeAsync(this BrowserClient client, CancellationToken cancellationToken = default)
        {
            var result = await client.GetDomAsync(cancellationToken).ConfigureAwait(false);
            if (result.Status == "completed" && !string.IsNullOrEmpty(result.Output))
            {
                string rawJson = result.Output;
                var rootNode = await Task.Run(
                    () => JsonSerializer.Deserialize<DomNode>(rawJson, DomJsonSerializerOptions),
                    cancellationToken).ConfigureAwait(false);
                client.CurrentDomRoot = rootNode;
                return rootNode;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the page DOM and builds a pre-indexed, syntax-tokenized <see cref="DomTreeSnapshot"/> on a background thread,
        /// caching the result in <see cref="BrowserClient.CurrentDomSnapshot"/> and <see cref="BrowserClient.CurrentDomRoot"/>.
        /// </summary>
        public static async Task<DomTreeSnapshot?> GetDomSnapshotAsync(this BrowserClient client, CancellationToken cancellationToken = default)
        {
            var rootNode = await client.GetDomNodeAsync(cancellationToken).ConfigureAwait(false);
            if (rootNode == null)
                return null;

            var snapshot = await Task.Run(() => DomTreeSnapshot.Build(rootNode), cancellationToken).ConfigureAwait(false);
            client.CurrentDomSnapshot = snapshot;
            client.CurrentDomRoot = snapshot.RootNode;
            return snapshot;
        }

        /// <summary>
        /// Highlights an element on the page using its CSS selector.
        /// </summary>
        public static Task<CommandResult> HighlightElementAsync(this BrowserClient client, string selector, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("HIGHLIGHT_ELEMENT", new { selector }, cancellationToken);
        }

        /// <summary>
        /// Removes the highlight overlay from the page.
        /// </summary>
        public static Task<CommandResult> RemoveHighlightAsync(this BrowserClient client, CancellationToken cancellationToken = default)
        {
            return client.SendCommandAsync("REMOVE_HIGHLIGHT", null, cancellationToken);
        }

        /// <summary>
        /// Highlights all elements matching a selector (or relative to a container selector) and returns the number of matched elements.
        /// </summary>
        public static async Task<int> HighlightElementsAsync(
            this BrowserClient client,
            string selector,
            string? containerSelector = null,
            bool matchAllElements = true,
            CancellationToken cancellationToken = default)
        {
            var res = await client.SendCommandAsync("HIGHLIGHT_ELEMENTS", new
            {
                selector,
                containerSelector = containerSelector ?? string.Empty,
                matchAllElements
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(res.Output))
            {
                try
                {
                    using var doc = JsonDocument.Parse(res.Output, HighDepthJsonDocumentOptions);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("count", out var cntProp) &&
                        cntProp.TryGetInt32(out int count))
                    {
                        return count;
                    }
                }
                catch
                {
                    if (int.TryParse(res.Output, out int parsed))
                        return parsed;
                }
            }

            return 0;
        }

        /// <summary>
        /// Highlights all elements matching a <see cref="ScrapeField"/> (resolved against an optional <see cref="ScrapeDefinition"/> container) and returns the match count.
        /// </summary>
        public static Task<int> HighlightScrapeFieldAsync(
            this BrowserClient client,
            ScrapeField field,
            ScrapeDefinition? parentDefinition = null,
            CancellationToken cancellationToken = default)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            string? containerSelector = parentDefinition?.ContainerSelector;
            bool matchAll = field.MatchAllElements || !string.IsNullOrWhiteSpace(containerSelector);
            return client.HighlightElementsAsync(field.Selector, containerSelector, matchAll, cancellationToken);
        }

        /// <summary>
        /// Highlights all repeating container elements configured on a <see cref="ScrapeDefinition"/> and returns the match count.
        /// </summary>
        public static Task<int> HighlightScrapeContainerAsync(
            this BrowserClient client,
            ScrapeDefinition definition,
            CancellationToken cancellationToken = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return client.HighlightElementsAsync(definition.ContainerSelector, null, true, cancellationToken);
        }

        /// <summary>
        /// Generalizes a container CSS selector using the client's cached DOM root (or <paramref name="fallbackDomRoot"/>),
        /// and updates <paramref name="definition"/> if provided.
        /// </summary>
        public static string GeneralizeContainerSelector(
            this BrowserClient? client,
            string rawSelector,
            ScrapeDefinition? definition = null,
            DomNode? fallbackDomRoot = null)
        {
            if (string.IsNullOrWhiteSpace(rawSelector))
                return string.Empty;

            DomNode? rootDom = client?.ResolveDomRoot(fallbackDomRoot) ?? fallbackDomRoot;
            return definition != null
                ? definition.GeneralizeAndSetContainer(rawSelector, rootDom)
                : SelectorHelper.GeneralizeContainerSelector(rawSelector, rootDom);
        }

        /// <summary>
        /// Highlights the repeating container configured on <paramref name="definition"/> if provided,
        /// or highlights all elements matching <paramref name="rawSelector"/>.
        /// </summary>
        public static Task<int> HighlightContainerOrSelectorAsync(
            this BrowserClient client,
            string rawSelector,
            ScrapeDefinition? definition = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (definition != null && !string.IsNullOrWhiteSpace(definition.ContainerSelector))
            {
                return client.HighlightScrapeContainerAsync(definition, cancellationToken);
            }
            return client.HighlightElementsAsync(rawSelector, null, true, cancellationToken);
        }

        /// <summary>
        /// Creates a fluent <see cref="ScrapeBuilder"/> bound to this <see cref="BrowserClient"/>.
        /// </summary>
        public static ScrapeBuilder CreateScrape(this BrowserClient client, string name = "Scrape")
        {
            return new ScrapeBuilder(name, client);
        }

        /// <summary>
        /// Executes a <see cref="ScrapeDefinition"/> on this client using the provided or default <see cref="DataScraper"/>.
        /// </summary>
        public static Task<ScrapeResult> ExecuteScrapeAsync(
            this BrowserClient client,
            ScrapeDefinition definition,
            IEnumerable<AutomationSequence>? availableSequences = null,
            DataScraper? scraper = null,
            CancellationToken cancellationToken = default)
        {
            var activeScraper = scraper ?? new DataScraper();
            return activeScraper.RunScrapeAsync(client, definition, availableSequences, cancellationToken);
        }

        /// <summary>
        /// Executes all active <see cref="ScrapeDefinition"/> items in an <see cref="AutomationProject"/> on this client.
        /// </summary>
        public static Task<List<ScrapeResult>> ExecuteProjectScrapesAsync(
            this BrowserClient client,
            AutomationProject project,
            DataScraper? scraper = null,
            CancellationToken cancellationToken = default)
        {
            var activeScraper = scraper ?? new DataScraper();
            return activeScraper.RunProjectScrapesAsync(client, project, cancellationToken);
        }

        /// <summary>
        /// Executes all active <see cref="ScrapeDefinition"/> items in an <see cref="AutomationProject"/> and combines them into a single <see cref="ScrapeResult"/>.
        /// </summary>
        public static Task<ScrapeResult> ExecuteProjectScrapesCombinedAsync(
            this BrowserClient client,
            AutomationProject project,
            DataScraper? scraper = null,
            bool deduplicate = false,
            CancellationToken cancellationToken = default)
        {
            var activeScraper = scraper ?? new DataScraper();
            return activeScraper.RunProjectScrapesCombinedAsync(client, project, deduplicate, cancellationToken);
        }

        /// <summary>
        /// Extracts a single value from the first element matching <paramref name="selector"/>.
        /// </summary>
        public static Task<string?> ScrapeElementAsync(
            this BrowserClient client,
            string selector,
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText,
            string? attributeName = null,
            CancellationToken cancellationToken = default)
        {
            var scraper = new DataScraper();
            return scraper.ExtractValueAsync(client, selector, extractionType, attributeName, cancellationToken);
        }

        /// <summary>
        /// Extracts a single value from the element represented by <paramref name="domNode"/> using the specified or automatically suggested <see cref="ScrapeExtractionType"/>.
        /// </summary>
        public static Task<string?> ScrapeElementAsync(
            this BrowserClient client,
            DomNode domNode,
            ScrapeExtractionType? extractionType = null,
            string? attributeName = null,
            CancellationToken cancellationToken = default)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            if (string.IsNullOrWhiteSpace(domNode.Path))
                throw new ArgumentException("The specified DomNode does not have a valid CSS path.", nameof(domNode));

            var resolvedType = extractionType ?? domNode.GetSuggestedExtractionType();
            return client.ScrapeElementAsync(domNode.Path, resolvedType, attributeName, cancellationToken);
        }

        /// <summary>
        /// Extracts a list of values from all elements matching <paramref name="selector"/>.
        /// </summary>
        public static Task<List<string>> ScrapeElementsAsync(
            this BrowserClient client,
            string selector,
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText,
            string? attributeName = null,
            CancellationToken cancellationToken = default)
        {
            var scraper = new DataScraper();
            return scraper.ExtractListAsync(client, selector, extractionType, attributeName, cancellationToken);
        }

        /// <summary>
        /// Automatically scrapes an HTML &lt;table&gt; element into a <see cref="ScrapeResult"/>.
        /// </summary>
        public static Task<ScrapeResult> ScrapeTableAsync(
            this BrowserClient client,
            string tableSelector = "table",
            CancellationToken cancellationToken = default)
        {
            var scraper = new DataScraper();
            return scraper.ExtractTableAsync(client, tableSelector, cancellationToken);
        }

        /// <summary>
        /// Executes a single <see cref="AutomationStep"/> on this client using the provided or default <see cref="AutomationRunner"/>.
        /// </summary>
        public static Task<CommandResult?> ExecuteStepAsync(this BrowserClient client, AutomationStep step, AutomationRunner? runner = null, CancellationToken cancellationToken = default)
        {
            var activeRunner = runner ?? new AutomationRunner();
            return activeRunner.RunStepAsync(client, step, cancellationToken);
        }

        /// <summary>
        /// Executes an <see cref="AutomationSequence"/> on this client using the provided or default <see cref="AutomationRunner"/>.
        /// </summary>
        public static Task ExecuteSequenceAsync(this BrowserClient client, AutomationSequence sequence, AutomationRunner? runner = null, CancellationToken cancellationToken = default)
        {
            var activeRunner = runner ?? new AutomationRunner();
            return activeRunner.RunSequenceAsync(client, sequence, cancellationToken);
        }

        /// <summary>
        /// Executes all active sequences in an <see cref="AutomationProject"/> on this client.
        /// </summary>
        public static Task ExecuteProjectAsync(this BrowserClient client, AutomationProject project, AutomationRunner? runner = null, CancellationToken cancellationToken = default)
        {
            var activeRunner = runner ?? new AutomationRunner();
            return activeRunner.RunProjectAsync(client, project, cancellationToken);
        }

        /// <summary>
        /// Dumps all HTML from the connected site (including accessible same-origin iframes).
        /// </summary>
        public static Task<SiteDumpResult> DumpHtmlAsync(
            this BrowserClient client,
            SiteDumpOptions? options = null,
            SiteDumper? dumper = null,
            CancellationToken cancellationToken = default)
        {
            var activeDumper = dumper ?? new SiteDumper();
            return activeDumper.DumpHtmlAsync(client, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all external, inline, and constructed CSS stylesheets from the connected site.
        /// </summary>
        public static Task<SiteDumpResult> DumpCssAsync(
            this BrowserClient client,
            SiteDumpOptions? options = null,
            SiteDumper? dumper = null,
            CancellationToken cancellationToken = default)
        {
            var activeDumper = dumper ?? new SiteDumper();
            return activeDumper.DumpCssAsync(client, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all external, inline, and dynamically loaded JavaScript from the connected site.
        /// </summary>
        public static Task<SiteDumpResult> DumpJavaScriptAsync(
            this BrowserClient client,
            SiteDumpOptions? options = null,
            SiteDumper? dumper = null,
            CancellationToken cancellationToken = default)
        {
            var activeDumper = dumper ?? new SiteDumper();
            return activeDumper.DumpJavaScriptAsync(client, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all HTML, CSS, and JavaScript from the connected site in a single operation.
        /// </summary>
        public static Task<SiteDumpResult> DumpAllAsync(
            this BrowserClient client,
            SiteDumpOptions? options = null,
            SiteDumper? dumper = null,
            CancellationToken cancellationToken = default)
        {
            var activeDumper = dumper ?? new SiteDumper();
            return activeDumper.DumpAllAsync(client, options, cancellationToken);
        }

        /// <summary>
        /// Dumps site assets matching the specified <see cref="SiteDumpType"/> from the connected site.
        /// </summary>
        public static Task<SiteDumpResult> DumpSiteAsync(
            this BrowserClient client,
            SiteDumpType dumpType,
            SiteDumpOptions? options = null,
            SiteDumper? dumper = null,
            CancellationToken cancellationToken = default)
        {
            var activeDumper = dumper ?? new SiteDumper();
            return activeDumper.DumpSiteAsync(client, dumpType, options, cancellationToken);
        }
    }
}