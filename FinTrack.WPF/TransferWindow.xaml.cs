using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;

namespace FinTrack.WPF
{
    public partial class TransferWindow : Window
    {
        private readonly AppDbContext _db;

        // Common model for ComboBox items
        private record TransferItem(string Label, BankAccount? Bank, CreditCardAccount? Card)
        {
            public override string ToString() => Label;
        }

        public TransferWindow(AppDbContext db, int? defaultSourceBankId = null)
        {
            InitializeComponent();
            _db = db;

            LoadAccounts();

            if (defaultSourceBankId.HasValue)
            {
                var match = SourceCombo.Items.Cast<TransferItem>().FirstOrDefault(i => i.Bank?.Id == defaultSourceBankId.Value);
                if (match != null) SourceCombo.SelectedItem = match;
            }
        }

        private void LoadAccounts()
        {
            var sourceItems = new List<TransferItem>();
            var targetItems = new List<TransferItem>();

            // 1. Cash (Nakit)
            var cashItem = new TransferItem("💵 Nakit", null, null);
            sourceItems.Add(cashItem);
            targetItems.Add(cashItem);

            // 2. Bank Accounts
            var banks = _db.BankAccounts.Where(b => b.IsActive).OrderBy(b => b.BankName).ToList();
            foreach (var bank in banks)
            {
                var item = new TransferItem($"🏦 {bank.BankName} – {bank.AccountName}", bank, null);
                sourceItems.Add(item);
                targetItems.Add(item);
            }

            // 3. Credit Cards (Usually only targets for paying debt, but technically could be cash advance source. We'll allow both for flexibility)
            var cards = _db.CreditCardAccounts.Where(c => c.IsActive).OrderBy(c => c.BankName).ToList();
            foreach (var card in cards)
            {
                var item = new TransferItem($"💳 {card.BankName} – {card.CardLabel}", null, card);
                sourceItems.Add(item);
                targetItems.Add(item);
            }

            SourceCombo.ItemsSource = sourceItems;
            TargetCombo.ItemsSource = targetItems;

            SourceCombo.SelectedIndex = 0;
            if (targetItems.Count > 1) TargetCombo.SelectedIndex = 1; else TargetCombo.SelectedIndex = 0;
        }

        private void SourceTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Simple validation could go here (e.g., warning if source == target)
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as TextBox);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourceCombo.SelectedItem is not TransferItem source || TargetCombo.SelectedItem is not TransferItem target)
            {
                MessageBox.Show("Lütfen kaynak ve hedef seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (source == target)
            {
                MessageBox.Show("Kaynak ve hedef aynı olamaz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!UIHelper.TryParseAmount(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Geçerli bir tutar giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Ensure "Transfer" category exists
                var transferCategory = _db.Categories.FirstOrDefault(c => c.Type == TransactionType.Transfer)
                                  ?? new Category { Name = "Transfer", Type = TransactionType.Transfer };
                
                if (transferCategory.Id == 0) _db.Categories.Add(transferCategory);

                DateTime date = DatePicker.SelectedDate ?? DateTime.Now;
                string baseDesc = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? "Para Transferi" : DescriptionTextBox.Text;

                // Create Outgoing Transaction (reduces Source)
                var outgoingTx = new Transaction
                {
                    Amount = -amount,
                    Date = date,
                    Description = $"{baseDesc} (Giden: {target.Label})",
                    Category = transferCategory,
                    BankAccountId = source.Bank?.Id,
                    CreditCardAccountId = source.Card?.Id
                };

                // Create Incoming Transaction (increases Target)
                var incomingTx = new Transaction
                {
                    Amount = amount,
                    Date = date,
                    Description = $"{baseDesc} (Gelen: {source.Label})",
                    Category = transferCategory,
                    BankAccountId = target.Bank?.Id,
                    CreditCardAccountId = target.Card?.Id
                    // Note: If Target is a CreditCard, this handles Paying off the debt (positive amount)
                };

                _db.Transactions.Add(outgoingTx);
                _db.Transactions.Add(incomingTx);

                _db.SaveChanges();

                MessageBox.Show("Transfer başarıyla tamamlandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Transfer işlemi sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
