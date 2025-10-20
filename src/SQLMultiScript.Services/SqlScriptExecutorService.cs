using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace SQLMultiScript.Services
{
    public class ScriptExecutorService : IScriptExecutorService
    {
        private readonly IConnectionService _connectionService;
        
        private readonly int _commandTimeoutSeconds;

        private ICollection<Connection> _connections;
        public ScriptExecutorService(IConnectionService connectionService, int commandTimeoutSeconds = 600)
        {
            
            _commandTimeoutSeconds = commandTimeoutSeconds;
            _connectionService = connectionService;
        }

        public async Task LoadConnectionsAsync()
        {

            _connections = await _connectionService.ListAsync();
        }
        public async Task<DataSet> ExecuteAsync(Database database, Script script, Action<string, bool> logAction)
        {



            var connectionObj = _connections.FirstOrDefault(c => c.Name == database.ConnectionName);

            if (connectionObj == null)
                throw new Exception("connection notfound");

            string content = script.Content ?? await File.ReadAllTextAsync(script.FilePath);

            string connectionString = _connectionService.BuildConnectionString(connectionObj);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString não pode ser vazio.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("script não pode ser vazio.", nameof(script));

            var resultDataSet = new DataSet();
            var batches = SplitBatches(content).ToList();

            logAction(string.Format("Executando script em {0}. Batches: {1}", HiddenConnectionInfo(connectionString), batches.Count), false);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                for (int i = 0; i < batches.Count; i++)
                {
                    string batch = batches[i];
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    logAction(string.Format("Executando batch {0}/{1} (tamanho: {2} chars)", i + 1, batches.Count, batch.Length), false);

                    await using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = batch;
                    cmd.CommandTimeout = _commandTimeoutSeconds;

                    using var reader = await cmd.ExecuteReaderAsync();

                    do
                    {
                        var table = new DataTable($"Batch{i + 1}_Result{resultDataSet.Tables.Count + 1}");
                        table.Load(reader);
                        resultDataSet.Tables.Add(table);
                    } while (!reader.IsClosed && await reader.NextResultAsync());
                }

                transaction.Commit();
                logAction( string.Format("Script executado com sucesso em {0}. {1} resultados obtidos.",
                    HiddenConnectionInfo(connectionString), resultDataSet.Tables.Count), false);
            }
            catch (Exception ex)
            {
                logAction(string.Format("Erro ao executar script em {0}. Fazendo rollback." + Environment.NewLine + "{1}", HiddenConnectionInfo(connectionString), ex.Message), true);
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rbEx)
                {
                    logAction(rbEx.Message, true);
                }
                throw;
            }

            return resultDataSet;
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