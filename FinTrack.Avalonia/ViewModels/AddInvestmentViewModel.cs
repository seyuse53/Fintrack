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

public partial class AddInvestmentViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;

    [ObservableProperty]
    private string _symbol = "";

    [ObservableProperty]
    private string _name = "";

    public ObservableCollection<string> Categories { get; } = new() 
    { "Altın", "Döviz", "Hisse Senedi", "Kripto Para", "Fon", "Diğer" };

    [ObservableProperty]
    private string _selectedCategory = "Altın";

    [ObservableProperty]
    private ObservableCollection<SymbolItem> _availableSymbols = new();

    [ObservableProperty]
    private SymbolItem? _selectedSymbolItem;

    public global::Avalonia.Controls.AutoCompleteFilterPredicate<object> SymbolFilter { get; }

    partial void OnSelectedSymbolItemChanged(SymbolItem? value)
    {
        if (value != null)
        {
            Symbol = value.Symbol;
            Name = value.Name;
        }
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = LoadSymbolsAsync(value);
    }

    private async Task LoadSymbolsAsync(string category)
    {
        var symbols = await FinTrack.Core.Services.PricingService.GetAvailableSymbolsAsync(category);
        
        // Kullanıcının mevcut portföyünden varlıkları da otomatik tamamlama listesine ekle
        var existingAssets = await _context.InvestmentAssets
            .Where(a => a.Category == category)
            .Select(a => new SymbolItem { Symbol = a.Symbol, Name = a.Name })
            .ToListAsync();
            
        foreach (var asset in existingAssets)
        {
            if (!symbols.Any(s => s.Symbol == asset.Symbol))
            {
                symbols.Insert(0, asset); // Portföydeki varlıkları listenin en başına koy
            }
        }

        // UI thread güvenliği için Avalonia Dispatcher kullanılabilir ama ObservableProperty ataması genelde çalışır.
        AvailableSymbols = new ObservableCollection<SymbolItem>(symbols);
    }

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Now;

    [ObservableProperty]
    private string _amountText = "";

    [ObservableProperty]
    private string _unitPriceText = "";

    [ObservableProperty]
    private string _feeText = "0";

    [ObservableProperty]
    private string _totalCostPreviewText = "₺0,00";

    [ObservableProperty]
    private PaymentItemViewModel? _selectedAccount;

    public ObservableCollection<PaymentItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    public AddInvestmentViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow)
    {
        _context = context;
        _ownerWindow = ownerWindow;

        SymbolFilter = (search, item) =>
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            if (item is SymbolItem symbolItem)
            {
                var s = search.ToLowerInvariant();
                return (symbolItem.Symbol?.ToLowerInvariant().Contains(s) ?? false) || 
                       (symbolItem.Name?.ToLowerInvariant().Contains(s) ?? false);
            }
            return false;
        };
    }

    public async Task InitializeAsync()
    {
        Accounts.Clear();
        // Load simple placeholder for Cash
        Accounts.Add(new PaymentItemViewModel { Label = "💵 Nakit", Bank = null, Card = null });

        var banks = await _context.BankAccounts.Where(b => b.IsActive).ToListAsync();
        foreach (var bank in banks)
        {
            Accounts.Add(new PaymentItemViewModel { Label = $"🏦 {bank.BankName} - {bank.AccountName}", Bank = bank, Card = null });
        }

        var cards = await _context.CreditCardAccounts.Where(c => c.IsActive).ToListAsync();
        foreach (var card in cards)
        {
            Accounts.Add(new PaymentItemViewModel { Label = $"💳 {card.BankName} - {card.CardLabel}", Bank = null, Card = card });
        }

        SelectedAccount = Accounts.First();

        await LoadSymbolsAsync(SelectedCategory);
    }

    // This method is called whenever text changes to update the cost preview
    partial void OnAmountTextChanged(string value) => UpdateCostPreview();
    partial void OnUnitPriceTextChanged(string value) => UpdateCostPreview();
    partial void OnFeeTextChanged(string value) => UpdateCostPreview();

    public void UpdateCostPreview()
    {
        decimal amount = ParseDecimal(AmountText);
        decimal unitPrice = ParseDecimal(UnitPriceText);
        decimal fee = ParseDecimal(FeeText);

        decimal total = (amount * unitPrice) + fee;
        TotalCostPreviewText = $"₺{total:N2}";
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
        if (string.IsNullOrWhiteSpace(Symbol) || string.IsNullOrWhiteSpace(Name))
        {
            ShowError("Sembol ve İsim zorunludur.");
            return;
        }

        decimal amount = ParseDecimal(AmountText);
        decimal unitPrice = ParseDecimal(UnitPriceText);
        decimal fee = ParseDecimal(FeeText);

        if (amount <= 0 || unitPrice < 0 || fee < 0)
        {
            ShowError("Lütfen geçerli değerler giriniz.");
            return;
        }

        decimal totalCost = (amount * unitPrice) + fee;

        try
        {
            var asset = await _context.InvestmentAssets.FirstOrDefaultAsync(a => a.Symbol == Symbol.ToUpper());
            if (asset == null)
            {
                asset = new InvestmentAsset
                {
                    Symbol = Symbol.ToUpper(),
                    Name = Name,
                    Category = SelectedCategory,
                    TotalAmount = amount,
                    AverageCost = unitPrice
                };
                _context.InvestmentAssets.Add(asset);
            }
            else
            {
                decimal oldCostBasis = asset.TotalAmount * asset.AverageCost;
                decimal newTotalAmount = asset.TotalAmount + amount;
                decimal newTotalCostBasis = oldCostBasis + totalCost;
                
                asset.AverageCost = newTotalAmount > 0 ? (newTotalCostBasis / newTotalAmount) : 0;
                asset.TotalAmount = newTotalAmount;
                asset.Category = SelectedCategory;
                _context.InvestmentAssets.Update(asset);
            }

            var transaction = new InvestmentTransaction
            {
                InvestmentAsset = asset,
                Type = InvestmentTransactionType.Buy,
                Amount = amount,
                UnitPrice = unitPrice,
                Fee = fee,
                TotalCost = totalCost,
                Date = SelectedDate ?? DateTime.Today,
                Notes = $"Varlık alımı: {amount} {Symbol.ToUpper()} @ {unitPrice:C2} (Masraf: {fee:C2})",
                LinkedBankAccountId = SelectedAccount?.Bank?.Id,
                LinkedCreditCardAccountId = SelectedAccount?.Card?.Id
            };

            _context.InvestmentTransactions.Add(transaction);
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
