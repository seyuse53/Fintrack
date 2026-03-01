using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace FinTrack.Core.Services
{
    public static class EncryptionService
    {
        /// <summary>
        /// Ensures the database at the given path is encrypted with the provided password.
        /// If the database is currently unencrypted, it will encrypt it.
        /// </summary>
        public static void EnsureDatabaseEncryption(string dbPath, string password)
        {
            if (!File.Exists(dbPath)) return;
            if (string.IsNullOrEmpty(password)) return;

            // Try opening without password to see if it's already encrypted
            bool isAlreadyEncrypted = false;
            try
            {
                // Use Pooling=False to ensure the file is released immediately after Dispose
                using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT count(*) FROM sqlite_master;";
                        command.ExecuteScalar();
                    }
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26) // SQLITE_NOTADB (Usually means encrypted)
            {
                isAlreadyEncrypted = true;
            }
            catch (Exception)
            {
                // Other errors might occur if it's encrypted but we don't have the key
                isAlreadyEncrypted = true; 
            }

            if (!isAlreadyEncrypted)
            {
                EncryptPlainDatabase(dbPath, password);
            }
        }

        private static void EncryptPlainDatabase(string dbPath, string password)
        {
            string tempPath = dbPath + ".tmp";
            
            try
            {
                // We use a raw connection to ATTACH and export to a new encrypted DB
                using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        // 1. Attach a new empty database with the password
                        command.CommandText = $"ATTACH DATABASE '{tempPath}' AS encrypted KEY '{password}';";
                        command.ExecuteNonQuery();

                        // 2. Export all data from main to encrypted
                        command.CommandText = "SELECT sqlcipher_export('encrypted');";
                        command.ExecuteNonQuery();

                        // 3. Detach
                        command.CommandText = "DETACH DATABASE encrypted;";
                        command.ExecuteNonQuery();
                    }
                }

                // Explicitly clear pools just in case, though Pooling=False should handle it
                SqliteConnection.ClearAllPools();

                // Replace old DB with encrypted one
                if (File.Exists(dbPath)) File.Delete(dbPath);
                File.Move(tempPath, dbPath);
            }
            catch (Exception ex)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw new Exception($"Veritabanı şifrelenirken bir hata oluştu: {ex.Message}", ex);
            }
        }
    }
}
