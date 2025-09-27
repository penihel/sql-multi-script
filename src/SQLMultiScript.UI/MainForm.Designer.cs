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
            splitContainer1 = new SplitContainer();
            panel2 = new Panel();
            panel1 = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)dataGridViewScripts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(3, 3);
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
            dataGridViewScripts.Dock = DockStyle.Fill;
            dataGridViewScripts.Location = new Point(0, 0);
            dataGridViewScripts.Name = "dataGridViewScripts";
            dataGridViewScripts.RowHeadersVisible = false;
            dataGridViewScripts.RowHeadersWidth = 51;
            dataGridViewScripts.ShowEditingIcon = false;
            dataGridViewScripts.Size = new Size(357, 410);
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
            button2.Location = new Point(189, 0);
            button2.Name = "button2";
            button2.Size = new Size(168, 29);
            button2.TabIndex = 4;
            button2.Text = "Add Existing";
            button2.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 32);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel2);
            splitContainer1.Panel1.Controls.Add(panel1);
            splitContainer1.Size = new Size(1071, 465);
            splitContainer1.SplitterDistance = 357;
            splitContainer1.TabIndex = 6;
            // 
            // panel2
            // 
            panel2.Controls.Add(dataGridViewScripts);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 55);
            panel2.Name = "panel2";
            panel2.Size = new Size(357, 410);
            panel2.TabIndex = 6;
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button2);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(357, 55);
            panel1.TabIndex = 5;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78.61314F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3868618F));
            tableLayoutPanel1.Controls.Add(splitContainer1, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            tableLayoutPanel1.Size = new Size(1370, 589);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1370, 589);
            Controls.Add(tableLayoutPanel1);
            Name = "MainForm";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridViewScripts).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
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
        private SplitContainer splitContainer1;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private Panel panel2;
    }
}
