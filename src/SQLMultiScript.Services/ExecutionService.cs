using Microsoft.Data.SqlClient;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.Data;
using System.Diagnostics;
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

        public async Task OpenConnectionsAsync(IEnumerable<Database> databases, CancellationToken cancellationToken = default)
        {
            _connections = await _connectionService.ListAsync();

            var onePerConnection = databases
                .GroupBy(d => d.ConnectionName)
                .Select(g => g.First())
                .ToList();

            RaiseOnUI(() => Log?.Invoke($"[Auth] Authenticating on {onePerConnection.Count} server(s)..."));

            var swAuth = Stopwatch.StartNew();
            int authIndex = 0;

            foreach (var database in onePerConnection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                authIndex++;
                var idx = authIndex;

                try
                {
                    var connection = _connections.FirstOrDefault(c => c.Name == database.ConnectionName)
                    ?? throw new Exception($"Connection '{database.ConnectionName}' not found.");

                    string connectionString = _connectionService.BuildConnectionString(connection, database.DatabaseName);

                    await using var sqlConnection = new SqlConnection(connectionString);

                    RaiseOnUI(() => Log?.Invoke($"[Auth] ({idx}/{onePerConnection.Count}) {database.ConnectionName}"));

                    await sqlConnection.OpenAsync(cancellationToken);

                    await sqlConnection.CloseAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    RaiseOnUI(() => Log?.Invoke($"[Auth] Error on {database.ConnectionName}: {ex.Message}"));
                    throw;
                }
            }

            swAuth.Stop();
            RaiseOnUI(() => Log?.Invoke($"[Auth] Completed in {swAuth.Elapsed.TotalSeconds:F1}s"));
        }

        public async Task ExecuteAsync(ExecutionScriptInfo scriptInfo, IProgress<ExecutionProgress> progress, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(10);

            var scriptName = scriptInfo.Script.Name;
            var totalDbs = scriptInfo.DatabasesInfo.Count;
            int completed = 0;

            RaiseOnUI(() => Log?.Invoke($"[Exec] {scriptName}: starting on {totalDbs} database(s) (parallelism: 10)"));

            var swExec = Stopwatch.StartNew();

            scriptInfo.Status = ExecutionStatus.Executing;

            progress?.Report(new ExecutionProgress(scriptInfo, null));

            var tasks = new List<Task>();

            foreach (var databaseInfo in scriptInfo.DatabasesInfo)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        databaseInfo.Status = ExecutionStatus.Executing;

                        progress?.Report(new ExecutionProgress(scriptInfo, databaseInfo));

                        var scriptResponse = await InternalExecuteAsync(scriptInfo, databaseInfo, cancellationToken);

                        databaseInfo.Status = scriptResponse.Success
                            ? ExecutionStatus.Success
                            : ExecutionStatus.Error;

                        databaseInfo.Response = scriptResponse;

                        var done = Interlocked.Increment(ref completed);
                        var dbName = databaseInfo.Database.DatabaseName;
                        var status = databaseInfo.Status;
                        RaiseOnUI(() => Log?.Invoke($"[Exec] {scriptName}: ({done}/{totalDbs}) {dbName} => {status}"));
                    }
                    catch (OperationCanceledException)
                    {
                        databaseInfo.Status = ExecutionStatus.Cancelled;
                        databaseInfo.Response = new ExecutionDatabaseResponse
                        {
                            Success = false,
                            Messages = new List<string> { "Execution cancelled by user." }
                        };
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

                        var done = Interlocked.Increment(ref completed);
                        var dbName = databaseInfo.Database.DatabaseName;
                        RaiseOnUI(() => Log?.Invoke($"[Exec] {scriptName}: ({done}/{totalDbs}) {dbName} => ERROR: {exScript.Message}"));
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

            foreach (DataTable table in scriptInfo.DataSet.Tables)
            {
                var t = table;
                SendOnUI(() => t.EndLoadData());
            }

            var hasCancelled = scriptInfo.DatabasesInfo.Any(di => di.Status == ExecutionStatus.Cancelled);
            var hasError = scriptInfo.DatabasesInfo.Any(di => di.Status == ExecutionStatus.Error);

            if (hasCancelled && !hasError)
                scriptInfo.Status = ExecutionStatus.Cancelled;
            else if (hasError)
                scriptInfo.Status = ExecutionStatus.Error;
            else
                scriptInfo.Status = ExecutionStatus.Success;

            swExec.Stop();
            var successCount = scriptInfo.DatabasesInfo.Count(di => di.Status == ExecutionStatus.Success);
            var errorCount = scriptInfo.DatabasesInfo.Count(di => di.Status == ExecutionStatus.Error);
            var cancelledCount = scriptInfo.DatabasesInfo.Count(di => di.Status == ExecutionStatus.Cancelled);
            RaiseOnUI(() => Log?.Invoke($"[Exec] {scriptName}: completed in {swExec.Elapsed.TotalSeconds:F1}s — {successCount} success, {errorCount} error, {cancelledCount} cancelled"));

            progress?.Report(new ExecutionProgress(scriptInfo, null));

        }
        private async Task<ExecutionDatabaseResponse> InternalExecuteAsync(ExecutionScriptInfo scriptInfo, ExecutionDatabaseInfo databaseInfo, CancellationToken cancellationToken = default)
        {
            var scriptResponse = new ExecutionDatabaseResponse();

            var database = databaseInfo.Database;

            var connectionModel = _connections.FirstOrDefault(c => c.Name == database.ConnectionName)

                ?? throw new Exception("connection notfound");

            var script = scriptInfo.Script;

            string content = script.Content ?? await File.ReadAllTextAsync(script.FilePath, cancellationToken);

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
                foreach (SqlError err in e.Errors)
                {
                    var msg = $"[{database.DatabaseName}] {err.Message}";
                    scriptResponse.Messages.Add(msg);
                    RaiseOnUI(() => InfoMessageRecived?.Invoke(scriptInfo, databaseInfo, msg));
                }
            };

            await sqlConnection.OpenAsync(cancellationToken);

            using var transaction = sqlConnection.BeginTransaction();

            try
            {
                for (int i = 0; i < batches.Count; i++)
                {
                    string batch = batches[i];
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    cancellationToken.ThrowIfCancellationRequested();

                    //Log($"{script.Name}|{databaseConnectionInfo}|batch {i + 1}/{batches.Count} => Executing batch");

                    await using var cmd = sqlConnection.CreateCommand();
                    cmd.UpdatedRowSource = UpdateRowSource.None;
                    cmd.Transaction = transaction;
                    cmd.CommandText = batch;
                    cmd.CommandTimeout = _commandTimeoutSeconds;
                    cmd.StatementCompleted += (s, e) =>
                    {
                        if (e.RecordCount >= 0)
                        {
                            var msg = $"[{database.DatabaseName}] {e.RecordCount} linha(s) afetada(s)";
                            scriptResponse.Messages.Add(msg);
                            RaiseOnUI(() => InfoMessageRecived?.Invoke(scriptInfo, databaseInfo, msg));
                        }
                    };
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    int resultIndex = 0;

                    do
                    {
                        var schema = reader.GetColumnSchema();
                        var tableName = $"Batch{i + 1}_Result{++resultIndex}";

                        DataTable table;
                        bool isNewTable = false;

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

                                    if (table.Columns.Contains(colName))
                                        colName = $"{colName}_{c + 1}";

                                    table.Columns.Add(colName, col.DataType ?? typeof(string));
                                }

                                scriptInfo.DataSet.Tables.Add(table);
                                table.BeginLoadData();
                                isNewTable = true;
                            }
                        }

                        if (isNewTable)
                        {
                            SendOnUI(() => TableAdded?.Invoke(scriptInfo, databaseInfo, table));
                        }

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            // Lê valores do reader em buffer local (thread-safe, sem tocar no DataTable)
                            var values = new object[schema.Count];
                            for (int c = 0; c < schema.Count; c++)
                            {
                                values[c] = reader.GetValue(c);
                            }

                            // Todas as mutações no DataTable dentro do lock
                            DataRow row;
                            lock (scriptInfo.DataSet)
                            {
                                row = table.NewRow();
                                row[0] = database.DatabaseName;
                                row[1] = database.ConnectionName;

                                for (int c = 0; c < values.Length; c++)
                                {
                                    row[c + 2] = values[c];
                                }

                                table.Rows.Add(row);
                            }

                            RaiseOnUI(() => RowAdded?.Invoke(scriptInfo, databaseInfo, table, row));
                        }

                    } while (await reader.NextResultAsync(cancellationToken));
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
                    scriptResponse.Messages.Add($"[{database.DatabaseName}][{database.ConnectionName}] Rollback failed: {rbEx.Message}");
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

        private void SendOnUI(Action action)
        {
            if (UiContext == null)
                action();
            else
                UiContext.Send(_ => action(), null);
        }
    }

}