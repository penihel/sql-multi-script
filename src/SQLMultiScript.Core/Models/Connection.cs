using System.Text.Json.Serialization;

namespace SQLMultiScript.Core.Models
{
    public class Connection
    {

        public Guid Id { get; set; }
        public string Server { get; set; }
        public string Auth { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        [JsonIgnore]
        public string FilePath { get; set; }
    }
}
