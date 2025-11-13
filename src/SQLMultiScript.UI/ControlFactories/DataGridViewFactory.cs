using System.Data;

namespace SQLMultiScript.UI.ControlFactories
{
    public static class DataGridViewFactory
    {
        public static DataGridView CreateToResult(DataTable source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var bs = new BindingSource { DataSource = source };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false, // we create columns explicitly
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowDrop = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText,
                DataSource = bs
            };

            // Ensure this grid uses an isolated BindingContext to avoid shared CurrencyManager collisions.
            grid.BindingContext = new BindingContext();

            // Create columns based on DataTable schema
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < source.Columns.Count; i++)
            {
                var col = source.Columns[i];
                var colName = string.IsNullOrWhiteSpace(col.ColumnName) ? $"Column{i + 1}" : col.ColumnName;

                // Ensure unique column name for the grid
                var uniqueName = colName;
                int suffix = 1;
                while (existingNames.Contains(uniqueName))
                {
                    uniqueName = $"{colName}_{++suffix}";
                }
                existingNames.Add(uniqueName);

                DataGridViewColumn dgvCol;

                var type = col.DataType ?? typeof(string);

                if (type == typeof(bool) || type == typeof(Boolean))
                {
                    dgvCol = new DataGridViewCheckBoxColumn();
                }
                else if (type == typeof(Image) || type == typeof(byte[]))
                {
                    dgvCol = new DataGridViewImageColumn();
                }
                else if (type == typeof(Uri))
                {
                    dgvCol = new DataGridViewLinkColumn();
                }
                else
                {
                    // Default to text column for numbers, dates, strings, etc.
                    dgvCol = new DataGridViewTextBoxColumn();
                }

                dgvCol.Name = uniqueName;
                dgvCol.HeaderText = col.ColumnName; // Keep original header text (may be empty)
                dgvCol.DataPropertyName = col.ColumnName;
                dgvCol.ReadOnly = true; // result grids are read-only

                grid.Columns.Add(dgvCol);
            }

            return grid;
        }
    }
}
