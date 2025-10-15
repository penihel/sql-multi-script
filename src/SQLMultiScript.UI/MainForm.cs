using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.UI.Forms;
using System.ComponentModel;

namespace SQLMultiScript.UI
{
    public class MainForm : Form
    {
        private readonly IProjectService _projectService;
        private readonly IDatabaseDistributionListService _databaseDistributionListService;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        private Project _currentProject = null;
        private Script _activeScript = null;
        private BindingList<DatabaseDistributionList> _databaseDistributionLists;

        // Componentes
        private SplitContainer 
            splitMain, 
            splitLeft, 
            splitCenterRight, 
            splitResultFooter;
        
        private DataGridView 
            dataGridViewScripts, 
            dataGridViewDatabases;
        
        private Scintilla sqlEditor;
        private TextBox logBox;
        private MenuStrip menuStrip;
        
        private Button 
            btnUp, 
            btnDown, 
            btnAdd, 
            btnNew, 
            btnSave, 
            btnRemove, 
            btnDatabaseDistributionList;

        private ComboBox comboBoxDatabaseDistributionList;

        

        public MainForm(
            ILogger logger,
            IProjectService projectService,
            IDatabaseDistributionListService databaseDistributionListService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _projectService = projectService;
            _databaseDistributionListService = databaseDistributionListService;
            _serviceProvider = serviceProvider;
            InitializeLayout();


        }

        private void InitializeLayout()
        {
            
            Text = $"{Constants.ApplicationName} - {Constants.ApplicationVersion}";
            Icon = new Icon("sql-multi-script.ico");
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;

            // Split principal (top/bottom)
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 800
            };
            Controls.Add(splitMain);

            // Split esquerda
            splitLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 10
            };
            splitMain.Panel1.Controls.Add(splitLeft);

            // Setup Scripts Panel
            SetupScriptsPanel(splitLeft.Panel1);

