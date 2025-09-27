using ScintillaNET;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.UI
{
    public partial class MainForm : Form
    {
        private Scintilla scintilla;
        public MainForm(IApplicationStateService applicationStateService)
        {
            InitializeComponent();
            InitializeScintilla();
            _applicationStateService = applicationStateService;
        }

        private IApplicationStateService _applicationStateService { get; }
        private ApplicationState _applicationState;

        private async void MainForm_Load(object sender, EventArgs e)
        {
            _applicationState = await _applicationStateService.LoadAsync();

            RefreshUI();
        }

        private void RefreshUI()
        {
            RefreshScriptsList();
        }
        private void RefreshScriptsList()
        {
            dataGridViewScripts.Rows.Clear();
            dataGridViewScripts.RowHeadersVisible = false;
            foreach (var script in _applicationState.ScriptsToExecute)
            {
                dataGridViewScripts.Rows.Add(script.Selected, script.Order, script.FilePath);
            }
        }

        private void InitializeScintilla()
        {
            scintilla = new Scintilla();
            scintilla.Dock = DockStyle.Fill;
            this.splitContainer1.Panel2.Controls.Add(scintilla);

            // Fonte e tamanho
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();

            // Numeração de linhas
            scintilla.Margins[0].Width = 40;

            // Destaque de sintaxe SQL
            scintilla.LexerName = "sql";

            scintilla.Styles[Style.Sql.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Sql.Number].ForeColor = Color.Orange;
            scintilla.Styles[Style.Sql.Word].ForeColor = Color.Blue;
            scintilla.Styles[Style.Sql.String].ForeColor = Color.Brown;

            // Palavras reservadas SQL
            scintilla.SetKeywords(0, "SELECT FROM WHERE INSERT UPDATE DELETE CREATE ALTER DROP JOIN ON AND OR NOT NULL");

            // Auto-indent e tabulação
            scintilla.IndentWidth = 4;
            scintilla.TabWidth = 4;
            scintilla.UseTabs = false;

            scintilla.CaretLineVisible = true;
            scintilla.CaretLineBackColor = Color.LightYellow;
        }
    }
}
