using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FinTrack.Avalonia.ViewModels;

public partial class PayCreditCardViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;
    private readonly int _cardId;

    [ObservableProperty]
    private string _cardTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TransferAccountItem> _accounts = new();

    [ObservableProperty]
    private TransferAccountItem? _selectedSourceAccount;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    public Action? CloseAction { get; set; }

    public PayCreditCardViewModel(int cardId)
    {
        _cardId = cardId;
        _context = App.Services?.GetService<AppDbContext>();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (_context == null) return;

        var card = await _context.CreditCardAccounts.FindAsync(_cardId);
        if (card != null)
        {
            CardTitle = $"{card.BankName} - {card.CardLabel} Borç Ödeme";
        }

        var dbAccounts = await _context.BankAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.BankName)
            .ToListAsync();

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Accounts.Clear();
            
            // Add Cash
            Accounts.Add(new TransferAccountItem { AccountId = null, DisplayName = "Cüzdan (Nakit)", Icon = "💵" });

            // Add Bank Accounts
            foreach (var acc in dbAccounts)
            {
                Accounts.Add(new TransferAccountItem 
                { 
                    AccountId = acc.Id, 
                    DisplayName = $"{acc.BankName} - {acc.AccountName}", 
                    Icon = acc.IsCryptoExchange ? "🪙" : "🏦" 
                });
            }

            SelectedSourceAccount = Accounts.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task SavePaymentAsync()
    {
        if (_context == null) return;

        if (SelectedSourceAccount == null)
        {
            ShowError("Lütfen ödemenin yapılacağı hesabı seçiniz.");
            return;
        }

        string cleanAmount = AmountText.Replace(".", "").Replace(",", ".");
        if (!decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
        {
            ShowError("Geçerli bir tutar giriniz.");
            return;
        }

        var date = SelectedDate ?? DateTime.Now;

        // Find or create "Kredi Kartı Ödemesi" transfer category
        var transferCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Type == TransactionType.Transfer && c.Name == "Kredi Kartı Ödemesi");
        if (transferCategory == null)
        {
            // Try fallback
            transferCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Type == TransactionType.Transfer);
            if (transferCategory == null)
            {
                transferCategory = new Category { Name = "Kredi Kartı Ödemesi", Type = TransactionType.Transfer };
                _context.Categories.Add(transferCategory);
                await _context.SaveChangesAsync();
            }
        }

        string groupId = Guid.NewGuid().ToString();
        string desc = string.IsNullOrWhiteSpace(Description) ? "Kredi Kartı Ödemesi" : Description;

        // Outgoing transaction from Bank (-)
        var outTrans = new Transaction
        {
            Amount = -amount,
            Date = date,
            Description = desc,
            CategoryId = transferCategory.Id,
            BankAccountId = SelectedSourceAccount.AccountId,
            CreditCardAccountId = null,
            GroupId = groupId
        };

        // Incoming transaction to Credit Card (+)
        var inTrans = new Transaction
        {
            Amount = amount,
            Date = date,
            Description = desc,
            CategoryId = transferCategory.Id,
            BankAccountId = null,
            CreditCardAccountId = _cardId,
            GroupId = groupId
        };

        _context.Transactions.Add(outTrans);
        _context.Transactions.Add(inTrans);

        await _context.SaveChangesAsync();
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke();
    }

    private void ShowError(string msg)
    {
        ErrorMessage = msg;
        HasError = true;
    }
}
