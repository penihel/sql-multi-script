using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace SQLMultiScript.Services
{
    public class ExecutionService : IExecutionService
    {

        public event Action<Execution, ExecutionScriptInfo, Database, string> InfoMessageRecived;
        public event Action<Execution, ExecutionScriptInfo, Database, DataTable, DataRow> RowAdded;
        public event Action<Execution, ExecutionScriptInfo, Database, DataTable> ResultSetCompleted;
        public event Action<Execution, ExecutionScriptInfo, Database, int> BatchCompleted;
        public event Action<Execution, ExecutionScriptInfo, Database, Exception> ErrorOccurred;


        private readonly IConnectionService _connectionService;
        private readonly ILogger _logger;
        private readonly int _commandTimeoutSeconds;

        private ICollection<Connection> _connections;
        public ExecutionService(IConnectionService connectionService, ILogger logger, int commandTimeoutSeconds = 600)
        {

            _commandTimeoutSeconds = commandTimeoutSeconds;
            _connectionService = connectionService;
            _logger = logger;
        }

        public async Task LoadConnectionsAsync()
        {
            _connections = await _connectionService.ListAsync();
        }

        public async Task ExecuteAsync(Execution execution, IProgress<ExecutionProgress> progress)
        {

            // Semaphore para limitar o número de execuções simultâneas
            using var semaphore = new SemaphoreSlim(1); // 5 threads paralelas


            execution.Status = ExecutionStatus.Executing;

            progress?.Report(new ExecutionProgress(execution, null, null));


            foreach (var scriptInfo in execution.ScriptsInfo)
            {

                scriptInfo.Status = ExecutionStatus.Executing;

                progress?.Report(new ExecutionProgress(execution, scriptInfo, null));

                var tasks = new List<Task>();


                foreach (var databaseInfo in scriptInfo.DatabasesInfo)
                {


                    tasks.Add(Task.Run(async () =>
                    {

                        await semaphore.WaitAsync();

                        var db = databaseInfo.Database;

                        try
                        {

                            databaseInfo.Status = ExecutionStatus.Executing;

                            
                            progress?.Report(new ExecutionProgress(execution, scriptInfo, databaseInfo));



                            var scriptResponse = await InternalExecuteAsync(db, scriptInfo);


                            if (scriptResponse.Success)
                            {
                                databaseInfo.Status = ExecutionStatus.Success;
                            }
                            else
                            {
                                databaseInfo.Status = ExecutionStatus.Error;
                            }


                            databaseInfo.Response = scriptResponse;

                        }
                        catch (Exception exScript)
                        {
                            scriptInfo.Status = ExecutionStatus.Error;

                            _logger.LogError($"Erro ao executar script '{scriptInfo.Script.Name}' no banco '{db.DatabaseName}': {exScript.Message}");
                        }
                        finally
                        {
                            progress?.Report(new ExecutionProgress(execution, scriptInfo, databaseInfo));

                            semaphore.Release();
                        }
                    }));



                }

                await Task.WhenAll(tasks);

                //UPDATE ScriptInfo
                var scriptInfoError = scriptInfo.DatabasesInfo.Any(di => di.Status == ExecutionStatus.Error);

                scriptInfo.Status = scriptInfoError ? ExecutionStatus.Error : ExecutionStatus.Success;

                
                progress?.Report(new ExecutionProgress(execution, scriptInfo, null));

            }


            var executionError = execution.ScriptsInfo.Any(si => si.Status == ExecutionStatus.Error);

            execution.Status = executionError ? ExecutionStatus.Error : ExecutionStatus.Success;

            
            progress?.Report(new ExecutionProgress(execution, null, null));

        }
        private async Task<ExecutionDatabaseResponse> InternalExecuteAsync(Database database, ExecutionScriptInfo scriptInfo)
        {
            var scriptResponse = new ExecutionDatabaseResponse();

            var connectionModel = _connections.FirstOrDefault(c => c.Name == database.ConnectionName)
                ?? throw new Exception("connection notfound");

            var script = scriptInfo.Script;
            string content = script.Content ?? await File.ReadAllTextAsync(script.FilePath);
            string connectionString = _connectionService.BuildConnectionString(connectionModel, database.DatabaseName);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString não pode ser vazio.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("script não pode ser vazio.", nameof(script));




            var batches = SplitBatches(content).ToList();

            _logger.LogInformation($"Executando script em {HiddenConnectionInfo(connectionString)}. Batches: {batches.Count}");

            await using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.InfoMessage += (s, e) =>
            {
                scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] {e.Message}");
            };

            await sqlConnection.OpenAsync();

            using var transaction = sqlConnection.BeginTransaction();

            try
            {
                for (int i = 0; i < batches.Count; i++)
                {
                    string batch = batches[i];
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    _logger.LogInformation($"Executando batch {i + 1}/{batches.Count} (tamanho: {batch.Length} chars)");

                    await using var cmd = sqlConnection.CreateCommand();
                    cmd.UpdatedRowSource = UpdateRowSource.None;
                    cmd.Transaction = transaction;
                    cmd.CommandText = batch;
                    cmd.CommandTimeout = _commandTimeoutSeconds;
                    cmd.StatementCompleted += (s, e) =>
                    {
                        // Captura mensagens do tipo "(X rows affected)"
                        if (e.RecordCount >= 0)
                            scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] {e.RecordCount} linha(s) afetada(s)");
                    };
                    using var reader = await cmd.ExecuteReaderAsync();

                    int resultIndex = 0;

                    do
                    {
                        // Cria uma nova tabela para cada resultset
                        var tableName = $"Batch{i + 1}_Result{++resultIndex}";


                        var table = scriptInfo.DataSet.Tables[tableName];

                        // Obtém o schema antes de começar a ler
                        var schema = reader.GetColumnSchema();

                        if (table == null)
                        {
                            table = new DataTable(tableName);

                            table.Columns.Add("DatabaseName", typeof(string));
                            table.Columns.Add("ConnectionName", typeof(string));


                            for (int c = 0; c < schema.Count; c++)
                            {
                                var col = schema[c];
                                var colName = string.IsNullOrWhiteSpace(col.ColumnName)
                                    ? $"Column{c + 1}"
                                    : col.ColumnName;

                                // evita colunas duplicadas
                                if (table.Columns.Contains(colName))
                                    colName = $"{colName}_{c + 1}";

                                table.Columns.Add(colName, col.DataType ?? typeof(string));
                            }

                            scriptInfo.DataSet.Tables.Add(table);
                        }



                        // lê linha a linha, adicionando incrementalmente
                        while (await reader.ReadAsync())
                        {
                            var row = table.NewRow();
                            row["DatabaseName"] = database.DatabaseName;
                            row["ConnectionName"] = database.ConnectionName;

                            for (int c = 0; c < schema.Count; c++)
                            {
                                var colName = string.IsNullOrWhiteSpace(schema[c].ColumnName)
                                    ? $"Column{c + 1}"
                                    : schema[c].ColumnName;

                                var value = reader.GetValue(c);

                                row[colName] = value;//== DBNull.Value ? null : value;
                            }

                            table.Rows.Add(row);

                            // Aqui a UI pode ser notificada (ex: evento, INotifyCollectionChanged, etc)
                            // Se você estiver em WPF/WinForms e o DataGrid estiver ligado ao DataSet,
                            // a linha já vai aparecer automaticamente.

                        }

                    } while (await reader.NextResultAsync());
                }

                transaction.Commit();
                scriptResponse.Success = true;

                _logger.LogInformation($"Script executado com sucesso em {HiddenConnectionInfo(connectionString)}. {scriptInfo.DataSet.Tables.Count} resultados obtidos.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao executar script em {HiddenConnectionInfo(connectionString)}. Fazendo rollback.\n{ex.Message}");
                scriptResponse.Success = false;

                while (ex != null)
                {
                    scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] {ex.Message}");
                    ex = ex.InnerException;
                }

                try
                {
                    transaction.Rollback();
                }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, rbEx.Message);
                    throw;
                }
            }


            return scriptResponse;
        }


        private static IEnumerable<string> SplitBatches(string script)
        {
            // Divide por linhas contendo apenas "GO"
            var pattern = @"^\s*GO\s*$";
            var parts = Regex.Split(script, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return parts.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p));
        }

        private static string HiddenConnectionInfo(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return $"{builder.DataSource}/{builder.InitialCatalog}";
            }
            catch
            {
                return "connection_string (masked)";
            }
        }


    }

}