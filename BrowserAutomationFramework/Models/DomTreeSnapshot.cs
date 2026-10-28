using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Classifies syntax tokens for rendering a <see cref="DomNode"/> in a developer tools tree view.
    /// </summary>
    public enum DomTokenColor : byte
    {
        /// <summary>
        /// Markup punctuation such as angle brackets, equals signs, and ellipsis tokens.
        /// </summary>
        Punctuation,

        /// <summary>
        /// HTML element tag name token.
        /// </summary>
        TagName,

        /// <summary>
        /// HTML attribute name token.
        /// </summary>
        AttributeName,

        /// <summary>
        /// Quoted HTML attribute value token.
        /// </summary>
        AttributeValue,

        /// <summary>
        /// Text node content token.
        /// </summary>
        TextContent
    }

    /// <summary>
    /// Provides default dark-theme RGB syntax highlighting and tree chrome colors for <see cref="DomTokenColor"/> values and DOM tree views.
    /// </summary>
    public static class DomTokenColorExtensions
    {
        /// <summary>
        /// Gets the default dark-mode RGB color tuple for selected tree rows.
        /// </summary>
        public static (byte R, byte G, byte B) SelectionBackgroundRgb => (9, 71, 113);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for hovered tree rows.
        /// </summary>
        public static (byte R, byte G, byte B) HoverBackgroundRgb => (40, 42, 46);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for the tree control border.
        /// </summary>
        public static (byte R, byte G, byte B) BorderRgb => (63, 63, 70);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for expand/collapse glyph box backgrounds.
        /// </summary>
        public static (byte R, byte G, byte B) GlyphFillRgb => (37, 37, 38);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for hierarchy guide lines.
        /// </summary>
        public static (byte R, byte G, byte B) GuideLineRgb => (58, 58, 62);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for expand/collapse glyph box borders.
        /// </summary>
        public static (byte R, byte G, byte B) GlyphBorderRgb => (130, 130, 135);

        /// <summary>
        /// Gets the default dark-mode RGB color tuple for expand/collapse plus/minus sign lines.
        /// </summary>
        public static (byte R, byte G, byte B) GlyphSignRgb => DomTokenColor.TextContent.GetDefaultRgb();

        /// <summary>
        /// Returns the default dark-mode RGB color tuple <c>(R, G, B)</c> for the specified <see cref="DomTokenColor"/>.
        /// </summary>
        public static (byte R, byte G, byte B) GetDefaultRgb(this DomTokenColor tokenColor)
        {
            return tokenColor switch
            {
                DomTokenColor.TagName => (86, 156, 214),
                DomTokenColor.AttributeName => (156, 220, 254),
                DomTokenColor.AttributeValue => (206, 145, 120),
                DomTokenColor.TextContent => (212, 212, 212),
                _ => (128, 128, 128)
            };
        }
    }

    /// <summary>
    /// Specifies a platform-independent keyboard navigation command for a <see cref="DomTreeViewportState"/>.
    /// </summary>
    public enum DomTreeNavigationKey
    {
        /// <summary>
        /// Moves selection one row up.
        /// </summary>
        Up,

        /// <summary>
        /// Moves selection one row down.
        /// </summary>
        Down,

        /// <summary>
        /// Moves selection one viewport page up.
        /// </summary>
        PageUp,

        /// <summary>
        /// Moves selection one viewport page down.
        /// </summary>
        PageDown,

        /// <summary>
        /// Moves selection to the first visible row.
        /// </summary>
        Home,

        /// <summary>
        /// Moves selection to the last visible row.
        /// </summary>
        End,

        /// <summary>
        /// Collapses an expanded node or moves selection to the parent node.
        /// </summary>
        Left,

        /// <summary>
        /// Expands a collapsed parent node or moves selection to its first child.
        /// </summary>
        Right
    }

    /// <summary>
    /// Represents a horizontally culled, positioned syntax token ready to be drawn by a UI renderer.
    /// </summary>
    public readonly struct DomTokenRenderSlice
    {
        /// <summary>
        /// Gets the token text to draw.
        /// </summary>
        public string Text { get; init; }

        /// <summary>
        /// Gets the syntax color classification of the token.
        /// </summary>
        public DomTokenColor ColorType { get; init; }

        /// <summary>
        /// Gets the horizontal pixel coordinate where the token should be drawn.
        /// </summary>
        public int X { get; init; }

        /// <summary>
        /// Gets the vertical pixel coordinate where the token should be drawn.
        /// </summary>
        public int Y { get; init; }
    }

    /// <summary>
    /// Contains precomputed pixel geometry for rendering a single visible <see cref="DomTreeItem"/> row.
    /// </summary>
    public readonly struct DomRowRenderLayout
    {
        /// <summary>
        /// Gets the top vertical coordinate of the row.
        /// </summary>
        public int RowY { get; init; }

        /// <summary>
        /// Gets the vertical center coordinate of the row.
        /// </summary>
        public int CenterY { get; init; }

        /// <summary>
        /// Gets the horizontal start coordinate of the node's indentation column.
        /// </summary>
        public int IndentX { get; init; }

        /// <summary>
        /// Gets the horizontal start coordinate where the node's text tokens begin.
        /// </summary>
        public int TextStartX { get; init; }

        /// <summary>
        /// Gets the vertical coordinate where the node's text tokens should be drawn.
        /// </summary>
        public int TextY { get; init; }

        /// <summary>
        /// Gets the total measured pixel width of the row's visible text tokens.
        /// </summary>
        public int TextTotalWidth { get; init; }

        /// <summary>
        /// Gets whether this node has children and is currently expanded.
        /// </summary>
        public bool IsExpanded { get; init; }

        /// <summary>
        /// Gets whether this row is currently selected.
        /// </summary>
        public bool IsSelected { get; init; }

        /// <summary>
        /// Gets whether this row is currently hovered and not selected.
        /// </summary>
        public bool IsHovered { get; init; }

        /// <summary>
        /// Gets the left coordinate of the selection/hover highlight rectangle.
        /// </summary>
        public int HighlightLeft { get; init; }

        /// <summary>
        /// Gets the top coordinate of the selection/hover highlight rectangle.
        /// </summary>
        public int HighlightTop { get; init; }

        /// <summary>
        /// Gets the width of the selection/hover highlight rectangle.
        /// </summary>
        public int HighlightWidth { get; init; }

        /// <summary>
        /// Gets the height of the selection/hover highlight rectangle.
        /// </summary>
        public int HighlightHeight { get; init; }

        /// <summary>
        /// Gets whether the L-shaped branch connector line for this node should be drawn.
        /// </summary>
        public bool HasBranchConnector { get; init; }

        /// <summary>
        /// Gets the horizontal center coordinate of the branch connector line.
        /// </summary>
        public int BranchLineCenterX { get; init; }

        /// <summary>
        /// Gets the right end coordinate of the horizontal branch connector segment.
        /// </summary>
        public int BranchHorizontalEndX { get; init; }

        /// <summary>
        /// Gets whether an expand/collapse plus-minus box should be drawn for this row.
        /// </summary>
        public bool ShowExpandGlyph { get; init; }

        /// <summary>
        /// Gets the left coordinate of the expand/collapse glyph box.
        /// </summary>
        public int GlyphBoxLeft { get; init; }

        /// <summary>
        /// Gets the top coordinate of the expand/collapse glyph box.
        /// </summary>
        public int GlyphBoxTop { get; init; }

        /// <summary>
        /// Gets the width and height of the expand/collapse glyph box border rectangle.
        /// </summary>
        public int GlyphBoxDrawSize { get; init; }

        /// <summary>
        /// Gets the left coordinate of the horizontal minus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignLeft { get; init; }

        /// <summary>
        /// Gets the right coordinate of the horizontal minus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignRight { get; init; }

        /// <summary>
        /// Gets the horizontal center coordinate of the vertical plus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignMidX { get; init; }

        /// <summary>
        /// Gets the vertical center coordinate of the horizontal minus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignMidY { get; init; }

        /// <summary>
        /// Gets the top coordinate of the vertical plus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignTop { get; init; }

        /// <summary>
        /// Gets the bottom coordinate of the vertical plus sign line inside the glyph box.
        /// </summary>
        public int GlyphSignBottom { get; init; }
    }

    /// <summary>
    /// Caches measured pixel widths for the syntax-highlighted display tokens of a <see cref="DomTreeItem"/> row.
    /// </summary>
    public sealed class DomRowMeasureCache
    {
        /// <summary>
        /// Gets or sets the font generation counter at which these widths were measured.
        /// </summary>
        public int MeasuredFontGeneration { get; set; }

        /// <summary>
        /// Gets or sets the measured pixel widths for each token in <see cref="DomTreeItem.OpenParts"/>.
        /// </summary>
        public int[] OpenPartWidths { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Gets or sets the measured pixel widths for each token in <see cref="DomTreeItem.CollapsedSuffixParts"/>.
        /// </summary>
        public int[] CollapsedSuffixWidths { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Gets or sets the sum of all widths in <see cref="OpenPartWidths"/>.
        /// </summary>
        public int OpenTotalWidth { get; set; }

        /// <summary>
        /// Gets or sets the sum of all widths in <see cref="CollapsedSuffixWidths"/>.
        /// </summary>
        public int CollapsedSuffixTotalWidth { get; set; }
    }

    /// <summary>
    /// Represents a platform-independent integer rectangle for layout and rendering bounds.
    /// </summary>
    public readonly struct DomRect
    {
        /// <summary>
        /// Gets the left X coordinate.
        /// </summary>
        public int X { get; init; }

        /// <summary>
        /// Gets the top Y coordinate.
        /// </summary>
        public int Y { get; init; }

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Gets the right X coordinate (<c>X + Width</c>).
        /// </summary>
        public int Right => X + Width;

        /// <summary>
        /// Gets the bottom Y coordinate (<c>Y + Height</c>).
        /// </summary>
        public int Bottom => Y + Height;

        /// <summary>
        /// Gets whether the rectangle has a non-positive width or height.
        /// </summary>
        public bool IsEmpty => Width <= 0 || Height <= 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomRect"/> struct.
        /// </summary>
        public DomRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Contains computed scrollbar visibility, viewport dimensions, scrollbar layout bounds, and scroll range parameters for a virtualized DOM tree control.
    /// </summary>
    public readonly struct DomScrollBarMetrics
    {
        /// <summary>
        /// Gets whether the vertical scrollbar should be visible.
        /// </summary>
        public bool NeedVertical { get; init; }

        /// <summary>
        /// Gets whether the horizontal scrollbar should be visible.
        /// </summary>
        public bool NeedHorizontal { get; init; }

        /// <summary>
        /// Gets the left X coordinate of the content viewport.
        /// </summary>
        public int ViewportX { get; init; }

        /// <summary>
        /// Gets the top Y coordinate of the content viewport.
        /// </summary>
        public int ViewportY { get; init; }

        /// <summary>
        /// Gets the available viewport width excluding visible scrollbars and border insets.
        /// </summary>
        public int ViewportWidth { get; init; }

        /// <summary>
        /// Gets the available viewport height excluding visible scrollbars and border insets.
        /// </summary>
        public int ViewportHeight { get; init; }

        /// <summary>
        /// Gets the bounding rectangle of the content viewport.
        /// </summary>
        public DomRect ViewportBounds => new(ViewportX, ViewportY, ViewportWidth, ViewportHeight);

        /// <summary>
        /// Gets the bounding rectangle for the vertical scrollbar control.
        /// </summary>
        public DomRect VerticalScrollBarBounds { get; init; }

        /// <summary>
        /// Gets the bounding rectangle for the horizontal scrollbar control.
        /// </summary>
        public DomRect HorizontalScrollBarBounds { get; init; }

        /// <summary>
        /// Gets the bounding rectangle for the bottom-right intersection corner when both scrollbars are visible.
        /// </summary>
        public DomRect ScrollCornerBounds { get; init; }

        /// <summary>
        /// Gets the large change (page size) for the vertical scrollbar.
        /// </summary>
        public int VerticalLargeChange { get; init; }

        /// <summary>
        /// Gets the small change (row step) for the vertical scrollbar.
        /// </summary>
        public int VerticalSmallChange { get; init; }

        /// <summary>
        /// Gets the maximum value for the vertical scrollbar.
        /// </summary>
        public int VerticalMaximum { get; init; }

        /// <summary>
        /// Gets the clamped value for the vertical scrollbar.
        /// </summary>
        public int VerticalValue { get; init; }

        /// <summary>
        /// Gets the large change (page width) for the horizontal scrollbar.
        /// </summary>
        public int HorizontalLargeChange { get; init; }

        /// <summary>
        /// Gets the small change (character step) for the horizontal scrollbar.
        /// </summary>
        public int HorizontalSmallChange { get; init; }

        /// <summary>
        /// Gets the maximum value for the horizontal scrollbar.
        /// </summary>
        public int HorizontalMaximum { get; init; }

        /// <summary>
        /// Gets the clamped value for the horizontal scrollbar.
        /// </summary>
        public int HorizontalValue { get; init; }
    }

    /// <summary>
    /// Represents a single syntax-highlighted text segment of a serialized <see cref="DomNode"/> line.
    /// </summary>
    public readonly struct DomDisplayPart
    {
        /// <summary>
        /// The text content of this display segment.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The syntax token classification determining how <see cref="Text"/> should be colored.
        /// </summary>
        public readonly DomTokenColor ColorType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomDisplayPart"/> struct.
        /// </summary>
        public DomDisplayPart(string text, DomTokenColor colorType)
        {
            Text = text;
            ColorType = colorType;
        }
    }

    /// <summary>
    /// Represents an indexed, pre-tokenized node entry inside a <see cref="DomTreeSnapshot"/>.
    /// </summary>
    public sealed class DomTreeItem
    {
        /// <summary>
        /// Gets the underlying <see cref="DomNode"/> represented by this tree item.
        /// </summary>
        public DomNode Node { get; }

        /// <summary>
        /// Gets the unique lookup key for this item (the element's CSS path or a synthetic text-node key).
        /// </summary>
        public string UniqueKey { get; }

        /// <summary>
        /// Gets the <see cref="UniqueKey"/> of this item's parent node, or <c>null</c> for the root node.
        /// </summary>
        public string? ParentKey { get; }

        /// <summary>
        /// Gets the zero-based nesting depth of this item in the DOM tree.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets the child <see cref="DomTreeItem"/> nodes under this item.
        /// </summary>
        public List<DomTreeItem> Children { get; } = new();

        /// <summary>
        /// Gets whether this tree item has one or more child items.
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// Gets the pre-tokenized syntax parts for the opening tag or text content.
        /// </summary>
        public DomDisplayPart[] OpenParts { get; }

        /// <summary>
        /// Gets the pre-tokenized syntax parts for the collapsed closing suffix (<c>...&lt;/tag&gt;</c>) when collapsed.
        /// </summary>
        public DomDisplayPart[] CollapsedSuffixParts { get; }

        /// <summary>
        /// Gets the estimated total character length of this row for fast horizontal scrollbar sizing.
        /// </summary>
        public int EstimatedCharLength { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomTreeItem"/> class.
        /// </summary>
        public DomTreeItem(
            DomNode node,
            string uniqueKey,
            string? parentKey,
            int depth,
            DomDisplayPart[] openParts,
            DomDisplayPart[] collapsedSuffixParts,
            int estimatedCharLength)
        {
            Node = node;
            UniqueKey = uniqueKey;
            ParentKey = parentKey;
            Depth = depth;
            OpenParts = openParts;
            CollapsedSuffixParts = collapsedSuffixParts;
            EstimatedCharLength = estimatedCharLength;
        }
    }

    /// <summary>
    /// Represents the result of a coalesced DOM tree snapshot refresh operation on a <see cref="Server.BrowserClient"/>.
    /// </summary>
    public sealed class DomSnapshotRefreshResult
    {
        /// <summary>
        /// Gets the indexed <see cref="DomTreeSnapshot"/> built from the page DOM.
        /// </summary>
        public DomTreeSnapshot Snapshot { get; }

        /// <summary>
        /// Gets the resolved CSS path that should be selected in the UI tree after applying the snapshot.
        /// </summary>
        public string? ResolvedSelectedPath { get; }

        /// <summary>
        /// Gets whether the UI should scroll the resolved selected node into view.
        /// </summary>
        public bool ScrollSelectionIntoView { get; }

        /// <summary>
        /// Gets the <see cref="DomNode"/> resolved from <see cref="ResolvedSelectedPath"/> within <see cref="Snapshot"/>, if any.
        /// </summary>
        public DomNode? SelectedNode => Snapshot.FindNodeByPath(ResolvedSelectedPath);

        /// <summary>
        /// Gets the formatted details for <see cref="SelectedNode"/>, or an empty string when no node is selected.
        /// </summary>
        public string SelectedNodeDetails => SelectedNode?.GetFormattedDetails() ?? string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomSnapshotRefreshResult"/> class.
        /// </summary>
        public DomSnapshotRefreshResult(DomTreeSnapshot snapshot, string? resolvedSelectedPath, bool scrollSelectionIntoView)
        {
            Snapshot = snapshot;
            ResolvedSelectedPath = resolvedSelectedPath;
            ScrollSelectionIntoView = scrollSelectionIntoView;
        }
    }

    /// <summary>
    /// Thread-safe, pre-indexed DOM tree snapshot built from a <see cref="DomNode"/> hierarchy.
    /// Provides O(1) lookup by CSS path, ancestor expansion helpers, and pre-tokenized syntax highlighting parts.
    /// </summary>
    public sealed class DomTreeSnapshot
    {
        private const int MaxAttrValueDisplayLength = 180;
        private const int MaxTextDisplayLength = 220;
        private const int MaxAttributesDisplayed = 24;

        /// <summary>
        /// Gets the root <see cref="DomNode"/> of this snapshot.
        /// </summary>
        public DomNode RootNode { get; }

        /// <summary>
        /// Gets the root <see cref="DomTreeItem"/> of this snapshot.
        /// </summary>
        public DomTreeItem RootItem { get; }

        /// <summary>
        /// Gets the O(1) lookup dictionary mapping each item's <see cref="DomTreeItem.UniqueKey"/> to its <see cref="DomTreeItem"/>.
        /// </summary>
        public Dictionary<string, DomTreeItem> ItemsByKey { get; }

        private DomTreeSnapshot(DomNode rootNode, DomTreeItem rootItem, Dictionary<string, DomTreeItem> itemsByKey)
        {
            RootNode = rootNode;
            RootItem = rootItem;
            ItemsByKey = itemsByKey;
        }

        /// <summary>
        /// Indexes and tokenizes a <see cref="DomNode"/> hierarchy into a <see cref="DomTreeSnapshot"/>.
        /// Safe to call on a background worker thread.
        /// </summary>
        public static DomTreeSnapshot Build(DomNode rootNode)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));
            var itemsByKey = new Dictionary<string, DomTreeItem>(4096, StringComparer.Ordinal);
            var rootItem = BuildItemRecursive(rootNode, null, 0, 0, itemsByKey);
            return new DomTreeSnapshot(rootNode, rootItem, itemsByKey);
        }

        /// <summary>
        /// Attempts to find a <see cref="DomTreeItem"/> by its unique CSS path key in O(1) time.
        /// </summary>
        public DomTreeItem? FindItemByPath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return ItemsByKey.TryGetValue(path, out var item) ? item : null;
        }

        /// <summary>
        /// Attempts to find a <see cref="DomNode"/> by its CSS path in O(1) time using the snapshot index.
        /// </summary>
        public DomNode? FindNodeByPath(string? path)
        {
            return FindItemByPath(path)?.Node;
        }

        /// <summary>
        /// Populates <paramref name="targetExpandedKeys"/> by preserving valid keys from <paramref name="previousExpandedKeys"/>,
        /// ensuring default root/body nodes are expanded if empty, and expanding all ancestors of <paramref name="selectedPath"/> if specified.
        /// </summary>
        /// <param name="targetExpandedKeys">The set of expanded keys to populate.</param>
        /// <param name="previousExpandedKeys">Optional previously expanded keys to retain if still present in this snapshot.</param>
        /// <param name="selectedPath">Optional CSS path of the selected node whose ancestors should be expanded.</param>
        /// <returns>The resolved <see cref="DomTreeItem"/> for <paramref name="selectedPath"/>, or <c>null</c> if not found.</returns>
        public DomTreeItem? PrepareExpandedState(
            HashSet<string> targetExpandedKeys,
            IEnumerable<string>? previousExpandedKeys = null,
            string? selectedPath = null)
        {
            if (targetExpandedKeys == null) throw new ArgumentNullException(nameof(targetExpandedKeys));

            IEnumerable<string> sourceKeys = previousExpandedKeys ?? targetExpandedKeys;
            var retained = new List<string>();
            foreach (var k in sourceKeys)
            {
                if (ItemsByKey.ContainsKey(k))
                    retained.Add(k);
            }

            targetExpandedKeys.Clear();
            foreach (var k in retained)
            {
                targetExpandedKeys.Add(k);
            }

            if (targetExpandedKeys.Count == 0)
            {
                targetExpandedKeys.Add(RootItem.UniqueKey);
                if (ItemsByKey.ContainsKey("body"))
                    targetExpandedKeys.Add("body");
            }

            if (!string.IsNullOrEmpty(selectedPath) && ItemsByKey.TryGetValue(selectedPath, out var foundTarget))
            {
                ExpandAncestors(foundTarget, targetExpandedKeys);
                return foundTarget;
            }

            return null;
        }

        /// <summary>
        /// Expands all ancestor keys of <paramref name="targetItem"/> into <paramref name="expandedKeys"/>.
        /// Returns <c>true</c> if any new ancestor key was added.
        /// </summary>
        public bool ExpandAncestors(DomTreeItem targetItem, HashSet<string> expandedKeys)
        {
            if (targetItem == null) throw new ArgumentNullException(nameof(targetItem));
            if (expandedKeys == null) throw new ArgumentNullException(nameof(expandedKeys));

            bool addedAny = false;
            string? parentKey = targetItem.ParentKey;
            while (!string.IsNullOrEmpty(parentKey))
            {
                if (expandedKeys.Add(parentKey))
                    addedAny = true;

                if (ItemsByKey.TryGetValue(parentKey, out var parentItem))
                    parentKey = parentItem.ParentKey;
                else
                    break;
            }
            return addedAny;
        }

        /// <summary>
        /// Builds the flat list of currently visible <see cref="DomTreeItem"/> rows based on the set of expanded node keys.
        /// </summary>
        public List<DomTreeItem> BuildVisibleRows(HashSet<string> expandedKeys)
        {
            return BuildVisibleRows(RootItem, expandedKeys);
        }

        /// <summary>
        /// Builds the flat list of currently visible <see cref="DomTreeItem"/> rows starting from <paramref name="rootItem"/>.
        /// </summary>
        public static List<DomTreeItem> BuildVisibleRows(DomTreeItem rootItem, HashSet<string> expandedKeys)
        {
            var list = new List<DomTreeItem>(Math.Max(64, (expandedKeys?.Count ?? 1) * 8));
            if (rootItem != null && expandedKeys != null)
            {
                AppendVisibleRecursive(rootItem, expandedKeys, list);
            }
            return list;
        }

        private static void AppendVisibleRecursive(DomTreeItem item, HashSet<string> expandedKeys, List<DomTreeItem> output)
        {
            output.Add(item);
            if (item.HasChildren && expandedKeys.Contains(item.UniqueKey))
            {
                var children = item.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    AppendVisibleRecursive(children[i], expandedKeys, output);
                }
            }
        }

        private static DomTreeItem BuildItemRecursive(
            DomNode node,
            string? parentKey,
            int depth,
            int siblingIndex,
            Dictionary<string, DomTreeItem> itemsByKey)
        {
            string uniqueKey = !string.IsNullOrEmpty(node.Path)
                ? node.Path!
                : $"{parentKey ?? "root"}#text_{siblingIndex}";

            BuildDisplayParts(node, out var openParts, out var suffixParts, out int charLen);

            var item = new DomTreeItem(node, uniqueKey, parentKey, depth, openParts, suffixParts, charLen);
            itemsByKey[uniqueKey] = item;

            if (node.Children != null && node.Children.Count > 0)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    var childItem = BuildItemRecursive(node.Children[i], uniqueKey, depth + 1, i, itemsByKey);
                    item.Children.Add(childItem);
                }
            }

            return item;
        }

        private static void BuildDisplayParts(
            DomNode domNode,
            out DomDisplayPart[] openParts,
            out DomDisplayPart[] collapsedSuffixParts,
            out int estimatedCharLen)
        {
            estimatedCharLen = 0;
            if (domNode.IsElement)
            {
                string tag = domNode.TagName ?? "element";
                int attrCount = domNode.Attributes?.Count ?? 0;
                var parts = new List<DomDisplayPart>(3 + (Math.Min(attrCount, MaxAttributesDisplayed) * 3));

                parts.Add(new DomDisplayPart("<", DomTokenColor.Punctuation));
                parts.Add(new DomDisplayPart(tag, DomTokenColor.TagName));
                estimatedCharLen += 1 + tag.Length;

                if (domNode.Attributes != null && attrCount > 0)
                {
                    int processed = 0;
                    foreach (var attr in domNode.Attributes)
                    {
                        if (processed >= MaxAttributesDisplayed)
                        {
                            parts.Add(new DomDisplayPart(" ...", DomTokenColor.Punctuation));
                            estimatedCharLen += 4;
                            break;
                        }

                        string cleanVal = SanitizeSingleLine(attr.Value, MaxAttrValueDisplayLength);
                        string quotedVal = $"\"{cleanVal}\"";

                        parts.Add(new DomDisplayPart($" {attr.Key}", DomTokenColor.AttributeName));
                        parts.Add(new DomDisplayPart("=", DomTokenColor.Punctuation));
                        parts.Add(new DomDisplayPart(quotedVal, DomTokenColor.AttributeValue));

                        estimatedCharLen += 1 + attr.Key.Length + 1 + quotedVal.Length;
                        processed++;
                    }
                }

                parts.Add(new DomDisplayPart(">", DomTokenColor.Punctuation));
                estimatedCharLen += 1;
                openParts = parts.ToArray();

                if (domNode.Children != null && domNode.Children.Count > 0)
                {
                    collapsedSuffixParts = new[]
                    {
                        new DomDisplayPart("...</", DomTokenColor.Punctuation),
                        new DomDisplayPart(tag, DomTokenColor.TagName),
                        new DomDisplayPart(">", DomTokenColor.Punctuation)
                    };
                    estimatedCharLen += 6 + tag.Length;
                }
                else
                {
                    collapsedSuffixParts = Array.Empty<DomDisplayPart>();
                }
            }
            else
            {
                string cleanText = SanitizeSingleLine(domNode.Text ?? string.Empty, MaxTextDisplayLength);
                string display = $"\"{cleanText}\"";
                openParts = new[] { new DomDisplayPart(display, DomTokenColor.TextContent) };
                collapsedSuffixParts = Array.Empty<DomDisplayPart>();
                estimatedCharLen = display.Length;
            }
        }

        private static string SanitizeSingleLine(string raw, int maxLen)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            int limit = Math.Min(raw.Length, maxLen);
            var sb = new StringBuilder(limit + 3);
            bool lastWasSpace = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == '\r' || c == '\n' || c == '\t')
                {
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    lastWasSpace = c == ' ';
                }

                if (sb.Length >= maxLen)
                {
                    if (i < raw.Length - 1)
                        sb.Append("...");
                    break;
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Manages the interactive state, visible row indexing, node expansion, selection, and keyboard navigation
    /// for a virtualized DOM tree view control.
    /// </summary>
    public sealed class DomTreeViewportState
    {
        /// <summary>
        /// Gets the sample string used to estimate the average character width during font/DPI measurement.
        /// </summary>
        public const string FontMeasurementSample = "MMMMMMMMMM";

        private readonly Dictionary<string, int> _rowIndexByKey = new(StringComparer.Ordinal);
        private readonly HashSet<string> _expandedKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DomRowMeasureCache> _measureCache = new(StringComparer.Ordinal);
        private List<DomTreeItem> _visibleRows = new();
        private int _itemHeight = 24;

        /// <summary>
        /// Gets the current font/DPI measurement generation counter.
        /// </summary>
        public int FontGeneration { get; private set; } = 1;

        /// <summary>
        /// Gets or sets the estimated average character width in pixels used for unmeasured row width estimation.
        /// </summary>
        public int AvgCharWidth { get; set; } = 8;

        /// <summary>
        /// Gets or sets the rendered height of each visible row in pixels. Values below 16 are clamped to 16.
        /// </summary>
        public int ItemHeight
        {
            get => _itemHeight;
            set => _itemHeight = Math.Max(16, value);
        }

        /// <summary>
        /// Gets the horizontal indentation applied per tree depth level in pixels.
        /// </summary>
        public int IndentWidth { get; private set; } = 18;

        /// <summary>
        /// Gets the width reserved for the expand/collapse glyph column in pixels.
        /// </summary>
        public int GlyphColumnWidth { get; private set; } = 18;

        /// <summary>
        /// Gets the left content padding in pixels.
        /// </summary>
        public int PaddingLeft { get; private set; } = 6;

        /// <summary>
        /// Gets the DPI-scaled expand/collapse glyph box size in pixels.
        /// </summary>
        public int GlyphBoxSize { get; private set; } = 9;

        /// <summary>
        /// Gets the minimum text width kept visible when horizontally scrolling to a row in pixels.
        /// </summary>
        public int MinVisibleTextWidth { get; private set; } = 100;

        /// <summary>
        /// Gets the horizontal scroll margin applied when bringing a row into view in pixels.
        /// </summary>
        public int HorizontalScrollMargin { get; private set; } = 24;

        /// <summary>
        /// Gets the border inset applied around the viewport in pixels.
        /// </summary>
        public int BorderInset { get; private set; } = 1;

        /// <summary>
        /// Gets or sets the current horizontal scroll offset in pixels.
        /// </summary>
        public int ScrollX { get; set; }

        /// <summary>
        /// Gets or sets the current vertical scroll offset in pixels.
        /// </summary>
        public int ScrollY { get; set; }

        /// <summary>
        /// Gets the 0-based visible row index currently under the mouse pointer, or -1 when no row is hovered.
        /// </summary>
        public int HoveredRowIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the maximum calculated horizontal content width in pixels across visible rows.
        /// </summary>
        public int MaxContentWidth { get; private set; } = 400;

        /// <summary>
        /// Gets the active <see cref="DomTreeSnapshot"/>, if any.
        /// </summary>
        public DomTreeSnapshot? Snapshot { get; private set; }

        /// <summary>
        /// Gets the flat list of currently visible <see cref="DomTreeItem"/> rows.
        /// </summary>
        public IReadOnlyList<DomTreeItem> VisibleRows => _visibleRows;

        /// <summary>
        /// Gets the currently selected <see cref="DomTreeItem"/>, if any.
        /// </summary>
        public DomTreeItem? SelectedItem { get; private set; }

        /// <summary>
        /// Gets the unique key of the currently selected <see cref="DomTreeItem"/>, if any.
        /// </summary>
        public string? SelectedKey { get; private set; }

        /// <summary>
        /// Gets the root <see cref="DomNode"/> of the active snapshot.
        /// </summary>
        public DomNode? RootDomNode => Snapshot?.RootNode;

        /// <summary>
        /// Gets the currently selected <see cref="DomNode"/>, if any.
        /// </summary>
        public DomNode? SelectedDomNode => SelectedItem?.Node;

        /// <summary>
        /// Gets the CSS path of the currently selected <see cref="DomNode"/>, if any.
        /// </summary>
        public string? SelectedPath => SelectedItem?.Node.Path;

        /// <summary>
        /// Returns whether the specified node key is currently expanded.
        /// </summary>
        public bool IsExpanded(string uniqueKey) => _expandedKeys.Contains(uniqueKey);

        /// <summary>
        /// Attempts to get the 0-based visible row index for the specified node key.
        /// </summary>
        public bool TryGetRowIndex(string? uniqueKey, out int rowIndex)
        {
            if (string.IsNullOrEmpty(uniqueKey))
            {
                rowIndex = -1;
                return false;
            }
            return _rowIndexByKey.TryGetValue(uniqueKey, out rowIndex);
        }

        /// <summary>
        /// Returns a snapshot copy of the currently expanded node keys.
        /// </summary>
        public HashSet<string> GetExpandedPathsSnapshot()
        {
            return new HashSet<string>(_expandedKeys, StringComparer.Ordinal);
        }

        /// <summary>
        /// Clears the snapshot, visible rows, expanded keys, measurement cache, scroll offsets, and selection state.
        /// </summary>
        public void Clear()
        {
            Snapshot = null;
            _visibleRows.Clear();
            _rowIndexByKey.Clear();
            _expandedKeys.Clear();
            _measureCache.Clear();
            SelectedItem = null;
            SelectedKey = null;
            ScrollX = 0;
            ScrollY = 0;
            HoveredRowIndex = -1;
            MaxContentWidth = 200;
        }

        /// <summary>
        /// Increments <see cref="FontGeneration"/>, clears cached token measurements, and updates <see cref="AvgCharWidth"/>.
        /// </summary>
        public void InvalidateMeasurements(int avgCharWidth)
        {
            FontGeneration++;
            _measureCache.Clear();
            AvgCharWidth = Math.Max(1, avgCharWidth);
        }

        /// <summary>
        /// Computes and stores all DPI-scaled row metrics, updates <see cref="BorderInset"/>,
        /// and invalidates cached token measurements.
        /// </summary>
        public void UpdateFontMetrics(
            int fontHeight,
            int sampleTextWidth,
            float dpiScale,
            int borderInset)
        {
            float safeScale = dpiScale > 0f ? dpiScale : 1f;
            int Scale(int val) => (int)Math.Ceiling(val * safeScale);

            ItemHeight = Math.Max(fontHeight + Scale(6), Scale(24));
            IndentWidth = Scale(18);
            GlyphColumnWidth = Scale(18);
            PaddingLeft = Scale(6);
            GlyphBoxSize = Scale(9);
            MinVisibleTextWidth = Scale(100);
            HorizontalScrollMargin = Scale(24);
            BorderInset = Math.Max(0, borderInset);

            int avgCharWidth = Math.Max(6, (int)Math.Ceiling(sampleTextWidth / (double)FontMeasurementSample.Length));
            InvalidateMeasurements(avgCharWidth);
        }

        /// <summary>
        /// Measures the standard <see cref="FontMeasurementSample"/> using <paramref name="measureTextWidth"/>,
        /// computes all DPI-scaled row metrics, and invalidates cached token measurements.
        /// </summary>
        public void UpdateFontMetrics(
            int fontHeight,
            Func<string, int> measureTextWidth,
            float dpiScale,
            int borderInset)
        {
            if (measureTextWidth == null) throw new ArgumentNullException(nameof(measureTextWidth));
            UpdateFontMetrics(fontHeight, measureTextWidth(FontMeasurementSample), dpiScale, borderInset);
        }

        /// <summary>
        /// Updates <see cref="BorderInset"/> without recomputing the remaining layout metrics.
        /// </summary>
        public void UpdateBorderInset(int borderInset)
        {
            BorderInset = Math.Max(0, borderInset);
        }

        /// <summary>
        /// Calculates the pixel layout geometry for rendering the visible row at <paramref name="rowIndex"/>.
        /// </summary>
        public DomRowRenderLayout CalculateRowRenderLayout(
            int rowIndex,
            DomTreeItem item,
            DomRowMeasureCache measure,
            int viewportLeft,
            int viewportTop,
            int viewportRight,
            int fontHeight,
            bool showLines,
            bool showRootLines,
            bool showPlusMinus)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (measure == null) throw new ArgumentNullException(nameof(measure));

            int itemHeight = ItemHeight;
            int paddingLeft = PaddingLeft;
            int indentWidth = IndentWidth;
            int glyphColumnWidth = GlyphColumnWidth;

            int glyphBoxSize = Math.Max(5, GlyphBoxSize);
            if (glyphBoxSize % 2 == 0)
                glyphBoxSize++;

            int rowY = viewportTop + (rowIndex * itemHeight) - ScrollY;
            int centerY = rowY + (itemHeight / 2);
            int indentX = viewportLeft + paddingLeft + (item.Depth * indentWidth) - ScrollX;
            int textStartX = indentX + glyphColumnWidth;
            int textY = rowY + ((itemHeight - fontHeight) / 2);

            bool isExpanded = item.HasChildren && _expandedKeys.Contains(item.UniqueKey);
            bool isSelected = string.Equals(item.UniqueKey, SelectedKey, StringComparison.Ordinal);
            bool isHovered = rowIndex == HoveredRowIndex && !isSelected;

            int textTotalW = GetMeasuredRowTextWidth(item, measure.OpenTotalWidth, measure.CollapsedSuffixTotalWidth);
            int lineCenterX = indentX + (glyphColumnWidth / 2);
            bool hasBranchConnector = showLines &&
                                      (item.Depth > 0 || showRootLines) &&
                                      lineCenterX >= viewportLeft - 20 &&
                                      lineCenterX <= viewportRight;

            int boxLeft = indentX + ((glyphColumnWidth - glyphBoxSize) / 2);
            int boxTop = centerY - (glyphBoxSize / 2);
            const int signMargin = 2;

            return new DomRowRenderLayout
            {
                RowY = rowY,
                CenterY = centerY,
                IndentX = indentX,
                TextStartX = textStartX,
                TextY = textY,
                TextTotalWidth = textTotalW,
                IsExpanded = isExpanded,
                IsSelected = isSelected,
                IsHovered = isHovered,
                HighlightLeft = textStartX - 2,
                HighlightTop = rowY,
                HighlightWidth = textTotalW + 6,
                HighlightHeight = itemHeight,
                HasBranchConnector = hasBranchConnector,
                BranchLineCenterX = lineCenterX,
                BranchHorizontalEndX = textStartX - 3,
                ShowExpandGlyph = showPlusMinus && item.HasChildren,
                GlyphBoxLeft = boxLeft,
                GlyphBoxTop = boxTop,
                GlyphBoxDrawSize = glyphBoxSize - 1,
                GlyphSignLeft = boxLeft + signMargin,
                GlyphSignRight = boxLeft + glyphBoxSize - 1 - signMargin,
                GlyphSignMidX = boxLeft + (glyphBoxSize / 2),
                GlyphSignMidY = boxTop + (glyphBoxSize / 2),
                GlyphSignTop = boxTop + signMargin,
                GlyphSignBottom = boxTop + glyphBoxSize - 1 - signMargin
            };
        }

        /// <summary>
        /// Attempts to compute the horizontal pixel coordinate for a vertical hierarchy guide line at <paramref name="depthLevel"/>
        /// if it falls within <c>[viewportLeft, viewportRight]</c>.
        /// </summary>
        public bool TryGetAncestorGuideLineX(
            int depthLevel,
            int viewportLeft,
            int viewportRight,
            out int guideX)
        {
            guideX = viewportLeft + PaddingLeft + (depthLevel * IndentWidth) + (GlyphColumnWidth / 2) - ScrollX;
            return guideX >= viewportLeft && guideX <= viewportRight;
        }

        /// <summary>
        /// Enumerates the horizontal X coordinates of all visible ancestor vertical hierarchy guide lines for <paramref name="item"/>.
        /// </summary>
        public IEnumerable<int> EnumerateAncestorGuideLineXs(
            DomTreeItem item,
            bool showLines,
            bool showRootLines,
            int viewportLeft,
            int viewportRight)
        {
            if (!showLines || item == null)
                yield break;

            int startDepth = showRootLines ? 0 : 1;
            for (int d = startDepth; d < item.Depth; d++)
            {
                if (TryGetAncestorGuideLineX(d, viewportLeft, viewportRight, out int guideX))
                {
                    yield return guideX;
                }
            }
        }

        /// <summary>
        /// Enumerates all syntax tokens for <paramref name="item"/> that are horizontally visible within <c>[viewportLeft, viewportRight]</c>.
        /// </summary>
        public IEnumerable<DomTokenRenderSlice> EnumerateVisibleTokens(
            DomTreeItem item,
            DomRowMeasureCache measure,
            DomRowRenderLayout rowLayout,
            int viewportLeft,
            int viewportRight)
        {
            if (item == null) yield break;
            if (measure == null) yield break;

            int curX = rowLayout.TextStartX;
            var openParts = item.OpenParts;
            var openWidths = measure.OpenPartWidths;

            for (int p = 0; p < openParts.Length; p++)
            {
                int w = openWidths[p];
                if (curX + w >= viewportLeft)
                {
                    yield return new DomTokenRenderSlice
                    {
                        Text = openParts[p].Text,
                        ColorType = openParts[p].ColorType,
                        X = curX,
                        Y = rowLayout.TextY
                    };
                }
                curX += w;
                if (curX > viewportRight)
                    yield break;
            }

            if (item.HasChildren && !rowLayout.IsExpanded && curX <= viewportRight)
            {
                var suffixParts = item.CollapsedSuffixParts;
                var suffixWidths = measure.CollapsedSuffixWidths;
                for (int p = 0; p < suffixParts.Length; p++)
                {
                    int w = suffixWidths[p];
                    if (curX + w >= viewportLeft)
                    {
                        yield return new DomTokenRenderSlice
                        {
                            Text = suffixParts[p].Text,
                            ColorType = suffixParts[p].ColorType,
                            X = curX,
                            Y = rowLayout.TextY
                        };
                    }
                    curX += w;
                    if (curX > viewportRight)
                        yield break;
                }
            }
        }

        /// <summary>
        /// Recalculates <see cref="MaxContentWidth"/> across all currently visible rows using cached measurements or character length estimates.
        /// </summary>
        public int RecalculateContentWidth(int minWidth = 200)
        {
            int maxEstimatedW = minWidth;
            for (int i = 0; i < _visibleRows.Count; i++)
            {
                var row = _visibleRows[i];
                int textWidth = (_measureCache.TryGetValue(row.UniqueKey, out var cached) && cached.MeasuredFontGeneration == FontGeneration)
                    ? GetMeasuredRowTextWidth(row, cached.OpenTotalWidth, cached.CollapsedSuffixTotalWidth)
                    : (row.EstimatedCharLength * AvgCharWidth);

                int rowWidth = CalculateFullRowWidth(row, textWidth, PaddingLeft, IndentWidth, GlyphColumnWidth);
                if (rowWidth > maxEstimatedW)
                    maxEstimatedW = rowWidth;
            }

            MaxContentWidth = maxEstimatedW;
            return MaxContentWidth;
        }

        /// <summary>
        /// Retrieves or computes the <see cref="DomRowMeasureCache"/> for <paramref name="item"/> using the supplied <paramref name="measureTextWidth"/> callback.
        /// </summary>
        public DomRowMeasureCache EnsureItemMeasured(
            DomTreeItem item,
            Func<string, int> measureTextWidth,
            out bool maxContentWidthIncreased)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (measureTextWidth == null) throw new ArgumentNullException(nameof(measureTextWidth));

            maxContentWidthIncreased = false;
            if (_measureCache.TryGetValue(item.UniqueKey, out var existing) &&
                existing.MeasuredFontGeneration == FontGeneration)
            {
                return existing;
            }

            var openParts = item.OpenParts;
            int[] openWidths = new int[openParts.Length];
            int openTotal = 0;
            for (int i = 0; i < openParts.Length; i++)
            {
                int w = measureTextWidth(openParts[i].Text);
                openWidths[i] = w;
                openTotal += w;
            }

            var suffixParts = item.CollapsedSuffixParts;
            int[] suffixWidths = suffixParts.Length > 0 ? new int[suffixParts.Length] : Array.Empty<int>();
            int suffixTotal = 0;
            for (int i = 0; i < suffixParts.Length; i++)
            {
                int w = measureTextWidth(suffixParts[i].Text);
                suffixWidths[i] = w;
                suffixTotal += w;
            }

            var cache = new DomRowMeasureCache
            {
                OpenPartWidths = openWidths,
                CollapsedSuffixWidths = suffixWidths,
                OpenTotalWidth = openTotal,
                CollapsedSuffixTotalWidth = suffixTotal,
                MeasuredFontGeneration = FontGeneration
            };
            _measureCache[item.UniqueKey] = cache;

            int fullRowW = CalculateFullRowWidth(item, openTotal + suffixTotal, PaddingLeft, IndentWidth, GlyphColumnWidth);
            if (fullRowW > MaxContentWidth)
            {
                MaxContentWidth = fullRowW;
                maxContentWidthIncreased = true;
            }

            return cache;
        }

        /// <summary>
        /// Computes vertical and horizontal scrollbar visibility, ranges, and clamped scroll offsets for the given control dimensions.
        /// </summary>
        public DomScrollBarMetrics CalculateScrollBarMetrics(
            int clientWidth,
            int clientHeight,
            int verticalScrollBarWidth,
            int horizontalScrollBarHeight)
        {
            int borderInset = BorderInset;
            int availW = Math.Max(0, clientWidth - (borderInset * 2));
            int availH = Math.Max(0, clientHeight - (borderInset * 2));

            int safeItemHeight = Math.Max(1, ItemHeight);
            int totalHeight = _visibleRows.Count * safeItemHeight;
            int totalWidth = MaxContentWidth;

            bool needV = totalHeight > availH;
            int viewW = needV ? Math.Max(0, availW - verticalScrollBarWidth) : availW;
            bool needH = totalWidth > viewW;
            int viewH = needH ? Math.Max(0, availH - horizontalScrollBarHeight) : availH;

            if (!needV && totalHeight > viewH)
            {
                needV = true;
                viewW = Math.Max(0, availW - verticalScrollBarWidth);
            }

            int vLarge = Math.Max(1, viewH);
            int vSmall = safeItemHeight;
            int vMax = Math.Max(0, totalHeight - 1);
            if (needV)
            {
                int maxScrollY = Math.Max(0, totalHeight - viewH);
                ScrollY = Math.Clamp(ScrollY, 0, maxScrollY);
            }
            else
            {
                ScrollY = 0;
            }
            int vVal = Math.Clamp(ScrollY, 0, Math.Max(0, vMax - vLarge + 1));

            int hLarge = Math.Max(1, viewW);
            int hSmall = Math.Max(16, AvgCharWidth * 4);
            int hMax = Math.Max(0, totalWidth - 1);
            if (needH)
            {
                int maxScrollX = Math.Max(0, totalWidth - viewW);
                ScrollX = Math.Clamp(ScrollX, 0, maxScrollX);
            }
            else
            {
                ScrollX = 0;
            }
            int hVal = Math.Clamp(ScrollX, 0, Math.Max(0, hMax - hLarge + 1));

            CalculateScrollBarLayout(
                clientWidth,
                clientHeight,
                borderInset,
                verticalScrollBarWidth,
                horizontalScrollBarHeight,
                needV,
                needH,
                out var vBounds,
                out var hBounds,
                out var cornerBounds);

            return new DomScrollBarMetrics
            {
                NeedVertical = needV,
                NeedHorizontal = needH,
                ViewportX = borderInset,
                ViewportY = borderInset,
                ViewportWidth = viewW,
                ViewportHeight = viewH,
                VerticalScrollBarBounds = vBounds,
                HorizontalScrollBarBounds = hBounds,
                ScrollCornerBounds = cornerBounds,
                VerticalLargeChange = vLarge,
                VerticalSmallChange = vSmall,
                VerticalMaximum = vMax,
                VerticalValue = vVal,
                HorizontalLargeChange = hLarge,
                HorizontalSmallChange = hSmall,
                HorizontalMaximum = hMax,
                HorizontalValue = hVal
            };
        }

        /// <summary>
        /// Computes the content viewport rectangle given the control client dimensions, border inset, and current scrollbar visibility.
        /// </summary>
        public DomRect GetViewportBounds(
            int clientWidth,
            int clientHeight,
            bool vScrollVisible,
            int vScrollWidth,
            bool hScrollVisible,
            int hScrollHeight)
        {
            int borderInset = BorderInset;
            int w = Math.Max(0, clientWidth - (borderInset * 2) - (vScrollVisible ? vScrollWidth : 0));
            int h = Math.Max(0, clientHeight - (borderInset * 2) - (hScrollVisible ? hScrollHeight : 0));
            return new DomRect(borderInset, borderInset, w, h);
        }

        /// <summary>
        /// Computes the pixel layout rectangles for the vertical scrollbar, horizontal scrollbar, and bottom-right corner box
        /// using the current <see cref="BorderInset"/>.
        /// </summary>
        public void CalculateScrollBarLayout(
            int clientWidth,
            int clientHeight,
            int verticalScrollBarWidth,
            int horizontalScrollBarHeight,
            bool vScrollVisible,
            bool hScrollVisible,
            out DomRect verticalScrollBarBounds,
            out DomRect horizontalScrollBarBounds,
            out DomRect scrollCornerBounds)
        {
            CalculateScrollBarLayout(
                clientWidth,
                clientHeight,
                BorderInset,
                verticalScrollBarWidth,
                horizontalScrollBarHeight,
                vScrollVisible,
                hScrollVisible,
                out verticalScrollBarBounds,
                out horizontalScrollBarBounds,
                out scrollCornerBounds);
        }

        /// <summary>
        /// Computes the bottom-right scrollbar corner rectangle using the current <see cref="BorderInset"/>.
        /// </summary>
        public DomRect GetScrollCornerBounds(
            int clientWidth,
            int clientHeight,
            int verticalScrollBarWidth,
            int horizontalScrollBarHeight,
            bool vScrollVisible,
            bool hScrollVisible)
        {
            CalculateScrollBarLayout(
                clientWidth,
                clientHeight,
                verticalScrollBarWidth,
                horizontalScrollBarHeight,
                vScrollVisible,
                hScrollVisible,
                out _,
                out _,
                out var scrollCornerBounds);
            return scrollCornerBounds;
        }

        /// <summary>
        /// Computes the pixel layout rectangles for the vertical scrollbar, horizontal scrollbar, and bottom-right corner box.
        /// </summary>
        public static void CalculateScrollBarLayout(
            int clientWidth,
            int clientHeight,
            int borderInset,
            int verticalScrollBarWidth,
            int horizontalScrollBarHeight,
            bool vScrollVisible,
            bool hScrollVisible,
            out DomRect verticalScrollBarBounds,
            out DomRect horizontalScrollBarBounds,
            out DomRect scrollCornerBounds)
        {
            int innerW = Math.Max(0, clientWidth - (borderInset * 2));
            int innerH = Math.Max(0, clientHeight - (borderInset * 2));

            int vLeft = clientWidth - borderInset - verticalScrollBarWidth;
            int vTop = borderInset;
            int vHeight = Math.Max(0, innerH - (hScrollVisible ? horizontalScrollBarHeight : 0));
            verticalScrollBarBounds = new DomRect(vLeft, vTop, verticalScrollBarWidth, vHeight);

            int hLeft = borderInset;
            int hTop = clientHeight - borderInset - horizontalScrollBarHeight;
            int hWidth = Math.Max(0, innerW - (vScrollVisible ? verticalScrollBarWidth : 0));
            horizontalScrollBarBounds = new DomRect(hLeft, hTop, hWidth, horizontalScrollBarHeight);

            scrollCornerBounds = (vScrollVisible && hScrollVisible)
                ? new DomRect(vLeft, hTop, verticalScrollBarWidth, horizontalScrollBarHeight)
                : new DomRect(0, 0, 0, 0);
        }

        /// <summary>
        /// Calculates the inclusive range of visible row indices <c>[firstRow, lastRow]</c> for the current <see cref="ScrollY"/> and viewport height.
        /// </summary>
        public bool GetVisibleRowRange(int viewportHeight, out int firstRow, out int lastRow)
        {
            int itemHeight = ItemHeight;
            if (_visibleRows.Count == 0 || viewportHeight <= 0 || itemHeight <= 0)
            {
                firstRow = 0;
                lastRow = -1;
                return false;
            }

            firstRow = Math.Max(0, ScrollY / itemHeight);
            int visibleRowCount = (viewportHeight / itemHeight) + 2;
            lastRow = Math.Min(_visibleRows.Count - 1, firstRow + visibleRowCount);
            return firstRow <= lastRow;
        }

        /// <summary>
        /// Applies a mouse wheel delta to <see cref="ScrollX"/> (when <paramref name="horizontal"/> is true) or <see cref="ScrollY"/>,
        /// computing the maximum scroll offset automatically from the current content size and viewport dimensions.
        /// Returns <c>true</c> if the scroll offset changed.
        /// </summary>
        public bool ApplyMouseWheel(
            int wheelDelta,
            bool horizontal,
            int viewportWidth,
            int viewportHeight,
            int mouseWheelLines = 3)
        {
            int itemHeight = ItemHeight;
            int maxScrollOffset = horizontal
                ? Math.Max(0, MaxContentWidth - Math.Max(0, viewportWidth))
                : Math.Max(0, (_visibleRows.Count * Math.Max(1, itemHeight)) - Math.Max(0, viewportHeight));
            return ApplyMouseWheelToOffset(wheelDelta, horizontal, maxScrollOffset, itemHeight, mouseWheelLines);
        }

        /// <summary>
        /// Applies a mouse wheel delta to <see cref="ScrollX"/> (when <paramref name="horizontal"/> is true) or <see cref="ScrollY"/>,
        /// clamping against the provided maximum scroll bound. Returns <c>true</c> if the scroll offset changed.
        /// </summary>
        private bool ApplyMouseWheelToOffset(int wheelDelta, bool horizontal, int maxScrollOffset, int itemHeight, int mouseWheelLines = 3)
        {
            if (maxScrollOffset <= 0 || wheelDelta == 0)
                return false;

            if (horizontal)
            {
                int deltaX = -(wheelDelta / 120) * (AvgCharWidth * 8);
                int nextX = Math.Clamp(ScrollX + deltaX, 0, maxScrollOffset);
                if (nextX != ScrollX)
                {
                    ScrollX = nextX;
                    return true;
                }
                return false;
            }
            else
            {
                int lines = mouseWheelLines > 0 ? mouseWheelLines : 3;
                int deltaY = -(wheelDelta / 120) * (itemHeight * lines);
                int nextY = Math.Clamp(ScrollY + deltaY, 0, maxScrollOffset);
                if (nextY != ScrollY)
                {
                    ScrollY = nextY;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clears the current selection without modifying expanded nodes.
        /// </summary>
        public bool ClearSelection()
        {
            if (SelectedItem == null && SelectedKey == null)
                return false;

            SelectedItem = null;
            SelectedKey = null;
            return true;
        }

        /// <summary>
        /// Applies a new <see cref="DomTreeSnapshot"/>, preserving expanded keys and resolving the selected path.
        /// </summary>
        public void ApplySnapshot(DomTreeSnapshot? snapshot, HashSet<string>? expandedKeys = null, string? selectedPath = null)
        {
            Snapshot = snapshot;
            if (snapshot == null)
            {
                Clear();
                return;
            }

            _measureCache.Clear();
            var resolvedSelected = snapshot.PrepareExpandedState(_expandedKeys, expandedKeys, selectedPath);
            SelectedItem = resolvedSelected;
            SelectedKey = resolvedSelected?.UniqueKey;
            RebuildVisibleRows();
            RecalculateContentWidth();
        }

        /// <summary>
        /// Applies a new <see cref="DomTreeSnapshot"/>, recalculates content width, and optionally scrolls the selected row into view.
        /// Returns <c>true</c> if the scroll offsets changed.
        /// </summary>
        public bool ApplySnapshot(
            DomTreeSnapshot? snapshot,
            HashSet<string>? expandedKeys,
            string? selectedPath,
            bool ensureSelectedVisible,
            int viewportWidth,
            int viewportHeight,
            bool centerVertically = true)
        {
            ApplySnapshot(snapshot, expandedKeys, selectedPath);
            if (snapshot == null)
                return false;

            if (!ensureSelectedVisible || !TryGetRowIndex(SelectedKey, out int selectedRowIndex))
                return false;

            return EnsureRowVisible(
                selectedRowIndex,
                viewportWidth,
                viewportHeight,
                centerVertically);
        }

        /// <summary>
        /// Applies a <see cref="DomSnapshotRefreshResult"/> directly, retaining and pruning the state's existing
        /// expanded keys against the refreshed snapshot. Returns <c>true</c> if the scroll offsets changed.
        /// </summary>
        public bool ApplyRefreshResult(
            DomSnapshotRefreshResult result,
            int viewportWidth,
            int viewportHeight)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return ApplySnapshot(
                result.Snapshot,
                null,
                result.ResolvedSelectedPath,
                result.ScrollSelectionIntoView,
                viewportWidth,
                viewportHeight);
        }

        /// <summary>
        /// Rebuilds the flat list of visible rows and updates the O(1) row index map.
        /// </summary>
        public void RebuildVisibleRows()
        {
            if (Snapshot == null)
            {
                _visibleRows.Clear();
                _rowIndexByKey.Clear();
                return;
            }

            _visibleRows = Snapshot.BuildVisibleRows(_expandedKeys);
            _rowIndexByKey.Clear();
            for (int i = 0; i < _visibleRows.Count; i++)
            {
                _rowIndexByKey[_visibleRows[i].UniqueKey] = i;
            }
        }

        private void RebuildVisibleRowsAndRecalculateWidth()
        {
            RebuildVisibleRows();
            RecalculateContentWidth();
        }

        /// <summary>
        /// Toggles the expanded/collapsed state of <paramref name="item"/>, rebuilds the visible rows,
        /// and recalculates the content width automatically.
        /// </summary>
        public bool ToggleExpansion(DomTreeItem item)
        {
            if (item == null || !item.HasChildren)
                return false;

            if (!_expandedKeys.Remove(item.UniqueKey))
            {
                _expandedKeys.Add(item.UniqueKey);
            }

            RebuildVisibleRowsAndRecalculateWidth();
            return true;
        }

        /// <summary>
        /// Selects and expands ancestors for the node matching <paramref name="path"/>.
        /// </summary>
        public bool SelectNodeByPath(string? path, out int selectedRowIndex)
        {
            selectedRowIndex = -1;
            if (string.IsNullOrEmpty(path))
            {
                ClearSelection();
                return false;
            }

            if (Snapshot == null || !Snapshot.ItemsByKey.TryGetValue(path, out var targetItem))
                return false;

            bool expandedChanged = Snapshot.ExpandAncestors(targetItem, _expandedKeys);
            if (expandedChanged || _visibleRows.Count == 0)
            {
                RebuildVisibleRowsAndRecalculateWidth();
            }

            SelectedItem = targetItem;
            SelectedKey = targetItem.UniqueKey;
            _rowIndexByKey.TryGetValue(targetItem.UniqueKey, out selectedRowIndex);
            return true;
        }

        /// <summary>
        /// Selects and expands to the node matching <paramref name="path"/>, recalculates content width,
        /// and optionally scrolls the selected row into view.
        /// Returns <c>true</c> when a node was selected.
        /// </summary>
        public bool SelectNodeByPath(
            string? path,
            bool ensureVisible,
            int viewportWidth,
            int viewportHeight,
            bool centerVertically,
            out bool scrollChanged,
            out int selectedRowIndex)
        {
            scrollChanged = false;
            if (!SelectNodeByPath(path, out selectedRowIndex))
                return false;

            RecalculateContentWidth();
            if (ensureVisible && selectedRowIndex >= 0)
            {
                scrollChanged = EnsureRowVisible(
                    selectedRowIndex,
                    viewportWidth,
                    viewportHeight,
                    centerVertically);
            }

            return true;
        }

        /// <summary>
        /// Selects the visible row at <paramref name="rowIndex"/>.
        /// </summary>
        public bool SelectRow(int rowIndex, out DomTreeItem? item, out bool selectionChanged)
        {
            if (rowIndex < 0 || rowIndex >= _visibleRows.Count)
            {
                item = null;
                selectionChanged = false;
                return false;
            }

            item = _visibleRows[rowIndex];
            selectionChanged = !string.Equals(SelectedKey, item.UniqueKey, StringComparison.Ordinal);
            SelectedItem = item;
            SelectedKey = item.UniqueKey;
            return true;
        }

        /// <summary>
        /// Gets the current selected row index, or 0 if no row is currently selected.
        /// </summary>
        public int GetCurrentOrFirstRowIndex()
        {
            return (SelectedKey != null && _rowIndexByKey.TryGetValue(SelectedKey, out int idx)) ? idx : 0;
        }

        /// <summary>
        /// Handles Right-arrow keyboard navigation (expands a collapsed parent node or selects its first child).
        /// </summary>
        public void NavigateRight(out bool expandedChanged, out int? newSelectedRowIndex)
        {
            expandedChanged = false;
            newSelectedRowIndex = null;
            if (_visibleRows.Count == 0) return;

            int currentRow = GetCurrentOrFirstRowIndex();
            var item = _visibleRows[currentRow];
            if (!item.HasChildren) return;

            if (!_expandedKeys.Contains(item.UniqueKey))
            {
                _expandedKeys.Add(item.UniqueKey);
                RebuildVisibleRowsAndRecalculateWidth();
                expandedChanged = true;
            }
            else if (currentRow + 1 < _visibleRows.Count)
            {
                newSelectedRowIndex = currentRow + 1;
            }
        }

        /// <summary>
        /// Handles Left-arrow keyboard navigation (collapses an expanded node or selects its parent node).
        /// </summary>
        public void NavigateLeft(out bool expandedChanged, out int? newSelectedRowIndex)
        {
            expandedChanged = false;
            newSelectedRowIndex = null;
            if (_visibleRows.Count == 0) return;

            int currentRow = GetCurrentOrFirstRowIndex();
            var item = _visibleRows[currentRow];
            if (item.HasChildren && _expandedKeys.Contains(item.UniqueKey))
            {
                _expandedKeys.Remove(item.UniqueKey);
                RebuildVisibleRowsAndRecalculateWidth();
                expandedChanged = true;
            }
            else if (!string.IsNullOrEmpty(item.ParentKey) && _rowIndexByKey.TryGetValue(item.ParentKey, out int parentRow))
            {
                newSelectedRowIndex = parentRow;
            }
        }

        /// <summary>
        /// Computes the 0-based visible row index at the specified vertical coordinate <paramref name="y"/>,
        /// or returns -1 if the coordinate is outside the viewport or visible row range.
        /// </summary>
        public int GetRowIndexAtY(int y, int viewportTop, int viewportHeight)
        {
            int itemHeight = ItemHeight;
            if (itemHeight <= 0 || viewportHeight <= 0)
                return -1;

            int viewportBottom = viewportTop + viewportHeight;
            if (y < viewportTop || y >= viewportBottom)
                return -1;

            int row = (y - viewportTop + ScrollY) / itemHeight;
            return (row >= 0 && row < _visibleRows.Count) ? row : -1;
        }

        /// <summary>
        /// Updates <see cref="HoveredRowIndex"/> from the pointer Y coordinate within the viewport.
        /// Returns <c>true</c> if the hovered row changed.
        /// </summary>
        public bool UpdateHoverAtY(int y, int viewportTop, int viewportHeight)
        {
            int row = GetRowIndexAtY(y, viewportTop, viewportHeight);
            if (row == HoveredRowIndex)
                return false;

            HoveredRowIndex = row;
            return true;
        }

        /// <summary>
        /// Clears the current hovered row state. Returns <c>true</c> if a hovered row was cleared.
        /// </summary>
        public bool ClearHover()
        {
            if (HoveredRowIndex == -1)
                return false;

            HoveredRowIndex = -1;
            return true;
        }

        /// <summary>
        /// Processes a pointer-down interaction at <c>(x, y)</c>, toggling node expansion if the left button hit an expand glyph or selecting the hit row.
        /// </summary>
        public bool HandlePointerDown(
            int x,
            int y,
            bool isLeftButton,
            int viewportLeft,
            int viewportTop,
            int viewportHeight,
            out DomTreeItem? hitItem,
            out bool expansionToggled,
            out bool selectionChanged)
        {
            hitItem = null;
            expansionToggled = false;
            selectionChanged = false;

            int rowIndex = GetRowIndexAtY(y, viewportTop, viewportHeight);
            if (rowIndex < 0)
                return false;

            hitItem = _visibleRows[rowIndex];
            if (isLeftButton && IsPointOnExpandGlyph(hitItem, x, viewportLeft))
            {
                expansionToggled = ToggleExpansion(hitItem);
                return true;
            }

            SelectRow(rowIndex, out hitItem, out selectionChanged);
            return true;
        }

        /// <summary>
        /// Processes a pointer double-click interaction at <c>(x, y)</c>, toggling expansion if a parent row was double-clicked outside its expand glyph.
        /// </summary>
        public bool HandlePointerDoubleClick(
            int x,
            int y,
            bool isLeftButton,
            int viewportLeft,
            int viewportTop,
            int viewportHeight)
        {
            if (!isLeftButton)
                return false;

            int rowIndex = GetRowIndexAtY(y, viewportTop, viewportHeight);
            if (rowIndex < 0 || rowIndex >= _visibleRows.Count)
                return false;

            var item = _visibleRows[rowIndex];
            if (!IsPointOnExpandGlyph(item, x, viewportLeft) && item.HasChildren)
            {
                return ToggleExpansion(item);
            }

            return false;
        }

        /// <summary>
        /// Processes a keyboard navigation command (<see cref="DomTreeNavigationKey"/>), updating node expansion, row selection, and viewport scroll offsets.
        /// </summary>
        public bool NavigateByKey(
            DomTreeNavigationKey key,
            int viewportWidth,
            int viewportHeight,
            out DomTreeItem? selectedItem,
            out bool expandedChanged,
            out bool selectionChanged,
            out bool scrollChanged)
        {
            selectedItem = null;
            expandedChanged = false;
            selectionChanged = false;
            scrollChanged = false;

            int rowCount = _visibleRows.Count;
            if (rowCount == 0)
                return false;

            int itemHeight = ItemHeight;
            int currentRow = GetCurrentOrFirstRowIndex();
            int pageRows = Math.Max(1, itemHeight > 0 ? viewportHeight / itemHeight : 1);
            int? targetRow = null;

            switch (key)
            {
                case DomTreeNavigationKey.Down:
                    if (currentRow < rowCount - 1)
                        targetRow = currentRow + 1;
                    break;
                case DomTreeNavigationKey.Up:
                    if (currentRow > 0)
                        targetRow = currentRow - 1;
                    break;
                case DomTreeNavigationKey.PageDown:
                    targetRow = Math.Min(rowCount - 1, currentRow + pageRows);
                    break;
                case DomTreeNavigationKey.PageUp:
                    targetRow = Math.Max(0, currentRow - pageRows);
                    break;
                case DomTreeNavigationKey.Home:
                    targetRow = 0;
                    break;
                case DomTreeNavigationKey.End:
                    targetRow = rowCount - 1;
                    break;
                case DomTreeNavigationKey.Right:
                    NavigateRight(out expandedChanged, out targetRow);
                    break;
                case DomTreeNavigationKey.Left:
                    NavigateLeft(out expandedChanged, out targetRow);
                    break;
                default:
                    return false;
            }

            if (targetRow.HasValue && SelectRow(targetRow.Value, out selectedItem, out selectionChanged))
            {
                scrollChanged = EnsureRowVisible(
                    targetRow.Value,
                    viewportWidth,
                    viewportHeight,
                    centerVertically: false);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the horizontal coordinate <paramref name="x"/> falls within the expand/collapse glyph box of <paramref name="item"/>.
        /// </summary>
        public bool IsPointOnExpandGlyph(
            DomTreeItem? item,
            int x,
            int viewportLeft,
            int hitSlop = 2)
        {
            if (item == null || !item.HasChildren)
                return false;

            int indentX = viewportLeft + PaddingLeft + (item.Depth * IndentWidth) - ScrollX;
            return x >= indentX - hitSlop && x < indentX + GlyphColumnWidth + hitSlop;
        }

        /// <summary>
        /// Updates <see cref="ScrollX"/> and <see cref="ScrollY"/> so that the row at <paramref name="rowIndex"/> is visible within the viewport.
        /// </summary>
        public bool EnsureRowVisible(
            int rowIndex,
            int viewportWidth,
            int viewportHeight,
            bool centerVertically)
        {
            int sx = ScrollX;
            int sy = ScrollY;
            if (CalculateScrollOffsetsForRow(
                    rowIndex,
                    viewportWidth,
                    viewportHeight,
                    centerVertically,
                    ref sx,
                    ref sy))
            {
                ScrollX = sx;
                ScrollY = sy;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Computes updated horizontal and vertical scroll offsets to ensure the row at <paramref name="rowIndex"/> is visible within the viewport.
        /// </summary>
        public bool CalculateScrollOffsetsForRow(
            int rowIndex,
            int viewportWidth,
            int viewportHeight,
            bool centerVertically,
            ref int scrollX,
            ref int scrollY)
        {
            int itemHeight = ItemHeight;
            if (rowIndex < 0 || rowIndex >= _visibleRows.Count || viewportHeight <= 0 || itemHeight <= 0)
                return false;

            int rowTop = rowIndex * itemHeight;
            int rowBottom = rowTop + itemHeight;

            if (rowTop < scrollY || rowBottom > scrollY + viewportHeight)
            {
                if (centerVertically)
                {
                    scrollY = rowTop - (viewportHeight / 2) + (itemHeight / 2);
                }
                else if (rowTop < scrollY)
                {
                    scrollY = rowTop;
                }
                else
                {
                    scrollY = rowBottom - viewportHeight;
                }
            }

            var item = _visibleRows[rowIndex];
            int itemTextStart = PaddingLeft + (item.Depth * IndentWidth);
            if (itemTextStart < scrollX || itemTextStart + MinVisibleTextWidth > scrollX + viewportWidth)
            {
                scrollX = Math.Max(0, itemTextStart - HorizontalScrollMargin);
            }

            return true;
        }

        /// <summary>
        /// Calculates the total rendered text width for <paramref name="item"/> given its measured open tag width
        /// and collapsed suffix width, accounting for whether the item is currently expanded.
        /// </summary>
        public int GetMeasuredRowTextWidth(DomTreeItem item, int openTotalWidth, int collapsedSuffixTotalWidth)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            bool includeCollapsedSuffix = item.HasChildren && !_expandedKeys.Contains(item.UniqueKey);
            return openTotalWidth + (includeCollapsedSuffix ? collapsedSuffixTotalWidth : 0);
        }

        /// <summary>
        /// Calculates the total horizontal pixel width of a row including indentation, expand glyph column, text width, and trailing padding.
        /// </summary>
        public static int CalculateFullRowWidth(
            DomTreeItem item,
            int textWidth,
            int paddingLeft,
            int indentWidth,
            int glyphColumnWidth,
            int trailingPadding = 32)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return paddingLeft + (item.Depth * indentWidth) + glyphColumnWidth + textWidth + trailingPadding;
        }
    }
}
