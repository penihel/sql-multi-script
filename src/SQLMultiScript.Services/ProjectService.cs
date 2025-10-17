using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Core.Models.Files;

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
                
                Name = "New Project",
                Scripts = new System.ComponentModel.BindingList<Script>()
                {
                    new Script()
                    {
                        DisplayName = "Script1.sql",
                        Selected = true
                        
                    }
                }
            };

            return await Task.FromResult(project);
        }

        public Task<Result<Project>> CreateNewAsync(string filePath)
        {
            throw new NotImplementedException();
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

        public async Task<Result<Project>> SaveAsync(Project project)
        {
            var projectFile = new ProjectFile()
            {
                Project = project,
            };

            var projectJson = System.Text.Json.JsonSerializer.Serialize(projectFile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(project.FilePath, projectJson);

            return Result<Project>.Ok(project);

        }
    }
}
