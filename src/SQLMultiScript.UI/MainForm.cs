
using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.UI
{
    public class MainForm : Form
    {
        private readonly IProjectService _projectService;
        private readonly ILogger _logger;


        private Project _currentProject;// = new Project();

        //Componentes
        private SplitContainer splitMain;
        private SplitContainer splitLeft;
        private SplitContainer splitCenterRight;
        private SplitContainer splitResultFooter;


        private Scintilla sqlEditor;
        private TextBox logBox;

        private MenuStrip menuStrip;
        private ToolStripMenuItem executarMenu;

        private DataGridView dataGridViewScripts;
        public MainForm(
            ILogger logger,
            IProjectService projectService)
        {
            _projectService = projectService;
            _logger = logger;

            InitializeLayout();
            InitializeMenu();
            InitializeEditor();

        }

        private void InitializeLayout()
        {
            //Form
            Text = "SQL MultiScript (by penihel@gmail.com)";
            WindowState = FormWindowState.Maximized;
            this.Load += MainForm_Load;




            // Split principal (top and bottom)
            splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = 800;
            splitMain.BorderStyle = UIConstants.SplitterBorderStyle;
            splitMain.SplitterWidth = UIConstants.SplitterWidth;
            Controls.Add(splitMain);
            splitMain.BringToFront(); // não deixar o MenuStrip escondido


            // Split esquerda
            splitLeft = new SplitContainer();
            splitLeft.Dock = DockStyle.Fill;
            splitLeft.Orientation = Orientation.Vertical;
            splitLeft.SplitterDistance = 10;
            splitLeft.BorderStyle = UIConstants.SplitterBorderStyle;
            splitLeft.SplitterWidth = UIConstants.SplitterWidth;
            splitMain.Panel1.Controls.Add(splitLeft);




            // Split centro / direita
            splitCenterRight = new SplitContainer();
            splitCenterRight.Dock = DockStyle.Fill;
            splitCenterRight.Orientation = Orientation.Vertical;
            splitCenterRight.SplitterDistance = 600;
            splitCenterRight.BorderStyle = UIConstants.SplitterBorderStyle;
            splitCenterRight.SplitterWidth = UIConstants.SplitterWidth;
            splitLeft.Panel2.Controls.Add(splitCenterRight);

            // Editor SQL (centro)
            sqlEditor = new Scintilla();
            sqlEditor.Dock = DockStyle.Fill;
            splitCenterRight.Panel1.Controls.Add(sqlEditor);

            // Split direita / footer
            splitResultFooter = new SplitContainer();
            splitResultFooter.Dock = DockStyle.Fill;
            splitResultFooter.Orientation = Orientation.Horizontal;
            splitResultFooter.SplitterDistance = 300;
            splitResultFooter.BorderStyle = UIConstants.SplitterBorderStyle;
            splitResultFooter.SplitterWidth = UIConstants.SplitterWidth;
            splitMain.Panel2.Controls.Add(splitResultFooter);


            // Placeholder na direita (ex: conexões)
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = Color.Beige;
            rightPanel.Controls.Add(new Label() { Text = "Painel Direito (ex: conexões)", Dock = DockStyle.Top });
            splitCenterRight.Panel2.Controls.Add(rightPanel);




            var resultPanel = new Panel();
            resultPanel.Dock = DockStyle.Fill;
            resultPanel.BackColor = Color.Beige;
            resultPanel.Controls.Add(new Label() { Text = "Painel de Resultados ", Dock = DockStyle.Top });
            splitResultFooter.Panel1.Controls.Add(resultPanel);

            logBox = new TextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;

            splitResultFooter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;  // dá um relevo no splitter
            splitResultFooter.SplitterWidth = 6;                  // aumenta a área visível do splitter
            splitResultFooter.Panel2.Controls.Add(logBox);



        }

        private void SetupScriptsPanel(Panel parentPanel)
        {
            var listPanel = new Panel()
            {
                Dock = DockStyle.Fill,
            };

            dataGridViewScripts = new DataGridView()
            {
                Dock = DockStyle.Fill,
            };

            // Faz o binding
            dataGridViewScripts.AutoGenerateColumns = false; // cria colunas sozinho

            // Coluna Checkbox (Selected)
            var colSelected = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected", // propriedade do objeto
                //HeaderText = "Selected?",
                Width = 60
            };
            dataGridViewScripts.Columns.Add(colSelected);

            // Coluna Texto (DisplayName)
            var colDisplayName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DisplayName",
                HeaderText = "Script",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dataGridViewScripts.Columns.Add(colDisplayName);
            dataGridViewScripts.CellDoubleClick += DataGridViewScripts_CellDoubleClick;
            dataGridViewScripts.RowHeadersVisible = false;
            dataGridViewScripts.AllowUserToAddRows = false;
            dataGridViewScripts.AllowUserToDeleteRows = false;





            listPanel.Controls.Add(dataGridViewScripts);
            parentPanel.Controls.Add(listPanel);

            var buttonPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 30

            };

            var btnAdd = new Button()
            {
                Text = "Add Existing",
                Dock = DockStyle.Right
            };

            btnAdd.Click += btnAdd_Click;

            var btnNew = new Button()
            {
                Text = "New",
                Dock = DockStyle.Left

            };

            btnNew.Click += btnNew_Click;

            buttonPanel.Controls.Add(btnAdd);
            buttonPanel.Controls.Add(btnNew);
            parentPanel.Controls.Add(buttonPanel);
        }

        private void DataGridViewScripts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Recupera o objeto Script vinculado à linha
                var script = (Script)dataGridViewScripts.Rows[e.RowIndex].DataBoundItem;

                if (script != null && File.Exists(script.FilePath))
                {
                    try
                    {
                        string sql = File.ReadAllText(script.FilePath);
                        sqlEditor.Text = sql; // carrega no editor
                        Log($"Script carregado: {script.FilePath}\r\n");
                    }
                    catch (Exception ex)
                    {
                        Log($"Não foi possível carregar {script.FilePath}: {ex.Message}\r\n", true);
                    }
                }
            }
        }


        private void btnAdd_Click(object sender, EventArgs e)
        {

            using var ofd = new OpenFileDialog();
            ofd.Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
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


        }
        private void btnNew_Click(object sender, EventArgs e)
        {

            if (_currentProject != null)
            {
                _currentProject.Scripts.Add(new Script()
                {
                    DisplayName = $"Script{_currentProject.Scripts.Count + 1}.sql",
                });
            }


        }


        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();
            executarMenu = new ToolStripMenuItem("Executar Script");
            executarMenu.Click += ExecutarMenu_Click;

            menuStrip.Items.Add(executarMenu);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }
        private void InitializeEditor()
        {
            // Fonte e estilo
            sqlEditor.StyleResetDefault();
            sqlEditor.Styles[Style.Default].Font = "Consolas";
            sqlEditor.Styles[Style.Default].Size = 10;
            sqlEditor.StyleClearAll();

            // Numeração de linhas
            sqlEditor.Margins[0].Width = 40;

            // Lexer SQL
            sqlEditor.LexerName = "sql";
            sqlEditor.Styles[Style.Sql.Comment].ForeColor = Color.Green;
            sqlEditor.Styles[Style.Sql.Number].ForeColor = Color.Orange;
            sqlEditor.Styles[Style.Sql.Word].ForeColor = Color.Blue;
            sqlEditor.Styles[Style.Sql.String].ForeColor = Color.Brown;

            // Palavras-chave SQL
            sqlEditor.SetKeywords(0, "SELECT FROM WHERE INSERT UPDATE DELETE CREATE ALTER DROP JOIN ON AND OR NOT NULL");

            // Tabulação
            sqlEditor.IndentWidth = 4;
            sqlEditor.TabWidth = 4;
            sqlEditor.UseTabs = false;

            // Linha ativa
            sqlEditor.CaretLineVisible = true;
            sqlEditor.CaretLineBackColor = Color.LightYellow;

            
            sqlEditor.LexerLanguage = "mssql";
            // Exemplo inicial
            sqlEditor.Text = "SELECT * FROM Users WHERE Active = 1;";
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
            ////Aqui é a pasta atual do projeto atual
            //var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //var folder = Path.Combine(appData, Constants.ApplicationName);

            //if (!Directory.Exists(folder))
            //{
            //    Directory.CreateDirectory(folder);
            //}

            //var filePath = Path.Combine(folder, Constants.ApplicationStateFileName);


            _currentProject = await _projectService.CreateNewAsync();



            SetupScriptsPanel(splitLeft.Panel1);

            BindData();
        }

        private void BindData()
        {
            dataGridViewScripts.DataSource = _currentProject.Scripts;
        }

        private void ExecutarMenu_Click(object sender, EventArgs e)
        {
            string script = sqlEditor.Text;

            if (string.IsNullOrWhiteSpace(script))
            {
                Log("Script vazio, nada a executar.\r\n");
                return;
            }

            // 🔹 Aqui seria onde você chama seu mecanismo de execução de SQL
            // Exemplo de simulação:
            Log($"Executando script às {DateTime.Now:T}\r\n");
            Log(script + "\r\n");
            Log("script executado com sucesso!\r\n\r\n");
        }


        private void Log(string message, bool isError = false)
        {
            string prefix = isError ? "[ERRO]" : "[INFO]";
            logBox.AppendText($"[{DateTime.Now:G}] {prefix} {message}\r\n");

            if (isError)
                _logger.LogError(message);
            else
                _logger.LogInformation(message);
        }


    }
}