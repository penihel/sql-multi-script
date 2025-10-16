namespace SQLMultiScript.Core.Models.Files
{
    public class ProjectFile : BaseFile
    {
        public const string Type = "ProjectFile";
        const string Version = "v1.0";
        public ProjectFile() : base(Type, Version)
        {
        }

        public Project Project { get; set; }
    }
}
