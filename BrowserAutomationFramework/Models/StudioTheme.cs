namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Defines the shared RGB color palette used by the Automation Studio UI.
    /// </summary>
    public static class StudioTheme
    {
        /// <summary>
        /// Gets the primary dark background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DarkBackground = (30, 30, 30);

        /// <summary>
        /// Gets the dark surface/border RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DarkSurface = (45, 45, 48);

        /// <summary>
        /// Gets the selected-row background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) SelectionBackground = (9, 71, 113);

        /// <summary>
        /// Gets the disabled text RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DisabledText = (127, 127, 127);

        /// <summary>
        /// Gets the standard foreground text RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) Foreground = (211, 211, 211);

        /// <summary>
        /// Gets the bright foreground text RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) BrightForeground = (255, 255, 255);

        /// <summary>
        /// Gets the dark message-box content background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DialogBackground = (43, 43, 43);

        /// <summary>
        /// Gets the dark message-box action-bar background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DialogActionBarBackground = (32, 32, 32);

        /// <summary>
        /// Gets the dark message-box button border RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DialogButtonBorder = (80, 80, 80);

        /// <summary>
        /// Gets the default dialog button background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) DefaultButtonBackground = (0, 120, 215);

        /// <summary>
        /// Gets the dark title-bar background RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) TitleBarBackground = (32, 32, 32);

        /// <summary>
        /// Gets the active title-bar text RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) TitleBarActiveText = (192, 192, 192);

        /// <summary>
        /// Gets the inactive title-bar text RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) TitleBarInactiveText = (102, 102, 102);

        /// <summary>
        /// Gets the success/connected status RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) SuccessStatus = (50, 205, 50);

        /// <summary>
        /// Gets the warning/disconnected status RGB color.
        /// </summary>
        public static readonly (byte R, byte G, byte B) WarningStatus = (255, 165, 0);
    }
}
