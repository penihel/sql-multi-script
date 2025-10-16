using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IConnectionService
    {
        Task<Result<Guid>> SaveAsync(Connection connection);
        Task<IList<Connection>> ListAsync();
        Task<IList<Database>> ListDatabasesAsync(Connection connection);

        Task TestAsync(Connection connection);
    }
}
    