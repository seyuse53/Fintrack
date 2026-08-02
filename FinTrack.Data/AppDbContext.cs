using FinTrack.Core.Models;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Helpers;

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
        public DbSet<PriceHistory> PriceHistories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public static AppDbContext CreateNew()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            return new AppDbContext(optionsBuilder.Options);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
                var password = FinTrack.Core.Services.SettingsManager.ActiveDataKey;

                // Build connection string WITHOUT password (we send PRAGMA key manually)
                var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
                }.ToString();

                // Create and open connection, then send PRAGMA key for SQLCipher
                var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                connection.Open();

                if (!string.IsNullOrEmpty(password))
                {
                    using var cmd = connection.CreateCommand();
                    // PRAGMA statements do not accept parameters. The Base64 ActiveDataKey is safe to interpolate.
                    // Replace any single quotes just in case, though a Base64 string will never have them.
                    string safePassword = password.Replace("'", "''");
                    cmd.CommandText = $"PRAGMA key = '{safePassword}';";
                    cmd.ExecuteNonQuery();

                    // Verify the key works by trying to read sqlite_master
                    bool keyWorks = false;
                    try
                    {
                        using var verify = connection.CreateCommand();
                        verify.CommandText = "SELECT count(*) FROM sqlite_master;";
                        verify.ExecuteScalar();
                        keyWorks = true;
                    }
                    catch
                    {
                        keyWorks = false;
                    }
                    
                    if (!keyWorks)
                    {
                        AppLogger.Error("[AppDbContext] SQLCipher key verification failed or DB is not encrypted.");
                    }
                }

                // Pass the already-opened connection to EF Core
                optionsBuilder.UseSqlite(connection);
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
                new Category { Id = 19, Name = "Kredi Kartı Nakit Çekim", Type = TransactionType.Expense },
                new Category { Id = 20, Name = "Ekstre Ödemesi",      Type = TransactionType.Transfer },
                // ── Transfer ─────────────────────────────────────────
                new Category { Id = 21, Name = "Kredi Kartı Ödemesi", Type = TransactionType.Transfer },
                // ── Fatura Alt Kategorileri (Parent: 5) ──────────────
                new Category { Id = 22, Name = "Elektrik",            Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 23, Name = "Su",                  Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 24, Name = "Doğalgaz",            Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 25, Name = "İnternet & TV",       Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 26, Name = "Telefon",             Type = TransactionType.Expense, ParentCategoryId = 5 },
                new Category { Id = 27, Name = "Aidat",               Type = TransactionType.Expense, ParentCategoryId = 5 },
                // ── Özel ─────────────────────────────────────────────
                new Category { Id = 28, Name = "Hatun",               Type = TransactionType.Income },
                new Category { Id = 29, Name = "Hatun",               Type = TransactionType.Expense },
                // ── Puan / Bonus ─────────────────────────────────────
                new Category { Id = 30, Name = "Puan Kullanımı",      Type = TransactionType.Income }
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

            // PriceHistory: günde 1 kayıt per sembol
            modelBuilder.Entity<PriceHistory>()
                .HasIndex(p => new { p.Symbol, p.Date })
                .IsUnique();
        }

        /// <summary>
        /// Decrypts any leftover encrypted BankName or IBAN values in the database.
        /// This ensures legacy AES-CBC encrypted names are permanently converted back to plain text.
        /// Handles multiple formats: "G:base64...", raw base64, etc.
        /// </summary>
        public static void DecryptBankNames(AppDbContext db)
        {
            var dek = FinTrack.Core.Services.SettingsManager.ActiveDataKey;
            if (string.IsNullOrEmpty(dek)) return;

            bool saveNeeded = false;

            try
            {
                var bankAccounts = db.BankAccounts.ToList();
                foreach (var bank in bankAccounts)
                {
                    var decryptedName = TryDecryptValue(bank.BankName, dek);
                    if (decryptedName != null && decryptedName != bank.BankName)
                    {
                        bank.BankName = decryptedName;
                        saveNeeded = true;
                    }

                    var decryptedIban = TryDecryptValue(bank.IBAN, dek);
                    if (decryptedIban != null && decryptedIban != bank.IBAN)
                    {
                        bank.IBAN = decryptedIban;
                        saveNeeded = true;
                    }
                }

                var creditCards = db.CreditCardAccounts.ToList();
                foreach (var card in creditCards)
                {
                    var decryptedName = TryDecryptValue(card.BankName, dek);
                    if (decryptedName != null && decryptedName != card.BankName)
                    {
                        card.BankName = decryptedName;
                        saveNeeded = true;
                    }
                }

                if (saveNeeded)
                {
                    db.SaveChanges();
                }
            }
            catch { /* Ignore decryption errors during startup migration */ }
        }

        /// <summary>
        /// Attempts to decrypt a value that may be encrypted in various formats.
        /// Returns the decrypted value if successful, or null if not encrypted / decryption fails.
        /// Supported formats:
        ///   - "G:base64ciphertext" (legacy prefix format)
        ///   - Raw base64 ciphertext (no prefix)
        /// </summary>
        private static string? TryDecryptValue(string? value, string dek)
        {
            if (string.IsNullOrEmpty(value)) return null;

            // Strategy 1: Strip "G:" prefix if present and decrypt the remainder
            if (value.StartsWith("G:"))
            {
                string base64Part = value.Substring(2);
                if (!string.IsNullOrEmpty(base64Part))
                {
                    try
                    {
                        var result = FinTrack.Core.Services.CryptoProvider.Decrypt(base64Part, dek);
                        // If Decrypt returned the same base64Part, it means decryption failed internally
                        if (result != base64Part && !string.IsNullOrEmpty(result))
                            return result;
                    }
                    catch { /* fall through */ }
                }
            }

            // Strategy 2: Try direct decryption (raw base64 ciphertext without prefix)
            // Only attempt if the value looks like base64 (contains +, /, = or is unusually long)
            if (IsLikelyEncrypted(value))
            {
                try
                {
                    var result = FinTrack.Core.Services.CryptoProvider.Decrypt(value, dek);
                    if (result != value && !string.IsNullOrEmpty(result))
                        return result;
                }
                catch { /* fall through */ }
            }

            return null; // Not encrypted or decryption failed
        }

        /// <summary>
        /// Heuristic: checks if a string looks like it could be Base64-encoded ciphertext
        /// rather than a normal human-readable bank name or IBAN.
        /// </summary>
        private static bool IsLikelyEncrypted(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 16) return false;

            // Real bank names are Turkish text (e.g. "Yapı Kredi", "Garanti BBVA")
            // Real IBANs start with "TR" and are 26 chars of digits
            // Encrypted values are long Base64 strings with +, /, = characters

            // If it starts with "TR" and is exactly 26 chars, it's likely a real IBAN
            if (value.StartsWith("TR") && value.Length == 26) return false;

            // Count Base64-specific characters
            int base64Chars = 0;
            foreach (char c in value)
            {
                if (c == '+' || c == '/' || c == '=')
                    base64Chars++;
            }

            // If it contains Base64 special chars, it's likely encrypted
            if (base64Chars > 0) return true;

            // If it's very long (>30 chars) and all alphanumeric, could be base64 without special chars
            if (value.Length > 30)
            {
                bool allBase64 = true;
                foreach (char c in value)
                {
                    if (!char.IsLetterOrDigit(c) && c != '+' && c != '/' && c != '=')
                    {
                        allBase64 = false;
                        break;
                    }
                }
                if (allBase64) return true;
            }

            return false;
        }

        /// <summary>
        /// Moves legacy BankAccount.InitialBalance values into the Transaction system.
        /// This is called during app startup if needed.
        /// </summary>
        public static void MigrateInitialBalances(AppDbContext db)
        {
            var accountsToMigrate = db.BankAccounts
                .Where(a => a.InitialBalance != 0)
                .ToList();

            if (!accountsToMigrate.Any()) return;

            // Ensure the "Açılış Bakiyesi" category exists (dynamic addition)
            var openingCategory = db.Categories.FirstOrDefault(c => c.Name == "Açılış Bakiyesi");
            if (openingCategory == null)
            {
                openingCategory = new Category 
                { 
                    Name = "Açılış Bakiyesi", 
                    Type = TransactionType.Income,
                    IsVisible = true 
                };
                db.Categories.Add(openingCategory);
                db.SaveChanges(); // ID will be assigned automatically
            }

            foreach (var account in accountsToMigrate)
            {
                // Check if we already have an "Açılış Bakiyesi" transaction for this account to avoid duplicates
                bool alreadyMigrated = db.Transactions.Any(t => t.BankAccountId == account.Id && t.CategoryId == openingCategory.Id);
                
                if (!alreadyMigrated)
                {
                    db.Transactions.Add(new Transaction
                    {
                        BankAccountId = account.Id,
                        CategoryId = openingCategory.Id,
                        Amount = account.InitialBalance,
                        Date = account.CreatedAt, // Use account creation date for opening balance
                        Description = "Açılış Bakiyesi (Otomatik Aktarıldı)"
                    });
                }

                // Reset the legacy InitialBalance to 0
                account.InitialBalance = 0;
            }

            db.SaveChanges();
        }

        public override void Dispose()
        {
            try { Database.GetDbConnection()?.Dispose(); } catch { }
            base.Dispose();
        }

        public override async ValueTask DisposeAsync()
        {
            try { if (Database.GetDbConnection() is { } conn) await conn.DisposeAsync(); } catch { }
            await base.DisposeAsync();
        }
    }
}
