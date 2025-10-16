namespace SQLMultiScript.Core.Models.Files
{
    public class DatabaseDistributionListFile : BaseFile
    {
        public const string Type = "DatabaseDistributionListFile";
        const string Version = "v1.0";
        public DatabaseDistributionListFile() : base(Type, Version)
        {
        }

        public DatabaseDistributionList DatabaseDistributionList { get; set; }
    }
}
