using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScintillaNET;

namespace CymaticLabs.InfluxDB.Studio
{
    /// <summary>
    /// Extends WinForms' built-in dark mode to controls it doesn't reach on its own: the
    /// Scintilla-based SQL editors, and ListView's native grid lines. Also makes a theme change
    /// take effect live, without recreating the main window.
    /// </summary>
    internal static class AppTheme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attribute, ref int value, int valueSize);

        // DWMWA_USE_IMMERSIVE_DARK_MODE - stable since Windows 10 20H1 / Windows 11.
        private const int DwmwaUseImmersiveDarkMode = 20;

        /// <summary>
        /// Maps the persisted theme setting to a WinForms color mode.
        /// </summary>
        public static SystemColorMode GetColorMode(string theme)
        {
            switch (theme)
            {
                case AppSettings.ThemeLight: return SystemColorMode.Classic;
                case AppSettings.ThemeDark: return SystemColorMode.Dark;
                default: return SystemColorMode.System;
            }
        }

        /// <summary>
        /// Applies a theme change to every open window, live: WinForms' own SystemColors-based
        /// controls pick up <see cref="Application.SetColorMode"/> immediately, but a window's
        /// title bar is only themed at creation time, and some controls (see <see cref="RefreshTree"/>)
        /// cache the mode at handle-creation time too - so those are handled explicitly here.
        /// Covers every form in <see cref="Application.OpenForms"/>, not just the active one -
        /// dialogs that were already shown once (e.g. the connections manager, auto-shown at
        /// startup) are separate top-level windows with their own stale handles. Call after
        /// <see cref="Application.SetColorMode"/>.
        /// </summary>
        public static void ApplyLiveThemeChange()
        {
            var useDark = Application.IsDarkModeEnabled ? 1 : 0;

            foreach (Form form in Application.OpenForms)
            {
                if (form.IsHandleCreated)
                {
                    DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
                }

                RefreshTree(form);
            }
        }

        // Control.RecreateHandle() - protected, so it's only reachable via reflection from here.
        private static readonly MethodInfo RecreateHandleMethod =
            typeof(Control).GetMethod("RecreateHandle", BindingFlags.NonPublic | BindingFlags.Instance);

        // Recursively fixes up every control so it reflects the new color mode:
        //  - TreeView/ListView/TabControl cache whether dark mode applies at handle-creation time
        //    as part of their built-in dark-mode support, so a live mode change needs their handle
        //    recreated (safe: WinForms replays their Nodes/Items/TabPages into the new handle,
        //    nothing is lost).
        //  - Button also renders natively (via visual styles) and, unlike the above, recreating
        //    its handle does not pick up the new mode - not even recreating the whole parent
        //    form's handle does. Forcing flat style with explicit colors sidesteps native
        //    rendering entirely, trading the native 3D look for one that reliably follows theme.
        //  - Scintilla bakes in a fixed RGB when a color is set rather than tracking the color
        //    mode, so its styles need to be re-applied explicitly.
        //  - Everything else (Panel, Label, MenuStrip, ToolStrip, our own owner-drawn
        //    ExtendedTabControl, ...) already reads SystemColors fresh on every paint and just
        //    needs a repaint.
        private static void RefreshTree(Control control)
        {
            if (control is Scintilla scintilla)
            {
                ApplySqlEditorTheme(scintilla);
            }
            else if (control is Button button)
            {
                ApplyButtonTheme(button);
            }
            else if ((control is TreeView || control is ListView || control is TabControl) && control.IsHandleCreated)
            {
                RecreateHandleMethod?.Invoke(control, null);
            }

            control.Invalidate(true);

            foreach (Control child in control.Controls) RefreshTree(child);
        }

        /// <summary>
        /// Applies theme-aware colors to a <see cref="Button"/>. Buttons render via native visual
        /// styles, which - unlike most other controls - do not pick up a live color mode change no
        /// matter how the control's handle is recreated, so flat style with explicit colors is
        /// used instead of the native look whenever dark mode is active.
        /// </summary>
        public static void ApplyButtonTheme(Button button)
        {
            if (Application.IsDarkModeEnabled)
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = SystemColors.Control;
                button.ForeColor = SystemColors.ControlText;
                button.FlatAppearance.BorderColor = SystemColors.ControlDark;
            }
            else
            {
                button.FlatStyle = FlatStyle.System;
                button.UseVisualStyleBackColor = true;
            }
        }

        /// <summary>
        /// Replaces a <see cref="ListView"/>'s native <see cref="ListView.GridLines"/> with
        /// manually-drawn ones. GridLines paints with a fixed native color that WinForms' dark
        /// mode does not touch, so it stays a harsh light gray and dominates a dark background
        /// regardless of theme. Owner-draws the list (falling back to default painting for
        /// everything else) so the grid lines can use a theme-aware, subdued color instead.
        /// </summary>
        public static void ApplyListViewGridTheme(ListView listView)
        {
            listView.GridLines = false;
            listView.OwnerDraw = true;
            listView.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            listView.DrawItem += (s, e) => e.DrawDefault = true;
            listView.DrawSubItem += (s, e) => e.DrawDefault = true;

            listView.Paint += (s, e) =>
            {
                using (var pen = new Pen(SystemColors.ControlDark))
                {
                    var x = 0;

                    foreach (ColumnHeader col in listView.Columns)
                    {
                        x += col.Width;
                        e.Graphics.DrawLine(pen, x, 0, x, listView.ClientSize.Height);
                    }

                    foreach (ListViewItem item in listView.Items)
                    {
                        e.Graphics.DrawLine(pen, 0, item.Bounds.Bottom, listView.ClientSize.Width, item.Bounds.Bottom);
                    }
                }
            };
        }

        /// <summary>
        /// Applies theme-aware colors to a SQL Scintilla editor, including base colors and SQL
        /// token colors. Scintilla is a native control that WinForms' color mode does not reach,
        /// and the light-mode syntax colors (plain blue/red/magenta) are unreadable on a dark
        /// background, so dark mode gets its own palette instead of reusing the light one.
        /// </summary>
        public static void ApplySqlEditorTheme(Scintilla editor)
        {
            editor.StyleResetDefault();
            editor.Styles[Style.Default].BackColor = SystemColors.Window;
            editor.Styles[Style.Default].ForeColor = SystemColors.WindowText;
            editor.StyleClearAll();

            editor.BackColor = SystemColors.Window;
            editor.CaretForeColor = SystemColors.WindowText;
            editor.SelectionBackColor = SystemColors.Highlight;
            editor.SelectionTextColor = SystemColors.HighlightText;

            if (Application.IsDarkModeEnabled)
            {
                editor.Styles[Style.Sql.Identifier].ForeColor = Color.FromArgb(86, 156, 214);
                editor.Styles[Style.Sql.String].ForeColor = Color.FromArgb(214, 157, 133);
                editor.Styles[Style.Sql.Number].ForeColor = Color.FromArgb(181, 206, 168);
                editor.Styles[Style.Sql.QuotedIdentifier].ForeColor = Color.FromArgb(214, 157, 133);
            }
            else
            {
                editor.Styles[Style.Sql.Identifier].ForeColor = Color.Blue;
                editor.Styles[Style.Sql.String].ForeColor = Color.Red;
                editor.Styles[Style.Sql.Number].ForeColor = Color.Magenta;
                editor.Styles[Style.Sql.QuotedIdentifier].ForeColor = Color.Red;
            }
        }
    }
}
