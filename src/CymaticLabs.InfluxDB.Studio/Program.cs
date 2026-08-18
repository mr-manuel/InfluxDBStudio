using System;
using System.Collections.Generic;
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppForm());
        }
    }
}
