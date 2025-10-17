using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class DatabaseDistributionList 
    {
        [JsonIgnore]
        public string FilePath { get; set; }
        public string Name { get; set; }

        public BindingList<Database> Databases { get; set; } = new BindingList<Database>();
    }
}
