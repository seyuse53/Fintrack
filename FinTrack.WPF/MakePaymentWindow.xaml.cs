using System;
using System.Linq;
using System.Windows;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF
{
    public partial class MakePaymentWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly CreditCardAccount _card;

        public record PaymentSourceItem(string Label, int? BankAccountId)
        {
            public override string ToString() => Label;
        }

        public MakePaymentWindow(AppDbContext db, CreditCardAccount card, decimal suggestedAmount)
        {
            InitializeComponent();
            _db = db;
            _card = card;

            Title = $"{card.BankName} - {card.CardLabel} Ödemesi";
            MessageText.Text = $"{card.BankName} kredi kartınızın toplam borcu ₺{suggestedAmount:N2}.\nNe kadar ödeme yapmak istiyorsunuz?";
            
            // Suggest the full debt
            if (suggestedAmount > 0)
                AmountBox.Text = suggestedAmount.ToString("N2");

            LoadSources();
        }

        private void LoadSources()
        {
            var sources = new System.Collections.Generic.List<PaymentSourceItem>
            {
                new PaymentSourceItem("💵 Nakit", null)
            };

            var banks = _db.BankAccounts.Where(b => b.IsActive).OrderBy(b => b.BankName).ThenBy(b => b.AccountName).ToList();
            foreach(var bank in banks)
            {
                sources.Add(new PaymentSourceItem($"🏦 {bank.BankName} – {bank.AccountName}", bank.Id));
            }

            SourceCombo.ItemsSource = sources;
            SourceCombo.SelectedIndex = 0;
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Geçerli bir tutar giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Find or create the Transfer category for Credit Card Payment.
                // ID 21 is our seeded category. Let's look for type Transfer just in case ID 21 doesn't exist.
                var payCategory = _db.Categories.FirstOrDefault(c => c.Type == TransactionType.Transfer)
                                  ?? new Category { Name = "Kredi Kartı Ödemesi", Type = TransactionType.Transfer };
                
                if (payCategory.Id == 0) 
                    _db.Categories.Add(payCategory);

                int? sourceBankId = null;
                if (SourceCombo.SelectedItem is PaymentSourceItem source)
                {
                    sourceBankId = source.BankAccountId;
                }

                var paymentTx = new Transaction
                {
                    Amount = -amount, // Payment reduces the card debt
                    Date = DateTime.Now,
                    Description = $"{_card.BankName} Kredi Kartı Ödemesi",
                    Category = payCategory,
                    CreditCardAccountId = _card.Id,
                    BankAccountId = sourceBankId
                };

                _db.Transactions.Add(paymentTx);
                _db.SaveChanges();

                MessageBox.Show("Ödeme başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ödeme işlemi sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
