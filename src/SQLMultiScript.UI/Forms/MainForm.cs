using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Resources;
using SQLMultiScript.UI.ControlFactories;
using SQLMultiScript.UI.UserControls;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace SQLMultiScript.UI.Forms
{
    public class MainForm : BaseForm
    {
        // -----------------------
        // Constants
        // -----------------------
        private const int TopHeight = 60;


        // -----------------------
        // Services
        // -----------------------
        private readonly IProjectService _projectService;
        private readonly IDatabaseDistributionListService _databaseDistributionListService;
        private readonly IExecutionService _executionService;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;


        // -----------------------
        // Fields
        // -----------------------
        private Project _currentProject = null;
        private Script _activeScript = null;

        private BindingList<Execution> _executions = new BindingList<Execution>();

        private CancellationTokenSource _executionCts;


        // -----------------------
        // Controls
        // -----------------------
        private DataGridView
            dataGridViewScripts,
            dataGridViewDatabases,
            dataGridViewDatabasesResults;



        private TreeView treeViewExecutions;

        private Scintilla sqlEditor;

        private MenuStrip menuStrip;

        private OutputMessagesPanel
            outputMessagesLog,
            outputMessagesResults;

        private ComboBox comboBoxDatabaseDistributionList;

        private ImageList imageListResults;

        private TabControl tabControlMessagesAndResults;

        private Button btnRun, btnStop;

        private System.Windows.Forms.Timer _refreshTimer;
        private bool _resultsDirty;


        // -----------------------
        // Properties
        // -----------------------

        #region DatabaseDistributionLists

        private BindingList<DatabaseDistributionList> _databaseDistributionLists;

        public BindingList<DatabaseDistributionList> DatabaseDistributionLists
        {
            get => _databaseDistributionLists;
            set
            {
                if (SetProperty(ref _databaseDistributionLists, value))
                {
                    DatabaseDistributionListsChanged();
                }
            }
        }

        private void DatabaseDistributionListsChanged()
        {
            comboBoxDatabaseDistributionList.Refresh();
            dataGridViewDatabases.Refresh();
        }

        #endregion


        #region SelectedDistributionList

        private DatabaseDistributionList _selectedDistributionList;

        public DatabaseDistributionList SelectedDistributionList
        {
            get => _selectedDistributionList;
            set
            {
                if (SetProperty(ref _selectedDistributionList, value))
                {
                    SelectedDistributionListChanged();
                }
            }
        }
        private void SelectedDistributionListChanged()
        {
            if (_selectedDistributionList == null)
            {

                dataGridViewDatabases.DataSource = null;
                _currentProject.SelectedDistributionList = null;
                return;
            }

            _currentProject.SelectedDistributionList = _selectedDistributionList.Name;
            dataGridViewDatabases.DataSource = _selectedDistributionList?.Databases;



            dataGridViewDatabases.Refresh();
        }

        #endregion


        #region SelectedExecutionScriptInfo

        private ExecutionScriptInfo _selectedExecutionScriptInfo;
        public ExecutionScriptInfo SelectedExecutionScriptInfo
        {
            get => _selectedExecutionScriptInfo;
            set
            {
                if (SetProperty(ref _selectedExecutionScriptInfo, value))
                {


                    SelectedExecutionScriptInfoChanged();
                }
            }
        }

        private void SelectedExecutionScriptInfoChanged()
        {

            outputMessagesResults.ClearMessages();
            tabControlMessagesAndResults.TabPages.Clear();

            if (SelectedExecutionScriptInfo == null)
            {
                dataGridViewDatabasesResults.DataSource = null;
                dataGridViewDatabasesResults.Refresh();

                return;
            }

            dataGridViewDatabasesResults.DataSource = SelectedExecutionScriptInfo.DatabasesInfo;
            dataGridViewDatabasesResults.Refresh();




            // Aba Messages
            var tabMessages = new TabPage(Strings.Messages);

            tabControlMessagesAndResults.TabPages.Add(tabMessages);


            tabMessages.Controls.Add(outputMessagesResults);


            foreach (var databaseInfo in SelectedExecutionScriptInfo.DatabasesInfo)
            {
                if (databaseInfo.Response != null)
                {
                    if (!string.IsNullOrEmpty(databaseInfo.Response.MessagesText))
                    {
                        if (databaseInfo.Status == ExecutionStatus.Error)
                        {
                            outputMessagesResults.AppendError(databaseInfo.Response.MessagesText);
                        }
                        else
                        {
                            outputMessagesResults.AppendInfo(databaseInfo.Response.MessagesText);
                        }
                    }


                }
            }

            if (SelectedExecutionScriptInfo.DataSet != null &&
                SelectedExecutionScriptInfo.DataSet.Tables.Count > 0)
            {


                foreach (DataTable table in SelectedExecutionScriptInfo.DataSet.Tables)
                {






                    var tabResult = new TabPage(table.TableName);

                    tabControlMessagesAndResults.TabPages.Add(tabResult);



                    tabResult.Controls.Add(DataGridViewFactory.CreateToResult(table));

                }
            }



            tabControlMessagesAndResults.SelectedIndex = tabControlMessagesAndResults.TabCount - 1;



        }



        #endregion


        /// <summary>
        /// Constructor Default
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="projectService"></param>
        /// <param name="databaseDistributionListService"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="executionService"></param>
        public MainForm(
            ILogger logger,
            IProjectService projectService,
            IDatabaseDistributionListService databaseDistributionListService,
            IServiceProvider serviceProvider,
            IExecutionService executionService)
        {
            _logger = logger;
            _projectService = projectService;
            _databaseDistributionListService = databaseDistributionListService;
            _serviceProvider = serviceProvider;
            _executionService = executionService;

            _executionService.UiContext = SynchronizationContext.Current;

            BindEvents();

            InitializeLayout();
        }


        private void BindEvents()
        {
            _executionService.InfoMessageRecived += ExecutionService_InfoMessageRecived;
            _executionService.ErrorOccurred += ExecutionService_ErrorOccurred;
            _executionService.Log += Log;
            _executionService.TableAdded += ExecutionService_TableAdded;
            _executionService.RowAdded += ExecutionService_RowAdded;
        }

        private void ExecutionService_ErrorOccurred(ExecutionScriptInfo arg1, ExecutionDatabaseInfo arg2, Exception arg3)
        {
            var prefix = $"[{arg2.Database.DatabaseName}]";
            do
            {
                outputMessagesResults.AppendError($"{prefix} {arg3.Message}");
                arg3 = arg3.InnerException;
            } while (arg3 != null);
        }

        private void ExecutionService_RowAdded(ExecutionScriptInfo arg2, ExecutionDatabaseInfo arg3, DataTable arg4, DataRow arg5)
        {
            _resultsDirty = true;
        }

        private void ExecutionService_TableAdded(ExecutionScriptInfo arg2, ExecutionDatabaseInfo arg3, DataTable table)
        {
            if (tabControlMessagesAndResults.TabPages.Cast<TabPage>().Any(tp => tp.Text == table.TableName))
            {
                return;
            }

            var tabResult = new TabPage(table.TableName);

            tabControlMessagesAndResults.TabPages.Add(tabResult);

            tabResult.Controls.Add(DataGridViewFactory.CreateToResult(table));

            _resultsDirty = true;
        }

        private void ExecutionService_InfoMessageRecived(ExecutionScriptInfo scriptInfo, ExecutionDatabaseInfo databaseInfo, string message)
        {
            outputMessagesResults.AppendInfo(message);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!_resultsDirty) return;
            _resultsDirty = false;

            var selectedTab = tabControlMessagesAndResults.SelectedTab;
            if (selectedTab != null)
            {
                foreach (Control c in selectedTab.Controls)
                {
                    if (c is DataGridView dgv && dgv.DataSource is BindingSource bs)
                    {
                        bs.ResetBindings(false);
                        if (dgv.RowCount > 0)
                            dgv.FirstDisplayedScrollingRowIndex = dgv.RowCount - 1;
                    }
                }
            }

            dataGridViewDatabasesResults.Refresh();
        }

        /// <summary>
        /// Initialize Layout (root method)
        /// </summary>
        private void InitializeLayout()
        {

            InitImageLists();


            Text = $"{Constants.ApplicationName} - {Constants.ApplicationVersion}";
            Icon = new Icon("sql-multi-script.ico");
            WindowState = FormWindowState.Maximized;


            Load += MainForm_Load;


            // main Split (top/bottom)
            var mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
            };

            Controls.Add(mainSplitContainer);

            // Split Left
            var splitLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 10
            };

            mainSplitContainer.Panel1.Controls.Add(splitLeft);
            mainSplitContainer.Panel1.Padding = Padding.Empty;

            // Setup Scripts Panel
            SetupScriptsPanel(splitLeft.Panel1);

            // Split center/right
            var splitCenterRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = GetPercentOfScreenWidth(0.7M)
            };
            splitLeft.Panel2.Controls.Add(splitCenterRight);

            //Setup Editor Panel
            SetupEditorPanel(splitCenterRight.Panel1);

            //Setup DAtabaseDistributionList panel
            SetupDatabaseDistributionListPanel(splitCenterRight.Panel2);

            // Split footer/result
            var splitResultFooter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = GetPercentOfScreenHeight(0.5M)
            };
            mainSplitContainer.Panel2.Controls.Add(splitResultFooter);



            SetupResultPanel(splitResultFooter.Panel1);


            var logContainer = PanelFactory.Create();

            outputMessagesLog = new OutputMessagesPanel();

            logContainer.Controls.Add(outputMessagesLog);

            splitResultFooter.Panel2.Controls.Add(logContainer);

            InitializeMenu();
        }

        private void InitImageLists()
        {
            imageListResults = new ImageList()
            {
                ImageSize = new Size(16, 16)
            };

            imageListResults.Images.Add(nameof(ExecutionStatus.Queued), Images.circle_gray);
            imageListResults.Images.Add(nameof(ExecutionStatus.Executing), Images.circle_blue);
            imageListResults.Images.Add(nameof(ExecutionStatus.Error), Images.circle_red);
            imageListResults.Images.Add(nameof(ExecutionStatus.Success), Images.circle_green);
            imageListResults.Images.Add(nameof(ExecutionStatus.Cancelled), Images.circle_gray);
        }

        private void SetupResultPanel(Panel panel)
        {

            // Split esquerda
            var splitLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 10
            };
            panel.Controls.Add(splitLeft);

            treeViewExecutions = new TreeView
            {
                Dock = DockStyle.Fill
            };





            treeViewExecutions.ImageList = imageListResults;

            treeViewExecutions.AfterSelect += treeViewExecutions_AfterSelect;

            var treeContainer = PanelFactory.Create();



            treeContainer.Controls.Add(treeViewExecutions);

            splitLeft.Panel1.Controls.Add(treeContainer);

            var splitDatabasesAndResults = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 10
            };

            splitLeft.Panel2.Controls.Add(splitDatabasesAndResults);


            // -----------------------
            // DataGridView
            // -----------------------
            dataGridViewDatabasesResults = new DataGridView
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
                BackgroundColor = Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D,
            };

            // Checkbox with select-all header
            dataGridViewDatabasesResults.AddCheckBoxColumnWithSelectAll(nameof(Database.Selected));

            // Database name column
            var colStatus = new DataGridViewImageColumn
            {
                Name = "colStatus",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                //ReadOnly = true,
                HeaderText = string.Empty
            };
            dataGridViewDatabasesResults.Columns.Add(colStatus);

            dataGridViewDatabasesResults.CellFormatting += (s, e) =>
            {
                if (dataGridViewDatabasesResults.Columns[e.ColumnIndex].Name == "colStatus")
                {
                    var item = (ExecutionDatabaseInfo)dataGridViewDatabasesResults.Rows[e.RowIndex].DataBoundItem;
                    e.Value = imageListResults.Images[item.Status.ToString()];
                }
            };

            // Database name column
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DatabaseName",
                HeaderText = Strings.ExecutedDatabases,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabasesResults.Columns.Add(colName);


            // Database name column
            var colServer = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ConnectionName",
                HeaderText = Strings.Connection,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                ReadOnly = true
            };
            dataGridViewDatabasesResults.Columns.Add(colServer);


            var gridContainer = PanelFactory.Create();

            gridContainer.Controls.Add(dataGridViewDatabasesResults);


            //PANEL RESULTIS
            var panelResults = PanelFactory.Create();

            tabControlMessagesAndResults = new TabControl { Dock = DockStyle.Fill };

            panelResults.Controls.Add(tabControlMessagesAndResults);

            outputMessagesResults = new OutputMessagesPanel();

            splitDatabasesAndResults.Panel1.Controls.Add(gridContainer);
            splitDatabasesAndResults.Panel2.Controls.Add(panelResults);
        }

        private void treeViewExecutions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level != 1)
            {
                SelectedExecutionScriptInfo = null;
                return;
            }

            var executionScriptInfo = e.Node.Tag as ExecutionScriptInfo;

            SelectedExecutionScriptInfo = executionScriptInfo;

        }

        private void SetupExecutionButtons(Panel buttonPanel)
        {
            btnRun = ButtonFactory.Create(ToolTip,
                Strings.Execute,
                Images.ic_fluent_play_multiple_16_regular,
                BtnRun_Click,
                DockStyle.Left)
                .Customize(b => b.AutoSize = true)
                .Customize(b => b.TextImageRelation = TextImageRelation.ImageBeforeText)
                .Customize(b => b.Text = Strings.Execute);

            btnStop = ButtonFactory.Create(ToolTip,
                Strings.Cancel,
                Images.ic_fluent_dismiss_24_regular,
                BtnStop_Click,
                DockStyle.Left)
                .Customize(b => b.AutoSize = true)
                .Customize(b => b.TextImageRelation = TextImageRelation.ImageBeforeText)
                .Customize(b => b.Text = Strings.Cancel)
                .Customize(b => b.Visible = false);

            buttonPanel.Controls.Add(btnStop);
            buttonPanel.Controls.Add(btnRun);
        }

        private void SetupDatabaseDistributionListPanel(Panel parentPanel)
        {
            // -----------------------
            // DataGridView
            // -----------------------
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
                BackgroundColor = Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D,
            };

            // Checkbox with select-all header
            dataGridViewDatabases.AddCheckBoxColumnWithSelectAll("Selected");

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
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                ReadOnly = true
            };
            dataGridViewDatabases.Columns.Add(colServer);

            



            // -----------------------
            // Painel do grid
            // -----------------------
            var gridPanel = PanelFactory.Create();


            gridPanel.Controls.Add(dataGridViewDatabases);

            parentPanel.Controls.Add(gridPanel);    // grid primeiro

            // -----------------------
            // TableLayoutPanel de Botões
            // -----------------------
            var topTableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = TopHeight,
                ColumnCount = 2,
                RowCount = 1,
            };
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Combo ocupa o espaço todo
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TopHeight)); // Botão fixo em 50px

            // Cria o ComboBox
            comboBoxDatabaseDistributionList = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right, // só alinha horizontal

                DropDownStyle = ComboBoxStyle.DropDownList,


            };

            comboBoxDatabaseDistributionList.SelectedIndexChanged += comboBoxDatabaseDistributionList_SelectedIndexChanged;


            topTableLayoutPanel.Controls.Add(comboBoxDatabaseDistributionList, 0, 0);

            var btnDatabaseDistributionList = ButtonFactory.Create(ToolTip,
                Strings.DatabaseDistributionLists,
                Images.ic_fluent_database_stack_16_regular,
                btnDatabaseDistributionList_Click);







            topTableLayoutPanel.Controls.Add(btnDatabaseDistributionList, 1, 0);

            parentPanel.Controls.Add(topTableLayoutPanel);  // botões por cima





        }

        private void comboBoxDatabaseDistributionList_SelectedIndexChanged(object sender, EventArgs e)

        {
            SelectedDistributionList = comboBoxDatabaseDistributionList.SelectedItem as DatabaseDistributionList;
        }


        private void SetupScriptsPanel(Panel parentPanel)
        {
            // -----------------------
            // DataGridView
            // -----------------------
            dataGridViewScripts = new DataGridView
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
                BackgroundColor = Color.White,
                BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D

            };

            // Checkbox with select-all header
            dataGridViewScripts.AddCheckBoxColumnWithSelectAll("Selected");

            // Nome do Script
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Script",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewScripts.Columns.Add(colName);

            dataGridViewScripts.CellClick += DataGridViewScripts_CellClick;


            //Menu de contexto (botão direito) do grid

            var contextMenu = new ContextMenuStrip();
            var removeItem = new ToolStripMenuItem(Resources.Strings.RemoveSelectedRows);
            removeItem.Click += ToolStripMenuItemRemove_Click;
            removeItem.Image = Images.ic_fluent_delete_24_regular;
            removeItem.ImageAlign = ContentAlignment.MiddleLeft;
            removeItem.ShortcutKeys = Keys.Delete;
            contextMenu.Items.Add(removeItem);

            dataGridViewScripts.ContextMenuStrip = contextMenu;


            //KeyDown no Grid
            dataGridViewScripts.KeyDown += (s, e) =>
            {
                //TEntou deletar
                if (e.KeyCode == Keys.Delete)
                {
                    e.Handled = true;
                    RemoveScripts();
                }
            };

            // -----------------------
            // Painel do grid
            // -----------------------
            var listPanel = PanelFactory.Create();

            listPanel.Controls.Add(dataGridViewScripts);

            parentPanel.Controls.Add(listPanel);    // grid primeiro


            // -----------------------
            // Painel de Botões
            // -----------------------
            var buttonPanel = PanelFactory.Create(TopHeight, DockStyle.Top);





            var btnDown = ButtonFactory.Create(ToolTip,
                Strings.Down,
                Images.ic_fluent_arrow_circle_down_24_regular,
                BtnDown_Click,
                DockStyle.Left);


            var btnUp = ButtonFactory.Create(ToolTip,
                Strings.Up,
                Images.ic_fluent_arrow_circle_up_24_regular,
                BtnUp_Click,
                DockStyle.Left);


            var btnRemove = ButtonFactory.Create(ToolTip,
                Strings.Remove,
                Images.ic_fluent_delete_24_regular,
                BtnRemove_Click,
                DockStyle.Left);






            buttonPanel.Controls.Add(btnRemove);
            buttonPanel.Controls.Add(btnUp);
            buttonPanel.Controls.Add(btnDown);


            //
            var btnNew = ButtonFactory.Create(ToolTip,
                Strings.New,
                Images.ic_fluent_new_24_regular,
                BtnNew_Click,
                DockStyle.Right);

            var btnAdd = ButtonFactory.Create(ToolTip,
                Strings.AddExisting,
                Images.ic_fluent_add_24_regular,
                BtnAdd_Click,
                DockStyle.Right);


            buttonPanel.Controls.Add(btnNew);
            buttonPanel.Controls.Add(btnAdd);





            parentPanel.Controls.Add(buttonPanel);  // botões por cima

        }



        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();

            // -------------------------------
            // Menu File
            // -------------------------------
            var fileMenu = new ToolStripMenuItem(Resources.Strings.File);

            var newProjectItem = new ToolStripMenuItem("New Project");
            newProjectItem.Click += NewProjectItem_Click;
            fileMenu.DropDownItems.Add(newProjectItem);

            var openProjectItem = new ToolStripMenuItem("Open Project");
            openProjectItem.Click += OpenProjectItem_Click;
            fileMenu.DropDownItems.Add(openProjectItem);

            var saveProjectItem = new ToolStripMenuItem("Save Project");
            saveProjectItem.Click += SaveProjectItem_Click;
            fileMenu.DropDownItems.Add(saveProjectItem);

            var closeProjectItem = new ToolStripMenuItem("Close Project");
            closeProjectItem.Click += CloseProjectItem_Click;
            fileMenu.DropDownItems.Add(closeProjectItem);

            fileMenu.DropDownItems.Add(new ToolStripSeparator()); // divisor

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;
            fileMenu.DropDownItems.Add(exitItem);

            menuStrip.Items.Add(fileMenu);

            // -------------------------------
            // Menu About
            // -------------------------------
            var aboutMenu = new ToolStripMenuItem("About");
            aboutMenu.Click += AboutMenu_Click;
            menuStrip.Items.Add(aboutMenu);

            // -------------------------------
            // Adiciona no form
            // -------------------------------
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }



        private void SetupEditorPanel(Panel parentPanel)
        {

            var editorPanel = PanelFactory.Create();
            //{
            //    Dock = DockStyle.Fill,
            //    Padding = UIConstants.PanelPadding,
            //    BackColor = Color.Transparent // opcional
            //};




            // Editor SQL (centro)
            sqlEditor = new Scintilla();
            sqlEditor.Dock = DockStyle.Fill;
            sqlEditor.Enabled = false;
            sqlEditor.TextChanged += SqlEditor_TextChanged;

            sqlEditor.StyleResetDefault();
            sqlEditor.Styles[Style.Default].Font = "Consolas";
            sqlEditor.Styles[Style.Default].Size = 10;
            sqlEditor.StyleClearAll();
            sqlEditor.Margins[0].Width = 40;
            sqlEditor.LexerName = "sql";

            sqlEditor.Styles[Style.Sql.Comment].ForeColor = Color.Green;
            sqlEditor.Styles[Style.Sql.Number].ForeColor = Color.Orange;
            sqlEditor.Styles[Style.Sql.Word].ForeColor = Color.Blue;
            sqlEditor.Styles[Style.Sql.String].ForeColor = Color.Brown;

            sqlEditor.SetKeywords(0, "SELECT FROM WHERE INSERT UPDATE DELETE CREATE ALTER DROP JOIN ON AND OR NOT NULL");

            sqlEditor.IndentWidth = 4;
            sqlEditor.TabWidth = 4;
            sqlEditor.UseTabs = false;

            //sqlEditor.CaretLineVisible = true;
            sqlEditor.CaretLineBackColor = Color.LightYellow;






            editorPanel.Controls.Add(sqlEditor);



            parentPanel.Controls.Add(editorPanel); // editor primeiro



            // -----------------------
            // Painel de Botões
            // -----------------------
            var buttonPanel = PanelFactory.Create(TopHeight, DockStyle.Top);

            // Botão Salvar no canto direito
            var btnSave = ButtonFactory.Create(ToolTip,
                Strings.Save,
                Images.ic_fluent_save_24_regular,
                BtnSave_Click,
                DockStyle.Right);


            buttonPanel.Controls.Add(btnSave);

            SetupExecutionButtons(buttonPanel);

            parentPanel.Controls.Add(buttonPanel);

        }



        private bool CheckUnsavedChanges()
        {
            if (_currentProject != null && _currentProject.Scripts.Where(s => s.IsDirty).Any())
            {
                var result = MessageBox.Show(
                    "Existem scripts não salvos. Deseja continuar e perder essas alterações?",
                    "Atenção",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                return result == DialogResult.Yes;
            }
            return true;
        }
        private async Task NewProjectAsync()
        {

            if (!CheckUnsavedChanges())
                return;

            _activeScript = null;
            sqlEditor.Text = string.Empty;

            _currentProject = await _projectService.CreateNewAsync();

            if (_currentProject != null)
            {

                var firstScript = _currentProject.Scripts.FirstOrDefault();
                if (firstScript != null)
                {
                    ShowScriptOnEditor(firstScript);
                }

                if (!string.IsNullOrEmpty(_currentProject.SelectedDistributionList))
                {
                    SelectedDistributionList = DatabaseDistributionLists?.FirstOrDefault(d => d.Name == _currentProject.SelectedDistributionList);
                }
                else
                {
                    SelectedDistributionList = DatabaseDistributionLists?.FirstOrDefault();

                }

            }


        }



        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;


            comboBoxDatabaseDistributionList.DataSource = DatabaseDistributionLists;
            comboBoxDatabaseDistributionList.DisplayMember = "Name";
            comboBoxDatabaseDistributionList.ValueMember = "Name";
            comboBoxDatabaseDistributionList.DataBindings.Add("SelectedItem", this, nameof(SelectedDistributionList), true, DataSourceUpdateMode.OnPropertyChanged);

            // Refresh grids
            dataGridViewScripts.Refresh();

        }

        private void ShowScriptOnEditor(Script script)
        {
            // Salva conteúdo do script anterior em memória
            if (_activeScript != null)
            {
                _activeScript.Content = sqlEditor.Text;
                _activeScript.IsDirty = true;
            }

            _activeScript = script;

            sqlEditor.Enabled = true;
            // Se o script tem conteúdo em memória, carrega
            if (!string.IsNullOrEmpty(_activeScript.Content))
            {
                sqlEditor.Text = _activeScript.Content;

            }
            else if (!string.IsNullOrEmpty(_activeScript.FilePath) && File.Exists(_activeScript.FilePath))
            {
                _activeScript.Content = File.ReadAllText(_activeScript.FilePath);
                sqlEditor.Text = _activeScript.Content;
            }
            else
            {
                // Novo script em branco
                _activeScript.Content = string.Empty;
                sqlEditor.Text = string.Empty;
                _activeScript.IsDirty = true;

            }

            Log($"Script ativo: {_activeScript.Name}");
        }
        private bool SaveScript(Script script)
        {




            // Se não tem caminho, pede ao usuário
            if (string.IsNullOrEmpty(script.FilePath))
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                    FileName = script.Name
                };

                if (sfd.ShowDialog() != DialogResult.OK) return false;

                script.FilePath = sfd.FileName;
                script.Name = Path.GetFileName(sfd.FileName);


            }

            try
            {
                File.WriteAllText(script.FilePath, script.Content ?? string.Empty);
                script.IsDirty = false;

                Log($"Script salvo: {script.FilePath}");

                return true;
            }
            catch (Exception ex)
            {
                Log($"Erro ao salvar {script.FilePath}: {ex.Message}", true);

                return false;
            }


        }

        private async Task<bool> SaveProjectAsync(Project project)
        {
            bool savedAllScripts = true;

            foreach (var script in _currentProject.Scripts.Where(s => s.IsDirty))
            {
                savedAllScripts = SaveScript(script);

                if (!savedAllScripts)
                    break;
            }

            if (!savedAllScripts) return false;

            // Se não tem caminho, pede ao usuário
            if (string.IsNullOrEmpty(project.FilePath))
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "SQL Multi Script Project Files (*.smsjsonproj)|*.smsjsonproj",
                    FileName = project.Name ?? "NewProject.smsjsonproj"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return false;

                project.FilePath = sfd.FileName;
                project.Name = Path.GetFileNameWithoutExtension(sfd.FileName);
            }

            try
            {
                await _projectService.SaveAsync(project);

                Log($"Project save: {project.FilePath}");

                return true;
            }
            catch (Exception ex)
            {
                Log($"Erro ao salvar {project.FilePath}: {ex.Message}", true);

                return false;
            }


        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await LoadDistribuitionListsAsync();
            await NewProjectAsync();


            BindData();
        }

        private async Task LoadDistribuitionListsAsync()
        {

            DatabaseDistributionLists =
                new BindingList<DatabaseDistributionList>(await _databaseDistributionListService.ListAsync());



        }

        private void SqlEditor_TextChanged(object sender, EventArgs e)
        {
            if (_activeScript != null)
            {
                _activeScript.IsDirty = true;
                _activeScript.Content = sqlEditor.Text;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;

            using var ofd = new OpenFileDialog { Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*", Multiselect = true };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            foreach (var file in ofd.FileNames)
            {
                _currentProject.Scripts.Add(new Script
                {
                    FilePath = file,
                    Name = Path.GetFileName(file),
                    Selected = true
                });
                Log($"Script adicionado: {file}");
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            RemoveScripts();
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            if (_currentProject == null || dataGridViewScripts.SelectedRows.Count != 1) return;

            int index = dataGridViewScripts.SelectedRows[0].Index;
            if (index <= 0) return;

            var scripts = _currentProject.Scripts;
            var item = scripts[index];
            scripts.RemoveAt(index);
            scripts.Insert(index - 1, item);

            dataGridViewScripts.ClearSelection();
            dataGridViewScripts.Rows[index - 1].Selected = true;
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            if (_currentProject == null || dataGridViewScripts.SelectedRows.Count != 1) return;

            int index = dataGridViewScripts.SelectedRows[0].Index;
            if (index >= _currentProject.Scripts.Count - 1) return;

            var scripts = _currentProject.Scripts;
            var item = scripts[index];
            scripts.RemoveAt(index);
            scripts.Insert(index + 1, item);

            dataGridViewScripts.ClearSelection();
            dataGridViewScripts.Rows[index + 1].Selected = true;
        }
        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;

            _currentProject.Scripts.Add(new Script
            {
                Name = $"Script{_currentProject.Scripts.Count + 1}.sql",
                Selected = true
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_activeScript == null) return;

            // Atualiza conteúdo em memória
            _activeScript.Content = sqlEditor.Text;

            if (SaveScript(_activeScript))
            {
                // Atualiza grid
                dataGridViewScripts.Refresh();
            }
        }

        private void DataGridViewScripts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;

            var clickedScript = (Script)dataGridViewScripts.Rows[e.RowIndex].DataBoundItem;

            if (clickedScript == null) return;

            ShowScriptOnEditor(clickedScript);
        }

        



        

        private async void NewProjectItem_Click(object sender, EventArgs e)
        {
            Log("[INFO] New Project clicado");
            await NewProjectAsync();
        }

        private async void OpenProjectItem_Click(object sender, EventArgs e)
        {
            if (!CheckUnsavedChanges()) return;

            using var ofd = new OpenFileDialog
            {
                Filter = "SQL Multi Script Project Files (*.smsjsonproj)|*.smsjsonproj"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                _activeScript = null;
                sqlEditor.Text = string.Empty;

                _currentProject = await _projectService.LoadAsync(ofd.FileName);

                dataGridViewScripts.DataSource = _currentProject.Scripts;

                var firstScript = _currentProject.Scripts.FirstOrDefault();
                if (firstScript != null)
                {
                    ShowScriptOnEditor(firstScript);
                }

                if (!string.IsNullOrEmpty(_currentProject.SelectedDistributionList))
                {
                    SelectedDistributionList = DatabaseDistributionLists?
                        .FirstOrDefault(d => d.Name == _currentProject.SelectedDistributionList);
                }

                Log($"Projeto carregado: {_currentProject.FilePath}");
            }
            catch (Exception ex)
            {
                Log($"Erro ao carregar projeto: {ex.Message}", true);
                MessageBox.Show($"Erro ao carregar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveProjectItem_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;


            // Lógica para salvar projeto atual
            if (_activeScript != null)
                // Atualiza conteúdo em memória
                _activeScript.Content = sqlEditor.Text;



            var savedProject = await SaveProjectAsync(_currentProject);

            if (savedProject)
            {
                // Atualiza tela
            }



        }



        private async void CloseProjectItem_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;

            var savedProject = await SaveProjectAsync(_currentProject);

            if (savedProject)
            {
                _currentProject = null;
                _activeScript = null;
                sqlEditor.Text = string.Empty;
                sqlEditor.Enabled = false;
                dataGridViewScripts.DataSource = null;
                dataGridViewScripts.Refresh();
                Log("[INFO] Projeto fechado");
            }


        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            Close(); // fecha o form
        }

        private void AboutMenu_Click(object sender, EventArgs e)
        {
            // Exibe um formulário About simples
            using var aboutForm = new Form
            {
                Text = "About",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                ShowInTaskbar = false
            };

            var lbl = new Label
            {
                Text = "SQL MultiScript\nby Penihel Roosewelt",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            aboutForm.Controls.Add(lbl);
            aboutForm.ShowDialog(this);
        }

        private void ToolStripMenuItemRemove_Click(object sender, EventArgs e)
        {
            RemoveScripts();

        }

        private async void btnDatabaseDistributionList_Click(object sender, EventArgs e)
        {
            var databaseDistributionListForm = _serviceProvider.GetRequiredService<DatabaseDistributionListForm>();

            var result = databaseDistributionListForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                var previousSelection = SelectedDistributionList?.Name;

                await LoadDistribuitionListsAsync();

                SelectedDistributionList = DatabaseDistributionLists?
                    .FirstOrDefault(d => d.Name == previousSelection)
                    ?? DatabaseDistributionLists?.FirstOrDefault();
            }
        }

        private void RemoveScripts()
        {
            if (dataGridViewScripts.SelectedRows.Count == 0)
                return;

            var confirm = MessageBox.Show(
                $"Deseja realmente remover {dataGridViewScripts.SelectedRows.Count} linha(s)?",
                "Confirmação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dataGridViewScripts.SelectedRows)
                {
                    if (!row.IsNewRow) // previne tentar remover linha vazia
                    {
                        dataGridViewScripts.Rows.Remove(row);
                    }
                }
            }


        }


        private async void BtnRun_Click(object sender, EventArgs e)
        {
            btnRun.Visible = false;
            btnStop.Visible = true;
            Cursor = Cursors.WaitCursor;

            _refreshTimer ??= new System.Windows.Forms.Timer { Interval = 500 };
            _refreshTimer.Tick -= RefreshTimer_Tick;
            _refreshTimer.Tick += RefreshTimer_Tick;
            _resultsDirty = false;
            _refreshTimer.Start();

            _executionCts?.Dispose();
            _executionCts = new CancellationTokenSource();
            var cancellationToken = _executionCts.Token;

            try
            {
                if (_currentProject == null)
                {
                    Log("Nenhum projeto carregado.", true);
                    return;
                }

                if (SelectedDistributionList == null)
                {
                    Log("Nenhuma lista de distribuição selecionada.", true);
                    return;
                }

                var selectedDatabases = SelectedDistributionList.Databases
                    .Where(d => d.Selected)
                    .ToList();

                if (!selectedDatabases.Any())
                {
                    Log("Nenhum banco de dados selecionado para execução.", true);
                    return;
                }

                var selectedScripts = _currentProject.Scripts
                    .Where(s => s.Selected)
                    .ToList();

                if (!selectedScripts.Any())
                {
                    Log("Nenhum script selecionado para execução.", true);
                    return;
                }

                var uniqueServers = selectedDatabases.Select(d => d.ConnectionName).Distinct().Count();
                Log($"Starting execution: {selectedScripts.Count} script(s), {selectedDatabases.Count} database(s), {uniqueServers} server(s)");

                // Phase 1: Authentication (one per server, for MFA token caching)
                await _executionService.OpenConnectionsAsync(selectedDatabases, cancellationToken);

                // Phase 2: Execution
                var execution = CreateExecution(selectedScripts, selectedDatabases);

                UpdateTreeView();

                execution.Status = ExecutionStatus.Executing;

                var totalScripts = execution.ScriptsInfo.Count;
                int scriptIndex = 0;

                foreach (var scriptInfo in execution.ScriptsInfo)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    scriptIndex++;

                    Log($"[Script {scriptIndex}/{totalScripts}] {scriptInfo.Script.Name}");

                    SelectedExecutionScriptInfo = scriptInfo;

                    await _executionService.ExecuteAsync(scriptInfo, new Progress<ExecutionProgress>(p => UpdateExecutionStatus(p)), cancellationToken);
                }

                // Phase 3: Summary
                var hasError = execution.ScriptsInfo.Any(si => si.Status == ExecutionStatus.Error);
                var hasCancelled = execution.ScriptsInfo.Any(si => si.Status == ExecutionStatus.Cancelled);

                if (hasError)
                    execution.Status = ExecutionStatus.Error;
                else if (hasCancelled)
                    execution.Status = ExecutionStatus.Cancelled;
                else
                    execution.Status = ExecutionStatus.Success;

                Log($"{execution.Name}: {execution.Status}");



            }
            catch (OperationCanceledException)
            {
                Log("Execução cancelada pelo usuário.", true);
            }
            catch (Exception ex)
            {
                Log($"Erro geral: {ex.Message}", true);
                MessageBox.Show($"Erro geral: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _refreshTimer?.Stop();
                RefreshTimer_Tick(null, EventArgs.Empty);

                btnRun.Visible = true;
                btnStop.Visible = false;
                btnStop.Enabled = true;
                Cursor = Cursors.Default;
                treeViewExecutions.Enabled = true;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (_executionCts != null && !_executionCts.IsCancellationRequested)
            {
                _executionCts.Cancel();
                btnStop.Enabled = false;
                btnRun.Visible = true;
                Log("Cancelamento solicitado...");
            }
        }


        private void UpdateTreeView()
        {
            treeViewExecutions.BeginUpdate();
            treeViewExecutions.Nodes.Clear();

            foreach (var execution in _executions)
            {
                var node = new TreeNode($"{execution.Name} - {execution.Status}")
                {
                    Tag = execution,
                    ImageKey = execution.Status.ToString(),
                    SelectedImageKey = execution.Status.ToString()
                };
                treeViewExecutions.Nodes.Add(node);


                foreach (var s in execution.ScriptsInfo)
                {
                    var nodeScript = new TreeNode($"{s.Script.Name} - {s.Status}")
                    {
                        Tag = s,
                        ImageKey = s.Status.ToString(),
                        SelectedImageKey = s.Status.ToString()
                    };


                    node.Nodes.Add(nodeScript);


                }
            }

            treeViewExecutions.EndUpdate();




            treeViewExecutions.Nodes[0].Expand();
            treeViewExecutions.SelectedNode = treeViewExecutions.Nodes[0].Nodes[0];
            treeViewExecutions.Focus();

        }

        private Execution CreateExecution(List<Script> selectedScripts, List<Database> selectedDatabases)
        {
            var execution = new Execution()
            {
                Name = $"Execution #{(_executions.Count + 1)}",
                Status = ExecutionStatus.Queued,

            };

            execution.ScriptsInfo = new BindingList<ExecutionScriptInfo>(selectedScripts.Select(s => new ExecutionScriptInfo
            {
                Script = s,
                Execution = execution,
                DataSet = new DataSet(),
                Status = ExecutionStatus.Queued,
                DatabasesInfo = new BindingList<ExecutionDatabaseInfo>(selectedDatabases.Select(d => new ExecutionDatabaseInfo
                {
                    Database = d,
                    Status = ExecutionStatus.Queued,
                }).ToList())
            }).ToList());

            _executions.Insert(0, execution);

            return execution;
        }

        private void Log(string message)
        {
            Log(message, false);
        }
        private void Log(string message, bool isError)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message, isError)));
                return;
            }

            string prefix = isError ? "[ERRO]" : "[INFO]";
            var mesasgeFull = $"[{DateTime.Now:G}] {prefix} {message}";
            if (isError)
            {
                outputMessagesLog.AppendError(mesasgeFull);
            }
            else
            {
                outputMessagesLog.AppendInfo(mesasgeFull);
            }


            if (isError)
                _logger.LogError(message);
            else
                _logger.LogInformation(message);
        }


        void UpdateExecutionStatus(ExecutionProgress executionProgress)
        {

            if (executionProgress.ScriptInfo != null)
            {
                var execution = executionProgress.ScriptInfo.Execution;
                var scriptInfo = executionProgress.ScriptInfo;
                var databaseInfo = executionProgress.DatabaseInfo;

                // Atualiza a TreeView
                var executionNode = treeViewExecutions.Nodes
                    .Cast<TreeNode>()
                    .FirstOrDefault(n => n.Tag == execution);


                if (executionNode != null)
                {

                    // Atualiza o nó do script
                    if (scriptInfo != null)
                    {
                        var scriptNode = executionNode.Nodes
                            .Cast<TreeNode>()
                            .FirstOrDefault(n => n.Tag == scriptInfo);
                        if (scriptNode != null)
                        {
                            if (databaseInfo == null)
                            {
                                //SelectedExecutionScriptInfo = null;

                                Log($"{execution.Name} - {scriptInfo.Script.Name} => {scriptInfo.Status}");

                                //SelectedExecutionScriptInfo = scriptInfo;
                            }


                            // Atualiza o status do script (pode ser feito com imagens ou texto)
                            scriptNode.Text = $"{scriptInfo.Script.Name} - {scriptInfo.Status}";
                            scriptNode.ImageKey = scriptInfo.Status.ToString();
                            scriptNode.SelectedImageKey = scriptInfo.Status.ToString();


                        }
                    }


                    // Atualiza o status geral da execução
                    executionNode.Text = $"{execution.Name} - {execution.Status}";
                    executionNode.ImageKey = execution.Status.ToString();
                    executionNode.SelectedImageKey = execution.Status.ToString();


                    dataGridViewDatabasesResults.Refresh();


                }
            }



        }



    }
}