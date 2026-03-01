using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinTrack.Core.Services
{
    public enum ApiProviderType
    {
        Manual = 0,
        YahooFinance = 1,
        Custom = 2
    }

    public class Settings
    {
        public string? HashedPassword { get; set; }
        public string? EncryptedDataKey { get; set; }
        public string? RecoveryEncryptedDataKey { get; set; }
        public string? DatabasePath { get; set; }
        public ApiProviderType PricingApiProvider { get; set; } = ApiProviderType.Manual;
        public string? CustomApiUrl { get; set; }

        // Backup Settings
        public bool BackupEnabled { get; set; } = false;
        public string? BackupDirectory { get; set; }
        public int MaxBackupCount { get; set; } = 5;
        public int MaxBackupAgeDays { get; set; } = 30;

        // Auto-Lock Settings
        public bool AutoLockEnabled { get; set; } = true;
        public int AutoLockTimeoutMinutes { get; set; } = 3;
    }

    public static class SettingsManager
    {
        private static string _currentProfile = "Default";
        private static string SettingsFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{_currentProfile}.json");
        private const string ProfilesFile = "profiles.json";

        // Holds the decrypted data key in memory while the app is running
        public static string? ActiveDataKey { get; private set; }

        public static string GetDefaultDbPath(string? profileName = null)
        {
            string pName = profileName ?? _currentProfile;
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string finTrackPath = Path.Combine(docPath, "FinTrack");
            
            if (!Directory.Exists(finTrackPath))
                Directory.CreateDirectory(finTrackPath);

            return Path.Combine(finTrackPath, $"fintrack_{pName}.db");
        }

        private static string GetProfilesPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
        }

        public static List<string> GetProfiles()
        {
            try
            {
                string path = GetProfilesPath();
                if (File.Exists(path))
                {
                    return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? new List<string>();
                }
            }
            catch { }
            return new List<string>();
        }

        public static void SwitchProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName)) return;
            _currentProfile = profileName;
            ActiveDataKey = null; // New profile requires new login
        }

        public static void CreateProfile(string profileName, string? databasePath = null)
        {
            var profiles = GetProfiles();
            if (!profiles.Contains(profileName))
            {
                profiles.Add(profileName);
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
                File.WriteAllText(path, JsonSerializer.Serialize(profiles));

                // Eğer bir veritabanı yolu belirtilmişse, profilin ayarlarını hemen oluşturup kaydet
                if (!string.IsNullOrEmpty(databasePath))
                {
                    string oldProfile = _currentProfile;
                    _currentProfile = profileName; // Geçici olarak geçiş yap
                    
                    var settings = new Settings { DatabasePath = databasePath };
                    SaveSettings(settings);
                    
                    _currentProfile = oldProfile; // Eski profile geri dön
                }
            }
        }

        public static void DeleteProfile(string profileName, bool deleteDatabase = false)
        {
            if (string.IsNullOrEmpty(profileName)) return;

            try
            {
                // Delete Settings File
                string sFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profileName}.json");
                if (File.Exists(sFile)) File.Delete(sFile);

                if (deleteDatabase)
                {
                    // Delete Database File from Documents\FinTrack (Default location)
                    string dbFile = GetDefaultDbPath(profileName);
                    if (File.Exists(dbFile)) File.Delete(dbFile);
                    
                    // Also check Legacy location (Base Directory) just in case
                    string legacyDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fintrack_{profileName}.db");
                    if (File.Exists(legacyDbFile)) File.Delete(legacyDbFile);
                }

                // Remove from Profile List
                var profiles = GetProfiles();
                if (profiles.Contains(profileName))
                {
                    profiles.Remove(profileName);
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
                    File.WriteAllText(path, JsonSerializer.Serialize(profiles));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Profil silinirken hata: {ex.Message}");
            }
        }

        public static void ResetApplication(bool deleteDatabase = false)
        {
            try
            {
                var profiles = GetProfiles();
                foreach (var profile in profiles)
                {
                    string sFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profile}.json");
                    if (File.Exists(sFile)) File.Delete(sFile);
                }

                string pFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
                if (File.Exists(pFile)) File.Delete(pFile);
                
                string oldFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(oldFile)) File.Delete(oldFile);

                if (deleteDatabase)
                {
                    // Mevcut ve eski tüm veritabanlarını temizle
                    foreach (var profile in profiles)
                    {
                        string dbP = GetDefaultDbPath(profile);
                        if (File.Exists(dbP)) File.Delete(dbP);

                        string legacyDbP = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fintrack_{profile}.db");
                        if (File.Exists(legacyDbP)) File.Delete(legacyDbP);
                    }
                    
                    // Eski genel veritabanı dosyasını da temizle
                    string oldDbP = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fintrack.db");
                    if (File.Exists(oldDbP)) File.Delete(oldDbP);
                }

                ActiveDataKey = null;
                _currentProfile = "Default";
            }
            catch (Exception ex)
            {
                throw new Exception($"Sıfırlama sırasında hata: {ex.Message}");
            }
        }

        public static Settings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                return new Settings();
            }

            try
            {
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            catch
            {
                return new Settings();
            }
        }

        public static void SaveSettings(Settings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }

        public static string GetDatabasePath()
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.DatabasePath))
            {
                // Yeni varsayılan konum: Belgelerim\FinTrack
                return GetDefaultDbPath(_currentProfile);
            }
            return settings.DatabasePath;
        }

        public static void ResetProfile(string profileName, bool deleteDatabase = false)
        {
            if (string.IsNullOrEmpty(profileName)) return;

            try
            {
                // Ayar dosyasını sil (şifre ve anahtar buradadır)
                string sFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profileName}.json");
                if (File.Exists(sFile)) File.Delete(sFile);

                if (deleteDatabase)
                {
                    // Profilin veritabanını sil (Yeni ve Eski konumlar)
                    string dbFile = GetDefaultDbPath(profileName);
                    if (File.Exists(dbFile)) File.Delete(dbFile);

                    string legacyDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fintrack_{profileName}.db");
                    if (File.Exists(legacyDbFile)) File.Delete(legacyDbFile);
                }

                // Eğer aktif profil sıfırlanıyorsa anahtarları temizle
                if (_currentProfile == profileName)
                {
                    ActiveDataKey = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Profil sıfırlanırken hata: {ex.Message}");
            }
        }
        public static void SetDatabasePath(string newPath)
        {
            var settings = LoadSettings();
            settings.DatabasePath = newPath;
            SaveSettings(settings);
        }

        public static ApiProviderType GetApiProvider()
        {
            var settings = LoadSettings();
            return settings.PricingApiProvider;
        }

        public static void SetApiProvider(ApiProviderType provider)
        {
            var settings = LoadSettings();
            settings.PricingApiProvider = provider;
            SaveSettings(settings);
        }

        public static string? GetCustomApiUrl()
        {
            var settings = LoadSettings();
            return settings.CustomApiUrl;
        }

        public static void SetCustomApiUrl(string? url)
        {
            var settings = LoadSettings();
            settings.CustomApiUrl = url;
            SaveSettings(settings);
        }

        public static bool IsPasswordSet()
        {
            var settings = LoadSettings();
            return !string.IsNullOrEmpty(settings.HashedPassword);
        }

        // ==================== BACKUP SETTINGS ====================

        public static (bool Enabled, string? Directory, int MaxCount, int MaxAgeDays) GetBackupSettings()
        {
            var settings = LoadSettings();
            return (settings.BackupEnabled, settings.BackupDirectory, settings.MaxBackupCount, settings.MaxBackupAgeDays);
        }

        // ==================== AUTO-LOCK SETTINGS ====================
        
        public static (bool Enabled, int TimeoutMinutes) GetAutoLockSettings()
        {
            var settings = LoadSettings();
            return (settings.AutoLockEnabled, settings.AutoLockTimeoutMinutes);
        }

        public static void SaveAutoLockSettings(bool enabled, int timeoutMinutes)
        {
            var settings = LoadSettings();
            settings.AutoLockEnabled = enabled;
            settings.AutoLockTimeoutMinutes = timeoutMinutes;
            SaveSettings(settings);
        }

        public static void SaveBackupSettings(bool enabled, string? directory, int maxCount, int maxAgeDays)
        {
            var settings = LoadSettings();
            settings.BackupEnabled = enabled;
            settings.BackupDirectory = directory;
            settings.MaxBackupCount = maxCount;
            settings.MaxBackupAgeDays = maxAgeDays;
            SaveSettings(settings);
        }

        /// <summary>
        /// Used when setting up the database/login for the very first time.
        /// Generates a Master DEK, encrypts it with the User Password, and also with a Recovery Code.
        /// Returns the Recovery Code to show to the user.
        /// </summary>
        public static string SetupFirstTimePassword(string plainPassword)
        {
            var settings = LoadSettings();
            
            // 1. Generate the Master Data Encryption Key (DEK)
            string masterDek = CryptoProvider.GenerateRandomDataEncryptionKey();
            
            // 2. Generate a Recovery Code
            string recoveryCode = CryptoProvider.GenerateRecoveryCode();
            
            // 3. Hash user password for verification using PBKDF2
            settings.HashedPassword = HashPassword(plainPassword);
            
            // 4. Encrypt the DEK with the user's password
            string passwordKey = CryptoProvider.DeriveKeyFromPassword(plainPassword);
            settings.EncryptedDataKey = CryptoProvider.Encrypt(masterDek, passwordKey);
            
            // 5. Encrypt the DEK with the recovery code
            string recoveryKey = CryptoProvider.DeriveKeyFromPassword(recoveryCode);
            settings.RecoveryEncryptedDataKey = CryptoProvider.Encrypt(masterDek, recoveryKey);
            
            SaveSettings(settings);

            // Keep the DEK in memory for the active session
            ActiveDataKey = masterDek;

            return recoveryCode;
        }

        public static bool VerifyPasswordAndLoadKey(string inputPassword)
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.HashedPassword))
                return false;

            bool isPasswordCorrect = false;
            bool needsUpgrade = false;

            // Check if it's a new PBKDF2 hash or old SHA256 hash
            if (settings.HashedPassword.StartsWith("V1:"))
            {
                isPasswordCorrect = CryptoProvider.VerifyPasswordPBKDF2(inputPassword, settings.HashedPassword);
            }
            else
            {
                // Old SHA256 implementation backward compatibility
                isPasswordCorrect = (settings.HashedPassword == HashPasswordLegacy(inputPassword));
                
                // If it's a correct old hash, we should upgrade it to PBKDF2 seamlessly
                if (isPasswordCorrect)
                {
                    needsUpgrade = true;
                }
            }

            if (isPasswordCorrect)
            {
                // Password is correct, let's load the DEK into memory
                string passwordKey = CryptoProvider.DeriveKeyFromPassword(inputPassword);
                ActiveDataKey = CryptoProvider.Decrypt(settings.EncryptedDataKey!, passwordKey);
                
                // Seamlessly upgrade the hash to PBKDF2 if it was old
                if (needsUpgrade)
                {
                    settings.HashedPassword = HashPassword(inputPassword);
                    SaveSettings(settings);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the user password. Requires the current password to be verified first.
        /// Re-encrypts the master DEK with the new password so no data is lost.
        /// The ActiveDataKey must already be loaded in memory (i.e. user is logged in).
        /// </summary>
        public static bool ChangePassword(string currentPassword, string newPassword)
        {
            var settings = LoadSettings();

            // Double-check current password
            bool isCurrentPasswordCorrect = false;
            if (!string.IsNullOrEmpty(settings.HashedPassword))
            {
                 if (settings.HashedPassword.StartsWith("V1:"))
                 {
                     isCurrentPasswordCorrect = CryptoProvider.VerifyPasswordPBKDF2(currentPassword, settings.HashedPassword);
                 }
                 else
                 {
                     isCurrentPasswordCorrect = (settings.HashedPassword == HashPasswordLegacy(currentPassword));
                 }
            }

            if (!isCurrentPasswordCorrect)
                return false;

            if (string.IsNullOrEmpty(ActiveDataKey))
                return false;

            // Re-encrypt the existing DEK with the new password
            string newPasswordKey = CryptoProvider.DeriveKeyFromPassword(newPassword);
            settings.EncryptedDataKey = CryptoProvider.Encrypt(ActiveDataKey, newPasswordKey);

            // Update the stored hash
            settings.HashedPassword = HashPassword(newPassword);

            SaveSettings(settings);
            return true;
        }

        public static bool ResetPasswordWithRecoveryCode(string recoveryCode, string newPassword)
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.RecoveryEncryptedDataKey))
                return false;

            string recoveryKey = CryptoProvider.DeriveKeyFromPassword(recoveryCode);
            string decryptedDek = CryptoProvider.Decrypt(settings.RecoveryEncryptedDataKey, recoveryKey);

            // If decryption failed, it returns the cipherText itself
            if (decryptedDek == settings.RecoveryEncryptedDataKey)
                return false;

            // Decryption succeeded, we have the Master DEK
            // 1. Re-encrypt DEK with the new password
            string newPasswordKey = CryptoProvider.DeriveKeyFromPassword(newPassword);
            settings.EncryptedDataKey = CryptoProvider.Encrypt(decryptedDek, newPasswordKey);
            
            // 2. Hash the new user password
            settings.HashedPassword = HashPassword(newPassword);

            SaveSettings(settings);

            // Also load into memory so user is effectively logged in
            ActiveDataKey = decryptedDek;
            return true;
        }

        private static string HashPassword(string password)
        {
            return CryptoProvider.HashPasswordPBKDF2(password);
        }

        // Legacy SHA256 hash method for backward compatibility
        private static string HashPasswordLegacy(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
