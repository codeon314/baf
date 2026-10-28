using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents the execution result or streaming payload returned by a browser client for a <see cref="CommandMessage"/>.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// The correlation ID matching <see cref="CommandMessage.Id"/>.
        /// </summary>
        [JsonPropertyName("task_id")]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// Execution status (<c>completed</c>, <c>streaming</c>, or <c>error</c>).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        /// <summary>
        /// Serialized command output or error message returned by the browser script.
        /// </summary>
        [JsonPropertyName("output")]
        public string? Output { get; set; }
    }
}