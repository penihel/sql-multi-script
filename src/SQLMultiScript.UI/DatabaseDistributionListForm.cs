using System.Windows.Forms;

namespace SQLMultiScript.UI
{
    public class DatabaseDistributionListForm : Form
    {
        private TableLayoutPanel tableLayoutPanel;

        private Button btnNew;

        public DatabaseDistributionListForm()
        {
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var screenSize = Screen.PrimaryScreen.WorkingArea;

            // Define tamanho como 70% da tela
            int width = (int)(screenSize.Width * 0.7);
            int height = (int)(screenSize.Height * 0.7);
            Size = new Size(width, height);

            Text = Resources.Strings.DatabaseDistributionLists;
            StartPosition = FormStartPosition.CenterScreen;

            tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(UIConstants.PanelPadding),
            };

            // Definindo larguras/alturas automáticas
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            
            Controls.Add(tableLayoutPanel);


            SetupDatabaseToAddPanel();
        }

        private void SetupDatabaseToAddPanel()
        {

            // Painel da célula (coluna 1, linha 1 → índice 0,0)
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding),
            };


            var panelTreeView = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding),
            };

            var label = new Label
            {
                Text = Resources.Strings.DatabasesToAdd,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 24, // altura fixa para não sobrepor
                Padding = new Padding(0, 0, 0, 4), // espaço abaixo do texto

            };
            // TreeView ocupa o topo e se expande
            var treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                // adiciona espaço no topo para não encostar no label
                Margin = new Padding(0, 4, 0, 0),
            };

            // Painel de botões no rodapé
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,

                Padding = new Padding(UIConstants.PanelPadding),
                Height = 50,
            };


            // Botões
            btnNew = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_new_24_regular,
                Width = 50
            };
            btnNew.Click += BtnNew_Click;
            var toolTipBtnNew = new ToolTip();
            toolTipBtnNew.SetToolTip(btnNew, Resources.Strings.New);
            buttonPanel.Controls.Add(btnNew);


            
            buttonPanel.Controls.Add(btnNew);


            // Monta painel
            panelTreeView.Controls.Add(treeView);
            panelTreeView.Controls.Add(label);
            
            panel.Controls.Add(panelTreeView);
            panel.Controls.Add(buttonPanel);

            // Adiciona no TableLayout
            tableLayoutPanel.Controls.Add(panel, 0, 0);

            
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var newConnectionForm
                 = new NewConnectionForm();

            var result = newConnectionForm.ShowDialog(this);
        }
    }
}
