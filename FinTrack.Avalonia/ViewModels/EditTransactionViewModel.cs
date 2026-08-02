using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class EditTransactionViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;
    private readonly int _transactionId;

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Now;

    [ObservableProperty]
    private string _amountText = "";

    [ObservableProperty]
    private CategoryItemViewModel? _selectedCategory;

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

    [ObservableProperty]
    private PaymentItemViewModel? _selectedPaymentMethod;

    public ObservableCollection<PaymentItemViewModel> PaymentMethods { get; } = new();

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isTransfer;

    public EditTransactionViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow, int transactionId)
    {
        _context = context;
        _ownerWindow = ownerWindow;
        _transactionId = transactionId;
    }

    public async Task InitializeAsync()
    {
        // Load categories - include Transfer categories for editing existing transfers
        var allCategories = await _context.Categories
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.ParentCategoryId == null ? 0 : 1)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var parents = allCategories.Where(c => c.ParentCategoryId == null).ToList();
        Categories.Clear();

        foreach (var parent in parents)
        {
            Categories.Add(new CategoryItemViewModel { Id = parent.Id, Name = parent.Name, Type = parent.Type, IsSubCategory = false });
            var children = allCategories.Where(c => c.ParentCategoryId == parent.Id).ToList();
            foreach (var child in children)
            {
                Categories.Add(new CategoryItemViewModel { Id = child.Id, Name = child.Name, Type = child.Type, IsSubCategory = true });
            }
        }

        // Load Payment Methods
        PaymentMethods.Clear();
        PaymentMethods.Add(new PaymentItemViewModel { Label = LocalizationService.GetString("AddTransaction_Cash"), Card = null, Bank = null });

        var bankAccounts = await _context.BankAccounts.Where(b => b.IsActive).OrderBy(b => b.BankName).ThenBy(b => b.AccountName).ToListAsync();
        foreach (var bank in bankAccounts)
            PaymentMethods.Add(new PaymentItemViewModel { Label = $"🏦 {bank.BankName} – {bank.AccountName}", Card = null, Bank = bank });

        var cards = await _context.CreditCardAccounts.Where(c => c.IsActive).OrderBy(c => c.BankName).ThenBy(c => c.CardLabel).ToListAsync();
        foreach (var card in cards)
            PaymentMethods.Add(new PaymentItemViewModel { Label = $"💳 {card.BankName} – {card.CardLabel}", Card = card, Bank = null });

        // Load existing transaction
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == _transactionId);

        if (transaction != null)
        {
            SelectedDate = transaction.Date;
            AmountText = transaction.Amount.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
            Description = transaction.Description ?? "";

            // Detect if this is a transfer transaction
            IsTransfer = transaction.Category?.Type == TransactionType.Transfer;

            // Select matching category
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == transaction.CategoryId);

            // Select matching payment method
            if (transaction.CreditCardAccountId != null)
                SelectedPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Card?.Id == transaction.CreditCardAccountId);
            else if (transaction.BankAccountId != null)
                SelectedPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Bank?.Id == transaction.BankAccountId);
            else
                SelectedPaymentMethod = PaymentMethods.FirstOrDefault(); // Nakit
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        HasError = false;

        string cleanAmount = AmountText.Replace(".", "").Replace(",", ".");
        if (!decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || (!IsTransfer && amount <= 0))
        {
            ShowError(LocalizationService.GetString("Global_ErrInvalidAmount"));
            return;
        }

        if (SelectedCategory == null)
        {
            ShowError(LocalizationService.GetString("AddTransaction_ErrCategory"));
            return;
        }

        try
        {
            var transaction = await _context.Transactions.FindAsync(_transactionId);
            if (transaction == null)
            {
                ShowError(LocalizationService.GetString("EditTransaction_ErrNotFound"));
                return;
            }

            transaction.Date = SelectedDate ?? DateTime.Today;
            transaction.Amount = amount;
            transaction.CategoryId = SelectedCategory.Id;
            transaction.Description = Description;
            transaction.CreditCardAccountId = SelectedPaymentMethod?.Card?.Id;
            transaction.BankAccountId = SelectedPaymentMethod?.Bank?.Id;

            // Synchronize transfer pairs
            if (IsTransfer && !string.IsNullOrEmpty(transaction.GroupId))
            {
                var pairedTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.GroupId == transaction.GroupId && t.Id != transaction.Id);
                    
                if (pairedTransaction != null)
                {
                    pairedTransaction.Date = transaction.Date;
                    pairedTransaction.Description = transaction.Description;
                    pairedTransaction.Amount = -transaction.Amount; // Ters işaretlisi
                }
            }

            await _context.SaveChangesAsync();
            _ownerWindow.Close(true);
        }
        catch (Exception ex)
        {
            ShowError(string.Format(LocalizationService.GetString("EditTransaction_ErrUpdate"), ex.Message));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _ownerWindow.Close(false);
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
