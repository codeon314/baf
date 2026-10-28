using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BrowserAutomationFramework.Scraping;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Defines a structured web scraping job, including optional container selectors, extraction fields,
    /// pre/post automation hooks, and multi-page pagination settings.
    /// </summary>
    public class ScrapeDefinition
    {
        /// <summary>
        /// Gets a shared default <see cref="ScrapeDefinition"/> instance used for UI fallback binding when no scrape is selected.
        /// </summary>
        [JsonIgnore]
        public static ScrapeDefinition Default { get; } = new();

        private int _maxPages = 1;
        private int _paginationStartPage = 1;
        private int _pageDelayMs = 1000;

        /// <summary>
        /// Gets the minimum valid value for <see cref="MaxPages"/>.
        /// </summary>
        public const int MinMaxPages = 1;

        /// <summary>
        /// Gets the maximum valid value for <see cref="MaxPages"/>.
        /// </summary>
        public const int MaxMaxPages = 10000;

        /// <summary>
        /// Gets the minimum valid value for <see cref="PaginationStartPage"/>.
        /// </summary>
        public const int MinPaginationStartPage = 1;

        /// <summary>
        /// Gets the maximum valid value for <see cref="PaginationStartPage"/>.
        /// </summary>
        public const int MaxPaginationStartPage = 10000;

        /// <summary>
        /// Gets the minimum valid value for <see cref="PageDelayMs"/>.
        /// </summary>
        public const int MinPageDelayMs = 0;

        /// <summary>
        /// Gets the maximum valid value for <see cref="PageDelayMs"/>.
        /// </summary>
        public const int MaxPageDelayMs = 3600000;

        /// <summary>
        /// Clamps <paramref name="value"/> to the valid <see cref="MaxPages"/> range.
        /// </summary>
        public static int ClampMaxPages(int value)
        {
            return Math.Clamp(value, MinMaxPages, MaxMaxPages);
        }

        /// <summary>
        /// Clamps <paramref name="value"/> to the valid <see cref="PaginationStartPage"/> range.
        /// </summary>
        public static int ClampPaginationStartPage(int value)
        {
            return Math.Clamp(value, MinPaginationStartPage, MaxPaginationStartPage);
        }

        /// <summary>
        /// Clamps <paramref name="value"/> to the valid <see cref="PageDelayMs"/> range.
        /// </summary>
        public static int ClampPageDelayMs(int value)
        {
            return Math.Clamp(value, MinPageDelayMs, MaxPageDelayMs);
        }

        /// <summary>
        /// Gets or sets the unique identifier for this scrape definition.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the display name of this scrape definition.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "New Scrape";

        /// <summary>
        /// Gets or sets whether this scrape definition is included when running all active project scrapes.
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the optional repeating container CSS selector (e.g., product card or table row).
        /// </summary>
        [JsonPropertyName("containerSelector")]
        public string ContainerSelector { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of <see cref="ScrapeField"/> columns to extract.
        /// </summary>
        [JsonPropertyName("fields")]
        public List<ScrapeField> Fields { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of an <see cref="AutomationSequence"/> to execute prior to scraping.
        /// </summary>
        [JsonPropertyName("preScrapeSequenceName")]
        public string PreScrapeSequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of an <see cref="AutomationSequence"/> to execute after scraping completes.
        /// </summary>
        [JsonPropertyName("postScrapeSequenceName")]
        public string PostScrapeSequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether multi-page pagination is enabled.
        /// </summary>
        [JsonPropertyName("enablePagination")]
        public bool EnablePagination { get; set; } = false;

        /// <summary>
        /// Gets or sets the CSS selector of the Next Page button/link to click between pages.
        /// </summary>
        [JsonPropertyName("nextPageSelector")]
        public string NextPageSelector { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional URL pattern (e.g., <c>https://site.com/items?page={page}</c>) for URL-based pagination.
        /// </summary>
        [JsonPropertyName("paginationUrlPattern")]
        public string PaginationUrlPattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the starting page number when using <see cref="PaginationUrlPattern"/>.
        /// </summary>
        [JsonPropertyName("paginationStartPage")]
        public int PaginationStartPage
        {
            get => _paginationStartPage;
            set => _paginationStartPage = ClampPaginationStartPage(value);
        }

        /// <summary>
        /// Gets or sets the page number increment between pages when using <see cref="PaginationUrlPattern"/>.
        /// </summary>
        [JsonPropertyName("paginationPageIncrement")]
        public int PaginationPageIncrement { get; set; } = 1;

        /// <summary>
        /// Gets or sets the name of an optional custom <see cref="AutomationSequence"/> used to advance pages.
        /// </summary>
        [JsonPropertyName("paginationSequenceName")]
        public string PaginationSequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of pages to scrape when <see cref="EnablePagination"/> is true.
        /// </summary>
        [JsonPropertyName("maxPages")]
        public int MaxPages
        {
            get => _maxPages;
            set => _maxPages = ClampMaxPages(value);
        }

        /// <summary>
        /// Gets or sets the initial wait time in milliseconds before scraping the first page.
        /// </summary>
        [JsonPropertyName("waitBeforeScrapeMs")]
        public int WaitBeforeScrapeMs { get; set; } = 300;

        /// <summary>
        /// Gets or sets the delay in milliseconds to wait before turning to each subsequent page.
        /// </summary>
        [JsonPropertyName("pageDelayMs")]
        public int PageDelayMs
        {
            get => _pageDelayMs;
            set => _pageDelayMs = ClampPageDelayMs(value);
        }

        /// <summary>
        /// Gets or sets whether duplicate rows should be filtered out across pages.
        /// </summary>
        [JsonPropertyName("deduplicateRows")]
        public bool DeduplicateRows { get; set; } = false;

        /// <summary>
        /// Gets whether this scrape definition has at least one <see cref="ScrapeField"/> configured.
        /// </summary>
        [JsonIgnore]
        public bool HasFields => Fields != null && Fields.Count > 0;

        /// <summary>
        /// Creates a new <see cref="ScrapeField"/> with a unique name within this definition and appends it to <see cref="Fields"/>.
        /// </summary>
        /// <param name="baseName">The base column name to assign.</param>
        /// <param name="selector">Optional initial CSS selector.</param>
        /// <param name="extractionType">The extraction type to use.</param>
        /// <returns>The newly created <see cref="ScrapeField"/>.</returns>
        public ScrapeField AddField(
            string baseName = "Field",
            string selector = "",
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText)
        {
            var field = new ScrapeField
            {
                Name = GetUniqueFieldName(baseName),
                Selector = selector ?? string.Empty,
                ExtractionType = extractionType
            };
            Fields.Add(field);
            return field;
        }

        /// <summary>
        /// Removes the specified <see cref="ScrapeField"/> from <see cref="Fields"/>.
        /// </summary>
        public bool RemoveField(ScrapeField? field)
        {
            if (field == null) return false;
            return Fields.Remove(field);
        }

        /// <summary>
        /// Updates the container, pre/post automation sequence hooks, and pagination settings on this definition.
        /// </summary>
        public void UpdateSettings(
            string? containerSelector,
            string? preSequenceName,
            string? postSequenceName,
            bool enablePagination,
            int maxPages,
            int startPage,
            int pageDelayMs,
            string? nextPageSelector,
            string? paginationUrlPattern)
        {
            ContainerSelector = containerSelector?.Trim() ?? string.Empty;
            PreScrapeSequenceName = NormalizeSequenceSelection(preSequenceName);
            PostScrapeSequenceName = NormalizeSequenceSelection(postSequenceName);
            EnablePagination = enablePagination;
            MaxPages = ClampMaxPages(maxPages);
            PaginationStartPage = ClampPaginationStartPage(startPage);
            PageDelayMs = ClampPageDelayMs(pageDelayMs);
            NextPageSelector = nextPageSelector?.Trim() ?? string.Empty;
            PaginationUrlPattern = paginationUrlPattern?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Moves the <see cref="ScrapeField"/> at <paramref name="index"/> one position up.
        /// </summary>
        public bool MoveFieldUp(int index)
        {
            if (index <= 0 || index >= Fields.Count) return false;
            var field = Fields[index];
            Fields.RemoveAt(index);
            Fields.Insert(index - 1, field);
            return true;
        }

        /// <summary>
        /// Moves the <see cref="ScrapeField"/> at <paramref name="index"/> one position down.
        /// </summary>
        public bool MoveFieldDown(int index)
        {
            if (index < 0 || index >= Fields.Count - 1) return false;
            var field = Fields[index];
            Fields.RemoveAt(index);
            Fields.Insert(index + 1, field);
            return true;
        }

        /// <summary>
        /// Sets the repeating container selector and automatically converts any existing child field selectors to be relative to the container.
        /// </summary>
        public void SetContainerAndRelativizeFields(string containerSelector)
        {
            ContainerSelector = containerSelector?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ContainerSelector))
                return;

            foreach (var field in Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.Selector) &&
                    SelectorHelper.IsInsideContainer(ContainerSelector, field.Selector))
                {
                    field.Selector = SelectorHelper.MakeRelativeSelector(ContainerSelector, field.Selector);
                }
            }
        }

        /// <summary>
        /// Generalizes <paramref name="rawContainerSelector"/> against <paramref name="rootDom"/> and relativizes existing fields.
        /// </summary>
        public string GeneralizeAndSetContainer(string rawContainerSelector, DomNode? rootDom = null)
        {
            string generalized = SelectorHelper.GeneralizeContainerSelector(rawContainerSelector, rootDom);
            SetContainerAndRelativizeFields(generalized);
            return generalized;
        }

        /// <summary>
        /// Sets the repeating container selector from a <see cref="DomNode"/> and relativizes existing child fields.
        /// </summary>
        public string SetContainerFromDomNode(DomNode domNode, DomNode? rootDom = null)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            string generalizedContainer = domNode.GetGeneralizedPath(rootDom);
            SetContainerAndRelativizeFields(generalizedContainer);
            return generalizedContainer;
        }

        /// <summary>
        /// Creates a <see cref="ScrapeField"/> from a <see cref="DomNode"/> with an automatically generated unique name and adds it to <see cref="Fields"/>.
        /// </summary>
        public ScrapeField AddFieldFromDomNode(
            DomNode domNode,
            bool matchAllElements = false,
            DomNode? rootDom = null,
            string? customFieldName = null,
            ScrapeExtractionType? extractionType = null)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));

            var resolvedType = extractionType ?? domNode.GetSuggestedExtractionType();
            string baseName = !string.IsNullOrWhiteSpace(customFieldName)
                ? customFieldName.Trim()
                : domNode.GetSuggestedFieldName(resolvedType);
            string uniqueName = GetUniqueFieldName(baseName);

            var field = domNode.CreateScrapeField(
                fieldName: uniqueName,
                extractionType: resolvedType,
                containerSelector: ContainerSelector,
                matchAllElements: matchAllElements,
                rootDom: rootDom);

            Fields.Add(field);
            return field;
        }

        /// <summary>
        /// Configures pagination to click the interactive element represented by <paramref name="domNode"/>.
        /// </summary>
        public string SetPaginationNextFromDomNode(DomNode domNode, DomNode? rootDom = null, int minimumMaxPages = 3)
        {
            if (domNode == null) throw new ArgumentNullException(nameof(domNode));
            string selector = domNode.GetBestInteractiveSelector(rootDom);
            EnablePagination = true;
            NextPageSelector = selector;
            if (MaxPages < 2 && minimumMaxPages >= 2)
            {
                MaxPages = minimumMaxPages;
            }
            return selector;
        }

        /// <summary>
        /// Generates a unique field name within this definition based on a base name.
        /// </summary>
        public string GetUniqueFieldName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "Field";

            string candidate = baseName.Trim();
            int counter = 2;
            while (Fields.Exists(f => string.Equals(f.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{baseName.Trim()}_{counter}";
                counter++;
            }
            return candidate;
        }

        /// <summary>
        /// Normalizes a UI dropdown selection string (such as <c>"(None)"</c>) into a sequence name suitable for <see cref="PreScrapeSequenceName"/> or <see cref="PostScrapeSequenceName"/>.
        /// </summary>
        public static string NormalizeSequenceSelection(string? selectedSequenceName)
        {
            if (string.IsNullOrWhiteSpace(selectedSequenceName) ||
                string.Equals(selectedSequenceName.Trim(), "(None)", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            return selectedSequenceName.Trim();
        }

        /// <summary>
        /// Returns the display name of this scrape definition.
        /// </summary>
        public override string ToString() => Name;
    }
}