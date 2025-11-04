using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQLMultiScript.Services
{
    public class ExecutionService : IExecutionService
    {
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
        private async Task<ExecutionDatabaseResponse> InternalExecuteAsync(Database database, Script script)
        {
            var scriptResponse = new ExecutionDatabaseResponse();


            var connectionModel = _connections.FirstOrDefault(c => c.Name == database.ConnectionName);

            if (connectionModel == null)
            {
                throw new Exception("connection notfound");
            }

            string content = script.Content ?? await File.ReadAllTextAsync(script.FilePath);

            string connectionString = _connectionService.BuildConnectionString(connectionModel, database.DatabaseName);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString não pode ser vazio.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("script não pode ser vazio.", nameof(script));

            var resultDataSet = new DataSet();

            var batches = SplitBatches(content).ToList();

            _logger.LogInformation(string.Format("Executando script em {0}. Batches: {1}", HiddenConnectionInfo(connectionString), batches.Count));

            await using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.InfoMessage += (s, e) =>
            {

                scriptResponse.Messages.Add(e.Message);

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

                    _logger.LogInformation(string.Format("Executando batch {0}/{1} (tamanho: {2} chars)", i + 1, batches.Count, batch.Length));

                    await using var cmd = sqlConnection.CreateCommand();
                    cmd.UpdatedRowSource = UpdateRowSource.None;
                    cmd.Transaction = transaction;
                    cmd.CommandText = batch;
                    cmd.StatementCompleted += (s, e) =>
                    {
                        // Captura mensagens do tipo "(X rows affected)"
                        if (e.RecordCount >= 0)
                            scriptResponse.Messages.Add($"{e.RecordCount} linha(s) afetada(s)");
                    };
                    cmd.CommandTimeout = _commandTimeoutSeconds;

                    using var reader = await cmd.ExecuteReaderAsync();

                    do
                    {
                        var table = new DataTable($"Batch{i + 1}_Result{resultDataSet.Tables.Count + 1}");
                        table.Load(reader);

                        table.Columns.Add("ConnectionName", typeof(string));
                        table.Columns.Add("DatabaseName", typeof(string));
                        table.Columns["ConnectionName"].SetOrdinal(1);
                        table.Columns["DatabaseName"].SetOrdinal(0);

                        foreach (DataRow row in table.Rows)
                        {
                            row["ConnectionName"] = database.ConnectionName;
                            row["DatabaseName"] = database.DatabaseName;
                        }

                        resultDataSet.Tables.Add(table);
                    } while (!reader.IsClosed && await reader.NextResultAsync());
                }

                transaction.Commit();

                scriptResponse.Success = true;

                _logger.LogInformation(string.Format("Script executado com sucesso em {0}. {1} resultados obtidos.",
                    HiddenConnectionInfo(connectionString), resultDataSet.Tables.Count));
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Erro ao executar script em {0}. Fazendo rollback." + Environment.NewLine + "{1}", HiddenConnectionInfo(connectionString), ex.Message));

                scriptResponse.Success = false;

                while (ex != null)
                {
                    scriptResponse.Messages.Add(ex.Message);
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

            scriptResponse.DataSet = resultDataSet;
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

        public async Task ExecuteAsync(Execution execution, Action<Execution, ExecutionScriptInfo, ExecutionDatabaseInfo> statusUpdated)
        {

            // Semaphore para limitar o número de execuções simultâneas
            using var semaphore = new SemaphoreSlim(1); // 5 threads paralelas


            execution.Status = ExecutionStatus.Executing;

            statusUpdated(execution, null, null);


            foreach (var scriptInfo in execution.ScriptsInfo)
            {

                scriptInfo.Status = ExecutionStatus.Executing;

                statusUpdated(execution, scriptInfo, null);

                var script = scriptInfo.Script;
                
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

                            statusUpdated(execution, scriptInfo, databaseInfo);


                            //logAction($"Executando script '{script.Name}' em '{db.DatabaseName}'...", false);

                            var scriptResponse = await InternalExecuteAsync(db, script);


                            if (scriptResponse.Success)
                            {
                                databaseInfo.Status = ExecutionStatus.Success;
                            }
                            else
                            {
                                databaseInfo.Status = ExecutionStatus.Error;
                            }


                            databaseInfo.Response = scriptResponse;

                            statusUpdated(execution, scriptInfo, databaseInfo);



                            

                        }
                        catch (Exception exScript)
                        {
                            scriptInfo.Status = ExecutionStatus.Error;

                            _logger.LogError($"Erro ao executar script '{script.Name}' no banco '{db.DatabaseName}': {exScript.Message}");
                        }
                        finally
                        {


                            semaphore.Release();
                        }
                    }));


                   
                }

                await Task.WhenAll(tasks);

                //UPDATE ScriptInfo
                var scriptInfoError = scriptInfo.DatabasesInfo.Any(di => di.Status == ExecutionStatus.Error);

                scriptInfo.Status = scriptInfoError ? ExecutionStatus.Error : ExecutionStatus.Success;

                statusUpdated(execution, scriptInfo, null);


            }


            var executionError = execution.ScriptsInfo.Any(si => si.Status == ExecutionStatus.Error);

            execution.Status = executionError ? ExecutionStatus.Error : ExecutionStatus.Success;

            statusUpdated(execution, null, null);

        }
    }

}