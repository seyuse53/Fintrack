using Avalonia.Controls;

namespace FinTrack.Avalonia.Views;

public partial class MainAppView : UserControl
{
    public MainAppView()
    {
        InitializeComponent();
        this.Loaded += MainAppView_Loaded;
    }

    private async void MainAppView_Loaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Loaded -= MainAppView_Loaded;
        
        try
        {
            var updateService = new FinTrack.Core.Services.GitHubReleaseService();
            string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.0.0";
            
            var releaseInfo = await updateService.CheckForUpdatesAsync(currentVersion);
            if (releaseInfo.IsUpdateAvailable)
            {
                var parentWindow = (this.VisualRoot as global::Avalonia.Controls.Window) 
                    ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

                if (parentWindow != null)
                {
                    var updateWindow = new UpdateAvailableWindow();
                    updateWindow.LoadData(currentVersion, releaseInfo);
                    await updateWindow.ShowDialog(parentWindow);
                }
            }
        }
        catch
        {
            // Ignore update check errors silently
        }
    }

    private async void AddTransactionButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dbContext = App.Services?.GetService(typeof(FinTrack.Data.AppDbContext)) as FinTrack.Data.AppDbContext;
        if (dbContext == null) return;

        var window = new AddTransactionWindow(dbContext);
        
        // Find the parent MainWindow to serve as the owner
        var parentWindow = (this.VisualRoot as global::Avalonia.Controls.Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (parentWindow != null)
        {
            var result = await window.ShowDialog<bool>(parentWindow);
            if (result)
            {
                // Refresh Current View
                if (DataContext is FinTrack.Avalonia.ViewModels.MainAppViewModel mainVm)
                {
                    await mainVm.RefreshCurrentViewAsync();
                }
            }
        }
    }
}
