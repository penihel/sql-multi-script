using System.ComponentModel;
using System.Data;

namespace SQLMultiScript.Core.Models
{
    public class Execution
    {
        public string Name { get; set; }
        public ExecutionStatus Status { get; set; }

        public BindingList<ExecutionScriptInfo> ScriptsInfo { get; set; } = new BindingList<ExecutionScriptInfo>();

    }

    public class ExecutionScriptInfo
    {
        public Script Script { get; set; }
        public ExecutionStatus Status { get; set; }

        public BindingList<ExecutionDatabaseInfo> DatabasesInfo { get; set; } = new BindingList<ExecutionDatabaseInfo>();


    }

    public class ExecutionDatabaseInfo
    {
        public Database Database { get; set; }
        public ExecutionStatus Status { get; set; }

        public bool Selected
        {
            get => Database?.Selected ?? false;
        }

        public string DatabaseName
        {
            get => Database?.DatabaseName;
        }

        public string ConnectionName
        {
            get => Database?.ConnectionName;
        }


        public ExecutionDatabaseResponse Response { get; set; }

    }
    public class ExecutionDatabaseResponse
    {
        public bool Success { get; set; }
        public DataSet DataSet { get; set; }
        public List<string> Messages { get; set; } = new();
        public string MessagesText => string.Join(Environment.NewLine, Messages);
    }
    public enum ExecutionStatus
    {
        Queued,
        Executing,
        Error,
        Success,
    }
}
