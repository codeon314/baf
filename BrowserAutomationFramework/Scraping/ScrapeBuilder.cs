using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

namespace BrowserAutomationFramework.Scraping
{
    /// <summary>
    /// Fluent builder for programmatically constructing and executing <see cref="ScrapeDefinition"/> workflows.
    /// </summary>
    public class ScrapeBuilder
    {
        private readonly BrowserClient? _client;
        private readonly ScrapeDefinition _definition;
        private readonly List<AutomationSequence> _sequences = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrapeBuilder"/> class.
        /// </summary>
        /// <param name="name">The display name for the built <see cref="ScrapeDefinition"/>.</param>
        /// <param name="client">An optional <see cref="BrowserClient"/> bound to this builder for direct execution.</param>
        public ScrapeBuilder(string name = "Scrape", BrowserClient? client = null)
        {
            _client = client;
            _definition = new ScrapeDefinition { Name = name };
        }

        /// <summary>
        /// Sets the CSS selector for repeating item containers (e.g., product cards or table rows).
        /// </summary>
        public ScrapeBuilder WithContainer(string containerSelector)
        {
            _definition.ContainerSelector = containerSelector;
            return this;
        }

        /// <summary>
        /// Adds a field to extract from each container or from the page.
        /// </summary>
        public ScrapeBuilder Field(
            string name,
            string selector,
            ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText,
            string attributeName = "",
            bool matchAllElements = false,
            string defaultValue = "",
            string regexPattern = "")
        {
            _definition.Fields.Add(new ScrapeField
            {
                Name = name,
                Selector = selector,
                ExtractionType = extractionType,
                AttributeName = attributeName,
                MatchAllElements = matchAllElements,
                DefaultValue = defaultValue,
                RegexPattern = regexPattern
            });
            return this;
        }

        /// <summary>
        /// Configures an <see cref="AutomationSequence"/> to run prior to scraping.
        /// </summary>
        public ScrapeBuilder WithPreScrapeAutomation(AutomationSequence sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            _definition.PreScrapeSequenceName = sequence.Name;
            _sequences.Add(sequence);
            return this;
        }

        /// <summary>
        /// Configures an <see cref="AutomationSequence"/> to run after scraping finishes.
        /// </summary>
        public ScrapeBuilder WithPostScrapeAutomation(AutomationSequence sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            _definition.PostScrapeSequenceName = sequence.Name;
            _sequences.Add(sequence);
            return this;
        }

        /// <summary>
        /// Sets the delay in milliseconds to wait before turning to each subsequent page during pagination.
        /// </summary>
        public ScrapeBuilder WithPageDelay(int pageDelayMs)
        {
            _definition.PageDelayMs = ScrapeDefinition.ClampPageDelayMs(pageDelayMs);
            return this;
        }

        /// <summary>
        /// Enables multi-page scraping by clicking a "Next Page" selector between pages.
        /// </summary>
        public ScrapeBuilder WithPagination(string nextPageSelector, int maxPages = 5, int pageDelayMs = 1000, bool deduplicateRows = false)
        {
            _definition.EnablePagination = true;
            _definition.NextPageSelector = nextPageSelector;
            _definition.MaxPages = ScrapeDefinition.ClampMaxPages(maxPages);
            _definition.PageDelayMs = ScrapeDefinition.ClampPageDelayMs(pageDelayMs);
            _definition.DeduplicateRows = deduplicateRows;
            return this;
        }

        /// <summary>
        /// Enables multi-page scraping by navigating directly to a URL pattern for each page (e.g., "https://example.com/items?page={page}").
        /// </summary>
        public ScrapeBuilder WithUrlPagination(string urlPattern, int maxPages = 5, int startPage = 1, int pageDelayMs = 1000, bool deduplicateRows = false)
        {
            _definition.EnablePagination = true;
            _definition.PaginationUrlPattern = urlPattern;
            _definition.PaginationStartPage = ScrapeDefinition.ClampPaginationStartPage(startPage);
            _definition.MaxPages = ScrapeDefinition.ClampMaxPages(maxPages);
            _definition.PageDelayMs = ScrapeDefinition.ClampPageDelayMs(pageDelayMs);
            _definition.DeduplicateRows = deduplicateRows;
            return this;
        }

        /// <summary>
        /// Enables multi-page scraping using a custom <see cref="AutomationSequence"/> to advance pages.
        /// </summary>
        public ScrapeBuilder WithPaginationSequence(AutomationSequence paginationSequence, int maxPages = 5, int pageDelayMs = 1000, bool deduplicateRows = false)
        {
            if (paginationSequence == null) throw new ArgumentNullException(nameof(paginationSequence));
            _definition.EnablePagination = true;
            _definition.PaginationSequenceName = paginationSequence.Name;
            _definition.MaxPages = ScrapeDefinition.ClampMaxPages(maxPages);
            _definition.PageDelayMs = ScrapeDefinition.ClampPageDelayMs(pageDelayMs);
            _definition.DeduplicateRows = deduplicateRows;
            _sequences.Add(paginationSequence);
            return this;
        }

        /// <summary>
        /// Returns the built <see cref="ScrapeDefinition"/>.
        /// </summary>
        public ScrapeDefinition Build() => _definition;

        /// <summary>
        /// Executes the built scrape definition against the associated or provided <see cref="BrowserClient"/>.
        /// </summary>
        public Task<ScrapeResult> ExecuteAsync(
            BrowserClient? client = null,
            DataScraper? scraper = null,
            CancellationToken cancellationToken = default)
        {
            var targetClient = client ?? _client ?? throw new InvalidOperationException("A BrowserClient must be provided to execute the scrape.");
            var activeScraper = scraper ?? new DataScraper();
            return activeScraper.RunScrapeAsync(targetClient, _definition, _sequences, cancellationToken);
        }
    }
}