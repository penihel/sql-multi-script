using Microsoft.Extensions.DependencyInjection;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Resources;
using SQLMultiScript.UI.ControlFactories;
using System.ComponentModel;

namespace SQLMultiScript.UI.Forms
{
    public class DatabaseDistributionListForm : BaseForm
    {
        // Services
        private readonly IDatabaseDistributionListService _databaseDistributionListService;
        private readonly IConnectionService _connectionService;
        private readonly IServiceProvider _serviceProvider;

        // UI Controls
        private DataGridView dataGridViewDatabases;
        private TreeView treeViewToAdd;

        private Button
            btnAdd,
            btnRemove,
            btnNewDatabaseDistribuitionList,
            btnRemoveDatabaseDistribuitionList,
            btnRenameDatabaseDistribuitionList;
        private ComboBox comboBoxDatabaseDistributionList;

        // Data
        private BindingList<Connection> _connections;
        private BindingList<DatabaseDistributionList> _databaseDistributionLists;

        public DatabaseDistributionListForm(
            IConnectionService connectionService,
            IServiceProvider serviceProvider,
            IDatabaseDistributionListService databaseDistributionListService)
        {
            _connectionService = connectionService;
            _serviceProvider = serviceProvider;
            _databaseDistributionListService = databaseDistributionListService;

            InitializeLayout();


        }

        // Layout and UI setup
        private void InitializeLayout()
        {
            InitializeFormAsFixedDialog();

            SetFormDialogSize(0.7M);

            Text = Strings.DatabaseDistributionLists;



            Load += DatabaseDistributionListForm_Load;

            TableLayoutPanel mainTableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            // Set column and row sizes
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));

            Controls.Add(mainTableLayoutPanel);

