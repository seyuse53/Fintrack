using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

using FinTrack.Avalonia.Views;

public partial class InvestmentItemViewModel : ObservableObject
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public required string Category { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageCost { get; set; }

    [ObservableProperty]
    private decimal _currentPrice;

    [ObservableProperty]
    private decimal _currentValue;

    [ObservableProperty]
    private decimal _profit;

    [ObservableProperty]
    private decimal _profitPercentage;

    [ObservableProperty]
    private string _portfolioWeight = "%0.0";

    [ObservableProperty]
    private System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint>? _sparklineData;
}

public partial class InvestmentsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    // Özet Kartları
    [ObservableProperty]
    private decimal _totalPortfolioValue;

    [ObservableProperty]
    private decimal _totalCostBasis;

    [ObservableProperty]
    private decimal _totalProfit;

    [ObservableProperty]
    private decimal _totalProfitPercentage;

    [ObservableProperty]
    private string _topPerformerText = "—";

    [ObservableProperty]
    private decimal _topPerformerProfitPercentage;

    // Filtreleme
    [ObservableProperty]
    private string _selectedCategory = "Tümü";

    public ObservableCollection<string> Categories { get; } = new() 
    { 
        "Tümü", "Altın", "Döviz", "Hisse Senedi", "Kripto Para", "Fon", "Diğer" 
    };

    private ObservableCollection<InvestmentItemViewModel> _allInvestments = new();

    [ObservableProperty]
    private ObservableCollection<InvestmentItemViewModel> _filteredInvestments = new();

    public InvestmentsViewModel()
    {
        _context = App.Services?.GetService<AppDbContext>();
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        if (_context == null) return;

        try
        {
            var assets = await _context.InvestmentAssets.ToListAsync();
            var tempList = new System.Collections.Generic.List<InvestmentItemViewModel>();

            foreach (var asset in assets)
            {
                decimal currentPrice = PricingService.GetCurrentPrice(asset.Symbol, asset.AverageCost);
                decimal currentValue = currentPrice * asset.TotalAmount;
                decimal costBasis = asset.AverageCost * asset.TotalAmount;
                decimal profit = currentValue - costBasis;
                decimal profitPct = costBasis > 0 ? (profit / costBasis) * 100 : 0;

                tempList.Add(new InvestmentItemViewModel
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    Symbol = asset.Symbol,
                    Category = asset.Category ?? "Diğer",
                    TotalAmount = asset.TotalAmount,
                    AverageCost = asset.AverageCost,
                    CurrentPrice = currentPrice,
                    CurrentValue = currentValue,
                    Profit = profit,
                    ProfitPercentage = profitPct,
                    SparklineData = GenerateTrendData(asset.AverageCost, currentPrice)
                });
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                _allInvestments.Clear();
                foreach(var item in tempList) _allInvestments.Add(item);
                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Yatırımlar çekilemedi: {ex.Message}");
        }
    }

    private System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint> GenerateTrendData(decimal avgCost, decimal currentPrice)
    {
        var list = new System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint>();
        var random = new Random();
        decimal basePrice = avgCost > 0 ? avgCost : currentPrice;
        
        for(int i = 6; i >= 1; i--)
        {
            decimal step = basePrice * (decimal)(random.NextDouble() * 0.04 - 0.02); // +/- 2% random step
            basePrice += step;
            list.Add(new FinTrack.Avalonia.Controls.SparklinePoint { Date = DateTime.Now.AddDays(-i), Close = basePrice, Low = basePrice * 0.99m, High = basePrice * 1.01m });
        }
        
        // Add current price as the last point
        list.Add(new FinTrack.Avalonia.Controls.SparklinePoint { Date = DateTime.Now, Close = currentPrice, Low = currentPrice * 0.99m, High = currentPrice * 1.01m });
        return list;
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filteredList = SelectedCategory == "Tümü"
            ? _allInvestments.ToList()
            : _allInvestments.Where(x => x.Category == SelectedCategory).ToList();

        // Toplam değerleri hesapla (Sadece filtreye uygun olanların toplamı)
        decimal totalValue = filteredList.Sum(x => x.CurrentValue);
        decimal totalCost = filteredList.Sum(x => x.AverageCost * x.TotalAmount);
        
        TotalPortfolioValue = totalValue;
        TotalCostBasis = totalCost;
        TotalProfit = totalValue - totalCost;
        TotalProfitPercentage = totalCost > 0 ? (TotalProfit / totalCost) * 100 : 0;

        // En kârlı varlık
        var top = filteredList.Where(x => x.CurrentValue > 0).OrderByDescending(x => x.ProfitPercentage).FirstOrDefault();
        if (top != null)
        {
            TopPerformerText = $"{top.Name} ({top.Symbol})";
            TopPerformerProfitPercentage = top.ProfitPercentage;
        }
        else
        {
            TopPerformerText = "—";
            TopPerformerProfitPercentage = 0;
        }

        // Portföy ağırlığı güncellemesi
        foreach (var item in filteredList)
        {
            if (totalValue > 0)
            {
                item.PortfolioWeight = $"%{(item.CurrentValue / totalValue * 100):N1}";
            }
            else
            {
                item.PortfolioWeight = "%0.0";
            }
        }

        FilteredInvestments.Clear();
        foreach (var item in filteredList) FilteredInvestments.Add(item);
    }

    [RelayCommand]
    private void OpenTaxReport()
    {
        if (_context != null)
        {
            var taxWindow = new TaxCalculationWindow(_context);
            if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                taxWindow.ShowDialog(desktop.MainWindow);
            }
        }
    }
}
