using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Classifies the asset category of an individual <see cref="SiteResourceItem"/>.
    /// </summary>
    public enum SiteResourceType
    {
        /// <summary>
        /// HTML document or iframe markup.
        /// </summary>
        Html,

        /// <summary>
        /// CSS stylesheet (external, inline, or constructed).
        /// </summary>
        Css,

        /// <summary>
        /// JavaScript source or JSON script block (external, inline, or dynamic resource).
        /// </summary>
        JavaScript
    }

    /// <summary>
    /// Specifies the scope of resources to capture in a reverse-engineering site dump operation.
    /// </summary>
    public enum SiteDumpType
    {
        /// <summary>
        /// Dumps only HTML documents and iframes.
        /// </summary>
        Html,

        /// <summary>
        /// Dumps only CSS stylesheets.
        /// </summary>
        Css,

        /// <summary>
        /// Dumps only JavaScript assets.
        /// </summary>
        JavaScript,

        /// <summary>
        /// Dumps all HTML, CSS, and JavaScript assets.
        /// </summary>
        All
    }

    /// <summary>
    /// Specifies the active viewer mode when inspecting or exporting a <see cref="SiteDumpResult"/>.
    /// </summary>
    public enum SiteDumpViewMode
    {
        /// <summary>
        /// Displays or exports an individual selected <see cref="SiteResourceItem"/>.
        /// </summary>
        SelectedResource,

        /// <summary>
        /// Displays or exports the combined HTML source across the document and iframes.
        /// </summary>
        CombinedHtml,

        /// <summary>
        /// Displays or exports the combined CSS source across all stylesheets.
        /// </summary>
        CombinedCss,

        /// <summary>
        /// Displays or exports the combined JavaScript source across all scripts.
        /// </summary>
        CombinedJavaScript,

        /// <summary>
        /// Displays or exports the full combined summary across HTML, CSS, and JavaScript.
        /// </summary>
        CombinedAll
    }

    /// <summary>
    /// Represents a single HTML, CSS, or JavaScript resource captured during a <see cref="ReverseEngineering.SiteDumper"/> operation.
    /// </summary>
    public class SiteResourceItem
    {
        /// <summary>
        /// Gets or sets the derived file name or display name of the resource.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="SiteResourceType"/> category of this resource.
        /// </summary>
        [JsonPropertyName("category")]
        public SiteResourceType Category { get; set; } = SiteResourceType.Html;

        /// <summary>
        /// Gets or sets the origin classification of this resource (e.g., <c>Document</c>, <c>Iframe</c>, <c>External</c>, <c>Inline</c>, <c>Constructed</c>, or <c>DynamicResource</c>).
        /// </summary>
        [JsonPropertyName("sourceType")]
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source URL of the resource (or the document URL for inline/constructed assets).
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the raw text content of the resource.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size in bytes of the resource content.
        /// </summary>
        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the 1-based index of this resource within its category.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the fetch or serialization error message if the resource could not be retrieved cleanly.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Returns a human-readable size string (e.g., "14.2 KB").
        /// </summary>
        public string GetFormattedSize()
        {
            long bytes = SizeBytes > 0 ? SizeBytes : Encoding.UTF8.GetByteCount(Content ?? string.Empty);
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:0.1} KB";
            return $"{bytes / (1024.0 * 1024.0):0.2} MB";
        }

        /// <summary>
        /// Returns a multi-line summary of this resource's metadata.
        /// </summary>
        public string GetFormattedDetails()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name:        {Name}");
            sb.AppendLine($"Category:    {Category}");
            sb.AppendLine($"Source Type: {SourceType}");
            sb.AppendLine($"Size:        {GetFormattedSize()} ({SizeBytes:N0} bytes)");
            sb.AppendLine($"URL:         {Url}");
            if (!string.IsNullOrWhiteSpace(Error))
            {
                sb.AppendLine($"Error:       {Error}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Determines whether this resource item matches the given search filter across its name, URL, source type, category, and optionally content.
        /// </summary>
        public bool MatchesFilter(string? filter, bool searchContent = true)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            string trimmed = filter.Trim();
            if (Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                Url.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                SourceType.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                Category.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return searchContent &&
                   !string.IsNullOrEmpty(Content) &&
                   Content.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Saves this resource's content to the specified file path.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, Content ?? string.Empty, Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously saves this resource's content to the specified file path.
        /// </summary>
        public Task SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return File.WriteAllTextAsync(filePath, Content ?? string.Empty, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Returns a formatted single-line summary of the resource's category, source type, name, and size.
        /// </summary>
        public override string ToString()
        {
            string tag = Category switch
            {
                SiteResourceType.Html => "HTML",
                SiteResourceType.Css => "CSS",
                SiteResourceType.JavaScript => "JS",
                _ => Category.ToString().ToUpperInvariant()
            };

            string errSuffix = !string.IsNullOrWhiteSpace(Error) ? " [!]" : string.Empty;
            return $"[{tag} | {SourceType}] {Name} ({GetFormattedSize()}){errSuffix}";
        }
    }
}