using System;
using System.Linq;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

class Program
{
    static void Main()
    {
        // Need to load the profile and get correct path
        var dbPath = @"C:\VSRepos\FinTrack\LocalData\fintrack_K_private.db";
        var password = FinTrack.Core.Services.SettingsManager.ActiveDataKey;

        var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();

        if (!string.IsNullOrEmpty(password))
        {
            using var cmd = connection.CreateCommand();
            string safePassword = password.Replace("'", "''");
            cmd.CommandText = $"PRAGMA key = '{safePassword}';";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new AppDbContext(options);
        
        var cards = db.CreditCardAccounts.ToList();
        foreach(var c in cards) {
            Console.WriteLine($"Card: {c.Id} - {c.BankName} {c.CardLabel} Limit: {c.Limit}");
        }

        var txs = db.Transactions
            .Include(t => t.Category)
            .Include(t => t.CreditCardAccount)
            .Where(t => t.CreditCardAccountId != null)
            .ToList();

        Console.WriteLine($"Found {txs.Count} CC transactions");
        foreach(var t in txs)
        {
            Console.WriteLine($"[{t.CreditCardAccountId}] {t.Date:d} - {t.Amount} - {t.Description} - Category: {t.Category?.Name} ({t.Category?.Type})");
        }
    }
}
