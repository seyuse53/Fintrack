using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF
{
    public partial class AccountDetailWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly BankAccount _account;

        public AccountDetailWindow(BankAccount account, IEnumerable<Transaction> transactions, AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _account = account;

            TitleText.Text = $"🏦 {account.BankName} – {account.AccountName}";
            
            // Populate OpeningBalance for this window
            var openingCategory = _db.Categories.FirstOrDefault(c => c.Id == 30 || c.Name == "Açılış Bakiyesi");
            if (openingCategory != null)
            {
                _account.OpeningBalance = _db.Transactions
                    .Where(t => t.BankAccountId == _account.Id && t.CategoryId == openingCategory.Id)
                    .Sum(t => (decimal?)t.Amount) ?? 0;
            }

            LoadData(transactions);
        }

        private void LoadData(IEnumerable<Transaction> transactions)
        {
            decimal totalIn = 0;
            decimal totalOut = 0;

            foreach(var t in transactions)
            {
                if (t.Category?.Type == TransactionType.Income)
                    totalIn += t.Amount;
                else if (t.Category?.Type == TransactionType.Expense)
                    totalOut += t.Amount;
                else if (t.Category?.Type == TransactionType.Transfer)
                {
                    if (t.Amount > 0) totalIn += t.Amount;
                    else totalOut += System.Math.Abs(t.Amount);
                }
            }

            decimal currentBalance = totalIn - totalOut; // InitialBalance is now 0 as it's moved to transactions

            InitialBalanceText.Text = $"₺{_account.OpeningBalance:N2}";
            TotalInText.Text = $"+₺{(totalIn - _account.OpeningBalance):N2}"; // Show other income separately? No, let's keep it simple.
            TotalOutText.Text = $"-₺{totalOut:N2}";
            CurrentBalanceText.Text = $"₺{currentBalance:N2}";

            // Bind to grid, descending order by date
            TransactionsGrid.ItemsSource = transactions.OrderByDescending(t => t.Date).ToList();
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is Transaction t)
            {
                var editWin = new EditTransactionWindow(_db, t);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true)
                {
                    // Dialog automatically saves if successful.
                    // Just refresh this window:
                    var updatedTransactions = _db.Transactions
                        .Include(tx => tx.Category)
                        .Where(tx => tx.BankAccountId == _account.Id)
                        .ToList();

                    LoadData(updatedTransactions);
                }
            }
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is Transaction t)
            {
                var result = MessageBox.Show(
                    $"Bu işlemi silmek istediğinize emin misiniz?\n\n" +
                    $"{t.Description} – ₺{t.Amount:N2}",
                    "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Detect if this is an installment and offer group deletion
                    if (!string.IsNullOrEmpty(t.GroupId))
                    {
                        var groupResult = MessageBox.Show(
                            "Bu işlem bir taksitli işlem grubunun parçası. Tüm taksit grubunu (gelecek aylar dahil) silmek ister misiniz?",
                            "Grup Silme", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (groupResult == MessageBoxResult.Yes)
                        {
                            var groupItems = _db.Transactions.Where(tx => tx.GroupId == t.GroupId).ToList();
                            _db.Transactions.RemoveRange(groupItems);
                            _db.SaveChanges();
                            LoadData(_db.Transactions.Include(tx => tx.Category).Where(tx => tx.BankAccountId == _account.Id).ToList());
                            return;
                        }
                        else if (groupResult == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                    }

                    // Detect if this is a transfer and find the pair
                    if (t.Category?.Type == TransactionType.Transfer)
                    {
                        // Look for a transaction with opposite amount, same date, and "Transfer" type
                        // This handles both Bank-to-Bank and Bank-to-Card transfers
                        var pair = _db.Transactions
                            .FirstOrDefault(tx => tx.Id != t.Id && 
                                                 tx.Date == t.Date && 
                                                 tx.Amount == -t.Amount && 
                                                 tx.CategoryId == t.CategoryId);

                        if (pair != null)
                        {
                            _db.Transactions.Remove(pair);
                        }
                    }

                    _db.Transactions.Remove(t);
                    _db.SaveChanges();

                    // Refresh this window:
                    var updatedTransactions = _db.Transactions
                        .Include(tx => tx.Category)
                        .Where(tx => tx.BankAccountId == _account.Id)
                        .ToList();

                    LoadData(updatedTransactions);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
