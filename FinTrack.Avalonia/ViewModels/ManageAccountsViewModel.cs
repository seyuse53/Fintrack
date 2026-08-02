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
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class ManageAccountsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private ObservableCollection<BankAccount> _accounts = new();

    [ObservableProperty]
    private BankAccount? _selectedAccount;

    [ObservableProperty]
    private ObservableCollection<string> _availableBanks = new(new[]
    {
        "Akbank", "Albaraka Türk", "Alternatif Bank", "Anadolubank", "BtcTurk", "Burgan Bank", 
        "DenizBank", "Enpara.com", "Fibabanka", "Garanti BBVA", "Halkbank", "HSBC", 
        "ING", "Kuveyt Türk", "Odeabank", "Papara", "QNB Finansbank", "Şekerbank", "TEB", 
        "Türkiye Finans", "Türkiye İş Bankası", "VakıfBank", "Vakıf Katılım", "Yapı Kredi", 
        "Ziraat Bankası", "Ziraat Katılım"
    });

    [ObservableProperty]
    private string _newBankName = string.Empty;

    [ObservableProperty]
    private string _newAccountName = string.Empty;

    [ObservableProperty]
    private string _newIBAN = string.Empty;

    [ObservableProperty]
    private decimal _newInitialBalance = 0;

    [ObservableProperty]
    private bool _newIsCryptoExchange = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private string _formTitle = LocalizationService.GetString("ManageAccounts_AddTitle");

    [ObservableProperty]
    private string _submitButtonText = LocalizationService.GetString("ManageAccounts_AddButton");

    private BankAccount? _editingAccount = null;

    // Action to close window
    public Action? CloseAction { get; set; }

    public ManageAccountsViewModel()
    {
        _context = AppDbContext.CreateNew();
        _ = LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        if (_context == null) return;

        var dbAccounts = await _context.BankAccounts
            .OrderBy(a => a.BankName)
            .ThenBy(a => a.AccountName)
            .ToListAsync();

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Accounts.Clear();
            foreach (var acc in dbAccounts)
            {
                Accounts.Add(acc);
            }
        });
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (_context == null) return;

        if (string.IsNullOrWhiteSpace(NewBankName) || string.IsNullOrWhiteSpace(NewAccountName))
        {
            ShowError(LocalizationService.GetString("ManageAccounts_ErrNameReq"));
            return;
        }

        if (IsEditing && _editingAccount != null)
        {
            var dbAcc = await _context.BankAccounts.FindAsync(_editingAccount.Id);
            if (dbAcc != null)
            {
                dbAcc.BankName = NewBankName.Trim();
                dbAcc.AccountName = NewAccountName.Trim();
                dbAcc.IBAN = NewIBAN?.Trim();
                dbAcc.InitialBalance = NewInitialBalance;
                dbAcc.IsCryptoExchange = NewIsCryptoExchange;
            }
        }
        else
        {
            var newAcc = new BankAccount
            {
                BankName = NewBankName.Trim(),
                AccountName = NewAccountName.Trim(),
                IBAN = NewIBAN?.Trim(),
                InitialBalance = NewInitialBalance,
                IsCryptoExchange = NewIsCryptoExchange,
                IsActive = true
            };

            _context.BankAccounts.Add(newAcc);
            
            // Add Opening Balance transaction if initial balance > 0
            if (newAcc.InitialBalance > 0)
            {
                var openingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Açılış Bakiyesi");
                if (openingCategory == null)
                {
                    openingCategory = new Category { Name = "Açılış Bakiyesi", Type = TransactionType.Income };
                    _context.Categories.Add(openingCategory);
                }
                
                var trans = new Transaction
                {
                    Amount = newAcc.InitialBalance,
                    Date = DateTime.Now,
                    Description = "Hesap Açılış Bakiyesi",
                    Category = openingCategory,
                    BankAccount = newAcc
                };
                _context.Transactions.Add(trans);
            }
        }

        await _context.SaveChangesAsync();
        await LoadAccountsAsync();

        CancelEdit();
    }

    [RelayCommand]
    private void EditAccount(BankAccount account)
    {
        _editingAccount = account;
        IsEditing = true;
        FormTitle = LocalizationService.GetString("ManageAccounts_EditTitle");
        SubmitButtonText = LocalizationService.GetString("ManageAccounts_UpdateButton");

        NewBankName = account.BankName;
        NewAccountName = account.AccountName;
        NewIBAN = account.IBAN ?? string.Empty;
        NewInitialBalance = account.InitialBalance;
        NewIsCryptoExchange = account.IsCryptoExchange;
        
        ClearError();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingAccount = null;
        IsEditing = false;
        FormTitle = LocalizationService.GetString("ManageAccounts_AddTitle");
        SubmitButtonText = LocalizationService.GetString("ManageAccounts_AddButton");

        NewBankName = string.Empty;
        NewAccountName = string.Empty;
        NewIBAN = string.Empty;
        NewInitialBalance = 0;
        NewIsCryptoExchange = false;
        ClearError();
    }

    [RelayCommand]
    private async Task ToggleActiveStatusAsync(BankAccount account)
    {
        if (_context == null || account == null) return;

        var dbAcc = await _context.BankAccounts.FindAsync(account.Id);
        if (dbAcc != null)
        {
            dbAcc.IsActive = !dbAcc.IsActive;
            await _context.SaveChangesAsync();
            await LoadAccountsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(BankAccount account)
    {
        if (_context == null || account == null) return;

        // Check if there are transactions
        bool hasTransactions = await _context.Transactions.AnyAsync(t => t.BankAccountId == account.Id);
        if (hasTransactions)
        {
            ShowError(LocalizationService.GetString("ManageAccounts_ErrHasTx"));
            return;
        }

        var dbAcc = await _context.BankAccounts.FindAsync(account.Id);
        if (dbAcc != null)
        {
            _context.BankAccounts.Remove(dbAcc);
            await _context.SaveChangesAsync();
            await LoadAccountsAsync();
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction?.Invoke();
    }

    private void ShowError(string msg)
    {
        ErrorMessage = msg;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
