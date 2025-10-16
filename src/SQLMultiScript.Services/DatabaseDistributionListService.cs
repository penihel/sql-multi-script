using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Core.Models.Files;
using SQLMultiScript.Resources;

namespace SQLMultiScript.Services
{
    public class DatabaseDistributionListService : IDatabaseDistributionListService
    {

        private readonly IPathService _pathService;

        public DatabaseDistributionListService(IPathService pathService)
        {
            _pathService = pathService;
        }

        public async Task<Result<Guid>> CreateAsync(string name)
        {
            var list = await ListAsync();



            if (list.Any(d => d.DisplayName == name))
            {
                return Result<Guid>.Fail(Strings.RecordAlreadyExists);
            }

            var directoryPath = _pathService.GetDatabaseDistributionListsPath();

            var databaseDistributionList = new DatabaseDistributionList()
            {
                DisplayName = name,
                Id = Guid.NewGuid(),
            };

            var databaseDistributionListFile = new DatabaseDistributionListFile()
            {
                DatabaseDistributionList = databaseDistributionList,
            };

            var file = Path.Combine(directoryPath, databaseDistributionList.Id.ToString() + ".json");

            if (File.Exists(file))
            {
                return Result<Guid>.Fail(Strings.RecordAlreadyExists);

            }

            var json = System.Text.Json.JsonSerializer.Serialize(databaseDistributionListFile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(file, json);

            return Result<Guid>.Ok(databaseDistributionList.Id);
        }

        public async Task<IList<DatabaseDistributionList>> ListAsync()
        {
            var directoryPath = _pathService.GetDatabaseDistributionListsPath();

            var retorno = new List<DatabaseDistributionList>();

            if (Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {

                    if (File.Exists(file))
                    {

                        var json = await File.ReadAllTextAsync(file);
                        var item = System.Text.Json.JsonSerializer.Deserialize<DatabaseDistributionListFile>(json);
                        if (item != null && item.FileType == DatabaseDistributionListFile.Type
                            && item.DatabaseDistributionList != null)
                        {
                            item.DatabaseDistributionList.FilePath = file;

                            retorno.Add(item.DatabaseDistributionList);
                        }

                    }
                }
            }

            return retorno
                .OrderBy(x => x.DisplayName)
                .ToList();
        }
    }
}
