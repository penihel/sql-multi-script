using FreeSQLMultiScript.Core.Models;

namespace FreeSQLMultiScript.Core.Interfaces
{
    public interface IProjectService
    {
        Task<Project> LoadAsync(string filePath);
        Task<Project> CreateNewAsync();
        Task<Result<Project>> SaveAsync(Project project);
    }
}
