namespace SQLMultiScript.Core.Models
{
    public class Database : SelectableItem
    {
        public string DisplayName { get; set; }

        

        public ServerConnection ServerConnection { get; set; }

    }
}
