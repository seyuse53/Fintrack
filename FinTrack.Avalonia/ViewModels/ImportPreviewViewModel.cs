using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FinTrack.Avalonia.Services;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels
{
    public class ImportItemViewModel : ObservableObject
    {
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    SelectionChanged?.Invoke();
                }
            }
        }

        public Action? SelectionChanged { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsExpense { get; set; } = true;

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public bool IsDuplicate { get; set; }
        public string DuplicateReason { get; set; } = LocalizationService.GetString("ImportPreview_DupeReason");

        public ObservableCollection<Category> AvailableCategories { get; set; } = new();

        public string AmountText => (IsExpense ? "-" : "+") + $"₺{Amount:N2}";
        public string ForegroundColor => IsExpense ? "#C62828" : "#27AE60";
        public string RowBackground => IsDuplicate ? "#FEF9E7" : "White";
        public string RowBorderBrush => IsDuplicate ? "#F9E79F" : "#E0E0E0";
    }

    public partial class ImportPreviewViewModel : ViewModelBase
    {
        private readonly AppDbContext? _context;
        private readonly int? _cardId;
        private readonly int? _accountId;

        public Action? CloseAction { get; set; }

        [ObservableProperty]
        private ObservableCollection<ImportItemViewModel> _items = new();

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

        public ImportPreviewViewModel(int? cardId, int? accountId, List<GeminiParsedTransaction> parsedTransactions)
        {
            _cardId = cardId;
            _accountId = accountId;
            _context = AppDbContext.CreateNew();

            LoadCategoriesAndProcess(parsedTransactions);
        }

        private void LoadCategoriesAndProcess(List<GeminiParsedTransaction> parsedTransactions)
        {
            if (_context == null) return;

            try
            {
                // Fetch visible categories
                var allCategories = _context.Categories
                    .Where(c => c.IsVisible && c.Type != TransactionType.Transfer)
                    .OrderBy(c => c.Name)
                    .ToList();
                var incomeCategories = allCategories.Where(c => c.Type == TransactionType.Income).ToList();
                var expenseCategories = allCategories.Where(c => c.Type == TransactionType.Expense).ToList();

                Categories = new ObservableCollection<Category>(allCategories);

                // Fetch existing transactions in the last 60 days for duplicate check
                var startDate = DateTime.Today.AddDays(-60);
                var existingTransactions = _context.Transactions
                    .Where(t => (_cardId.HasValue && t.CreditCardAccountId == _cardId.Value) || 
                                (_accountId.HasValue && t.BankAccountId == _accountId.Value))
                    .Where(t => t.Date >= startDate)
                    .ToList();

                foreach (var parsed in parsedTransactions)
                {
                    // Parse Date
                    DateTime txDate = DateTime.Today;
                    if (DateTime.TryParse(parsed.Date, out DateTime dt))
                    {
                        txDate = dt;
                    }

                    // Expenses in Gemini response are returned as negative numbers (e.g. -150.50)
                    bool isExpense = parsed.Amount < 0;
                    decimal absoluteAmount = Math.Abs(parsed.Amount);

                    // Suggested Category matching
                    Category? matchedCat = null;
                    if (!string.IsNullOrWhiteSpace(parsed.SuggestedCategory))
                    {
                        // Exact match first
                        matchedCat = allCategories.FirstOrDefault(c =>
                            c.Name.Equals(parsed.SuggestedCategory, StringComparison.OrdinalIgnoreCase));

                        // Partial match fallback
                        if (matchedCat == null)
                        {
                            matchedCat = allCategories.FirstOrDefault(c =>
                                c.Name.Contains(parsed.SuggestedCategory, StringComparison.OrdinalIgnoreCase) ||
                                parsed.SuggestedCategory.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    // Explicit keyword fallback (for cafes, bakeries, markets)
                    if (matchedCat == null && !string.IsNullOrWhiteSpace(parsed.Description))
                    {
                        string descLower = parsed.Description.ToLowerInvariant();
                        if (descLower.Contains("fırın") || descLower.Contains("cafe") || descLower.Contains("kahvaltı") ||
                            descLower.Contains("pasta") || descLower.Contains("market") || descLower.Contains("hertat") ||
                            descLower.Contains("bim") || descLower.Contains("a101") || descLower.Contains("şok") || descLower.Contains("hakmar") || descLower.Contains("happy center"))
                        {
                            matchedCat = allCategories.FirstOrDefault(c => c.Name.Equals("Market & Mutfak", StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    // Fallback matching logic: match by type
                    if (matchedCat == null)
                    {
                        matchedCat = allCategories.FirstOrDefault(c => c.Type == (isExpense ? TransactionType.Expense : TransactionType.Income));
                    }

                    // Duplicate check: Match strictly by Date and Amount. Ignore description as manual entries often differ from bank statements.
                    var matchingTransaction = existingTransactions.FirstOrDefault(et =>
                        et.Date.Date == txDate.Date &&
                        Math.Abs(et.Amount) == absoluteAmount);

                    bool isDuplicate = matchingTransaction != null;
                    if (isDuplicate)
                    {
                        // Remove the matched transaction so it doesn't trigger a duplicate again 
                        // if the statement has multiple identical transactions in the same day.
                        existingTransactions.Remove(matchingTransaction!);
                    }

                    string tooltip = isDuplicate ? string.Format(LocalizationService.GetString("ImportPreview_DupeDetails"), matchingTransaction!.Date, matchingTransaction.Description, Math.Abs(matchingTransaction.Amount)) : LocalizationService.GetString("ImportPreview_DupeReason");

                    Items.Add(new ImportItemViewModel
                    {
                        IsSelected = !isDuplicate, // default uncheck if duplicate
                        Date = txDate,
                        Description = parsed.Description ?? string.Empty,
                        Amount = absoluteAmount,
                        IsExpense = isExpense,
                        AvailableCategories = new ObservableCollection<Category>(isExpense ? expenseCategories : incomeCategories),
                        SelectedCategory = matchedCat,
                        IsDuplicate = isDuplicate,
                        DuplicateReason = tooltip,
                        SelectionChanged = OnSelectionChanged
                    });
                }
                OnSelectionChanged(); // Initial calculation
            }
            catch (Exception ex)
            {
                FinTrack.Core.Helpers.AppLogger.Error($"Error loading categories/duplicate check: {ex.Message}", ex);
            }
        }

        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(TotalSelectedAmountText));
        }

        public string TotalSelectedAmountText
        {
            get
            {
                decimal total = Items.Where(i => i.IsSelected).Sum(i => i.IsExpense ? -i.Amount : i.Amount);
                return total <= 0 ? $"-₺{Math.Abs(total):N2}" : $"+₺{total:N2}";
            }
        }

        [RelayCommand]
        private async Task ImportSelectedAsync()
        {
            if (_context == null) return;

            int importCount = 0;
            try
            {
                foreach (var item in Items.Where(i => i.IsSelected))
                {
                    if (item.SelectedCategory == null) continue;

                    var transaction = new Transaction
                    {
                        CreditCardAccountId = _cardId,
                        BankAccountId = _accountId,
                        Date = item.Date,
                        Description = item.Description,
                        CategoryId = item.SelectedCategory.Id,
                        Amount = item.Amount
                    };

                    _context.Transactions.Add(transaction);
                    importCount++;
                }

                if (importCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                FinTrack.Core.Helpers.AppLogger.Error($"Failed to import: {ex.Message}", ex);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseAction?.Invoke();
        }
    }
}
