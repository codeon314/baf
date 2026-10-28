using System;
using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents an open browser tab reported by the BAF Chrome Extension.
    /// </summary>
    public class BrowserTabInfo
    {
        /// <summary>
        /// Gets or sets the unique Chrome tab identifier.
        /// </summary>
        [JsonPropertyName("tabId")]
        public int TabId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the browser window containing this tab.
        /// </summary>
        [JsonPropertyName("windowId")]
        public int WindowId { get; set; }

        /// <summary>
        /// Gets or sets the zero-based position of the tab within its browser window.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the title of the tab's active document.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL currently loaded in the tab.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the tab is the currently focused tab in its browser window.
        /// </summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the loading status of the tab (e.g., <c>loading</c> or <c>complete</c>).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "complete";

        /// <summary>
        /// Gets or sets whether the URL allows script injection (false for restricted <c>chrome://</c> or webstore pages).
        /// </summary>
        [JsonPropertyName("canInject")]
        public bool CanInject { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the BAF automation bridge is currently injected and active in this tab.
        /// </summary>
        [JsonPropertyName("isInjected")]
        public bool IsInjected { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Server.BrowserClient"/> instance ID associated with this tab (defaults to <c>tab_{TabId}</c>).
        /// </summary>
        [JsonPropertyName("instanceId")]
        public string InstanceId
        {
            get => string.IsNullOrWhiteSpace(_instanceId) ? $"tab_{TabId}" : _instanceId;
            set => _instanceId = value ?? string.Empty;
        }
        private string _instanceId = string.Empty;

        /// <summary>
        /// Determines whether this tab is already the active connected <see cref="Server.BrowserClient"/> and does not require re-injection.
        /// </summary>
        public bool IsAlreadyActiveClient(Server.BrowserClient? currentClient, bool exclusiveMode)
        {
            return IsInjected && currentClient != null && currentClient.TabId == TabId && !exclusiveMode;
        }

        /// <summary>
        /// Determines whether this browser tab matches the specified search filter against its title, URL, or tab ID.
        /// </summary>
        public bool MatchesFilter(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            string trimmed = filter.Trim();
            return Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                   Url.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                   TabId.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a formatted summary of the tab's hook state, tab ID, title, and host/path.
        /// </summary>
        public override string ToString()
        {
            string statusTag = IsInjected
                ? "[HOOKED] "
                : (!CanInject ? "[RESTRICTED] " : (Active ? "* " : string.Empty));

            string displayTitle = !string.IsNullOrWhiteSpace(Title) ? Title.Trim() : "(Untitled Tab)";
            if (displayTitle.Length > 42)
            {
                displayTitle = displayTitle.Substring(0, 39) + "...";
            }

            string hostOrUrl = Url;
            if (Uri.TryCreate(Url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                hostOrUrl = uri.Host + (uri.AbsolutePath != "/" ? uri.AbsolutePath : string.Empty);
                if (hostOrUrl.Length > 36)
                {
                    hostOrUrl = hostOrUrl.Substring(0, 33) + "...";
                }
            }

            return $"{statusTag}[#{TabId}] {displayTitle} ({hostOrUrl})";
        }
    }
}