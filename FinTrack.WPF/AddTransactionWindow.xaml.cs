using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF
{
    public partial class AddTransactionWindow : Window
    {
        private readonly AppDbContext _context;

        // Item in the payment method combo
        private record PaymentItem(string Label, CreditCardAccount? Card, BankAccount? Bank)
        {
            public override string ToString() => Label;
        }

        public class CategoryViewModel
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
            public required string DisplayName { get; set; }
            public required TransactionType Type { get; set; }
            public bool IsSubCategory { get; set; }
        }

        public AddTransactionWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (_, _) =>
            {
                // Load all categories including hierarchy
                var allCategories = await _context.Categories
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.ParentCategoryId == null ? 0 : 1) // Parent first
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var viewModels = new List<CategoryViewModel>();
                
                // Group by type and then parent
                var parents = allCategories.Where(c => c.ParentCategoryId == null).ToList();
                foreach (var parent in parents)
                {
                    viewModels.Add(new CategoryViewModel 
                    { 
                        Id = parent.Id, 
                        Name = parent.Name, 
                        DisplayName = parent.Name, 
                        Type = parent.Type,
                        IsSubCategory = false
                    });

                    var children = allCategories.Where(c => c.ParentCategoryId == parent.Id).ToList();
                    foreach (var child in children)
                    {
                        viewModels.Add(new CategoryViewModel 
                        { 
                            Id = child.Id, 
                            Name = child.Name, 
                            DisplayName = $"  ↳ {child.Name}", 
                            Type = child.Type,
                            IsSubCategory = true
                        });
                    }
                }

                CategoryComboBox.ItemsSource = viewModels;
                if (viewModels.Any()) CategoryComboBox.SelectedIndex = 0;

                // Load payment methods
                LoadPaymentMethods();
            };
        }

        private void LoadPaymentMethods()
        {
            var items = new List<PaymentItem>
            {
                new("💵 Nakit", null, null)
            };

            var bankAccounts = _context.BankAccounts
                                       .Where(b => b.IsActive)
                                       .OrderBy(b => b.BankName)
                                       .ThenBy(b => b.AccountName)
                                       .ToList();

            foreach (var bank in bankAccounts)
                items.Add(new PaymentItem($"🏦 {bank.BankName} – {bank.AccountName}", null, bank));

            var cards = _context.CreditCardAccounts
                                .Where(c => c.IsActive)
                                .OrderBy(c => c.BankName)
                                .ThenBy(c => c.CardLabel)
                                .ToList();

            foreach (var card in cards)
                items.Add(new PaymentItem($"💳 {card.BankName} – {card.CardLabel}", card, null));

            PaymentMethodCombo.ItemsSource = items;
            PaymentMethodCombo.SelectedIndex = 0; // default: Nakit
        }

        private void PaymentMethod_Changed(object sender, SelectionChangedEventArgs e)
        {
            // No additional UI change needed — card is read at save time
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as TextBox);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UIHelper.TryParseAmount(AmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Lütfen geçerli bir tutar giriniz.", "Doğrulama Hatası");
                return;
            }

            if (CategoryComboBox.SelectedItem is not CategoryViewModel selectedCategory)
            {
                MessageBox.Show("Lütfen bir kategori seçiniz.", "Doğrulama Hatası");
                return;
            }

            int? cardId = null;
            int? bankId = null;

            if (PaymentMethodCombo.SelectedItem is PaymentItem item)
            {
                if (item.Card != null) cardId = item.Card.Id;
                if (item.Bank != null) bankId = item.Bank.Id;
            }

            var transaction = new Transaction
            {
                Date                = DatePicker.SelectedDate ?? System.DateTime.Now,
                Amount              = amount,
                CategoryId          = selectedCategory.Id,
                Description         = DescriptionTextBox.Text,
                CreditCardAccountId = cardId,
                BankAccountId       = bankId
            };

            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"İşlem kaydedilirken hata oluştu: {ex.Message}", "Hata");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
