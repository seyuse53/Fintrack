using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

public partial class MainAppViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _viewTitle = "Ana Ekran (Özet)";

    [ObservableProperty]
    private bool _isDateFilterVisible = true;

    [ObservableProperty]
    private ObservableCollection<string> _months = new();

    [ObservableProperty]
    private int _selectedMonthIndex;

    [ObservableProperty]
    private ObservableCollection<int> _years = new();

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public MainAppViewModel()
    {
        CurrentView = new DashboardViewModel();
        SetupInitialFilters();
    }

    private void SetupInitialFilters()
    {
        var culture = new System.Globalization.CultureInfo("tr-TR");
        foreach (var month in culture.DateTimeFormat.MonthNames)
        {
            if (!string.IsNullOrEmpty(month))
                Months.Add(char.ToUpper(month[0]) + month.Substring(1));
        }

        int currentYear = System.DateTime.Now.Year;
        for (int i = currentYear - 5; i < currentYear + 5; i++)
        {
            Years.Add(i);
        }

        SelectedMonthIndex = System.DateTime.Now.Month - 1;
        SelectedYear = currentYear;
    }

    [RelayCommand]
    private void SwitchToDashboard()
    {
        if (CurrentView is not DashboardViewModel)
        {
            var dashboard = new DashboardViewModel();
            CurrentView = dashboard;
            ViewTitle = "Ana Ekran (Özet)";
            IsDateFilterVisible = true;
            _ = dashboard.LoadDataAsync(SelectedYear, SelectedMonthIndex + 1);
        }
    }

    [RelayCommand]
    private void SwitchToAccounts()
    {
        if (CurrentView is not AccountsViewModel)
        {
            CurrentView = new AccountsViewModel();
            ViewTitle = "Hesaplarım";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToCards()
    {
        if (CurrentView is not CardsViewModel)
        {
            CurrentView = new CardsViewModel();
            ViewTitle = "Kartlarım";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToInvestments()
    {
        if (CurrentView is not InvestmentsViewModel)
        {
            CurrentView = new InvestmentsViewModel();
            ViewTitle = "Yatırımlar";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToBes()
    {
        if (CurrentView is not BesViewModel)
        {
            CurrentView = new BesViewModel();
            ViewTitle = "Bireysel Emeklilik (BES)";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToBudget()
    {
        if (CurrentView is not BudgetViewModel)
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db != null) CurrentView = new BudgetViewModel(db);
            ViewTitle = "Bütçe Yönetimi";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToReports()
    {
        if (CurrentView is not ReportsViewModel)
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db != null) CurrentView = new ReportsViewModel(db);
            ViewTitle = "Raporlar ve Analiz";
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToSettings()
    {
        if (CurrentView is not SettingsViewModel)
        {
            CurrentView = new SettingsViewModel();
            ViewTitle = "Ayarlar";
            IsDateFilterVisible = false;
        }
    }

    public async System.Threading.Tasks.Task RefreshCurrentViewAsync()
    {
        if (CurrentView is DashboardViewModel dashboard)
        {
            await dashboard.LoadDataAsync(SelectedYear, SelectedMonthIndex + 1);
        }
        else if (CurrentView is InvestmentsViewModel investments)
        {
            await investments.LoadDataAsync();
        }
        else if (CurrentView is AccountsViewModel accounts)
        {
            await accounts.LoadDataAsync();
        }
        else if (CurrentView is CardsViewModel cards)
        {
            await cards.LoadDataAsync();
        }
    }

    partial void OnSelectedMonthIndexChanged(int value)
    {
        _ = RefreshCurrentViewAsync();
    }

    partial void OnSelectedYearChanged(int value)
    {
        _ = RefreshCurrentViewAsync();
    }
}
