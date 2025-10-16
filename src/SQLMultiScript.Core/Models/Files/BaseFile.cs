namespace SQLMultiScript.Core.Models.Files
{
    public abstract class BaseFile
    {
        public string FileType { get; set; }
        public string FileVersion{ get; set; }
        public string ApplicationVersion { get; set; }

        protected BaseFile(string fileType, string fileVersion)
        {
            FileType = fileType;
            FileVersion = fileVersion;
            ApplicationVersion = Constants.ApplicationVersion;
        }
    }
}
