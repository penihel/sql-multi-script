namespace SQLMultiScript.Core.Interfaces
{
    public interface IPathService
    {
        string GetConnectionsPath();
        string GetDatabaseDistributionListsPath();
        string GetNewValidJsonFileName(string path, string text);
    }
}
