using System.Collections.Generic;
using System.Windows;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF
{
    public partial class CardDetailWindow : Window
    {
        private readonly AppDbContext? _db;
        private readonly CreditCardAccount? _card;
        private readonly decimal _totalDebt;

        public CardDetailWindow(string cardLabel, string monthLabel,
                                IEnumerable<Transaction> transactions, decimal total, 
                                AppDbContext? db = null, CreditCardAccount? card = null)
        {
            InitializeComponent();
            _db = db;
            _card = card;
            _totalDebt = total;

            TitleText.Text    = $"💳 {cardLabel}";
            SubtitleText.Text = $"{monthLabel}  ·  G.Toplam: ₺{total:N2}";
            TransactionsGrid.ItemsSource = transactions;

            // Only show payment button if we are viewing a specific card (not a grouped bank row)
            if (_card != null && _db != null && total > 0)
            {
                PayButton.Visibility = Visibility.Visible;
            }
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _card == null) return;

            var payWin = new MakePaymentWindow(_db, _card, _totalDebt);
            payWin.Owner = this;
            if (payWin.ShowDialog() == true)
            {
                // We should ideally reload data, but for now we just close and they can reopen
                MessageBox.Show("Ödeme yapıldı. Ekran güncellenmesi için Ana Ekran'a dönülecek.", "Bilgi");
                DialogResult = true;
                Close();
            }
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is Transaction t && _db != null)
            {
                var editWin = new EditTransactionWindow(_db, t);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true)
                {
                    // For card view, closing is safer because recalculating the exact period debt 
                    // is complex without reloading from Account/Card view level.
                    var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Bilgi", "İşlem güncellendi. Değişikliklerin yansıması için detay ekranı kapatılacak.");
                    infoDialog.Owner = this;
                    infoDialog.ShowDialog();
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is Transaction t && _db != null)
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
                            
                            DialogResult = true;
                            Close();
                            return;
                        }
                        else if (groupResult == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                    }

                    // Detect if this is a transfer (e.g., Credit Card Payment)
                    if (t.Category?.Type == TransactionType.Transfer)
                    {
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

                    var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Bilgi", "İşlem ve varsa transfer eşi silindi. Değişikliklerin yansıması için detay ekranı kapatılacak.");
                    infoDialog.Owner = this;
                    infoDialog.ShowDialog();
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
