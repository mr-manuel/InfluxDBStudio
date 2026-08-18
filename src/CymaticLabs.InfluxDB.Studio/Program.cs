using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CymaticLabs.InfluxDB.Studio
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Must be set before EnableVisualStyles/Run so the initial window is created with
            // the right title bar theme; later changes are applied live via AppTheme.
            Application.SetColorMode(AppTheme.GetColorMode(Properties.Settings.Default.Theme));

            // Replaces the WinForms default ("Microsoft Sans Serif", 8.25pt) app-wide, for every
            // control that doesn't set its own Font explicitly.
            Application.SetDefaultFont(new Font("Segoe UI", 9F));

            // Flat, SystemColors-driven chrome so menus/toolbars/status bars follow the active
            // theme the same way the rest of AppTheme's dark mode support does.
            ToolStripManager.Renderer = new AppToolStripRenderer();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppForm());
        }
    }
}
