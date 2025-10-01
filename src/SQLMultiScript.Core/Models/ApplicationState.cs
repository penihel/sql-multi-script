namespace SQLMultiScript.Core.Models
{
    /// <summary>
    /// Classe geral que guarda todo o estado da aplicação
    /// </summary>
    public class ApplicationState
    {
        public string Path { get; set; } = string.Empty;
        public List<Script> ScriptsToExecute { get; set; }


        public async Task<Script> NewScript()
        {
            var fileName = $"Script_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            var filePath = System.IO.Path.Combine(Path, fileName);

            File.CreateText(filePath).Close();

            var script = new Script() { FilePath = filePath, Selected = true };

            ScriptsToExecute.Add(script );

            return script;
        }
    }
}
