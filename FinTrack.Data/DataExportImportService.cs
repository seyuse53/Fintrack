using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using System.Collections.Generic;

namespace FinTrack.Data
{
    public class FinTrackExportData
    {
        public List<Category> Categories { get; set; } = new();
        public List<BankAccount> BankAccounts { get; set; } = new();
        public List<CreditCardAccount> CreditCardAccounts { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
        public List<BudgetLimit> BudgetLimits { get; set; } = new();
        public List<InvestmentAsset> InvestmentAssets { get; set; } = new();
        public List<InvestmentTransaction> InvestmentTransactions { get; set; } = new();
        public List<PriceHistory> PriceHistories { get; set; } = new();
        public List<InflationCache> InflationCaches { get; set; } = new();
    }

    public static class DataExportImportService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Ignore cycles in case navigation properties are somehow loaded, 
            // but we won't Include() them so the JSON remains clean and easy to edit.
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true
        };

        public static async Task ExportDataAsync(AppDbContext context, string filePath)
        {
            // We fetch data WITHOUT .Include() so navigation properties remain empty/null.
            // This prevents huge nested JSON files and keeps relations strictly on Foreign Key properties (like CategoryId).
            var data = new FinTrackExportData
            {
                Categories = await context.Categories.AsNoTracking().ToListAsync(),
                BankAccounts = await context.BankAccounts.AsNoTracking().ToListAsync(),
                CreditCardAccounts = await context.CreditCardAccounts.AsNoTracking().ToListAsync(),
                Transactions = await context.Transactions.AsNoTracking().ToListAsync(),
                BudgetLimits = await context.BudgetLimits.AsNoTracking().ToListAsync(),
                InvestmentAssets = await context.InvestmentAssets.AsNoTracking().ToListAsync(),
                InvestmentTransactions = await context.InvestmentTransactions.AsNoTracking().ToListAsync(),
                PriceHistories = await context.PriceHistories.AsNoTracking().ToListAsync(),
                InflationCaches = await context.InflationCaches.AsNoTracking().ToListAsync()
            };

            string json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task ImportDataAsync(AppDbContext context, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("İçe aktarılacak JSON dosyası bulunamadı.");

            string json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<FinTrackExportData>(json, _jsonOptions);

            if (data == null)
                throw new Exception("JSON dosyası okunamadı veya format geçersiz.");

            // To avoid SQLCipher recreation issues or breaking schema, we manually wipe data instead of EnsureDeleted()
            // Turn off FK checks temporarily for bulk delete
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Clear all tables (ignore if table doesn't exist)
                var tablesToClear = new[] { 
                    "InvestmentTransactions", "PriceHistories", "Transactions", 
                    "BudgetLimits", "CreditCardAccounts", "InvestmentAssets", 
                    "BankAccounts", "Categories", "InflationCaches" 
                };

                foreach (var table in tablesToClear)
                {
                    try 
                    { 
                        await context.Database.ExecuteSqlRawAsync($"DELETE FROM {table};"); 
                    } 
                    catch { /* ignore missing tables */ }
                }

                // Reset auto-increment sequences
                try { await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence;"); } catch { }

                // Add imported data
                if (data.Categories?.Any() == true) await context.Categories.AddRangeAsync(data.Categories);
                if (data.BankAccounts?.Any() == true) await context.BankAccounts.AddRangeAsync(data.BankAccounts);
                if (data.CreditCardAccounts?.Any() == true) await context.CreditCardAccounts.AddRangeAsync(data.CreditCardAccounts);
                if (data.Transactions?.Any() == true) await context.Transactions.AddRangeAsync(data.Transactions);
                if (data.BudgetLimits?.Any() == true) await context.BudgetLimits.AddRangeAsync(data.BudgetLimits);
                if (data.InvestmentAssets?.Any() == true) await context.InvestmentAssets.AddRangeAsync(data.InvestmentAssets);
                if (data.InvestmentTransactions?.Any() == true) await context.InvestmentTransactions.AddRangeAsync(data.InvestmentTransactions);
                if (data.PriceHistories?.Any() == true) await context.PriceHistories.AddRangeAsync(data.PriceHistories);
                if (data.InflationCaches?.Any() == true) await context.InflationCaches.AddRangeAsync(data.InflationCaches);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                // Restore FK checks
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            }
        }
    }
}
