namespace SQLMultiScript.UI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            dataGridViewScripts = new DataGridView();
            ColumnCheckbox = new DataGridViewCheckBoxColumn();
            ColumnOrder = new DataGridViewTextBoxColumn();
            ColumnScriptName = new DataGridViewTextBoxColumn();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            button2 = new Button();
            button3 = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridViewScripts).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 119);
            button1.Name = "button1";
            button1.Size = new Size(94, 29);
            button1.TabIndex = 1;
            button1.Text = "New";
            button1.UseVisualStyleBackColor = true;
            // 
            // dataGridViewScripts
            // 
            dataGridViewScripts.AllowUserToAddRows = false;
            dataGridViewScripts.AllowUserToDeleteRows = false;
            dataGridViewScripts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewScripts.Columns.AddRange(new DataGridViewColumn[] { ColumnCheckbox, ColumnOrder, ColumnScriptName });
            dataGridViewScripts.Location = new Point(12, 154);
            dataGridViewScripts.Name = "dataGridViewScripts";
            dataGridViewScripts.RowHeadersVisible = false;
            dataGridViewScripts.RowHeadersWidth = 51;
            dataGridViewScripts.ShowEditingIcon = false;
            dataGridViewScripts.Size = new Size(776, 122);
            dataGridViewScripts.TabIndex = 3;
            // 
            // ColumnCheckbox
            // 
            ColumnCheckbox.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnCheckbox.HeaderText = "Selected";
            ColumnCheckbox.MinimumWidth = 6;
            ColumnCheckbox.Name = "ColumnCheckbox";
            ColumnCheckbox.Width = 72;
            // 
            // ColumnOrder
            // 
            ColumnOrder.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            ColumnOrder.HeaderText = "Order";
            ColumnOrder.MinimumWidth = 6;
            ColumnOrder.Name = "ColumnOrder";
            ColumnOrder.ReadOnly = true;
            ColumnOrder.Resizable = DataGridViewTriState.True;
            ColumnOrder.SortMode = DataGridViewColumnSortMode.NotSortable;
            ColumnOrder.Width = 53;
            // 
            // ColumnScriptName
            // 
            ColumnScriptName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ColumnScriptName.HeaderText = "Script Name";
            ColumnScriptName.MinimumWidth = 6;
            ColumnScriptName.Name = "ColumnScriptName";
            ColumnScriptName.ReadOnly = true;
            ColumnScriptName.Resizable = DataGridViewTriState.True;
            ColumnScriptName.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // button2
            // 
            button2.Location = new Point(112, 119);
            button2.Name = "button2";
            button2.Size = new Size(168, 29);
            button2.TabIndex = 4;
            button2.Text = "Add Existing";
            button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(312, 24);
            button3.Name = "button3";
            button3.Size = new Size(293, 93);
            button3.TabIndex = 5;
            button3.Text = "EXECUTE";
            button3.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(dataGridViewScripts);
            Controls.Add(button1);
            Name = "MainForm";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridViewScripts).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private DataGridView dataGridViewScripts;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private DataGridViewCheckBoxColumn ColumnCheckbox;
        private DataGridViewTextBoxColumn ColumnOrder;
        private DataGridViewTextBoxColumn ColumnScriptName;
        private Button button2;
        private Button button3;
    }
}
