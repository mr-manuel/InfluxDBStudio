using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScintillaNET;

namespace CymaticLabs.InfluxDB.Studio
{
    /// <summary>
    /// Flat, modern rendering for every MenuStrip/ToolStrip/StatusStrip/ContextMenuStrip in the
    /// app (wired once via <see cref="ToolStripManager.Renderer"/> in Program.cs). Replaces the
    /// default <see cref="ToolStripProfessionalRenderer"/>'s gradients/bevels with flat fills,
    /// while still deriving every color from <see cref="SystemColors"/> - like the rest of
    /// AppTheme, this keeps it correct in light, dark and system color modes without any
    /// mode-specific branching here.
    /// </summary>
    internal sealed class AppToolStripRenderer : ToolStripProfessionalRenderer
    {
        public AppToolStripRenderer() : base(new AppToolStripColorTable())
        {
            RoundedEdges = false;
        }
    }

    file sealed class AppToolStripColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => SystemColors.Control;
        public override Color ToolStripGradientMiddle => SystemColors.Control;
        public override Color ToolStripGradientEnd => SystemColors.Control;

        public override Color MenuStripGradientBegin => SystemColors.Control;
        public override Color MenuStripGradientEnd => SystemColors.Control;

        public override Color ImageMarginGradientBegin => SystemColors.Control;
        public override Color ImageMarginGradientMiddle => SystemColors.Control;
        public override Color ImageMarginGradientEnd => SystemColors.Control;

        public override Color ToolStripContentPanelGradientBegin => SystemColors.Control;
        public override Color ToolStripContentPanelGradientEnd => SystemColors.Control;

        public override Color ToolStripPanelGradientBegin => SystemColors.Control;
        public override Color ToolStripPanelGradientEnd => SystemColors.Control;

        public override Color OverflowButtonGradientBegin => SystemColors.Control;
        public override Color OverflowButtonGradientMiddle => SystemColors.Control;
        public override Color OverflowButtonGradientEnd => SystemColors.Control;

        public override Color ButtonSelectedGradientBegin => SystemColors.ControlLight;
        public override Color ButtonSelectedGradientMiddle => SystemColors.ControlLight;
        public override Color ButtonSelectedGradientEnd => SystemColors.ControlLight;
        public override Color ButtonSelectedHighlight => SystemColors.ControlLight;
        public override Color ButtonSelectedHighlightBorder => SystemColors.ControlDark;

        public override Color ButtonPressedGradientBegin => SystemColors.ControlDark;
        public override Color ButtonPressedGradientMiddle => SystemColors.ControlDark;
        public override Color ButtonPressedGradientEnd => SystemColors.ControlDark;
        public override Color ButtonPressedHighlight => SystemColors.ControlDark;
        public override Color ButtonPressedHighlightBorder => SystemColors.ControlDark;

        public override Color ButtonCheckedGradientBegin => SystemColors.ControlLight;
        public override Color ButtonCheckedGradientMiddle => SystemColors.ControlLight;
        public override Color ButtonCheckedGradientEnd => SystemColors.ControlLight;
        public override Color ButtonCheckedHighlight => SystemColors.ControlLight;
        public override Color ButtonCheckedHighlightBorder => SystemColors.ControlDark;

        public override Color MenuItemSelected => SystemColors.ControlLight;
        public override Color MenuItemSelectedGradientBegin => SystemColors.ControlLight;
        public override Color MenuItemSelectedGradientEnd => SystemColors.ControlLight;
        public override Color MenuItemPressedGradientBegin => SystemColors.ControlDark;
        public override Color MenuItemPressedGradientEnd => SystemColors.ControlDark;
        public override Color MenuItemBorder => SystemColors.ControlDark;
        public override Color MenuBorder => SystemColors.ControlDark;

        public override Color SeparatorDark => SystemColors.ControlDark;
        public override Color SeparatorLight => SystemColors.Control;

        public override Color ToolStripBorder => SystemColors.ControlDark;
        public override Color GripDark => SystemColors.ControlDark;
        public override Color GripLight => SystemColors.Control;

