using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Specifies the built-in or custom action type performed by an <see cref="AutomationStep"/>.
    /// </summary>
    public enum AutomationActionType
    {
        /// <summary>
        /// Simulates a trusted React or native click on the target element.
        /// </summary>
        Click,

        /// <summary>
        /// Sets the value of an input or textarea element, bypassing React's native value tracker.
        /// </summary>
        SetValue,

        /// <summary>
        /// Navigates the browser tab to a target URL and waits for the page transition to complete.
        /// </summary>
        Navigate,

        /// <summary>
        /// Evaluates custom JavaScript code in the context of the page.
        /// </summary>
        EvaluateScript,

        /// <summary>
        /// Executes a registered <see cref="ScrapeDefinition"/> or an ad-hoc single-field scrape.
        /// </summary>
        Scrape,

        /// <summary>
        /// Executes a custom command handler or sends a raw command type to the browser bridge.
        /// </summary>
        Custom,

        /// <summary>
        /// Executes a reverse-engineering site dump (<see cref="SiteDumpType"/>) and optionally saves the output to disk.
        /// </summary>
        DumpSite
    }

    /// <summary>
    /// Represents a single automation step within an <see cref="AutomationSequence"/>.
    /// </summary>
    public class AutomationStep
    {
        /// <summary>
        /// Gets or sets the display name for this automation step.
        /// </summary>
        [Category("1. General")]
        [Description("Display name for this automation step.")]
        public string Name { get; set; } = "New Step";

        /// <summary>
        /// Gets or sets the type of browser action to execute.
        /// </summary>
        [Category("1. General")]
        [Description("The type of browser action to execute.")]
        public AutomationActionType ActionType { get; set; }

        /// <summary>
        /// Gets or sets the CSS selector of the target DOM element (for Click, SetValue, or ad-hoc Scrape steps).
        /// </summary>
        [Category("2. Target & Payload")]
        [Description("CSS selector of the target DOM element (for Click, SetValue, or ad-hoc Scrape steps).")]
        public string Selector { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input text, target URL, JavaScript code, Scrape list name, or dump output folder path.
        /// </summary>
        [Category("2. Target & Payload")]
        [Description("Input text, target URL, JavaScript code, Scrape list name, or dump output folder path.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom command identifier, <see cref="ScrapeExtractionType"/>, or <see cref="SiteDumpType"/> depending on <see cref="ActionType"/>.
        /// </summary>
        [Category("2. Target & Payload")]
        [Description("Custom command identifier, ScrapeExtractionType, or SiteDumpType depending on ActionType.")]
        public string CustomCommandType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to replace existing input text instead of appending when <see cref="ActionType"/> is <see cref="AutomationActionType.SetValue"/>.
        /// </summary>
        [Category("3. Behavior")]
        [Description("When true on SetValue, replaces existing input text instead of appending.")]
        public bool Overwrite { get; set; } = true;

        /// <summary>
        /// Gets or sets the delay in milliseconds to wait after this step completes.
        /// </summary>
        [Category("3. Behavior")]
        [Description("Delay in milliseconds to wait after this step completes.")]
        public int DelayAfterMs { get; set; } = 500;

        /// <summary>
        /// Returns a formatted string representation of this step and its action type.
        /// </summary>
        public override string ToString() => $"{Name} ({ActionType})";
    }
}