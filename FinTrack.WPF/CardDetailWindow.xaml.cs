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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
