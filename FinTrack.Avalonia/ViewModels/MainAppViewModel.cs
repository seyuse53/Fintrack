using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using FinTrack.Data;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Avalonia.Localization;
using System.ComponentModel;

namespace FinTrack.Avalonia.ViewModels;

public partial class MainAppViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _viewTitle = string.Empty;

    private string _currentViewKey = "Main_Title_Dashboard";

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
        _currentViewKey = "Main_Title_Dashboard";
        
        SetupInitialFilters();
        UpdateViewTitle();

        LocalizationService.Instance.PropertyChanged += OnLocalizationPropertyChanged;
    }

    private void OnLocalizationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.LanguageVersion))
        {
            UpdateViewTitle();
            UpdateFiltersLanguage();
        }
    }

    private void UpdateViewTitle()
    {
        ViewTitle = LocalizationService.GetString(_currentViewKey);
    }

    private void UpdateFiltersLanguage()
    {
        var lang = FinTrack.Core.Services.SettingsManager.GetLanguagePreference();
        var culture = new System.Globalization.CultureInfo(lang == "en" ? "en-US" : "tr-TR");
        
        int prevSelected = SelectedMonthIndex;
        Months.Clear();
        foreach (var month in culture.DateTimeFormat.MonthNames)
        {
            if (!string.IsNullOrEmpty(month))
                Months.Add(char.ToUpper(month[0]) + month.Substring(1));
        }
        if (prevSelected >= 0 && prevSelected < Months.Count)
            SelectedMonthIndex = prevSelected;
    }

    private void SetupInitialFilters()
    {
        UpdateFiltersLanguage();

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
            (CurrentView as IDisposable)?.Dispose();
            var dashboard = new DashboardViewModel();
            CurrentView = dashboard;
            _currentViewKey = "Main_Title_Dashboard";
            UpdateViewTitle();
            IsDateFilterVisible = true;
            _ = dashboard.LoadDataAsync(SelectedYear, SelectedMonthIndex + 1);
        }
    }

    [RelayCommand]
    private void SwitchToAccounts()
    {
        if (CurrentView is not AccountsViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var vm = new AccountsViewModel();
            CurrentView = vm;
            _currentViewKey = "Main_Title_Accounts";
            UpdateViewTitle();
            IsDateFilterVisible = false;
            _ = vm.LoadDataAsync();
        }
    }

    [RelayCommand]
    private void SwitchToCards()
    {
        if (CurrentView is not CardsViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var vm = new CardsViewModel();
            CurrentView = vm;
            _currentViewKey = "Main_Title_Cards";
            UpdateViewTitle();
            IsDateFilterVisible = false;
            _ = vm.LoadDataAsync();
        }
    }

    [RelayCommand]
    private void SwitchToInvestments()
    {
        if (CurrentView is not InvestmentsViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var vm = new InvestmentsViewModel();
            CurrentView = vm;
            _currentViewKey = "Main_Title_Investments";
            UpdateViewTitle();
            IsDateFilterVisible = false;
            _ = vm.LoadDataAsync();
        }
    }

    [RelayCommand]
    private void SwitchToBes()
    {
        if (CurrentView is not BesViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var vm = new BesViewModel();
            CurrentView = vm;
            _currentViewKey = "Main_Title_Bes";
            UpdateViewTitle();
            IsDateFilterVisible = false;
            _ = vm.InitializeAsync();
        }
    }

    [RelayCommand]
    private void SwitchToBudget()
    {
        if (CurrentView is not BudgetViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var db = AppDbContext.CreateNew();
            if (db != null) CurrentView = new BudgetViewModel(db);
            _currentViewKey = "Main_Title_Budget";
            UpdateViewTitle();
            IsDateFilterVisible = false;
        }
    }

    [RelayCommand]
    private void SwitchToReports()
    {
        if (CurrentView is not ReportsViewModel)
        {
            (CurrentView as IDisposable)?.Dispose();
            var db = AppDbContext.CreateNew();
            if (db != null) CurrentView = new ReportsViewModel(db);
            _currentViewKey = "Main_Title_Reports";
            UpdateViewTitle();
            IsDateFilterVisible = true;
        }
    }

    [RelayCommand]
    private void SwitchToSettings()
    {
        if (CurrentView is not SettingsViewModel)
        {
            var vm = new SettingsViewModel();
            CurrentView = vm;
            _currentViewKey = "Main_Title_Settings";
            UpdateViewTitle();
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
