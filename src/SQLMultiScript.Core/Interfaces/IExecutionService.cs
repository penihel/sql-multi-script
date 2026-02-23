using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IExecutionService
    {
        SynchronizationContext UiContext { get; set; }
        Task OpenConnectionsAsync(IEnumerable<Database> databases, CancellationToken cancellationToken = default);
        Task ExecuteAsync(ExecutionScriptInfo scriptInfo, IProgress<ExecutionProgress> progress, CancellationToken cancellationToken = default);

        event Action<string> Log;
        event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, string> InfoMessageRecived;
        event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable, DataRow> RowAdded;
        event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable> TableAdded;
        event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, int> BatchCompleted;
        event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, Exception> ErrorOccurred;
    }

}
