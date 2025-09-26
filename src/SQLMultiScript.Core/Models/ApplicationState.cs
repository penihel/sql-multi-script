namespace SQLMultiScript.Core.Models
{
    /// <summary>
    /// Classe geral que guarda todo o estado da aplicação
    /// </summary>
    public class ApplicationState
    {
        public List<Script> ScriptsToExecute { get;  set; }
    }
}
