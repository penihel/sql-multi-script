using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IExecutionService
    {
        Task LoadConnectionsAsync();
        Task ExecuteAsync(Execution execution, IProgress<ExecutionProgress> progress);

        event Action<Execution, ExecutionScriptInfo, Database, string> InfoMessageRecived;
        event Action<Execution, ExecutionScriptInfo, Database, DataTable, DataRow> RowAdded;
        event Action<Execution, ExecutionScriptInfo, Database, DataTable> ResultSetCompleted;
        event Action<Execution, ExecutionScriptInfo, Database, int> BatchCompleted;
        event Action<Execution, ExecutionScriptInfo, Database, Exception> ErrorOccurred;
    }

}
