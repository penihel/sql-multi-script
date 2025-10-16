namespace SQLMultiScript.Core.Models.Files
{
    public class ConnectionFile : BaseFile
    {
        public const string Type = "ConnectionFile";
        const string Version = "v1.0";
        public ConnectionFile() : base(Type, Version)
        {
        }

        public Connection Connection { get; set; }
    }
}
