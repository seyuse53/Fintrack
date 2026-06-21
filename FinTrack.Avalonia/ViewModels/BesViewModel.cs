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

public partial class BesViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private decimal _totalBesValue;

    [ObservableProperty]
    private decimal _totalCostBasis;

    [ObservableProperty]
    private decimal _totalProfit;

    [ObservableProperty]
    private decimal _totalProfitPercentage;

    [ObservableProperty]
    private decimal _estimatedStateContribution;

    [ObservableProperty]
    private decimal _totalGramGold;

    [ObservableProperty]
    private ObservableCollection<InvestmentItemViewModel> _besInvestments = new();

    public BesViewModel()
    {
        _context = App.Services?.GetService<AppDbContext>();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadDataAsync();
        
        // Altın karşılığı hesaplamaları için XAU fiyatını güvenceye al
        await PricingService.FetchPriceSmartAsync("XAU", "Altın");
        
        // Fiyatı çektikten sonra arayüzü güncelle
        global::Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            await LoadDataAsync();
        });
    }

    public async Task LoadDataAsync()
    {
        if (_context == null) return;

        try
        {
            var assets = await _context.InvestmentAssets.Where(a => a.Category == "BES").ToListAsync();
            
            PricingService.LoadPricesFromAssets(assets);

            var allHistories = await _context.PriceHistories.OrderBy(h => h.Date).ToListAsync();
            var historiesBySymbol = allHistories.GroupBy(h => h.Symbol).ToDictionary(g => g.Key, g => g.ToList());

            var tempList = new System.Collections.Generic.List<InvestmentItemViewModel>();

            decimal totalValue = 0;
            decimal totalCost = 0;

            foreach (var asset in assets)
            {
                decimal currentPrice = PricingService.GetCurrentPrice(asset.Symbol, asset.AverageCost);
                decimal costBasis = asset.AverageCost * asset.TotalAmount;
                
                decimal currentValue = asset.CustomCurrentValue ?? (currentPrice * asset.TotalAmount);
                decimal profit = currentValue - costBasis;
                decimal profitPct = costBasis > 0 ? (profit / costBasis) * 100 : 0;
                decimal stateContribution = asset.CustomStateContribution ?? (costBasis * 0.30m);

                totalValue += currentValue;
                totalCost += costBasis;

                var itemGramGold = await _context.InvestmentTransactions
                    .Where(t => t.InvestmentAssetId == asset.Id && t.GramGoldEquivalent.HasValue)
                    .SumAsync(t => t.GramGoldEquivalent ?? 0);

                decimal vestingPct = 0;
                string timeLeft = "Belirtilmemiş";
                string timeInSystem = "Belirtilmemiş";
                decimal monthlyAvg = 0;
                int monthsIn = 1;
                
                if (asset.BesStartDate.HasValue)
                {
                    monthsIn = (DateTime.Now.Year - asset.BesStartDate.Value.Year) * 12 + DateTime.Now.Month - asset.BesStartDate.Value.Month;
                    if (monthsIn < 1) monthsIn = 1;
                    
                    int yIn = monthsIn / 12;
                    int mIn = monthsIn % 12;
                    timeInSystem = $"{yIn} Yıl {mIn} Ay";

                    var yearsIn = monthsIn / 12.0;

                    if (yearsIn >= 10 && asset.BesRetirementDate.HasValue && DateTime.Now >= asset.BesRetirementDate.Value)
                    {
                        vestingPct = 1.0m; // %100
                    }
                    else if (yearsIn >= 10)
                    {
                        vestingPct = 0.60m; // %60
                    }
                    else if (yearsIn >= 6)
                    {
                        vestingPct = 0.35m; // %35
                    }
                    else if (yearsIn >= 3)
                    {
                        vestingPct = 0.15m; // %15
                    }
                    else
                    {
                        vestingPct = 0.0m; // %0
                    }
                }

                if (asset.BesRetirementDate.HasValue)
                {
                    if (DateTime.Now >= asset.BesRetirementDate.Value)
                    {
                        timeLeft = "Emeklilik Hakkı Kazanıldı!";
                    }
                    else
                    {
                        var span = asset.BesRetirementDate.Value - DateTime.Now;
                        int totalMonths = (int)(span.TotalDays / 30.436875);
                        int y = totalMonths / 12;
                        int m = totalMonths % 12;
                        timeLeft = $"{y} yıl {m} ay";
                    }
                }

                tempList.Add(new InvestmentItemViewModel
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    Symbol = asset.Symbol,
                    Category = asset.Category ?? "BES",
                    TotalAmount = asset.TotalAmount,
                    AverageCost = asset.AverageCost,
                    CurrentPrice = currentPrice,
                    CurrentValue = currentValue,
                    Profit = profit,
                    ProfitPercentage = profitPct,
                    BesTotalCostBasis = costBasis,
                    BesGramGold = itemGramGold,
                    BesStateContribution = stateContribution,
                    BesContractNo = asset.BesContractNo,
                    BesStartDate = asset.BesStartDate,
                    BesRetirementDate = asset.BesRetirementDate,
                    TimeUntilRetirement = timeLeft,
                    VestingPercentage = vestingPct,
                    VestedStateContribution = stateContribution * vestingPct,
                    CurrentTotalGramGold = PricingService.GetCurrentPrice("XAU", 0) > 0 ? (currentValue + (stateContribution * vestingPct)) / PricingService.GetCurrentPrice("XAU", 0) : 0,
                    GoldProfitLoss = (PricingService.GetCurrentPrice("XAU", 0) > 0 ? (currentValue + (stateContribution * vestingPct)) / PricingService.GetCurrentPrice("XAU", 0) : 0) - itemGramGold,
                    BesTimeInSystem = timeInSystem,
                    BesMonthlyAverage = (currentValue + (stateContribution * vestingPct)) / monthsIn,
                    SparklineData = InvestmentsViewModel.GenerateTrendData(asset.AverageCost, currentPrice, historiesBySymbol.ContainsKey(asset.Symbol) ? historiesBySymbol[asset.Symbol] : new System.Collections.Generic.List<PriceHistory>())
                });
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                BesInvestments.Clear();
                foreach(var item in tempList.OrderByDescending(x => x.CurrentValue)) 
                {
                    if (totalValue > 0) item.PortfolioWeight = $"%{(item.CurrentValue / totalValue * 100):N1}";
                    else item.PortfolioWeight = "%0.0";
                    BesInvestments.Add(item);
                }

                TotalBesValue = totalValue;
                TotalCostBasis = totalCost;
                TotalProfit = totalValue - totalCost;
                TotalProfitPercentage = totalCost > 0 ? (TotalProfit / totalCost) * 100 : 0;
                EstimatedStateContribution = totalCost * 0.30m; // %30 Devlet Katkısı (Tahmini)
                TotalGramGold = tempList.Sum(t => t.BesGramGold);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BES verileri çekilemedi: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(int assetId)
    {
        if (_context != null && global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var detailWindow = new FinTrack.Avalonia.Views.InvestmentDetailWindow(_context, assetId);
            await detailWindow.ShowDialog(desktop.MainWindow);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task UpdateValuesAsync(int assetId)
    {
        if (_context != null && global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var asset = await _context.InvestmentAssets.FindAsync(assetId);
            if (asset != null)
            {
                var vm = new FinTrack.Avalonia.ViewModels.UpdateBesValuesViewModel(_context, asset);
                var window = new FinTrack.Avalonia.Views.UpdateBesValuesWindow(vm);
                await window.ShowDialog(desktop.MainWindow);
                await LoadDataAsync();
            }
        }
    }
}
