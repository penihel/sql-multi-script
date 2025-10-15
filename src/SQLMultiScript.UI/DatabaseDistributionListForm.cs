using Microsoft.Extensions.DependencyInjection;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.ComponentModel;

namespace SQLMultiScript.UI
{
    public class DatabaseDistributionListForm : Form
    {
        private TableLayoutPanel tableLayoutPanel;
        private TreeView treeViewToAdd, treeViewToExecute;
        private Button 
            btnNew, 
            btnAdd, 
            btnRemove,
            btnNewDatabaseDistribuitionList,
            btnRemoveDatabaseDistribuitionList,
            btnRenameDatabaseDistribuitionList;
        
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
            
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;


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


            SetupLeftColumn();
            SetupCenterColumn();
            SetupRightColumn();
        }

        private void SetupRightColumn()
        {
            // -----------------------
            // Painel de Botões
            // -----------------------
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            btnNewDatabaseDistribuitionList = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_new_24_regular,
                Size = UIConstants.ButtonSize
            };
            
            var toolTipBtnNew = new ToolTip();
            toolTipBtnNew.SetToolTip(btnNewDatabaseDistribuitionList, Resources.Strings.NewDatabaseDistributionList);
            
            buttonPanel.Controls.Add(btnNewDatabaseDistribuitionList);


            //
            btnRenameDatabaseDistribuitionList = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_delete_24_regular,
                Size = UIConstants.ButtonSize
            };

            var toolTipBtnRename = new ToolTip();
            toolTipBtnRename.SetToolTip(btnRenameDatabaseDistribuitionList, Resources.Strings.Rename);

            buttonPanel.Controls.Add(btnRenameDatabaseDistribuitionList);


            //
            btnRemoveDatabaseDistribuitionList = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_delete_24_regular,
                Size = UIConstants.ButtonSize
            };
            
            var toolTipBtnRemove = new ToolTip();
            toolTipBtnRemove.SetToolTip(btnRemoveDatabaseDistribuitionList, Resources.Strings.Remove);

            buttonPanel.Controls.Add(btnRemoveDatabaseDistribuitionList);

            
            
            // Adiciona no TableLayout
            tableLayoutPanel.Controls.Add(buttonPanel, 2, 0);
        }

        private void SetupCenterColumn()
        {
            
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.None, // <- ESSENCIAL
                Padding = new Padding(UIConstants.PanelPadding),
                
                
                
            };


            


            // Botões
            btnAdd = new Button
            {

                
                Image = Images.ic_fluent_arrow_circle_right_24_regular,
                Size = UIConstants.ButtonSize,
            };
            btnAdd.Click += BtnAdd_Click;
            var toolTipBtnAdd = new ToolTip();
            toolTipBtnAdd.SetToolTip(btnAdd, Resources.Strings.Add);


            btnRemove = new Button
            {

               
                Image = Images.ic_fluent_arrow_circle_left_24_regular,
                Size = UIConstants.ButtonSize,
            };
            btnRemove.Click += BtnRemove_Click;
            var toolTipBtnRemove = new ToolTip();
            toolTipBtnRemove.SetToolTip(btnRemove, Resources.Strings.Remove);


            panel.Controls.Add(btnAdd);
            panel.Controls.Add(btnRemove);



            

            // Adiciona no TableLayout
            tableLayoutPanel.Controls.Add(panel, 1, 0);

            

        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            
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
            treeViewToAdd.BeginUpdate();
            treeViewToAdd.Nodes.Clear();

            foreach (var conn in _connections)
            {
                var node = new TreeNode(conn.DisplayName)
                {
                    Tag = conn, // guarda o objeto completo
                    // Não marcar checkbox para raiz
                    ImageKey = "disconnected",
                    SelectedImageKey = "disconnected"

                };
                treeViewToAdd.Nodes.Add(node);
            }

            treeViewToAdd.EndUpdate();
        }

        private void SetupLeftColumn()
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
            treeViewToAdd = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                // adiciona espaço no topo para não encostar no label
                Margin = new Padding(0, 4, 0, 0),

                CheckBoxes = true
            };
            treeViewToAdd.AfterCheck += TreeView_AfterCheck;

            treeViewToAdd.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;

            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);

            
            imageList.Images.Add("disconnected", Images.circle_red);
            imageList.Images.Add("connected", Images.circle_green);
            imageList.Images.Add("database", Images.ic_fluent_database_24_regular);
            
            treeViewToAdd.ImageList = imageList;


            // Painel de botões no rodapé
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,

                Padding = new Padding(UIConstants.PanelPadding),
                Height = 60,
            };


            // Botões
            btnNew = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_new_24_regular,
                Size = UIConstants.ButtonSize,
            };
            btnNew.Click += BtnNew_Click;
            var toolTipBtnNew = new ToolTip();
            toolTipBtnNew.SetToolTip(btnNew, Resources.Strings.NewConnection);
            buttonPanel.Controls.Add(btnNew);



            buttonPanel.Controls.Add(btnNew);


            // Monta painel
            panelTreeView.Controls.Add(treeViewToAdd);
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
            treeViewToAdd.AfterCheck -= TreeView_AfterCheck;

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
                treeViewToAdd.AfterCheck += TreeView_AfterCheck;
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
