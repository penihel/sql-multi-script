using Microsoft.Data.SqlClient;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;
using SQLMultiScript.Core.Models.Files;
using SQLMultiScript.Resources;

namespace SQLMultiScript.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly IPathService _pathService;

        public ConnectionService(IPathService pathService)
        {
            _pathService = pathService;
        }

        public async Task<IList<Connection>> ListAsync()
        {
            var directoryPath = _pathService.GetConnectionsPath();

            var retorno = new List<Connection>();

            if (Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {

                    if (File.Exists(file))
                    {

                        var json = await File.ReadAllTextAsync(file);
                        var item = System.Text.Json.JsonSerializer.Deserialize<ConnectionFile>(json);
                        if (item != null && item.FileType == ConnectionFile.Type
                            && item.Connection != null)
                        {
                            item.Connection.FilePath = file;

                            retorno.Add(item.Connection);
                        }

                    }
                }
            }

            return retorno;
        }

        public async Task<IList<Database>> ListDatabasesAsync(Connection connection)
        {
            var databases = new List<Database>();

            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var connString = BuildConnectionString(connection);

            try
            {
                using var sqlConnection = new SqlConnection(connString);
                await sqlConnection.OpenAsync();

                // Consulta todos os bancos no servidor
                var command = new SqlCommand(
                    "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", // ignora system DBs
                    sqlConnection
                );

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var db = new Database
                    {
                        DatabaseName = reader.GetString(0),
                        ConnectionName = connection.Name,
                        Connection = connection,
                        Selected = true
                        
                    };
                    databases.Add(db);
                }
            }
            catch (Exception ex)
            {
                // Aqui você pode logar ou propagar a exceção
                throw new InvalidOperationException($"Erro ao listar bancos do servidor {connection.Name}", ex);
            }

            return databases;
        }

        public async Task<Result> SaveAsync(Connection connection)
        {

            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(connection.Name))
            {
                connection.Name = connection.Server;
            }

            var list = await ListAsync();

            if (list.Any(d => d.Name == connection.Name))
            {
                return Result.Fail(Strings.RecordAlreadyExists);
            }


            if (string.IsNullOrEmpty(connection.FilePath))
            {

                var fileName = _pathService.GetNewValidJsonFileName(_pathService.GetConnectionsPath(), connection.Name);

                

                connection.FilePath = fileName;
            }

            var connectionFile = new ConnectionFile()
            {
                Connection = connection,
            };

            var json = System.Text.Json.JsonSerializer.Serialize(connectionFile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });


            await File.WriteAllTextAsync(connection.FilePath, json);

            return Result.Ok();

        }

        public async Task TestAsync(Connection connection)
        {
            var connString = BuildConnectionString(connection);

            using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await conn.CloseAsync();

        }

        public string BuildConnectionString(Connection connection)
        {
            string server = connection.Server.Trim();

            string auth = connection.Auth.Trim();

            var username = connection.UserName.Trim();

            var password = connection.Password?.Trim();

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "master",
                //ConnectTimeout = 5
            };

            switch (auth)
            {
                case Constants.WindowsAuthentication:
                    builder.IntegratedSecurity = true;
                    break;
                case Constants.SQLServerAuthentication:
                    builder.UserID = username;
                    builder.Password = password;
                    builder.IntegratedSecurity = false;
                    break;
                case Constants.MicrosoftEntraMFA:
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                    if (!string.IsNullOrWhiteSpace(username))
                        builder.UserID = username;
                    break;
                case Constants.MicrosoftEntraIntegrated:
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                    break;
                case Constants.MicrosoftEntraPassword:
                    builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                    builder.UserID = username;
                    builder.Password = password;
                    break;
            }

            return builder.ConnectionString;
        }
    }
}