        public override Color StatusStripGradientBegin => SystemColors.Control;
        public override Color StatusStripGradientEnd => SystemColors.Control;
    }

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
        /// title bar needs the explicit <see cref="DwmSetWindowAttribute"/> call re-applied (see
        /// <see cref="ApplyDarkTitleBar"/>), and some controls (see <see cref="RefreshTree"/>)
        /// cache the mode at handle-creation time too - so those are handled explicitly here.
        /// Covers every form in <see cref="Application.OpenForms"/>, not just the active one -
        /// dialogs that were already shown once (e.g. the connections manager, auto-shown at
        /// startup) are separate top-level windows with their own stale handles. Note this only
        /// reaches non-modal/currently-visible forms - a modal dialog isn't in that collection
        /// while it's not showing, which is why every dialog also wires <see cref="ApplyDarkTitleBar"/>
        /// itself, so its title bar is correct the moment it's shown regardless of what the theme
        /// was when the app started. Call after <see cref="Application.SetColorMode"/>.
        /// </summary>
        public static void ApplyLiveThemeChange()
        {
            foreach (Form form in Application.OpenForms)
            {
                ApplyDarkTitleBarNow(form);
                RefreshTree(form);
            }
        }

        /// <summary>
        /// Keeps a window's native title bar in sync with the current color mode. Unlike
        /// SystemColors-based client-area rendering, a title bar is DWM/non-client chrome that
        /// WinForms does not automatically re-theme for every window - reliably only at that
        /// window's own handle-creation time, via the explicit call this wires up. That covers a
        /// dialog shown for the first time at any point (not just ones open at app startup), which
        /// <see cref="ApplyLiveThemeChange"/>'s <see cref="Application.OpenForms"/> walk cannot
        /// reach on its own: a modal dialog (<see cref="Form.ShowDialog()"/>) is only in that
        /// collection while actually showing, and the user can't be switching theme from the main
        /// window's menu while a modal child has focus - so without this, a dialog created after
        /// startup would only ever get whatever title bar mode existed when its handle happened to
        /// be created, not necessarily the theme active when it's actually shown. Call once, from
        /// a dialog's constructor.
        /// </summary>
        public static void ApplyDarkTitleBar(Form form)
        {
            form.HandleCreated += (s, e) => ApplyDarkTitleBarNow(form);
            if (form.IsHandleCreated) ApplyDarkTitleBarNow(form);
        }

        private static void ApplyDarkTitleBarNow(Form form)
        {
            if (!form.IsHandleCreated) return;

            var useDark = Application.IsDarkModeEnabled ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
        }

        // Control.RecreateHandle() - protected, so it's only reachable via reflection from here.
        private static readonly MethodInfo RecreateHandleMethod =
            typeof(Control).GetMethod("RecreateHandle", BindingFlags.NonPublic | BindingFlags.Instance);

        // Recursively fixes up every control so it reflects the new color mode:
        //  - TreeView/ListView/TabControl cache whether dark mode applies at handle-creation time
        //    as part of their built-in dark-mode support, so a live mode change needs their handle
        //    recreated (safe: WinForms replays their Nodes/Items/TabPages into the new handle,
        //    nothing is lost).
        //  - Scintilla bakes in a fixed RGB when a color is set rather than tracking the color
        //    mode, so its styles need to be re-applied explicitly.
        //  - Everything else (Panel, Label, MenuStrip, ToolStrip, Button once flattened via
        //    ApplyButtonsTheme, our own owner-drawn ExtendedTabControl, ...) already reads
        //    SystemColors fresh on every paint and just needs a repaint.
        private static void RefreshTree(Control control)
        {
            if (control is Scintilla scintilla)
            {
                ApplySqlEditorTheme(scintilla);
            }
            else if ((control is TreeView || control is ListView || control is TabControl) && control.IsHandleCreated)
            {
                RecreateHandleMethod?.Invoke(control, null);
            }

            control.Invalidate(true);

            foreach (Control child in control.Controls) RefreshTree(child);
        }

        /// <summary>
        /// Flattens every <see cref="Button"/> in a control tree (a form/dialog, called once from
        /// its constructor). Buttons default to native visual-style rendering, which - unlike most
        /// other controls - does not track a live color mode change at all (not even via handle
        /// recreation): it would render correctly for the mode the app started in, but not after
        /// switching theme without restarting. Flat style with <see cref="SystemColors"/> sidesteps
        /// native rendering entirely, so buttons behave like the rest of the flattened chrome
        /// (menus, toolbars) - a plain repaint is enough to follow a live theme change, no reactive
        /// re-application needed. A disabled button is a separate problem: WinForms' built-in
        /// disabled-button text is drawn "etched" - twice, offset by a pixel, in two fixed shades -
        /// which reads fine on a light face but loses almost all contrast on a dark one, and does
        /// not honor <see cref="Control.ForeColor"/> at all (it computes its own colors), so
        /// setting ForeColor cannot fix it. Repainting the whole disabled button here, the same way
        /// the ListView header/hover paint is done, replaces that unreadable native rendering
        /// outright instead of trying to work around it.
        /// </summary>
        public static void ApplyButtonsTheme(Control root)
        {
            if (root is Button button)
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = SystemColors.Control;
                button.ForeColor = SystemColors.ControlText;
                button.FlatAppearance.BorderColor = SystemColors.ControlDark;
                button.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
                button.FlatAppearance.MouseDownBackColor = SystemColors.ControlDark;

                button.Paint += (s, e) =>
                {
                    if (button.Enabled) return;

                    using (var backBrush = new SolidBrush(button.BackColor))
                    {
                        e.Graphics.FillRectangle(backBrush, button.ClientRectangle);
                    }

                    using (var pen = new Pen(SystemColors.ControlDark))
                    {
                        e.Graphics.DrawRectangle(pen, Rectangle.Inflate(button.ClientRectangle, -1, -1));
                    }

                    TextRenderer.DrawText(e.Graphics, button.Text, button.Font, button.ClientRectangle, SystemColors.GrayText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                };
            }

            foreach (Control child in root.Controls) ApplyButtonsTheme(child);
        }

        // How much more breathing room dialogs get on top of their normal AutoScaleMode.Font
        // scaling (see ApplyModernSpacing). 1.0 = untouched.
        private const float ModernSpacingScale = 1.12f;

        /// <summary>
        /// Gives a dialog a bit more breathing room than its hand-positioned Designer layout has
        /// by default. Every dialog in the app uses absolute Location/Size coordinates (no
        /// TableLayoutPanel anywhere) with <see cref="Form.AutoScaleMode"/> set to
        /// <see cref="AutoScaleMode.Font"/> - which already rescales that whole layout
        /// proportionally to match the current font's actual metrics (see the Segoe UI switch in
        /// Program.cs) before this ever runs. Rewriting every control's coordinates by hand across
        /// ~15 dialogs to add more padding would be substantial, hard-to-verify surgery for a
        /// cosmetic want. <see cref="Control.Scale(SizeF)"/> gets a similar effect for free and
        /// risk-free instead: it multiplies every child control's position, size, and font by a
        /// fixed factor, so the relative layout (nothing overlaps anything else) is preserved
        /// exactly - just uniformly larger, with proportionally bigger gaps and click targets.
        /// Call once, from a dialog's constructor, right after InitializeComponent().
        /// </summary>
        public static void ApplyModernSpacing(Form dialog)
        {
            dialog.Scale(new SizeF(ModernSpacingScale, ModernSpacingScale));
        }

        /// <summary>
        /// Replaces a <see cref="ListView"/>'s native <see cref="ListView.GridLines"/> and column
        /// header painting with manually-drawn versions, and adds a row hover highlight. GridLines
        /// paints with a fixed native color that WinForms' dark mode does not touch, so it stays a
        /// harsh light gray and dominates a dark background regardless of theme. The column header
        /// is worse: it's a distinct native child window (SysHeader32) that WinForms' dark mode
        /// support does not reach at all, so it keeps painting black text on a light background
        /// even once the rest of the list has gone dark - unreadable. <see cref="SetWindowTheme"/>
        /// with Explorer's "DarkMode_ItemsView" pseudo-theme (the usual trick for this) does not
        /// reliably recolor the header's text on every Windows build, so instead the header is
        /// owner-drawn from scratch here with theme-aware colors, the same approach already used
        /// for the grid lines and for everything else <see cref="RefreshTree"/> just repaints. The
        /// hover highlight is purely cosmetic (native ListView has none), added the same way.
        /// </summary>
        public static void ApplyListViewGridTheme(ListView listView)
        {
            listView.GridLines = false;
            listView.OwnerDraw = true;
            listView.DrawItem += (s, e) => e.DrawDefault = true;

            var hoveredIndex = -1;

            void SetHovered(int index)
            {
                if (hoveredIndex == index) return;
                var previous = hoveredIndex;
                hoveredIndex = index;
                if (previous >= 0 && previous < listView.Items.Count) listView.Invalidate(listView.Items[previous].Bounds);
                if (index >= 0 && index < listView.Items.Count) listView.Invalidate(listView.Items[index].Bounds);
            }

            listView.MouseMove += (s, e) => SetHovered(listView.HitTest(e.Location).Item?.Index ?? -1);
            listView.MouseLeave += (s, e) => SetHovered(-1);

            listView.DrawSubItem += (s, e) =>
            {
                if (e.ItemIndex != hoveredIndex || e.Item.Selected)
                {
                    e.DrawDefault = true;
                    return;
                }

                using (var hoverBrush = new SolidBrush(SystemColors.ControlLight))
                {
                    e.Graphics.FillRectangle(hoverBrush, e.Bounds);
                }

                var textBounds = Rectangle.Inflate(e.Bounds, -3, 0);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, textBounds, SystemColors.ControlText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Left);
            };

            listView.DrawColumnHeader += (s, e) =>
            {
                using (var backBrush = new SolidBrush(SystemColors.Control))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }

                using (var pen = new Pen(SystemColors.ControlDark))
                {
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                }

                var textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
                textFlags |= e.Header.TextAlign switch
                {
                    HorizontalAlignment.Center => TextFormatFlags.HorizontalCenter,
                    HorizontalAlignment.Right => TextFormatFlags.Right,
                    _ => TextFormatFlags.Left,
                };

                var textBounds = Rectangle.Inflate(e.Bounds, -4, 0);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, textBounds, SystemColors.ControlText, textFlags);
            };

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
