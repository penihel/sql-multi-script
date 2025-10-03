using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Services
{
    public class DatabaseDistributionListService : IDatabaseDistributionListService
    {
        public DatabaseDistributionListService()
        {
            
        }
        public async Task<IList<DatabaseDistributionList>> ListAsync(string directoryPath)
        {   
            var retorno = new List<DatabaseDistributionList>();

            if (Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (var file in files) {

                    if (File.Exists(file)) { 
                        
                        var json = await File.ReadAllTextAsync(file);
                        var item = System.Text.Json.JsonSerializer.Deserialize<DatabaseDistributionList>(json);
                        if (item != null && item.Id != Guid.Empty)
                        {
                            item.FilePath = file;
                            retorno.Add(item);
                        }
                            
                    }
                }
            }

            return retorno;
        }
    }
}
