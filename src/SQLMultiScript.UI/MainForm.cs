using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.UI
{
    public partial class MainForm : Form
    {
        public MainForm(IApplicationStateService applicationStateService)
        {
            InitializeComponent();

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
    }
}
