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

            SetupMenu();
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

            dataGridViewScripts.CellDoubleClick += DataGridViewScripts_CellDoubleClick;



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
                Height = 30
            };

            btnAdd = new Button
            {
                Text = "Add Existing",
                Dock = DockStyle.Right
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

            btnSave = new Button
            {
                Text = "Save",
                Dock = DockStyle.Left
            };
            btnSave.Click += BtnSave_Click;
            buttonPanel.Controls.Add(btnSave);

            parentPanel.Controls.Add(buttonPanel);  // botões por cima

        }

        private void SetupMenu()
        {
            menuStrip = new MenuStrip();
            executarMenu = new ToolStripMenuItem("Executar Script");
            executarMenu.Click += ExecutarMenu_Click;
            menuStrip.Items.Add(executarMenu);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

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

        private async void MainForm_Load(object sender, EventArgs e)
        {
            _currentProject = await _projectService.CreateNewAsync();

            BindData();
        }

        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
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
            _currentProject.Scripts.Add(new Script
            {
                DisplayName = $"Script{_currentProject.Scripts.Count + 1}.sql"
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_activeScript == null) return;
            try
            {
                File.WriteAllText(_activeScript.FilePath, _activeScript.Content ?? string.Empty);
                _activeScript.IsDirty = false;
                Log($"Script salvo: {_activeScript.FilePath}");
            }
            catch (Exception ex)
            {
                Log($"Erro ao salvar {_activeScript.FilePath}: {ex.Message}", true);
            }
        }

        private void DataGridViewScripts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;

            var clickedScript = (Script)dataGridViewScripts.Rows[e.RowIndex].DataBoundItem;
            if (clickedScript == null) return;

            // Salva conteúdo do script anterior em memória
            if (_activeScript != null)
            {
                _activeScript.Content = sqlEditor.Text;
                _activeScript.IsDirty = true;
            }

            _activeScript = clickedScript;

            if (!string.IsNullOrEmpty(_activeScript.Content))
            {
                sqlEditor.Text = _activeScript.Content;
            }
            else if (File.Exists(_activeScript.FilePath))
            {
                _activeScript.Content = File.ReadAllText(_activeScript.FilePath);
                sqlEditor.Text = _activeScript.Content;
            }
            else
            {
                _activeScript.Content = string.Empty;
                sqlEditor.Text = string.Empty;
            }

            Log($"Script ativo: {_activeScript.DisplayName}");
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

        private void Log(string message, bool isError = false)
        {
            string prefix = isError ? "[ERRO]" : "[INFO]";
            logBox.AppendText($"[{DateTime.Now:G}] {prefix} {message}");

            if (isError) _logger.LogError(message);
            else _logger.LogInformation(message);
        }
    }
}
