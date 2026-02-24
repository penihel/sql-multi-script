using FreeSQLMultiScript.Core.Models;

namespace FreeSQLMultiScript.Core.Interfaces
{
    public interface IDatabaseDistributionListService
    {
        Task<IList<DatabaseDistributionList>> ListAsync();
        Task<Result<DatabaseDistributionList>> CreateAsync(string name);
        Task<Result> SaveAsync(DatabaseDistributionList databaseDistributionList);
        Task<Result> DeleteAsync(DatabaseDistributionList databaseDistributionList);
        Task<Result> RenameAsync(DatabaseDistributionList databaseDistributionList, string newName);

    }
}
    