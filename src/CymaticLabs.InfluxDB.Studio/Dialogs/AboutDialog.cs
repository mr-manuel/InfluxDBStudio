using System;
using System.Windows.Forms;

namespace CymaticLabs.InfluxDB.Studio.Dialogs
{
    /// <summary>
    /// Application about dialog.
    /// </summary>
    public partial class AboutDialog : Form
    {
        #region Fields

        #endregion Fields

        #region Properties

        #endregion Properties

        #region Constructors

        public AboutDialog()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Event Handlers

        // Form Load
        private void AboutDialog_Load(object sender, EventArgs e)
        {
            // Apply the current version number
            versionLabel.Text = AppForm.Settings.Version;

            // Position the version right after the title so there is no gap
            // regardless of the title's rendered width.
            versionLabel.Left = titleLabel.Right + 6;
        }

        // Launch project link
        private void projectLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/mr-manuel/InfluxDBStudio",
                UseShellExecute = true
            });
        }

        // Launch InfluxData.Net link
        private void influxDataNetLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/pootzko/InfluxData.Net",
                UseShellExecute = true
            });
        }

        #endregion Event Handlers

        #region Methods

        #endregion Methods

    }
}
