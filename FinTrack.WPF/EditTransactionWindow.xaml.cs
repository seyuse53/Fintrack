using System.Windows;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF
{
    /// <summary>
    /// Interaction logic for EditTransactionWindow.xaml
    /// </summary>
    public partial class EditTransactionWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Transaction _transactionToEdit;

        public class CategoryViewModel
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
            public required TransactionType Type { get; set; }
            public string TypeIcon => Type == TransactionType.Income ? "💰" : Type == TransactionType.Transfer ? "🔄" : "💸";
            public bool IsSubCategory { get; set; }
        }

        // Item in the payment method combo
        private record PaymentItem(string Label, CreditCardAccount? Card, BankAccount? Bank)
        {
            public override string ToString() => Label;
        }

        public EditTransactionWindow(AppDbContext context, Transaction transactionToEdit)
        {
            InitializeComponent();
            _context = context;
            _transactionToEdit = transactionToEdit;
            Loaded += EditTransactionWindow_Loaded;
        }

        private async void EditTransactionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var allCategories = await _context.Categories
                    .Where(c => c.IsVisible || c.Id == _transactionToEdit.CategoryId) 
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.ParentCategoryId == null ? 0 : 1)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var viewModels = new List<CategoryViewModel>();
                var parents = allCategories.Where(c => c.ParentCategoryId == null).ToList();
                foreach (var parent in parents)
                {
                    viewModels.Add(new CategoryViewModel { Id = parent.Id, Name = parent.Name, Type = parent.Type, IsSubCategory = false });
                    var children = allCategories.Where(c => c.ParentCategoryId == parent.Id).ToList();
                    foreach (var child in children)
                    {
                        viewModels.Add(new CategoryViewModel { Id = child.Id, Name = child.Name, Type = child.Type, IsSubCategory = true });
                    }
                }

                CategoryComboBox.ItemsSource = viewModels;
                
                // Populate data
                DatePicker.SelectedDate = _transactionToEdit.Date;
                AmountTextBox.Text = Math.Abs(_transactionToEdit.Amount).ToString("0.##");
                UIHelper.FormatAmountTextBox(AmountTextBox);
                DescriptionTextBox.Text = _transactionToEdit.Description;
                
                CategoryComboBox.SelectedValue = _transactionToEdit.CategoryId;

                // Load payment methods
                LoadPaymentMethods();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Kategoriler yüklenirken hata oluştu: {ex.Message}", "Hata");
            }
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

            // Select the current payment method
            PaymentMethodCombo.SelectedIndex = 0; // default: Nakit
            if (_transactionToEdit.CreditCardAccountId != null)
            {
                var match = items.FirstOrDefault(i => i.Card?.Id == _transactionToEdit.CreditCardAccountId);
                if (match != null) PaymentMethodCombo.SelectedItem = match;
            }
            else if (_transactionToEdit.BankAccountId != null)
            {
                var match = items.FirstOrDefault(i => i.Bank?.Id == _transactionToEdit.BankAccountId);
                if (match != null) PaymentMethodCombo.SelectedItem = match;
            }
        }

        private void PaymentMethod_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Handled on save
        }

        private void AmountTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as System.Windows.Controls.TextBox);
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

            // Update transaction
            _transactionToEdit.Date = DatePicker.SelectedDate ?? DateTime.Now;
            
            decimal finalAmount = Math.Abs(amount);
            if (selectedCategory.Type == TransactionType.Transfer && _transactionToEdit.Amount < 0)
            {
                // If it was originally an outgoing transfer (like credit card payment), keep it negative
                finalAmount = -finalAmount;
            }
            
            _transactionToEdit.Amount = finalAmount;
            _transactionToEdit.CategoryId = selectedCategory.Id;
            _transactionToEdit.Description = DescriptionTextBox.Text;

            int? cardId = null;
            int? bankId = null;

            if (PaymentMethodCombo.SelectedItem is PaymentItem item)
            {
                if (item.Card != null) cardId = item.Card.Id;
                if (item.Bank != null) bankId = item.Bank.Id;
            }

            // Limit Check for Credit Card
            if (cardId.HasValue && selectedCategory.Type == TransactionType.Expense)
            {
                // Note: We subtract the original amount because we are checking the new total after update
                decimal addedDebt = amount;
                if (_transactionToEdit.CreditCardAccountId == cardId)
                {
                    // If it was already on this card, the "CalculateCardDebt" already includes the OLD amount.
                    // So we check: CurrentDebt - OldAmount + NewAmount > Limit
                    addedDebt = amount - _transactionToEdit.Amount;
                }

                if (addedDebt > 0) // Only check if debt is increasing
                {
                    if (!UIHelper.CheckCardLimit(_context, cardId.Value, addedDebt, this))
                    {
                        return; // Aborted by user
                    }
                }
            }

            _transactionToEdit.CreditCardAccountId = cardId;
            _transactionToEdit.BankAccountId = bankId;

            try
            {
                _context.Transactions.Update(_transactionToEdit);
                await _context.SaveChangesAsync();
                
                DialogResult = true; // Closes window and returns success
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem güncellenirken hata oluştu: {ex.Message}", "Hata");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
