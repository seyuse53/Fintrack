using System.Linq;
using Microsoft.EntityFrameworkCore;

using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF
{
    public partial class BankAccountsWindow : Window
    {
        private readonly AppDbContext _db;

        public BankAccountsWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            var accounts = _db.BankAccounts
                              .Include(a => a.Transactions)
                              .OrderBy(a => a.BankName)
                              .ThenBy(a => a.AccountName)
                              .ToList();
            
            // Populate OpeningBalance from transactions for display
            var openingCategory = _db.Categories.FirstOrDefault(c => c.Id == 30 || c.Name == "Açılış Bakiyesi");
            
            foreach (var account in accounts)
            {
                if (openingCategory != null)
                {
                    account.OpeningBalance = _db.Transactions
                        .Where(t => t.BankAccountId == account.Id && t.CategoryId == openingCategory.Id)
                        .Sum(t => (decimal?)t.Amount) ?? 0;
                }

                // Format existing IBANs correctly
                if (!string.IsNullOrEmpty(account.IBAN))
                {
                    string formatted = FinTrack.WPF.Helpers.UIHelper.FormatIban(account.IBAN);
                    if (account.IBAN != formatted)
                    {
                        account.IBAN = formatted;
                    }
                }
            }

            _db.SaveChanges(); // Persist any IBAN formatting changes
            AccountsGrid.ItemsSource = accounts;
        }

        private void IbanBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FinTrack.WPF.Helpers.UIHelper.FormatIbanTextBox(sender as TextBox);
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrWhiteSpace(BankNameBox.Text) &&
                           !string.IsNullOrWhiteSpace(AccountNameBox.Text) &&
                           !string.IsNullOrWhiteSpace(InitialBalanceBox.Text);

            AddAccountBtn.IsEnabled = hasText;
            
            // If an item is selected, we can update it
            if (AccountsGrid.SelectedItem != null)
            {
                UpdateAccountBtn.IsEnabled = hasText;
            }
        }

        private void AccountsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountsGrid.SelectedItem is BankAccount account)
            {
                BankNameBox.Text = account.BankName;
                AccountNameBox.Text = account.AccountName;
                InitialBalanceBox.Text = account.OpeningBalance.ToString("0.##");
                IbanBox.Text = account.IBAN;
                IsCryptoCheckBox.IsChecked = account.IsCryptoExchange;

                DeleteAccountBtn.IsEnabled = true;
                ToggleActiveBtn.IsEnabled = true;
                UpdateAccountBtn.IsEnabled = true;
                ResetOpeningBtn.IsEnabled = account.OpeningBalance != 0;
                AddAccountBtn.IsEnabled = false; // Prevent adding when something is selected to avoid confusion
            }
            else
            {
                BankNameBox.Clear();
                AccountNameBox.Clear();
                InitialBalanceBox.Clear();
                IbanBox.Clear();
                IsCryptoCheckBox.IsChecked = false;

                DeleteAccountBtn.IsEnabled = false;
                ToggleActiveBtn.IsEnabled = false;
                UpdateAccountBtn.IsEnabled = false;
                ResetOpeningBtn.IsEnabled = false;
            }
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            string bank = BankNameBox.Text.Trim();
            string accountName = AccountNameBox.Text.Trim();
            string iban = IbanBox.Text.Trim();

            if (!decimal.TryParse(InitialBalanceBox.Text, out decimal initialBalance))
            {
                MessageBox.Show("Lütfen geçerli bir açılış bakiyesi giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool exists = _db.BankAccounts.Any(a => a.BankName == bank && a.AccountName == accountName);
            if (exists)
            {
                MessageBox.Show("Bu hesap adı ile bu bankada zaten bir kayıt var.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newAccount = new BankAccount
            {
                BankName = bank,
                AccountName = accountName,
                IBAN = string.IsNullOrEmpty(iban) ? null : iban,
                InitialBalance = 0, // Legacy field remains 0
                IsActive = true,
                IsCryptoExchange = IsCryptoCheckBox.IsChecked ?? false
            };

            _db.BankAccounts.Add(newAccount);
            _db.SaveChanges(); // Save to get ID

            // Create Opening Balance Transaction if amount > 0
            if (initialBalance != 0)
            {
                var openingCategory = _db.Categories.FirstOrDefault(c => c.Id == 30 || c.Name == "Açılış Bakiyesi");
                if (openingCategory == null)
                {
                    openingCategory = new Category { Name = "Açılış Bakiyesi", Type = TransactionType.Income };
                    _db.Categories.Add(openingCategory);
                    _db.SaveChanges();
                }

                _db.Transactions.Add(new Transaction
                {
                    BankAccountId = newAccount.Id,
                    CategoryId = openingCategory.Id,
                    Amount = initialBalance,
                    Date = DateTime.Now,
                    Description = "Açılış Bakiyesi"
                });
                _db.SaveChanges();
            }

            AccountsGrid.SelectedItem = null; // Clear selection to reset fields
            LoadAccounts();
        }

        private void UpdateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsGrid.SelectedItem is not BankAccount account) return;

            string bank = BankNameBox.Text.Trim();
            string accountName = AccountNameBox.Text.Trim();
            string iban = IbanBox.Text.Trim();

            if (!decimal.TryParse(InitialBalanceBox.Text, out decimal initialBalance))
            {
                MessageBox.Show("Lütfen geçerli bir açılış bakiyesi giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entity = _db.BankAccounts.Find(account.Id);
            if (entity == null) return;

            entity.BankName = bank;
            entity.AccountName = accountName;
            entity.IBAN = string.IsNullOrEmpty(iban) ? null : iban;
            entity.InitialBalance = 0; // Ensure legacy field is 0
            entity.IsCryptoExchange = IsCryptoCheckBox.IsChecked ?? false;

            // Update or Create Opening Balance Transaction
            var openingCategory = _db.Categories.FirstOrDefault(c => c.Id == 30 || c.Name == "Açılış Bakiyesi");
            if (openingCategory == null)
            {
                openingCategory = new Category { Name = "Açılış Bakiyesi", Type = TransactionType.Income };
                _db.Categories.Add(openingCategory);
                _db.SaveChanges();
            }

            var openingTx = _db.Transactions.FirstOrDefault(t => t.BankAccountId == entity.Id && t.CategoryId == openingCategory.Id);
            if (openingTx != null)
            {
                openingTx.Amount = initialBalance;
            }
            else if (initialBalance != 0)
            {
                _db.Transactions.Add(new Transaction
                {
                    BankAccountId = entity.Id,
                    CategoryId = openingCategory.Id,
                    Amount = initialBalance,
                    Date = DateTime.Now,
                    Description = "Açılış Bakiyesi"
                });
            }

            _db.SaveChanges();
            
            AccountsGrid.SelectedItem = null;
            LoadAccounts();
            MessageBox.Show("Hesap bilgileri güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsGrid.SelectedItem is not BankAccount account) return;

            var entity = _db.BankAccounts.Find(account.Id);
            if (entity == null) return;

            entity.IsActive = !entity.IsActive;
            _db.SaveChanges();
            
            AccountsGrid.SelectedItem = null;
            LoadAccounts();
        }

        private void ResetOpening_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsGrid.SelectedItem is not BankAccount account) return;

            var result = MessageBox.Show(
                $"{account.BankName} – {account.AccountName} hesabının açılış bakiyesini sıfırlamak istiyor musunuz?",
                "Bakiyeyi Sıfırla", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var openingCategory = _db.Categories.FirstOrDefault(c => c.Id == 30 || c.Name == "Açılış Bakiyesi");
            if (openingCategory == null) return;

            var openingTxs = _db.Transactions.Where(t => t.BankAccountId == account.Id && t.CategoryId == openingCategory.Id).ToList();
            
            if (openingTxs.Any())
            {
                _db.Transactions.RemoveRange(openingTxs);
                _db.SaveChanges();
            }

            InitialBalanceBox.Text = "0";
            LoadAccounts();
            MessageBox.Show("Açılış bakiyesi sıfırlandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsGrid.SelectedItem is not BankAccount account) return;

            var result = MessageBox.Show(
                $"{account.BankName} – {account.AccountName} hesabını silmek istiyor musunuz?\n" +
                "Bu hesapla ilişkili işlemler korunacak ancak banka hesap bilgisi silinecektir.",
                "Hesap Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var entity = _db.BankAccounts.Find(account.Id);
            if (entity == null) return;

            _db.BankAccounts.Remove(entity);
            _db.SaveChanges();
            
            AccountsGrid.SelectedItem = null;
            LoadAccounts();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
