using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class DatabaseDistributionList 
    {
        
        public Guid Id { get; set; }
        
        [JsonIgnore]
        public string FilePath { get; set; }
        public string Name { get; set; }

        public BindingList<Database> Databases { get; private set; } = new BindingList<Database>();
    }
}
