using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Services
{
    public class ApplicationStateService: IApplicationStateService
    {
        public ApplicationStateService()
        {
            
        }

        public async Task<ApplicationState> LoadAsync()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var folder = Path.Combine(appData, Constants.ApplicationName);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filePath = Path.Combine(folder, Constants.ApplicationStateFileName);
            var state = new ApplicationState();

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                state = System.Text.Json.JsonSerializer.Deserialize<ApplicationState>(json);
            }
            return state ?? new ApplicationState();

        }
    }
}
