using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

public class AccountItemViewModel
{
    public int AccountId { get; set; }
    public required string BankName { get; set; }
    public required string AccountName { get; set; }
    public string IBAN { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsCryptoExchange { get; set; }
    public string Icon => IsCryptoExchange ? "🪙" : "🏦";
}

public partial class AccountsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private string _cashBalanceDisplay = "₺0,00";

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accountsList = new();

    public AccountsViewModel()
    {
        _context = App.Services?.GetService<AppDbContext>();
        _ = LoadDataAsync();
    }

    public event Action? RequestManageAccounts;
    public event Action<int?>? RequestTransfer;
    public event Action<int?>? RequestAccountDetails;

    [RelayCommand]
    private void ManageAccounts() => RequestManageAccounts?.Invoke();

    [RelayCommand]
    private void Transfer(int? accountId) => RequestTransfer?.Invoke(accountId);

    [RelayCommand]
    private void ShowDetails(int? accountId) => RequestAccountDetails?.Invoke(accountId);

    public async Task LoadDataAsync()
    {
        if (_context == null) return;
        
        try
        {
            var activeAccounts = await _context.BankAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.BankName)
                .ThenBy(a => a.AccountName)
                .ToListAsync();

            var allBankTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.BankAccountId != null)
                .ToListAsync();
                
            var allInvestmentTransactions = await _context.InvestmentTransactions
                .Where(t => t.LinkedBankAccountId != null)
                .ToListAsync();

            var newItems = new System.Collections.Generic.List<AccountItemViewModel>();

            foreach (var account in activeAccounts)
            {
                var accountTransactions = allBankTransactions
                    .Where(t => t.BankAccountId == account.Id)
                    .ToList();

                decimal currentBalance = 0; 
                foreach (var t in accountTransactions)
                {
                    if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
                        currentBalance += t.Amount;
                    else if (t.Category?.Type == TransactionType.Expense)
                        currentBalance -= t.Amount;
                }

                var invTransactions = allInvestmentTransactions
                    .Where(t => t.LinkedBankAccountId == account.Id)
                    .ToList();

                foreach (var invT in invTransactions)
                {
                    if (invT.Type == InvestmentTransactionType.Buy)
                        currentBalance -= invT.TotalCost;
                    else if (invT.Type == InvestmentTransactionType.Sell)
                        currentBalance += invT.TotalCost;
                }

                newItems.Add(new AccountItemViewModel
                {
                    AccountId = account.Id,
                    BankName = account.BankName,
                    AccountName = account.AccountName,
                    IBAN = FormatIban(account.IBAN ?? string.Empty),
                    CurrentBalance = currentBalance,
                    IsCryptoExchange = account.IsCryptoExchange
                });
            }

            decimal cashBalance = CalculateCashBalance();

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                AccountsList.Clear();
                foreach(var item in newItems) AccountsList.Add(item);
                CashBalanceDisplay = $"₺{cashBalance:N2}";
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private decimal CalculateCashBalance()
    {
        if (_context == null) return 0;
        var cashTransactions = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.BankAccountId == null && t.CreditCardAccountId == null)
            .ToList();
            
        decimal balance = 0;
        foreach (var t in cashTransactions)
        {
            if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
                balance += t.Amount;
            else if (t.Category?.Type == TransactionType.Expense)
                balance -= t.Amount;
        }
        return balance;
    }
    
    private string FormatIban(string iban) {
        if (string.IsNullOrWhiteSpace(iban)) return string.Empty;
        var cleanIban = new string(iban.Where(char.IsLetterOrDigit).ToArray());
        return string.Join(" ", Enumerable.Range(0, (cleanIban.Length + 3) / 4).Select(i => cleanIban.Substring(i * 4, Math.Min(4, cleanIban.Length - i * 4))));
    }
}
