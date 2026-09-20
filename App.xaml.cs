using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortDetective.Interfaces;
using PortDetective.Services;
using PortDetective.ViewModels;
using PortDetective.Views;
using Serilog;

namespace PortDetective
{
    public partial class App : Application
    {
        private static IServiceProvider? _serviceProvider;

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized");
            return _serviceProvider.GetRequiredService<T>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ----------------------------------------------------------------
            // Configure Serilog
            // ----------------------------------------------------------------
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PortDetective", "Logs");
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(logDir, "portdetective-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Port Detective starting up");

            // ----------------------------------------------------------------
            // DI container
            // ----------------------------------------------------------------
            var services = new ServiceCollection();

            services.AddLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddSerilog(dispose: true);
            });

            // Services (singletons shared across the app lifetime)
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IPortScannerService, PortScannerService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<IPortMonitorService>(sp =>
            {
                var settings = sp.GetRequiredService<ISettingsService>();
                settings.Load();
                return new PortMonitorService(
                    sp.GetRequiredService<IPortScannerService>(),
                    sp.GetRequiredService<ILogger<PortMonitorService>>(),
                    TimeSpan.FromSeconds(settings.Current.AutoRefreshIntervalSeconds));
            });

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Views (transient so a new instance is created each time settings opens)
            services.AddSingleton<MainWindow>();
            services.AddTransient<SettingsWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // ----------------------------------------------------------------
            // Launch
            // ----------------------------------------------------------------
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            if (settingsService.Current.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.ShowInTaskbar = true;
            }

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Port Detective shutting down");
            Log.CloseAndFlush();

            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            base.OnExit(e);
        }
    }
}
