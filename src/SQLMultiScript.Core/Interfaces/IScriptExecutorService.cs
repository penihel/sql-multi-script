using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IScriptExecutorService
    {
        Task LoadConnectionsAsync();
        Task<ScriptExecutorResponse> ExecuteAsync(Database database, Script script, Action<string, bool> logAction);
        
    }

}
