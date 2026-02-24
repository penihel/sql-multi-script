using System.Reflection;

namespace SQLMultiScript.UI.ControlFactories
{
    /// <summary>
    /// A custom <see cref="DataGridViewColumnHeaderCell"/> that renders a checkbox
    /// in the column header. Clicking the header toggles all checkboxes in the column.
    /// </summary>
    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell
    {
        private bool _checked;
        private bool _isToggling;
        private Rectangle _checkBoxBounds;

        public DataGridViewCheckBoxHeaderCell()
        {
            _checked = false;
        }

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates dataGridViewElementState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                dataGridViewElementState, value, formattedValue, errorText,
                cellStyle, advancedBorderStyle, paintParts);

            var checkBoxSize = CheckBoxRenderer.GetGlyphSize(graphics,
                System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);

            var x = cellBounds.X + (cellBounds.Width - checkBoxSize.Width) / 2;
            var y = cellBounds.Y + (cellBounds.Height - checkBoxSize.Height) / 2;

            _checkBoxBounds = new Rectangle(x, y, checkBoxSize.Width, checkBoxSize.Height);

            var state = _checked
                ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;

            CheckBoxRenderer.DrawCheckBox(graphics, new Point(x, y), state);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && DataGridView != null)
            {
                var cellBounds = DataGridView.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                var clickPoint = new Point(cellBounds.X + e.X, cellBounds.Y + e.Y);

                if (_checkBoxBounds.Contains(clickPoint))
                {
                    _checked = !_checked;
                    ToggleAllCheckBoxes();
                    DataGridView.InvalidateColumn(e.ColumnIndex);
                    return;
                }
            }

            base.OnMouseClick(e);
        }

        public event Action CheckedChanged;

        private void ToggleAllCheckBoxes()
        {
            if (DataGridView == null) return;

            _isToggling = true;

            try
            {
                var col = OwningColumn as DataGridViewCheckBoxColumn;
                var dataPropertyName = col?.DataPropertyName;

                DataGridView.EndEdit();

                if (!string.IsNullOrEmpty(dataPropertyName))
                {
                    PropertyInfo propInfo = null;

                    foreach (DataGridViewRow row in DataGridView.Rows)
                    {
                        if (row.IsNewRow) continue;

                        var item = row.DataBoundItem;
                        if (item != null)
                        {
                            propInfo ??= item.GetType().GetProperty(dataPropertyName);
                            propInfo?.SetValue(item, _checked);
                        }
                    }
                }

                DataGridView.Refresh();
            }
            finally
            {
                _isToggling = false;
            }

            CheckedChanged?.Invoke();
        }

        /// <summary>
        /// Indicates whether a bulk toggle operation is in progress.
        /// </summary>
        public bool IsToggling => _isToggling;

        /// <summary>
        /// Updates the header checkbox state based on current row values.
        /// Call this after the DataSource changes.
        /// </summary>
        public void RefreshState()
        {
            if (_isToggling) return;

            if (DataGridView == null || DataGridView.Rows.Count == 0)
            {
                _checked = false;
                return;
            }

            var col = OwningColumn as DataGridViewCheckBoxColumn;
            var dataPropertyName = col?.DataPropertyName;

            if (!string.IsNullOrEmpty(dataPropertyName))
            {
                PropertyInfo propInfo = null;
                _checked = DataGridView.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.DataBoundItem != null)
                    .All(r =>
                    {
                        propInfo ??= r.DataBoundItem.GetType().GetProperty(dataPropertyName);
                        return propInfo?.GetValue(r.DataBoundItem) is true;
                    });
            }
            else
            {
                var colIndex = OwningColumn.Index;
                _checked = DataGridView.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow)
                    .All(r => r.Cells[colIndex].Value is true);
            }

            DataGridView.InvalidateColumn(OwningColumn.Index);
        }
    }

    public static class DataGridViewCheckBoxColumnExtensions
    {
        /// <summary>
        /// Enables a "select all" checkbox in the column header. 
        /// Clicking the header checkbox toggles all rows.
        /// The header state auto-refreshes when individual cells change or the DataSource changes.
        /// </summary>
        public static DataGridViewCheckBoxColumn EnableSelectAll(this DataGridViewCheckBoxColumn column)
        {
            var headerCell = new DataGridViewCheckBoxHeaderCell();
            column.HeaderCell = headerCell;

            column.HeaderText = string.Empty;

            void WireEvents(object sender, EventArgs e)
            {
                if (column.DataGridView == null) return;

                var dgv = column.DataGridView;

                dgv.CellValueChanged += (s, args) =>
                {
                    if (!headerCell.IsToggling && args.ColumnIndex == column.Index && args.RowIndex >= 0)
                        headerCell.RefreshState();
                };

                dgv.CurrentCellDirtyStateChanged += (s, args) =>
                {
                    if (!headerCell.IsToggling && dgv.CurrentCell?.ColumnIndex == column.Index)
                        dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgv.DataSourceChanged += (s, args) => headerCell.RefreshState();
                dgv.DataBindingComplete += (s, args) => headerCell.RefreshState();

                dgv.HandleCreated -= WireEvents;
            }

            if (column.DataGridView != null)
            {
                WireEvents(null, EventArgs.Empty);
            }
            else
            {
                column.HeaderCell = headerCell;
                _pendingWiring[column] = WireEvents;
            }

            return column;
        }

        private static readonly Dictionary<DataGridViewCheckBoxColumn, EventHandler> _pendingWiring = new();

        /// <summary>
        /// Call this after adding the column to the DataGridView to complete event wiring.
        /// This is called automatically if you use <see cref="DataGridViewExtensions.AddCheckBoxColumnWithSelectAll"/>.
        /// </summary>
        public static void CompletePendingWiring(this DataGridViewCheckBoxColumn column)
        {
            if (_pendingWiring.Remove(column, out var wireEvents) && column.DataGridView != null)
            {
                wireEvents(null, EventArgs.Empty);
            }
        }
    }

    public static class DataGridViewExtensions
    {
        /// <summary>
        /// Adds a <see cref="DataGridViewCheckBoxColumn"/> with select-all header to the DataGridView.
        /// </summary>
        public static void AddCheckBoxColumnWithSelectAll(
            this DataGridView dgv,
            string dataPropertyName,
            DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.AllCells)
        {
            var col = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = dataPropertyName,
                AutoSizeMode = autoSizeMode,
            };

            col.EnableSelectAll();
            dgv.Columns.Add(col);
            col.CompletePendingWiring();
        }
    }
}
