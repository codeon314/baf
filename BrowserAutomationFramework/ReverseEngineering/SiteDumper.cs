using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.ReverseEngineering
{
    /// <summary>
    /// High-level reverse engineering engine for dumping all loaded HTML, CSS stylesheets, and JavaScript assets from a connected browser tab.
    /// </summary>
    public class SiteDumper
    {
        /// <summary>
        /// Raised when a reverse-engineering site dump operation starts.
        /// </summary>
        public event EventHandler<SiteDumpEventArgs>? OnDumpStarted;

        /// <summary>
        /// Raised when a reverse-engineering site dump operation completes successfully.
        /// </summary>
        public event EventHandler<SiteDumpEventArgs>? OnDumpCompleted;

        /// <summary>
        /// Raised when a reverse-engineering site dump operation fails with an exception.
        /// </summary>
        public event EventHandler<SiteDumpEventArgs>? OnDumpFailed;

        /// <summary>
        /// Dumps all HTML from the currently loaded site.
        /// </summary>
        public Task<SiteDumpResult> DumpHtmlAsync(
            BrowserClient client,
            SiteDumpOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return DumpSiteAsync(client, SiteDumpType.Html, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all external, inline, and constructed CSS stylesheets from the currently loaded site.
        /// </summary>
        public Task<SiteDumpResult> DumpCssAsync(
            BrowserClient client,
            SiteDumpOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return DumpSiteAsync(client, SiteDumpType.Css, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all external, inline, and dynamically loaded JavaScript files from the currently loaded site.
        /// </summary>
        public Task<SiteDumpResult> DumpJavaScriptAsync(
            BrowserClient client,
            SiteDumpOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return DumpSiteAsync(client, SiteDumpType.JavaScript, options, cancellationToken);
        }

        /// <summary>
        /// Dumps all HTML, CSS, and JavaScript from the currently loaded site in a single operation.
        /// </summary>
        public Task<SiteDumpResult> DumpAllAsync(
            BrowserClient client,
            SiteDumpOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return DumpSiteAsync(client, SiteDumpType.All, options, cancellationToken);
        }

        /// <summary>
        /// Executes a site dump of the specified <see cref="SiteDumpType"/> against the target <see cref="BrowserClient"/>.
        /// </summary>
        public async Task<SiteDumpResult> DumpSiteAsync(
            BrowserClient client,
            SiteDumpType dumpType,
            SiteDumpOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var resolvedOptions = SiteDumpOptions.ForType(dumpType, options);
            string commandName = dumpType switch
            {
                SiteDumpType.Html => "DUMP_HTML",
                SiteDumpType.Css => "DUMP_CSS",
                SiteDumpType.JavaScript => "DUMP_JS",
                _ => "DUMP_SITE"
            };

            OnDumpStarted?.Invoke(this, new SiteDumpEventArgs(dumpType));

            try
            {
                var cmdResult = await client.SendCommandAsync(commandName, resolvedOptions, cancellationToken);
                var dumpResult = ParseSiteDumpOutput(cmdResult.Output, dumpType, client.CurrentUrl);
                dumpResult.Success = cmdResult.Status == "completed";

                OnDumpCompleted?.Invoke(this, new SiteDumpEventArgs(dumpType, dumpResult));
                return dumpResult;
            }
            catch (Exception ex)
            {
                var failedResult = new SiteDumpResult
                {
                    DumpType = dumpType,
                    PageUrl = client.CurrentUrl ?? string.Empty,
                    TimestampUtc = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message
                };
                OnDumpFailed?.Invoke(this, new SiteDumpEventArgs(dumpType, failedResult, ex));
                throw;
            }
        }

        internal static SiteDumpResult ParseSiteDumpOutput(string? rawOutput, SiteDumpType dumpType, string? fallbackUrl)
        {
            var result = new SiteDumpResult
            {
                DumpType = dumpType,
                PageUrl = fallbackUrl ?? string.Empty,
                TimestampUtc = DateTime.UtcNow,
                Success = true
            };

            if (string.IsNullOrWhiteSpace(rawOutput))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(rawOutput, new JsonDocumentOptions { MaxDepth = 1024 });
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return result;

                if (root.TryGetProperty("pageUrl", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                {
                    result.PageUrl = urlProp.GetString() ?? result.PageUrl;
                }

                if (root.TryGetProperty("pageTitle", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    result.PageTitle = titleProp.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("html", out var htmlProp) && htmlProp.ValueKind == JsonValueKind.String)
                {
                    result.Html = htmlProp.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("htmlResources", out var htmlArr) && htmlArr.ValueKind == JsonValueKind.Array)
                {
                    result.HtmlResources = ParseResourceList(htmlArr, SiteResourceType.Html);
                }

                if (root.TryGetProperty("cssResources", out var cssArr) && cssArr.ValueKind == JsonValueKind.Array)
                {
                    result.CssResources = ParseResourceList(cssArr, SiteResourceType.Css);
                }

                if (root.TryGetProperty("javascriptResources", out var jsArr) && jsArr.ValueKind == JsonValueKind.Array)
                {
                    result.JavaScriptResources = ParseResourceList(jsArr, SiteResourceType.JavaScript);
                }

                if (string.IsNullOrEmpty(result.Html) && result.HtmlResources.Count > 0)
                {
                    result.Html = result.HtmlResources[0].Content;
                }
            }
            catch
            {
                // Fallback if a custom script returned plain HTML string
                if (dumpType == SiteDumpType.Html)
                {
                    result.Html = rawOutput;
                    result.HtmlResources.Add(new SiteResourceItem
                    {
                        Index = 1,
                        Name = "document.html",
                        Category = SiteResourceType.Html,
                        SourceType = "Document",
                        Url = result.PageUrl,
                        Content = rawOutput,
                        SizeBytes = Encoding.UTF8.GetByteCount(rawOutput)
                    });
                }
            }

            return result;
        }

        private static List<SiteResourceItem> ParseResourceList(JsonElement arrayElement, SiteResourceType defaultCategory)
        {
            var list = new List<SiteResourceItem>();
            int idx = 1;

            foreach (var elem in arrayElement.EnumerateArray())
            {
                if (elem.ValueKind != JsonValueKind.Object)
                    continue;

                string content = elem.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String
                    ? (contentProp.GetString() ?? string.Empty)
                    : string.Empty;

                long sizeBytes = elem.TryGetProperty("sizeBytes", out var sizeProp) && sizeProp.TryGetInt64(out long parsedSize)
                    ? parsedSize
                    : Encoding.UTF8.GetByteCount(content);

                int itemIndex = elem.TryGetProperty("index", out var idxProp) && idxProp.TryGetInt32(out int parsedIdx)
                    ? parsedIdx
                    : idx;

                var category = defaultCategory;
                if (elem.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String)
                {
                    if (Enum.TryParse<SiteResourceType>(catProp.GetString(), true, out var parsedCat))
                    {
                        category = parsedCat;
                    }
                }

                var item = new SiteResourceItem
                {
                    Index = itemIndex,
                    Name = elem.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                        ? (nameProp.GetString() ?? $"resource-{itemIndex}")
                        : $"resource-{itemIndex}",
                    Category = category,
                    SourceType = elem.TryGetProperty("sourceType", out var stProp) && stProp.ValueKind == JsonValueKind.String
                        ? (stProp.GetString() ?? "External")
                        : "External",
                    Url = elem.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String
                        ? (urlProp.GetString() ?? string.Empty)
                        : string.Empty,
                    Content = content,
                    SizeBytes = sizeBytes,
                    Error = elem.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String
                        ? errProp.GetString()
                        : null
                };

                list.Add(item);
                idx++;
            }

            return list;
        }
    }
}