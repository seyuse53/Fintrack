using Avalonia.Controls;

namespace FinTrack.Avalonia.Views;

public partial class BesView : UserControl
{
    public BesView()
    {
        InitializeComponent();
    }

    private async void ManageBesButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dbContext = App.Services?.GetService(typeof(FinTrack.Data.AppDbContext)) as FinTrack.Data.AppDbContext;
        if (dbContext == null) return;

        if (DataContext is ViewModels.BesViewModel vm)
        {
            var app = global::Avalonia.Application.Current;
            if (app?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new ManageInvestmentsWindow(dbContext);
                await window.ShowDialog(desktop.MainWindow!);
                
                await vm.LoadDataAsync();
            }
        }
    }
}
