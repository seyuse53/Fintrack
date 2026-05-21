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

public partial class PaymentItemViewModel : ObservableObject
{
    public required string Label { get; set; }
    public CreditCardAccount? Card { get; set; }
    public BankAccount? Bank { get; set; }

    public override string ToString() => Label;
}

public partial class CategoryItemViewModel : ObservableObject
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required TransactionType Type { get; set; }
    public string TypeIcon => Type == TransactionType.Income ? "💰" : Type == TransactionType.Transfer ? "🔄" : "💸";
    public bool IsSubCategory { get; set; }

    public double IndentWidth => IsSubCategory ? 20 : 0;
    public string TypeColor => Type == TransactionType.Income ? "#27AE60" : Type == TransactionType.Transfer ? "#3498DB" : "#E74C3C";
    public string FontWeight => IsSubCategory ? "Normal" : "SemiBold";
    public string ForegroundColor => Type == TransactionType.Income ? "#2E7D32" : Type == TransactionType.Transfer ? "#1565C0" : "#C62828";
}

public partial class AddTransactionViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;

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
    private bool _isInstallmentAllowed;

    [ObservableProperty]
    private bool _isInstallment;

    [ObservableProperty]
    private string _selectedInstallmentCount = "3";

    public ObservableCollection<string> InstallmentOptions { get; } = new() { "2", "3", "4", "5", "6", "9", "12" };

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    public AddTransactionViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow)
    {
        _context = context;
        _ownerWindow = ownerWindow;
    }

    public async Task InitializeAsync()
    {
        // Load categories
        var allCategories = await _context.Categories
            .Where(c => c.Type != TransactionType.Transfer && c.IsVisible)
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

        if (Categories.Any()) SelectedCategory = Categories.First();

        // Load Payment Methods
        PaymentMethods.Clear();
        PaymentMethods.Add(new PaymentItemViewModel { Label = "💵 Nakit", Card = null, Bank = null });

        var bankAccounts = await _context.BankAccounts.Where(b => b.IsActive).OrderBy(b => b.BankName).ThenBy(b => b.AccountName).ToListAsync();
        foreach (var bank in bankAccounts)
            PaymentMethods.Add(new PaymentItemViewModel { Label = $"🏦 {bank.BankName} – {bank.AccountName}", Card = null, Bank = bank });

        var cards = await _context.CreditCardAccounts.Where(c => c.IsActive).OrderBy(c => c.BankName).ThenBy(c => c.CardLabel).ToListAsync();
        foreach (var card in cards)
            PaymentMethods.Add(new PaymentItemViewModel { Label = $"💳 {card.BankName} – {card.CardLabel}", Card = card, Bank = null });

        SelectedPaymentMethod = PaymentMethods.First();
    }

    partial void OnSelectedPaymentMethodChanged(PaymentItemViewModel? value)
    {
        IsInstallmentAllowed = value?.Card != null;
        if (!IsInstallmentAllowed) IsInstallment = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        HasError = false;
        
        string cleanAmount = AmountText.Replace(".", "").Replace(",", ".");
        if (!decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalAmount) || totalAmount <= 0)
        {
            ShowError("Lütfen geçerli bir tutar giriniz.");
            return;
        }

        if (SelectedCategory == null)
        {
            ShowError("Lütfen bir kategori seçiniz.");
            return;
        }

        int? cardId = SelectedPaymentMethod?.Card?.Id;
        int? bankId = SelectedPaymentMethod?.Bank?.Id;

        // Skip limit checks in Avalonia to keep it straightforward, or add later when Dialogs are fully ported
        // Since we lack WPF's MessageBox, we'll assume it's OK for now or we could add another layer of confirmation.

        int installmentCount = 1;
        if (IsInstallment && int.TryParse(SelectedInstallmentCount, out int parsed))
        {
            installmentCount = parsed;
        }

        try
        {
            DateTime startDate = SelectedDate ?? DateTime.Today;
            string baseDescription = Description ?? "";
            string? groupId = IsInstallment ? Guid.NewGuid().ToString() : null;

            if (IsInstallment && installmentCount > 1)
            {
                decimal installmentAmount = totalAmount / installmentCount;
                
                for (int i = 0; i < installmentCount; i++)
                {
                    var t = new Transaction
                    {
                        Date = startDate.AddMonths(i),
                        Amount = installmentAmount,
                        CategoryId = SelectedCategory.Id,
                        Description = $"{baseDescription} ({i + 1}/{installmentCount} Taksit)".Trim(),
                        CreditCardAccountId = cardId,
                        BankAccountId = bankId,
                        GroupId = groupId
                    };
                    _context.Transactions.Add(t);
                }
            }
            else
            {
                var transaction = new Transaction
                {
                    Date = startDate,
                    Amount = totalAmount,
                    CategoryId = SelectedCategory.Id,
                    Description = baseDescription,
                    CreditCardAccountId = cardId,
                    BankAccountId = bankId,
                    GroupId = groupId
                };
                _context.Transactions.Add(transaction);
            }

            await _context.SaveChangesAsync();
            _ownerWindow.Close(true); // Close window with True result
        }
        catch (Exception ex)
        {
            ShowError($"İşlem kaydedilirken hata oluştu: {ex.Message}");
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
