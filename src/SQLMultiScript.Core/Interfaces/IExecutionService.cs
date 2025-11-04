using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IExecutionService
    {
        Task LoadConnectionsAsync();
        Task ExecuteAsync(Execution execution, Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo> statusUpdated);
        
    }

}
