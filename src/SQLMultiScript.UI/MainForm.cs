using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.UI
{
    public class MainForm : Form
    {
        private readonly IProjectService _projectService;
        private readonly ILogger _logger;

        private Project _currentProject = null;
        private Script _activeScript = null;

        // Componentes
        private SplitContainer splitMain, splitLeft, splitCenterRight, splitResultFooter;
        private DataGridView dataGridViewScripts;
        private Scintilla sqlEditor;
        private TextBox logBox;
        private MenuStrip menuStrip;
        private ToolStripMenuItem executarMenu;
        private Button btnAdd, btnNew, btnSave;


        public MainForm(ILogger logger, IProjectService projectService)
        {
            _logger = logger;
            _projectService = projectService;

            InitializeLayout();


        }

        private void InitializeLayout()
        {
            Text = $"{Constants.ApplicationName} - {Constants.ApplicationVersion}";
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

            //SetupMenu();

            InitializeMenu();
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
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Checkbox
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected",
                Width = 60
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



            // -----------------------
            // Painel do grid
            // -----------------------
            var listPanel = new Panel { Dock = DockStyle.Fill };

            listPanel.Controls.Add(dataGridViewScripts);

            parentPanel.Controls.Add(listPanel);    // grid primeiro


            // -----------------------
            // Painel de Botões
            // -----------------------
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(UIConstants.PanelPadding)
            };

            btnAdd = new Button
            {
                Text = "Add Existing",
                Dock = DockStyle.Right,
                Image = Resources.Images.ic_fluent_add_24_regular,
                ImageAlign = ContentAlignment.MiddleLeft,
                Width = 150

            };
            btnAdd.Click += BtnAdd_Click;
            buttonPanel.Controls.Add(btnAdd);

            btnNew = new Button
            {
                Text = "New",
                Dock = DockStyle.Left
            };
            btnNew.Click += BtnNew_Click;
            buttonPanel.Controls.Add(btnNew);



            parentPanel.Controls.Add(buttonPanel);  // botões por cima

        }

        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();

            // -------------------------------
            // Menu File
            // -------------------------------
            var fileMenu = new ToolStripMenuItem("File");

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

        //private void SetupMenu()
        //{
        //    menuStrip = new MenuStrip();
        //    executarMenu = new ToolStripMenuItem("Executar Script");
        //    executarMenu.Click += ExecutarMenu_Click;
        //    menuStrip.Items.Add(executarMenu);
        //    MainMenuStrip = menuStrip;
        //    Controls.Add(menuStrip);
        //}

        private void SetupEditorPanel(Panel parentPanel)
        {

            var editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30,
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
                Height = 30
            };

            // Botão Salvar no canto direito
            btnSave = new Button
            {
                Text = "Save",
                Dock = DockStyle.Right,
                Width = 80
            };
            btnSave.Click += BtnSave_Click;

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
        private async Task NewProject()
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

            BindData();
        }



        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;

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
            await NewProject();
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

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (_currentProject == null) return;

            _currentProject.Scripts.Add(new Script
            {
                DisplayName = $"Script{_currentProject.Scripts.Count + 1}.sql"
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
            await NewProject();
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
                StartPosition = FormStartPosition.CenterParent
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


        private void Log(string message, bool isError = false)
        {
            string prefix = isError ? "[ERRO]" : "[INFO]";
            logBox.AppendText($"[{DateTime.Now:G}] {prefix} {message}" + Environment.NewLine);

            if (isError) _logger.LogError(message);
            else _logger.LogInformation(message);
        }
    }
}
