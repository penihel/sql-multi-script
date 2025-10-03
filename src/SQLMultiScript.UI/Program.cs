using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Services;

namespace SQLMultiScript.UI
{
    public static class Program
    {
        private static ILogger _logger;
        private const string SQLMultiScript = "SQLMultiScript";
        private const string ApplicationStateFileName = "ApplicationState.json";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Configura o LoggerFactory
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole(); // escreve no console

            });

            _logger = loggerFactory.CreateLogger(SQLMultiScript);

            // Captura erros não tratados no UI thread
            Application.ThreadException += (s, e) =>
            {
                _logger.LogError(e.Exception, "Erro não tratado no thread da UI");
            };

            // Captura erros fatais (AppDomain)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    _logger.LogCritical(ex, "Erro fatal");
            };


            var services = new ServiceCollection();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IDatabaseDistributionListService, DatabaseDistributionListService>();
            

            using (var provider = services.BuildServiceProvider())
            {
                ApplicationConfiguration.Initialize();

                _logger.LogInformation("Aplicação iniciada");

                var projectService = provider.GetRequiredService<IProjectService>();
                var databaseDistributionListService = provider.GetRequiredService<IDatabaseDistributionListService>();



                Application.Run(new MainForm(_logger, projectService, databaseDistributionListService));
            }


        }

        
    }
}