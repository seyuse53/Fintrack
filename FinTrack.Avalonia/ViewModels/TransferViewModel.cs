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

public class TransferAccountItem
{
    public int? AccountId { get; set; } // null = Cash
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public partial class TransferViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private ObservableCollection<TransferAccountItem> _accounts = new();

    [ObservableProperty]
    private TransferAccountItem? _selectedSourceAccount;

    [ObservableProperty]
    private TransferAccountItem? _selectedDestinationAccount;

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
    
    private int? _initialSourceAccountId;

    public TransferViewModel(int? initialSourceAccountId = null)
    {
        _context = App.Services?.GetService<AppDbContext>();
        _initialSourceAccountId = initialSourceAccountId;
        _ = LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        if (_context == null) return;

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

            // Set initial selection
            SelectedSourceAccount = Accounts.FirstOrDefault(a => a.AccountId == _initialSourceAccountId);
            if (SelectedSourceAccount == null)
            {
                SelectedSourceAccount = Accounts.FirstOrDefault();
            }
        });
    }

    [RelayCommand]
    private async Task SaveTransferAsync()
    {
        if (_context == null) return;

        if (SelectedSourceAccount == null || SelectedDestinationAccount == null)
        {
            ShowError("Lütfen kaynak ve hedef hesapları seçiniz.");
            return;
        }

        if (SelectedSourceAccount.AccountId == SelectedDestinationAccount.AccountId)
        {
            ShowError("Kaynak ve hedef hesaplar aynı olamaz.");
            return;
        }

        string cleanAmount = AmountText.Replace(".", "").Replace(",", ".");
        if (!decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
        {
            ShowError("Geçerli bir tutar giriniz.");
            return;
        }

        var date = SelectedDate ?? DateTime.Now;

        // Find or create Transfer category
        var transferCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Type == TransactionType.Transfer && c.Name == "Transfer");
        if (transferCategory == null)
        {
            transferCategory = new Category { Name = "Transfer", Type = TransactionType.Transfer };
            _context.Categories.Add(transferCategory);
            await _context.SaveChangesAsync();
        }

        string groupId = Guid.NewGuid().ToString();
        string desc = string.IsNullOrWhiteSpace(Description) ? "Para Transferi" : Description;

        // Outgoing transaction (-)
        var outTrans = new Transaction
        {
            Amount = -amount,
            Date = date,
            Description = desc,
            CategoryId = transferCategory.Id,
            BankAccountId = SelectedSourceAccount.AccountId,
            GroupId = groupId
        };

        // Incoming transaction (+)
        var inTrans = new Transaction
        {
            Amount = amount,
            Date = date,
            Description = desc,
            CategoryId = transferCategory.Id,
            BankAccountId = SelectedDestinationAccount.AccountId,
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
