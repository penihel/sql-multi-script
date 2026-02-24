using FreeSQLMultiScript.Core.Interfaces;
using FreeSQLMultiScript.Core.Models;
using FreeSQLMultiScript.Core.Models.Files;
using FreeSQLMultiScript.Resources;

namespace FreeSQLMultiScript.Services
{
    public class DatabaseDistributionListService : IDatabaseDistributionListService
    {

        private readonly IPathService _pathService;

        public DatabaseDistributionListService(IPathService pathService)
        {
            _pathService = pathService;
        }

        public async Task<Result<DatabaseDistributionList>> CreateAsync(string name)
        {
            var list = await ListAsync();

            if (list.Any(d => d.Name == name))
            {
                return Result<DatabaseDistributionList>.Fail(Strings.RecordAlreadyExists);
            }

            var directoryPath = _pathService.GetDatabaseDistributionListsPath();

            var fileName = _pathService.GetNewValidJsonFileName(_pathService.GetDatabaseDistributionListsPath(), name);

            


            var databaseDistributionList = new DatabaseDistributionList()
            {
                Name = name,
                FilePath = fileName
            };

            var databaseDistributionListFile = new DatabaseDistributionListFile()
            {
                DatabaseDistributionList = databaseDistributionList,
            };


            if (File.Exists(fileName))
            {
                return Result<DatabaseDistributionList>.Fail(Strings.RecordAlreadyExists);

            }

            var json = System.Text.Json.JsonSerializer.Serialize(databaseDistributionListFile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(fileName, json);

            return Result<DatabaseDistributionList>.Ok(databaseDistributionList);
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
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<Result> SaveAsync(DatabaseDistributionList databaseDistributionList)
        {
            

            var file = databaseDistributionList.FilePath;

            var databaseDistributionListFile = new DatabaseDistributionListFile()
            {
                DatabaseDistributionList = databaseDistributionList,
            };

            var json = System.Text.Json.JsonSerializer.Serialize(databaseDistributionListFile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(file, json);

            return Result.Ok();
        }

        public Task<Result> DeleteAsync(DatabaseDistributionList databaseDistributionList)
        {
            if (databaseDistributionList == null) throw new ArgumentNullException(nameof(databaseDistributionList));

            if (!string.IsNullOrEmpty(databaseDistributionList.FilePath) && File.Exists(databaseDistributionList.FilePath))
            {
                File.Delete(databaseDistributionList.FilePath);
            }

            return Task.FromResult(Result.Ok());
        }

        public async Task<Result> RenameAsync(DatabaseDistributionList databaseDistributionList, string newName)
        {
            if (databaseDistributionList == null) throw new ArgumentNullException(nameof(databaseDistributionList));
            if (string.IsNullOrWhiteSpace(newName)) return Result.Fail(Strings.FieldCannotBeEmpty);

            var list = await ListAsync();
            if (list.Any(d => d.Name == newName && d.FilePath != databaseDistributionList.FilePath))
            {
                return Result.Fail(Strings.RecordAlreadyExists);
            }

            databaseDistributionList.Name = newName;
            return await SaveAsync(databaseDistributionList);
        }
    }

}
