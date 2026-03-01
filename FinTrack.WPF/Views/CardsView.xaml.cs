using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class CardsView : UserControl
    {
        private AppDbContext _context = null!;

        public CardsView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;
            await LoadCardsAsync();
        }

        public async Task LoadCardsAsync()
        {
            if (_context == null) return;

            try
            {
                var now = DateTime.Now;
                
                // Load active cards
                var activeCards = await _context.CreditCardAccounts
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.BankName)
                    .ThenBy(c => c.CardLabel)
                    .ToListAsync();

                // Load all transactions for these cards to calculate debt
                var cardTransactions = await _context.Transactions
                    .Include(t => t.CreditCardAccount)
                    .Include(t => t.Category)
                    .Where(t => t.CreditCardAccount != null && t.CreditCardAccount.IsActive)
                    .ToListAsync();

                var masterCards = activeCards.Where(c => c.ParentCardId == null).ToList();
                var childCards = activeCards.Where(c => c.ParentCardId != null).ToList();

                var viewModels = new List<CardViewModel>();

                foreach (var master in masterCards)
                {
                    // Group this master and all its children
                    var familyIds = new List<int> { master.Id };
                    var linkedOnes = childCards.Where(c => c.ParentCardId == master.Id).ToList();
                    familyIds.AddRange(linkedOnes.Select(c => c.Id));

                    // Total consolidated debt for the whole family
                    decimal consolidatedDebt = cardTransactions
                        .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                        .Sum(t => t.Amount);

                    var period = master.GetStatementPeriod(now);
                    
                    var vm = new CardViewModel
                    {
                        CardId = master.Id,
                        BankName = master.BankName,
                        CardLabel = master.CardLabel,
                        TotalDebt = consolidatedDebt,
                        PeriodStart = period.Start,
                        PeriodEnd = period.End,
                        CanPay = consolidatedDebt > 0,
                        LinkedCardsText = linkedOnes.Any() 
                            ? "+ " + string.Join(", ", linkedOnes.Select(o => o.CardLabel))
                            : string.Empty
                    };

                    // Calculate progress
                    double totalDays = (period.End - period.Start).TotalDays;
                    double elapsedDays = (now - period.Start).TotalDays;
                    double remainingDays = (period.End - now).TotalDays;

                    // Clamp values
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
                        vm.ProgressColor = "#3Active Blue"; // Use hex or named color
                        vm.ProgressColor = "#3498DB"; 
                        vm.DaysRemainingText = $"{Math.Ceiling(remainingDays)} Gün";
                    }

                    vm.StatementDescription = $"{period.Start:dd MMM} - {period.End:dd MMM} Ekstresi";
                    
                    viewModels.Add(vm);
                }

                CardsItemsControl.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kartlar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ManageCardsButton_Click(object sender, RoutedEventArgs e)
        {
            var oldWindow = new CreditCardAccountsWindow(_context);
            oldWindow.Owner = Window.GetWindow(this);
            oldWindow.ShowDialog();
            
            // Reload data in case cards were added, updated, or deleted
            await LoadCardsAsync();
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int cardId)
            {
                OpenDetailWindow(cardId);
            }
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int cardId)
            {
                var card = _context.CreditCardAccounts.FirstOrDefault(c => c.Id == cardId);
                if (card == null) return;

                // Consolidated debt for parent and all its children
                var childIds = _context.CreditCardAccounts
                    .Where(c => c.ParentCardId == card.Id)
                    .Select(c => c.Id)
                    .ToList();
                
                var familyIds = new List<int> { card.Id };
                familyIds.AddRange(childIds);

                decimal totalDebt = _context.Transactions
                    .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                    .Sum(t => t.Amount);

                if (totalDebt <= 0)
                {
                    MessageBox.Show("Bu kartın borcu bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var payWin = new MakePaymentWindow(_context, card, totalDebt);
                payWin.Owner = Window.GetWindow(this);
                if (payWin.ShowDialog() == true)
                {
                    await LoadCardsAsync(); // Refresh UI
                }
            }
        }

        private void OpenDetailWindow(int cardId)
        {
            var card = _context.CreditCardAccounts.FirstOrDefault(c => c.Id == cardId);
            if (card == null) return;

            var period = card.GetStatementPeriod(DateTime.Now);

            var childIds = _context.CreditCardAccounts
                .Where(c => c.ParentCardId == card.Id)
                .Select(c => c.Id)
                .ToList();

            var familyIds = new List<int> { card.Id };
            familyIds.AddRange(childIds);

            var transactions = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.CreditCardAccount)
                .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0) && t.Date >= period.Start && t.Date <= period.End)
                .OrderByDescending(t => t.Date)
                .ToList();

            decimal totalDebt = _context.Transactions
                .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                .Sum(t => t.Amount);

            var detailWin = new CardDetailWindow(
                $"{card.BankName} – {card.CardLabel}",
                $"{period.Start:dd MMM} - {period.End:dd MMM} Ekstresi",
                transactions,
                totalDebt,
                _context,
                card);

            detailWin.Owner = Window.GetWindow(this);
            detailWin.ShowDialog();
            
            // We use standard async void refresh pattern for dialogs 
            _ = LoadCardsAsync();
        }
    }

    public class CardViewModel
    {
        public int CardId { get; set; }
        public required string BankName { get; set; }
        public required string CardLabel { get; set; }
        public decimal TotalDebt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public bool CanPay { get; set; }
        
        public string StatementDescription { get; set; } = string.Empty;
        public string DaysRemainingText { get; set; } = string.Empty;
        public string LinkedCardsText { get; set; } = string.Empty;
        public double ProgressValue { get; set; }
        public string ProgressColor { get; set; } = "#3498DB";
    }
}
