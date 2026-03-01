using System.Linq;
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
                              .OrderBy(a => a.BankName)
                              .ThenBy(a => a.AccountName)
                              .ToList();
            AccountsGrid.ItemsSource = accounts;
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
                InitialBalanceBox.Text = account.InitialBalance.ToString("0.##");
                IbanBox.Text = account.IBAN;

                DeleteAccountBtn.IsEnabled = true;
                ToggleActiveBtn.IsEnabled = true;
                UpdateAccountBtn.IsEnabled = true;
                AddAccountBtn.IsEnabled = false; // Prevent adding when something is selected to avoid confusion
            }
            else
            {
                BankNameBox.Clear();
                AccountNameBox.Clear();
                InitialBalanceBox.Clear();
                IbanBox.Clear();

                DeleteAccountBtn.IsEnabled = false;
                ToggleActiveBtn.IsEnabled = false;
                UpdateAccountBtn.IsEnabled = false;
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

            _db.BankAccounts.Add(new BankAccount
            {
                BankName = bank,
                AccountName = accountName,
                IBAN = string.IsNullOrEmpty(iban) ? null : iban,
                InitialBalance = initialBalance,
                IsActive = true
            });
            _db.SaveChanges();

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
            entity.InitialBalance = initialBalance;

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
