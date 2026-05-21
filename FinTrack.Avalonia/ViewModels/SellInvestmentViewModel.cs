using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.Avalonia.ViewModels;

public partial class SellInvestmentViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;
    private readonly int _assetId;
    private InvestmentAsset? _asset;

    [ObservableProperty]
    private string _assetInfoText = "Yükleniyor...";

    [ObservableProperty]
    private string _availableAmountText = "Satılabilir Miktar: 0";

    [ObservableProperty]
    private string _avgCostInfoText = "";

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Now;

    [ObservableProperty]
    private string _amountText = "";

    [ObservableProperty]
    private string _unitPriceText = "";

    [ObservableProperty]
    private string _feeText = "0";

    [ObservableProperty]
    private string _revenuePreviewText = "₺0,00";

    [ObservableProperty]
    private string _profitPreviewText = "₺0,00";

    [ObservableProperty]
    private PaymentItemViewModel? _selectedAccount;

    public ObservableCollection<PaymentItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    public SellInvestmentViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow, int assetId)
    {
        _context = context;
        _ownerWindow = ownerWindow;
        _assetId = assetId;
    }

    public async Task InitializeAsync()
    {
        _asset = await _context.InvestmentAssets.FindAsync(_assetId);
        if (_asset == null)
        {
            ShowError("Varlık bulunamadı.");
            return;
        }

        AssetInfoText = $"Varlık: {_asset.Name} ({_asset.Symbol})";
        AvailableAmountText = $"Satılabilir Miktar: {_asset.TotalAmount:N4}";
        AvgCostInfoText = $"Ort. Maliyet: ₺{_asset.AverageCost:N2}";

        Accounts.Clear();
        Accounts.Add(new PaymentItemViewModel { Label = "💵 Nakit (İsteğe Bağlı)", Bank = null, Card = null });

        var banks = await _context.BankAccounts.Where(b => b.IsActive).ToListAsync();
        foreach (var bank in banks)
        {
            Accounts.Add(new PaymentItemViewModel { Label = $"🏦 {bank.BankName} - {bank.AccountName}", Bank = bank, Card = null });
        }

        SelectedAccount = Accounts.First();

        decimal currentPrice = FinTrack.Core.Services.PricingService.GetCurrentPrice(_asset.Symbol, _asset.AverageCost);
        UnitPriceText = currentPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void SellAll()
    {
        if (_asset != null)
        {
            AmountText = _asset.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    partial void OnAmountTextChanged(string value) => UpdatePreview();
    partial void OnUnitPriceTextChanged(string value) => UpdatePreview();
    partial void OnFeeTextChanged(string value) => UpdatePreview();

    private void UpdatePreview()
    {
        if (_asset == null) return;

        decimal amount = ParseDecimal(AmountText);
        decimal unitPrice = ParseDecimal(UnitPriceText);
        decimal fee = ParseDecimal(FeeText);

        decimal revenue = (amount * unitPrice) - fee;
        decimal costBasis = amount * _asset.AverageCost;
        decimal profit = revenue - costBasis;

        RevenuePreviewText = $"₺{revenue:N2}";
        ProfitPreviewText = profit >= 0 ? $"+₺{profit:N2}" : $"-₺{Math.Abs(profit):N2}";
    }

    private decimal ParseDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        string cleanText = text.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
            return val;
        return 0;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        HasError = false;
        if (_asset == null) return;

        decimal amount = ParseDecimal(AmountText);
        decimal unitPrice = ParseDecimal(UnitPriceText);
        decimal fee = ParseDecimal(FeeText);

        if (amount <= 0 || amount > _asset.TotalAmount || unitPrice < 0 || fee < 0)
        {
            ShowError("Geçerli bir miktar (sahip olduğunuz kadar) ve fiyat giriniz.");
            return;
        }

        try
        {
            decimal revenue = (amount * unitPrice) - fee;
            decimal costBasis = amount * _asset.AverageCost;
            decimal profitLoss = revenue - costBasis;

            var transaction = new InvestmentTransaction
            {
                InvestmentAssetId = _asset.Id,
                Type = InvestmentTransactionType.Sell,
                Amount = amount,
                UnitPrice = unitPrice,
                Fee = fee,
                TotalCost = revenue, // Total Revenue
                Date = SelectedDate ?? DateTime.Today,
                Notes = $"Satış: {amount} {_asset.Symbol} @ {unitPrice:C2} (Masraf: {fee:C2}, K/Z: {profitLoss:C2})",
                LinkedBankAccountId = SelectedAccount?.Bank?.Id
            };

            _context.InvestmentTransactions.Add(transaction);

            _asset.TotalAmount -= amount;
            if (_asset.TotalAmount == 0)
            {
                _asset.AverageCost = 0;
            }
            
            _context.InvestmentAssets.Update(_asset);
            await _context.SaveChangesAsync();

            _ownerWindow.Close(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _ownerWindow.Close(false);
    }

    private void ShowError(string msg)
    {
        HasError = true;
        ErrorMessage = msg;
    }
}
