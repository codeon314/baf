using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using BrowserAutomationFramework.Scraping;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents a serialized DOM element or text node from a connected browser page.
    /// </summary>
    public class DomNode
    {
        /// <summary>
        /// Gets or sets the node type (<c>element</c> or <c>text</c>).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets whether this node represents an HTML element.
        /// </summary>
        [JsonIgnore]
        public bool IsElement => string.Equals(Type, "element", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets whether this node represents a text node.
        /// </summary>
        [JsonIgnore]
        public bool IsText => string.Equals(Type, "text", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the display label for this node, preferring the tag name and falling back to its text content.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => TagName ?? Text ?? string.Empty;

        /// <summary>
        /// Gets whether this node has a non-empty CSS selector path.
        /// </summary>
        [JsonIgnore]
        public bool HasPath => !string.IsNullOrEmpty(Path);

        /// <summary>
        /// Gets or sets the lowercase HTML tag name when <see cref="Type"/> is <c>element</c>.
        /// </summary>
        [JsonPropertyName("tagName")]
        public string? TagName { get; set; }

        /// <summary>
        /// Gets or sets the unique CSS selector path to this element in the document.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the trimmed text content when <see cref="Type"/> is <c>text</c>.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of HTML attributes on this element.
        /// </summary>
        [JsonPropertyName("attributes")]
        public Dictionary<string, string>? Attributes { get; set; }

        /// <summary>
        /// Gets or sets the child DOM nodes of this element.
        /// </summary>
        [JsonPropertyName("children")]
        public List<DomNode>? Children { get; set; }

        /// <summary>
        /// Builds a pre-indexed, syntax-tokenized <see cref="DomTreeSnapshot"/> from this DOM node subtree.
        /// </summary>
        public DomTreeSnapshot CreateSnapshot()
        {
            return DomTreeSnapshot.Build(this);
        }

        /// <summary>
        /// Searches this DOM subtree for a node matching the specified CSS path using fast prefix pruning.
        /// </summary>
        public DomNode? FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (string.Equals(Path, path, StringComparison.Ordinal)) return this;

            if (Children != null && Children.Count > 0)
            {
                // Fast O(depth) path walk when child paths are prefixes of the target path
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (!child.IsElement || string.IsNullOrEmpty(child.Path))
                        continue;

                    if (string.Equals(child.Path, path, StringComparison.Ordinal))
                        return child;

                    if (path.Length > child.Path.Length &&
                        path.StartsWith(child.Path, StringComparison.Ordinal) &&
                        path[child.Path.Length] == ' ')
                    {
                        var foundOnPrefix = child.FindByPath(path);
                        if (foundOnPrefix != null)
                            return foundOnPrefix;
                    }
                }

                // Fallback scan for non-prefixed roots (such as "html" -> "body")
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (!child.IsElement)
                        continue;

                    if (!string.IsNullOrEmpty(child.Path) &&
                        child.Path != "html" &&
                        !path.StartsWith(child.Path, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var found = child.FindByPath(path);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively returns all nodes in this DOM subtree matching the given predicate.
        /// </summary>
        public IEnumerable<DomNode> QueryAll(Func<DomNode, bool> predicate)
        {
            if (predicate(this))
            {
                yield return this;
            }

            if (Children != null)
            {
                foreach (var child in Children)
                {
                    foreach (var match in child.QueryAll(predicate))
                    {
                        yield return match;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a formatted multi-line summary of the DOM node's properties and attributes.
        /// </summary>
        public string GetFormattedDetails()
        {
            var details = new StringBuilder();
            details.AppendLine($"Type: {Type}");
            if (IsElement)
            {
                details.AppendLine($"Tag: {TagName}");
                details.AppendLine($"Path: {Path}");
                details.AppendLine("Attributes:");
                if (Attributes != null)
                {
                    foreach (var kvp in Attributes)
                    {
                        details.AppendLine($"  {kvp.Key} = \"{kvp.Value}\"");
                    }
                }
            }
            else if (IsText)
            {
                details.AppendLine($"Text: {Text}");
            }

            return details.ToString();
        }

        /// <summary>
        /// Creates an <see cref="AutomationStep"/> targeting this DOM node.
        /// </summary>
        public AutomationStep CreateStep(AutomationActionType actionType, string value = "", bool overwrite = true)
        {
            string actionLabel = actionType switch
            {
                AutomationActionType.Click => "Click",
                AutomationActionType.SetValue => "Set Value",
                AutomationActionType.EvaluateScript => "Evaluate Script",
                _ => actionType.ToString()
            };

            return new AutomationStep
            {
                Name = $"{actionLabel} <{TagName}>",
                ActionType = actionType,
                Selector = Path ?? string.Empty,
                Value = value,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// Returns a generalized CSS selector for this node by stripping :nth-child(N) from repeating segments.
        /// </summary>
        public string GetGeneralizedPath(DomNode? rootDom = null)
        {
            return SelectorHelper.GeneralizeContainerSelector(Path ?? string.Empty, rootDom);
        }

        /// <summary>
        /// Returns a resilient CSS selector for clicking this interactive element (such as a pagination Next button),
        /// resolving child spans or wrapper list items to their anchor/button element when applicable.
        /// </summary>
        public string GetBestInteractiveSelector(DomNode? rootDom = null)
        {
            DomNode target = this;

            if (Children != null &&
                !string.Equals(TagName, "a", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(TagName, "button", StringComparison.OrdinalIgnoreCase))
            {
                var childLinkOrBtn = QueryAll(n =>
                    n.IsElement &&
                    (string.Equals(n.TagName, "a", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(n.TagName, "button", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();

                if (childLinkOrBtn != null)
                {
                    target = childLinkOrBtn;
                }
            }

            if (rootDom != null &&
                !string.IsNullOrEmpty(target.Path) &&
                !string.Equals(target.TagName, "a", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target.TagName, "button", StringComparison.OrdinalIgnoreCase))
            {
                var parts = target.Path.Split('>', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                for (int i = parts.Length - 1; i >= 1; i--)
                {
                    string ancestorPath = string.Join(" > ", parts.Take(i));
                    var ancestor = rootDom.FindByPath(ancestorPath);
                    if (ancestor != null &&
                        (string.Equals(ancestor.TagName, "a", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ancestor.TagName, "button", StringComparison.OrdinalIgnoreCase)))
                    {
                        target = ancestor;
                        break;
                    }
                }
            }

            if (target.Attributes != null)
            {
                string tag = (target.TagName ?? "a").ToLowerInvariant();
                if (target.Attributes.TryGetValue("id", out var idVal) && !string.IsNullOrWhiteSpace(idVal))
                {
                    return $"#{idVal.Trim()}";
                }
                if (target.Attributes.TryGetValue("aria-label", out var ariaVal) && !string.IsNullOrWhiteSpace(ariaVal))
                {
                    return $"{tag}[aria-label=\"{ariaVal.Replace("\"", "\\\"")}\"]";
                }
                if (target.Attributes.TryGetValue("rel", out var relVal) && string.Equals(relVal, "next", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{tag}[rel=\"next\"]";
                }
                if (target.Attributes.TryGetValue("title", out var titleVal) && !string.IsNullOrWhiteSpace(titleVal))
                {
                    return $"{tag}[title=\"{titleVal.Replace("\"", "\\\"")}\"]";
                }
            }

            return target.Path ?? Path ?? string.Empty;
        }

        /// <summary>
        /// Determines the most appropriate default <see cref="ScrapeExtractionType"/> for this DOM element.
        /// </summary>
        public ScrapeExtractionType GetSuggestedExtractionType()
        {
            string tag = (TagName ?? string.Empty).ToLowerInvariant();
            return tag switch
            {
                "img" or "video" or "audio" or "source" or "iframe" => ScrapeExtractionType.Src,
                "input" or "textarea" or "select" => ScrapeExtractionType.Value,
                _ => ScrapeExtractionType.InnerText
            };
        }

        /// <summary>
        /// Generates a clean, human-readable field name based on this element's attributes or tag name.
        /// </summary>
        public string GetSuggestedFieldName(ScrapeExtractionType? extractionType = null)
        {
            var ext = extractionType ?? GetSuggestedExtractionType();

            if (Attributes != null)
            {
                string[] candidateAttrs = { "id", "name", "data-testid", "aria-label", "placeholder", "title", "alt" };
                foreach (var key in candidateAttrs)
                {
                    if (Attributes.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) && val.Length <= 30)
                    {
                        return SanitizeFieldName(val);
                    }
                }

                if (Attributes.TryGetValue("class", out var cls) && !string.IsNullOrWhiteSpace(cls))
                {
                    var firstClass = cls.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstClass) && firstClass.Length <= 28)
                    {
                        return SanitizeFieldName(firstClass);
                    }
                }
            }

            string tag = (TagName ?? "Element").ToLowerInvariant();
            if (ext == ScrapeExtractionType.Href) return "LinkUrl";
            if (ext == ScrapeExtractionType.Src) return "MediaUrl";

            return tag switch
            {
                "a" => "LinkText",
                "h1" or "h2" or "h3" or "h4" => "Heading",
                "p" => "Paragraph",
                "li" => "ListItem",
                "td" or "th" => "Cell",
                "button" => "Button",
                "input" => "InputValue",
                _ => char.ToUpperInvariant(tag[0]) + tag.Substring(1)
            };
        }

        private static string SanitizeFieldName(string raw)
        {
            var sb = new StringBuilder();
            bool capitalizeNext = true;
            foreach (char c in raw.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }
            return sb.Length > 0 ? sb.ToString() : "Field";
        }

        /// <summary>
        /// Creates a <see cref="ScrapeField"/> configured for this DOM node.
        /// </summary>
        public ScrapeField CreateScrapeField(
            string? fieldName = null,
            ScrapeExtractionType? extractionType = null,
            string? attributeName = null,
            string? containerSelector = null,
            bool matchAllElements = false,
            DomNode? rootDom = null)
        {
            var resolvedType = extractionType ?? GetSuggestedExtractionType();
            string rawPath = Path ?? string.Empty;

            string selector;
            if (!string.IsNullOrWhiteSpace(containerSelector) && SelectorHelper.IsInsideContainer(containerSelector, rawPath))
            {
                selector = SelectorHelper.MakeRelativeSelector(containerSelector, rawPath);
            }
            else if (matchAllElements)
            {
                selector = SelectorHelper.GeneralizeRepeatingFieldSelector(rawPath, rootDom);
            }
            else
            {
                selector = rawPath;
            }

            return new ScrapeField
            {
                Name = !string.IsNullOrWhiteSpace(fieldName) ? fieldName : GetSuggestedFieldName(resolvedType),
                Selector = selector,
                ExtractionType = resolvedType,
                AttributeName = attributeName ?? string.Empty,
                MatchAllElements = matchAllElements
            };
        }

        /// <summary>
        /// Returns the display label for this node.
        /// </summary>
        public override string ToString() => DisplayName;
    }
}