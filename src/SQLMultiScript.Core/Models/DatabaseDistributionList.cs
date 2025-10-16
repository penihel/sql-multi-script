using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class DatabaseDistributionList 
    {
        
        public Guid Id { get; set; }
        
        [JsonIgnore]
        public string FilePath { get; set; }
        public string DisplayName { get; set; }

        public List<Database> Databases { get; private set; } = new List<Database>();
    }
}
