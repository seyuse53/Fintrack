using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class AccountsView : UserControl
    {
        private AppDbContext _context = null!;

        public AccountsView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;
            await LoadAccountsAsync();
        }

        public async Task LoadAccountsAsync()
        {
            if (_context == null) return;

            try
            {
                var activeAccounts = await _context.BankAccounts
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.BankName)
                    .ThenBy(a => a.AccountName)
                    .ToListAsync();

                // Load all transactions to calculate balances
                // For a bank account, balance = InitialBalance + Income - Expense
                // Note: We need to consider Transfer transactions carefully (they could be coming in or out)
                var allBankTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.BankAccountId != null)
                    .ToListAsync();
                    
                var allInvestmentTransactions = await _context.InvestmentTransactions
                    .Where(t => t.LinkedBankAccountId != null)
                    .ToListAsync();

                var viewModels = new List<AccountViewModel>();

                foreach (var account in activeAccounts)
                {
                    var accountTransactions = allBankTransactions
                        .Where(t => t.BankAccountId == account.Id)
                        .ToList();

                    // Calculate current balance based on transaction types
                    decimal currentBalance = account.InitialBalance;

                    foreach (var t in accountTransactions)
                    {
                        if (t.Category?.Type == TransactionType.Income)
                        {
                            currentBalance += t.Amount;
                        }
                        else if (t.Category?.Type == TransactionType.Expense)
                        {
                            currentBalance -= t.Amount;
                        }
                        else if (t.Category?.Type == TransactionType.Transfer)
                        {
                            // Transfer transactions are already saved with the correct sign (+ for incoming, - for outgoing)
                            // because we save pairs of transactions in TransferWindow.cs.
                            // Simply add the Amount to the currentBalance.
                            currentBalance += t.Amount;
                        }
                    }

                    var invTransactions = allInvestmentTransactions
                        .Where(t => t.LinkedBankAccountId == account.Id)
                        .ToList();

                    foreach (var invT in invTransactions)
                    {
                        if (invT.Type == InvestmentTransactionType.Buy)
                        {
                            currentBalance -= invT.TotalCost;
                        }
                        else if (invT.Type == InvestmentTransactionType.Sell)
                        {
                            currentBalance += invT.TotalCost;
                        }
                    }

                    viewModels.Add(new AccountViewModel
                    {
                        AccountId = account.Id,
                        BankName = account.BankName,
                        AccountName = account.AccountName,
                        IBAN = Helpers.UIHelper.FormatIban(account.IBAN ?? string.Empty),
                        CurrentBalance = currentBalance
                    });
                }

                AccountsItemsControl.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ManageAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            var accountsWin = new BankAccountsWindow(_context);
            accountsWin.Owner = Window.GetWindow(this);
            if (accountsWin.ShowDialog() == true)
            {
                await LoadAccountsAsync();
            }
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int accountId)
            {
                var account = _context.BankAccounts.FirstOrDefault(a => a.Id == accountId);
                if (account == null) return;

                var transactions = _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.BankAccountId == account.Id)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                var detailWin = new AccountDetailWindow(account, transactions, _context);
                detailWin.Owner = Window.GetWindow(this);
                detailWin.ShowDialog();
                
                // Refresh accounts view after closing details (in case of edits)
                _ = LoadAccountsAsync();
            }
        }

        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int accountId)
            {
                var transferWin = new TransferWindow(_context, accountId);
                transferWin.Owner = Window.GetWindow(this);
                if (transferWin.ShowDialog() == true)
                {
                    await LoadAccountsAsync(); // Refresh UI
                }
            }
        }
    }

    public class AccountViewModel
    {
        public int AccountId { get; set; }
        public required string BankName { get; set; }
        public required string AccountName { get; set; }
        public string IBAN { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
    }
}
