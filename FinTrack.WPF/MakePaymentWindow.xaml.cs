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
                var payCategory = _db.Categories.FirstOrDefault(c => c.Name == "Kredi Kartı Ödemesi" && c.Type == TransactionType.Transfer)
                                  ?? new Category { Name = "Kredi Kartı Ödemesi", Type = TransactionType.Transfer };
                
                if (payCategory.Id == 0) 
                    _db.Categories.Add(payCategory);

                int? sourceBankId = null;
                if (SourceCombo.SelectedItem is PaymentSourceItem source)
                {
                    sourceBankId = source.BankAccountId;
                }

                if (!sourceBankId.HasValue)
                {
                    if (!FinTrack.WPF.Helpers.UIHelper.CheckCashLimit(_db, amount))
                    {
                        return; // Aborted by user
                    }
                }

                // Create Outgoing Transaction (reduces Source)
                var outgoingTx = new Transaction
                {
                    Amount = -amount,
                    Date = DateTime.Now,
                    Description = $"{_card.BankName} Kredi Kartı Ödemesi (Giden)",
                    Category = payCategory,
                    BankAccountId = sourceBankId,
                    CreditCardAccountId = null
                };

                // Create Incoming Transaction (reduces Card Debt)
                var incomingTx = new Transaction
                {
                    Amount = amount,
                    Date = DateTime.Now,
                    Description = $"{_card.BankName} Kredi Kartı Ödemesi (Gelen)",
                    Category = payCategory,
                    BankAccountId = null,
                    CreditCardAccountId = _card.Id
                };

                // Remove the old single transaction logic and add these two:
                _db.Transactions.Add(outgoingTx);
                _db.Transactions.Add(incomingTx);
                _db.SaveChanges();

                var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Bilgi", "Ödeme başarıyla kaydedildi.");
                infoDialog.Owner = this;
                infoDialog.ShowDialog();
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
