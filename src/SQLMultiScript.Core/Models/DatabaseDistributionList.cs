namespace SQLMultiScript.Core.Models
{
    public class DatabaseDistributionList: SelectableItem
    {
        public string DisplayName { get; set; }

        public List<Database> Databases { get; private set; } = new List<Database>();
    }
}
