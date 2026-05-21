using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

public class CardItemViewModel
{
    public int CardId { get; set; }
    public required string BankName { get; set; }
    public required string CardLabel { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal Limit { get; set; }
    public decimal RemainingLimit { get; set; }
    public double LimitProgressValue { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool CanPay { get; set; }
    
    public string StatementDescription { get; set; } = string.Empty;
    public string DaysRemainingText { get; set; } = string.Empty;
    public string LinkedCardsText { get; set; } = string.Empty;
    public double ProgressValue { get; set; }
    public string ProgressColor { get; set; } = "#3498DB";
}

public partial class CardsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private ObservableCollection<CardItemViewModel> _cardsList = new();

    public CardsViewModel()
    {
        _context = App.Services?.GetService<AppDbContext>();
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        if (_context == null) return;
        
        try
        {
            var now = DateTime.Now;
            
            var activeCards = await _context.CreditCardAccounts
                .Where(c => c.IsActive)
                .OrderBy(c => c.BankName)
                .ThenBy(c => c.CardLabel)
                .ToListAsync();

            var cardTransactions = await _context.Transactions
                .Include(t => t.CreditCardAccount)
                .Include(t => t.Category)
                .Where(t => t.CreditCardAccount != null && t.CreditCardAccount.IsActive)
                .ToListAsync();

            var masterCards = activeCards.Where(c => c.ParentCardId == null).ToList();
            var childCards = activeCards.Where(c => c.ParentCardId != null).ToList();

            var newItems = new System.Collections.Generic.List<CardItemViewModel>();

            foreach (var master in masterCards)
            {
                var familyIds = new System.Collections.Generic.List<int> { master.Id };
                var linkedOnes = childCards.Where(c => c.ParentCardId == master.Id).ToList();
                familyIds.AddRange(linkedOnes.Select(c => c.Id));

                decimal consolidatedDebt = cardTransactions
                    .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                    .Sum(t => t.Amount);

                var period = master.GetStatementPeriod(now);
                
                var vm = new CardItemViewModel
                {
                    CardId = master.Id,
                    BankName = master.BankName,
                    CardLabel = master.CardLabel,
                    TotalDebt = consolidatedDebt,
                    Limit = master.Limit,
                    RemainingLimit = master.Limit > 0 ? (master.Limit - consolidatedDebt) : 0,
                    LimitProgressValue = (master.Limit > 0) ? (double)(consolidatedDebt / master.Limit * 100) : 0,
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    CanPay = consolidatedDebt > 0,
                    LinkedCardsText = linkedOnes.Any() 
                        ? "+ " + string.Join(", ", linkedOnes.Select(o => o.CardLabel))
                        : string.Empty
                };

                double totalDays = (period.End - period.Start).TotalDays;
                double elapsedDays = (now - period.Start).TotalDays;
                double remainingDays = (period.End - now).TotalDays;

                if (remainingDays < 0) remainingDays = 0;
                if (elapsedDays < 0) elapsedDays = 0;
                if (totalDays <= 0) totalDays = 1;

                vm.ProgressValue = (elapsedDays / totalDays) * 100;

                if (remainingDays <= 3)
                {
                    vm.ProgressColor = "#E74C3C"; 
                    vm.DaysRemainingText = $"{Math.Ceiling(remainingDays)} Gün Kaldı!";
                }
                else if (remainingDays <= 10)
                {
                    vm.ProgressColor = "#F39C12"; 
                    vm.DaysRemainingText = $"{Math.Ceiling(remainingDays)} Gün Kaldı";
                }
                else
                {
                    vm.ProgressColor = "#3498DB"; 
                    vm.DaysRemainingText = $"{Math.Ceiling(remainingDays)} Gün";
                }

                vm.StatementDescription = $"{period.Start:dd MMM} - {period.End:dd MMM} Ekstresi";
                
                newItems.Add(vm);
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                CardsList.Clear();
                foreach(var item in newItems) CardsList.Add(item);
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}
