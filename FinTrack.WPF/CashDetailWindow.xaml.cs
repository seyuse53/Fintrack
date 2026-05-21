using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;

namespace FinTrack.WPF
{
    public partial class CashDetailWindow : Window
    {
        private readonly AppDbContext _db;

        public CashDetailWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;

            TitleText.Text = "💵 Nakit Kasa İşlemleri";
            
            LoadData();
        }

        private void LoadData()
        {
            // Cash transactions are those null for both bank and credit card fields
            var transactions = _db.Transactions
                .Include(tx => tx.Category)
                .Where(tx => tx.BankAccountId == null && tx.CreditCardAccountId == null)
                .OrderByDescending(tx => tx.Date)
                .ToList();

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

            // Investment operations directly impacting cash
            var invTxs = _db.InvestmentTransactions
                .Where(t => t.LinkedBankAccountId == null && t.LinkedCreditCardAccountId == null)
                .ToList();

            foreach (var it in invTxs)
            {
                if (it.Type == InvestmentTransactionType.Buy)
                    totalOut += it.TotalCost;
                else if (it.Type == InvestmentTransactionType.Sell)
                    totalIn += it.TotalCost;
            }

            decimal currentBalance = UIHelper.CalculateCashBalance(_db);

            TotalInText.Text = $"+₺{totalIn:N2}";
            TotalOutText.Text = $"-₺{totalOut:N2}";
            CurrentBalanceText.Text = $"₺{currentBalance:N2}";

            TitleText.Text = $"💵 Nakit Kasa İşlemleri ({transactions.Count} kayıt bulundu)";

            // Note: We're only binding standard transactions to the grid for editing simplicity,
            // like the existing account details grid.
            TransactionsGrid.ItemsSource = transactions;
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is Transaction t)
            {
                var editWin = new EditTransactionWindow(_db, t);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true)
                {
                    LoadData();
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
                            LoadData();
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
                        // Because cash transfers could be Cash-to-Bank, tracking the exact pair requires careful matching
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

                    LoadData();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
