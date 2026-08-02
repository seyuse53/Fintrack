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
using FinTrack.Core.Helpers;
using Avalonia.Platform.Storage;

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
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accountsList = new();

    public AccountsViewModel()
    {
        _context = AppDbContext.CreateNew();
    }

    public override void Dispose()
    {
        base.Dispose();
        _context?.Dispose();
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
            AppLogger.Error(ex.Message);
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

    [RelayCommand]
    private async Task UploadStatementAsync(int accountId)
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Banka Ekstresi Seçin (PDF veya Görsel)",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                { 
                    new FilePickerFileType("Desteklenen Dosyalar") { Patterns = new[] { "*.pdf", "*.png", "*.jpg", "*.jpeg" } } 
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var filePath = file.Path.LocalPath;
                string apiKey = FinTrack.Core.Services.SettingsManager.GetGeminiApiKey() ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    var errorDialog = new FinTrack.Avalonia.Views.ConfirmDialog("API Anahtarı Eksik", "Lütfen Ayarlar sayfasından Gemini API anahtarınızı giriniz.", "Tamam", "");
                    await errorDialog.ShowDialog<bool?>(desktop.MainWindow);
                    return;
                }

                try
                {
                    IsLoading = true;
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    string extension = System.IO.Path.GetExtension(filePath);
                    
                    var parser = new FinTrack.Core.Services.GeminiParserService();
                    var parsedTransactions = await parser.ParseStatementAsync(fileBytes, extension, apiKey);

                    if (parsedTransactions == null || parsedTransactions.Count == 0)
                    {
                        var infoDialog = new FinTrack.Avalonia.Views.ConfirmDialog("Sonuç Bulunamadı", "Dosyadan herhangi bir işlem çıkarılamadı.", "Tamam", "");
                        await infoDialog.ShowDialog<bool?>(desktop.MainWindow);
                        return;
                    }

                    var window = new FinTrack.Avalonia.Views.ImportPreviewWindow(null, accountId, parsedTransactions);
                    await window.ShowDialog(desktop.MainWindow);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new FinTrack.Avalonia.Views.ConfirmDialog("Hata Oluştu", $"Ekstre işlenirken bir hata oluştu:\n{ex.Message}", "Tamam", "");
                    await errorDialog.ShowDialog<bool?>(desktop.MainWindow);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
}
