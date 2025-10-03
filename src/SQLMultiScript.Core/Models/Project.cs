using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    /// <summary>
    /// Classe geral que guarda todo o estado da aplicação
    /// </summary>
    public class Project
    {
        [JsonIgnore]
        public string FilePath { get; set; }
        public string Version { get; set; }
        public string DisplayName { get; set; }
        public BindingList<Script> Scripts { get; set; } = new BindingList<Script>();


        public Guid? SelectedDistributionListId { get; set; }

        

    }
}
