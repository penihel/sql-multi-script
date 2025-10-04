using System.Windows.Forms;

namespace SQLMultiScript.UI
{
    public class DatabaseDistributionListForm : Form
    {
        private TableLayoutPanel tableLayoutPanel;

        public DatabaseDistributionListForm()
        {
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            MaximizeBox = false;
            MinimizeBox = false;

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
            };

            // Definindo larguras/alturas automáticas
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 90f));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10f));

            // Exemplo: adicionar um botão
            var button = new Button
            {
                Text = "OK",
                Dock = DockStyle.Fill
            };
            tableLayoutPanel.Controls.Add(button, 0, 0);

            Controls.Add(tableLayoutPanel);


            SetupDatabaseToAddPanel();
        }

        private void SetupDatabaseToAddPanel()
        {

            // Painel da célula (coluna 1, linha 1 → índice 0,0)
            var panel = new Panel
            {
                Dock = DockStyle.Fill
            };

            // TreeView ocupa o topo e se expande
            var treeView = new TreeView
            {
                Dock = DockStyle.Fill
            };

            // Painel de botões no rodapé
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft, // botões à direita
                Height = 40,
                Padding = new Padding(5)
            };

            // Botões
            var btnAdd = new Button { Text = "Adicionar", AutoSize = true };
            var btnEdit = new Button { Text = "Editar", AutoSize = true };
            var btnRemove = new Button { Text = "Remover", AutoSize = true };

            // Adiciona botões (em ordem inversa, já que o FlowDirection é RightToLeft)
            buttonPanel.Controls.Add(btnAdd);
            buttonPanel.Controls.Add(btnEdit);
            buttonPanel.Controls.Add(btnRemove);

            // Monta painel
            panel.Controls.Add(treeView);
            panel.Controls.Add(buttonPanel);

            // Adiciona no TableLayout
            tableLayoutPanel.Controls.Add(panel, 0, 0);

            
        }
    }
}
