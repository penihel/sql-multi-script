namespace SQLMultiScript.Core.Models
{
    public class DatabaseDistributionList : SelectableItem
    {
        
        public Guid Id { get; set; }
        public string FilePath { get; set; }
        public string DisplayName { get; set; }

        public List<Database> Databases { get; private set; } = new List<Database>();
    }
}
