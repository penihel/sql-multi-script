using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using System.Text.RegularExpressions;

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

        public string GetNewValidJsonFileName(string path, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = Path.GetRandomFileName();

            // Remove espaços no início/fim e caracteres inválidos
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = $"[{invalidChars}]+";
            string cleaned = Regex.Replace(text.Trim(), invalidReStr, "_");

            // Evita nomes vazios ou só com pontos
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.All(c => c == '.'))
                cleaned = Path.GetRandomFileName();


            var fileName = Path.Combine(path, cleaned + ".json");

            while (File.Exists(fileName))
            {
                fileName = Path.GetRandomFileName() + ".json";
            }

            return fileName;
        }
    }
}
