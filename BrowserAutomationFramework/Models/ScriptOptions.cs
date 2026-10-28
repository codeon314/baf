using System.Collections.Generic;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Configuration options for generating a Tampermonkey userscript or Chrome Extension bridge script via <see cref="Generators.ScriptGenerator"/>.
    /// </summary>
    public class ScriptOptions
    {
        /// <summary>
        /// Gets or sets the <c>@name</c> metadata header for the generated userscript.
        /// </summary>
        public string ScriptName { get; set; } = "Generated Automation Bridge";

        /// <summary>
        /// Gets or sets the <c>@namespace</c> metadata header for the generated userscript.
        /// </summary>
        public string Namespace { get; set; } = "automation.bridge";

        /// <summary>
        /// Gets or sets the <c>@version</c> metadata header for the generated userscript.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the list of <c>@match</c> URL patterns for the generated userscript.
        /// </summary>
        public List<string> MatchUrls { get; set; } = new() { "*://*/*" };

        /// <summary>
        /// Gets or sets the TCP port of the local <see cref="Server.AutomationServer"/>.
        /// </summary>
        public int Port { get; set; } = 8081;

        /// <summary>
        /// Gets or sets the preferred <see cref="IpcMode"/> transport protocol.
        /// </summary>
        public IpcMode Mode { get; set; } = IpcMode.HttpPolling;

        /// <summary>
        /// Gets or sets the polling interval in milliseconds when operating in <see cref="IpcMode.HttpPolling"/> mode.
        /// </summary>
        public int PollingIntervalMs { get; set; } = 1000;
        
        /// <summary>
        /// Allows developers to append custom JS functions and register custom actions.
        /// </summary>
        public string CustomScript { get; set; } = string.Empty;

        /// <summary>
        /// Optional fixed instance ID (e.g. "tab_123" when injected by the Chrome Extension).
        /// When empty, a random "bot_*" instance ID is generated for Tampermonkey compatibility.
        /// </summary>
        public string? ForcedInstanceId { get; set; }
    }
}