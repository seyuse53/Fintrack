using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF
{
    public partial class AccountDetailWindow : Window
    {
        public AccountDetailWindow(BankAccount account, IEnumerable<Transaction> transactions)
        {
            InitializeComponent();

            TitleText.Text = $"🏦 {account.BankName} – {account.AccountName}";
            
            decimal totalIn = 0;
            decimal totalOut = 0;

            foreach(var t in transactions)
            {
                if (t.Category?.Type == TransactionType.Income)
                    totalIn += t.Amount;
                else if (t.Category?.Type == TransactionType.Expense)
                    totalOut += t.Amount;
                else if (t.Category?.Type == TransactionType.Transfer)
                {
                    if (t.Amount > 0) totalIn += t.Amount;
                    else totalOut += System.Math.Abs(t.Amount);
                }
            }

            decimal currentBalance = account.InitialBalance + totalIn - totalOut;

            InitialBalanceText.Text = $"₺{account.InitialBalance:N2}";
            TotalInText.Text = $"+₺{totalIn:N2}";
            TotalOutText.Text = $"-₺{totalOut:N2}";
            CurrentBalanceText.Text = $"₺{currentBalance:N2}";

            // Bind to grid, descending order by date
            TransactionsGrid.ItemsSource = transactions.OrderByDescending(t => t.Date).ToList();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
