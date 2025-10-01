using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Services
{
    public class ApplicationStateService : IApplicationStateService
    {
        public ApplicationStateService()
        {

        }

        public async Task<ApplicationState> LoadAsync(string filePath)
        {





            var state = new ApplicationState();

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                state = System.Text.Json.JsonSerializer.Deserialize<ApplicationState>(json);
                if (state != null)
                    state.Path = Path.GetDirectoryName(filePath) ?? string.Empty;
            }
            return state ?? new ApplicationState();

        }
    }
}
