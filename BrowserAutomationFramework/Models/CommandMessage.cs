using System;
using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents an IPC command message sent from <see cref="Server.AutomationServer"/> to a connected browser client.
    /// </summary>
    public class CommandMessage
    {
        /// <summary>
        /// Unique correlation ID for matching the command with its <see cref="CommandResult"/>.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Action identifier (e.g., <c>EVAL</c>, <c>REACT_CLICK</c>, <c>SET_VALUE</c>, <c>GET_DOM</c>, <c>SCRAPE_DATA</c>).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Optional JSON-serializable command payload.
        /// </summary>
        [JsonPropertyName("payload")]
        public object? Payload { get; set; }
    }
}