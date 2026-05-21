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
            public required TransactionType Type { get; set; }
            public string TypeIcon => Type == TransactionType.Income ? "💰" : Type == TransactionType.Transfer ? "🔄" : "💸";
            public bool IsSubCategory { get; set; }
        }

        public AddTransactionWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (_, _) =>
            {
                // Load categories (Filter out Transfers and Hidden Categories)
                var allCategories = await _context.Categories
                    .Where(c => c.Type != TransactionType.Transfer && c.IsVisible)
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
            if (PaymentMethodCombo.SelectedItem is PaymentItem item)
            {
                // Show installment option only for credit cards
                InstallmentPanel.Visibility = item.Card != null ? Visibility.Visible : Visibility.Collapsed;
                if (item.Card == null) IsInstallmentCheckBox.IsChecked = false;
            }
        }

        private void IsInstallment_Toggled(object sender, RoutedEventArgs e)
        {
            bool isChecked = IsInstallmentCheckBox.IsChecked == true;
            InstallmentCountLabel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            InstallmentCountCombo.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            InstallmentCountCombo.IsEnabled = isChecked;
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as TextBox);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UIHelper.TryParseAmount(AmountTextBox.Text, out decimal totalAmount))
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

            // Limit Check for Credit Card (Check total amount even if it's installments)
            if (cardId.HasValue && selectedCategory.Type == TransactionType.Expense)
            {
                if (!UIHelper.CheckCardLimit(_context, cardId.Value, totalAmount, this))
                {
                    return; // Aborted by user
                }
            }
            else if (!cardId.HasValue && !bankId.HasValue && selectedCategory.Type == TransactionType.Expense)
            {
                // Check Cash Limit
                if (!UIHelper.CheckCashLimit(_context, totalAmount))
                {
                    return; // Aborted by user
                }
            }

            bool isInstallment = IsInstallmentCheckBox.IsChecked == true && cardId.HasValue;
            int installmentCount = 1;
            if (isInstallment && InstallmentCountCombo.SelectedItem is ComboBoxItem countItem)
            {
                int.TryParse(countItem.Content.ToString(), out installmentCount);
            }

            try
            {
                DateTime startDate = DatePicker.SelectedDate ?? System.DateTime.Now;
                string baseDescription = DescriptionTextBox.Text ?? "";
                string? groupId = isInstallment ? System.Guid.NewGuid().ToString() : null;

                if (isInstallment && installmentCount > 1)
                {
                    decimal installmentAmount = totalAmount / installmentCount;
                    
                    for (int i = 0; i < installmentCount; i++)
                    {
                        var t = new Transaction
                        {
                            Date = startDate.AddMonths(i),
                            Amount = installmentAmount,
                            CategoryId = selectedCategory.Id,
                            Description = $"{baseDescription} ({i + 1}/{installmentCount} Taksit)".Trim(),
                            CreditCardAccountId = cardId,
                            BankAccountId = bankId,
                            GroupId = groupId
                        };
                        _context.Transactions.Add(t);
                    }
                }
                else
                {
                    var transaction = new Transaction
                    {
                        Date = startDate,
                        Amount = totalAmount,
                        CategoryId = selectedCategory.Id,
                        Description = baseDescription,
                        CreditCardAccountId = cardId,
                        BankAccountId = bankId,
                        GroupId = groupId
                    };
                    _context.Transactions.Add(transaction);
                }

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
