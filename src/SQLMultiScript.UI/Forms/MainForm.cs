using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Resources;
using SQLMultiScript.UI.ControlFactories;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SQLMultiScript.UI.Forms
{
    public class MainForm : BaseForm
    {
        private const int TopHeight = 60;

        private readonly IProjectService _projectService;
        private readonly IDatabaseDistributionListService _databaseDistributionListService;
        private readonly IScriptExecutorService _scriptExecutorService;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        private Project _currentProject = null;
        private Script _activeScript = null;


        private BindingList<DatabaseDistributionList> _databaseDistributionLists = new BindingList<DatabaseDistributionList>();
        private BindingList<Execution> _executions = new BindingList<Execution>();



        private DataGridView
            dataGridViewScripts,
            dataGridViewDatabases,
            dataGridViewDatabasesResults;

        private TreeView treeViewExecutions;

        private Scintilla sqlEditor;
        private TextBox logBox;
        private MenuStrip menuStrip;

        private Panel panelResults;

        private ComboBox comboBoxDatabaseDistributionList;

        //Properties
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
        }



        public MainForm(
            ILogger logger,
            IProjectService projectService,
            IDatabaseDistributionListService databaseDistributionListService,
            IServiceProvider serviceProvider,
            IScriptExecutorService scriptExecutorService)
        {
            _logger = logger;
            _projectService = projectService;
            _databaseDistributionListService = databaseDistributionListService;
            _serviceProvider = serviceProvider;
            _scriptExecutorService = scriptExecutorService;

            InitializeLayout();
        }

        private void InitializeLayout()
        {

            Text = $"{Constants.ApplicationName} - {Constants.ApplicationVersion}";
            Icon = new Icon("sql-multi-script.ico");
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;

            var mainTableLayoutPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2
            };
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, TopHeight));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));



            // Split principal (top/bottom)
            var splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                //SplitterDistance = 200
            };


            var mainTopButtonsPanel = PanelFactory.Create();

            SetupTopButtonsPanel(mainTopButtonsPanel);

            mainTableLayoutPanel.Controls.Add(mainTopButtonsPanel, 0, 0);
            mainTableLayoutPanel.Controls.Add(splitMain, 0, 1);
            Controls.Add(mainTableLayoutPanel);

            // Split esquerda
            var splitLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 10
            };
            splitMain.Panel1.Controls.Add(splitLeft);

            // Setup Scripts Panel
            SetupScriptsPanel(splitLeft.Panel1);

            // Split centro/direita
            var splitCenterRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600
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
                SplitterDistance = 300
            };
            splitMain.Panel2.Controls.Add(splitResultFooter);



            SetupResultPanel(splitResultFooter.Panel1);


            var logContainer = PanelFactory.Create();
            logBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            logContainer.Controls.Add(logBox);
            splitResultFooter.Panel2.Controls.Add(logContainer);

            InitializeMenu();
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
                Dock = DockStyle.Fill,
                //BorderStyle = BorderStyle.FixedSingle,
                // Add space at the top to avoid touching the label
                //Margin = new Padding(0, 4, 0, 0),
                //CheckBoxes = true
            };

            treeViewExecutions.AfterSelect += treeViewExecutions_AfterSelect;

            var treeContainer = PanelFactory.Create();
            panelResults = PanelFactory.Create();

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

            // Checkbox
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            };
            dataGridViewDatabasesResults.Columns.Add(colSelected);

            // Database name column
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DatabaseName",
                HeaderText = Strings.DatabasesToExecute,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabasesResults.Columns.Add(colName);


            // Database name column
            var colServer = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ConnectionName",
                HeaderText = Strings.Connection,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabasesResults.Columns.Add(colServer);


            var gridContainer = PanelFactory.Create();

            gridContainer.Controls.Add(dataGridViewDatabasesResults);

            splitDatabasesAndResults.Panel1.Controls.Add(gridContainer);
            splitDatabasesAndResults.Panel2.Controls.Add(panelResults);
        }

        private void treeViewExecutions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level != 1) return; // script level
            string scriptName = e.Node.Text;

            panelResults.Controls.Clear();

            var tabControl = new TabControl { Dock = DockStyle.Fill };
            panelResults.Controls.Add(tabControl);

            // Aba Resultados
            var tabResults = new TabPage("Resultados");
            tabControl.TabPages.Add(tabResults);

            var flowResults = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            tabResults.Controls.Add(flowResults);

            if (_scriptResults.TryGetValue(scriptName, out var consolidatedResults) && consolidatedResults.Count > 0)
            {
                int idx = 1;
                foreach (var kvp in consolidatedResults.OrderBy(k => k.Key))
                {
                    var dt = kvp.Value;

                    var label = new Label
                    {
                        Text = $"Resultado #{idx} — {dt.Rows.Count} linhas",
                        AutoSize = true,
                        Font = new Font(Font, FontStyle.Bold),
                        Padding = new Padding(0, 10, 0, 2)
                    };

                    var grid = new DataGridView
                    {
                        DataSource = dt,
                        ReadOnly = true,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        Width = flowResults.ClientSize.Width - 40,
                        Height = Math.Min(400, 40 + dt.Rows.Count * 22),
                        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                    };

                    flowResults.Controls.Add(label);
                    flowResults.Controls.Add(grid);
                    idx++;
                }
            }
            else
            {
                flowResults.Controls.Add(new Label
                {
                    Text = "Nenhum resultado retornado.",
                    AutoSize = true,
                    Padding = new Padding(10),
                    Font = new Font(Font, FontStyle.Italic)
                });
            }

            // Aba Mensagens
            var tabMessages = new TabPage("Mensagens");
            tabControl.TabPages.Add(tabMessages);

            var flowMessages = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            tabMessages.Controls.Add(flowMessages);

            if (_scriptMessages.TryGetValue(scriptName, out var msgs) && msgs.Count > 0)
            {
                var textBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Width = flowMessages.ClientSize.Width - 40,
                    Height = 200,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                    Font = new Font("Consolas", 9),
                    Text = string.Join(Environment.NewLine + Environment.NewLine, msgs)
                };
                flowMessages.Controls.Add(textBox);
            }
            else
            {
                flowMessages.Controls.Add(new Label
                {
                    Text = "Nenhuma mensagem retornada.",
                    AutoSize = true,
                    Padding = new Padding(10),
                    Font = new Font(Font, FontStyle.Italic)
                });
            }
        }

        private void SetupTopButtonsPanel(Panel parentPanel)
        {



            var btnRun = ButtonFactory.Create(ToolTip,
                Strings.Execute,
                Images.ic_fluent_play_multiple_16_regular,
                BtnRun_Click)
                .Customize(b => b.AutoSize = true)
                .Customize(b => b.Padding = new Padding(20, 5, 20, 5))
                .Customize(b => b.TextImageRelation = TextImageRelation.ImageBeforeText)
                .Customize(b => b.Text = Strings.Execute)
                .Customize(b => b.Anchor = AnchorStyles.Top);



            parentPanel.Controls.Add(btnRun);

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

            // Checkbox
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            };
            dataGridViewDatabases.Columns.Add(colSelected);

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

            dataGridViewDatabases.CellClick += DataGridViewDatabases_CellClick;



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



            topTableLayoutPanel.Controls.Add(comboBoxDatabaseDistributionList, 0, 0);

            var btnDatabaseDistributionList = ButtonFactory.Create(ToolTip,
                Strings.DatabaseDistributionLists,
                Images.ic_fluent_database_stack_16_regular,
                btnDatabaseDistributionList_Click);







            topTableLayoutPanel.Controls.Add(btnDatabaseDistributionList, 1, 0);

            parentPanel.Controls.Add(topTableLayoutPanel);  // botões por cima





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

            // Checkbox
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            };
            dataGridViewScripts.Columns.Add(colSelected);

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
                    SelectedDistributionList = _databaseDistributionLists?.FirstOrDefault(d => d.Name == _currentProject.SelectedDistributionList);
                }
                else
                {
                    SelectedDistributionList = _databaseDistributionLists?.FirstOrDefault();

                }

            }


        }



        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;
            comboBoxDatabaseDistributionList.DataSource = _databaseDistributionLists;
            comboBoxDatabaseDistributionList.DisplayMember = "Name";
            comboBoxDatabaseDistributionList.ValueMember = "Name";
            comboBoxDatabaseDistributionList.DataBindings.Add("SelectedItem", this, nameof(SelectedDistributionList), true, DataSourceUpdateMode.OnPropertyChanged);

            // Atualiza grid
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

            _databaseDistributionLists =
                new BindingList<DatabaseDistributionList>(await _databaseDistributionListService.ListAsync());



        }

        private void SqlEditor_TextChanged(object sender, EventArgs e)
        {
            if (_activeScript != null)
            {
                _activeScript.IsDirty = true;
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

        }

        private void BtnUp_Click(object sender, EventArgs e)
        {

        }

        private void BtnDown_Click(object sender, EventArgs e)
        {

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

        private void DataGridViewDatabases_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }



        private void ExecutarMenu_Click(object sender, EventArgs e)
        {
            if (_activeScript == null)
            {
                Log("[WARN] Nenhum script ativo para executar", true);
                return;
            }

            string script = _activeScript.Content;
            if (string.IsNullOrWhiteSpace(script))
            {
                Log("[WARN] Script vazio, nada a executar", true);
                return;
            }

            // Aqui executa o script (simulado)
            Log($"Executando script: {_activeScript.Name}");
            Log(script);
            Log("Execução concluída com sucesso");
        }

        private async void NewProjectItem_Click(object sender, EventArgs e)
        {
            // Lógica para criar um novo projeto
            Log("[INFO] New Project clicado");
            await NewProjectAsync();
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

        private void btnDatabaseDistributionList_Click(object sender, EventArgs e)
        {
            var databaseDistributionListForm = _serviceProvider.GetRequiredService<DatabaseDistributionListForm>();


            var result = databaseDistributionListForm.ShowDialog(this);

            databaseDistributionListForm.SelectedDistributionList = SelectedDistributionList;

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

        // Armazena resultados por script
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, DataTable>> _scriptResults
            = new ConcurrentDictionary<string, ConcurrentDictionary<int, DataTable>>();

        // Armazena mensagens por script
        private readonly ConcurrentDictionary<string, List<string>> _scriptMessages
            = new ConcurrentDictionary<string, List<string>>();

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            btn.Enabled = false;
            Cursor = Cursors.WaitCursor;

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

                Log($"Iniciando execução em {selectedDatabases.Count} banco(s) com {selectedScripts.Count} script(s)...");

                await _scriptExecutorService.LoadConnectionsAsync();

                // Limpa dados antigos
                _scriptResults.Clear();
                _scriptMessages.Clear();

                // Semaphore para limitar o número de execuções simultâneas
                using var semaphore = new SemaphoreSlim(1); // 5 threads paralelas

                CreateExecution(selectedScripts, selectedDatabases);
                UpdateTreeView();

                var tasks = selectedDatabases.Select(async db =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        foreach (var script in selectedScripts)
                        {
                            try
                            {
                                Log($"Executando script '{script.Name}' em '{db.DatabaseName}'...");

                                var scriptExecutorResponse = await _scriptExecutorService.ExecuteAsync(db, script, Log);

                                var dataSet = scriptExecutorResponse.DataSet;

                                // ✅ Armazena resultados
                                if (dataSet != null && dataSet.Tables.Count > 0)
                                {
                                    for (int resultIndex = 0; resultIndex < dataSet.Tables.Count; resultIndex++)
                                    {
                                        var dt = dataSet.Tables[resultIndex];

                                        var consolidated = _scriptResults
                                            .GetOrAdd(script.Name, _ => new ConcurrentDictionary<int, DataTable>())
                                            .GetOrAdd(resultIndex, _ =>
                                            {
                                                var newDt = dt.Clone();
                                                newDt.TableName = $"Result_{resultIndex + 1}";
                                                newDt.Columns.Add("ConnectionName", typeof(string));
                                                newDt.Columns.Add("DatabaseName", typeof(string));
                                                newDt.Columns["ConnectionName"].SetOrdinal(0);
                                                newDt.Columns["DatabaseName"].SetOrdinal(1);
                                                return newDt;
                                            });

                                        lock (consolidated)
                                        {
                                            foreach (DataRow row in dt.Rows)
                                            {
                                                var newRow = consolidated.NewRow();
                                                foreach (DataColumn col in dt.Columns)
                                                    newRow[col.ColumnName] = row[col.ColumnName];

                                                newRow["ConnectionName"] = db.ConnectionName;
                                                newRow["DatabaseName"] = db.DatabaseName;
                                                consolidated.Rows.Add(newRow);
                                            }
                                        }
                                    }
                                }

                                // ✅ Armazena mensagens sempre, mesmo que não haja DataSet
                                if (scriptExecutorResponse.Messages.Count > 0)
                                {
                                    var formatted = new StringBuilder();
                                    formatted.AppendLine($"🔹 Banco: {db.DatabaseName}");
                                    foreach (var msg in scriptExecutorResponse.Messages)
                                        formatted.AppendLine(msg);

                                    _scriptMessages.AddOrUpdate(
                                        script.Name,
                                        _ => new List<string> { formatted.ToString() },
                                        (_, list) =>
                                        {
                                            lock (list) list.Add(formatted.ToString());
                                            return list;
                                        });
                                }

                                Log($"Concluído '{script.Name}' em '{db.DatabaseName}'.");
                            }
                            catch (Exception exScript)
                            {
                                Log($"Erro ao executar script '{script.Name}' no banco '{db.DatabaseName}': {exScript.Message}", true);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks);

                Log("Execução concluída com sucesso.");
                //MessageBox.Show("Execução concluída com sucesso.", "Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"Erro geral: {ex.Message}", true);
                MessageBox.Show($"Erro geral: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn.Enabled = true;
                Cursor = Cursors.Default;
            }
        }





        private void UpdateTreeView()
        {
            treeViewExecutions.BeginUpdate();
            treeViewExecutions.Nodes.Clear();

            foreach (var e in _executions)
            {
                var node = new TreeNode(e.Name)
                {
                    Tag = e, // Store the full object
                    // Do not check the root node
                    //ImageKey = "disconnected",
                    //SelectedImageKey = "disconnected"
                };
                treeViewExecutions.Nodes.Add(node);


                foreach (var s in e.ScriptsInfo)
                {
                    var nodeScript = new TreeNode(s.Script.Name)
                    {
                        Tag = s, // Store the full object
                                 // Do not check the root node
                                 //ImageKey = "disconnected",
                                 //SelectedImageKey = "disconnected"
                    };

                    
                    node.Nodes.Add(nodeScript);

                    foreach (var d in s.DatabasesInfo)
                    {
                        var nodeDatabase = new TreeNode(d.Database.DatabaseName)
                        {
                            Tag = d, // Store the full object
                                     // Do not check the root node
                                     //ImageKey = "disconnected",
                                     //SelectedImageKey = "disconnected"
                        };

                        nodeScript.Nodes.Add(nodeDatabase);
                    }
                }
            }

            treeViewExecutions.EndUpdate();
        }

        private void CreateExecution(List<Script> selectedScripts, List<Database> selectedDatabases)
        {
            _executions.Add(new Execution()
            {
                Name = $"Execution in {DateTime.Now}",
                Status = ExecutionStatus.Queued,
                ScriptsInfo = new BindingList<ExecutionScriptInfo>(selectedScripts.Select(s => new ExecutionScriptInfo
                {
                    Script = s,
                    Status = ExecutionStatus.Queued,
                    DatabasesInfo = new BindingList<ExecutionDatabaseInfo>(selectedDatabases.Select(d => new ExecutionDatabaseInfo
                    {
                        Database = d,
                        Status = ExecutionStatus.Queued,
                    }).ToList())
                }).ToList())
            });
        }

        private void Log(string message, bool isError = false)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message, isError)));
                return;
            }

            string prefix = isError ? "[ERRO]" : "[INFO]";
            logBox.AppendText($"[{DateTime.Now:G}] {prefix} {message}{Environment.NewLine}");

            if (isError)
                _logger.LogError(message);
            else
                _logger.LogInformation(message);
        }

    }
}
