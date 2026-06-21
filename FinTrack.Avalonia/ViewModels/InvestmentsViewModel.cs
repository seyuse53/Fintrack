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

    // BES Özellikleri
    [ObservableProperty]
    private decimal _besTotalCostBasis;

    [ObservableProperty]
    private decimal _besGramGold;

    [ObservableProperty]
    private decimal _besStateContribution;

    // BES Sözleşme Detayları
    [ObservableProperty]
    private string? _besContractNo;

    [ObservableProperty]
    private DateTime? _besStartDate;

    [ObservableProperty]
    private DateTime? _besRetirementDate;

    [ObservableProperty]
    private string? _timeUntilRetirement;

    [ObservableProperty]
    private decimal _vestingPercentage;

    [ObservableProperty]
    private decimal _vestedStateContribution;

    [ObservableProperty]
    private decimal _currentTotalGramGold;

    [ObservableProperty]
    private decimal _goldProfitLoss;

    [ObservableProperty]
    private string? _besTimeInSystem;

    [ObservableProperty]
    private decimal _besMonthlyAverage;

    public string FormattedCurrentPrice
    {
        get
        {
            if (Category == "Kripto Para") return $"₺{CurrentPrice.ToString("0.########")}";
            return $"₺{CurrentPrice:N2}";
        }
    }

    public string FormattedAverageCost
    {
        get
        {
            if (Category == "Kripto Para") return $"₺{AverageCost.ToString("0.########")}";
            return $"₺{AverageCost:N2}";
        }
    }

    partial void OnCurrentPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(FormattedCurrentPrice));
        OnPropertyChanged(nameof(FormattedAverageCost));
    }
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
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadDataAsync();
        _ = FetchPricesAsync(); // Arka planda otomatik fiyat çek
    }

    public async Task LoadDataAsync()
    {
        if (_context == null) return;

        try
        {
            var assets = await _context.InvestmentAssets.Where(a => a.Category != "BES").ToListAsync();
            
            // Veritabanındaki hafızada tutulan son bilinen fiyatları yükle
            PricingService.LoadPricesFromAssets(assets);

            var allHistories = await _context.PriceHistories.OrderBy(h => h.Date).ToListAsync();
            var historiesBySymbol = allHistories.GroupBy(h => h.Symbol).ToDictionary(g => g.Key, g => g.ToList());

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
                    SparklineData = GenerateTrendData(asset.AverageCost, currentPrice, historiesBySymbol.ContainsKey(asset.Symbol) ? historiesBySymbol[asset.Symbol] : new System.Collections.Generic.List<PriceHistory>())
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

    public static System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint> GenerateTrendData(decimal avgCost, decimal currentPrice, System.Collections.Generic.List<PriceHistory> history)
    {
        var list = new System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint>();
        
        if (history != null && history.Count > 1)
        {
            // Geçmiş veriyi kullanarak grafiği çiz
            foreach (var h in history.TakeLast(30)) // Son 30 gün
            {
                list.Add(new FinTrack.Avalonia.Controls.SparklinePoint 
                { 
                    Date = h.Date, 
                    Close = h.ClosePrice, 
                    Low = h.LowPrice, 
                    High = h.HighPrice 
                });
            }
            // Güncel fiyatı da son nokta olarak ekle
            if (list.Last().Date < DateTime.Today)
            {
                list.Add(new FinTrack.Avalonia.Controls.SparklinePoint { Date = DateTime.Now, Close = currentPrice, Low = currentPrice * 0.99m, High = currentPrice * 1.01m });
            }
            else
            {
                list.Last().Close = currentPrice;
            }
        }
        else
        {
            // Yeterli geçmiş yoksa geçici çizgi oluştur
            var random = new Random();
            decimal basePrice = avgCost > 0 ? avgCost : currentPrice;
            
            for(int i = 6; i >= 1; i--)
            {
                decimal step = basePrice * (decimal)(random.NextDouble() * 0.04 - 0.02);
                basePrice += step;
                list.Add(new FinTrack.Avalonia.Controls.SparklinePoint { Date = DateTime.Now.AddDays(-i), Close = basePrice, Low = basePrice * 0.99m, High = basePrice * 1.01m });
            }
            list.Add(new FinTrack.Avalonia.Controls.SparklinePoint { Date = DateTime.Now, Close = currentPrice, Low = currentPrice * 0.99m, High = currentPrice * 1.01m });
        }

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
        foreach (var item in filteredList.OrderByDescending(x => x.CurrentValue)) FilteredInvestments.Add(item);
    }

    [RelayCommand]
    private async Task FetchPricesAsync()
    {
        if (_context == null || _allInvestments.Count == 0) return;

        try
        {
            // Iterate over all unique symbols and fetch their prices
            var symbolsToFetch = _allInvestments
                .Where(x => !string.IsNullOrWhiteSpace(x.Symbol))
                .Select(x => new { x.Symbol, x.Category })
                .Distinct()
                .ToList();

            foreach (var item in symbolsToFetch)
            {
                await PricingService.FetchPriceSmartAsync(item.Symbol, item.Category);
            }

            // Save fetched prices to the database
            var assets = await _context.InvestmentAssets.Where(a => a.Category != "BES").ToListAsync();
            PricingService.SavePricesToAssets(assets);
            
            // Record Price History for graphs
            var existingHistories = await _context.PriceHistories.Where(h => h.Date == DateTime.Today).ToListAsync();
            PricingService.RecordPriceHistory(assets, existingHistories, newRecord => _context.PriceHistories.Add(newRecord));

            await _context.SaveChangesAsync();

            // Refresh the screen with new prices
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fiyat çekme hatası: {ex.Message}");
        }
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

    [RelayCommand]
    private async Task ShowDetailsAsync(int assetId)
    {
        if (_context != null && global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var detailWindow = new FinTrack.Avalonia.Views.InvestmentDetailWindow(_context, assetId);
            await detailWindow.ShowDialog(desktop.MainWindow);
        }
    }
}
