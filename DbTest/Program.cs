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
        var dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        Console.WriteLine($"DB Path: {dbPath}");

        using var db = new AppDbContext(options);
        var txs = db.Transactions
            .Include(t => t.Category)
            .Where(t => t.BankAccountId == null && t.CreditCardAccountId == null)
            .ToList();

        Console.WriteLine($"Found {txs.Count} cash transactions");
        foreach(var t in txs)
        {
            Console.WriteLine($"{t.Date:d} - {t.Amount} - {t.Description} - Category: {t.Category?.Name}");
        }
    }
}
