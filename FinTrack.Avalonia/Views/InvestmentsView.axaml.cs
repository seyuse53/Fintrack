using Avalonia.Controls;

namespace FinTrack.Avalonia.Views;

public partial class InvestmentsView : UserControl
{
    public InvestmentsView()
    {
        InitializeComponent();
    }

    private async void ManageInvestmentsButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dbContext = App.Services?.GetService(typeof(FinTrack.Data.AppDbContext)) as FinTrack.Data.AppDbContext;
        if (dbContext == null) return;

        var window = new ManageInvestmentsWindow(dbContext);
        
        var parentWindow = (this.VisualRoot as global::Avalonia.Controls.Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (parentWindow != null)
        {
            await window.ShowDialog<bool>(parentWindow);
            
            // Refresh data when returning from the manage window
            if (DataContext is FinTrack.Avalonia.ViewModels.InvestmentsViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }
    }
}
