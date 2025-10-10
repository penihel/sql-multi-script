using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IConnectionService
    {
        Task SaveAsync(Connection connection);
        Task<IList<Connection>> ListAsync();

        string BuildConnectionString(Connection connection);
    }
}
    