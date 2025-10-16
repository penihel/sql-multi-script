using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Core.Interfaces
{
    public interface IDatabaseDistributionListService
    {
        Task<IList<DatabaseDistributionList>> ListAsync();
        Task<Result<Guid>> CreateAsync(string name);

    }
}
    