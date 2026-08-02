using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Linq;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class InvestmentDetailViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;
    private readonly int _assetId;

    [ObservableProperty] private string _assetNameText = LocalizationService.GetString("InvestmentDetail_Loading");
    [ObservableProperty] private string _assetSymbolText = "(-) ";
    [ObservableProperty] private string _assetCategoryText = "";
    [ObservableProperty] private string _totalAmountText = "0";
    [ObservableProperty] private string _avgCostText = "₺0";
    [ObservableProperty] private string _currentPriceText = "₺0";

    [ObservableProperty] private string _totalBuyText = "₺0";
    [ObservableProperty] private string _totalSellText = "₺0";
    [ObservableProperty] private string _totalFeeText = "₺0";
    [ObservableProperty] private string _netProfitText = "₺0";

    [ObservableProperty]
    private ObservableCollection<InvestmentTransaction> _transactions = new();

    [ObservableProperty]
    private System.Collections.Generic.List<FinTrack.Avalonia.Controls.SparklinePoint>? _sparklineData;

    public InvestmentDetailViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow, int assetId)
    {
        _context = context;
        _ownerWindow = ownerWindow;
        _assetId = assetId;
    }

    public async Task InitializeAsync()
    {
        var asset = await _context.InvestmentAssets.FindAsync(_assetId);
        if (asset == null) return;

        AssetNameText = asset.Name;
        AssetSymbolText = $"({asset.Symbol})";
        AssetCategoryText = asset.Category ?? LocalizationService.GetString("Global_Other");
        TotalAmountText = asset.TotalAmount.ToString("N4");
        decimal currentPrice = FinTrack.Core.Services.PricingService.GetCurrentPrice(asset.Symbol, asset.AverageCost);

        if (asset.Category == "Kripto Para")
        {
            AvgCostText = $"₺{asset.AverageCost.ToString("0.########")}";
            CurrentPriceText = $"₺{currentPrice.ToString("0.########")}";
        }
        else
        {
            AvgCostText = $"₺{asset.AverageCost:N2}";
            CurrentPriceText = $"₺{currentPrice:N2}";
        }

        var histories = await _context.PriceHistories.Where(h => h.Symbol == asset.Symbol).OrderBy(h => h.Date).ToListAsync();
        SparklineData = InvestmentsViewModel.GenerateTrendData(asset.AverageCost, currentPrice, histories);

        var txs = await _context.InvestmentTransactions
            .Include(t => t.LinkedBankAccount)
            .Where(t => t.InvestmentAssetId == _assetId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        Transactions.Clear();
        decimal totalBuy = 0, totalSell = 0, totalFee = 0;

        foreach (var t in txs)
        {
            Transactions.Add(t);
            if (t.Type == InvestmentTransactionType.Buy)
                totalBuy += (t.Amount * t.UnitPrice);
            else if (t.Type == InvestmentTransactionType.Sell)
                totalSell += (t.Amount * t.UnitPrice);

            totalFee += t.Fee;
        }

        TotalBuyText = $"₺{totalBuy:N2}";
        TotalSellText = $"₺{totalSell:N2}";
        TotalFeeText = $"₺{totalFee:N2}";

        decimal currentValuation = asset.TotalAmount * currentPrice;
        decimal netProfit = totalSell + currentValuation - totalBuy - totalFee;
        
        NetProfitText = netProfit >= 0 ? $"+₺{netProfit:N2}" : $"-₺{System.Math.Abs(netProfit):N2}";
    }

    [RelayCommand]
    private void Close()
    {
        _ownerWindow.Close();
    }
}
