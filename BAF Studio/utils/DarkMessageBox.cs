using Microsoft.Win32;
using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BrowserAutomationFramework.Models;

namespace BAFStudio.Utils
{
    public static class DarkMessageBox
    {
        #region PInvoke and Structures for Dark Title Bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);

        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        }
        #endregion

        #region Private Fields - Theming Colors
        private static readonly Color DarkModeContentBack = ToColor(StudioTheme.DialogBackground);
        private static readonly Color DarkModeActionBarBack = ToColor(StudioTheme.DialogActionBarBackground);
        private static readonly Color DarkModeFore = ToColor(StudioTheme.BrightForeground);
        private static readonly Color DarkModeButtonBorder = ToColor(StudioTheme.DialogButtonBorder);
        private static readonly Color DefaultButtonBack = ToColor(StudioTheme.DefaultButtonBackground);
        #endregion

        private static Color ToColor((byte R, byte G, byte B) rgb) => Color.FromArgb(rgb.R, rgb.G, rgb.B);

        #region Public Show Methods (Mirrors System.Windows.Forms.MessageBox)
        // All 12 overloads are included for drop-in replacement
        public static DialogResult Show(string text) => ShowMessageBox(null, text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
        public static DialogResult Show(string text, string caption) => ShowMessageBox(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) => ShowMessageBox(null, text, caption, buttons, MessageBoxIcon.None);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => ShowMessageBox(null, text, caption, buttons, icon);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, Icon ico) => ShowMessageBox(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, ico);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => ShowMessageBox(null, text, caption, buttons, icon, defaultButton);
        public static DialogResult Show(Form owner, string text) => ShowMessageBox(owner, text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
        public static DialogResult Show(Form owner, string text, string caption) => ShowMessageBox(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        public static DialogResult Show(Form owner, string text, string caption, MessageBoxButtons buttons) => ShowMessageBox(owner, text, caption, buttons, MessageBoxIcon.None);
        public static DialogResult Show(Form owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => ShowMessageBox(owner, text, caption, buttons, icon);
        public static DialogResult Show(Form owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => ShowMessageBox(owner, text, caption, buttons, icon, defaultButton);
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options) => ShowMessageBox(null, text, caption, buttons, icon, defaultButton, null, options);
        public static DialogResult Show(Form owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options) => ShowMessageBox(owner, text, caption, buttons, icon, defaultButton, null, options);
        #endregion

        #region Core Logic

        private static bool IsSystemInDarkMode()
        {
            try
            {
                return (int?)Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme") == 0;
            }
            catch { return false; }
        }

        private static DialogResult ShowMessageBox(Form? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, Icon? ico = null, MessageBoxOptions options = 0)
        {
            using (var msgBox = new Form())
            {
                // Prevent WinForms from double-scaling our manually calculated pixel values
                msgBox.AutoScaleMode = AutoScaleMode.None;

                // Calculate the DPI scaling factor (e.g., 1.75 for 175% scaling)
                float scaleFactor = 1.0f;
                using (Graphics g = msgBox.CreateGraphics())
                {
                    scaleFactor = g.DpiX / 96f;
                }

                // Helper function to scale hardcoded pixel values
                int Scale(int value) => (int)(value * scaleFactor);

                msgBox.Text = caption;
                msgBox.BackColor = DarkModeContentBack;
                msgBox.ForeColor = DarkModeFore;
                msgBox.FormBorderStyle = FormBorderStyle.FixedDialog;
                msgBox.StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
                msgBox.MaximizeBox = false;
                msgBox.MinimizeBox = false;
                msgBox.ShowInTaskbar = false;
                msgBox.Font = SystemFonts.MessageBoxFont;

                if (owner != null && owner.Icon != null)
                {
                    msgBox.Icon = owner.Icon;
                }

                if (ico != null)
                {
                    msgBox.Icon = ico;
                }

                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(msgBox.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));

                var actionBarPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    Dock = DockStyle.Bottom,
                    BackColor = DarkModeActionBarBack,
                    Height = Scale(50),
                    Padding = new Padding(0, Scale(10), Scale(8), Scale(10))
                };

                var contentPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = DarkModeContentBack,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(Scale(24), Scale(24), Scale(24), Scale(12)),
                    ColumnStyles = { new ColumnStyle(SizeType.AutoSize), new ColumnStyle(SizeType.Percent, 100F) }
                };

                msgBox.Controls.Add(contentPanel);
                msgBox.Controls.Add(actionBarPanel);

                PictureBox? iconPictureBox = AddIcon(contentPanel, icon, Scale);
                if (iconPictureBox != null)
                {
                    iconPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                }

                var messageLabel = new Label
                {
                    Text = text,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.TopLeft,
                    AutoSize = false,
                    Margin = new Padding(Scale(10), Scale(10), 0, 0)
                };
                contentPanel.Controls.Add(messageLabel, 1, 0);

                AddButtons(actionBarPanel, buttons, defaultButton, msgBox, Scale);

                // --- SCALED SIZING LOGIC ---
                int minWidth = Scale(350);
                int maxWidth = Scale(700); // Prevent ultra-wide boxes on 4K screens

                int buttonsWidth = actionBarPanel.Controls.Cast<Control>().Sum(c => c.Width + c.Margin.Left + c.Margin.Right) + actionBarPanel.Padding.Horizontal;

                // Measure raw text width on a single line
                int rawTextWidth = TextRenderer.MeasureText(text, msgBox.Font).Width;
                int textAndIconWidth = rawTextWidth + (iconPictureBox?.Width ?? 0) + contentPanel.Padding.Horizontal + messageLabel.Margin.Horizontal;

                // Bound the final width
                int finalWidth = Math.Max(minWidth, Math.Max(buttonsWidth, Math.Min(textAndIconWidth, maxWidth)));

                int labelWidth = finalWidth - contentPanel.Padding.Horizontal - (iconPictureBox?.Width ?? 0) - messageLabel.Margin.Horizontal;
                Size textSize = TextRenderer.MeasureText(text, msgBox.Font, new Size(labelWidth, 0), TextFormatFlags.WordBreak);

                int contentHeight = Math.Max(iconPictureBox?.Height ?? 0, textSize.Height + messageLabel.Margin.Vertical) + contentPanel.Padding.Vertical;

                msgBox.ClientSize = new Size(finalWidth, contentHeight + actionBarPanel.Height);

                ReThemer.ThemeAllControls(msgBox);

                PlayIconSound(icon);

                return owner == null ? msgBox.ShowDialog() : msgBox.ShowDialog(owner);
            }
        }

