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
        private readonly ILogger _logger;
        private readonly int _commandTimeoutSeconds;

        public ScriptExecutorService(ILogger logger, IConnectionService connectionService, int commandTimeoutSeconds = 600)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandTimeoutSeconds = commandTimeoutSeconds;
            _connectionService = connectionService;
        }

        public async Task<DataSet> ExecuteAsync(Database database, Script script)
        {

            string content = script.Content ?? await File.ReadAllTextAsync(script.FilePath);
            
            string connectionString = _connectionService.BuildConnectionString(database.Connection);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString não pode ser vazio.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("script não pode ser vazio.", nameof(script));

            var resultDataSet = new DataSet();
            var batches = SplitBatches(content).ToList();

            _logger.LogInformation("Executando script em {Conn}. Batches: {Count}", HiddenConnectionInfo(connectionString), batches.Count);

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

                    _logger.LogInformation("Executando batch {Index}/{Total} (tamanho: {Length} chars)", i + 1, batches.Count, batch.Length);

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
                _logger.LogInformation("Script executado com sucesso em {Conn}. {Tables} resultados obtidos.",
                    HiddenConnectionInfo(connectionString), resultDataSet.Tables.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar script em {Conn}. Fazendo rollback.", HiddenConnectionInfo(connectionString));
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rbEx)
                {
                    _logger.LogWarning(rbEx, "Rollback falhou.");
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