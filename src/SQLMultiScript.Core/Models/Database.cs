namespace SQLMultiScript.Core.Models
{
    public class Database : SelectableItem
    {
        public string DatabaseName { get; set; }

        public Connection Connection { get; set; }

    }
}
