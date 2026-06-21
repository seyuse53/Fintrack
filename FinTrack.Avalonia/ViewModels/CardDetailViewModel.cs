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
    private ObservableCollection<CardDetailItem> _transactions = new();

    public Action? CloseAction { get; set; }
    public event Action<int>? RequestEditTransaction;
    public event Action? DataChanged;
    public Func<string, Task<bool>>? ConfirmDeleteFunc { get; set; }

    public CardDetailViewModel(int cardId)
    {
        _context = App.Services?.GetService<AppDbContext>();
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
            CardTitle = $"{card.BankName} - {card.CardLabel} Detayları";
            
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

        foreach (var t in cardTransactions)
        {
            string sign = "";
            string color = "#333333";
            
            // For credit cards:
            // Expense means spending from the card -> Increases debt
            if (t.Category?.Type == TransactionType.Expense)
            {
                sign = "-"; // Represents spending (debt)
                color = "#C62828";
                currentDebt += t.Amount;
            }
            // Income/Transfer means paying to the card (or refund) -> Decreases debt
            else if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
            {
                sign = "+"; // Represents payment (clearing debt)
                color = "#27AE60";
                currentDebt -= t.Amount;
            }

            items.Add(new CardDetailItem
            {
                TransactionId = t.Id,
                Date = t.Date,
                Description = t.Description ?? "Kredi Kartı İşlemi",
                CategoryName = t.Category?.Name ?? "Diğer",
                Icon = t.Category?.TypeIcon ?? "💳",
                AmountText = $"{sign}₺{Math.Abs(t.Amount):N2}",
                ForegroundColor = color
            });
        }

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Transactions.Clear();
            foreach (var item in items)
                Transactions.Add(item);
                
            CurrentDebtDisplay = $"₺{Math.Max(0, currentDebt):N2}"; // Avoid showing negative debt in display
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

                string confirmMessage = "Bu işlemi kalıcı olarak silmek istediğinize emin misiniz?";
                if (hasGroup && isTransfer)
                {
                    confirmMessage = "Bu bir transfer/ödeme/taksit işlemidir. Sildiğinizde bağlantılı tüm kayıtlar da otomatik olarak silinecektir.\n\nEmin misiniz?";
                }
                else if (hasGroup)
                {
                    confirmMessage = "Bu işlem bir taksit grubuna aittir. Sildiğinizde bu gruba ait tüm taksitler silinecektir.\n\nEmin misiniz?";
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
            System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
        }
    }

    public async Task RefreshAsync()
    {
        await LoadDataAsync();
        DataChanged?.Invoke();
    }
}