            // Split centro/direita
            splitCenterRight = new SplitContainer
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
            splitResultFooter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };
            splitMain.Panel2.Controls.Add(splitResultFooter);


            var resultPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Beige
            };

            resultPanel.Controls.Add(new Label { Text = "Painel de Resultados", Dock = DockStyle.Top });
            splitResultFooter.Panel1.Controls.Add(resultPanel);

            logBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            splitResultFooter.Panel2.Controls.Add(logBox);

            InitializeMenu();
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

            };

            // Checkbox
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            };
            dataGridViewDatabases.Columns.Add(colSelected);

            // Nome do Script
            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DisplayName",
                HeaderText = Resources.Strings.Database,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewDatabases.Columns.Add(colName);

            dataGridViewDatabases.CellClick += DataGridViewDatabases_CellClick;



            // -----------------------
            // Painel do grid
            // -----------------------
            var listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            listPanel.Controls.Add(dataGridViewDatabases);

            parentPanel.Controls.Add(listPanel);    // grid primeiro

            // -----------------------
            // TableLayoutPanel de Botões
            // -----------------------
            var topTableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
            };
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Combo ocupa o espaço todo
            topTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F)); // Botão fixo em 50px

            // Cria o ComboBox
            comboBoxDatabaseDistributionList = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right, // só alinha horizontal
                
                DropDownStyle = ComboBoxStyle.DropDownList,
                

            };

            

            topTableLayoutPanel.Controls.Add(comboBoxDatabaseDistributionList,0,0);

            btnDatabaseDistributionList = new Button
            {
                Dock = DockStyle.Right,
                Image = Images.ic_fluent_database_stack_16_regular,
                Size = UIConstants.ButtonSize,
            };
            var toolTipBtn = new ToolTip();
            toolTipBtn.SetToolTip(btnDatabaseDistributionList, Resources.Strings.DatabaseDistributionLists);

            btnDatabaseDistributionList.Click += btnDatabaseDistributionList_Click;


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
                DataPropertyName = "DisplayName",
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
            var listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            listPanel.Controls.Add(dataGridViewScripts);

            parentPanel.Controls.Add(listPanel);    // grid primeiro


            // -----------------------
            // Painel de Botões
            // -----------------------
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            


            btnDown = new Button
            {

                Dock = DockStyle.Left,
                Image = Images.ic_fluent_arrow_circle_down_24_regular,
                Size = UIConstants.ButtonSize,
            };
            btnDown.Click += BtnDown_Click;
            var toolTipBtnDown = new ToolTip();
            toolTipBtnDown.SetToolTip(btnDown, Resources.Strings.Down);


            btnUp = new Button
            {

                Dock = DockStyle.Left,
                Image = Images.ic_fluent_arrow_circle_up_24_regular,
                Size = UIConstants.ButtonSize
            };
            btnUp.Click += BtnUp_Click;
            var toolTipBtnUp = new ToolTip();
            toolTipBtnUp.SetToolTip(btnUp, Resources.Strings.Up);


            btnRemove = new Button
            {

                Dock = DockStyle.Left,
                Image = Images.ic_fluent_delete_24_regular,               
                Size = UIConstants.ButtonSize

            };
            btnRemove.Click += BtnRemove_Click;
            var toolTipBtnRemove = new ToolTip();
            toolTipBtnRemove.SetToolTip(btnRemove, Resources.Strings.Remove);

            
            
            buttonPanel.Controls.Add(btnRemove);
            buttonPanel.Controls.Add(btnDown);
            buttonPanel.Controls.Add(btnUp);


            //

            btnNew = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_new_24_regular,
                Size = UIConstants.ButtonSize
            };
            btnNew.Click += BtnNew_Click;
            var toolTipBtnNew = new ToolTip();
            toolTipBtnNew.SetToolTip(btnNew, Resources.Strings.New);
            buttonPanel.Controls.Add(btnNew);



            btnAdd = new Button
            {

                Dock = DockStyle.Right,
                Image = Images.ic_fluent_add_24_regular,                
                Size = UIConstants.ButtonSize

            };
            btnAdd.Click += BtnAdd_Click;
            var toolTipBtnAdd = new ToolTip();
            toolTipBtnAdd.SetToolTip(btnAdd, Resources.Strings.AddExisting);

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

            var editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UIConstants.PanelPadding),
                BackColor = Color.Transparent // opcional
            };




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
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(UIConstants.PanelPadding),
                Height = 60,
            };

            // Botão Salvar no canto direito
            btnSave = new Button
            {
                Image = Images.ic_fluent_save_24_regular,
                Dock = DockStyle.Right,
                Size = UIConstants.ButtonSize
            };
            btnSave.Click += BtnSave_Click;
            var toolTipBtnSave = new ToolTip();
            toolTipBtnSave.SetToolTip(btnSave, Resources.Strings.Save);
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



            }


        }



        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;
            comboBoxDatabaseDistributionList.DataSource = _databaseDistributionLists;
            comboBoxDatabaseDistributionList.DisplayMember = "DisplayName";
            comboBoxDatabaseDistributionList.ValueMember = "Id";
            comboBoxDatabaseDistributionList.DataBindings.Add("SelectedValue", _currentProject, "SelectedDistributionListId", true, DataSourceUpdateMode.OnPropertyChanged);

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

            Log($"Script ativo: {_activeScript.DisplayName}");
        }
        private bool SaveScript(Script script)
        {




            // Se não tem caminho, pede ao usuário
            if (string.IsNullOrEmpty(script.FilePath))
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                    FileName = script.DisplayName
                };

                if (sfd.ShowDialog() != DialogResult.OK) return false;

                script.FilePath = sfd.FileName;
                script.DisplayName = Path.GetFileName(sfd.FileName);


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

        private bool SaveProject(Project project)
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
                    FileName = project.DisplayName ?? "NewProject.smsjsonproj"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return false;

                project.FilePath = sfd.FileName;
                project.DisplayName = Path.GetFileNameWithoutExtension(sfd.FileName);
            }

            try
            {
                var projectJson = System.Text.Json.JsonSerializer.Serialize(project, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(project.FilePath, projectJson);


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
                    DisplayName = Path.GetFileName(file),
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
                DisplayName = $"Script{_currentProject.Scripts.Count + 1}.sql",
                Order = _currentProject.Scripts.Count + 1,
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
            Log($"Executando script: {_activeScript.DisplayName}");
            Log(script);
            Log("Execução concluída com sucesso");
        }

        private async void NewProjectItem_Click(object sender, EventArgs e)
        {
            // Lógica para criar um novo projeto
            Log("[INFO] New Project clicado");
            await NewProjectAsync();
        }

        private void SaveProjectItem_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;


            // Lógica para salvar projeto atual
            if (_activeScript != null)
                // Atualiza conteúdo em memória
                _activeScript.Content = sqlEditor.Text;



            var savedProject = SaveProject(_currentProject);

            if (savedProject)
            {
                // Atualiza tela
            }



        }



        private void CloseProjectItem_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;

            var savedProject = SaveProject(_currentProject);

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

        
        private void Log(string message, bool isError = false)
        {
            string prefix = isError ? "[ERRO]" : "[INFO]";
            logBox.AppendText($"[{DateTime.Now:G}] {prefix} {message}" + Environment.NewLine);

            if (isError) _logger.LogError(message);
            else _logger.LogInformation(message);
        }
    }
}
