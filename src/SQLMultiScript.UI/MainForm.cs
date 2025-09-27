
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

        private ListBox listScripts;
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

            var height20 = (int)Math.Ceiling(Height * 0.8);


            // Split esquerda / resto
            splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = height20;
            Controls.Add(splitMain);
            splitMain.BringToFront(); // não deixar o MenuStrip escondido


            // Split centro / direita+footer
            splitLeft = new SplitContainer();
            splitLeft.Dock = DockStyle.Fill;
            splitLeft.Orientation = Orientation.Vertical;
            splitLeft.SplitterDistance = 600;
            splitMain.Panel1.Controls.Add(splitLeft);


            // Split centro / direita+footer
            splitCenterRight = new SplitContainer();
            splitCenterRight.Dock = DockStyle.Fill;
            splitCenterRight.Orientation = Orientation.Vertical;
            splitCenterRight.SplitterDistance = 600;
            splitLeft.Panel2.Controls.Add(splitCenterRight);

            // Editor SQL (centro)
            sqlEditor = new Scintilla();
            sqlEditor.Dock = DockStyle.Fill;
            splitCenterRight.Panel1.Controls.Add(sqlEditor);

            // Split direita / footer
            splitResultFooter = new SplitContainer();
            splitResultFooter.Dock = DockStyle.Bottom;
            splitResultFooter.Orientation = Orientation.Horizontal;
            splitResultFooter.SplitterDistance = 300;
            Controls.Add(splitResultFooter);
            splitResultFooter.BringToFront();

            // Placeholder na direita (ex: conexões)
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = Color.Beige;
            splitCenterRight.Panel2.Controls.Add(rightPanel);



            // Placeholder na direita (ex: conexões)
            var resultPanel = new Panel();
            resultPanel.Dock = DockStyle.Fill;
            resultPanel.BackColor = Color.Beige;
            splitResultFooter.Panel1.Controls.Add(resultPanel);

            logBox = new TextBox();
            logBox.Dock = DockStyle.Bottom;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Height = 150; // altura fixa do footer
            splitResultFooter.Panel2.Controls.Add(logBox);
            logBox.BringToFront();

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
