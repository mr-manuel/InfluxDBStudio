using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CymaticLabs.InfluxDB.Studio.Dialogs
{
    /// <summary>
    /// Dialog used to edit the values of a single query result row. Columns flagged as
    /// read-only (a point's tags) are shown for context but cannot be changed here, since
    /// changing a tag value moves a point to a different series rather than updating it in place.
    /// </summary>
    public partial class EditRowDialog : Form
    {
        #region Fields

        // Editable value text boxes, keyed by column name.
        readonly Dictionary<string, TextBox> valueTextBoxesByColumn = new Dictionary<string, TextBox>();

        #endregion Fields

        #region Constructors

        public EditRowDialog()
        {
            InitializeComponent();
            AppTheme.ApplyModernSpacing(this);
            AppTheme.ApplyDarkTitleBar(this);
            AppTheme.ApplyButtonsTheme(this);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Populates the dialog with one row per result column, pre-filled with its current
        /// display value. Series tags that aren't part of <paramref name="columnNames"/> (e.g.
        /// from a GROUP BY query) can be supplied via <paramref name="seriesTags"/> and are
        /// shown read-only, above the editable columns, for context.
        /// </summary>
        public void BindToRow(string measurement, IDictionary<string, string> seriesTags,
            IList<string> columnNames, IList<string> displayValues, ISet<string> readOnlyColumns)
        {
            Text = "Edit Row - " + measurement;

            valueTextBoxesByColumn.Clear();
            fieldsPanel.Controls.Clear();

            var y = 6;

            if (seriesTags != null)
            {
                foreach (var tag in seriesTags)
                {
                    AddRow(tag.Key + " (series tag)", tag.Value, true, ref y);
                }
            }

            for (var i = 0; i < columnNames.Count; i++)
            {
                var columnName = columnNames[i];
                var value = i < displayValues.Count ? displayValues[i] : null;
                var isReadOnly = readOnlyColumns != null && readOnlyColumns.Contains(columnName);
                var textBox = AddRow(isReadOnly ? columnName + " (tag)" : columnName, value, isReadOnly, ref y);
                valueTextBoxesByColumn[columnName] = textBox;
            }
        }

        // Adds a single label/value row to the fields panel and returns its value text box.
        TextBox AddRow(string labelText, string value, bool isReadOnly, ref int y)
        {
            const int rowHeight = 26;
            const int rowSpacing = 6;
            const int labelWidth = 140;

            var label = new Label
            {
                Text = labelText,
                AutoSize = false,
                Location = new Point(6, y + 3),
                Size = new Size(labelWidth, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var textBox = new TextBox
            {
                Text = value,
                ReadOnly = isReadOnly,
                TabStop = !isReadOnly,
                Location = new Point(labelWidth + 12, y),
                Size = new Size(fieldsPanel.ClientSize.Width - labelWidth - 30, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            fieldsPanel.Controls.Add(label);
            fieldsPanel.Controls.Add(textBox);

            y += rowHeight + rowSpacing;

            return textBox;
        }

        /// <summary>
        /// Gets the current (possibly edited) text for a given editable column.
        /// </summary>
        public string GetValueText(string columnName)
        {
            return valueTextBoxesByColumn.TryGetValue(columnName, out var textBox) ? textBox.Text : null;
        }

        #endregion Methods
    }
}
