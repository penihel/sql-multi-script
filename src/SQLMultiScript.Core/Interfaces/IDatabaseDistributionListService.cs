using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IDatabaseDistributionListService
    {
        Task<IList<DatabaseDistributionList>> ListAsync();
        Task<Result<DatabaseDistributionList>> CreateAsync(string name);
        Task<Result> SaveAsync(DatabaseDistributionList databaseDistributionList);

    }
}
    