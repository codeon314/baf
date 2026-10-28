using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.Scraping
{
    /// <summary>
    /// High-level execution engine for running structured web scraping jobs, single-element extractions,
    /// HTML table extractions, and multi-page automated scrapes.
    /// </summary>
    public class DataScraper
    {
        private readonly AutomationRunner _runner;

        /// <summary>
        /// Raised when a <see cref="ScrapeDefinition"/> begins execution.
        /// </summary>
        public event EventHandler<ScrapeEventArgs>? OnScrapeStarted;

        /// <summary>
        /// Raised when a single page of a <see cref="ScrapeDefinition"/> finishes extracting records.
        /// </summary>
        public event EventHandler<ScrapePageEventArgs>? OnScrapePageCompleted;

        /// <summary>
        /// Raised before waiting the configured <see cref="ScrapeDefinition.PageDelayMs"/> prior to advancing to the next page.
        /// </summary>
        public event EventHandler<ScrapePageDelayEventArgs>? OnPageTurnDelay;

        /// <summary>
        /// Raised when an HTTP 429 rate-limit response is detected on the current page.
        /// </summary>
        public event EventHandler<ScrapeRateLimitEventArgs>? OnRateLimitDetected;

        /// <summary>
        /// Raised when a <see cref="ScrapeDefinition"/> finishes extracting all pages.
        /// </summary>
        public event EventHandler<ScrapeEventArgs>? OnScrapeCompleted;

        /// <summary>
        /// Raised when a <see cref="ScrapeDefinition"/> fails with an exception.
        /// </summary>
        public event EventHandler<ScrapeEventArgs>? OnScrapeFailed;

        /// <summary>
        /// Gets the <see cref="AutomationRunner"/> used to execute pre-scrape, post-scrape, and pagination sequences.
        /// </summary>
        public AutomationRunner Runner => _runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataScraper"/> class.
        /// </summary>
        /// <param name="runner">An optional <see cref="AutomationRunner"/> used to execute pre-scrape, post-scrape, and pagination sequences.</param>
        public DataScraper(AutomationRunner? runner = null)
        {
            _runner = runner ?? new AutomationRunner(this);
        }

        /// <summary>
        /// Executes all active <see cref="ScrapeDefinition"/> items in an <see cref="AutomationProject"/>.
        /// </summary>
        public async Task<List<ScrapeResult>> RunProjectScrapesAsync(
            BrowserClient client,
            AutomationProject project,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var results = new List<ScrapeResult>();
            foreach (var scrape in project.Scrapes.Where(s => s.IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var res = await RunScrapeAsync(client, scrape, project.Sequences, cancellationToken);
                results.Add(res);
            }

            return results;
        }

        /// <summary>
        /// Executes all active <see cref="ScrapeDefinition"/> items in an <see cref="AutomationProject"/>
        /// and merges their records into a single combined <see cref="ScrapeResult"/>.
        /// </summary>
        public async Task<ScrapeResult> RunProjectScrapesCombinedAsync(
            BrowserClient client,
            AutomationProject project,
            bool deduplicate = false,
            CancellationToken cancellationToken = default)
        {
            var results = await RunProjectScrapesAsync(client, project, cancellationToken);
            return ScrapeResult.Combine(results, deduplicate: deduplicate);
        }

        /// <summary>
        /// Executes a single <see cref="ScrapeDefinition"/>, including any configured pre-scrape automation,
        /// multi-page pagination, and post-scrape automation.
        /// </summary>
        public async Task<ScrapeResult> RunScrapeAsync(
            BrowserClient client,
            ScrapeDefinition definition,
            IEnumerable<AutomationSequence>? availableSequences = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var sequenceLookup = availableSequences?
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, AutomationSequence>(StringComparer.OrdinalIgnoreCase);

            var aggregateResult = new ScrapeResult
            {
                ScrapeId = definition.Id,
                ScrapeName = definition.Name,
                TimestampUtc = DateTime.UtcNow,
                PagesScraped = 0
            };

            OnScrapeStarted?.Invoke(this, new ScrapeEventArgs(definition));

            try
            {
                // 1. Run Pre-Scrape Automation Sequence if configured
                if (!string.IsNullOrWhiteSpace(definition.PreScrapeSequenceName) &&
                    sequenceLookup.TryGetValue(definition.PreScrapeSequenceName, out var preSeq))
                {
                    string? beforePreLoadId = client.CurrentPageLoadId;
                    await _runner.RunSequenceAsync(client, preSeq, cancellationToken);
                    await client.WaitForPageTransitionAsync(beforePreLoadId, expectNavigation: false, cancellationToken: cancellationToken);
                }

                // Ensure initial page is completely loaded
                await client.WaitForPageLoadAsync(cancellationToken: cancellationToken);

                int totalPagesToScrape = definition.EnablePagination ? Math.Max(1, definition.MaxPages) : 1;
                int startPageNum = Math.Max(1, definition.PaginationStartPage);
                int pageIncrement = Math.Max(1, definition.PaginationPageIncrement);

                // If URL pagination is configured, ensure Page 1 starts on the initial resolved URL
                if (definition.EnablePagination && !string.IsNullOrWhiteSpace(definition.PaginationUrlPattern))
                {
                    string firstPageUrl = SelectorHelper.ResolvePaginationUrl(definition.PaginationUrlPattern, startPageNum);
                    if (!string.IsNullOrWhiteSpace(firstPageUrl) && !IsSamePageUrl(client.CurrentUrl, firstPageUrl))
                    {
                        await client.NavigateToUrlAsync(firstPageUrl, cancellationToken: cancellationToken);
                    }
                }

                if (definition.WaitBeforeScrapeMs > 0)
                {
                    await Task.Delay(definition.WaitBeforeScrapeMs, cancellationToken);
                }

                var seenFingerprints = new HashSet<string>();

                // 2. Scrape Page Loop
                for (int page = 1; page <= totalPagesToScrape; page++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int currentTargetPageNum = startPageNum + ((page - 1) * pageIncrement);
                    string? currentTargetUrl = (definition.EnablePagination && !string.IsNullOrWhiteSpace(definition.PaginationUrlPattern))
                        ? SelectorHelper.ResolvePaginationUrl(definition.PaginationUrlPattern, currentTargetPageNum)
                        : client.CurrentUrl;

                    await EnsurePageNotRateLimitedAsync(client, definition, page, currentTargetUrl, cancellationToken);

                    var pageRecords = await ExecuteSinglePageScrapeInternalAsync(client, definition, cancellationToken);
                    var addedPageRecords = new List<Dictionary<string, string?>>();

                    foreach (var record in pageRecords)
                    {
                        if (definition.DeduplicateRows)
                        {
                            string fp = ScrapeResult.GetRecordFingerprint(record);
                            if (!seenFingerprints.Add(fp))
                                continue;
                        }

                        aggregateResult.Records.Add(record);
                        addedPageRecords.Add(record);
                    }

                    aggregateResult.PagesScraped = page;
                    OnScrapePageCompleted?.Invoke(this, new ScrapePageEventArgs(definition, page, addedPageRecords));

                    // Advance to next page if more pages remain
                    if (page < totalPagesToScrape && definition.EnablePagination)
                    {
                        // Wait configured page delay BEFORE moving onto the next page to avoid triggering rate limits
                        if (definition.PageDelayMs > 0)
                        {
                            OnPageTurnDelay?.Invoke(this, new ScrapePageDelayEventArgs(definition, page + 1, definition.PageDelayMs));
                            await Task.Delay(definition.PageDelayMs, cancellationToken);
                        }

                        bool advanced = false;

                        if (!string.IsNullOrWhiteSpace(definition.PaginationUrlPattern))
                        {
                            int nextTargetPage = startPageNum + (page * pageIncrement);
                            string nextUrl = SelectorHelper.ResolvePaginationUrl(definition.PaginationUrlPattern, nextTargetPage);
                            if (!string.IsNullOrWhiteSpace(nextUrl))
                            {
                                await client.NavigateToUrlAsync(nextUrl, cancellationToken: cancellationToken);
                                advanced = true;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(definition.PaginationSequenceName) &&
                                 sequenceLookup.TryGetValue(definition.PaginationSequenceName, out var pageSeq))
                        {
                            string? beforePageLoadId = client.CurrentPageLoadId;
                            await _runner.RunSequenceAsync(client, pageSeq, cancellationToken);
                            await client.WaitForPageTransitionAsync(beforePageLoadId, expectNavigation: false, cancellationToken: cancellationToken);
                            advanced = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(definition.NextPageSelector))
                        {
                            try
                            {
                                string? beforePageLoadId = client.CurrentPageLoadId;
                                var clickRes = await client.ClickReactElementAsync(definition.NextPageSelector, cancellationToken);
                                bool isFullNavigation = clickRes.Output != null &&
                                                        clickRes.Output.StartsWith("Link click executed", StringComparison.OrdinalIgnoreCase);
                                await client.WaitForPageTransitionAsync(beforePageLoadId, expectNavigation: isFullNavigation, cancellationToken: cancellationToken);
                                advanced = true;
                            }
                            catch (Exception) when (!cancellationToken.IsCancellationRequested)
                            {
                                // Next page element not found or no longer clickable; stop pagination cleanly
                                break;
                            }
                        }

                        if (!advanced)
                            break;
                    }
                }

                // 3. Run Post-Scrape Automation Sequence if configured
                if (!string.IsNullOrWhiteSpace(definition.PostScrapeSequenceName) &&
                    sequenceLookup.TryGetValue(definition.PostScrapeSequenceName, out var postSeq))
                {
                    await _runner.RunSequenceAsync(client, postSeq, cancellationToken);
                }

                aggregateResult.Success = true;
                OnScrapeCompleted?.Invoke(this, new ScrapeEventArgs(definition, aggregateResult));
                return aggregateResult;
            }
            catch (Exception ex)
            {
                aggregateResult.Success = false;
                aggregateResult.ErrorMessage = ex.Message;
                OnScrapeFailed?.Invoke(this, new ScrapeEventArgs(definition, aggregateResult, ex));
                throw;
            }
        }

        private async Task<List<Dictionary<string, string?>>> ExecuteSinglePageScrapeInternalAsync(
            BrowserClient client,
            ScrapeDefinition definition,
            CancellationToken cancellationToken)
        {
            var payload = new
            {
                containerSelector = definition.ContainerSelector,
                fields = definition.Fields.Select(f => new
                {
                    name = f.Name,
                    selector = f.Selector,
                    extractionType = f.ExtractionType.ToString(),
                    attributeName = f.AttributeName,
                    defaultValue = f.DefaultValue,
                    regexPattern = f.RegexPattern,
                    regexGroup = f.RegexGroup,
                    trim = f.Trim,
                    matchAllElements = f.MatchAllElements
                }).ToList()
            };

            var cmdResult = await client.SendCommandAsync("SCRAPE_DATA", payload, cancellationToken);
            var records = ParseRecordsFromCommandOutput(cmdResult.Output);

            // Apply C#-side post-processing (Regex/Trim/DefaultValue fallback) in case custom scripts returned raw strings
            foreach (var record in records)
            {
                foreach (var field in definition.Fields)
                {
                    if (!record.TryGetValue(field.Name, out var rawVal) || string.IsNullOrEmpty(rawVal))
                    {
                        record[field.Name] = field.DefaultValue;
                        continue;
                    }

                    string processed = field.Trim ? rawVal.Trim() : rawVal;
                    if (!string.IsNullOrWhiteSpace(field.RegexPattern))
                    {
                        try
                        {
                            var match = Regex.Match(processed, field.RegexPattern);
                            if (match.Success)
                            {
                                processed = field.RegexGroup >= 0 && field.RegexGroup < match.Groups.Count
                                    ? match.Groups[field.RegexGroup].Value
                                    : match.Value;
                            }
                            else
                            {
                                processed = field.DefaultValue;
                            }
                        }
                        catch
                        {
                            // Keep processed value if regex is invalid
                        }
                    }

                    record[field.Name] = processed;
                }
            }

            return records;
        }

        /// <summary>
        /// Previews a single <see cref="ScrapeField"/> against the current page.
        /// </summary>
        public Task<ScrapeResult> PreviewFieldAsync(
            BrowserClient client,
            ScrapeField field,
            string? containerSelector = null,
            CancellationToken cancellationToken = default)
        {
            var tempDef = new ScrapeDefinition
            {
                Name = $"Preview: {field.Name}",
                ContainerSelector = containerSelector ?? string.Empty,
                WaitBeforeScrapeMs = 0,
                Fields = new List<ScrapeField> { field }
            };

            return RunScrapeAsync(client, tempDef, null, cancellationToken);
        }

        /// <summary>
        /// Extracts a single value from the first element matching <paramref name="selector"/>.
        /// </summary>
        public async Task<string?> ExtractValueAsync(
            BrowserClient client,
            string selector,
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText,
            string? attributeName = null,
            CancellationToken cancellationToken = default)
        {
            var field = new ScrapeField
            {
                Name = "Value",
                Selector = selector,
                ExtractionType = extractionType,
                AttributeName = attributeName ?? string.Empty,
                MatchAllElements = false
            };

            var res = await PreviewFieldAsync(client, field, null, cancellationToken);
            return res.GetFirstValue("Value");
        }

        /// <summary>
        /// Extracts a list of values from all elements matching <paramref name="selector"/>.
        /// </summary>
        public async Task<List<string>> ExtractListAsync(
            BrowserClient client,
            string selector,
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText,
            string? attributeName = null,
            CancellationToken cancellationToken = default)
        {
            var field = new ScrapeField
            {
                Name = "Item",
                Selector = selector,
                ExtractionType = extractionType,
                AttributeName = attributeName ?? string.Empty,
                MatchAllElements = true
            };

            var res = await PreviewFieldAsync(client, field, null, cancellationToken);
            return res.GetColumnValues("Item")
                .Where(v => v != null)
                .Select(v => v!)
                .ToList();
        }

        /// <summary>
        /// Automatically extracts an HTML &lt;table&gt; into a structured <see cref="ScrapeResult"/> using the table's headers as column names.
        /// </summary>
        public async Task<ScrapeResult> ExtractTableAsync(
            BrowserClient client,
            string tableSelector = "table",
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var cmdResult = await client.SendCommandAsync("SCRAPE_TABLE", new { selector = tableSelector }, cancellationToken);
            var records = ParseRecordsFromCommandOutput(cmdResult.Output);

            return new ScrapeResult
            {
                ScrapeName = $"Table ({tableSelector})",
                Success = cmdResult.Status == "completed",
                PagesScraped = 1,
                Records = records
            };
        }

        private async Task EnsurePageNotRateLimitedAsync(
            BrowserClient client,
            ScrapeDefinition definition,
            int pageNumber,
            string? targetPageUrl,
            CancellationToken cancellationToken)
        {
            const int maxRateLimitRetries = 10;
            int attempt = 0;

            while (client.LastStatusCode == 429 && attempt < maxRateLimitRetries)
            {
                attempt++;
                cancellationToken.ThrowIfCancellationRequested();

                string? retryHeader = client.LastRetryAfter;
                ParseRetryAfter(
                    retryHeader,
                    definition.PageDelayMs,
                    out DateTimeOffset retryAtUtc,
                    out TimeSpan waitDuration,
                    out bool isDateHeader,
                    out int? updatedPageDelayMs);

                if (updatedPageDelayMs.HasValue)
                {
                    definition.PageDelayMs = ScrapeDefinition.ClampPageDelayMs(updatedPageDelayMs.Value);
                }

                OnRateLimitDetected?.Invoke(this, new ScrapeRateLimitEventArgs(
                    definition,
                    pageNumber,
                    retryHeader,
                    retryAtUtc,
                    waitDuration,
                    isDateHeader,
                    definition.PageDelayMs));

                if (isDateHeader)
                {
                    while (DateTimeOffset.UtcNow < retryAtUtc)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var remaining = retryAtUtc - DateTimeOffset.UtcNow;
                        if (remaining <= TimeSpan.Zero)
                            break;

                        var slice = remaining > TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : remaining;
                        await Task.Delay(slice, cancellationToken);
                    }
                }
                else if (waitDuration > TimeSpan.Zero)
                {
                    await Task.Delay(waitDuration, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(targetPageUrl))
                {
                    await client.NavigateToUrlAsync(targetPageUrl, cancellationToken: cancellationToken);
                }
                else
                {
                    await client.ReloadPageAsync(cancellationToken: cancellationToken);
                }
            }
        }

        internal static void ParseRetryAfter(
            string? retryAfterHeader,
            int currentPageDelayMs,
            out DateTimeOffset retryAtUtc,
            out TimeSpan waitDuration,
            out bool isDateHeader,
            out int? updatedPageDelayMs)
        {
            if (!string.IsNullOrWhiteSpace(retryAfterHeader))
            {
                string trimmed = retryAfterHeader.Trim();

                // 1. Check if Retry-After is a number of seconds (or Unix epoch seconds)
                var secMatch = Regex.Match(trimmed, @"^(\d+(?:\.\d+)?)\s*(?:s|sec|seconds?)?$", RegexOptions.IgnoreCase);
                if (secMatch.Success && double.TryParse(secMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    if (seconds > 1_000_000_000)
                    {
                        // Unix epoch timestamp in seconds
                        isDateHeader = true;
                        retryAtUtc = DateTimeOffset.FromUnixTimeSeconds((long)seconds);
                        waitDuration = retryAtUtc - DateTimeOffset.UtcNow;
                        if (waitDuration < TimeSpan.FromSeconds(1))
                            waitDuration = TimeSpan.FromSeconds(1);
                        updatedPageDelayMs = null;
                        return;
                    }

                    isDateHeader = false;
                    int delayMs = Math.Max(1000, (int)Math.Ceiling(seconds * 1000.0));
                    waitDuration = TimeSpan.FromMilliseconds(delayMs);
                    retryAtUtc = DateTimeOffset.UtcNow.Add(waitDuration);
                    updatedPageDelayMs = delayMs;
                    return;
                }

                // 2. Check if Retry-After is an HTTP-date / timestamp
                if (DateTimeOffset.TryParse(
                        trimmed,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out DateTimeOffset parsedDate))
                {
                    isDateHeader = true;
                    retryAtUtc = parsedDate;
                    waitDuration = parsedDate - DateTimeOffset.UtcNow;
                    if (waitDuration < TimeSpan.FromSeconds(1))
                    {
                        waitDuration = TimeSpan.FromSeconds(1);
                        retryAtUtc = DateTimeOffset.UtcNow.Add(waitDuration);
                    }
                    updatedPageDelayMs = null;
                    return;
                }
            }

            // 3. Fallback when 429 has no Retry-After header
            isDateHeader = false;
            int fallbackDelayMs = Math.Max(currentPageDelayMs > 0 ? currentPageDelayMs * 2 : 5000, 5000);
            waitDuration = TimeSpan.FromMilliseconds(fallbackDelayMs);
            retryAtUtc = DateTimeOffset.UtcNow.Add(waitDuration);
            updatedPageDelayMs = fallbackDelayMs;
        }

        private static bool IsSamePageUrl(string? currentUrl, string targetUrl)
        {
            if (string.IsNullOrWhiteSpace(currentUrl) || string.IsNullOrWhiteSpace(targetUrl))
                return false;

            if (string.Equals(currentUrl.Trim(), targetUrl.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
            {
                if (Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
                {
                    return Uri.Compare(currentUri, targetUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
                }

                if (Uri.TryCreate(currentUri, targetUrl, out var resolvedUri))
                {
                    return Uri.Compare(currentUri, resolvedUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
                }
            }

            return false;
        }

        internal static List<Dictionary<string, string?>> ParseRecordsFromCommandOutput(string? output)
        {
            var records = new List<Dictionary<string, string?>>();
            if (string.IsNullOrWhiteSpace(output))
                return records;

            try
            {
                using var doc = JsonDocument.Parse(output, new JsonDocumentOptions { MaxDepth = 1024 });
                JsonElement recordsElement = doc.RootElement;

                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("records", out var prop) &&
                    prop.ValueKind == JsonValueKind.Array)
                {
                    recordsElement = prop;
                }

                if (recordsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in recordsElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, string?>();
                            foreach (var kvp in item.EnumerateObject())
                            {
                                dict[kvp.Name] = kvp.Value.ValueKind == JsonValueKind.Null
                                    ? null
                                    : kvp.Value.ToString();
                            }
                            records.Add(dict);
                        }
                    }
                }
            }
            catch
            {
                // Return empty list if output is not valid JSON
            }

            return records;
        }
    }
}