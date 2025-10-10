using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IConnectionService
    {
        Task SaveAsync(Connection connection);

        string BuildConnectionString(Connection connection);
    }
}
    