namespace SQLMultiScript.Core.Models
{
    public class Script : SelectableItem
    {
        public string FilePath { get; set; }

        public override string ToString()   
        {
            return Path.GetFileName(FilePath);
        }
        
    }
}
