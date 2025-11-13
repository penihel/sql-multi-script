using Microsoft.Data.SqlClient;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace SQLMultiScript.Services
{
    public class ExecutionService : IExecutionService
    {
        public SynchronizationContext UiContext { get; set; }

        public event Action<string> Log;
        public event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, string> InfoMessageRecived;
        public event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable, DataRow> RowAdded;
        public event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, DataTable> TableAdded;
        public event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, int> BatchCompleted;
        public event Action<ExecutionScriptInfo, ExecutionDatabaseInfo, Exception> ErrorOccurred;


        private readonly IConnectionService _connectionService;

        private readonly int _commandTimeoutSeconds;

        private ICollection<Connection> _connections;
        public ExecutionService(IConnectionService connectionService, int commandTimeoutSeconds = 60000)
        {

            _commandTimeoutSeconds = commandTimeoutSeconds;
            _connectionService = connectionService;
            

        }

        public async Task OpenConnectionsAsync(IEnumerable<Database> databases)
        {
            _connections = await _connectionService.ListAsync();

            RaiseOnUI(() => Log?.Invoke("Opening connections to databases..."));


            foreach (var database in databases)
            {
                try
                {
                    var connection = _connections.FirstOrDefault(c => c.Name == database.ConnectionName)
                    ?? throw new Exception($"Connection '{database.ConnectionName}' not found.");

                    string connectionString = _connectionService.BuildConnectionString(connection, database.DatabaseName);

                    await using var sqlConnection = new SqlConnection(connectionString);

                    RaiseOnUI(() => Log?.Invoke($"Connecting to {database.DatabaseName}"));

                    await sqlConnection.OpenAsync();

                    await sqlConnection.CloseAsync();
                }
                catch (Exception ex)
                {
                    RaiseOnUI(() => Log?.Invoke(ex.Message));
                    throw;
                }

            }


        }

        public async Task ExecuteAsync(ExecutionScriptInfo scriptInfo, IProgress<ExecutionProgress> progress)
        {
            // Semaphore para limitar o número de execuções simultâneas
            using var semaphore = new SemaphoreSlim(3); // 5 threads paralelas

            scriptInfo.Status = ExecutionStatus.Executing;

            progress?.Report(new ExecutionProgress(scriptInfo, null));

            var tasks = new List<Task>();


            foreach (var databaseInfo in scriptInfo.DatabasesInfo)
            {


                tasks.Add(Task.Run(async () =>
                {

                    await semaphore.WaitAsync();



                    try
                    {

                        databaseInfo.Status = ExecutionStatus.Executing;


                        progress?.Report(new ExecutionProgress(scriptInfo, databaseInfo));



                        var scriptResponse = await InternalExecuteAsync(scriptInfo, databaseInfo);


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
                        databaseInfo.Status = ExecutionStatus.Error;
                        databaseInfo.Response = new ExecutionDatabaseResponse
                        {
                            Success = false,
                            Messages = new List<string> { exScript.Message }
                        };

                        RaiseOnUI(() => ErrorOccurred?.Invoke(scriptInfo, databaseInfo, exScript));


                    }
                    finally
                    {
                        progress?.Report(new ExecutionProgress(scriptInfo, databaseInfo));

                        semaphore.Release();
                    }
                }));



            }

            await Task.WhenAll(tasks);


            var scriptInfoError = scriptInfo.DatabasesInfo.Any(di => di.Status == ExecutionStatus.Error);

            scriptInfo.Status = scriptInfoError ? ExecutionStatus.Error : ExecutionStatus.Success;


            progress?.Report(new ExecutionProgress(scriptInfo, null));

        }
        private async Task<ExecutionDatabaseResponse> InternalExecuteAsync(ExecutionScriptInfo scriptInfo, ExecutionDatabaseInfo databaseInfo)//;;,Database database, ExecutionScriptInfo scriptInfo)
        {
            var scriptResponse = new ExecutionDatabaseResponse();

            var database = databaseInfo.Database;

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


            //Log($"{script.Name}|{databaseConnectionInfo} => Batches: {batches.Count}");

            await using var sqlConnection = new SqlConnection(connectionString);

            sqlConnection.InfoMessage += (s, e) =>
            {
                e.Errors.Cast<SqlError>().ToList().ForEach(err =>
                {
                    scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] {err.Message}");
                });

                scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] {e.Message}");
            };

            await sqlConnection.OpenAsync();


            RaiseLog(script, connectionString, "Connection Opened");

            using var transaction = sqlConnection.BeginTransaction();

            try
            {
                for (int i = 0; i < batches.Count; i++)
                {
                    string batch = batches[i];
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;


                    //Log($"{script.Name}|{databaseConnectionInfo}|batch {i + 1}/{batches.Count} => Executing batch");

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
                        // Obtém o schema antes de começar a ler
                        var schema = reader.GetColumnSchema();

                        // Cria uma nova tabela para cada resultset
                        var tableName = $"Batch{i + 1}_Result{++resultIndex}";

                        DataTable table;

                        lock (scriptInfo.DataSet)
                        {



                            table = scriptInfo.DataSet.Tables[tableName];



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


                                RaiseOnUI(() => TableAdded?.Invoke(scriptInfo, databaseInfo, table));

                            }
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

                            RaiseOnUI(() => RowAdded?.Invoke(scriptInfo, databaseInfo, table, row));

                            // Aqui a UI pode ser notificada (ex: evento, INotifyCollectionChanged, etc)
                            // Se você estiver em WPF/WinForms e o DataGrid estiver ligado ao DataSet,
                            // a linha já vai aparecer automaticamente.

                        }

                    } while (await reader.NextResultAsync());
                }

                transaction.Commit();
                scriptResponse.Success = true;

                //_logger.LogInformation($"Script executado com sucesso em {GetDatabaseConnectionInfo(connectionString)}. {scriptInfo.DataSet.Tables.Count} resultados obtidos.");
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Erro ao executar script em {GetDatabaseConnectionInfo(connectionString)}. Fazendo rollback.\n{ex.Message}");
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
                    //_logger.LogError(rbEx, rbEx.Message);
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

        private void RaiseLog(Script script, string connectionString, string message)
        {
            var logPrefix = GetLogPrefix(script, connectionString);

            RaiseOnUI(() => Log?.Invoke($"{logPrefix} {message}"));

        }
        private static string GetLogPrefix(Script script, string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                var db = $"{builder.DataSource}.{builder.InitialCatalog}";

                return $"[{script.Name}] [{db}]";
            }
            catch
            {
                return "connection_string (masked)";
            }
        }

        private void RaiseOnUI(Action action)
        {
            if (UiContext == null)
                action();
            else
                UiContext.Post(_ => action(), null);
        }
    }

}