        private static PictureBox? AddIcon(TableLayoutPanel panel, MessageBoxIcon icon, Func<int, int> scale)
        {
            if (icon == MessageBoxIcon.None)
            {
                panel.ColumnStyles[0].Width = 0;
                panel.Padding = new Padding(scale(12), scale(24), scale(24), scale(12));
                return null;
            }

            Icon systemIcon;
            switch (icon)
            {
                case MessageBoxIcon.Information: systemIcon = SystemIcons.Information; break;
                case MessageBoxIcon.Question: systemIcon = SystemIcons.Question; break;
                case MessageBoxIcon.Warning: systemIcon = SystemIcons.Warning; break;
                case MessageBoxIcon.Error: systemIcon = SystemIcons.Error; break;
                default:
                panel.ColumnStyles[0].Width = 0;
                panel.Padding = new Padding(scale(12), scale(24), scale(24), scale(12));
                return null;
            }

            var iconPictureBox = new PictureBox
            {
                Image = new Bitmap(systemIcon.ToBitmap(), new Size(scale(32), scale(32))),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(scale(40), scale(40)),
                Dock = DockStyle.Top
            };
            panel.Controls.Add(iconPictureBox, 0, 0);
            return iconPictureBox;
        }

        private static void AddButtons(FlowLayoutPanel panel, MessageBoxButtons buttons, MessageBoxDefaultButton defaultButton, Form parentForm, Func<int, int> scale)
        {
            Button CreateButton(string text, DialogResult result, bool isDefault)
            {
                var button = new Button
                {
                    Text = text,
                    DialogResult = result,
                    FlatStyle = FlatStyle.System,
                    MinimumSize = new Size(scale(85), scale(25)),
                    Margin = new Padding(scale(5), 0, 0, 0),
                    AutoSize = true
                };

                button.FlatAppearance.BorderColor = DarkModeButtonBorder;
                button.FlatAppearance.BorderSize = 1;

                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(button.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));

                if (isDefault)
                {
                    button.BackColor = DefaultButtonBack;
                    button.ForeColor = DarkModeFore;
                    parentForm.AcceptButton = button;
                }
                else
                {
                    button.BackColor = panel.BackColor;
                    button.ForeColor = DarkModeFore;
                }

                button.Click += (sender, e) => { parentForm.Close(); };
                panel.Controls.Add(button);
                return button;
            }

            switch (buttons)
            {
                case MessageBoxButtons.OK: CreateButton("OK", DialogResult.OK, true); break;
                case MessageBoxButtons.OKCancel: CreateButton("Cancel", DialogResult.Cancel, defaultButton == MessageBoxDefaultButton.Button2); CreateButton("OK", DialogResult.OK, defaultButton == MessageBoxDefaultButton.Button1); break;
                case MessageBoxButtons.YesNo: CreateButton("No", DialogResult.No, defaultButton == MessageBoxDefaultButton.Button2); CreateButton("Yes", DialogResult.Yes, defaultButton == MessageBoxDefaultButton.Button1); break;
                case MessageBoxButtons.YesNoCancel: CreateButton("Cancel", DialogResult.Cancel, defaultButton == MessageBoxDefaultButton.Button3); CreateButton("No", DialogResult.No, defaultButton == MessageBoxDefaultButton.Button2); CreateButton("Yes", DialogResult.Yes, defaultButton == MessageBoxDefaultButton.Button1); break;
                case MessageBoxButtons.RetryCancel: CreateButton("Cancel", DialogResult.Cancel, defaultButton == MessageBoxDefaultButton.Button2); CreateButton("Retry", DialogResult.Retry, defaultButton == MessageBoxDefaultButton.Button1); break;
                case MessageBoxButtons.AbortRetryIgnore: CreateButton("Ignore", DialogResult.Ignore, defaultButton == MessageBoxDefaultButton.Button3); CreateButton("Retry", DialogResult.Retry, defaultButton == MessageBoxDefaultButton.Button2); CreateButton("Abort", DialogResult.Abort, defaultButton == MessageBoxDefaultButton.Button1); break;
            }
        }

        private static void PlayIconSound(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Information: SystemSounds.Asterisk.Play(); break;
                case MessageBoxIcon.Question: SystemSounds.Question.Play(); break;
                case MessageBoxIcon.Warning: SystemSounds.Exclamation.Play(); break;
                case MessageBoxIcon.Error: SystemSounds.Hand.Play(); break;
            }
        }

        #endregion
    }
}