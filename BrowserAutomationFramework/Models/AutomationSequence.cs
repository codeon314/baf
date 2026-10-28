using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents an ordered sequence of <see cref="AutomationStep"/> actions that can be executed against a <see cref="Server.BrowserClient"/>.
    /// </summary>
    public class AutomationSequence
    {
        /// <summary>
        /// Gets or sets the unique identifier for this automation sequence.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the display name of this automation sequence.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "New Sequence";

        /// <summary>
        /// Gets or sets whether this sequence is enabled when running all active sequences in a project.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the ordered list of <see cref="AutomationStep"/> actions in this sequence.
        /// </summary>
        [JsonPropertyName("steps")]
        public List<AutomationStep> Steps { get; set; } = new();

        /// <summary>
        /// Creates a new <see cref="AutomationStep"/> and appends it to <see cref="Steps"/>.
        /// </summary>
        /// <param name="name">Display name for the step.</param>
        /// <param name="actionType">The action type to execute.</param>
        /// <param name="selector">Target CSS selector.</param>
        /// <param name="value">Step value or payload.</param>
        /// <param name="overwrite">Whether to overwrite existing text on input steps.</param>
        /// <returns>The newly created <see cref="AutomationStep"/>.</returns>
        public AutomationStep AddStep(
            string name = "New Step",
            AutomationActionType actionType = AutomationActionType.Click,
            string selector = "",
            string value = "",
            bool overwrite = true)
        {
            var step = new AutomationStep
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Step" : name,
                ActionType = actionType,
                Selector = selector ?? string.Empty,
                Value = value ?? string.Empty,
                Overwrite = overwrite
            };
            Steps.Add(step);
            return step;
        }

        /// <summary>
        /// Creates an <see cref="AutomationStep"/> targeting <paramref name="domNode"/> and appends it to <see cref="Steps"/>.
        /// </summary>
        public AutomationStep AddStepFromDomNode(
            DomNode domNode,
            AutomationActionType actionType,
            string value = "",
            bool overwrite = true)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            var step = domNode.CreateStep(actionType, value, overwrite);
            Steps.Add(step);
            return step;
        }

        /// <summary>
        /// Removes the specified <see cref="AutomationStep"/> from <see cref="Steps"/>.
        /// </summary>
        public bool RemoveStep(AutomationStep? step)
        {
            if (step == null) return false;
            return Steps.Remove(step);
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position earlier in the sequence.
        /// </summary>
        public bool MoveStepUp(int index)
        {
            if (index <= 0 || index >= Steps.Count) return false;
            var step = Steps[index];
            Steps.RemoveAt(index);
            Steps.Insert(index - 1, step);
            return true;
        }

        /// <summary>
        /// Moves the step at <paramref name="index"/> one position later in the sequence.
        /// </summary>
        public bool MoveStepDown(int index)
        {
            if (index < 0 || index >= Steps.Count - 1) return false;
            var step = Steps[index];
            Steps.RemoveAt(index);
            Steps.Insert(index + 1, step);
            return true;
        }

        /// <summary>
        /// Returns the display name of this sequence.
        /// </summary>
        public override string ToString() => Name;
    }
}