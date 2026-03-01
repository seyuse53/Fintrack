using System.Windows;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database Configuration
            services.AddDbContext<AppDbContext>();

            // Register MainWindow
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize SQLitePCL for SQLCipher
            SQLitePCL.Batteries_V2.Init();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                // NOW we have the ActiveDataKey (Master Key).
                // 1. Ensure the database file is encrypted with this key (handles migration from plain DB)
                var dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
                var masterKey = FinTrack.Core.Services.SettingsManager.ActiveDataKey;
                
                if (!string.IsNullOrEmpty(masterKey))
                {
                    FinTrack.Core.Services.EncryptionService.EnsureDatabaseEncryption(dbPath, masterKey);
                }

                // 2. Ensure database is created and migrated
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    context.Database.Migrate();
                }

                ShutdownMode = ShutdownMode.OnLastWindowClose;
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            else
            {
                // Unsuccessful login or window closed
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Execute the Auto-Backup process right before the application completely shuts down.
            // This is safe because SQLCipher and EntityFramework connections are likely closed or 
            // the OS will allow read-only file copy operations.
            FinTrack.Core.Services.BackupService.PerformBackup();
            
            base.OnExit(e);
        }
    }
}
