using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;

namespace SQLMultiScript.Services
{
    public class PathService : IPathService
    {
        public string GetDatabaseDistributionListsPath()
        {
            var path = Path.Combine(GetAppDataPath(), "DatabaseDistributionLists");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;

        }

        public string GetConnectionsPath()
        {
            var path = Path.Combine(GetAppDataPath(), "Connections");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;

        }

        private static string GetAppDataPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, Constants.ApplicationName);
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            return appFolder;
        }
    }
}
