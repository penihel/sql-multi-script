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
        private const int TopHeight = 70;
        private const int FooterpHeight = 70;

        // Services
        private readonly IDatabaseDistributionListService _databaseDistributionListService;
        private readonly IConnectionService _connectionService;
        private readonly IServiceProvider _serviceProvider;

        // UI Controls
        private DataGridView dataGridViewDatabases;
        private TreeView treeViewToAdd;


        private ComboBox comboBoxDatabaseDistributionList;



        // Data
        private BindingList<Connection> _connections;
        private BindingList<DatabaseDistributionList> _databaseDistributionLists = new BindingList<DatabaseDistributionList>();


        private DatabaseDistributionList _currentDatabaseDistributionList = null;

        //Properties
        private Guid _selectedDistributionListId;

        public Guid SelectedDistributionListId
        {
            get => _selectedDistributionListId;
            set
            {
                if (SetProperty(ref _selectedDistributionListId, value))
                {
                    LoadCurrentDistributionList();
                }
            }
        }
        private void LoadCurrentDistributionList()
        {
            if (_selectedDistributionListId == Guid.Empty)
            {
                _currentDatabaseDistributionList = null;
                dataGridViewDatabases.DataSource = null;
                return;
            }

            _currentDatabaseDistributionList =
                _databaseDistributionLists
                .FirstOrDefault(x => x.Id == _selectedDistributionListId);

            dataGridViewDatabases.DataSource = _currentDatabaseDistributionList?.Databases;
        }


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
                RowCount = 4,
                Padding = UIConstants.PanelPadding
            };

            // Set column and row sizes
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, TopHeight));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, FooterpHeight));


            var panelCol0Row0 = PanelFactory.Create();
            var panelCol0Row1 = PanelFactory.Create();
            var panelCol1Row0 = PanelFactory.Create();
            var panelCol1Row1 = PanelFactory.Create();
            var panelCol2Row0 = PanelFactory.Create();
            var panelCol2Row1 = PanelFactory.Create();
            var panelBottonCol0 = PanelFactory.Create();

            var divider = PanelFactory.CreateDivider();

            mainTableLayoutPanel.Controls.Add(panelCol0Row0, 0, 0);
            mainTableLayoutPanel.Controls.Add(panelCol0Row1, 0, 1);
            mainTableLayoutPanel.Controls.Add(panelCol1Row0, 1, 0);
            mainTableLayoutPanel.Controls.Add(panelCol1Row1, 1, 1);
            mainTableLayoutPanel.Controls.Add(panelCol2Row0, 2, 0);
            mainTableLayoutPanel.Controls.Add(panelCol2Row1, 2, 1);
            mainTableLayoutPanel.Controls.Add(divider, 0, 2);
            mainTableLayoutPanel.SetColumnSpan(divider, 3);
            mainTableLayoutPanel.Controls.Add(panelBottonCol0, 0, 3);
            mainTableLayoutPanel.SetColumnSpan(panelBottonCol0, 3);


            Controls.Add(mainTableLayoutPanel);


            SetupLeftTop(panelCol0Row0);
            SetupLeftBody(panelCol0Row1);
            SetupCenterBody(panelCol1Row1);
            SetupRightTop(panelCol2Row0);
            SetupRightBody(panelCol2Row1);
            SetupBottomRow(panelBottonCol0);



        }

        private void SetupRightTop(Panel parentPanel)
        {



            // ComboBox for distribution lists
            comboBoxDatabaseDistributionList = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                //Dock = DockStyle.Left
            };



            // New Distribution List Button
            var btnNewDatabaseDistribuitionList = ButtonFactory.Create(ToolTip,
                Strings.NewDatabaseDistributionList,
                Images.ic_fluent_add_24_regular,
                BtnNewDatabaseDistribuitionList_Click,
                DockStyle.Right);



            var btnRenameDatabaseDistribuitionList = ButtonFactory.Create(ToolTip,
                Strings.Rename,
                Images.ic_fluent_rename_24_regular,
                null,
                DockStyle.Right);

            var btnRemoveDatabaseDistribuitionList = ButtonFactory.Create(ToolTip,
                Strings.Remove,
                Images.ic_fluent_delete_24_regular,
                null,
                DockStyle.Right);


            parentPanel.Controls.Add(btnNewDatabaseDistribuitionList);
            parentPanel.Controls.Add(btnRenameDatabaseDistribuitionList);
            parentPanel.Controls.Add(btnRemoveDatabaseDistribuitionList);

            parentPanel.Controls.Add(comboBoxDatabaseDistributionList);


            comboBoxDatabaseDistributionList.AlignAndStretch();


        }

        private void SetupLeftTop(Panel parentPanel)
        {
            // New Connection Button
            var btnNew = ButtonFactory.Create(ToolTip,
                Strings.NewConnection,
                Images.ic_fluent_new_24_regular,
                BtnNew_Click,
                DockStyle.Right);


            parentPanel.Controls.Add(btnNew);
        }

        private void SetupLeftBody(Panel parentPanel)
        {


            var label = LabelFactory.Create(Strings.DatabasesToAdd, DockStyle.Top);

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




            parentPanel.Controls.Add(treeViewToAdd);
            parentPanel.Controls.Add(label);
        }

        private void SetupCenterBody(Panel parentPanel)
        {

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                
            };


            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            
            var flow = PanelFactory.CreateFlowLayoutPanel(FlowDirection.TopDown)
                .Customize(c => c.AutoSize = true)
                .Customize(c => c.Anchor = AnchorStyles.None);
            

            // Add Button
            var btnAdd = ButtonFactory.Create(ToolTip,
                Strings.Add,
                Images.ic_fluent_arrow_circle_right_24_regular,
                BtnAdd_Click);

            // Remove Button
            var btnRemove = ButtonFactory.Create(ToolTip,
                Strings.Remove,
                Images.ic_fluent_arrow_circle_left_24_regular,
                BtnRemove_Click);

            
            // Adiciona botões ao flow
            flow.Controls.Add(btnAdd);
            flow.Controls.Add(btnRemove);

            
            table.Controls.Add(flow, 0, 1);

            
            parentPanel.Controls.Add(table);

            
            
        }


        private void SetupRightBody(Panel parentPanel)
        {


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



            // Database name column
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DatabaseName",
                HeaderText = Strings.DatabasesToExecute,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabases.Columns.Add(colName);


            // Database name column
            var colServer = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ConnectionName",
                HeaderText = Strings.Connection,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabases.Columns.Add(colServer);






            // Add to TableLayout
            parentPanel.Controls.Add(dataGridViewDatabases);
        }

        private void SetupBottomRow(Panel parentPanel)
        {


            var btnSave = ButtonFactory
                .Create(ToolTip,
                    Strings.Save,
                    Images.ic_fluent_save_24_regular,
                    BtnSave_Click,
                    DockStyle.Right);


            var btnCancel = ButtonFactory
                .Create(ToolTip,
                    Strings.Cancel,
                    Images.ic_fluent_dismiss_24_regular,
                    null,
                    DockStyle.Right);




            // Adiciona ao TableLayoutPanel
            parentPanel.Controls.Add(btnSave);
            parentPanel.Controls.Add(btnCancel);

        }





        // Data binding and loading
        private async Task BindDataAsync()
        {
            await LoadConnectionsAsync();
            await LoadDistribuitionListsAsync();

            // Whenever the list changes, update the TreeView
            UpdateTreeView();

            comboBoxDatabaseDistributionList.DataSource = _databaseDistributionLists;
            comboBoxDatabaseDistributionList.DisplayMember = "Name";
            comboBoxDatabaseDistributionList.ValueMember = "Id";

            if (comboBoxDatabaseDistributionList.DataBindings.Count == 0)
                comboBoxDatabaseDistributionList.DataBindings.Add("SelectedValue", this, nameof(SelectedDistributionListId), true, DataSourceUpdateMode.OnPropertyChanged);
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
                var node = new TreeNode(conn.Name)
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
            await BindDataAsync();
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
            if (SelectedDistributionListId == Guid.Empty)
            {
                MessageBox.Show(Strings.NoDistributionListSelected);
                comboBoxDatabaseDistributionList.Focus();
                SendKeys.Send("{F4}");
                return;
            }

            var selectedLeafNodes = treeViewToAdd
                .Nodes
                .Cast<TreeNode>()
                .SelectMany(n => TreeNodeUtil.GetAllNodes(n))
                .Where(n => n.Nodes.Count == 0 && n.Checked)
                .ToList();

            if (selectedLeafNodes.Count == 0)
            {
                MessageBox.Show(Strings.NoDatabaseSelected);
                return;

            }

            foreach (var item in selectedLeafNodes)
            {
                var conn = (Connection)item.Parent.Tag;
                var db = (Database)item.Tag;

                if (!_currentDatabaseDistributionList.Databases.Any(d => d.DatabaseName == db.DatabaseName && d.ConnectionName == db.ConnectionName))
                    _currentDatabaseDistributionList.Databases.Add(db);
            }

            dataGridViewDatabases.Refresh();
        }




        private async void BtnNew_Click(object sender, EventArgs e)
        {
            var newConnectionForm = _serviceProvider.GetRequiredService<NewConnectionForm>();
            var result = newConnectionForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                await BindDataAsync();
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
                                Tag = db, // Store info
                                Checked = false,
                                ImageKey = "database",
                                SelectedImageKey = "database",

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

        private async void BtnNewDatabaseDistribuitionList_Click(object sender, EventArgs e)
        {
            string name = Prompt.ShowDialog(Strings.DatabaseDistributionListEnterPrompt, Strings.NewDatabaseDistributionList);

            if (!string.IsNullOrWhiteSpace(name))
            {

                var result = await _databaseDistributionListService.CreateAsync(name);

                if (!result.Success)
                {
                    MessageBox.Show(result.ToString());
                    return;
                }

                await BindDataAsync();

                SelectedDistributionListId = result.Value;



            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {

            foreach (var item in _databaseDistributionLists)
            {
                var result = await _databaseDistributionListService.SaveAsync(item);

                if (!result.Success)
                {
                    MessageBox.Show(result.ToString());
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}