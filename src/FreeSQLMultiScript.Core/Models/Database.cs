using System.Text.Json.Serialization;

namespace FreeSQLMultiScript.Core.Models
{
    public class Database : SelectableItem
    {
        public string DatabaseName { get; set; }
        public string ConnectionName { get; set; }
        
    }
}
