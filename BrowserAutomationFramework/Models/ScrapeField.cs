using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Specifies the DOM property or attribute to extract for a <see cref="ScrapeField"/>.
    /// </summary>
    public enum ScrapeExtractionType
    {
        /// <summary>
        /// Extracts the rendered <c>innerText</c> of the element.
        /// </summary>
        InnerText,

        /// <summary>
        /// Extracts the raw <c>textContent</c> of the element.
        /// </summary>
        TextContent,

        /// <summary>
        /// Extracts the <c>innerHTML</c> markup of the element.
        /// </summary>
        InnerHtml,

        /// <summary>
        /// Extracts the <c>outerHTML</c> markup of the element.
        /// </summary>
        OuterHtml,

        /// <summary>
        /// Extracts the current <c>value</c> of an input, select, or textarea element.
        /// </summary>
        Value,

        /// <summary>
        /// Extracts the value of the HTML attribute named in <see cref="ScrapeField.AttributeName"/>.
        /// </summary>
        Attribute,

        /// <summary>
        /// Extracts the resolved <c>href</c> URL from an anchor element (or its closest/child link).
        /// </summary>
        Href,

        /// <summary>
        /// Extracts the resolved <c>src</c> URL from a media or iframe element.
        /// </summary>
        Src,

        /// <summary>
        /// Extracts a <c>data-*</c> attribute specified by <see cref="ScrapeField.AttributeName"/>.
        /// </summary>
        DataAttribute,

        /// <summary>
        /// Extracts a computed CSS style property specified by <see cref="ScrapeField.AttributeName"/>.
        /// </summary>
        ComputedStyle,

        /// <summary>
        /// Returns <c>"true"</c> if at least one matching element exists, or <c>"false"</c> otherwise.
        /// </summary>
        Exists,

        /// <summary>
        /// Returns the number of elements matching the field selector.
        /// </summary>
        Count
    }

    /// <summary>
    /// Defines a single data field/column to extract within a <see cref="ScrapeDefinition"/>.
    /// </summary>
    public class ScrapeField
    {
        /// <summary>
        /// Gets or sets the column/property name for this extracted value.
        /// </summary>
        [Category("1. General")]
        [Description("The column/property name for this extracted value.")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "New Field";

        /// <summary>
        /// Gets or sets the CSS selector for the target element(s). Can be a full path or relative to <see cref="ScrapeDefinition.ContainerSelector"/>.
        /// </summary>
        [Category("1. General")]
        [Description("CSS selector for the target element(s). Can be a full path or relative to the Scrape Definition's ContainerSelector.")]
        [JsonPropertyName("selector")]
        public string Selector { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets what data to extract from the matched DOM element.
        /// </summary>
        [Category("2. Extraction")]
        [Description("Specifies what data to extract from the matched DOM element.")]
        [JsonPropertyName("extractionType")]
        public ScrapeExtractionType ExtractionType { get; set; } = ScrapeExtractionType.InnerText;

        /// <summary>
        /// Gets or sets the name of the attribute, <c>data-*</c> key, or CSS style property when <see cref="ExtractionType"/> is
        /// <see cref="ScrapeExtractionType.Attribute"/>, <see cref="ScrapeExtractionType.DataAttribute"/>, or <see cref="ScrapeExtractionType.ComputedStyle"/>.
        /// </summary>
        [Category("2. Extraction")]
        [Description("Name of the attribute, data-* key, or CSS style property when ExtractionType is Attribute, DataAttribute, or ComputedStyle.")]
        [JsonPropertyName("attributeName")]
        public string AttributeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to match all similar elements on the page (when no container selector is used) or join multiple matches inside a container.
        /// </summary>
        [Category("2. Extraction")]
        [Description("When true, matches all similar elements on the page (if no ContainerSelector is used) or joins multiple matches within a container.")]
        [JsonPropertyName("matchAllElements")]
        public bool MatchAllElements { get; set; } = false;

        /// <summary>
        /// Gets or sets the fallback value returned when the element is not found or the extracted string is empty.
        /// </summary>
        [Category("3. Formatting & Filtering")]
        [Description("Fallback value if the element is not found or the extracted value is empty.")]
        [JsonPropertyName("defaultValue")]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional regular expression pattern used to filter or transform the extracted string.
        /// </summary>
        [Category("3. Formatting & Filtering")]
        [Description("Optional Regular Expression pattern to extract or filter the scraped string.")]
        [JsonPropertyName("regexPattern")]
        public string RegexPattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the regex capture group index to return when <see cref="RegexPattern"/> is specified (0 for full match).
        /// </summary>
        [Category("3. Formatting & Filtering")]
        [Description("Regex capture group index to return when RegexPattern is specified (0 for full match).")]
        [JsonPropertyName("regexGroup")]
        public int RegexGroup { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether leading and trailing whitespace should be trimmed from the extracted value.
        /// </summary>
        [Category("3. Formatting & Filtering")]
        [Description("Whether to trim leading and trailing whitespace from the extracted value.")]
        [JsonPropertyName("trim")]
        public bool Trim { get; set; } = true;

        /// <summary>
        /// Generalizes this field's <see cref="Selector"/> so it matches all similar repeating elements on the page and enables <see cref="MatchAllElements"/>.
        /// </summary>
        public string GeneralizeSelector(DomNode? rootDom = null)
        {
            if (!string.IsNullOrWhiteSpace(Selector))
            {
                Selector = BrowserAutomationFramework.Scraping.SelectorHelper.GeneralizeRepeatingFieldSelector(Selector, rootDom);
                MatchAllElements = true;
            }
            return Selector;
        }

        /// <summary>
        /// Creates a shallow copy of this <see cref="ScrapeField"/>.
        /// </summary>
        public ScrapeField Clone()
        {
            return new ScrapeField
            {
                Name = Name,
                Selector = Selector,
                ExtractionType = ExtractionType,
                AttributeName = AttributeName,
                MatchAllElements = MatchAllElements,
                DefaultValue = DefaultValue,
                RegexPattern = RegexPattern,
                RegexGroup = RegexGroup,
                Trim = Trim
            };
        }

        /// <summary>
        /// Returns a formatted summary of the field name and extraction settings.
        /// </summary>
        public override string ToString()
        {
            string extra = (ExtractionType == ScrapeExtractionType.Attribute ||
                            ExtractionType == ScrapeExtractionType.DataAttribute ||
                            ExtractionType == ScrapeExtractionType.ComputedStyle) &&
                           !string.IsNullOrWhiteSpace(AttributeName)
                ? $"{ExtractionType}:{AttributeName}"
                : ExtractionType.ToString();

            if (MatchAllElements)
            {
                extra += ", All";
            }

            return $"{Name} [{extra}]";
        }
    }
}