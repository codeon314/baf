using System;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Event arguments raised when the connected browser page's DOM mutates.
    /// </summary>
    public class DomChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the updated CSS path of the currently highlighted element on the page, if any.
        /// </summary>
        public string? HighlightedPath { get; }

        /// <summary>
        /// Gets whether the DOM change event payload included a highlighted path property.
        /// </summary>
        public bool HasHighlightedPath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomChangedEventArgs"/> class.
        /// </summary>
        public DomChangedEventArgs(string? highlightedPath, bool hasHighlightedPath)
        {
            HighlightedPath = highlightedPath;
            HasHighlightedPath = hasHighlightedPath;
        }
    }

    /// <summary>
    /// Event arguments raised when the user middle-clicks an element in the connected browser tab.
    /// </summary>
    public class ElementClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the CSS selector path of the middle-clicked DOM element.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementClickedEventArgs"/> class.
        /// </summary>
        public ElementClickedEventArgs(string path)
        {
            Path = path;
        }
    }

    /// <summary>
    /// Event arguments raised when a page load or in-place navigation completes in the browser tab.
    /// </summary>
    public class PageLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier generated for this page load instance.
        /// </summary>
        public string PageLoadId { get; }

        /// <summary>
        /// Gets the URL of the loaded page.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Gets the document readyState reported by the browser (typically <c>complete</c>).
        /// </summary>
        public string ReadyState { get; }

        /// <summary>
        /// Gets the HTTP status code detected for the page load (e.g., 200 or 429).
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the raw <c>Retry-After</c> header or value if the page returned HTTP 429.
        /// </summary>
        public string? RetryAfter { get; }

        /// <summary>
        /// Gets whether the page load returned an HTTP 429 rate-limit status code.
        /// </summary>
        public bool IsRateLimited => StatusCode == 429;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageLoadedEventArgs"/> class.
        /// </summary>
        public PageLoadedEventArgs(string pageLoadId, string url, string readyState, int statusCode = 200, string? retryAfter = null)
        {
            PageLoadId = pageLoadId;
            Url = url;
            ReadyState = readyState;
            StatusCode = statusCode;
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Returns a formatted log message describing the page load or HTTP 429 rate-limit state.
        /// </summary>
        /// <param name="instanceId">Optional client instance identifier to include in the prefix.</param>
        public string GetLogMessage(string? instanceId = null)
        {
            string prefix = !string.IsNullOrWhiteSpace(instanceId) ? $"[NAV <- {instanceId}] " : string.Empty;
            return IsRateLimited
                ? $"{prefix}Page rate-limited (HTTP 429): {Url}"
                : $"{prefix}Page loaded: {Url}";
        }

        /// <summary>
        /// Returns a concise status message describing the page load or HTTP 429 rate-limit state.
        /// </summary>
        public string GetStatusMessage()
        {
            return IsRateLimited
                ? $"Page rate-limited (HTTP 429): {Url}"
                : $"Page loaded: {Url}";
        }
    }

    /// <summary>
    /// Event arguments raised before waiting between paginated scrape pages.
    /// </summary>
    public class ScrapePageDelayEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ScrapeDefinition"/> currently executing.
        /// </summary>
        public ScrapeDefinition Definition { get; }

        /// <summary>
        /// Gets the 1-based page number that will be scraped after the delay finishes.
        /// </summary>
        public int NextPageNumber { get; }

        /// <summary>
        /// Gets the configured delay in milliseconds before turning to the next page.
        /// </summary>
        public int DelayMs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapePageDelayEventArgs"/> class.
        /// </summary>
        public ScrapePageDelayEventArgs(ScrapeDefinition definition, int nextPageNumber, int delayMs)
        {
            Definition = definition;
            NextPageNumber = nextPageNumber;
            DelayMs = delayMs;
        }

        /// <summary>
        /// Returns a formatted status message describing the page turn delay.
        /// </summary>
        public string GetStatusMessage()
        {
            return $"Waiting {DelayMs} ms before moving to page {NextPageNumber}...";
        }
    }

    /// <summary>
    /// Event arguments raised when an HTTP 429 rate-limit response is detected during scraping.
    /// </summary>
    public class ScrapeRateLimitEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ScrapeDefinition"/> that encountered the HTTP 429 response.
        /// </summary>
        public ScrapeDefinition Definition { get; }

        /// <summary>
        /// Gets the 1-based page number on which rate limiting occurred.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the raw <c>Retry-After</c> header value if provided by the server.
        /// </summary>
        public string? RetryAfterHeader { get; }

        /// <summary>
        /// Gets the UTC timestamp when the scraper will retry the request.
        /// </summary>
        public DateTimeOffset RetryAtUtc { get; }

        /// <summary>
        /// Gets the calculated duration to wait before retrying.
        /// </summary>
        public TimeSpan WaitDuration { get; }

        /// <summary>
        /// Gets whether the <c>Retry-After</c> value was parsed as an exact date/timestamp rather than a relative delay in seconds.
        /// </summary>
        public bool IsDateHeader { get; }

        /// <summary>
        /// Gets the updated <see cref="ScrapeDefinition.PageDelayMs"/> applied for subsequent pages.
        /// </summary>
        public int NewPageDelayMs { get; }

        /// <summary>
        /// Gets whether <see cref="ScrapeDefinition.PageDelayMs"/> was updated for subsequent pages.
        /// This is <c>false</c> when the <c>Retry-After</c> header specified an exact date/timestamp.
        /// </summary>
        public bool UpdatedPageDelay => !IsDateHeader;

        /// <summary>
        /// Determines whether the updated page delay should be applied to <paramref name="selectedScrape"/>.
        /// </summary>
        public bool ShouldUpdatePageDelayFor(ScrapeDefinition? selectedScrape)
        {
            return UpdatedPageDelay && ReferenceEquals(selectedScrape, Definition);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapeRateLimitEventArgs"/> class.
        /// </summary>
        public ScrapeRateLimitEventArgs(
            ScrapeDefinition definition,
            int pageNumber,
            string? retryAfterHeader,
            DateTimeOffset retryAtUtc,
            TimeSpan waitDuration,
            bool isDateHeader,
            int newPageDelayMs)
        {
            Definition = definition;
            PageNumber = pageNumber;
            RetryAfterHeader = retryAfterHeader;
            RetryAtUtc = retryAtUtc;
            WaitDuration = waitDuration;
            IsDateHeader = isDateHeader;
            NewPageDelayMs = newPageDelayMs;
        }

        /// <summary>
        /// Returns a detailed log message describing the HTTP 429 rate-limit wait and retry behavior.
        /// </summary>
        public string GetLogMessage()
        {
            if (IsDateHeader)
            {
                var localRetryTime = RetryAtUtc.ToLocalTime();
                return $"HTTP 429 on page {PageNumber}! Retry-After: '{RetryAfterHeader}'. Waiting until {localRetryTime:yyyy-MM-dd HH:mm:ss} ({WaitDuration.TotalSeconds:0.#}s) before retrying...";
            }

            string headerInfo = !string.IsNullOrWhiteSpace(RetryAfterHeader)
                ? $"Retry-After: '{RetryAfterHeader}'"
                : "No Retry-After header (using fallback)";
            return $"HTTP 429 on page {PageNumber}! {headerInfo}. Waiting {WaitDuration.TotalSeconds:0.#}s and setting Page Delay to {NewPageDelayMs} ms for subsequent pages...";
        }

        /// <summary>
        /// Returns a concise status bar message describing the HTTP 429 rate-limit wait state.
        /// </summary>
        public string GetStatusMessage()
        {
            if (IsDateHeader)
            {
                var localRetryTime = RetryAtUtc.ToLocalTime();
                return $"HTTP 429 Rate Limited - Retry at {localRetryTime:HH:mm:ss} ({WaitDuration.TotalSeconds:0.#}s)...";
            }

            return $"HTTP 429 Rate Limited - Waiting {WaitDuration.TotalSeconds:0.#}s (Page Delay updated to {NewPageDelayMs} ms)...";
        }
    }

    /// <summary>
    /// Event arguments raised when an <see cref="AutomationSequence"/> starts or completes.
    /// </summary>
    public class AutomationSequenceEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="AutomationSequence"/> associated with the event.
        /// </summary>
        public AutomationSequence Sequence { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationSequenceEventArgs"/> class.
        /// </summary>
        public AutomationSequenceEventArgs(AutomationSequence sequence)
        {
            Sequence = sequence;
        }

        /// <summary>
        /// Returns a formatted log message indicating that the sequence has started.
        /// </summary>
        public string GetStartedLogMessage() => $"--- Starting List: {Sequence.Name} ---";

        /// <summary>
        /// Returns a formatted log message indicating that the sequence has completed.
        /// </summary>
        public string GetCompletedLogMessage() => $"--- Completed List: {Sequence.Name} ---";

        /// <summary>
        /// Returns a formatted log message for this sequence event.
        /// </summary>
        public string GetLogMessage() => GetStartedLogMessage();

        /// <summary>
        /// Returns a concise status message for this sequence event.
        /// </summary>
        public string GetStatusMessage() => $"Running sequence '{Sequence.Name}'...";
    }

    /// <summary>
    /// Event arguments raised when an <see cref="AutomationStep"/> starts, completes, or fails.
    /// </summary>
    public class AutomationStepEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the parent <see cref="AutomationSequence"/> if the step is running as part of a sequence.
        /// </summary>
        public AutomationSequence? Sequence { get; }

        /// <summary>
        /// Gets the <see cref="AutomationStep"/> being executed.
        /// </summary>
        public AutomationStep Step { get; }

        /// <summary>
        /// Gets the <see cref="CommandResult"/> returned by the step when completed.
        /// </summary>
        public CommandResult? Result { get; }

        /// <summary>
        /// Gets the exception thrown if the step failed.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationStepEventArgs"/> class.
        /// </summary>
        public AutomationStepEventArgs(AutomationSequence? sequence, AutomationStep step, CommandResult? result = null, Exception? error = null)
        {
            Sequence = sequence;
            Step = step;
            Result = result;
            Error = error;
        }

        /// <summary>
        /// Returns a formatted log message indicating that the step has started executing.
        /// </summary>
        public string GetStartedLogMessage() => $"Executing: {Step.Name}";

        /// <summary>
        /// Returns a formatted log message for the step's completion result.
        /// </summary>
        public string GetCompletedLogMessage() =>
            Result != null ? $"{Result.Status} | {Result.Output}" : $"Completed: {Step.Name}";

        /// <summary>
        /// Returns a formatted log message describing the step failure.
        /// </summary>
        public string GetFailedLogMessage() => $"Step '{Step.Name}' failed: {Error?.Message}";

        /// <summary>
        /// Returns a formatted log message based on the current state of the step event.
        /// </summary>
        public string GetLogMessage()
        {
            if (Error != null) return GetFailedLogMessage();
            if (Result != null) return GetCompletedLogMessage();
            return GetStartedLogMessage();
        }

        /// <summary>
        /// Returns a concise status bar message for this step event.
        /// </summary>
        public string GetStatusMessage()
        {
            if (Error != null) return $"Step '{Step.Name}' failed: {Error.Message}";
            if (Result != null) return $"Completed step '{Step.Name}'.";
            return $"Executing step '{Step.Name}'...";
        }
    }

    /// <summary>
    /// Event arguments raised when a <see cref="ScrapeDefinition"/> starts, completes, or fails.
    /// </summary>
    public class ScrapeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ScrapeDefinition"/> associated with the event.
        /// </summary>
        public ScrapeDefinition Definition { get; }

        /// <summary>
        /// Gets the <see cref="ScrapeResult"/> produced by the scrape operation.
        /// </summary>
        public ScrapeResult? Result { get; }

        /// <summary>
        /// Gets the exception thrown if the scrape operation failed.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapeEventArgs"/> class.
        /// </summary>
        public ScrapeEventArgs(ScrapeDefinition definition, ScrapeResult? result = null, Exception? error = null)
        {
            Definition = definition;
            Result = result;
            Error = error;
        }

        /// <summary>
        /// Returns a formatted log message indicating that the scrape operation has started.
        /// </summary>
        public string GetStartedLogMessage() => $"--- Starting Scrape: {Definition.Name} ---";

        /// <summary>
        /// Returns a formatted completion summary message when <see cref="Result"/> is available.
        /// </summary>
        public string GetCompletedLogMessage()
        {
            if (Result == null)
                return $"Completed '{Definition.Name}'.";
            return $"Completed '{Definition.Name}': {Result.RecordCount} total record(s) across {Result.PagesScraped} page(s).";
        }

        /// <summary>
        /// Returns a formatted log message describing the scrape failure.
        /// </summary>
        public string GetFailedLogMessage() => $"'{Definition.Name}' failed: {Error?.Message}";
    }

    /// <summary>
    /// Event arguments raised when a single page of a <see cref="ScrapeDefinition"/> finishes extracting records.
    /// </summary>
    public class ScrapePageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ScrapeDefinition"/> being executed.
        /// </summary>
        public ScrapeDefinition Definition { get; }

        /// <summary>
        /// Gets the 1-based page number that completed extraction.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the records extracted from this page.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.Dictionary<string, string?>> PageRecords { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapePageEventArgs"/> class.
        /// </summary>
        public ScrapePageEventArgs(
            ScrapeDefinition definition,
            int pageNumber,
            System.Collections.Generic.IReadOnlyList<System.Collections.Generic.Dictionary<string, string?>> pageRecords)
        {
            Definition = definition;
            PageNumber = pageNumber;
            PageRecords = pageRecords;
        }

        /// <summary>
        /// Returns a formatted log message summarizing the number of records extracted on this page.
        /// </summary>
        public string GetLogMessage() => $"Page {PageNumber} extracted {PageRecords.Count} record(s).";

        /// <summary>
        /// Returns a concise status message summarizing the number of records extracted on this page.
        /// </summary>
        public string GetStatusMessage() => $"Page {PageNumber}: {PageRecords.Count} record(s) extracted.";
    }

    /// <summary>
    /// Event arguments raised when a reverse-engineering site dump starts, completes, or fails.
    /// </summary>
    public class SiteDumpEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="SiteDumpType"/> of the dump operation.
        /// </summary>
        public SiteDumpType DumpType { get; }

        /// <summary>
        /// Gets the <see cref="SiteDumpResult"/> containing the extracted site resources.
        /// </summary>
        public SiteDumpResult? Result { get; }

        /// <summary>
        /// Gets the exception thrown if the site dump operation failed.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteDumpEventArgs"/> class.
        /// </summary>
        public SiteDumpEventArgs(SiteDumpType dumpType, SiteDumpResult? result = null, Exception? error = null)
        {
            DumpType = dumpType;
            Result = result;
            Error = error;
        }

        /// <summary>
        /// Returns a formatted log message indicating that the site dump operation has started.
        /// </summary>
        public string GetStartedLogMessage() => $"--- Starting Site Dump ({DumpType}) ---";

        /// <summary>
        /// Returns a formatted completion summary message when <see cref="Result"/> is available.
        /// </summary>
        public string GetCompletedLogMessage()
        {
            if (Result == null)
                return $"Dump ({DumpType}) completed.";
            return $"Dump ({DumpType}) completed: {Result.HtmlResources.Count} HTML, {Result.CssResources.Count} CSS, {Result.JavaScriptResources.Count} JS resource(s) ({Result.GetFormattedTotalSize()}).";
        }

        /// <summary>
        /// Returns a formatted log message describing the site dump failure.
        /// </summary>
        public string GetFailedLogMessage() => $"Dump ({DumpType}) failed: {Error?.Message}";
    }

    /// <summary>
    /// Event arguments raised when the BAF Chrome Extension reports an updated list of open browser tabs.
    /// </summary>
    public class ExtensionTabsEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the read-only list of open browser tabs reported by the Chrome Extension.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<BrowserTabInfo> Tabs { get; }

        /// <summary>
        /// Gets the ID of the currently active attached tab, if any.
        /// </summary>
        public int? ActiveAttachedTabId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionTabsEventArgs"/> class.
        /// </summary>
        public ExtensionTabsEventArgs(
            System.Collections.Generic.IReadOnlyList<BrowserTabInfo> tabs,
            int? activeAttachedTabId = null)
        {
            Tabs = tabs;
            ActiveAttachedTabId = activeAttachedTabId;
        }
    }

    /// <summary>
    /// Classifies the severity or category of an <see cref="AutomationLogEventArgs"/> message for UI color-coding.
    /// </summary>
    public enum AutomationLogLevel
    {
        /// <summary>
        /// Standard informational or command response output.
        /// </summary>
        Default,

        /// <summary>
        /// System lifecycle notification.
        /// </summary>
        System,

        /// <summary>
        /// Server stop or caution notification.
        /// </summary>
        ServerStopped,

        /// <summary>
        /// Chrome Extension synchronization or tab operation message.
        /// </summary>
        Extension,

        /// <summary>
        /// Browser client or extension connection established.
        /// </summary>
        ClientConnected,

        /// <summary>
        /// Non-fatal warning or rate-limit notification.
        /// </summary>
        Warning,

        /// <summary>
        /// Execution or transport error.
        /// </summary>
        Error,

        /// <summary>
        /// Outgoing IPC command transmission.
        /// </summary>
        CommandTx,

        /// <summary>
        /// Incoming IPC command result.
        /// </summary>
        CommandRx,

        /// <summary>
        /// Streaming partial command data.
        /// </summary>
        Stream,

        /// <summary>
        /// Browser page load or navigation event.
        /// </summary>
        Navigation,

        /// <summary>
        /// Automation sequence lifecycle event.
        /// </summary>
        Automation,

        /// <summary>
        /// Individual automation step execution event.
        /// </summary>
        AutomationStep,

        /// <summary>
        /// Data scrape started or field/container configured.
        /// </summary>
        ScrapeStart,

        /// <summary>
        /// Data scrape page extraction completed.
        /// </summary>
        ScrapePage,

        /// <summary>
        /// Pagination delay wait notification.
        /// </summary>
        ScrapeDelay,

        /// <summary>
        /// Operation or scrape completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Reverse-engineering site dump started.
        /// </summary>
        ReverseEngineering
    }

    /// <summary>
    /// Provides default dark-theme RGB color mappings for <see cref="AutomationLogLevel"/> values.
    /// </summary>
    public static class AutomationLogLevelExtensions
    {
        /// <summary>
        /// Returns the default dark-theme RGB color tuple <c>(R, G, B)</c> for the specified <see cref="AutomationLogLevel"/>.
        /// </summary>
        public static (byte R, byte G, byte B) GetDefaultRgb(this AutomationLogLevel level)
        {
            return level switch
            {
                AutomationLogLevel.System => (0, 255, 255),
                AutomationLogLevel.ServerStopped => (255, 255, 0),
                AutomationLogLevel.Extension => (0, 255, 127),
                AutomationLogLevel.ClientConnected => (50, 205, 50),
                AutomationLogLevel.Warning => (255, 165, 0),
                AutomationLogLevel.Error => (255, 0, 0),
                AutomationLogLevel.CommandTx => (0, 191, 255),
                AutomationLogLevel.CommandRx => (255, 255, 255),
                AutomationLogLevel.Stream => (147, 112, 219),
                AutomationLogLevel.Navigation => (0, 255, 255),
                AutomationLogLevel.Automation => (255, 0, 255),
                AutomationLogLevel.AutomationStep => (221, 160, 221),
                AutomationLogLevel.ScrapeStart => (0, 255, 127),
                AutomationLogLevel.ScrapePage => (144, 238, 144),
                AutomationLogLevel.ScrapeDelay => (240, 230, 140),
                AutomationLogLevel.Success => (0, 255, 0),
                AutomationLogLevel.ReverseEngineering => (0, 191, 255),
                _ => (211, 211, 211)
            };
        }
    }

    /// <summary>
    /// Event arguments representing a structured log entry emitted by <see cref="Automation.AutomationStudioSession"/>.
    /// </summary>
    public class AutomationLogEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the local timestamp when the log entry was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the short bracketed category tag (e.g., <c>"SYS"</c>, <c>"EXT"</c>, <c>"SCRAPE"</c>), or empty if already prefixed.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the log message body.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the semantic <see cref="AutomationLogLevel"/> used for UI color-coding.
        /// </summary>
        public AutomationLogLevel Level { get; }

        /// <summary>
        /// Gets the default dark-theme RGB color tuple for this log entry.
        /// </summary>
        public (byte R, byte G, byte B) ColorRgb => Level.GetDefaultRgb();

        /// <summary>
        /// Gets the complete formatted log text without a trailing newline (e.g., <c>"[12:34:56.789] [SYS] Server stopped."</c>).
        /// </summary>
        public string FormattedMessage =>
            string.IsNullOrWhiteSpace(Category)
                ? $"[{Timestamp:HH:mm:ss.fff}] {Message}"
                : $"[{Timestamp:HH:mm:ss.fff}] [{Category}] {Message}";

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationLogEventArgs"/> class.
        /// </summary>
        public AutomationLogEventArgs(string category, string message, AutomationLogLevel level = AutomationLogLevel.Default, DateTime? timestamp = null)
        {
            Timestamp = timestamp ?? DateTime.Now;
            Category = category ?? string.Empty;
            Message = message ?? string.Empty;
            Level = level;
        }
    }
}