using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLMultiScript.Core;
using SQLMultiScript.Core.Interfaces;
using SQLMultiScript.Services;
using SQLMultiScript.UI.Forms;

namespace SQLMultiScript.UI
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            // Registrar serviços
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IDatabaseDistributionListService, DatabaseDistributionListService>();
            services.AddSingleton<IConnectionService, ConnectionService>();
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IScriptExecutorService, ScriptExecutorService>();
            

            // Registrar um ILogger genérico com categoria "SQLMultiScript"
            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<ILoggerFactory>();
                return factory.CreateLogger(Constants.ApplicationName);
            });

            // Registrar Forms (agora recebem ILogger injetado)
            services.AddTransient<MainForm>();
            services.AddTransient<DatabaseDistributionListForm>();
            services.AddTransient<NewConnectionForm>();

            using (var provider = services.BuildServiceProvider())
            {
                ApplicationConfiguration.Initialize();

                // Pega o logger diretamente
                var logger = provider.GetRequiredService<ILogger>();

                logger.LogInformation("Aplicação iniciada");

                Application.ThreadException += (s, e) =>
                {
                    logger.LogError(e.Exception, "Erro não tratado no thread da UI");
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                        logger.LogCritical(ex, "Erro fatal");
                };

                var mainForm = provider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
        }
    }
}
