using System.Text.Json.Serialization;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Configuration options controlling which page resources are extracted during a reverse-engineering site dump.
    /// </summary>
    public class SiteDumpOptions
    {
        /// <summary>
        /// Gets or sets whether to dump document and iframe HTML resources.
        /// </summary>
        [JsonPropertyName("includeHtml")]
        public bool IncludeHtml { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to dump CSS stylesheet resources.
        /// </summary>
        [JsonPropertyName("includeCss")]
        public bool IncludeCss { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to dump JavaScript resources.
        /// </summary>
        [JsonPropertyName("includeJavaScript")]
        public bool IncludeJavaScript { get; set; } = true;

        /// <summary>
        /// Gets or sets whether inline <c>&lt;style&gt;</c> tags are included when dumping CSS.
        /// </summary>
        [JsonPropertyName("includeInlineCss")]
        public bool IncludeInlineCss { get; set; } = true;

        /// <summary>
        /// Gets or sets whether external <c>&lt;link rel="stylesheet"&gt;</c> stylesheets are fetched when dumping CSS.
        /// </summary>
        [JsonPropertyName("includeExternalCss")]
        public bool IncludeExternalCss { get; set; } = true;

        /// <summary>
        /// Gets or sets whether CSSOM constructed and adopted stylesheets are serialized when dumping CSS.
        /// </summary>
        [JsonPropertyName("includeConstructedCss")]
        public bool IncludeConstructedCss { get; set; } = true;

        /// <summary>
        /// Gets or sets whether inline <c>&lt;script&gt;</c> blocks are included when dumping JavaScript.
        /// </summary>
        [JsonPropertyName("includeInlineScripts")]
        public bool IncludeInlineScripts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether external <c>&lt;script src="..."&gt;</c> files are fetched when dumping JavaScript.
        /// </summary>
        [JsonPropertyName("includeExternalScripts")]
        public bool IncludeExternalScripts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether dynamically loaded scripts discovered via the Performance Resource Timing API are fetched.
        /// </summary>
        [JsonPropertyName("includePerformanceResourceScripts")]
        public bool IncludePerformanceResourceScripts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether accessible <c>&lt;iframe&gt;</c> documents are included when dumping HTML.
        /// </summary>
        [JsonPropertyName("includeIframes")]
        public bool IncludeIframes { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout in milliseconds for fetching individual external CSS/JS assets.
        /// </summary>
        [JsonPropertyName("fetchTimeoutMs")]
        public int FetchTimeoutMs { get; set; } = 15000;

        /// <summary>
        /// Creates a <see cref="SiteDumpOptions"/> instance configured from high-level inclusion flags.
        /// </summary>
        public static SiteDumpOptions Create(
            bool includeExternal = true,
            bool includeInline = true,
            bool includeDynamicScripts = true,
            bool includeIframes = true,
            int fetchTimeoutMs = 15000)
        {
            return new SiteDumpOptions
            {
                IncludeExternalCss = includeExternal,
                IncludeExternalScripts = includeExternal,
                IncludeInlineCss = includeInline,
                IncludeConstructedCss = includeInline,
                IncludeInlineScripts = includeInline,
                IncludePerformanceResourceScripts = includeDynamicScripts,
                IncludeIframes = includeIframes,
                FetchTimeoutMs = fetchTimeoutMs
            };
        }

        /// <summary>
        /// Creates a shallow copy of this <see cref="SiteDumpOptions"/> instance.
        /// </summary>
        public SiteDumpOptions Clone()
        {
            return new SiteDumpOptions
            {
                IncludeHtml = IncludeHtml,
                IncludeCss = IncludeCss,
                IncludeJavaScript = IncludeJavaScript,
                IncludeInlineCss = IncludeInlineCss,
                IncludeExternalCss = IncludeExternalCss,
                IncludeConstructedCss = IncludeConstructedCss,
                IncludeInlineScripts = IncludeInlineScripts,
                IncludeExternalScripts = IncludeExternalScripts,
                IncludePerformanceResourceScripts = IncludePerformanceResourceScripts,
                IncludeIframes = IncludeIframes,
                FetchTimeoutMs = FetchTimeoutMs
            };
        }

        /// <summary>
        /// Returns a <see cref="SiteDumpOptions"/> copy with <see cref="IncludeHtml"/>, <see cref="IncludeCss"/>, and <see cref="IncludeJavaScript"/> set according to <paramref name="dumpType"/>.
        /// </summary>
        public static SiteDumpOptions ForType(SiteDumpType dumpType, SiteDumpOptions? baseOptions = null)
        {
            var opts = baseOptions?.Clone() ?? new SiteDumpOptions();
            opts.IncludeHtml = dumpType == SiteDumpType.Html || dumpType == SiteDumpType.All;
            opts.IncludeCss = dumpType == SiteDumpType.Css || dumpType == SiteDumpType.All;
            opts.IncludeJavaScript = dumpType == SiteDumpType.JavaScript || dumpType == SiteDumpType.All;
            return opts;
        }
    }
}