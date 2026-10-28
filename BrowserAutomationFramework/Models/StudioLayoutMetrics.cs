using System;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Provides DPI-aware proportional splitter distances for the Automation Studio panel layout.
    /// </summary>
    public static class StudioLayoutMetrics
    {
        /// <summary>Gets the left sidebar vertical split ratio.</summary>
        public const double LeftSidebarRatio = 0.58;

        /// <summary>Gets the DOM tab horizontal split ratio.</summary>
        public const double DomRatio = 0.45;

        /// <summary>Gets the editor sequences panel ratio.</summary>
        public const double SequencesRatio = 0.28;

        /// <summary>Gets the editor details panel ratio.</summary>
        public const double EditorRatio = 0.36;

        /// <summary>Gets the scraper main panel ratio.</summary>
        public const double ScraperMainRatio = 0.32;

        /// <summary>Gets the scraper right panel ratio.</summary>
        public const double ScraperRightRatio = 0.52;

        /// <summary>Gets the scraper fields panel ratio.</summary>
        public const double FieldsRatio = 0.35;

        /// <summary>Gets the reverse-engineering main panel ratio.</summary>
        public const double ReverseEngineeringMainRatio = 0.34;

        /// <summary>Gets the reverse-engineering resource panel ratio.</summary>
        public const double ReverseEngineeringLeftRatio = 0.65;

        /// <summary>
        /// Calculates the left sidebar splitter distance.
        /// </summary>
        public static int CalculateLeftSidebarSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, LeftSidebarRatio, 180, 120, 0, 190, dpiScale);
        }

        /// <summary>
        /// Calculates the DOM tab splitter distance.
        /// </summary>
        public static int CalculateDomSplitterDistance(int totalSize)
        {
            return (int)(totalSize * DomRatio);
        }

        /// <summary>
        /// Calculates the editor sequences splitter distance.
        /// </summary>
        public static int CalculateSequencesSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, SequencesRatio, 220, 0, 380, 0, dpiScale);
        }

        /// <summary>
        /// Calculates the editor details splitter distance.
        /// </summary>
        public static int CalculateEditorSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, EditorRatio, 180, 0, 420, 0, dpiScale);
        }

        /// <summary>
        /// Calculates the scraper main splitter distance.
        /// </summary>
        public static int CalculateScraperMainSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, ScraperMainRatio, 260, 0, 440, 0, dpiScale);
        }

        /// <summary>
        /// Calculates the scraper right splitter distance.
        /// </summary>
        public static int CalculateScraperRightSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, ScraperRightRatio, 160, 140, 0, 170, dpiScale);
        }

        /// <summary>
        /// Calculates the scraper fields splitter distance.
        /// </summary>
        public static int CalculateFieldsSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, FieldsRatio, 180, 0, 400, 0, dpiScale);
        }

        /// <summary>
        /// Calculates the reverse-engineering main splitter distance.
        /// </summary>
        public static int CalculateReverseEngineeringMainSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, ReverseEngineeringMainRatio, 260, 0, 440, 0, dpiScale);
        }

        /// <summary>
        /// Calculates the reverse-engineering resource splitter distance.
        /// </summary>
        public static int CalculateReverseEngineeringLeftSplitterDistance(int totalSize, float dpiScale)
        {
            return CalculateClamped(totalSize, ReverseEngineeringLeftRatio, 150, 110, 0, 160, dpiScale);
        }

        private static int CalculateClamped(
            int totalSize,
            double ratio,
            int minFirstPanelPx,
            int minSecondPanelPx,
            int maxFirstPanelPx,
            int minimumMaximumPx,
            float dpiScale)
        {
            float safeScale = dpiScale > 0f ? dpiScale : 1f;
            int Scale(int value) => (int)Math.Ceiling(value * safeScale);

            int minFirst = Scale(minFirstPanelPx);
            int maxFirst = maxFirstPanelPx > 0
                ? Scale(maxFirstPanelPx)
                : Math.Max(Scale(minimumMaximumPx), totalSize - Scale(minSecondPanelPx));

            int safeUpperLimit = Math.Max(25, totalSize - 30);
            if (totalSize >= 55)
            {
                maxFirst = Math.Min(maxFirst, safeUpperLimit);
                minFirst = Math.Min(minFirst, safeUpperLimit);
            }

            if (maxFirst < minFirst)
            {
                maxFirst = minFirst;
            }

            return Math.Clamp((int)(totalSize * ratio), minFirst, maxFirst);
        }
    }
}
