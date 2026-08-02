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
using System.Collections.Generic;
using FinTrack.Avalonia.Localization;
using FinTrack.Core.Helpers;

namespace FinTrack.Avalonia.ViewModels;

public class AccountDetailItem
{
    public int TransactionId { get; set; }
    public bool IsInvestment { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string ForegroundColor { get; set; } = "#333333";
    public string Icon { get; set; } = string.Empty;
}

public partial class AccountDetailViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;
    private readonly int? _accountId;

    [ObservableProperty]
    private string _accountTitle = string.Empty;

    [ObservableProperty]
    private string _currentBalanceDisplay = "₺0,00";

    [ObservableProperty]
    private ObservableCollection<AccountDetailItem> _transactions = new();

    public Action? CloseAction { get; set; }
    public event Action<int>? RequestEditTransaction;
    public event Action? DataChanged;
    public Func<string, Task<bool>>? ConfirmDeleteFunc { get; set; }

    public AccountDetailViewModel(int? accountId)
    {
        _context = AppDbContext.CreateNew();
        _accountId = accountId;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (_context == null) return;

        decimal currentBalance = 0;
        var items = new List<AccountDetailItem>();

        if (_accountId == null)
        {
            // Cash Account
            AccountTitle = LocalizationService.GetString("AccountDetail_WalletTitle");

            var cashTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.BankAccountId == null && t.CreditCardAccountId == null)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            foreach (var t in cashTransactions)
            {
                string sign = "";
                string color = "#333333";
                
                if (t.Category?.Type == TransactionType.Income || (t.Category?.Type == TransactionType.Transfer && t.Amount > 0))
                {
                    sign = "+";
                    color = "#27AE60";
                    currentBalance += t.Amount;
                }
                else if (t.Category?.Type == TransactionType.Expense || (t.Category?.Type == TransactionType.Transfer && t.Amount < 0))
                {
                    sign = "-";
                    color = "#C62828";
                    currentBalance -= Math.Abs(t.Amount);
                }

                string desc = t.Description ?? LocalizationService.GetString("Global_Other");
                if (desc.StartsWith("G:"))
                {
                    string dek = FinTrack.Core.Services.SettingsManager.ActiveDataKey ?? "";
                    if (!string.IsNullOrEmpty(dek))
                    {
                        desc = FinTrack.Core.Services.CryptoProvider.Decrypt(desc.Substring(2), dek);
                    }
                }

                items.Add(new AccountDetailItem
                {
                    TransactionId = t.Id,
                    IsInvestment = false,
                    Date = t.Date,
                    Description = desc,
                    CategoryName = t.Category?.Name ?? LocalizationService.GetString("Global_Other"),
                    Icon = t.Category?.TypeIcon ?? "💵",
                    AmountText = $"{sign}₺{Math.Abs(t.Amount):N2}",
                    ForegroundColor = color
                });
            }
        }
        else
        {
            // Bank Account
            var account = await _context.BankAccounts.FindAsync(_accountId);
            if (account != null)
            {
                AccountTitle = $"{account.BankName} - {account.AccountName}";
            }

            var bankTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.BankAccountId == _accountId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            foreach (var t in bankTransactions)
            {
                string sign = "";
                string color = "#333333";
                
                if (t.Category?.Type == TransactionType.Income || (t.Category?.Type == TransactionType.Transfer && t.Amount > 0))
                {
                    sign = "+";
                    color = "#27AE60";
                    currentBalance += t.Amount;
                }
                else if (t.Category?.Type == TransactionType.Expense || (t.Category?.Type == TransactionType.Transfer && t.Amount < 0))
                {
                    sign = "-";
                    color = "#C62828";
                    currentBalance -= Math.Abs(t.Amount);
                }

                string desc = t.Description ?? LocalizationService.GetString("Global_Other");
                if (desc.StartsWith("G:"))
                {
                    string dek = FinTrack.Core.Services.SettingsManager.ActiveDataKey ?? "";
                    if (!string.IsNullOrEmpty(dek))
                    {
                        desc = FinTrack.Core.Services.CryptoProvider.Decrypt(desc.Substring(2), dek);
                    }
                }

                items.Add(new AccountDetailItem
                {
                    TransactionId = t.Id,
                    IsInvestment = false,
                    Date = t.Date,
                    Description = desc,
                    CategoryName = t.Category?.Name ?? LocalizationService.GetString("Global_Other"),
                    Icon = t.Category?.TypeIcon ?? "🏦",
                    AmountText = $"{sign}₺{Math.Abs(t.Amount):N2}",
                    ForegroundColor = color
                });
            }

            // Include Investment Transactions if linked to this bank
            var invTransactions = await _context.InvestmentTransactions
                .Where(t => t.LinkedBankAccountId == _accountId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            foreach (var inv in invTransactions)
            {
                string sign = inv.Type == InvestmentTransactionType.Buy ? "-" : "+";
                string color = inv.Type == InvestmentTransactionType.Buy ? "#C62828" : "#27AE60";
                
                if (inv.Type == InvestmentTransactionType.Buy)
                    currentBalance -= inv.TotalCost;
                else if (inv.Type == InvestmentTransactionType.Sell)
                    currentBalance += inv.TotalCost;

                items.Add(new AccountDetailItem
                {
                    TransactionId = inv.Id,
                    IsInvestment = true,
                    Date = inv.Date,
                    Description = string.Format(LocalizationService.GetString("AddInvestment_DescFormat"), inv.Amount, _context.InvestmentAssets.Find(inv.InvestmentAssetId)?.Symbol, inv.UnitPrice, inv.Fee),
                    CategoryName = LocalizationService.GetString("Global_Other"),
                    Icon = "📈",
                    AmountText = $"{sign}₺{Math.Abs(inv.TotalCost):N2}",
                    ForegroundColor = color
                });
            }
        }

        // Re-sort items by date since we might have mixed them (Bank + Investments)
        items = items.OrderByDescending(i => i.Date).ThenByDescending(i => i.TransactionId).ToList();

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Transactions.Clear();
            foreach (var item in items)
                Transactions.Add(item);
                
            CurrentBalanceDisplay = $"₺{currentBalance:N2}";
        });
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void EditTransaction(AccountDetailItem? item)
    {
        if (item == null || item.IsInvestment) return;
        RequestEditTransaction?.Invoke(item.TransactionId);
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(AccountDetailItem? item)
    {
        if (item == null || item.IsInvestment || _context == null) return;

        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == item.TransactionId);

            if (transaction != null)
            {
                bool hasGroup = !string.IsNullOrEmpty(transaction.GroupId);
                bool isTransfer = transaction.Category?.Type == TransactionType.Transfer;

                string confirmMessage = LocalizationService.GetString("AccountDetail_DeleteConfirm");
                if (hasGroup && isTransfer)
                {
                    confirmMessage = LocalizationService.GetString("AccountDetail_TransferDeleteConfirm");
                }
                else if (hasGroup)
                {
                    confirmMessage = LocalizationService.GetString("AccountDetail_InstallmentDeleteConfirm");
                }

                if (ConfirmDeleteFunc != null)
                {
                    bool confirmed = await ConfirmDeleteFunc(confirmMessage);
                    if (!confirmed) return;
                }

                if (hasGroup)
                {
                    var groupItems = await _context.Transactions.Where(t => t.GroupId == transaction.GroupId).ToListAsync();
                    _context.Transactions.RemoveRange(groupItems);
                }
                else
                {
                    _context.Transactions.Remove(transaction);
                }
                
                await _context.SaveChangesAsync();
                await LoadDataAsync();
                DataChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Delete error: {ex.Message}");
        }
    }

    public async Task RefreshAsync()
    {
        await LoadDataAsync();
        DataChanged?.Invoke();
    }
}
