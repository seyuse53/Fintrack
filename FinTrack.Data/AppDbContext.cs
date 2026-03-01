using FinTrack.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BudgetLimit> BudgetLimits { get; set; }
        public DbSet<InflationCache> InflationCaches { get; set; }
        public DbSet<CreditCardAccount> CreditCardAccounts { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<InvestmentAsset> InvestmentAssets { get; set; }
        public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Use SQLite with a dynamic path and SQLCipher password
                var dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
                var password = FinTrack.Core.Services.SettingsManager.ActiveDataKey;
                
                if (string.IsNullOrEmpty(password))
                {
                    optionsBuilder.UseSqlite($"Data Source={dbPath}");
                }
                else
                {
                    optionsBuilder.UseSqlite($"Data Source={dbPath};Password={password}");
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Encryption for Description
            var descriptionConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string?, string>(
                v => v == null ? "" : FinTrack.Core.Services.CryptoProvider.Encrypt(v, FinTrack.Core.Services.SettingsManager.ActiveDataKey ?? ""),
                v => FinTrack.Core.Services.CryptoProvider.Decrypt(v, FinTrack.Core.Services.SettingsManager.ActiveDataKey ?? "")
            );

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Description)
                .HasConversion(descriptionConverter);

            // Seed some initial categories
            modelBuilder.Entity<Category>().HasData(
                // ── Mevcut ──────────────────────────────────────────
                new Category { Id = 1, Name = "Maaş",              Type = TransactionType.Income },
                new Category { Id = 2, Name = "Ek Gelir/Serbest",  Type = TransactionType.Income },
                new Category { Id = 3, Name = "Market & Mutfak",   Type = TransactionType.Expense },
                new Category { Id = 4, Name = "Kira",              Type = TransactionType.Expense },
                new Category { Id = 5, Name = "Faturalar",         Type = TransactionType.Expense },
                new Category { Id = 6, Name = "Eğlence",           Type = TransactionType.Expense },
                // ── Yeni Gider ───────────────────────────────────────
                new Category { Id = 7,  Name = "Kişisel Harçlık",  Type = TransactionType.Expense },
                new Category { Id = 8,  Name = "Çocuk Harçlığı",   Type = TransactionType.Expense },
                new Category { Id = 9,  Name = "Taze Gıda & Pazar",Type = TransactionType.Expense },
                new Category { Id = 10, Name = "Aile Harcamaları", Type = TransactionType.Expense },
                new Category { Id = 11, Name = "Ulaşım",           Type = TransactionType.Expense },
                new Category { Id = 12, Name = "Ev & Yaşam",       Type = TransactionType.Expense },
                new Category { Id = 13, Name = "Giyim",            Type = TransactionType.Expense },
                new Category { Id = 14, Name = "Eğitim",           Type = TransactionType.Expense },
                new Category { Id = 15, Name = "Sağlık",           Type = TransactionType.Expense },
                new Category { Id = 16, Name = "Hediye & Bağış",   Type = TransactionType.Expense },
                // ── Yeni Gelir ───────────────────────────────────────
                new Category { Id = 17, Name = "Yemek Ödeneği",       Type = TransactionType.Income },
                new Category { Id = 18, Name = "Aile Desteği",        Type = TransactionType.Income },
                // ── Kredi Kartı ──────────────────────────────────────
                new Category { Id = 19, Name = "Kredi Kartı Çekilen", Type = TransactionType.Income },
                new Category { Id = 20, Name = "Ekstra Ödemesi",      Type = TransactionType.Expense },
                // ── Transfer ─────────────────────────────────────────
                new Category { Id = 21, Name = "Kredi Kartı Ödemesi", Type = TransactionType.Transfer },
                // ── Fatura Alt Kategorileri (Parent: 5) ──────────────
                new Category { Id = 22, Name = "Elektrik",            Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 23, Name = "Su",                  Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 24, Name = "Doğalgaz",            Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 25, Name = "İnternet & TV",       Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 26, Name = "Telefon",             Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 27, Name = "Aidat",               Type = TransactionType.Expense, ParentCategoryId = 5 }
            );

            // BudgetLimit: one limit per category
            modelBuilder.Entity<BudgetLimit>()
                .HasIndex(b => b.CategoryId)
                .IsUnique();

            modelBuilder.Entity<BudgetLimit>()
                .HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // InflationCache: unique per year+month
            modelBuilder.Entity<InflationCache>()
                .HasIndex(i => new { i.Year, i.Month })
                .IsUnique();

            // Transaction → CreditCardAccount (optional, null = cash)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.CreditCardAccount)
                .WithMany()
                .HasForeignKey(t => t.CreditCardAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // Transaction → BankAccount (optional, null = cash)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.BankAccount)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BankAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // InvestmentTransaction → InvestmentAsset
            modelBuilder.Entity<InvestmentTransaction>()
                .HasOne(it => it.InvestmentAsset)
                .WithMany(a => a.Transactions)
                .HasForeignKey(it => it.InvestmentAssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // InvestmentTransaction → BankAccount (optional link)
            modelBuilder.Entity<InvestmentTransaction>()
                .HasOne(it => it.LinkedBankAccount)
                .WithMany()
                .HasForeignKey(it => it.LinkedBankAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // InvestmentTransaction → CreditCardAccount (optional link)
            modelBuilder.Entity<InvestmentTransaction>()
                .HasOne(it => it.LinkedCreditCardAccount)
                .WithMany()
                .HasForeignKey(it => it.LinkedCreditCardAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // CreditCardAccount hierarchy (Master/Linked)
            modelBuilder.Entity<CreditCardAccount>()
                .HasOne(c => c.ParentCard)
                .WithMany(c => c.LinkedCards)
                .HasForeignKey(c => c.ParentCardId)
                .OnDelete(DeleteBehavior.SetNull);

            // Category hierarchy
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
