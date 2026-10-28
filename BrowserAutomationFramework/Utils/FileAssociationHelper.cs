using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace BrowserAutomationFramework.Utils
{
    /// <summary>
    /// Provides Windows shell file-extension association utilities for registering project file types with an application executable.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class FileAssociationHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        /// <summary>
        /// Ensures that the specified file extension is associated with the given ProgID and application executable in the current user's Windows Registry.
        /// </summary>
        /// <param name="extension">The file extension including the leading period (e.g., <c>".baf"</c>).</param>
        /// <param name="progId">The programmatic identifier for the file type.</param>
        /// <param name="description">The human-readable description of the file type.</param>
        /// <param name="applicationFilePath">The full path to the application executable that opens the file.</param>
        public static void EnsureAssociation(string extension, string progId, string description, string applicationFilePath)
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                bool needsUpdate = false;

                using (var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}"))
                {
                    if (key?.GetValue("") as string != progId) needsUpdate = true;
                }

                using (var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command"))
                {
                    string expectedCommand = $"\"{applicationFilePath}\" \"%1\"";
                    if (key?.GetValue("") as string != expectedCommand) needsUpdate = true;
                }

                if (!needsUpdate) return;

                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
                {
                    key.SetValue("", progId);
                }

                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                {
                    key.SetValue("", description);
                }

                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\DefaultIcon"))
                {
                    key.SetValue("", $"\"{applicationFilePath}\",0");
                }

                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command"))
                {
                    key.SetValue("", $"\"{applicationFilePath}\" \"%1\"");
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                // Silently ignore if registry permissions are restricted
            }
        }
    }
}