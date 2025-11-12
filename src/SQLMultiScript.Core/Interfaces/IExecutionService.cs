using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IExecutionService
    {
        Task LoadConnectionsAsync();
        Task ExecuteAsync(Execution execution, IProgress<ExecutionProgress> progress);

        event Action<string> Log;
        event Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo, string> InfoMessageRecived;
        event Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable, DataRow> RowAdded;
        event Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable> ResultSetCompleted;
        event Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo, int> BatchCompleted;
        event Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo, Exception> ErrorOccurred;
    }

}