            SetupLeftColumn(mainTableLayoutPanel);
            SetupCenterColumn(mainTableLayoutPanel);
            SetupRightColumn(mainTableLayoutPanel);
            SetupBottomRow(mainTableLayoutPanel);
        }

        private void SetupLeftColumn(TableLayoutPanel tableLayoutPanel)
        {

            var panelLeftContainer = PanelFactory.Create();

            var panelTreeView = PanelFactory.Create();

            var label = LabelFactory.Create(Strings.DatabasesToAdd);

            // TreeView occupies the top and expands
            treeViewToAdd = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                // Add space at the top to avoid touching the label
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

            // Button panel at the bottom
            var buttonPanel = PanelFactory.Create(DockStyle.Bottom, 60);

            // New Connection Button
            var btnNew = ButtonFactory.Create(ToolTip,
                Strings.NewConnection,
                Images.ic_fluent_new_24_regular, 
                onClick: BtnNew_Click);
            
            
            buttonPanel.Controls.Add(btnNew);

            // Assemble panels
            panelTreeView.Controls.Add(treeViewToAdd);
            panelTreeView.Controls.Add(label);

            panelLeftContainer.Controls.Add(panelTreeView);
            panelLeftContainer.Controls.Add(buttonPanel);

            // Add to TableLayout
            tableLayoutPanel.Controls.Add(panelLeftContainer, 0, 0);
        }

        private void SetupCenterColumn(TableLayoutPanel tableLayoutPanel)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.None,
                Padding = new Padding(UIConstants.PanelPadding),
            };

            // Add Button
            btnAdd = new Button
            {
                Image = Images.ic_fluent_arrow_circle_right_24_regular,
                Size = UIConstants.ButtonSize,
            };
            btnAdd.Click += BtnAdd_Click;
            var toolTipBtnAdd = new ToolTip();
            toolTipBtnAdd.SetToolTip(btnAdd, Resources.Strings.Add);

            // Remove Button
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

            // Add to TableLayout
            tableLayoutPanel.Controls.Add(panel, 1, 0);
        }

        private void SetupRightColumn(TableLayoutPanel tableLayoutPanel)
        {
            // Top-right TableLayoutPanel for buttons and ComboBox
            var toprightTableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
            };
            toprightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // ComboBox takes all available space
            toprightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F)); // Button panel fixed width

            // ComboBox for distribution lists
            comboBoxDatabaseDistributionList = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            toprightTableLayoutPanel.Controls.Add(comboBoxDatabaseDistributionList, 0, 0);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            // New Distribution List Button
            btnNewDatabaseDistribuitionList = new Button
            {
                Dock = DockStyle.Right,
                Image = Images.ic_fluent_add_24_regular,
                Size = UIConstants.ButtonSize
            };
            var toolTipBtnNew = new ToolTip();
            toolTipBtnNew.SetToolTip(btnNewDatabaseDistribuitionList, Resources.Strings.NewDatabaseDistributionList);
            buttonPanel.Controls.Add(btnNewDatabaseDistribuitionList);

            // Rename Distribution List Button
            btnRenameDatabaseDistribuitionList = new Button
            {
                Dock = DockStyle.Right,
                Image = Images.ic_fluent_rename_24_regular,
                Size = UIConstants.ButtonSize
            };
            var toolTipBtnRename = new ToolTip();
            toolTipBtnRename.SetToolTip(btnRenameDatabaseDistribuitionList, Resources.Strings.Rename);
            buttonPanel.Controls.Add(btnRenameDatabaseDistribuitionList);

            // Remove Distribution List Button
            btnRemoveDatabaseDistribuitionList = new Button
            {
                Dock = DockStyle.Right,
                Image = Images.ic_fluent_delete_24_regular,
                Size = UIConstants.ButtonSize
            };
            var toolTipBtnRemove = new ToolTip();
            toolTipBtnRemove.SetToolTip(btnRemoveDatabaseDistribuitionList, Resources.Strings.Remove);
            buttonPanel.Controls.Add(btnRemoveDatabaseDistribuitionList);

            toprightTableLayoutPanel.Controls.Add(buttonPanel, 1, 0);

            // DataGridView for databases
            dataGridViewDatabases = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowDrop = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            };

            // Checkbox column
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            };
            dataGridViewDatabases.Columns.Add(colSelected);

            // Database name column
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DisplayName",
                HeaderText = Resources.Strings.Database,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabases.Columns.Add(colName);

            // Panel for the DataGridView
            var listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding)
            };
            listPanel.Controls.Add(dataGridViewDatabases);

            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            containerPanel.Controls.Add(listPanel);
            containerPanel.Controls.Add(toprightTableLayoutPanel);

            // Add to TableLayout
            tableLayoutPanel.Controls.Add(containerPanel, 2, 0);
        }

        private void SetupBottomRow(TableLayoutPanel tableLayoutPanel)
        {
            // Divider line (Panel)
            var divider = new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = Color.LightGray
            };

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            var btnSave = ButtonFactory
                .Create(ToolTip,
                    Strings.Save,
                    Images.ic_fluent_save_24_regular,
                    DockStyle.Right);


            var btnCancel = ButtonFactory
                .Create(ToolTip,
                    Strings.Cancel,
                    Images.ic_fluent_dismiss_24_regular,
                    DockStyle.Right);

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);

            // Adiciona o divider e o painel de botões na TableLayoutPanel
            // Cria um painel container para alinhar o divider acima dos botões
            var bottomContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,

            };
            bottomContainer.Controls.Add(buttonPanel);
            bottomContainer.Controls.Add(divider);

            // Adiciona ao TableLayoutPanel
            tableLayoutPanel.Controls.Add(bottomContainer, 0, 1);
            tableLayoutPanel.SetColumnSpan(bottomContainer, 3);
        }

        // Data binding and loading
        private async Task BindData()
        {
            await LoadConnectionsAsync();
            await LoadDistribuitionListsAsync();

            // Whenever the list changes, update the TreeView
            UpdateTreeView();

            comboBoxDatabaseDistributionList.DataSource = _databaseDistributionLists;
            comboBoxDatabaseDistributionList.DisplayMember = "DisplayName";
            comboBoxDatabaseDistributionList.ValueMember = "Id";
            //comboBoxDatabaseDistributionList.DataBindings.Add("SelectedValue", _project, "SelectedDistributionListId", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private async Task LoadConnectionsAsync()
        {
            _connections = new BindingList<Connection>(await _connectionService.ListAsync());
            _connections.ListChanged += Connections_ListChanged;
        }

        private async Task LoadDistribuitionListsAsync()
        {
            _databaseDistributionLists = new BindingList<DatabaseDistributionList>(await _databaseDistributionListService.ListAsync());
        }

        private void UpdateTreeView()
        {
            treeViewToAdd.BeginUpdate();
            treeViewToAdd.Nodes.Clear();

            foreach (var conn in _connections)
            {
                var node = new TreeNode(conn.DisplayName)
                {
                    Tag = conn, // Store the full object
                    // Do not check the root node
                    ImageKey = "disconnected",
                    SelectedImageKey = "disconnected"
                };
                treeViewToAdd.Nodes.Add(node);
            }

            treeViewToAdd.EndUpdate();
        }

        // Event handlers (placed at the end as requested)

        private async void DatabaseDistributionListForm_Load(object sender, EventArgs e)
        {
            await BindData();
        }

        private void Connections_ListChanged(object sender, ListChangedEventArgs e)
        {
            // Whenever the list changes, update the TreeView
            UpdateTreeView();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            // TODO: Implement remove logic
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // TODO: Implement add logic
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

        private async void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Only process if the node is root (has no children yet)
            if (e.Node.Level == 0 && e.Node.Nodes.Count == 0)
            {
                if (e.Node.Tag is Connection conn)
                {
                    try
                    {
                        // Fetch databases for the connection
                        var databases = await _connectionService.ListDatabasesAsync(conn);

                        // Add each database as a child node
                        foreach (var db in databases)
                        {
                            var dbNode = new TreeNode(db.DatabaseName)
                            {
                                Tag = new { Connection = conn, Database = db }, // Store info
                                Checked = false,
                                ImageKey = "database",
                                SelectedImageKey = "database"
                            };
                            e.Node.Nodes.Add(dbNode);
                        }

                        e.Node.ImageKey = "connected";
                        e.Node.SelectedImageKey = "connected";

                        // Automatically expand
                        e.Node.Expand();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading databases:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Prevent infinite recursion
            treeViewToAdd.AfterCheck -= TreeView_AfterCheck;

            try
            {
                // Update children
                if (e.Node.Nodes.Count > 0)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                    }
                }

                // Update parent recursively
                UpdateParentCheckState(e.Node);
            }
            finally
            {
                treeViewToAdd.AfterCheck += TreeView_AfterCheck;
            }
        }

        // Check the parent if all children are selected, uncheck otherwise
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

            // Propagate upwards
            UpdateParentCheckState(node.Parent);
        }
    }
}