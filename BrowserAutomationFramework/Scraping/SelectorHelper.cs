using System;
using System.Linq;
using System.Text.RegularExpressions;
using BrowserAutomationFramework.Models;

namespace BrowserAutomationFramework.Scraping
{
    /// <summary>
    /// Provides static utility methods for generalizing CSS selectors, relativizing child selectors against containers,
    /// and resolving pagination URL templates.
    /// </summary>
    public static class SelectorHelper
    {
        private static readonly Regex NthChildRegex = new(@":nth-child\(\d+\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PageQueryParamRegex = new(@"([?&](?:page|p|pg)=)(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Resolves the target URL for a given page number using a URL template (e.g., "https://site.com/list?page={page}" or "https://site.com/list?page=1").
        /// </summary>
        public static string ResolvePaginationUrl(string urlPattern, int pageNumber)
        {
            if (string.IsNullOrWhiteSpace(urlPattern))
                return string.Empty;

            string pattern = urlPattern.Trim();

            if (pattern.Contains("{{page}}", StringComparison.OrdinalIgnoreCase))
            {
                return pattern.Replace("{{page}}", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.Contains("{page}", StringComparison.OrdinalIgnoreCase))
            {
                return pattern.Replace("{page}", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.Contains("{0}", StringComparison.Ordinal))
            {
                return pattern.Replace("{0}", pageNumber.ToString(), StringComparison.Ordinal);
            }

            if (PageQueryParamRegex.IsMatch(pattern))
            {
                return PageQueryParamRegex.Replace(pattern, $"$1{pageNumber}", 1);
            }

            if (pattern.EndsWith("=", StringComparison.Ordinal))
            {
                return pattern + pageNumber;
            }

            char separator = pattern.Contains('?') ? '&' : '?';
            return $"{pattern}{separator}page={pageNumber}";
        }

        /// <summary>
        /// Removes :nth-child(N) from a single selector segment.
        /// </summary>
        public static string StripNthChild(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return string.Empty;

            return NthChildRegex.Replace(segment, string.Empty).Trim();
        }

        /// <summary>
        /// Generalizes a CSS path by removing :nth-child(N) from the final element segment so it matches all sibling elements of the same tag.
        /// </summary>
        public static string GeneralizeSelector(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return string.Empty;

            var parts = selector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 0)
                return selector.Trim();

            parts[^1] = StripNthChild(parts[^1]);
            return string.Join(" > ", parts);
        }

        /// <summary>
        /// Generalizes a container selector, including any higher-level repeating structural wrappers (such as multi-column card tables).
        /// </summary>
        public static string GeneralizeContainerSelector(string selector, DomNode? rootDom = null)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return string.Empty;

            var parts = selector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 0)
                return selector.Trim();

            parts[^1] = StripNthChild(parts[^1]);

            if (rootDom != null)
            {
                for (int i = parts.Length - 2; i >= 1; i--)
                {
                    string childTag = StripNthChild(parts[i]);
                    if (string.Equals(childTag, "td", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(childTag, "th", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string ancestorPath = string.Join(" > ", selector.Split('>', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Take(i));
                    var ancestorNode = rootDom.FindByPath(ancestorPath);
                    if (ancestorNode?.Children != null)
                    {
                        var remainingSuffix = parts.Skip(i + 1).ToArray();
                        int matchingSiblings = ancestorNode.Children.Count(c =>
                            c.IsElement &&
                            string.Equals(c.TagName, childTag, StringComparison.OrdinalIgnoreCase) &&
                            HasMatchingSubpath(c, remainingSuffix));

                        if (matchingSiblings > 1)
                        {
                            parts[i] = childTag;
                        }
                    }
                }
            }

            return string.Join(" > ", parts);
        }

        /// <summary>
        /// Generalizes a repeating field selector by finding the repeating row/container ancestor(s) or leaf segment in the DOM tree.
        /// </summary>
        public static string GeneralizeRepeatingFieldSelector(string selector, DomNode? rootDom = null)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return string.Empty;

            var parts = selector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 0)
                return selector;

            if (rootDom != null)
            {
                var generalizedParts = parts.ToArray();
                bool generalizedAnyAncestor = false;

                for (int i = parts.Length - 2; i >= 1; i--)
                {
                    string childTag = StripNthChild(parts[i]);
                    if (string.Equals(childTag, "td", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(childTag, "th", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string ancestorPath = string.Join(" > ", parts.Take(i));
                    var ancestorNode = rootDom.FindByPath(ancestorPath);
                    if (ancestorNode?.Children != null)
                    {
                        var remainingSuffix = generalizedParts.Skip(i + 1).ToArray();
                        int matchingSiblings = ancestorNode.Children.Count(c =>
                            c.IsElement &&
                            string.Equals(c.TagName, childTag, StringComparison.OrdinalIgnoreCase) &&
                            HasMatchingSubpath(c, remainingSuffix));

                        if (matchingSiblings > 1)
                        {
                            generalizedParts[i] = childTag;
                            generalizedAnyAncestor = true;
                        }
                    }
                }

                if (generalizedAnyAncestor)
                {
                    return string.Join(" > ", generalizedParts);
                }
            }

            string lastTag = StripNthChild(parts[^1]);
            if (parts.Length >= 2 &&
                (string.Equals(lastTag, "td", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(lastTag, "th", StringComparison.OrdinalIgnoreCase)))
            {
                parts[^2] = StripNthChild(parts[^2]);
                return string.Join(" > ", parts);
            }

            return GeneralizeSelector(selector);
        }

        private static bool HasMatchingSubpath(DomNode node, string[] subpathParts)
        {
            if (subpathParts.Length == 0)
                return true;

            if (node.Children == null || node.Children.Count == 0)
                return false;

            string currentSeg = subpathParts[0];
            string targetTag = StripNthChild(currentSeg);
            int? nthIndex = TryParseNthChild(currentSeg);

            var elementChildren = node.Children.Where(c => c.IsElement).ToList();
            if (nthIndex.HasValue)
            {
                int idx = nthIndex.Value - 1;
                if (idx < 0 || idx >= elementChildren.Count)
                    return false;

                var child = elementChildren[idx];
                if (!string.Equals(child.TagName, targetTag, StringComparison.OrdinalIgnoreCase))
                    return false;

                return HasMatchingSubpath(child, subpathParts.Skip(1).ToArray());
            }

            return elementChildren.Any(c =>
                string.Equals(c.TagName, targetTag, StringComparison.OrdinalIgnoreCase) &&
                HasMatchingSubpath(c, subpathParts.Skip(1).ToArray()));
        }

        private static int? TryParseNthChild(string segment)
        {
            var match = Regex.Match(segment, @":nth-child\((\d+)\)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
                return n;
            return null;
        }

        /// <summary>
        /// Determines whether <paramref name="fieldSelector"/> is located inside (or equals) <paramref name="containerSelector"/>.
        /// </summary>
        public static bool IsInsideContainer(string containerSelector, string fieldSelector)
        {
            if (string.IsNullOrWhiteSpace(containerSelector) || string.IsNullOrWhiteSpace(fieldSelector))
                return false;

            var cParts = containerSelector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var fParts = fieldSelector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (fParts.Length < cParts.Length)
                return false;

            for (int i = 0; i < cParts.Length; i++)
            {
                if (!string.Equals(StripNthChild(cParts[i]), StripNthChild(fParts[i]), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a full DOM path into a selector relative to <paramref name="containerSelector"/> if it resides inside the container.
        /// </summary>
        public static string MakeRelativeSelector(string containerSelector, string fieldSelector)
        {
            if (string.IsNullOrWhiteSpace(fieldSelector))
                return ":scope";

            if (string.IsNullOrWhiteSpace(containerSelector))
                return fieldSelector.Trim();

            if (!IsInsideContainer(containerSelector, fieldSelector))
                return fieldSelector.Trim();

            var cParts = containerSelector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var fParts = fieldSelector.Split('>', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (fParts.Length == cParts.Length)
                return ":scope";

            return string.Join(" > ", fParts.Skip(cParts.Length));
        }
    }
}