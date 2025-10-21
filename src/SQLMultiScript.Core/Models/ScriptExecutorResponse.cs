using System.Data;

namespace SQLMultiScript.Core.Models
{
    public class ScriptExecutorResponse
    {
        public DataSet DataSet { get; set; }
        public List<string> Messages { get; set; } = new();
        public string MessagesText => string.Join(Environment.NewLine, Messages);
    }
}
