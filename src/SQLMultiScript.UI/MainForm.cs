
using Microsoft.Extensions.Logging;
using ScintillaNET;
using SQLMultiScript.Core.Interfaces;

namespace SQLMultiScript.UI
{
    public class MainForm : Form
    {
        private readonly IApplicationStateService _applicationStateService;
        private readonly ILogger _logger;

        //Componentes
        private SplitContainer splitMain;
        private SplitContainer splitLeft;
        private SplitContainer splitCenterRight;
        private SplitContainer splitResultFooter;

        private CheckedListBox listScripts;
        private Scintilla sqlEditor;
        private TextBox logBox;

        private MenuStrip menuStrip;
        private ToolStripMenuItem executarMenu;

        public MainForm(
            ILogger logger,
            IApplicationStateService applicationStateService)
        {
            _applicationStateService = applicationStateService;
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

            // Exemplo inicial
            sqlEditor.Text = "SELECT * FROM Users WHERE Active = 1;";
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void ExecutarMenu_Click(object sender, EventArgs e)
        {
            string script = sqlEditor.Text;

            if (string.IsNullOrWhiteSpace(script))
            {
                logBox.AppendText("[WARN] Script vazio, nada a executar.\r\n");
                return;
            }

            // 🔹 Aqui seria onde você chama seu mecanismo de execução de SQL
            // Exemplo de simulação:
            logBox.AppendText($"[EXEC] Executando script às {DateTime.Now:T}\r\n");
            logBox.AppendText(script + "\r\n");
            logBox.AppendText("[SUCESSO] Script executado com sucesso!\r\n\r\n");
        }
    }
}
