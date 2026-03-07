using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF.Views
{
    public partial class DashboardView : UserControl
    {
        private AppDbContext _context = null!;

        public DashboardView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (_context == null) return;

            try
            {
                var now = DateTime.Now;
                int selectedYear = MainWindow.GlobalSelectedYear;
                int selectedMonth = MainWindow.GlobalSelectedMonth;

                // Fetch ALL transactions within the date range
                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                        .ThenInclude(c => c!.ParentCategory)
                    .Include(t => t.CreditCardAccount)
                    .Where(t => t.Date.Year == selectedYear && t.Date.Month == selectedMonth)
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();

                var incomeList  = transactions.Where(t => t.Category?.Type == TransactionType.Income).ToList();
                var expenseList = transactions.Where(t => t.Category?.Type == TransactionType.Expense).ToList();

                IncomeGrid.ItemsSource  = incomeList;
                ExpenseGrid.ItemsSource = expenseList;

                // Load all credit card transactions to calculate true rolling debt
                var allCardTransactions = await _context.Transactions
                    .Include(t => t.CreditCardAccount)
                    .Include(t => t.Category)
                        .ThenInclude(c => c!.ParentCategory)
                    .Where(t => t.CreditCardAccount != null)
                    .ToListAsync();

                decimal cardTotal = allCardTransactions.Sum(t => t.Amount);

                decimal totalIncome  = incomeList.Sum(t => t.Amount) + cardTotal;
                decimal totalExpense = expenseList.Sum(t => t.Amount);

                // Fetch bank accounts to add their initial balances to the total wealth
                var bankAccounts = await _context.BankAccounts.Where(b => b.IsActive).ToListAsync();
                decimal bankInitialBalances = bankAccounts.Sum(b => b.InitialBalance);

                decimal balance = totalIncome - totalExpense + bankInitialBalances;

                IncomeCardText.Text  = $"₺{totalIncome:N2}";
                ExpenseCardText.Text = $"₺{totalExpense:N2}";
                BalanceCardText.Text = $"₺{balance:N2}";

                // Update headers to reflect the selected month
                var culture = new System.Globalization.CultureInfo("tr-TR");
                string monthName = culture.DateTimeFormat.GetAbbreviatedMonthName(selectedMonth).ToUpper();
                IncomeGridHeader.Text = $"🟢 ALACAK ({monthName} {selectedYear})";
                ExpenseGridHeader.Text = $"🔴 BORÇ ({monthName} {selectedYear})";

                LoadCardSummary(allCardTransactions);
                LoadBillBreakdown(expenseList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCardSummary(List<Transaction> transactions)
        {
            var now = DateTime.Now;
            var finalRows = new List<CardSummaryRow>();

            // Get active cards and figure out hierarchy
            var allCards = _context.CreditCardAccounts.ToList();
            var masterCards = allCards.Where(c => c.ParentCardId == null && c.IsActive).ToList();

            var bankGroups = allCards
                .Where(c => c.IsActive)
                .GroupBy(c => c.BankName)
                .OrderBy(g => g.Key);

            foreach (var bankGroup in bankGroups)
            {
                var bankTransactions = transactions.Where(t => t.CreditCardAccount?.BankName == bankGroup.Key).ToList();
                decimal bankTotal = bankTransactions.Sum(t => t.Amount);

                finalRows.Add(new CardSummaryRow(
                    $"[ {bankGroup.Key} ]", 
                    bankTotal, 
                    IsHeader: true, 
                    BankName: bankGroup.Key));

                // Show Master cards for this bank
                var mastersInBank = masterCards.Where(m => m.BankName == bankGroup.Key).OrderBy(m => m.CardLabel);

                foreach (var master in mastersInBank)
                {
                    // Consolidate IDs for this master and its linked cards
                    var familyIds = new List<int> { master.Id };
                    familyIds.AddRange(allCards.Where(c => c.ParentCardId == master.Id).Select(c => c.Id));

                    decimal familyTotal = bankTransactions
                        .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                        .Sum(t => t.Amount);

                    finalRows.Add(new CardSummaryRow(
                        $"    {master.CardLabel}", 
                        familyTotal, 
                        IsHeader: false, 
                        BankName: bankGroup.Key, 
                        CardLabel: master.CardLabel));
                }
            }

            CardSummaryList.ItemsSource = finalRows;
            decimal total = finalRows.Where(r => r.IsHeader).Sum(r => r.Total);
            TotalCardLabel.Text = $"₺{total:N0}";
        }

        private async void EditTransaction_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var dataGrid = contextMenu?.PlacementTarget as DataGrid;

            if (dataGrid?.SelectedItem is Transaction selectedTransaction)
            {
                var editWindow = new EditTransactionWindow(_context, selectedTransaction);
                if (editWindow.ShowDialog() == true)
                    await LoadDataAsync();
            }
        }

        private void LoadBillBreakdown(List<Transaction> expenses)
        {
            // ID 5 = Faturalar. Find all transactions that belong to Category 5 OR its children.
            var billCategory = _context.Categories.FirstOrDefault(c => c.Id == 5);
            if (billCategory == null) return;

            var billSubCategoryIds = _context.Categories
                .Where(c => c.ParentCategoryId == 5)
                .Select(c => c.Id)
                .ToList();

            var billExpenses = expenses
                .Where(e => e.CategoryId == 5 || billSubCategoryIds.Contains(e.CategoryId))
                .ToList();

            if (!billExpenses.Any())
            {
                BillDistributionPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Group by Category Name
            var breakdown = billExpenses
                .GroupBy(e => (e.Category?.Name) ?? "Diğer")
                .Select(g => new { Name = g.Key, Total = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            BillBreakdownList.ItemsSource = breakdown;
            BillDistributionPanel.Visibility = Visibility.Visible;
        }

        private async void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var dataGrid = contextMenu?.PlacementTarget as DataGrid;

            if (dataGrid?.SelectedItem is Transaction selectedTransaction)
            {
                var result = MessageBox.Show(
                    $"Bu işlemi silmek istediğinize emin misiniz?\n\n" +
                    $"{selectedTransaction.Description} – ₺{selectedTransaction.Amount:N2}",
                    "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.Transactions.Remove(selectedTransaction);
                        await _context.SaveChangesAsync();
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Silme hatası: {ex.Message}", "Hata",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CardSummaryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardSummaryList.SelectedItem is not CardSummaryRow selectedRow) return;
            CardSummaryList.SelectedItem = null;

            var now = DateTime.Now;
            
            // Re-fetch card entity to get the StatementDay
            var cardEntity = _context.CreditCardAccounts.FirstOrDefault(c => 
                c.BankName == selectedRow.BankName && 
                (selectedRow.IsHeader || c.CardLabel == selectedRow.CardLabel));

            if (cardEntity == null) return;

            var period = cardEntity.GetStatementPeriod(now);

            // Fetch IDs for the family (master and its children)
            var familyIds = new List<int> { cardEntity.Id };
            if (selectedRow.IsHeader || cardEntity.ParentCardId == null)
            {
                 var childIds = _context.CreditCardAccounts
                    .Where(c => c.ParentCardId == cardEntity.Id)
                    .Select(c => c.Id)
                    .ToList();
                 familyIds.AddRange(childIds);
            }

            // Fetch transactions for the current statement period of this family
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.CreditCardAccount)
                .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0)
                         && t.Date >= period.Start 
                         && t.Date <= period.End);

            if (!selectedRow.IsHeader && selectedRow.CardLabel != null && familyIds.Count == 1) // Force single card if it was just a label and it has a parent (not applicable in current Master-only dashboard logic, but safe)
            {
                query = query.Where(t => t.CreditCardAccount!.CardLabel == selectedRow.CardLabel);
            }

            var transactions = query.OrderByDescending(t => t.Date).ToList();

            var detailWin = new CardDetailWindow(
                selectedRow.IsHeader ? selectedRow.BankName : $"{selectedRow.BankName} – {selectedRow.CardLabel}",
                $"{period.Start:dd MMM yyyy} - {period.End:dd MMM yyyy} Ekstresi",
                transactions,
                selectedRow.Total,
                _context,
                selectedRow.IsHeader ? null : cardEntity);
            
            detailWin.Owner = Window.GetWindow(this);
            detailWin.ShowDialog();
        }
    }

    public record CardSummaryRow(string Label, decimal Total, bool IsHeader, string BankName, string? CardLabel = null);
}
