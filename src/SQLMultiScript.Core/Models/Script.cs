using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class Script : SelectableItem
    {
        public string FilePath { get; set; }

        public string Name { get; set; }
        
        [JsonIgnore] 
        public string Content { get; set; }

        // Flag para saber se o conteúdo foi alterado
        [JsonIgnore]
        public bool IsDirty { get; set; } = false;

        public override string ToString()   
        {
            return Name;
        }
        
    }
}
