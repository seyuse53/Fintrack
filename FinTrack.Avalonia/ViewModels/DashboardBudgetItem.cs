using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Avalonia.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FinTrack.Avalonia.ViewModels;

public class BudgetDetailItem
{
    public string DateStr { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string FormattedAmount { get; set; } = "";
}

public partial class DashboardBudgetItem : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<BudgetDetailItem> _details = new();
    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private decimal _plannedAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActualAmountDisplay))]
    [NotifyPropertyChangedFor(nameof(ActualForeground))]
    private decimal _actualAmount;

    public string PlannedAmountDisplay => PlannedAmount == 0 ? "₺0,00" : $"₺{PlannedAmount:N2}";
    public string ActualAmountDisplay => $"₺{ActualAmount:N2}";

    public string ActualForeground => ActualAmount > PlannedAmount && PlannedAmount > 0 ? "#E74C3C" : "#333333";

    [RelayCommand]
    private async Task ShowDetailsAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new CategoryDetailWindow(this);
            await window.ShowDialog(desktop.MainWindow);
        }
    }
}
