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

public class CardDetailItem
{
    public int TransactionId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string ForegroundColor { get; set; } = "#333333";
    public string Icon { get; set; } = string.Empty;
}

public class StatementGroup
{
    public string PeriodName { get; set; } = string.Empty;
    public string TotalDebtDisplay { get; set; } = string.Empty;
    public string TotalPaymentDisplay { get; set; } = string.Empty;
    public string RemainingDebtDisplay { get; set; } = string.Empty;
    public bool IsExpanded { get; set; } = false;
    public ObservableCollection<CardDetailItem> Transactions { get; set; } = new();
}

public partial class CardDetailViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;
    private readonly int _cardId;

    [ObservableProperty]
    private string _cardTitle = string.Empty;

    [ObservableProperty]
    private string _currentDebtDisplay = "₺0,00";

    [ObservableProperty]
    private string _limitDisplay = "₺0,00";

    [ObservableProperty]
    private string _remainingLimitDisplay = "₺0,00";

    [ObservableProperty]
    private ObservableCollection<StatementGroup> _statementGroups = new();

    // Still need a flat list for some operations or bindings if any, but we will mostly use StatementGroups
    [ObservableProperty]
    private ObservableCollection<CardDetailItem> _transactions = new();

    public Action? CloseAction { get; set; }
    public event Action<int>? RequestEditTransaction;
    public event Action? DataChanged;
    public Func<string, Task<bool>>? ConfirmDeleteFunc { get; set; }

    public CardDetailViewModel(int cardId)
    {
        _context = AppDbContext.CreateNew();
        _cardId = cardId;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (_context == null) return;

        decimal currentDebt = 0;
        var items = new List<CardDetailItem>();

        decimal limit = 0;

        var card = await _context.CreditCardAccounts.FindAsync(_cardId);
        if (card != null)
        {
            CardTitle = $"{card.BankName} - {card.CardLabel}";
            
            // If supplementary card has 0 limit, use parent's limit
            if (card.Limit == 0 && card.ParentCardId != null)
            {
                var parent = await _context.CreditCardAccounts.FindAsync(card.ParentCardId);
                limit = parent?.Limit ?? 0;
            }
            else
            {
                limit = card.Limit;
            }
        }

        var familyIds = new List<int> { _cardId };
        var childCards = await _context.CreditCardAccounts.Where(c => c.ParentCardId == _cardId).Select(c => c.Id).ToListAsync();
        familyIds.AddRange(childCards);

        var cardTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.CreditCardAccountId != null && familyIds.Contains(t.CreditCardAccountId.Value))
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        var groupDict = new Dictionary<string, StatementGroup>();
        var sortedGroups = new List<StatementGroup>();

        foreach (var t in cardTransactions)
        {
            string sign = "";
            string color = "#333333";
            
            if (t.Category?.Type == TransactionType.Expense)
            {
                sign = "-"; 
                color = "#C62828";
                currentDebt += t.Amount;
            }
            else if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
            {
                sign = "+"; 
                color = "#27AE60";
                currentDebt -= t.Amount;
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

            var item = new CardDetailItem
            {
                TransactionId = t.Id,
                Date = t.Date,
                Description = desc,
                CategoryName = t.Category?.Name ?? LocalizationService.GetString("Global_Other"),
                Icon = t.Category?.TypeIcon ?? "💳",
                AmountText = $"{sign}₺{Math.Abs(t.Amount):N2}",
                ForegroundColor = color
            };
            items.Add(item);

            // Determine Statement Period for Grouping
            var period = card != null ? card.GetStatementPeriod(t.Date) : (Start: t.Date, End: t.Date);
            string periodKey = $"{period.Start:yyyyMMdd}-{period.End:yyyyMMdd}";
            
            if (!groupDict.TryGetValue(periodKey, out StatementGroup? grp))
            {
                string pName = string.Format(LocalizationService.GetString("Cards_StatementDesc") ?? "{0} - {1} Ekstresi", period.Start.ToString("dd MMM"), period.End.ToString("dd MMM"));
                grp = new StatementGroup { PeriodName = pName };
                groupDict[periodKey] = grp;
                sortedGroups.Add(grp);
            }
            grp.Transactions.Add(item);
        }

        // Calculate totals for each group
        foreach (var grp in sortedGroups)
        {
            decimal gDebt = 0;
            decimal gPay = 0;
            foreach (var tItem in grp.Transactions)
            {
                if (tItem.ForegroundColor == "#C62828") // Expense
                    gDebt += decimal.Parse(tItem.AmountText.Replace("₺", "").Replace("-", ""));
                else if (tItem.ForegroundColor == "#27AE60") // Payment
                    gPay += decimal.Parse(tItem.AmountText.Replace("₺", "").Replace("+", ""));
            }
            grp.TotalDebtDisplay = $"Harcama: ₺{gDebt:N2}";
            grp.TotalPaymentDisplay = $"Ödeme: ₺{gPay:N2}";
            decimal remaining = Math.Max(0, gDebt - gPay);
            grp.RemainingDebtDisplay = $"Kalan: ₺{remaining:N2}";
        }

        // First group should be expanded by default
        if (sortedGroups.Count > 0)
        {
            sortedGroups[0].IsExpanded = true;
        }

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Transactions.Clear();
            foreach (var item in items)
                Transactions.Add(item);
                
            StatementGroups.Clear();
            foreach (var g in sortedGroups)
                StatementGroups.Add(g);
                
            CurrentDebtDisplay = $"₺{Math.Max(0, currentDebt):N2}";
            LimitDisplay = $"₺{limit:N2}";
            decimal remaining = limit - Math.Max(0, currentDebt);
            RemainingLimitDisplay = $"₺{Math.Max(0, remaining):N2}";
        });
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void EditTransaction(CardDetailItem? item)
    {
        if (item == null) return;
        RequestEditTransaction?.Invoke(item.TransactionId);
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(CardDetailItem? item)
    {
        if (item == null || _context == null) return;

        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == item.TransactionId);

            if (transaction != null)
            {
                bool hasGroup = !string.IsNullOrEmpty(transaction.GroupId);
                bool isTransfer = transaction.Category?.Type == TransactionType.Transfer;

                string confirmMessage = LocalizationService.GetString("CardDetail_DeleteConfirm");
                if (hasGroup && isTransfer)
                {
                    confirmMessage = LocalizationService.GetString("CardDetail_TransferDeleteConfirm");
                }
                else if (hasGroup)
                {
                    confirmMessage = LocalizationService.GetString("CardDetail_InstallmentDeleteConfirm");
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
