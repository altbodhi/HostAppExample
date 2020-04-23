using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows.Forms;

namespace host
{
    public class MainForm : Form
    {
        public MainForm(Action close)
        {
            this.FormClosed += (o, e) => close();
            var button = new Button();
            Controls.Add(button);
            button.Text = "OK";
            button.Click += (o, e) =>
            {
                Text = DateTime.Now.ToString("s");
                new Form().Show();
            };
        }
    }

    public class App : IHostedService
    {
        private readonly ILogger<App> logger;
        private readonly IHostApplicationLifetime appLifetime;
        public App(ILogger<App> logger, IHostApplicationLifetime appLifetime)
        {
            this.logger = logger;
            this.appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogCritical("StartAsync...");
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("{date}", DateTime.Now);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }, cancellationToken);
            
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Task.Run(() => Application.Run(new MainForm(() => appLifetime.StopApplication())));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogError("StopAsync...");
            return Task.CompletedTask;
        }
    }

    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs\\app.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<App>();
                }).ConfigureLogging(c =>
                {
                    c.ClearProviders();
                    c.AddSerilog();
                });//.UseConsoleLifetime();
    }
}
