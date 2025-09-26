using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IApplicationStateService
    {
        Task<ApplicationState> LoadAsync();
    }
}
