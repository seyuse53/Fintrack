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

            // Setup global exception handling for debugging silent crashes
            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Unhandled Exception:\n\n{args.Exception.Message}\n\nStack:\n{args.Exception.StackTrace}", "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true; // Prevent automatic shutdown to see the message
                Application.Current.Shutdown();
            };

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

                // Clear all connection pools to ensure fresh connections with the correct password
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                // 2. Ensure database is created and migrated
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    try
                    {
                        context.Database.Migrate();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 26)
                    {
                        // "file is not a database" — the ActiveDataKey might not match the encryption key
                        System.Diagnostics.Debug.WriteLine($"Migration failed (Error 26): ActiveDataKey might be incorrect. Path: {dbPath}");
                        
                        var errorWin = new FinTrack.WPF.Views.ErrorDialogWindow(
                            "Veritabanı Erişim Hatası",
                            "Veritabanına erişilemiyor.\nBu genellikle şifreleme anahtarının veritabanı ile eşleşmediği anlamına gelir.\n\nLütfen doğru şifreyi girdiğinizden emin olun veya uygulamayı yeniden başlatın.",
                            $"SQLite Error 26: file is not a database"
                        );
                        errorWin.ShowDialog();
                        
                        Current.Shutdown();
                        return;
                    }

                    // Perform custom data migration for Opening Balances
                    AppDbContext.MigrateInitialBalances(context);
                }

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                ShutdownMode = ShutdownMode.OnLastWindowClose;
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
