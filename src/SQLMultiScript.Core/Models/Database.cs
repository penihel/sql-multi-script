using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class Database : SelectableItem
    {
        public string DatabaseName { get; set; }
        public string ConnectionName { get; set; }

        [JsonIgnore]
        public Connection Connection { get; set; }

    }
}
