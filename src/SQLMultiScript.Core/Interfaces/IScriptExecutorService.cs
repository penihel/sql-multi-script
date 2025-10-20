using SQLMultiScript.Core.Models;
using System.Data;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IScriptExecutorService
    {
        Task<DataSet> ExecuteAsync(Database database, Script script);
        
    }

}
