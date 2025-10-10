using Microsoft.Extensions.DependencyInjection;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Services;
using System.ComponentModel;
using System.Windows.Forms;

namespace SQLMultiScript.UI
{
    public class DatabaseDistributionListForm : Form
    {
        private TableLayoutPanel tableLayoutPanel;
        private TreeView treeView;
        private Button btnNew;
        private readonly IConnectionService _connectionService;
        private readonly IServiceProvider _serviceProvider;


        private BindingList<Connection> _connections;
        public DatabaseDistributionListForm(
            IConnectionService connectionService,
            IServiceProvider serviceProvider)
        {
            InitializeLayout();
            _connectionService = connectionService;
            _serviceProvider = serviceProvider;
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

            Load += DatabaseDistributionListForm_Load;

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

        private async void DatabaseDistributionListForm_Load(object sender, EventArgs e)
        {


            await BindData();
        }

        private async Task LoadConnectionsAsync()
        {

            _connections =
                new BindingList<Connection>(await _connectionService.ListAsync());
            _connections.ListChanged += Connections_ListChanged;

        }

        private async Task BindData()
        {
            await LoadConnectionsAsync();

            // Sempre que a lista mudar, atualiza a TreeView
            UpdateTreeView();
        }
        private void Connections_ListChanged(object sender, ListChangedEventArgs e)
        {
            // Sempre que a lista mudar, atualiza a TreeView
            UpdateTreeView();
        }
        private void UpdateTreeView()
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            foreach (var conn in _connections)
            {
                var node = new TreeNode(conn.DisplayName)
                {
                    Tag = conn, // guarda o objeto completo
                    // Não marcar checkbox para raiz
                    ImageKey = "disconnected",
                    SelectedImageKey = "disconnected"

                };
                treeView.Nodes.Add(node);
            }

            treeView.EndUpdate();
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
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                // adiciona espaço no topo para não encostar no label
                Margin = new Padding(0, 4, 0, 0),

                CheckBoxes = true
            };
            treeView.AfterCheck += TreeView_AfterCheck;

            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;

            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);

            
            imageList.Images.Add("disconnected", Images.circle_red);
            imageList.Images.Add("connected", Images.circle_green);
            imageList.Images.Add("database", Images.ic_fluent_database_24_regular);
            
            treeView.ImageList = imageList;


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
            toolTipBtnNew.SetToolTip(btnNew, Resources.Strings.NewConnection);
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

        private async void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Só processa se o node for raiz (não tiver filhos ainda)
            if (e.Node.Level == 0 && e.Node.Nodes.Count == 0)
            {
                if (e.Node.Tag is Connection conn)
                {
                    try
                    {
                        // Busca bancos da conexão
                        var databases = await _connectionService.ListDatabasesAsync(conn);

                        // Adiciona cada banco como node filho
                        foreach (var db in databases)
                        {
                            var dbNode = new TreeNode(db.DatabaseName)
                            {
                                Tag = new { Connection = conn, Database = db }, // guarda info
                                Checked = false,
                                
                                ImageKey = "database",
                                SelectedImageKey = "database"
                            };
                            e.Node.Nodes.Add(dbNode);
                        }
                        
                        e.Node.ImageKey = "connected";
                        e.Node.SelectedImageKey = "connected";
                        
                        // Expande automaticamente
                        e.Node.Expand();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao carregar bancos:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Evita recursão infinita
            treeView.AfterCheck -= TreeView_AfterCheck;

            try
            {
                // Atualiza filhos
                if (e.Node.Nodes.Count > 0)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                    }
                }

                // Atualiza pai (recursivamente)
                UpdateParentCheckState(e.Node);
            }
            finally
            {
                treeView.AfterCheck += TreeView_AfterCheck;
            }
        }

        // Marca o pai se todos os filhos estiverem selecionados, desmarca caso contrário
        private void UpdateParentCheckState(TreeNode node)
        {
            if (node.Parent == null) return;

            bool allChecked = true;
            foreach (TreeNode sibling in node.Parent.Nodes)
            {
                if (!sibling.Checked)
                {
                    allChecked = false;
                    break;
                }
            }

            node.Parent.Checked = allChecked;

            // Propaga para cima
            UpdateParentCheckState(node.Parent);
        }


        private async void BtnNew_Click(object sender, EventArgs e)
        {
            var newConnectionForm = _serviceProvider.GetRequiredService<NewConnectionForm>();

            var result = newConnectionForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                await BindData();
            }
        }
    }
}
