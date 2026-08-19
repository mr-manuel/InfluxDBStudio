using CymaticLabs.InfluxDB.Data;
using CymaticLabs.InfluxDB.Studio.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CymaticLabs.InfluxDB.Studio.Controls
{
    /// <summary>
    /// Renders the results for a single InfluxDB query.
    /// </summary>
    public partial class QueryResultsControl : UserControl
    {
        #region Fields

        // Used to give resulting rows an ID number
        int resultsCount = 0;

        // A cache of the last results received.
        InfluxDbSeries lastResult;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="InfluxDB.InfluxDbClient">InfluxDB connection</see> associated
        /// with the control.
        /// </summary>
        public InfluxDbClient InfluxDbClient { get; set; }

        /// <summary>
        /// Gets or sets the name of the database associated with the control.
        /// </summary>
        public string Database { get; set; }

        #endregion Properties

        #region Constructors

        public QueryResultsControl()
        {
            InitializeComponent();

            AppTheme.ApplyListViewGridTheme(listView);
        }

        #endregion Constructors

        #region Event Handlers

        // Export All -> CSV
        private async void exportAllCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await ExportToCsv();
        }

        // Export All -> JSON
        private void jSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToJson();
        }

        // Export Selected -> CSV
        private async void exportSelectedCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await ExportToCsv(true);
        }

        // Export Selected -> JSON
        private void jSONToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ExportToJson(true);
        }

        // Edit Selected Row
        private async void editSelectedRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await EditSelectedRow();
        }

        // Delete Selected Row(s)
        private async void deleteSelectedRowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await DeleteSelectedRows();
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Clears the current query results from the UI.
        /// </summary>
        public void ClearResults()
        {
            // Clear out current items
            resultsCount = 0;
            tagsTextBox.Text = null;
            listView.BeginUpdate();
            listView.Columns.Clear();
            listView.Items.Clear();
            listView.EndUpdate();
        }

        /// <summary>
        /// Updates the query control's query results UI with the supplied result.
        /// </summary>
        /// <param name="result">The query result to render.</param>
        /// <returns>The total number of results found.</returns>
        public int UpdateResults(InfluxDbSeries result, bool clear = false)
        {
            if (result == null) throw new ArgumentNullException("result");

            // Cache
            lastResult = result;

            // Clear as needed
            if (clear) ClearResults();

            // Add tag values to to results
            if (result.Tags.Count > 0)
            {
                splitContainer.Panel1Collapsed = false;
                var tagCount = result.Tags.Count;
                var tagCounter = 0;
                var sb = new StringBuilder();

                foreach (var tag in result.Tags)
                {
                    sb.AppendFormat("{0} = {1}{2}", tag.Key, tag.Value, ++tagCounter < tagCount ? ", " : null);
                }

                tagsTextBox.Text = sb.ToString();
            }
            // Hide tag area if there are no tag values
            else
            {
                splitContainer.Panel1Collapsed = true;
            }

            // Start to update the list view with the new results
            listView.BeginUpdate();

            // Build the first column
            var colRecordNum = new ColumnHeader() { Text = "#" };
            listView.Columns.Add(colRecordNum);

            // Build the dynamic columns
            foreach (var c in result.Columns)
            {
                var col = new ColumnHeader();
                col.Text = c;
                listView.Columns.Add(col);
            }

            // Build the rows
            for (var i = 0; i < result.Values.Count; i++)
            {
                // Create the top level row item and give it the record number as a label
                ListViewItem li = new ListViewItem((++resultsCount).ToString());
                listView.Items.Add(li);

                // Get the columns/values for the row
                var r = result.Values[i];

                for (var x = 0; x < r.Count; x++)
                {
                    // Attach the column values as subitems
                    var li2 = new ListViewItem.ListViewSubItem(li, FormatCellValue(r[x]));
                    li2.Tag = r;
                    li.SubItems.Add(li2);
                }
            }

            // Resize each column
            if (listView.Columns.Count > 0)
            {
                var columnWidth = (Width - 12) / listView.Columns.Count;
                if (columnWidth < 96) columnWidth = 96;
                foreach (ColumnHeader col in listView.Columns) col.Width = columnWidth;
            }

            listView.EndUpdate();

            return resultsCount;
        }

        // Exports series data to CSV
        async Task ExportToCsv(bool onlySelected = false)
        {
            try
            {
                // Configure save dialog and open
                saveFileDialog.FileName = string.Format("{0}.csv", InfluxDbClient.Connection.Name + "_" + Database);
                saveFileDialog.Filter = "CSV files|*.csv|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();

                    // Create a stream writer to write the CSV file
                    using (var sw = new StreamWriter(saveFileDialog.FileName))
                    {
                        sb.Clear();

                        // Write the CSV column names (skip first column which is just row # label)
                        for (var i = 1; i < listView.Columns.Count; i++)
                        {
                            sb.Append(listView.Columns[i].Text);
                            if (i < listView.Columns.Count - 1) sb.Append(",");
                        }

                        await sw.WriteLineAsync(sb.ToString());

                        // Now write each series row
                        foreach (ListViewItem li in listView.Items)
                        {
                            if (onlySelected && !li.Selected) continue;

                            sb.Clear();

                            // (skip first column which is just row # label)
                            for (var i = 1; i < li.SubItems.Count; i++)
                            {
                                var sli = li.SubItems[i];
                                sb.Append(sli.Text);
                                if (i < li.SubItems.Count - 1) sb.Append(",");
                            }

                            await sw.WriteLineAsync(sb.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppForm.DisplayException(ex);
            }
        }

        // Exports series data to a JSON array
        void ExportToJson(bool onlySelected = false)
        {
            try
            {
                // Configure save dialog and open
                saveFileDialog.FileName = string.Format("{0}.json", InfluxDbClient.Connection.Name + "_" + Database);
                saveFileDialog.Filter = "JSON files|*.json|All files|*.*";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Collect the list of points
                    var array = new List<object>();

                    if (lastResult != null)
                    {
                        // Build name lookup
                        var indexToName = new Dictionary<int, string>();

                        foreach (var colName in lastResult.Columns)
                        {
                            if (!indexToName.ContainsKey(indexToName.Count))
                                indexToName.Add(indexToName.Count, colName);
                        }

                        // Build selected states from UI state
                        var selectedByRowId = new Dictionary<int, bool>();

                        for (var i = 0; i < listView.Items.Count; i++)
                        {
                            var li = listView.Items[i];
                            selectedByRowId.Add(i, li.Selected);
                        }

                        // Convert results to JSON for export
                        for (var i = 0; i < lastResult.Values.Count; i++)
                        {
                            var r = lastResult.Values[i];

                            if (onlySelected && !selectedByRowId[i]) continue;

                            // Convert to outgoing dictionary
                            var d = new Dictionary<string, object>();

                            for (var x = 0; x < r.Count; x++)
                            {
                                var key = indexToName[x];
                                var value = r[x];

                                if (d.ContainsKey(key)) d[key] = value;
                                else d.Add(key, value);
                            }

                            // Add to outgoing json structure
                            array.Add(d);
                        }
                    }

                    // Serialize to json
                    var json = JsonConvert.SerializeObject(array, Formatting.Indented);

                    // Write to disk
                    File.WriteAllText(saveFileDialog.FileName, json);
                }
            }
            catch (Exception ex)
            {
                AppForm.DisplayException(ex);
            }
        }

        // Deletes the currently selected rows from InfluxDB as individual data points,
        // identified by their "time" column value (and series tags, if any).
        async Task DeleteSelectedRows()
        {
            try
            {
                if (InfluxDbClient == null || string.IsNullOrWhiteSpace(Database))
                {
                    MessageBox.Show("No active database connection.", "Delete Row(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (lastResult == null || string.IsNullOrWhiteSpace(lastResult.Name))
                {
                    MessageBox.Show("These results are not associated with a single measurement, so rows cannot be deleted.", "Delete Row(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var timeColumnIndex = lastResult.GetColumnIndex("time");

                if (timeColumnIndex < 0)
                {
                    MessageBox.Show("These results do not contain a \"time\" column, so rows cannot be identified for deletion.", "Delete Row(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedItems = new List<ListViewItem>();
                foreach (ListViewItem li in listView.Items)
                {
                    if (li.Selected) selectedItems.Add(li);
                }

                if (selectedItems.Count == 0)
                {
                    MessageBox.Show("No rows are selected.", "Delete Row(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Collect the time predicate for each selected row
                var timePredicates = new List<string>();

                foreach (var li in selectedItems)
                {
                    // Column 0 is the "#" label and has no row data attached, so real
                    // columns start at sub-item index 1
                    if (li.SubItems.Count <= 1) continue;

                    var row = li.SubItems[1].Tag as IList<object>;
                    if (row == null || timeColumnIndex >= row.Count) continue;

                    timePredicates.Add("time = " + ToInfluxQlTimeLiteral(row[timeColumnIndex]));
                }

                if (timePredicates.Count == 0)
                {
                    MessageBox.Show("Unable to determine the timestamp for the selected row(s).", "Delete Row(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Confirm delete
                var confirmMessage = string.Format(
                    "Delete {0} selected row{1} from measurement \"{2}\"?\n\nThis action cannot be undone.",
                    timePredicates.Count, timePredicates.Count == 1 ? null : "s", lastResult.Name);

                if (MessageBox.Show(confirmMessage, "Confirm Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                    return;

                // InfluxQL's DELETE WHERE clause only supports AND (no OR), so each row
                // needs its own DELETE statement rather than one combined "time = a OR
                // time = b" predicate. They're issued as separate requests (rather than
                // joined with semicolons into one multi-statement query) because the
                // underlying InfluxData.Net client assumes a single result per query and
                // throws when a multi-statement response comes back. Scope each delete to
                // the exact series the row came from, if the results are for a tagged
                // series (e.g. from a GROUP BY query).
                foreach (var timePredicate in timePredicates)
                {
                    var where = new StringBuilder(timePredicate);

                    foreach (var tag in lastResult.Tags)
                    {
                        where.AppendFormat(" AND \"{0}\" = '{1}'", tag.Key, tag.Value.Replace("'", "\\'"));
                    }

                    var query = string.Format("DELETE FROM \"{0}\" WHERE {1}", lastResult.Name, where);

                    await InfluxDbClient.QueryAsync(Database, query);
                }

                // Remove the deleted rows from the UI
                listView.BeginUpdate();
                foreach (var li in selectedItems) listView.Items.Remove(li);
                listView.EndUpdate();
            }
            catch (Exception ex)
            {
                AppForm.DisplayException(ex);
            }
        }

        // Formats a raw "time" column value as an InfluxQL-compatible time literal.
        static string ToInfluxQlTimeLiteral(object timeValue)
        {
            if (timeValue is DateTime dateTime)
            {
                return "'" + dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture) + "'";
            }

            // Fall back to treating the value as a pre-formatted/numeric timestamp
            return timeValue?.ToString();
        }

        // Formats a raw column value for display in the grid/edit dialog.
        static string FormatCellValue(object value)
        {
            if (value is DateTime dateTime) return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return value?.ToString();
        }

        // Edits the single currently selected row by writing an updated point to InfluxDB
        // with the same measurement/tags/timestamp (which overwrites the existing field
        // values), unless the timestamp itself was changed, in which case the original
        // point is deleted after the new one is written successfully so it isn't left behind.
        async Task EditSelectedRow()
        {
            try
            {
                if (InfluxDbClient == null || string.IsNullOrWhiteSpace(Database))
                {
                    MessageBox.Show("No active database connection.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (lastResult == null || string.IsNullOrWhiteSpace(lastResult.Name))
                {
                    MessageBox.Show("These results are not associated with a single measurement, so this row cannot be edited.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var timeColumnIndex = lastResult.GetColumnIndex("time");

                if (timeColumnIndex < 0)
                {
                    MessageBox.Show("These results do not contain a \"time\" column, so this row cannot be identified for editing.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedItems = new List<ListViewItem>();
                foreach (ListViewItem li in listView.Items)
                {
                    if (li.Selected) selectedItems.Add(li);
                }

                if (selectedItems.Count == 0)
                {
                    MessageBox.Show("No row is selected.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedItems.Count > 1)
                {
                    MessageBox.Show("Please select exactly one row to edit.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var item = selectedItems[0];
                if (item.SubItems.Count <= 1) return;
                var row = item.SubItems[1].Tag as IList<object>;
                if (row == null) return;

                // Determine which result columns are tags (only relevant for ungrouped
                // queries, where tag values come back as regular columns) so they're shown
                // read-only rather than treated as editable fields
                var tagKeys = new HashSet<string>(StringComparer.Ordinal);
                try
                {
                    var measurementTagKeys = await InfluxDbClient.GetTagKeysAsync(Database, lastResult.Name);
                    if (measurementTagKeys != null) foreach (var k in measurementTagKeys) tagKeys.Add(k);
                }
                catch (Exception)
                {
                    // Best effort - if this fails, columns are still shown/edited as fields
                }

                var displayValues = new List<string>(lastResult.Columns.Count);
                for (var i = 0; i < lastResult.Columns.Count; i++)
                {
                    displayValues.Add(FormatCellValue(i < row.Count ? row[i] : null));
                }

                using (var dialog = new EditRowDialog())
                {
                    dialog.BindToRow(lastResult.Name, lastResult.Tags, lastResult.Columns, displayValues, tagKeys);

                    if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

                    // Parse edited values, preserving each column's original .NET type
                    var editedRow = new List<object>(row.Count);

                    for (var i = 0; i < lastResult.Columns.Count; i++)
                    {
                        var columnName = lastResult.Columns[i];
                        var originalValue = i < row.Count ? row[i] : null;
                        var text = dialog.GetValueText(columnName);
                        editedRow.Add(ParseEditedValue(text, originalValue));
                    }

                    if (!(editedRow[timeColumnIndex] is DateTime newTime))
                    {
                        MessageBox.Show("The \"time\" value is invalid.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var originalTime = row[timeColumnIndex] as DateTime?;

                    // Build the tags (unchanged, not editable here) and fields (everything
                    // except time/tags) to write back
                    var tags = new Dictionary<string, object>();
                    foreach (var tag in lastResult.Tags) tags[tag.Key] = tag.Value;

                    var fields = new Dictionary<string, object>();

                    for (var i = 0; i < lastResult.Columns.Count; i++)
                    {
                        if (i == timeColumnIndex) continue;

                        var columnName = lastResult.Columns[i];

                        if (tagKeys.Contains(columnName))
                        {
                            // Tag column present among the result columns (ungrouped query) -
                            // keep its original value, tags can't be changed via edit
                            tags[columnName] = row[i]?.ToString();
                            continue;
                        }

                        fields[columnName] = editedRow[i];
                    }

                    if (fields.Count == 0)
                    {
                        MessageBox.Show("There are no editable field values for this row.", "Edit Row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var timeChanged = originalTime == null || newTime.ToUniversalTime() != originalTime.Value.ToUniversalTime();

                    var confirmMessage = timeChanged
                        ? string.Format("Save changes to this row in measurement \"{0}\"?\n\nThe timestamp has changed, so the original data point will be replaced.", lastResult.Name)
                        : string.Format("Save changes to this row in measurement \"{0}\"?", lastResult.Name);

                    if (MessageBox.Show(confirmMessage, "Confirm Edit", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                        return;

                    // Write the updated point first...
                    var writeResponse = await InfluxDbClient.WriteAsync(Database, lastResult.Name, tags, fields, newTime.ToUniversalTime());

                    if (!writeResponse.Success)
                    {
                        AppForm.DisplayError(writeResponse.Body, "Error Writing Row");
                        return;
                    }

                    // ...then remove the original point if its identity changed, so it isn't left behind
                    if (timeChanged && originalTime != null)
                    {
                        var where = new StringBuilder();
                        where.Append("time = ").Append(ToInfluxQlTimeLiteral(originalTime.Value));

                        foreach (var tag in lastResult.Tags)
                        {
                            where.AppendFormat(" AND \"{0}\" = '{1}'", tag.Key, tag.Value.Replace("'", "\\'"));
                        }

                        var deleteQuery = string.Format("DELETE FROM \"{0}\" WHERE {1}", lastResult.Name, where);
                        await InfluxDbClient.QueryAsync(Database, deleteQuery);
                    }

                    // Reflect the changes in the UI and backing data in place
                    for (var i = 0; i < row.Count && i < editedRow.Count; i++) row[i] = editedRow[i];

                    for (var i = 0; i < lastResult.Columns.Count; i++)
                    {
                        var subItemIndex = i + 1; // +1 because SubItems[0] is the "#" label
                        if (subItemIndex >= item.SubItems.Count) break;
                        item.SubItems[subItemIndex].Text = FormatCellValue(row[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                AppForm.DisplayException(ex);
            }
        }

        // Parses a value edited as text back into the same .NET type as the row's original
        // value for that column, so the type of an existing field doesn't change on write
        // (InfluxDB rejects writes that would change an existing field's type).
        static object ParseEditedValue(string text, object originalValue)
        {
            if (originalValue == null || originalValue is string) return text;

            var type = originalValue.GetType();

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                    return dt;

                throw new FormatException("Invalid time value: \"" + text + "\"");
            }

            try
            {
                return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new FormatException(string.Format("Invalid {0} value: \"{1}\"", type.Name, text));
            }
        }

        #endregion Methods

        private void listView_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender != listView) return;

            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        SelectedAll();
                        break;
                    case Keys.C:
                        CopySelectedToClipboard();
                        break;
                }
            }
        }

        private void SelectedAll()
        {
            foreach (ListViewItem li in listView.Items)
            {
                li.Selected = true;
            }
        }

        private void CopySelectedToClipboard()
        {
            var sb = new StringBuilder();

            foreach (ListViewItem li in listView.Items)
            {
                if (!li.Selected) continue;

                // (skip first column which is just row # label)
                for (var i = 1; i < li.SubItems.Count; i++)
                {
                    var sli = li.SubItems[i];
                    sb.Append(sli.Text);
                    if (i < li.SubItems.Count - 1) sb.Append('\t');
                }

                sb.Append(Environment.NewLine);
            }

            Clipboard.SetText(sb.ToString());
        }
    }
}
