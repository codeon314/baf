using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Contains all extracted HTML, CSS, and JavaScript assets from a reverse-engineering site dump operation,
    /// along with helper methods for combining, serializing, and exporting the dumped files to disk.
    /// </summary>
    public class SiteDumpResult
    {
        /// <summary>
        /// Gets the file dialog filter used when saving dumped resources or combined output.
        /// </summary>
        public const string FileDialogFilter = "All Files (*.*)|*.*|HTML Files (*.html)|*.html|CSS Files (*.css)|*.css|JavaScript Files (*.js)|*.js";

        /// <summary>
        /// Gets or sets the <see cref="SiteDumpType"/> that was requested for this dump.
        /// </summary>
        [JsonPropertyName("dumpType")]
        public SiteDumpType DumpType { get; set; } = SiteDumpType.All;

        /// <summary>
        /// Gets or sets the URL of the page from which resources were dumped.
        /// </summary>
        [JsonPropertyName("pageUrl")]
        public string PageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document title of the dumped page.
        /// </summary>
        [JsonPropertyName("pageTitle")]
        public string PageTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the dump completed.
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether the dump operation succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if <see cref="Success"/> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the primary serialized HTML of the main document.
        /// </summary>
        [JsonPropertyName("html")]
        public string Html { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of extracted HTML resources (main document and accessible iframes).
        /// </summary>
        [JsonPropertyName("htmlResources")]
        public List<SiteResourceItem> HtmlResources { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of extracted CSS stylesheet resources (external, inline, and constructed).
        /// </summary>
        [JsonPropertyName("cssResources")]
        public List<SiteResourceItem> CssResources { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of extracted JavaScript resources (external, inline, and dynamically loaded).
        /// </summary>
        [JsonPropertyName("javascriptResources")]
        public List<SiteResourceItem> JavaScriptResources { get; set; } = new();

        /// <summary>
        /// Gets the total number of HTML, CSS, and JavaScript resources captured in this dump.
        /// </summary>
        [JsonIgnore]
        public int TotalResourceCount => HtmlResources.Count + CssResources.Count + JavaScriptResources.Count;

        /// <summary>
        /// Gets whether this site dump contains at least one captured HTML, CSS, or JavaScript resource.
        /// </summary>
        [JsonIgnore]
        public bool HasResources => TotalResourceCount > 0;

        /// <summary>
        /// Gets the combined size in bytes across all captured resources.
        /// </summary>
        [JsonIgnore]
        public long TotalSizeBytes => GetAllResources().Sum(r => r.SizeBytes > 0 ? r.SizeBytes : Encoding.UTF8.GetByteCount(r.Content ?? string.Empty));

        /// <summary>
        /// Returns a flat read-only list of all dumped HTML, CSS, and JavaScript resource items.
        /// </summary>
        public IReadOnlyList<SiteResourceItem> GetAllResources()
        {
            var all = new List<SiteResourceItem>(TotalResourceCount);
            all.AddRange(HtmlResources);
            all.AddRange(CssResources);
            all.AddRange(JavaScriptResources);
            return all;
        }

        /// <summary>
        /// Filters all dumped resources by an optional search query and/or <see cref="SiteResourceType"/> category.
        /// </summary>
        public IReadOnlyList<SiteResourceItem> FilterResources(
            string? filter = null,
            SiteResourceType? category = null,
            bool searchContent = true)
        {
            return GetAllResources()
                .Where(r => (!category.HasValue || r.Category == category.Value) &&
                            r.MatchesFilter(filter, searchContent))
                .ToList();
        }

        /// <summary>
        /// Returns a concise status summary of the dumped HTML, CSS, and JavaScript resource counts and total size.
        /// </summary>
        public string GetStatusSummary()
        {
            return $"Dumped {HtmlResources.Count} HTML, {CssResources.Count} CSS, {JavaScriptResources.Count} JS ({GetFormattedTotalSize()})";
        }

        /// <summary>
        /// Returns a human-readable total size string (e.g. "512.4 KB").
        /// </summary>
        public string GetFormattedTotalSize()
        {
            long bytes = TotalSizeBytes;
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:0.1} KB";
            return $"{bytes / (1024.0 * 1024.0):0.2} MB";
        }

        /// <summary>
        /// Returns the combined HTML source across the main document and any captured iframes.
        /// </summary>
        public string GetCombinedHtml()
        {
            if (HtmlResources.Count <= 1)
                return !string.IsNullOrEmpty(Html) ? Html : (HtmlResources.FirstOrDefault()?.Content ?? string.Empty);

            var sb = new StringBuilder();
            foreach (var item in HtmlResources)
            {
                sb.AppendLine($"<!-- ======================================================================");
                sb.AppendLine($"     HTML Resource #{item.Index}: {item.Name} ({item.SourceType})");
                sb.AppendLine($"     URL: {item.Url}");
                sb.AppendLine($"     ====================================================================== -->");
                sb.AppendLine(item.Content);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns all dumped CSS stylesheets concatenated into a single stylesheet with source headers.
        /// </summary>
        public string GetCombinedCss()
        {
            if (CssResources.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in CssResources)
            {
                sb.AppendLine("/* ======================================================================");
                sb.AppendLine($"   CSS Resource #{item.Index}: {item.Name} ({item.SourceType})");
                sb.AppendLine($"   URL: {item.Url}");
                if (!string.IsNullOrWhiteSpace(item.Error))
                {
                    sb.AppendLine($"   Error: {item.Error}");
                }
                sb.AppendLine("   ====================================================================== */");
                sb.AppendLine(item.Content);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns all dumped JavaScript resources concatenated into a single script with source headers.
        /// </summary>
        public string GetCombinedJavaScript()
        {
            if (JavaScriptResources.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in JavaScriptResources)
            {
                sb.AppendLine("// ======================================================================");
                sb.AppendLine($"// JS Resource #{item.Index}: {item.Name} ({item.SourceType})");
                sb.AppendLine($"// URL: {item.Url}");
                if (!string.IsNullOrWhiteSpace(item.Error))
                {
                    sb.AppendLine($"// Error: {item.Error}");
                }
                sb.AppendLine("// ======================================================================");
                sb.AppendLine(item.Content);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns the combined text output corresponding to <see cref="DumpType"/>.
        /// </summary>
        public string GetCombinedContent() => GetCombinedContent(DumpType);

        /// <summary>
        /// Returns the combined text output for the specified <paramref name="viewType"/>.
        /// </summary>
        public string GetCombinedContent(SiteDumpType viewType)
        {
            return viewType switch
            {
                SiteDumpType.Html => GetCombinedHtml(),
                SiteDumpType.Css => GetCombinedCss(),
                SiteDumpType.JavaScript => GetCombinedJavaScript(),
                _ => BuildFullCombinedSummary()
            };
        }

        /// <summary>
        /// Returns a default file name (such as <c>"combined.html"</c>, <c>"combined.css"</c>, or <c>"combined.js"</c>) for the given <see cref="SiteDumpType"/>.
        /// </summary>
        public static string GetSuggestedCombinedFileName(SiteDumpType viewType)
        {
            return viewType switch
            {
                SiteDumpType.Html => "combined.html",
                SiteDumpType.Css => "combined.css",
                SiteDumpType.JavaScript => "combined.js",
                _ => "combined_dump.txt"
            };
        }

        /// <summary>
        /// Returns the recommended initial <see cref="SiteDumpViewMode"/> to display after this dump completes.
        /// </summary>
        public SiteDumpViewMode GetDefaultViewMode()
        {
            return DumpType switch
            {
                SiteDumpType.Html => SiteDumpViewMode.CombinedHtml,
                SiteDumpType.Css => SiteDumpViewMode.CombinedCss,
                SiteDumpType.JavaScript => SiteDumpViewMode.CombinedJavaScript,
                _ => HasResources ? SiteDumpViewMode.SelectedResource : SiteDumpViewMode.CombinedAll
            };
        }

        /// <summary>
        /// Resolves the source text and suggested export filename for the specified <see cref="SiteDumpViewMode"/> and optional <paramref name="selectedResource"/>.
        /// </summary>
        /// <param name="viewMode">The active viewer mode.</param>
        /// <param name="selectedResource">The currently selected resource when <paramref name="viewMode"/> is <see cref="SiteDumpViewMode.SelectedResource"/>.</param>
        /// <param name="suggestedFileName">Outputs the recommended filename for saving the returned content.</param>
        /// <returns>The resolved resource or combined source text.</returns>
        public string GetViewContent(SiteDumpViewMode viewMode, SiteResourceItem? selectedResource, out string suggestedFileName)
        {
            if (viewMode == SiteDumpViewMode.SelectedResource)
            {
                if (selectedResource == null)
                {
                    suggestedFileName = "resource.txt";
                    return string.Empty;
                }

                suggestedFileName = string.IsNullOrWhiteSpace(selectedResource.Name) ? "resource.txt" : selectedResource.Name;
                return selectedResource.Content;
            }

            var resolvedType = viewMode switch
            {
                SiteDumpViewMode.CombinedHtml => SiteDumpType.Html,
                SiteDumpViewMode.CombinedCss => SiteDumpType.Css,
                SiteDumpViewMode.CombinedJavaScript => SiteDumpType.JavaScript,
                _ => DumpType
            };

            suggestedFileName = GetSuggestedCombinedFileName(resolvedType);
            return GetCombinedContent(resolvedType);
        }

        /// <summary>
        /// Resolves the source text and suggested export filename for either a specific <paramref name="selectedResource"/> or a combined <paramref name="viewType"/>.
        /// </summary>
        /// <param name="viewType">The combined dump type to view if <paramref name="selectedResource"/> is null.</param>
        /// <param name="selectedResource">An optional individual resource item to view instead of combined output.</param>
        /// <param name="suggestedFileName">Outputs the recommended filename for saving the returned content.</param>
        /// <returns>The resolved resource or combined source text.</returns>
        public string GetViewContent(SiteDumpType? viewType, SiteResourceItem? selectedResource, out string suggestedFileName)
        {
            if (selectedResource != null)
            {
                suggestedFileName = string.IsNullOrWhiteSpace(selectedResource.Name) ? "resource.txt" : selectedResource.Name;
                return selectedResource.Content;
            }

            var resolvedType = viewType ?? DumpType;
            suggestedFileName = GetSuggestedCombinedFileName(resolvedType);
            return GetCombinedContent(resolvedType);
        }

        private string BuildFullCombinedSummary()
        {
            var sb = new StringBuilder();
            if (HtmlResources.Count > 0 || !string.IsNullOrEmpty(Html))
            {
                sb.AppendLine("/* ==================== DUMPED HTML ==================== */");
                sb.AppendLine(GetCombinedHtml());
                sb.AppendLine();
            }
            if (CssResources.Count > 0)
            {
                sb.AppendLine("/* ==================== DUMPED CSS ==================== */");
                sb.AppendLine(GetCombinedCss());
                sb.AppendLine();
            }
            if (JavaScriptResources.Count > 0)
            {
                sb.AppendLine("/* ==================== DUMPED JAVASCRIPT ==================== */");
                sb.AppendLine(GetCombinedJavaScript());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Serializes the entire <see cref="SiteDumpResult"/> to JSON.
        /// </summary>
        public string ToJson(bool writeIndented = true)
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = writeIndented });
        }

        /// <summary>
        /// Saves the resolved view content for the specified <see cref="SiteDumpViewMode"/> to the given file path.
        /// </summary>
        public void SaveViewContentToFile(string filePath, SiteDumpViewMode viewMode, SiteResourceItem? selectedResource)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            if (viewMode == SiteDumpViewMode.SelectedResource)
            {
                if (selectedResource == null)
                    throw new InvalidOperationException("No resource is selected to save.");

                selectedResource.SaveToFile(filePath);
                return;
            }

            var resolvedType = viewMode switch
            {
                SiteDumpViewMode.CombinedHtml => SiteDumpType.Html,
                SiteDumpViewMode.CombinedCss => SiteDumpType.Css,
                SiteDumpViewMode.CombinedJavaScript => SiteDumpType.JavaScript,
                _ => DumpType
            };

            SaveCombinedToFile(filePath, resolvedType);
        }

        /// <summary>
        /// Asynchronously saves the resolved view content for the specified <see cref="SiteDumpViewMode"/> to the given file path.
        /// </summary>
        public Task SaveViewContentToFileAsync(
            string filePath,
            SiteDumpViewMode viewMode,
            SiteResourceItem? selectedResource,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            if (viewMode == SiteDumpViewMode.SelectedResource)
            {
                if (selectedResource == null)
                    throw new InvalidOperationException("No resource is selected to save.");

                return selectedResource.SaveToFileAsync(filePath, cancellationToken);
            }

            var resolvedType = viewMode switch
            {
                SiteDumpViewMode.CombinedHtml => SiteDumpType.Html,
                SiteDumpViewMode.CombinedCss => SiteDumpType.Css,
                SiteDumpViewMode.CombinedJavaScript => SiteDumpType.JavaScript,
                _ => DumpType
            };

            return SaveCombinedToFileAsync(filePath, resolvedType, cancellationToken);
        }

        /// <summary>
        /// Saves the resolved view content (either a specific <paramref name="selectedResource"/> or the combined output for <paramref name="viewType"/>) to the specified file path.
        /// </summary>
        public void SaveViewContentToFile(string filePath, SiteDumpType? viewType, SiteResourceItem? selectedResource)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            if (selectedResource != null)
            {
                selectedResource.SaveToFile(filePath);
                return;
            }

            SaveCombinedToFile(filePath, viewType ?? DumpType);
        }

        /// <summary>
        /// Asynchronously saves the resolved view content (either a specific <paramref name="selectedResource"/> or the combined output for <paramref name="viewType"/>) to the specified file path.
        /// </summary>
        public Task SaveViewContentToFileAsync(
            string filePath,
            SiteDumpType? viewType,
            SiteResourceItem? selectedResource,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            if (selectedResource != null)
            {
                return selectedResource.SaveToFileAsync(filePath, cancellationToken);
            }

            return SaveCombinedToFileAsync(filePath, viewType ?? DumpType, cancellationToken);
        }

        /// <summary>
        /// Saves the combined dump output (for <paramref name="viewType"/> or <see cref="DumpType"/>) to a single file.
        /// </summary>
        public void SaveCombinedToFile(string filePath, SiteDumpType? viewType = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, GetCombinedContent(viewType ?? DumpType), Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously saves the combined dump output (for <paramref name="viewType"/> or <see cref="DumpType"/>) to a single file.
        /// </summary>
        public Task SaveCombinedToFileAsync(string filePath, SiteDumpType? viewType = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return File.WriteAllTextAsync(filePath, GetCombinedContent(viewType ?? DumpType), Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Writes all dumped HTML, CSS, and JavaScript resources as individual files and combined bundles into <paramref name="directoryPath"/>.
        /// </summary>
        public string SaveToDirectory(string directoryPath, bool includeCombinedFiles = true, bool includeManifest = true)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);

            // 1. Save HTML
            if (HtmlResources.Count > 0 || !string.IsNullOrEmpty(Html))
            {
                string mainHtml = !string.IsNullOrEmpty(Html) ? Html : (HtmlResources[0].Content ?? string.Empty);
                File.WriteAllText(Path.Combine(directoryPath, "index.html"), mainHtml, Encoding.UTF8);

                if (HtmlResources.Count > 1)
                {
                    string htmlDir = Path.Combine(directoryPath, "html");
                    Directory.CreateDirectory(htmlDir);
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in HtmlResources)
                    {
                        string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"document-{item.Index}.html"), usedNames);
                        File.WriteAllText(Path.Combine(htmlDir, fileName), item.Content ?? string.Empty, Encoding.UTF8);
                    }
                }
            }

            // 2. Save CSS
            if (CssResources.Count > 0)
            {
                string cssDir = Path.Combine(directoryPath, "css");
                Directory.CreateDirectory(cssDir);
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in CssResources)
                {
                    string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"style-{item.Index}.css"), usedNames);
                    File.WriteAllText(Path.Combine(cssDir, fileName), item.Content ?? string.Empty, Encoding.UTF8);
                }

                if (includeCombinedFiles)
                {
                    File.WriteAllText(Path.Combine(directoryPath, "combined.css"), GetCombinedCss(), Encoding.UTF8);
                }
            }

            // 3. Save JavaScript
            if (JavaScriptResources.Count > 0)
            {
                string jsDir = Path.Combine(directoryPath, "js");
                Directory.CreateDirectory(jsDir);
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in JavaScriptResources)
                {
                    string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"script-{item.Index}.js"), usedNames);
                    File.WriteAllText(Path.Combine(jsDir, fileName), item.Content ?? string.Empty, Encoding.UTF8);
                }

                if (includeCombinedFiles)
                {
                    File.WriteAllText(Path.Combine(directoryPath, "combined.js"), GetCombinedJavaScript(), Encoding.UTF8);
                }
            }

            // 4. Save Manifest
            if (includeManifest)
            {
                var manifest = new
                {
                    dumpType = DumpType.ToString(),
                    pageUrl = PageUrl,
                    pageTitle = PageTitle,
                    timestampUtc = TimestampUtc,
                    totalResources = TotalResourceCount,
                    totalSizeBytes = TotalSizeBytes,
                    resources = GetAllResources().Select(r => new
                    {
                        r.Index,
                        r.Name,
                        Category = r.Category.ToString(),
                        r.SourceType,
                        r.Url,
                        r.SizeBytes,
                        r.Error
                    }).ToList()
                };

                string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(directoryPath, "manifest.json"), manifestJson, Encoding.UTF8);
            }

            return directoryPath;
        }

        /// <summary>
        /// Asynchronously writes all dumped HTML, CSS, and JavaScript resources into <paramref name="directoryPath"/>.
        /// </summary>
        public async Task<string> SaveToDirectoryAsync(
            string directoryPath,
            bool includeCombinedFiles = true,
            bool includeManifest = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);

            if (HtmlResources.Count > 0 || !string.IsNullOrEmpty(Html))
            {
                string mainHtml = !string.IsNullOrEmpty(Html) ? Html : (HtmlResources[0].Content ?? string.Empty);
                await File.WriteAllTextAsync(Path.Combine(directoryPath, "index.html"), mainHtml, Encoding.UTF8, cancellationToken);

                if (HtmlResources.Count > 1)
                {
                    string htmlDir = Path.Combine(directoryPath, "html");
                    Directory.CreateDirectory(htmlDir);
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in HtmlResources)
                    {
                        string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"document-{item.Index}.html"), usedNames);
                        await File.WriteAllTextAsync(Path.Combine(htmlDir, fileName), item.Content ?? string.Empty, Encoding.UTF8, cancellationToken);
                    }
                }
            }

            if (CssResources.Count > 0)
            {
                string cssDir = Path.Combine(directoryPath, "css");
                Directory.CreateDirectory(cssDir);
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in CssResources)
                {
                    string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"style-{item.Index}.css"), usedNames);
                    await File.WriteAllTextAsync(Path.Combine(cssDir, fileName), item.Content ?? string.Empty, Encoding.UTF8, cancellationToken);
                }

                if (includeCombinedFiles)
                {
                    await File.WriteAllTextAsync(Path.Combine(directoryPath, "combined.css"), GetCombinedCss(), Encoding.UTF8, cancellationToken);
                }
            }

            if (JavaScriptResources.Count > 0)
            {
                string jsDir = Path.Combine(directoryPath, "js");
                Directory.CreateDirectory(jsDir);
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in JavaScriptResources)
                {
                    string fileName = EnsureUniqueFileName(SanitizeFileName(item.Name, $"script-{item.Index}.js"), usedNames);
                    await File.WriteAllTextAsync(Path.Combine(jsDir, fileName), item.Content ?? string.Empty, Encoding.UTF8, cancellationToken);
                }

                if (includeCombinedFiles)
                {
                    await File.WriteAllTextAsync(Path.Combine(directoryPath, "combined.js"), GetCombinedJavaScript(), Encoding.UTF8, cancellationToken);
                }
            }

            if (includeManifest)
            {
                var manifest = new
                {
                    dumpType = DumpType.ToString(),
                    pageUrl = PageUrl,
                    pageTitle = PageTitle,
                    timestampUtc = TimestampUtc,
                    totalResources = TotalResourceCount,
                    totalSizeBytes = TotalSizeBytes,
                    resources = GetAllResources().Select(r => new
                    {
                        r.Index,
                        r.Name,
                        Category = r.Category.ToString(),
                        r.SourceType,
                        r.Url,
                        r.SizeBytes,
                        r.Error
                    }).ToList()
                };

                string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(directoryPath, "manifest.json"), manifestJson, Encoding.UTF8, cancellationToken);
            }

            return directoryPath;
        }

        private static string SanitizeFileName(string? rawName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return fallback;

            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(rawName.Length);
            foreach (char c in rawName.Trim())
            {
                sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
            }

            string cleaned = sb.ToString().Trim('.');
            if (string.IsNullOrWhiteSpace(cleaned))
                return fallback;

            if (cleaned.Length > 100)
            {
                string ext = Path.GetExtension(cleaned);
                string baseName = Path.GetFileNameWithoutExtension(cleaned);
                cleaned = baseName.Substring(0, Math.Min(baseName.Length, 85)) + ext;
            }

            return cleaned;
        }

        private static string EnsureUniqueFileName(string candidate, HashSet<string> usedNames)
        {
            if (usedNames.Add(candidate))
                return candidate;

            string ext = Path.GetExtension(candidate);
            string baseName = Path.GetFileNameWithoutExtension(candidate);
            int counter = 2;
            while (true)
            {
                string numbered = $"{baseName}_{counter}{ext}";
                if (usedNames.Add(numbered))
                    return numbered;
                counter++;
            }
        }
    }
}