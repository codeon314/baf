using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Generators;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.ReverseEngineering;
using BrowserAutomationFramework.Scraping;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.Automation
{
    /// <summary>
    /// High-level studio session controller that coordinates <see cref="AutomationServer"/>, <see cref="AutomationProject"/>,
    /// <see cref="AutomationRunner"/>, <see cref="DataScraper"/>, and <see cref="SiteDumper"/> into a single facade with unified logging and state management.
    /// </summary>
    public class AutomationStudioSession : IDisposable
    {
        /// <summary>
        /// Gets the default TCP port used by the Automation Studio server.
        /// </summary>
        public const int DefaultPort = 9999;

        /// <summary>
        /// Gets the minimum valid TCP port for the Automation Server.
        /// </summary>
        public const int MinPort = 1024;

        /// <summary>
        /// Gets the maximum valid TCP port for the Automation Server.
        /// </summary>
        public const int MaxPort = 65535;

        /// <summary>
        /// Clamps <paramref name="port"/> to the valid <see cref="MinPort"/>..<see cref="MaxPort"/> range.
        /// </summary>
        public static int ClampPort(int port)
        {
            return Math.Clamp(port, MinPort, MaxPort);
        }

        /// <summary>
        /// Gets the default raw command type used by the command tester.
        /// </summary>
        public const string DefaultTestCommandType = "EVAL";

        /// <summary>
        /// Gets the default raw command payload used by the command tester.
        /// </summary>
        public const string DefaultTestPayload = "console.log('Hello from C#'); return 'Success';";

        /// <summary>
        /// Gets the default IPC transport mode used by the Automation Studio.
        /// </summary>
        public const IpcMode DefaultIpcMode = IpcMode.WebSocket;

        /// <summary>
        /// Gets the short display name for the BAF Studio application.
        /// </summary>
        public const string StudioShortTitle = "BAF Studio";

        /// <summary>
        /// Gets the display title for the BAF Studio application.
        /// </summary>
        public const string StudioTitle = "BAF Studio - Browser Automation Framework Studio";

        /// <summary>
        /// Gets the display version for the BAF Studio application.
        /// </summary>
        public const string StudioVersion = "1.0";

        /// <summary>
        /// Gets the description shown in the About dialog.
        /// </summary>
        public const string StudioDescription = "Browser Automation Framework Studio — A stealth browser automation, data scraping, and reverse-engineering IDE.";

        /// <summary>
        /// Gets the full About dialog message for the Automation Studio application.
        /// </summary>
        public static string StudioAboutMessage => $"{StudioTitle}\nVersion {StudioVersion}\n\n{StudioDescription}";

        /// <summary>
        /// Gets the file dialog filter used when opening or saving automation project files.
        /// </summary>
        public const string ProjectFileDialogFilter = AutomationProject.FileDialogFilter;

        /// <summary>
        /// Gets the default file extension used for automation project files.
        /// </summary>
        public const string ProjectFileExtension = AutomationProject.DefaultFileExtension;

        /// <summary>
        /// Gets the file dialog filter used when exporting scrape results as JSON.
        /// </summary>
        public const string ScrapeJsonFileDialogFilter = ScrapeResult.JsonFileDialogFilter;

        /// <summary>
        /// Gets the file dialog filter used when exporting scrape results as CSV.
        /// </summary>
        public const string ScrapeCsvFileDialogFilter = ScrapeResult.CsvFileDialogFilter;

        /// <summary>
        /// Gets the file dialog filter used when saving dumped resources or combined output.
        /// </summary>
        public const string DumpFileDialogFilter = SiteDumpResult.FileDialogFilter;

        private AutomationServer? _server;
        private bool _exclusiveTabMode = true;
        private bool _disposed;

        /// <summary>
        /// Gets the active <see cref="AutomationProject"/> managed by this session.
        /// </summary>
        public AutomationProject Project { get; private set; } = new();

        /// <summary>
        /// Gets whether the active project contains at least one <see cref="AutomationSequence"/>.
        /// </summary>
        public bool HasSequences => Project.HasSequences;

        /// <summary>
        /// Gets whether the active project contains at least one <see cref="ScrapeDefinition"/>.
        /// </summary>
        public bool HasScrapes => Project.HasScrapes;

        /// <summary>
        /// Gets the current <see cref="AutomationServer"/> instance if started.
        /// </summary>
        public AutomationServer? Server => _server;

        /// <summary>
        /// Gets the <see cref="AutomationRunner"/> engine for this session.
        /// </summary>
        public AutomationRunner Runner { get; }

        /// <summary>
        /// Gets the <see cref="DataScraper"/> engine for this session.
        /// </summary>
        public DataScraper Scraper { get; }

        /// <summary>
        /// Gets the <see cref="SiteDumper"/> reverse-engineering engine for this session.
        /// </summary>
        public SiteDumper SiteDumper { get; }

        /// <summary>
        /// Gets the most recent <see cref="ScrapeResult"/> produced during this session, if any.
        /// </summary>
        public ScrapeResult? LastScrapeResult { get; private set; }

        /// <summary>
        /// Gets the most recent <see cref="SiteDumpResult"/> produced during this session, if any.
        /// </summary>
        public SiteDumpResult? LastSiteDumpResult { get; private set; }

        /// <summary>
        /// Gets or sets the site dump options used for the next reverse-engineering dump.
        /// </summary>
        public SiteDumpOptions DumpOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the active reverse-engineering viewer mode.
        /// </summary>
        public SiteDumpViewMode ActiveDumpViewMode { get; set; } = SiteDumpViewMode.SelectedResource;

        /// <summary>
        /// Gets or sets the currently selected dumped resource.
        /// </summary>
        public SiteResourceItem? SelectedDumpResource { get; set; }

        /// <summary>
        /// Gets or sets the current reverse-engineering resource filter text.
        /// </summary>
        public string? ResourceFilterText { get; set; }

        /// <summary>
        /// Gets or sets whether switching to a Chrome Extension tab detaches other tabs automatically.
        /// </summary>
        public bool ExclusiveTabMode
        {
            get => _exclusiveTabMode;
            set
            {
                _exclusiveTabMode = value;
                if (_server != null)
                {
                    _server.ExclusiveTabMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently active/selected <see cref="BrowserClient"/> on the server.
        /// </summary>
        public BrowserClient? ActiveClient
        {
            get => _server?.ActiveClient;
            set
            {
                if (_server != null)
                {
                    _server.ActiveClient = value;
                }
            }
        }

        /// <summary>
        /// Gets whether the <see cref="AutomationServer"/> is currently running.
        /// </summary>
        public bool IsServerRunning => _server?.IsRunning ?? false;

        /// <summary>
        /// Gets whether the BAF Chrome Extension is currently connected to the server.
        /// </summary>
        public bool IsExtensionConnected => _server?.IsExtensionConnected ?? false;

        /// <summary>
        /// Raised whenever any component in the session emits a formatted log message.
        /// </summary>
        public event EventHandler<AutomationLogEventArgs>? OnLog;

        /// <summary>
        /// Raised when the scraper status bar summary text changes.
        /// </summary>
        public event EventHandler<string>? OnScrapeStatusChanged;

        /// <summary>
        /// Raised when the reverse-engineering dump status bar summary text changes.
        /// </summary>
        public event EventHandler<string>? OnDumpStatusChanged;

        /// <summary>
        /// Raised when an HTTP 429 rate-limit response is detected during scraping.
        /// </summary>
        public event EventHandler<ScrapeRateLimitEventArgs>? OnRateLimitDetected;

        /// <summary>
        /// Raised when a new <see cref="ScrapeResult"/> is available for display.
        /// </summary>
        public event EventHandler<ScrapeResult>? OnScrapeResultAvailable;

        /// <summary>
        /// Raised when a new <see cref="SiteDumpResult"/> is available for display.
        /// </summary>
        public event EventHandler<SiteDumpResult>? OnSiteDumpResultAvailable;

        /// <summary>
        /// Raised when a <see cref="BrowserClient"/> connects to the server.
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientConnected;

        /// <summary>
        /// Raised when a <see cref="BrowserClient"/> disconnects from the server.
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientDisconnected;

        /// <summary>
        /// Raised when a connected <see cref="BrowserClient"/> updates its metadata (title, URL, or IPC mode).
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientMetadataUpdated;

        /// <summary>
        /// Raised when <see cref="ActiveClient"/> changes.
        /// </summary>
        public event EventHandler<BrowserClient?>? OnActiveClientChanged;

        /// <summary>
        /// Raised when <see cref="ActiveClient"/> finishes loading a page.
        /// </summary>
        public event EventHandler<PageLoadedEventArgs>? OnActiveClientPageLoaded;

        /// <summary>
        /// Raised when <see cref="ActiveClient"/> reports a DOM mutation.
        /// </summary>
        public event EventHandler<DomChangedEventArgs>? OnActiveClientDomChanged;

        /// <summary>
        /// Raised when the highlighted element on <see cref="ActiveClient"/> is removed from the DOM.
        /// </summary>
        public event EventHandler? OnActiveClientHighlightedElementRemoved;

        /// <summary>
        /// Raised when an element is middle-clicked in the <see cref="ActiveClient"/> browser tab.
        /// </summary>
        public event EventHandler<ElementClickedEventArgs>? OnActiveClientElementClicked;

        /// <summary>
        /// Raised when the Chrome Extension connects, disconnects, or updates its open tab list.
        /// </summary>
        public event EventHandler? OnExtensionStateChanged;

        /// <summary>
        /// Raised when the Chrome Extension reports an updated list of open browser tabs.
        /// </summary>
        public event EventHandler<ExtensionTabsEventArgs>? OnExtensionTabsUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationStudioSession"/> class and wires all engine lifecycle events.
        /// </summary>
        public AutomationStudioSession()
        {
            Runner = new AutomationRunner();
            Scraper = Runner.Scraper;
            SiteDumper = Runner.SiteDumper;

            Runner.OnSequenceStarted += (s, e) => Log("AUTO", e.GetStartedLogMessage(), AutomationLogLevel.Automation);
            Runner.OnStepStarted += (s, e) => Log("AUTO", e.GetStartedLogMessage(), AutomationLogLevel.AutomationStep);
            Runner.OnStepCompleted += (s, e) =>
            {
                if (e.Result != null)
                {
                    Log("AUTO-RX", e.GetCompletedLogMessage(), AutomationLogLevel.CommandRx);
                }
            };

            Scraper.OnScrapeStarted += (s, e) => Log("SCRAPE", e.GetStartedLogMessage(), AutomationLogLevel.ScrapeStart);
            Scraper.OnScrapePageCompleted += (s, e) => Log("SCRAPE", e.GetLogMessage(), AutomationLogLevel.ScrapePage);
            Scraper.OnPageTurnDelay += (s, e) =>
            {
                string msg = e.GetStatusMessage();
                Log("SCRAPE", msg, AutomationLogLevel.ScrapeDelay);
                OnScrapeStatusChanged?.Invoke(this, msg);
            };
            Scraper.OnRateLimitDetected += (s, e) =>
            {
                Log("RATE-LIMIT 429", e.GetLogMessage(), AutomationLogLevel.Warning);
                OnScrapeStatusChanged?.Invoke(this, e.GetStatusMessage());
                OnRateLimitDetected?.Invoke(this, e);
            };
            Scraper.OnScrapeCompleted += (s, e) =>
            {
                if (e.Result != null)
                {
                    Log("SCRAPE", e.GetCompletedLogMessage(), AutomationLogLevel.Success);
                    SetLastScrapeResult(e.Result);
                }
            };
            Scraper.OnScrapeFailed += (s, e) => Log("SCRAPE-ERR", e.GetFailedLogMessage(), AutomationLogLevel.Error);

            SiteDumper.OnDumpStarted += (s, e) => Log("REV-ENG", e.GetStartedLogMessage(), AutomationLogLevel.ReverseEngineering);
            SiteDumper.OnDumpCompleted += (s, e) =>
            {
                if (e.Result != null)
                {
                    Log("REV-ENG", e.GetCompletedLogMessage(), AutomationLogLevel.Success);
                    SetLastSiteDumpResult(e.Result);
                }
            };
            SiteDumper.OnDumpFailed += (s, e) => Log("REV-ENG-ERR", e.GetFailedLogMessage(), AutomationLogLevel.Error);
        }

        /// <summary>
        /// Emits a structured log message to <see cref="OnLog"/>.
        /// </summary>
        public void Log(string category, string message, AutomationLogLevel level = AutomationLogLevel.Default)
        {
            OnLog?.Invoke(this, new AutomationLogEventArgs(category, message, level));
        }

        /// <summary>
        /// Attempts to synchronize the default unpacked Chrome Extension folder on startup without throwing if disk access is restricted.
        /// </summary>
        public void TrySyncDefaultChromeExtensionQuietly(int port = DefaultPort, IpcMode mode = IpcMode.WebSocket)
        {
            try
            {
                ChromeExtensionGenerator.SyncDefaultDirectory(ClampPort(port), mode);
            }
            catch { }
        }

        /// <summary>
        /// Returns the default unpacked Chrome Extension directory if it exists on disk, or an empty string otherwise.
        /// </summary>
        public string GetExistingDefaultExtensionDirectory()
        {
            return ChromeExtensionGenerator.GetExistingDefaultExtensionDirectory();
        }

        /// <summary>
        /// Updates <see cref="LastScrapeResult"/> and raises <see cref="OnScrapeStatusChanged"/> and <see cref="OnScrapeResultAvailable"/>.
        /// </summary>
        public void SetLastScrapeResult(ScrapeResult result)
        {
            if (result == null) return;
            LastScrapeResult = result;
            OnScrapeStatusChanged?.Invoke(this, result.GetStatusSummary());
            OnScrapeResultAvailable?.Invoke(this, result);
        }

        /// <summary>
        /// Updates <see cref="LastSiteDumpResult"/> and raises <see cref="OnDumpStatusChanged"/> and <see cref="OnSiteDumpResultAvailable"/>.
        /// </summary>
        public void SetLastSiteDumpResult(SiteDumpResult result)
        {
            if (result == null) return;
            LastSiteDumpResult = result;
            SelectedDumpResource = null;
            ResourceFilterText = null;
            ActiveDumpViewMode = result.GetDefaultViewMode();
            OnDumpStatusChanged?.Invoke(this, result.GetStatusSummary());
            OnSiteDumpResultAvailable?.Invoke(this, result);
        }

        /// <summary>
        /// Returns a formatted status string describing the current Chrome Extension connection and open tab count.
        /// </summary>
        public string GetExtensionStatusMessage()
        {
            return _server != null
                ? _server.GetExtensionStatusMessage()
                : AutomationServer.GetExtensionStatusMessage(false, false, 0);
        }

        /// <summary>
        /// Returns the RGB color that represents the current Chrome Extension connection status.
        /// </summary>
        public (byte R, byte G, byte B) GetExtensionStatusColorRgb()
        {
            return IsExtensionConnected ? StudioTheme.SuccessStatus : StudioTheme.WarningStatus;
        }

        /// <summary>
        /// Returns all open browser tabs reported by the Chrome Extension, optionally filtered by a search string.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetExtensionTabs(string? filter = null)
        {
            return _server?.GetExtensionTabs(filter) ?? Array.Empty<BrowserTabInfo>();
        }

        /// <summary>
        /// Returns all open browser tabs reported by the Chrome Extension, optionally filtered by a search string,
        /// and resolves the tab matching <paramref name="preferredSelectedTabId"/> for list selection preservation.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetExtensionTabs(
            string? filter,
            int? preferredSelectedTabId,
            out BrowserTabInfo? resolvedSelectedTab)
        {
            if (_server != null)
            {
                return _server.GetExtensionTabs(filter, preferredSelectedTabId, out resolvedSelectedTab);
            }

            resolvedSelectedTab = null;
            return Array.Empty<BrowserTabInfo>();
        }

        /// <summary>
        /// Inserts or updates <paramref name="client"/> inside <paramref name="items"/> by matching <see cref="BrowserClient.InstanceId"/>.
        /// </summary>
        public int UpsertClientInList(System.Collections.IList items, BrowserClient client)
        {
            return AutomationServer.UpsertClientInList(items, client);
        }

        /// <summary>
        /// Removes all entries matching <paramref name="client"/>'s <see cref="BrowserClient.InstanceId"/> from <paramref name="items"/>.
        /// </summary>
        public bool RemoveClientFromList(System.Collections.IList items, BrowserClient? client)
        {
            return AutomationServer.RemoveClientFromList(items, client);
        }

        /// <summary>
        /// Finds the index of <paramref name="client"/> in <paramref name="items"/> by <see cref="BrowserClient.InstanceId"/>.
        /// </summary>
        public int FindClientIndex(System.Collections.IList items, BrowserClient? client)
        {
            return AutomationServer.FindClientIndex(items, client);
        }

        /// <summary>
        /// Refreshes the display entry for <paramref name="client"/> in <paramref name="items"/> if present, returning its index or -1.
        /// </summary>
        public int RefreshClientInList(System.Collections.IList items, BrowserClient? client)
        {
            return AutomationServer.RefreshClientInList(items, client);
        }

        /// <summary>
        /// Refreshes the display entry for <paramref name="client"/> and reports whether the refreshed item was at
        /// <paramref name="previousSelectedIndex"/>.
        /// </summary>
        public int RefreshClientInList(
            System.Collections.IList items,
            BrowserClient? client,
            int previousSelectedIndex,
            out bool wasSelected)
        {
            int index = AutomationServer.RefreshClientInList(items, client);
            wasSelected = index >= 0 && index == previousSelectedIndex;
            return index;
        }

        /// <summary>
        /// Moves an item from <paramref name="fromIndex"/> to <paramref name="toIndex"/> within <paramref name="items"/>.
        /// </summary>
        public bool MoveItemInList(System.Collections.IList items, int fromIndex, int toIndex)
        {
            if (items == null)
                return false;
            if (fromIndex < 0 || fromIndex >= items.Count || toIndex < 0 || toIndex >= items.Count)
                return false;
            if (fromIndex == toIndex)
                return false;

            object? item = items[fromIndex];
            items.RemoveAt(fromIndex);
            items.Insert(toIndex, item);
            return true;
        }

        /// <summary>
        /// Reassigns the item at <paramref name="index"/> to itself so bound UI controls re-evaluate its display text.
        /// </summary>
        public bool RefreshItemInList(System.Collections.IList items, int index)
        {
            if (items == null || index < 0 || index >= items.Count)
                return false;

            object? item = items[index];
            if (item == null)
                return false;

            items[index] = item;
            return true;
        }

        /// <summary>
        /// Creates, configures, and starts a new <see cref="AutomationServer"/> on the specified <paramref name="port"/>.
        /// </summary>
        public async Task StartServerAsync(int port, IpcMode mode = IpcMode.WebSocket, CancellationToken cancellationToken = default)
        {
            port = ClampPort(port);
            _server?.Dispose();
            _server = new AutomationServer(port)
            {
                ExclusiveTabMode = _exclusiveTabMode
            };

            try
            {
                string defaultExtDir = _server.SyncDefaultChromeExtension(mode);
                Log("EXT", $"Synced Chrome Extension files (Port {port}) at: {defaultExtDir}", AutomationLogLevel.Extension);
            }
            catch (Exception ex)
            {
                Log("WARN", $"Could not auto-sync default ChromeExtension folder: {ex.Message}", AutomationLogLevel.Warning);
            }

            _server.OnClientConnected += (s, c) =>
            {
                Log("SYS", $"Client Connected: {c.InstanceId} via {c.Mode}", AutomationLogLevel.ClientConnected);
                OnClientConnected?.Invoke(this, c);

                if (IsActiveClient(c))
                {
                    OnActiveClientChanged?.Invoke(this, c);
                }
            };
            _server.OnClientDisconnected += (s, c) =>
            {
                Log("SYS", $"Client Disconnected: {c.InstanceId}", AutomationLogLevel.Warning);
                OnClientDisconnected?.Invoke(this, c);
            };
            _server.OnClientMetadataUpdated += (s, c) => OnClientMetadataUpdated?.Invoke(this, c);
            _server.OnActiveClientChanged += (s, c) => OnActiveClientChanged?.Invoke(this, c);
            _server.OnClientModeChanged += (s, c) => Log("SYS", $"Client {c.InstanceId} switched IPC transport to {c.Mode}.", AutomationLogLevel.System);
            _server.OnClientStreamDataReceived += (s, ev) => Log($"STREAM <- {ev.Client.InstanceId}", ev.Result.Output ?? string.Empty, AutomationLogLevel.Stream);
            _server.OnClientPageLoaded += (s, ev) =>
            {
                Log(string.Empty, ev.Args.GetLogMessage(ev.Client.InstanceId), ev.Args.IsRateLimited ? AutomationLogLevel.Warning : AutomationLogLevel.Navigation);
            };
            _server.OnActiveClientPageLoaded += (s, e) => OnActiveClientPageLoaded?.Invoke(s, e);
            _server.OnActiveClientDomChanged += (s, e) => OnActiveClientDomChanged?.Invoke(s, e);
            _server.OnActiveClientHighlightedElementRemoved += (s, e) => OnActiveClientHighlightedElementRemoved?.Invoke(s, e);
            _server.OnActiveClientElementClicked += (s, e) => OnActiveClientElementClicked?.Invoke(s, e);

            _server.OnExtensionConnected += (s, e) =>
            {
                Log("EXT", "Chrome Extension connected to Automation Server!", AutomationLogLevel.ClientConnected);
                OnExtensionStateChanged?.Invoke(this, EventArgs.Empty);
            };
            _server.OnExtensionDisconnected += (s, e) =>
            {
                Log("EXT", "Chrome Extension disconnected.", AutomationLogLevel.Warning);
                OnExtensionStateChanged?.Invoke(this, EventArgs.Empty);
            };
            _server.OnExtensionTabsUpdated += (s, e) =>
            {
                OnExtensionStateChanged?.Invoke(this, EventArgs.Empty);
                OnExtensionTabsUpdated?.Invoke(this, e);
            };

            Log("SYS", $"Starting Automation Server on port {port}...", AutomationLogLevel.System);
            OnExtensionStateChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _server.StartAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log("ERR", $"Server error: {ex.Message}", AutomationLogLevel.Error);
                StopServer();
                throw;
            }
        }

        /// <summary>
        /// Attempts to start the <see cref="AutomationServer"/>, returning <c>false</c> when a non-cancellation
        /// startup error occurs. Startup errors are already logged and cleaned up by <see cref="StartServerAsync"/>.
        /// </summary>
        public async Task<bool> TryStartServerAsync(
            int port,
            IpcMode mode = IpcMode.WebSocket,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await StartServerAsync(port, mode, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Stops the active <see cref="AutomationServer"/> if running.
        /// </summary>
        public void StopServer()
        {
            _server?.Stop();
            Log("SYS", "Server stopped.", AutomationLogLevel.ServerStopped);
            OnExtensionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resets <see cref="Project"/> to a new empty <see cref="AutomationProject"/>.
        /// </summary>
        public AutomationProject ResetProject()
        {
            Project.Reset();
            Log("SYS", "Started new automation project.", AutomationLogLevel.System);
            return Project;
        }

        /// <summary>
        /// Loads an <see cref="AutomationProject"/> from disk into <see cref="Project"/>.
        /// </summary>
        public AutomationProject LoadProjectFromFile(string filePath)
        {
            Project = AutomationProject.LoadFromFile(filePath);
            Log("SYS", $"Loaded project from {filePath}", AutomationLogLevel.Success);
            return Project;
        }

        /// <summary>
        /// Attempts to load an <see cref="AutomationProject"/> from disk, logging an error and returning <c>null</c> on failure.
        /// </summary>
        public AutomationProject? TryLoadProjectFromFile(string filePath)
        {
            try
            {
                return LoadProjectFromFile(filePath);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to load project: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Resolves the first non-switch command-line argument to an existing project file and loads it into <see cref="Project"/>.
        /// Returns <c>null</c> when no startup project file is supplied or the file cannot be loaded.
        /// </summary>
        public AutomationProject? TryLoadProjectFromCommandLineArgs(string[]? args)
        {
            string? filePath = GetProjectFilePathFromCommandLineArgs(args);
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            if (!File.Exists(filePath))
            {
                Log("WARN", $"Startup project file not found: {filePath}", AutomationLogLevel.Warning);
                return null;
            }

            return TryLoadProjectFromFile(filePath);
        }

        /// <summary>
        /// Loads a startup project from command-line arguments. Alias for <see cref="TryLoadProjectFromCommandLineArgs"/>.
        /// </summary>
        public AutomationProject? TryHandleStartupArguments(string[]? args)
        {
            return TryLoadProjectFromCommandLineArgs(args);
        }

        /// <summary>
        /// Extracts the first non-switch command-line argument as a potential project file path.
        /// </summary>
        public static string? GetProjectFilePathFromCommandLineArgs(string[]? args)
        {
            if (args == null)
                return null;

            foreach (string arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                string candidate = arg.Trim().Trim('"');
                if (candidate.Length == 0 || candidate.StartsWith("-", StringComparison.Ordinal))
                    continue;

                return candidate;
            }

            return null;
        }

        /// <summary>
        /// Saves <see cref="Project"/> to the specified file path.
        /// </summary>
        public void SaveProjectToFile(string filePath)
        {
            Project.SaveToFile(filePath);
            Log("SYS", $"Saved project to {filePath}", AutomationLogLevel.Success);
        }

        /// <summary>
        /// Attempts to save <see cref="Project"/> to disk, logging an error and returning <c>false</c> on failure.
        /// </summary>
        public bool TrySaveProjectToFile(string filePath)
        {
            try
            {
                SaveProjectToFile(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to save project: {ex.Message}", AutomationLogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Updates whether the project matches any site.
        /// </summary>
        public void SetMatchAnySite(bool matchAnySite)
        {
            Project.MatchAnySite = matchAnySite;
        }

        /// <summary>
        /// Adds a trimmed URL match pattern to the project.
        /// </summary>
        public string? AddMatchUrl(string? url)
        {
            return Project.AddMatchUrl(url);
        }

        /// <summary>
        /// Updates the URL match pattern at <paramref name="index"/> in the project.
        /// </summary>
        public bool UpdateMatchUrl(int index, string? url)
        {
            return Project.UpdateMatchUrl(index, url);
        }

        /// <summary>
        /// Removes a URL match pattern from the project.
        /// </summary>
        public bool RemoveMatchUrl(string? url)
        {
            return Project.RemoveMatchUrl(url);
        }

        /// <summary>
        /// Creates and appends a new <see cref="AutomationSequence"/> to the project.
        /// </summary>
        public AutomationSequence AddSequence(string? name)
        {
            return Project.AddSequence(name ?? string.Empty);
        }

        /// <summary>
        /// Adds a new <see cref="AutomationSequence"/> only when <paramref name="name"/> is non-empty.
        /// Returns <c>null</c> when the supplied name is blank or canceled.
        /// </summary>
        public AutomationSequence? TryAddSequence(string? name)
        {
            return Project.TryAddSequence(name);
        }

        /// <summary>
        /// Removes <paramref name="sequence"/> from the project.
        /// </summary>
        public bool RemoveSequence(AutomationSequence? sequence)
        {
            return Project.RemoveSequence(sequence);
        }

        /// <summary>
        /// Renames <paramref name="sequence"/> and updates scrape references to its previous name.
        /// </summary>
        public bool RenameSequence(AutomationSequence? sequence, string? newName)
        {
            return Project.RenameSequence(sequence, newName);
        }

        /// <summary>
        /// Moves the sequence at <paramref name="index"/> one position up.
        /// </summary>
        public bool MoveSequenceUp(int index)
        {
            return Project.MoveSequenceUp(index);
        }

        /// <summary>
        /// Moves the sequence at <paramref name="index"/> one position up in both the project and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveSequenceUp(System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (!Project.MoveSequenceUp(index))
                return false;

            newIndex = index - 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Moves the sequence at <paramref name="index"/> one position down.
        /// </summary>
        public bool MoveSequenceDown(int index)
        {
            return Project.MoveSequenceDown(index);
        }

        /// <summary>
        /// Moves the sequence at <paramref name="index"/> one position down in both the project and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveSequenceDown(System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (!Project.MoveSequenceDown(index))
                return false;

            newIndex = index + 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Updates the active state of <paramref name="sequence"/>.
        /// </summary>
        public void SetSequenceActive(AutomationSequence? sequence, bool isActive)
        {
            if (sequence != null)
                sequence.IsActive = isActive;
        }

        /// <summary>
        /// Removes <paramref name="step"/> from <paramref name="sequence"/>.
        /// </summary>
        public bool RemoveStep(AutomationSequence? sequence, AutomationStep? step)
        {
            return sequence != null && sequence.RemoveStep(step);
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position up within <paramref name="sequence"/>.
        /// </summary>
        public bool MoveStepUp(AutomationSequence? sequence, int index)
        {
            return sequence != null && sequence.MoveStepUp(index);
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position up in both <paramref name="sequence"/> and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveStepUp(AutomationSequence? sequence, System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (sequence == null || !sequence.MoveStepUp(index))
                return false;

            newIndex = index - 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position down within <paramref name="sequence"/>.
        /// </summary>
        public bool MoveStepDown(AutomationSequence? sequence, int index)
        {
            return sequence != null && sequence.MoveStepDown(index);
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position down in both <paramref name="sequence"/> and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveStepDown(AutomationSequence? sequence, System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (sequence == null || !sequence.MoveStepDown(index))
                return false;

            newIndex = index + 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Creates and appends a new <see cref="ScrapeDefinition"/> to the project.
        /// </summary>
        public ScrapeDefinition AddScrape(string? name)
        {
            return Project.AddScrape(name ?? string.Empty);
        }

        /// <summary>
        /// Adds a new <see cref="ScrapeDefinition"/> only when <paramref name="name"/> is non-empty.
        /// Returns <c>null</c> when the supplied name is blank or canceled.
        /// </summary>
        public ScrapeDefinition? TryAddScrape(string? name)
        {
            return Project.TryAddScrape(name);
        }

        /// <summary>
        /// Creates a new <see cref="ScrapeDefinition"/> and immediately adds a field from <paramref name="domNode"/>.
        /// Returns <c>null</c> when the scrape name is blank or the DOM node cannot be applied.
        /// </summary>
        public ScrapeDefinition? CreateScrapeWithFieldFromDomNode(
            string? name,
            DomNode? domNode,
            bool matchAllElements,
            DomNode? fallbackDomRoot = null)
        {
            var scrape = Project.TryAddScrape(name);
            if (scrape == null)
                return null;

            if (AddFieldFromDomNode(scrape, domNode, matchAllElements, fallbackDomRoot) == null)
            {
                Project.RemoveScrape(scrape);
                return null;
            }

            return scrape;
        }

        /// <summary>
        /// Creates a new <see cref="ScrapeDefinition"/> and immediately sets its repeating container from <paramref name="domNode"/>.
        /// Returns <c>null</c> when the scrape name is blank or the DOM node cannot be applied.
        /// </summary>
        public ScrapeDefinition? CreateScrapeWithContainerFromDomNode(
            string? name,
            DomNode? domNode,
            DomNode? fallbackDomRoot = null)
        {
            var scrape = Project.TryAddScrape(name);
            if (scrape == null)
                return null;

            if (SetContainerFromDomNode(scrape, domNode, fallbackDomRoot) == null)
            {
                Project.RemoveScrape(scrape);
                return null;
            }

            return scrape;
        }

        /// <summary>
        /// Creates a new <see cref="ScrapeDefinition"/> and immediately configures its pagination Next button from <paramref name="domNode"/>.
        /// Returns <c>null</c> when the scrape name is blank or the DOM node cannot be applied.
        /// </summary>
        public ScrapeDefinition? CreateScrapeWithPaginationFromDomNode(
            string? name,
            DomNode? domNode,
            DomNode? fallbackDomRoot = null)
        {
            var scrape = Project.TryAddScrape(name);
            if (scrape == null)
                return null;

            if (SetPaginationNextFromDomNode(scrape, domNode, fallbackDomRoot) == null)
            {
                Project.RemoveScrape(scrape);
                return null;
            }

            return scrape;
        }

        /// <summary>
        /// Removes <paramref name="scrape"/> from the project.
        /// </summary>
        public bool RemoveScrape(ScrapeDefinition? scrape)
        {
            return Project.RemoveScrape(scrape);
        }

        /// <summary>
        /// Renames <paramref name="scrape"/>.
        /// </summary>
        public bool RenameScrape(ScrapeDefinition? scrape, string? newName)
        {
            return Project.RenameScrape(scrape, newName);
        }

        /// <summary>
        /// Moves the scrape at <paramref name="index"/> one position up.
        /// </summary>
        public bool MoveScrapeUp(int index)
        {
            return Project.MoveScrapeUp(index);
        }

        /// <summary>
        /// Moves the scrape at <paramref name="index"/> one position up in both the project and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveScrapeUp(System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (!Project.MoveScrapeUp(index))
                return false;

            newIndex = index - 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Moves the scrape at <paramref name="index"/> one position down.
        /// </summary>
        public bool MoveScrapeDown(int index)
        {
            return Project.MoveScrapeDown(index);
        }

        /// <summary>
        /// Moves the scrape at <paramref name="index"/> one position down in both the project and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveScrapeDown(System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (!Project.MoveScrapeDown(index))
                return false;

            newIndex = index + 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Updates the active state of <paramref name="scrape"/>.
        /// </summary>
        public void SetScrapeActive(ScrapeDefinition? scrape, bool isActive)
        {
            if (scrape != null)
                scrape.IsActive = isActive;
        }

        /// <summary>
        /// Updates the container, automation hooks, and pagination settings on <paramref name="scrape"/>.
        /// </summary>
        public void UpdateScrapeSettings(
            ScrapeDefinition? scrape,
            string? containerSelector,
            string? preSequenceName,
            string? postSequenceName,
            bool enablePagination,
            int maxPages,
            int startPage,
            int pageDelayMs,
            string? nextPageSelector,
            string? paginationUrlPattern)
        {
            scrape?.UpdateSettings(
                containerSelector,
                preSequenceName,
                postSequenceName,
                enablePagination,
                maxPages,
                startPage,
                pageDelayMs,
                nextPageSelector,
                paginationUrlPattern);
        }

        /// <summary>
        /// Removes <paramref name="field"/> from <paramref name="scrape"/>.
        /// </summary>
        public bool RemoveFieldFromScrape(ScrapeDefinition? scrape, ScrapeField? field)
        {
            return scrape != null && scrape.RemoveField(field);
        }

        /// <summary>
        /// Moves the field at <paramref name="index"/> one position up within <paramref name="scrape"/>.
        /// </summary>
        public bool MoveFieldUp(ScrapeDefinition? scrape, int index)
        {
            return scrape != null && scrape.MoveFieldUp(index);
        }

        /// <summary>
        /// Moves the field at <paramref name="index"/> one position up in both <paramref name="scrape"/> and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveFieldUp(ScrapeDefinition? scrape, System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (scrape == null || !scrape.MoveFieldUp(index))
                return false;

            newIndex = index - 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Moves the field at <paramref name="index"/> one position down within <paramref name="scrape"/>.
        /// </summary>
        public bool MoveFieldDown(ScrapeDefinition? scrape, int index)
        {
            return scrape != null && scrape.MoveFieldDown(index);
        }

        /// <summary>
        /// Moves the field at <paramref name="index"/> one position down in both <paramref name="scrape"/> and <paramref name="uiItems"/>.
        /// Returns the new index via <paramref name="newIndex"/>.
        /// </summary>
        public bool MoveFieldDown(ScrapeDefinition? scrape, System.Collections.IList uiItems, int index, out int newIndex)
        {
            newIndex = index;
            if (scrape == null || !scrape.MoveFieldDown(index))
                return false;

            newIndex = index + 1;
            return MoveItemInList(uiItems, index, newIndex);
        }

        /// <summary>
        /// Generates a Tampermonkey userscript for the current <see cref="Project"/>.
        /// </summary>
        public string GenerateUserscript(int port, IpcMode mode)
        {
            if (!Project.HasValidMatchRules)
            {
                Log("WARN", "No Match URLs provided. Defaulting to match any site.", AutomationLogLevel.Warning);
            }

            port = ClampPort(port);
            string script = ScriptGenerator.Generate(Project, port, mode);
            Log("SYS", $"Tampermonkey script generated for {mode} and copied to clipboard!", AutomationLogLevel.Success);
            return script;
        }

        /// <summary>
        /// Exports the unpacked BAF Chrome Extension to <paramref name="folderPath"/> and returns the user load instructions.
        /// </summary>
        public string ExportChromeExtension(string folderPath, int port, IpcMode mode)
        {
            port = ClampPort(port);
            string savedPath = ChromeExtensionGenerator.ExportToDirectory(folderPath, port, mode);
            Log("EXT", $"Exported unpacked Chrome Extension (Port {port}, {mode}) to: {savedPath}", AutomationLogLevel.Success);
            return ChromeExtensionGenerator.GetLoadInstructions(savedPath);
        }

        /// <summary>
        /// Attempts to export the unpacked BAF Chrome Extension, logging an error and returning <c>null</c> on failure.
        /// </summary>
        public string? TryExportChromeExtension(string folderPath, int port, IpcMode mode)
        {
            try
            {
                return ExportChromeExtension(folderPath, port, mode);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to export Chrome Extension: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Resolves the target <see cref="BrowserClient"/> (using <paramref name="explicitClient"/> or <see cref="ActiveClient"/>),
        /// logging a warning if no client is selected.
        /// </summary>
        public BrowserClient? RequireClient(BrowserClient? explicitClient = null, string warningMessage = "Please select a connected client first.")
        {
            var client = explicitClient ?? ActiveClient;
            if (client == null)
            {
                Log("WARN", warningMessage, AutomationLogLevel.Warning);
            }
            return client;
        }

        /// <summary>
        /// Resolves the most recent root <see cref="DomNode"/> from <see cref="ActiveClient"/> or <paramref name="fallbackDomRoot"/>.
        /// </summary>
        public DomNode? ResolveDomRoot(DomNode? fallbackDomRoot = null)
        {
            return ActiveClient?.ResolveDomRoot(fallbackDomRoot) ?? fallbackDomRoot;
        }

        /// <summary>
        /// Determines whether <paramref name="domNode"/> has a valid path and a target <see cref="BrowserClient"/> is available.
        /// </summary>
        public bool CanTargetDomNode(DomNode? domNode, BrowserClient? client = null)
        {
            return domNode != null && domNode.HasPath && (client ?? ActiveClient) != null;
        }

        /// <summary>
        /// Records a user-initiated <see cref="DomNode"/> selection and highlights it in the connected browser tab.
        /// </summary>
        public void SelectDomNode(DomNode? domNode, BrowserClient? client = null)
        {
            (client ?? ActiveClient)?.SelectDomNode(domNode);
        }

        /// <summary>
        /// Determines whether <paramref name="client"/> is currently the active client managed by this session.
        /// </summary>
        public bool IsActiveClient(BrowserClient? client)
        {
            return client != null &&
                   ActiveClient != null &&
                   string.Equals(client.InstanceId, ActiveClient.InstanceId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Refreshes the DOM snapshot for the active <see cref="BrowserClient"/> and invokes
        /// <paramref name="onSnapshotReady"/> only while the originating client remains active.
        /// </summary>
        public async Task RefreshActiveClientDomAsync(
            BrowserClient? client = null,
            string? targetSelectedPath = null,
            bool hasTargetPath = false,
            Func<string?>? getCurrentSelectedPath = null,
            Action<BrowserClient, DomSnapshotRefreshResult>? onSnapshotReady = null)
        {
            client ??= ActiveClient;
            if (client == null || !IsActiveClient(client))
                return;

            try
            {
                await client.RefreshDomSnapshotCoalescedAsync(
                    getCurrentSelectedPath: getCurrentSelectedPath ?? (() => null),
                    onSnapshotReady: result =>
                    {
                        if (!IsActiveClient(client))
                            return;

                        onSnapshotReady?.Invoke(client, result);
                    },
                    targetSelectedPath: targetSelectedPath,
                    hasTargetPath: hasTargetPath).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to fetch DOM: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Handles a browser middle-click DOM selection for the active <see cref="BrowserClient"/>,
        /// trying the existing tree first and falling back to a coalesced snapshot refresh.
        /// </summary>
        public async Task HandleActiveClientElementClickedAsync(
            ElementClickedEventArgs e,
            BrowserClient? client = null,
            Func<string, bool>? trySelectExistingPath = null,
            Action<BrowserClient>? onSelectedInExistingTree = null,
            Func<string?>? getCurrentSelectedPath = null,
            Action<BrowserClient, DomSnapshotRefreshResult>? onSnapshotReady = null)
        {
            if (e == null)
                return;

            client ??= ActiveClient;
            if (client == null || !IsActiveClient(client))
                return;

            try
            {
                await client.HandleElementClickedOrRefreshDomAsync(
                    clickedPath: e.Path,
                    trySelectExistingPath: path => trySelectExistingPath?.Invoke(path) ?? false,
                    onSelectedInExistingTree: () => onSelectedInExistingTree?.Invoke(client),
                    getCurrentSelectedPath: getCurrentSelectedPath ?? (() => null),
                    onSnapshotReady: result =>
                    {
                        if (!IsActiveClient(client))
                            return;

                        onSnapshotReady?.Invoke(client, result);
                    }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to handle element click: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Instructs the Chrome Extension to inject and switch to <paramref name="tab"/>.
        /// Returns any already-connected <see cref="BrowserClient"/> for that tab if an immediate DOM refresh should be triggered.
        /// </summary>
        public async Task<BrowserClient?> SwitchToExtensionTabAsync(
            BrowserTabInfo? tab,
            IpcMode mode,
            bool focusBrowserTab = false,
            bool skipIfAlreadyActive = false,
            CancellationToken cancellationToken = default)
        {
            if (tab == null)
            {
                Log("WARN", "Please select a browser tab from the Chrome Extension tabs list first.", AutomationLogLevel.Warning);
                return null;
            }

            if (_server == null)
            {
                Log("WARN", "Start the Automation Server first.", AutomationLogLevel.Warning);
                return null;
            }

            if (skipIfAlreadyActive && tab.IsAlreadyActiveClient(ActiveClient, _exclusiveTabMode))
            {
                return null;
            }

            Log("EXT", $"Injecting & switching to Tab #{tab.TabId}: {tab.Title} ({tab.Url})", AutomationLogLevel.Extension);
            BrowserClient? previousActiveClient = ActiveClient;
            try
            {
                var existingClient = await _server.SwitchToExtensionTabAsync(tab, focusBrowserTab, _exclusiveTabMode, mode, skipIfAlreadyActive: false, cancellationToken);

                if (existingClient != null &&
                    ReferenceEquals(previousActiveClient, existingClient) &&
                    IsActiveClient(existingClient))
                {
                    OnActiveClientChanged?.Invoke(this, existingClient);
                }

                return existingClient;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to switch to tab #{tab.TabId}: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Returns all currently hooked/injected browser tabs known to the active server.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetHookedExtensionTabs()
        {
            return _server?.GetHookedExtensionTabs() ?? Array.Empty<BrowserTabInfo>();
        }

        /// <summary>
        /// Detaches the automation bridge from <paramref name="tab"/>.
        /// </summary>
        public async Task DetachExtensionTabAsync(BrowserTabInfo? tab, CancellationToken cancellationToken = default)
        {
            if (_server == null) return;
            if (tab == null)
            {
                Log("WARN", "Please select a tab to detach.", AutomationLogLevel.Warning);
                return;
            }

            Log("EXT", $"Detaching automation bridge from Tab #{tab.TabId}...", AutomationLogLevel.ServerStopped);
            try
            {
                await _server.DetachExtensionTabAsync(tab.TabId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to detach tab: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Detects any currently hooked Chrome Extension tabs and detaches the automation bridge from each of them.
        /// </summary>
        public async Task<int> DetachHookedExtensionTabsAsync(CancellationToken cancellationToken = default)
        {
            if (_server == null) return 0;

            var hookedTabs = _server.GetHookedExtensionTabs();
            foreach (var tab in hookedTabs)
            {
                await DetachExtensionTabAsync(tab, cancellationToken);
            }

            return hookedTabs.Count;
        }

        /// <summary>
        /// Detects and detaches any hooked browser tabs, waits briefly for the extension to process the detach command,
        /// and then stops the active <see cref="AutomationServer"/>.
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            if (_server == null || !_server.IsRunning)
                return;

            int detachedCount = await DetachHookedExtensionTabsAsync(cancellationToken);
            if (detachedCount > 0)
            {
                try
                {
                    await Task.Delay(100, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }

            StopServer();
        }

        /// <summary>
        /// Brings <paramref name="tab"/> to the foreground in Chrome.
        /// </summary>
        public async Task FocusExtensionTabAsync(BrowserTabInfo? tab, CancellationToken cancellationToken = default)
        {
            if (_server == null) return;
            if (tab == null)
            {
                Log("WARN", "Please select a tab to focus in Chrome.", AutomationLogLevel.Warning);
                return;
            }

            try
            {
                await _server.FocusExtensionTabAsync(tab.TabId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to focus tab: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Requests an immediate refresh of open tabs from the Chrome Extension.
        /// </summary>
        public async Task RefreshExtensionTabsAsync(CancellationToken cancellationToken = default)
        {
            if (_server == null) return;
            try
            {
                await _server.RequestExtensionTabsRefreshAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to refresh tabs: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Sends a raw command and payload to the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<CommandResult?> SendRawCommandAsync(string type, string? payloadStr, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client, "Please select a client from the list first.");
            if (target == null) return null;

            Log($"TX -> {target.InstanceId}", $"{type} | Payload: {payloadStr}", AutomationLogLevel.CommandTx);
            try
            {
                var result = await target.SendRawCommandAsync(type, payloadStr, cancellationToken);
                Log($"RX <- {target.InstanceId}", $"Status: {result.Status} | Output: {result.Output}", AutomationLogLevel.CommandRx);
                return result;
            }
            catch (Exception ex)
            {
                Log($"ERR <- {target.InstanceId}", ex.Message, AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Sends a <c>REACT_CLICK</c> command for <paramref name="selector"/> to the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<CommandResult?> TestReactClickAsync(string selector, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = client ?? ActiveClient;
            if (target == null) return null;

            string trimmed = selector?.Trim() ?? string.Empty;
            Log($"TX -> {target.InstanceId}", $"REACT_CLICK | Selector: {trimmed}", AutomationLogLevel.CommandTx);
            try
            {
                var result = await target.ClickReactElementAsync(trimmed, cancellationToken);
                Log($"RX <- {target.InstanceId}", $"Status: {result.Status} | Output: {result.Output}", AutomationLogLevel.CommandRx);
                return result;
            }
            catch (Exception ex)
            {
                Log("ERR", ex.Message, AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Evaluates JavaScript <paramref name="code"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<CommandResult?> TestEvalAsync(string code, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = client ?? ActiveClient;
            if (target == null) return null;

            string trimmed = code?.Trim() ?? string.Empty;
            Log($"TX -> {target.InstanceId}", $"EVAL | Code: {trimmed}", AutomationLogLevel.CommandTx);
            try
            {
                var result = await target.EvaluateJavaScriptAsync(trimmed, cancellationToken);
                Log($"RX <- {target.InstanceId}", $"Status: {result.Status} | Output: {result.Output}", AutomationLogLevel.CommandRx);
                return result;
            }
            catch (Exception ex)
            {
                Log("ERR", ex.Message, AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Tests clicking a <see cref="DomNode"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<CommandResult?> ClickDomNodeAsync(DomNode? domNode, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = client ?? ActiveClient;
            if (domNode == null || !domNode.HasPath || target == null)
                return null;

            Log("SYS", $"Testing click on {domNode.Path}", AutomationLogLevel.System);
            try
            {
                var res = await target.ClickReactElementAsync(domNode, cancellationToken);
                Log("RX", res.Output ?? string.Empty, AutomationLogLevel.CommandRx);
                return res;
            }
            catch (Exception ex)
            {
                Log("ERR", ex.Message, AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Tests setting an input <paramref name="value"/> on <paramref name="domNode"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<CommandResult?> SetDomNodeValueAsync(DomNode? domNode, string? value, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = client ?? ActiveClient;
            if (domNode == null || !domNode.HasPath || target == null || string.IsNullOrEmpty(value))
                return null;

            Log("SYS", $"Testing set value on {domNode.Path}", AutomationLogLevel.System);
            try
            {
                var res = await target.SetReactInputValueAsync(domNode, value, cancellationToken: cancellationToken);
                Log("RX", res.Output ?? string.Empty, AutomationLogLevel.CommandRx);
                return res;
            }
            catch (Exception ex)
            {
                Log("ERR", ex.Message, AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Tests extracting text/value from <paramref name="domNode"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<string?> QuickScrapeDomNodeAsync(DomNode? domNode, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = client ?? ActiveClient;
            if (domNode == null || !domNode.HasPath || target == null)
                return null;

            var extType = domNode.GetSuggestedExtractionType();
            Log("SCRAPE", $"Testing extraction ({extType}) on {domNode.Path}...", AutomationLogLevel.ScrapeStart);
            try
            {
                string? val = await target.ScrapeElementAsync(domNode, extType, cancellationToken: cancellationToken);
                Log("SCRAPE-RX", $"Extracted Value: {val}", AutomationLogLevel.CommandRx);
                return val;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Quick scrape failed: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Creates an <see cref="AutomationStep"/> from <paramref name="domNode"/> and appends it to <paramref name="sequence"/>.
        /// </summary>
        public AutomationStep? AddStepFromDomNode(AutomationSequence? sequence, DomNode? domNode, AutomationActionType actionType)
        {
            if (sequence == null || domNode == null || !domNode.HasPath)
                return null;

            var step = sequence.AddStepFromDomNode(domNode, actionType);
            Log("SYS", $"Added step to list '{sequence.Name}'", AutomationLogLevel.System);
            return step;
        }

        /// <summary>
        /// Creates a blank <see cref="AutomationStep"/> and appends it to <paramref name="sequence"/>,
        /// logging a warning when no sequence is selected.
        /// </summary>
        public AutomationStep? AddStepToSequence(AutomationSequence? sequence)
        {
            if (sequence == null)
            {
                Log("WARN", "Please select an automation list first.", AutomationLogLevel.Warning);
                return null;
            }

            var step = sequence.AddStep();
            Log("SYS", $"Added step to list '{sequence.Name}'", AutomationLogLevel.System);
            return step;
        }

        /// <summary>
        /// Creates a blank <see cref="ScrapeField"/> and appends it to <paramref name="scrape"/>,
        /// logging a warning when no scrape definition is selected.
        /// </summary>
        public ScrapeField? AddFieldToScrape(ScrapeDefinition? scrape, string baseName = "Field")
        {
            if (scrape == null)
            {
                Log("WARN", "Please select or create a scrape list first.", AutomationLogLevel.Warning);
                return null;
            }

            var field = scrape.AddField(baseName);
            Log("SCRAPE", $"Added field '{field.Name}' to '{scrape.Name}'.", AutomationLogLevel.ScrapeStart);
            return field;
        }

        /// <summary>
        /// Creates a <see cref="ScrapeField"/> from <paramref name="domNode"/> and adds it to <paramref name="scrape"/>.
        /// </summary>
        public ScrapeField? AddFieldFromDomNode(ScrapeDefinition? scrape, DomNode? domNode, bool matchAllElements, DomNode? fallbackDomRoot = null)
        {
            if (scrape == null || domNode == null || !domNode.HasPath)
                return null;

            var field = scrape.AddFieldFromDomNode(domNode, matchAllElements, ResolveDomRoot(fallbackDomRoot));
            Log("SCRAPE", $"Added field '{field.Name}' ({field.ExtractionType}) to '{scrape.Name}'.", AutomationLogLevel.ScrapeStart);
            return field;
        }

        /// <summary>
        /// Sets the repeating container selector on <paramref name="scrape"/> from <paramref name="domNode"/>.
        /// </summary>
        public string? SetContainerFromDomNode(ScrapeDefinition? scrape, DomNode? domNode, DomNode? fallbackDomRoot = null)
        {
            if (scrape == null || domNode == null || !domNode.HasPath)
                return null;

            string generalizedContainer = scrape.SetContainerFromDomNode(domNode, ResolveDomRoot(fallbackDomRoot));
            Log("SCRAPE", $"Set repeating container for '{scrape.Name}' to: {generalizedContainer}", AutomationLogLevel.ScrapeStart);
            return generalizedContainer;
        }

        /// <summary>
        /// Configures pagination Next button on <paramref name="scrape"/> from <paramref name="domNode"/>.
        /// </summary>
        public string? SetPaginationNextFromDomNode(ScrapeDefinition? scrape, DomNode? domNode, DomNode? fallbackDomRoot = null)
        {
            if (scrape == null || domNode == null || !domNode.HasPath)
                return null;

            string selector = scrape.SetPaginationNextFromDomNode(domNode, ResolveDomRoot(fallbackDomRoot));
            Log("SCRAPE", $"Set pagination Next button for '{scrape.Name}' to: {selector}", AutomationLogLevel.ScrapeStart);
            return selector;
        }

        /// <summary>
        /// Generalizes <paramref name="field"/>'s selector using the active client's DOM root (or <paramref name="fallbackDomRoot"/>).
        /// Returns <c>true</c> when the field selector was updated.
        /// </summary>
        public bool GeneralizeScrapeFieldSelector(ScrapeField? field, DomNode? fallbackDomRoot = null)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.Selector))
                return false;

            string generalized = field.GeneralizeSelector(ResolveDomRoot(fallbackDomRoot));
            Log("SCRAPE", $"Generalized selector for field '{field.Name}' to: {generalized}", AutomationLogLevel.ScrapeStart);
            return true;
        }

        /// <summary>
        /// Generalizes a container CSS selector using the active client's DOM root (or <paramref name="fallbackDomRoot"/>).
        /// </summary>
        public string GeneralizeContainerSelector(string rawSelector, ScrapeDefinition? scrape = null, DomNode? fallbackDomRoot = null)
        {
            return ActiveClient.GeneralizeContainerSelector(rawSelector, scrape, fallbackDomRoot);
        }

        /// <summary>
        /// Generalizes a non-empty container selector, returning <c>false</c> when <paramref name="rawSelector"/> is blank.
        /// </summary>
        public bool TryGeneralizeContainerSelector(
            string? rawSelector,
            ScrapeDefinition? scrape,
            DomNode? fallbackDomRoot,
            out string generalizedSelector)
        {
            generalizedSelector = string.Empty;
            if (string.IsNullOrWhiteSpace(rawSelector))
                return false;

            generalizedSelector = GeneralizeContainerSelector(rawSelector, scrape, fallbackDomRoot);
            return true;
        }

        /// <summary>
        /// Highlights all elements matching a container selector on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<int> HighlightContainerAsync(string rawSelector, ScrapeDefinition? scrape = null, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client);
            if (target == null) return 0;

            string selector = rawSelector?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(selector))
            {
                Log("WARN", "Container selector is empty.", AutomationLogLevel.Warning);
                return 0;
            }

            try
            {
                int count = await target.HighlightContainerOrSelectorAsync(selector, scrape, cancellationToken);
                Log("SCRAPE", $"Highlighted {count} container element(s) matching '{selector}'.", AutomationLogLevel.ScrapeStart);
                return count;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to highlight container: {ex.Message}", AutomationLogLevel.Error);
                return 0;
            }
        }

        /// <summary>
        /// Highlights all elements matching <paramref name="field"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<int> HighlightScrapeFieldAsync(ScrapeField? field, ScrapeDefinition? parentScrape = null, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client);
            if (target == null) return 0;

            if (field == null || string.IsNullOrWhiteSpace(field.Selector))
            {
                Log("WARN", "Please select a field with a valid selector first.", AutomationLogLevel.Warning);
                return 0;
            }

            try
            {
                int count = await target.HighlightScrapeFieldAsync(field, parentScrape, cancellationToken);
                Log("SCRAPE", $"Highlighted {count} element(s) for field '{field.Name}'.", AutomationLogLevel.ScrapeStart);
                return count;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Highlight failed: {ex.Message}", AutomationLogLevel.Error);
                return 0;
            }
        }

        /// <summary>
        /// Executes all active <see cref="AutomationSequence"/> items in <see cref="Project"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<bool> RunActiveSequencesAsync(BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client, "Please select a client first.");
            if (target == null) return false;

            if (!Project.HasActiveSequences)
            {
                Log("WARN", "No active automation lists selected to run.", AutomationLogLevel.Warning);
                return false;
            }

            Log("SYS", "Starting Automation Run...", AutomationLogLevel.Automation);
            try
            {
                await target.ExecuteProjectAsync(Project, Runner, cancellationToken);
                Log("SYS", "Automation Run Completed.", AutomationLogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Automation aborted: {ex.Message}", AutomationLogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Previews a single <see cref="ScrapeDefinition"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<ScrapeResult?> PreviewScrapeAsync(ScrapeDefinition? scrape, BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client);
            if (target == null) return null;

            if (scrape == null)
            {
                Log("WARN", "Please select a scrape list to preview.", AutomationLogLevel.Warning);
                return null;
            }

            if (!scrape.HasFields)
            {
                Log("WARN", "The selected scrape list has no fields defined.", AutomationLogLevel.Warning);
                return null;
            }

            try
            {
                var result = await target.ExecuteScrapeAsync(scrape, Project.Sequences, Scraper, cancellationToken);
                SetLastScrapeResult(result);
                return result;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Scrape preview failed: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Executes all active <see cref="ScrapeDefinition"/> items in <see cref="Project"/> and combines their results.
        /// </summary>
        public async Task<ScrapeResult?> RunActiveScrapesAsync(BrowserClient? client = null, CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client);
            if (target == null) return null;

            if (!Project.HasActiveScrapes)
            {
                Log("WARN", "No active scrape lists selected to run.", AutomationLogLevel.Warning);
                return null;
            }

            try
            {
                var combined = await target.ExecuteProjectScrapesCombinedAsync(Project, Scraper, cancellationToken: cancellationToken);
                SetLastScrapeResult(combined);
                return combined;
            }
            catch (Exception ex)
            {
                Log("ERR", $"Active scrapes run aborted: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Returns the JSON string for <see cref="LastScrapeResult"/> if it contains records, or logs a warning and returns <c>null</c>.
        /// </summary>
        public string? GetLastScrapeJsonForCopy()
        {
            if (LastScrapeResult == null || !LastScrapeResult.HasRecords)
            {
                Log("WARN", "No scrape results available to copy.", AutomationLogLevel.Warning);
                return null;
            }

            string json = LastScrapeResult.ToJson(true, true);
            Log("SCRAPE", "Copied scraped JSON records to clipboard.", AutomationLogLevel.Success);
            return json;
        }

        /// <summary>
        /// Validates that <see cref="LastScrapeResult"/> contains records for file export.
        /// </summary>
        public bool EnsureHasScrapeResultForExport()
        {
            if (LastScrapeResult == null || !LastScrapeResult.HasRecords)
            {
                Log("WARN", "No scrape results available to export.", AutomationLogLevel.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that <see cref="LastScrapeResult"/> is available for export and resolves its suggested file name
        /// using <paramref name="extension"/>.
        /// </summary>
        public bool TryGetLastScrapeSuggestedFileName(string extension, out string suggestedFileName)
        {
            suggestedFileName = string.Empty;
            if (!EnsureHasScrapeResultForExport() || LastScrapeResult == null)
                return false;

            suggestedFileName = LastScrapeResult.GetSuggestedFileName(extension);
            return true;
        }

        /// <summary>
        /// Exports <see cref="LastScrapeResult"/> as JSON to <paramref name="filePath"/>.
        /// </summary>
        public void ExportLastScrapeToJsonFile(string filePath)
        {
            if (LastScrapeResult == null) return;
            try
            {
                LastScrapeResult.SaveToJsonFile(filePath, recordsOnly: true);
                Log("SCRAPE", $"Exported {LastScrapeResult.RecordCount} record(s) to {filePath}", AutomationLogLevel.Success);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to export JSON: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Exports <see cref="LastScrapeResult"/> as CSV to <paramref name="filePath"/>.
        /// </summary>
        public void ExportLastScrapeToCsvFile(string filePath)
        {
            if (LastScrapeResult == null) return;
            try
            {
                LastScrapeResult.SaveToCsvFile(filePath);
                Log("SCRAPE", $"Exported {LastScrapeResult.RecordCount} record(s) to {filePath}", AutomationLogLevel.Success);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to export CSV: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Executes a reverse-engineering site dump of <paramref name="dumpType"/> on the specified or active <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<SiteDumpResult?> ExecuteSiteDumpAsync(
            SiteDumpType dumpType,
            SiteDumpOptions? options = null,
            BrowserClient? client = null,
            CancellationToken cancellationToken = default)
        {
            var target = RequireClient(client);
            if (target == null) return null;

            OnDumpStatusChanged?.Invoke(this, $"Dumping {dumpType} from {target.InstanceId}...");
            try
            {
                var result = await target.DumpSiteAsync(dumpType, options ?? DumpOptions, SiteDumper, cancellationToken);
                SetLastSiteDumpResult(result);
                return result;
            }
            catch (Exception ex)
            {
                OnDumpStatusChanged?.Invoke(this, $"Dump failed: {ex.Message}");
                Log("ERR", $"Site dump ({dumpType}) failed: {ex.Message}", AutomationLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Resolves the text content and suggested file name for the active dump view.
        /// </summary>
        public string GetActiveDumpViewText(SiteDumpViewMode viewMode, SiteResourceItem? selectedResource, out string suggestedFileName)
        {
            suggestedFileName = "dump.txt";
            if (LastSiteDumpResult == null)
                return string.Empty;

            return LastSiteDumpResult.GetViewContent(viewMode, selectedResource, out suggestedFileName);
        }

        /// <summary>
        /// Filters the resources in <see cref="LastSiteDumpResult"/> by an optional search query and resource category.
        /// </summary>
        public IReadOnlyList<SiteResourceItem> FilterLastSiteDumpResources(
            string? filter,
            SiteResourceType? category = null,
            bool searchContent = true)
        {
            if (LastSiteDumpResult == null)
                return Array.Empty<SiteResourceItem>();

            return LastSiteDumpResult.FilterResources(filter?.Trim(), category, searchContent);
        }

        /// <summary>
        /// Filters the resources in <see cref="LastSiteDumpResult"/>, preserving the preferred selection when possible,
        /// and updates <see cref="ResourceFilterText"/> and <see cref="SelectedDumpResource"/>.
        /// </summary>
        public IReadOnlyList<SiteResourceItem> FilterLastSiteDumpResources(
            string? filter,
            SiteResourceItem? preferredSelection,
            out SiteResourceItem? resolvedSelection)
        {
            ResourceFilterText = filter;
            var resources = FilterLastSiteDumpResources(filter);
            resolvedSelection = ResolveDumpResourceSelection(resources, preferredSelection);
            SelectedDumpResource = resolvedSelection;
            return resources;
        }

        private static SiteResourceItem? ResolveDumpResourceSelection(
            IReadOnlyList<SiteResourceItem> resources,
            SiteResourceItem? preferredSelection)
        {
            if (resources.Count == 0)
                return null;
            if (preferredSelection == null)
                return resources[0];

            foreach (var resource in resources)
            {
                if (ReferenceEquals(resource, preferredSelection))
                    return resource;
            }

            foreach (var resource in resources)
            {
                if (resource.Index == preferredSelection.Index &&
                    resource.Category == preferredSelection.Category &&
                    string.Equals(resource.Name, preferredSelection.Name, StringComparison.Ordinal))
                {
                    return resource;
                }
            }

            foreach (var resource in resources)
            {
                if (resource.Index == preferredSelection.Index &&
                    resource.Category == preferredSelection.Category)
                {
                    return resource;
                }
            }

            return resources[0];
        }

        /// <summary>
        /// Updates <see cref="DumpOptions"/> from the high-level reverse-engineering inclusion flags.
        /// </summary>
        public void SetDumpOptions(bool includeExternal, bool includeInline, bool includeDynamicScripts)
        {
            DumpOptions = SiteDumpOptions.Create(includeExternal, includeInline, includeDynamicScripts);
        }

        /// <summary>
        /// Returns the text content for the active dump view if it is not empty, or logs a warning and returns <c>null</c>.
        /// </summary>
        public string? GetActiveDumpViewTextForCopy(SiteDumpViewMode viewMode, SiteResourceItem? selectedResource)
        {
            string content = GetActiveDumpViewText(viewMode, selectedResource, out string displayFileName);
            if (string.IsNullOrEmpty(content))
            {
                Log("WARN", "No dumped source content in the active view to copy.", AutomationLogLevel.Warning);
                return null;
            }

            Log("REV-ENG", $"Copied '{displayFileName}' ({content.Length:N0} chars) to clipboard.", AutomationLogLevel.Success);
            return content;
        }

        /// <summary>
        /// Returns the text content for the active dump view using <see cref="ActiveDumpViewMode"/> and <see cref="SelectedDumpResource"/>.
        /// </summary>
        public string? GetActiveDumpViewTextForCopy()
        {
            return GetActiveDumpViewTextForCopy(ActiveDumpViewMode, SelectedDumpResource);
        }

        /// <summary>
        /// Validates that the active dump view contains content for file export and resolves its suggested file name.
        /// </summary>
        public bool TryGetActiveDumpViewForExport(
            SiteDumpViewMode viewMode,
            SiteResourceItem? selectedResource,
            out string suggestedFileName)
        {
            suggestedFileName = "dump.txt";
            if (LastSiteDumpResult == null)
            {
                Log("WARN", "Please run a dump operation first.", AutomationLogLevel.Warning);
                return false;
            }

            string content = GetActiveDumpViewText(viewMode, selectedResource, out suggestedFileName);
            if (string.IsNullOrEmpty(content))
            {
                Log("WARN", "Active dump view is empty.", AutomationLogLevel.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that the active dump view contains content using <see cref="ActiveDumpViewMode"/> and <see cref="SelectedDumpResource"/>.
        /// </summary>
        public bool TryGetActiveDumpViewForExport(out string suggestedFileName)
        {
            return TryGetActiveDumpViewForExport(ActiveDumpViewMode, SelectedDumpResource, out suggestedFileName);
        }

        /// <summary>
        /// Validates that <see cref="LastSiteDumpResult"/> contains resources for folder export.
        /// </summary>
        public bool EnsureHasSiteDumpResultForExport()
        {
            if (LastSiteDumpResult == null || !LastSiteDumpResult.HasResources)
            {
                Log("WARN", "No site dump results available to save.", AutomationLogLevel.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the active dump view to <paramref name="filePath"/>.
        /// </summary>
        public void SaveActiveDumpViewToFile(string filePath, SiteDumpViewMode viewMode, SiteResourceItem? selectedResource, string displayFileName)
        {
            if (LastSiteDumpResult == null) return;
            try
            {
                LastSiteDumpResult.SaveViewContentToFile(filePath, viewMode, selectedResource);
                Log("REV-ENG", $"Saved '{displayFileName}' to {filePath}", AutomationLogLevel.Success);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to save file: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Saves the active dump view using <see cref="ActiveDumpViewMode"/> and <see cref="SelectedDumpResource"/>.
        /// </summary>
        public void SaveActiveDumpViewToFile(string filePath, string displayFileName)
        {
            SaveActiveDumpViewToFile(filePath, ActiveDumpViewMode, SelectedDumpResource, displayFileName);
        }

        /// <summary>
        /// Saves all resources in <see cref="LastSiteDumpResult"/> to <paramref name="folderPath"/>.
        /// </summary>
        public void SaveLastDumpToFolder(string folderPath)
        {
            if (LastSiteDumpResult == null) return;
            try
            {
                string savedPath = LastSiteDumpResult.SaveToDirectory(folderPath);
                Log("REV-ENG", $"Saved {LastSiteDumpResult.TotalResourceCount} resource(s) ({LastSiteDumpResult.GetFormattedTotalSize()}) to {savedPath}", AutomationLogLevel.Success);
            }
            catch (Exception ex)
            {
                Log("ERR", $"Failed to save dump to folder: {ex.Message}", AutomationLogLevel.Error);
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="AutomationServer"/> if created.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _server?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}