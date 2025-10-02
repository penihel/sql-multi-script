using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class Script : SelectableItem
    {
        public string FilePath { get; set; }

        public string DisplayName { get; set; }
        
        [JsonIgnore] 
        public string RuntimeContent { get; set; }
        public override string ToString()   
        {
            return DisplayName;
        }
        
    }
}
