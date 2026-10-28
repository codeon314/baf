using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents an asynchronous lifecycle or DOM event sent from a browser client to <see cref="Server.AutomationServer"/>.
    /// </summary>
    public class EventMessage
    {
        /// <summary>
        /// Event identifier (e.g., <c>PAGE_LOADED</c>, <c>DOM_CHANGED</c>, <c>ELEMENT_CLICKED</c>, <c>ELEMENT_REMOVED</c>).
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Optional event payload data.
        /// </summary>
        [JsonPropertyName("payload")]
        public object? Payload { get; set; }
    }
}