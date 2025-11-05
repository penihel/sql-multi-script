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

            // Register services
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IDatabaseDistributionListService, DatabaseDistributionListService>();
            services.AddSingleton<IConnectionService, ConnectionService>();
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IExecutionService, ExecutionService>();


            // Register ILogger
            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<ILoggerFactory>();
                return factory.CreateLogger(Constants.ApplicationName);
            });

            // Register Forms
            services.AddTransient<MainForm>();
            services.AddTransient<DatabaseDistributionListForm>();
            services.AddTransient<NewConnectionForm>();

            using (var provider = services.BuildServiceProvider())
            {
                ApplicationConfiguration.Initialize();

                
                var logger = provider.GetRequiredService<ILogger>();

                logger.LogInformation("Application iniciada");

                Application.ThreadException += (s, e) =>
                {
                    logger.LogError(e.Exception, e.Exception.Message);
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                        logger.LogCritical(ex, ex.Message);
                };

                var mainForm = provider.GetRequiredService<MainForm>();

                Application.Run(mainForm);
            }
        }
    }
}
