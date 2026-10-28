using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BrowserAutomationFramework.Models;
using BAFStudio.Utils;

namespace BAFStudio.Controls
{
    /// <summary>
    /// High-performance, virtualized DOM tree viewer control that renders only visible rows
    /// from a <see cref="DomTreeSnapshot"/> without Win32 SysTreeView32 overhead or flicker.
    /// </summary>
    public class DomTreeView : Control
    {
        private readonly VScrollBar _vScrollBar;
        private readonly HScrollBar _hScrollBar;
        private Font _codeFont;

        private readonly SolidBrush _selBgBrush;
        private readonly SolidBrush _hoverBgBrush;
        private readonly SolidBrush _glyphFillBrush;
        private readonly Pen _borderPen;
        private readonly Pen _linePen;
        private readonly Pen _glyphBorderPen;
        private readonly Pen _glyphSignPen;

        private readonly DomTreeViewportState _state = new();
        private bool _updatingScrollBars;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TreeViewDrawMode DrawMode { get; set; } = TreeViewDrawMode.OwnerDrawText;

        [DefaultValue(BorderStyle.FixedSingle)]
        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (_borderStyle != value)
                {
                    _borderStyle = value;
                    _state.UpdateBorderInset(_borderStyle == BorderStyle.FixedSingle ? 1 : 0);
                    LayoutScrollBars();
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        [DefaultValue(false)]
        public bool HideSelection { get; set; } = false;

        [DefaultValue(24)]
        public int ItemHeight
        {
            get => _state.ItemHeight;
            set
            {
                int previous = _state.ItemHeight;
                _state.ItemHeight = value;
                if (_state.ItemHeight != previous)
                {
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public bool ShowLines { get; set; } = true;

        [DefaultValue(true)]
        public bool ShowPlusMinus { get; set; } = true;

        [DefaultValue(true)]
        public bool ShowRootLines { get; set; } = true;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DomNode? RootDomNode => _state.RootDomNode;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DomNode? SelectedDomNode => _state.SelectedDomNode;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? SelectedPath => _state.SelectedPath;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TreeNode? SelectedNode
        {
            get => _state.SelectedDomNode != null ? CreateShimTreeNode(_state.SelectedDomNode) : null;
            set
            {
                if (value == null)
                {
                    ClearSelection();
                }
                else if (value.Tag is DomNode domNode && domNode.HasPath)
                {
                    SelectNodeByPath(domNode.Path, ensureVisible: true, raiseAfterSelect: false);
                }
            }
        }

        public event TreeViewEventHandler? AfterSelect;
        public event TreeNodeMouseClickEventHandler? NodeMouseClick;

        public DomTreeView()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            BackColor = ToColor(StudioTheme.DarkBackground);
            ForeColor = ToColor(StudioTheme.Foreground);
            _codeFont = new Font("Consolas", 10F);

            _selBgBrush = new SolidBrush(ToColor(DomTokenColorExtensions.SelectionBackgroundRgb));
            _hoverBgBrush = new SolidBrush(ToColor(DomTokenColorExtensions.HoverBackgroundRgb));
            _glyphFillBrush = new SolidBrush(ToColor(DomTokenColorExtensions.GlyphFillRgb));
            _borderPen = new Pen(ToColor(DomTokenColorExtensions.BorderRgb), 1f);
            _linePen = new Pen(ToColor(DomTokenColorExtensions.GuideLineRgb), 1f);
            _glyphBorderPen = new Pen(ToColor(DomTokenColorExtensions.GlyphBorderRgb), 1f);
            _glyphSignPen = new Pen(ToColor(DomTokenColorExtensions.GlyphSignRgb), 1f);

            _vScrollBar = new VScrollBar { Visible = false };
            _hScrollBar = new HScrollBar { Visible = false };

            _vScrollBar.Scroll += OnVScroll;
            _vScrollBar.ValueChanged += OnVScroll;
            _hScrollBar.Scroll += OnHScroll;
            _hScrollBar.ValueChanged += OnHScroll;

            Controls.Add(_vScrollBar);
            Controls.Add(_hScrollBar);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                ReThemer.ThemeControl(_vScrollBar);
                ReThemer.ThemeControl(_hScrollBar);
            }
            catch { }
            UpdateMetrics();
            LayoutScrollBars();
            UpdateScrollBars();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateMetrics();
            LayoutScrollBars();
            UpdateScrollBars();
            Invalidate();
        }

        private void UpdateMetrics()
        {
            const TextFormatFlags measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            int borderInset = _borderStyle == BorderStyle.FixedSingle ? 1 : 0;
            _state.UpdateFontMetrics(
                _codeFont.Height,
                sample => TextRenderer.MeasureText(sample, _codeFont, Size.Empty, measureFlags).Width,
                DeviceDpi / 96f,
                borderInset);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutScrollBars();
            UpdateScrollBars();
        }

        private static Rectangle ToRectangle(DomRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

        private Rectangle GetViewportBounds()
        {
            return ToRectangle(_state.GetViewportBounds(
                ClientSize.Width,
                ClientSize.Height,
                _vScrollBar.Visible,
                _vScrollBar.Width,
                _hScrollBar.Visible,
                _hScrollBar.Height));
        }

        private void LayoutScrollBars()
        {
            _state.CalculateScrollBarLayout(
                ClientSize.Width,
                ClientSize.Height,
                SystemInformation.VerticalScrollBarWidth,
                SystemInformation.HorizontalScrollBarHeight,
                _vScrollBar.Visible,
                _hScrollBar.Visible,
                out var vBounds,
                out var hBounds,
                out _);

            _vScrollBar.Bounds = ToRectangle(vBounds);
            _hScrollBar.Bounds = ToRectangle(hBounds);
        }

        private void UpdateScrollBars()
        {
            if (!IsHandleCreated) return;

            _updatingScrollBars = true;
            try
            {
                var metrics = _state.CalculateScrollBarMetrics(
                    ClientSize.Width,
                    ClientSize.Height,
                    SystemInformation.VerticalScrollBarWidth,
                    SystemInformation.HorizontalScrollBarHeight);

                if (_vScrollBar.Visible != metrics.NeedVertical || _hScrollBar.Visible != metrics.NeedHorizontal)
                {
                    _vScrollBar.Visible = metrics.NeedVertical;
                    _hScrollBar.Visible = metrics.NeedHorizontal;
                    _vScrollBar.Bounds = ToRectangle(metrics.VerticalScrollBarBounds);
                    _hScrollBar.Bounds = ToRectangle(metrics.HorizontalScrollBarBounds);
                }

                if (metrics.NeedVertical)
                {
                    _vScrollBar.Minimum = 0;
                    _vScrollBar.LargeChange = metrics.VerticalLargeChange;
                    _vScrollBar.SmallChange = metrics.VerticalSmallChange;
                    _vScrollBar.Maximum = metrics.VerticalMaximum;
                    _vScrollBar.Value = metrics.VerticalValue;
                }

                if (metrics.NeedHorizontal)
                {
                    _hScrollBar.Minimum = 0;
                    _hScrollBar.LargeChange = metrics.HorizontalLargeChange;
                    _hScrollBar.SmallChange = metrics.HorizontalSmallChange;
                    _hScrollBar.Maximum = metrics.HorizontalMaximum;
                    _hScrollBar.Value = metrics.HorizontalValue;
                }
            }
            finally
            {
                _updatingScrollBars = false;
            }
        }

        private void OnVScroll(object? sender, EventArgs e)
        {
            if (_updatingScrollBars) return;
            if (_state.ScrollY != _vScrollBar.Value)
            {
                _state.ScrollY = _vScrollBar.Value;
                Invalidate();
            }
        }

        private void OnHScroll(object? sender, EventArgs e)
        {
            if (_updatingScrollBars) return;
            if (_state.ScrollX != _hScrollBar.Value)
            {
                _state.ScrollX = _hScrollBar.Value;
                Invalidate();
            }
        }

        /// <summary>
        /// Returns a copy of the currently expanded DOM path keys.
        /// </summary>
        public HashSet<string> GetExpandedPathsSnapshot() => _state.GetExpandedPathsSnapshot();

        /// <summary>
        /// Clears all nodes and resets the tree state.
        /// </summary>
        public void Clear()
        {
            _state.Clear();
            UpdateScrollBars();
            Invalidate();
        }

        /// <summary>
        /// Clears the current selection without collapsing nodes.
        /// </summary>
        public void ClearSelection()
        {
            if (_state.ClearSelection())
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Atomically applies a background-built <see cref="DomTreeSnapshot"/> to the control without flickering.
        /// </summary>
        public void ApplySnapshot(
            DomTreeSnapshot? snapshot,
            HashSet<string>? expandedKeys = null,
            string? selectedPath = null,
            bool ensureSelectedVisible = false,
            bool raiseAfterSelect = false)
        {
            if (snapshot == null)
            {
                Clear();
                return;
            }

            Rectangle view = GetViewportBounds();
            _state.ApplySnapshot(
                snapshot,
                expandedKeys,
                selectedPath,
                ensureSelectedVisible,
                view.Width,
                view.Height,
                centerVertically: true);

            UpdateScrollBars();
            Invalidate();

            if (raiseAfterSelect && _state.SelectedDomNode != null)
            {
                RaiseAfterSelect(_state.SelectedDomNode, TreeViewAction.Unknown);
            }
        }

        /// <summary>
        /// Applies a coalesced DOM refresh result while preserving the control's existing expanded paths.
        /// </summary>
        public void ApplySnapshotResult(DomSnapshotRefreshResult result, bool raiseAfterSelect = false)
        {
            if (result == null)
            {
                Clear();
                return;
            }

            Rectangle view = GetViewportBounds();
            _state.ApplyRefreshResult(result, view.Width, view.Height);
            UpdateScrollBars();
            Invalidate();

            if (raiseAfterSelect && _state.SelectedDomNode != null)
            {
                RaiseAfterSelect(_state.SelectedDomNode, TreeViewAction.Unknown);
            }
        }

        /// <summary>
        /// Selects and expands to the node matching <paramref name="path"/> in O(1) lookup time with a single repaint.
        /// </summary>
        public bool SelectNodeByPath(string? path, bool ensureVisible = true, bool raiseAfterSelect = false)
        {
            Rectangle view = GetViewportBounds();
            if (!_state.SelectNodeByPath(
                    path,
                    ensureVisible,
                    view.Width,
                    view.Height,
                    centerVertically: true,
                    out _,
                    out _))
            {
                if (string.IsNullOrEmpty(path))
                    Invalidate();
                return false;
            }

            UpdateScrollBars();
            Invalidate();

            if (raiseAfterSelect && _state.SelectedDomNode != null)
            {
                RaiseAfterSelect(_state.SelectedDomNode, TreeViewAction.Unknown);
            }

            return true;
        }

        private DomRowMeasureCache EnsureItemMeasured(Graphics g, DomTreeItem item)
        {
            const TextFormatFlags measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            var cache = _state.EnsureItemMeasured(
                item,
                text => TextRenderer.MeasureText(g, text, _codeFont, Size.Empty, measureFlags).Width,
                out bool widthIncreased);

            if (widthIncreased)
            {
                UpdateScrollBars();
            }

            return cache;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            Rectangle view = GetViewportBounds();
            if (view.Width <= 0 || view.Height <= 0)
            {
                DrawControlBorder(g);
                return;
            }

            Region oldClip = g.Clip;
            g.SetClip(view);

            try
            {
                var rows = _state.VisibleRows;
                if (_state.GetVisibleRowRange(view.Height, out int firstRow, out int lastRow))
                {
                    const TextFormatFlags drawFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping;

                    for (int rowIdx = firstRow; rowIdx <= lastRow; rowIdx++)
                    {
                        var item = rows[rowIdx];
                        var measure = EnsureItemMeasured(g, item);
                        var rowLayout = _state.CalculateRowRenderLayout(
                            rowIdx,
                            item,
                            measure,
                            view.Left,
                            view.Top,
                            view.Right,
                            _codeFont.Height,
                            ShowLines,
                            ShowRootLines,
                            ShowPlusMinus);

                        if (rowLayout.IsSelected || rowLayout.IsHovered)
                        {
                            Rectangle hlRect = new Rectangle(
                                rowLayout.HighlightLeft,
                                rowLayout.HighlightTop,
                                rowLayout.HighlightWidth,
                                rowLayout.HighlightHeight);
                            g.FillRectangle(rowLayout.IsSelected ? _selBgBrush : _hoverBgBrush, hlRect);
                        }

                        if (ShowLines)
                        {
                            foreach (int guideX in _state.EnumerateAncestorGuideLineXs(
                                         item,
                                         ShowLines,
                                         ShowRootLines,
                                         view.Left,
                                         view.Right))
                            {
                                g.DrawLine(_linePen, guideX, rowLayout.RowY, guideX, rowLayout.RowY + _state.ItemHeight);
                            }

                            if (rowLayout.HasBranchConnector)
                            {
                                g.DrawLine(_linePen, rowLayout.BranchLineCenterX, rowLayout.RowY, rowLayout.BranchLineCenterX, rowLayout.CenterY);
                                g.DrawLine(_linePen, rowLayout.BranchLineCenterX, rowLayout.CenterY, rowLayout.BranchHorizontalEndX, rowLayout.CenterY);
                            }
                        }

                        if (rowLayout.ShowExpandGlyph)
                        {
                            Rectangle boxRect = new Rectangle(
                                rowLayout.GlyphBoxLeft,
                                rowLayout.GlyphBoxTop,
                                rowLayout.GlyphBoxDrawSize,
                                rowLayout.GlyphBoxDrawSize);

                            g.FillRectangle(_glyphFillBrush, boxRect);
                            g.DrawRectangle(_glyphBorderPen, boxRect);
                            g.DrawLine(_glyphSignPen, rowLayout.GlyphSignLeft, rowLayout.GlyphSignMidY, rowLayout.GlyphSignRight, rowLayout.GlyphSignMidY);
                            if (!rowLayout.IsExpanded)
                            {
                                g.DrawLine(_glyphSignPen, rowLayout.GlyphSignMidX, rowLayout.GlyphSignTop, rowLayout.GlyphSignMidX, rowLayout.GlyphSignBottom);
                            }
                        }

                        foreach (var token in _state.EnumerateVisibleTokens(item, measure, rowLayout, view.Left, view.Right))
                        {
                            TextRenderer.DrawText(
                                g,
                                token.Text,
                                _codeFont,
                                new Point(token.X, token.Y),
                                GetColorForToken(token.ColorType),
                                drawFlags);
                        }
                    }
                }
            }
            finally
            {
                g.Clip = oldClip;
            }

            var cornerRect = _state.GetScrollCornerBounds(
                ClientSize.Width,
                ClientSize.Height,
                _vScrollBar.Width,
                _hScrollBar.Height,
                _vScrollBar.Visible,
                _hScrollBar.Visible);

            if (!cornerRect.IsEmpty)
            {
                using var cornerBrush = new SolidBrush(BackColor);
                g.FillRectangle(cornerBrush, ToRectangle(cornerRect));
            }

            DrawControlBorder(g);
        }

        private void DrawControlBorder(Graphics g)
        {
            if (_borderStyle == BorderStyle.FixedSingle && Width > 1 && Height > 1)
            {
                g.DrawRectangle(_borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private static Color ToColor((byte R, byte G, byte B) rgb) => Color.FromArgb(rgb.R, rgb.G, rgb.B);

        private static Color GetColorForToken(DomTokenColor tokenColor) => ToColor(tokenColor.GetDefaultRgb());

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Focused && CanFocus)
                Focus();

            Rectangle view = GetViewportBounds();
            if (!_state.HandlePointerDown(
                    e.X,
                    e.Y,
                    e.Button == MouseButtons.Left,
                    view.Left,
                    view.Top,
                    view.Height,
                    out var hitItem,
                    out bool expansionToggled,
                    out bool selectionChanged) || hitItem == null)
            {
                return;
            }

            if (expansionToggled)
            {
                UpdateScrollBars();
                Invalidate();
                return;
            }

            Invalidate();
            if (selectionChanged)
            {
                RaiseAfterSelect(hitItem.Node, TreeViewAction.ByMouse);
            }

            if (e.Button == MouseButtons.Right)
            {
                NodeMouseClick?.Invoke(
                    this,
                    new TreeNodeMouseClickEventArgs(CreateShimTreeNode(hitItem.Node), e.Button, e.Clicks, e.X, e.Y));
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            Rectangle view = GetViewportBounds();
            if (_state.HandlePointerDoubleClick(
                    e.X,
                    e.Y,
                    e.Button == MouseButtons.Left,
                    view.Left,
                    view.Top,
                    view.Height))
            {
                UpdateScrollBars();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Rectangle view = GetViewportBounds();
            if (_state.UpdateHoverAtY(e.Y, view.Top, view.Height))
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_state.ClearHover())
            {
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            Rectangle view = GetViewportBounds();
            bool horizontal = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (_state.ApplyMouseWheel(e.Delta, horizontal, view.Width, view.Height, SystemInformation.MouseWheelScrollLines))
            {
                _updatingScrollBars = true;
                if (horizontal)
                {
                    _hScrollBar.Value = _state.ScrollX;
                }
                else
                {
                    _vScrollBar.Value = _state.ScrollY;
                }
                _updatingScrollBars = false;
                Invalidate();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            DomTreeNavigationKey? navKey = e.KeyCode switch
            {
                Keys.Up => DomTreeNavigationKey.Up,
                Keys.Down => DomTreeNavigationKey.Down,
                Keys.PageUp => DomTreeNavigationKey.PageUp,
                Keys.PageDown => DomTreeNavigationKey.PageDown,
                Keys.Home => DomTreeNavigationKey.Home,
                Keys.End => DomTreeNavigationKey.End,
                Keys.Left => DomTreeNavigationKey.Left,
                Keys.Right => DomTreeNavigationKey.Right,
                _ => null
            };

            if (!navKey.HasValue)
                return;

            Rectangle view = GetViewportBounds();
            if (_state.NavigateByKey(
                    navKey.Value,
                    view.Width,
                    view.Height,
                    out var selectedItem,
                    out bool expandedChanged,
                    out bool selectionChanged,
                    out bool scrollChanged))
            {
                if (expandedChanged || scrollChanged)
                {
                    UpdateScrollBars();
                }

                Invalidate();

                if (selectionChanged && selectedItem != null)
                {
                    RaiseAfterSelect(selectedItem.Node, TreeViewAction.ByKeyboard);
                }

                e.Handled = true;
            }
        }

        private void RaiseAfterSelect(DomNode domNode, TreeViewAction action)
        {
            AfterSelect?.Invoke(this, new TreeViewEventArgs(CreateShimTreeNode(domNode), action));
        }

        private static TreeNode CreateShimTreeNode(DomNode domNode)
        {
            return new TreeNode(domNode.DisplayName)
            {
                Tag = domNode
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _codeFont?.Dispose();
                _selBgBrush?.Dispose();
                _hoverBgBrush?.Dispose();
                _glyphFillBrush?.Dispose();
                _borderPen?.Dispose();
                _linePen?.Dispose();
                _glyphBorderPen?.Dispose();
                _glyphSignPen?.Dispose();
                _vScrollBar?.Dispose();
                _hScrollBar?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}