using FreeSQLMultiScript.Core;
using FreeSQLMultiScript.Core.Interfaces;
using FreeSQLMultiScript.Core.Models;
using FreeSQLMultiScript.Core.Models.Files;

namespace FreeSQLMultiScript.Services
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
                        Name = "Script1.sql",
                        Selected = true
                        
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
                var projectFile = System.Text.Json.JsonSerializer.Deserialize<ProjectFile>(json);
                if (projectFile?.Project != null)
                {
                    state = projectFile.Project;
                    state.FilePath = filePath;
                }
            }
            return state;

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
