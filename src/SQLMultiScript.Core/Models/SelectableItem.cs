namespace SQLMultiScript.Core.Models
{
    public abstract class SelectableItem
    {
        public bool Selected { get; set; }
        public int Order { get; set; }
    }
}
