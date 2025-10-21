using System.ComponentModel;

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



    }

    public enum ExecutionStatus
    {
        Queued,
        Executing,
        Error,
        Success,
    }
}
