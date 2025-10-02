using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Services
{
    public class ProjectService : IProjectService
    {
        public ProjectService()
        {

        }

        public async Task<Project> CreateNewAsync()
        {
            var project = new Project
            {
                Version = Constants.ApplicationVersion, 
                Scripts = new System.ComponentModel.BindingList<Script>()
                {
                    new Script()
                    {
                        DisplayName = "Script1.sql",
                    }
                }
            };

            return await Task.FromResult(project);
        }

        public async Task<Project> LoadAsync(string filePath)
        {





            var state = new Project();

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                state = System.Text.Json.JsonSerializer.Deserialize<Project>(json);
                if (state != null)
                    state.FilePath = filePath;
            }
            return state ?? new Project();

        }
    }
}
