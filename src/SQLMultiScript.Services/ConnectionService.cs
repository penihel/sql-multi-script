using Microsoft.Data.SqlClient;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Core.Models;

namespace SQLMultiScript.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly IPathService _pathService;

        public ConnectionService(IPathService pathService)
        {
            _pathService = pathService;
        }
        public async Task SaveAsync(Connection connection)
        {

            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(connection.DisplayName))
            {
                connection.DisplayName = connection.Server;
            }

            if (string.IsNullOrEmpty(connection.FilePath))
            {
                var fileName = connection.DisplayName + ".json";


                connection.FilePath = Path.Combine(_pathService.GetConnectionsPath(), fileName);
            }


            var json = System.Text.Json.JsonSerializer.Serialize(connection, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });


            await File.WriteAllTextAsync(connection.FilePath, json);

        }

        public string BuildConnectionString(Connection connection)
        {
            string server = connection.Server.Trim();

            string auth = connection.Auth.Trim();

            var username = connection.UserName.Trim();

            var password = connection.Password.Trim();

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
