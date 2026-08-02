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
        Custom = 2,
        GenelPara = 3,
        WebScraper = 4
    }

    public class Settings
    {
        public string? HashedPassword { get; set; }
        public string? PasswordSalt { get; set; } // Salt for DEK encryption
        public string? RecoverySalt { get; set; } // Salt for Recovery Key
        public string? EncryptedDataKey { get; set; }
        public string? RecoveryEncryptedDataKey { get; set; }
        public string? DatabasePath { get; set; }
        public ApiProviderType PricingApiProvider { get; set; } = ApiProviderType.Manual;
        public string? CustomApiUrl { get; set; }
        public string? GeminiApiKey { get; set; }

        // Kategori bazlı API sağlayıcıları (yazılımdan değiştirmeden ayarlanabilir)
        public ApiProviderType DovizProvider { get; set; } = ApiProviderType.GenelPara;
        public ApiProviderType AltinProvider { get; set; } = ApiProviderType.GenelPara;
        public ApiProviderType HisseProvider { get; set; } = ApiProviderType.YahooFinance;
        public ApiProviderType KriptoProvider { get; set; } = ApiProviderType.YahooFinance;
        public ApiProviderType FonProvider { get; set; } = ApiProviderType.Manual;
        public ApiProviderType DigerProvider { get; set; } = ApiProviderType.Manual;

        // Backup Settings
        public bool BackupEnabled { get; set; } = false;
        public string? BackupDirectory { get; set; }
        public int MaxBackupCount { get; set; } = 5;
        public int MaxBackupAgeDays { get; set; } = 30;

        // Auto-Lock Settings
        public bool AutoLockEnabled { get; set; } = true;
        public int AutoLockTimeoutMinutes { get; set; } = 3;

        // Vergi Oranları (%) — Ayarlar sayfasından değiştirilebilir
        public decimal TaxRateHisseSenedi { get; set; } = 0m;      // BIST hisse alım-satım stopajı %0
        public decimal TaxRateMKYO { get; set; } = 10m;            // MKYO (1 yıl altı) %10
        public decimal TaxRateTemettü { get; set; } = 15m;         // Temettü stopajı %15
        public decimal TaxRateDöviz { get; set; } = 0m;            // Döviz — beyan (stopaj yok)
        public decimal TaxRateAltın { get; set; } = 0m;            // Altın — beyan (stopaj yok)
        public decimal TaxRateKripto { get; set; } = 0m;           // Kripto — henüz düzenleme yok
        public decimal TaxRateYurtDışı { get; set; } = 0m;         // Yurt dışı — beyan zorunlu
    }

    public static class SettingsManager
    {
        private static string _currentProfile = "Default";
        private const string ProfilesFile = "profiles.json";

        private static string GetAppDataFolder()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = Path.Combine(localApp, "FinTrack");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string GetSettingsFilePath(string profile)
        {
            string fileName = $"settings_{profile}.json";
            string newPath = Path.Combine(GetAppDataFolder(), fileName);

            // Migration: Move from BaseDirectory to AppData if exists
            string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                try { File.Move(oldPath, newPath); } catch { }
            }

            return newPath;
        }

        private static string SettingsFile => GetSettingsFilePath(_currentProfile);

        // Holds the decrypted data key in memory while the app is running
        public static string? ActiveDataKey { get; private set; }

        public static void ClearActiveKey()
        {
            ActiveDataKey = null;
        }

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
            string newPath = Path.Combine(GetAppDataFolder(), ProfilesFile);

            // Migration
            string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                try { File.Move(oldPath, newPath); } catch { }
            }

            return newPath;
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
                string path = GetProfilesPath();
                File.WriteAllText(path, JsonSerializer.Serialize(profiles));

                // Initialize settings for the new profile
                string oldProfile = _currentProfile;
                _currentProfile = profileName;
                
                var settings = new Settings { DatabasePath = databasePath };

                // [SIDE-CAR RECOVERY] 
                // Check for a companion .keys file to restore encryption info
                if (!string.IsNullOrEmpty(databasePath) && File.Exists(databasePath))
                {
                    string oldKeysFile = databasePath + ".keys";
                    string newKeysFile = Path.Combine(GetAppDataFolder(), $"recovery_{profileName}.keys");
                    string? keysFileToRead = File.Exists(newKeysFile) ? newKeysFile : (File.Exists(oldKeysFile) ? oldKeysFile : null);

                    if (keysFileToRead != null)
                    {
                        try
                        {
                            string keysJson = File.ReadAllText(keysFileToRead);
                            var options = new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                AllowTrailingCommas = true
                            };
                            var keysData = JsonSerializer.Deserialize<Settings>(keysJson, options);
                            if (keysData != null && !string.IsNullOrEmpty(keysData.HashedPassword))
                            {
                                settings.HashedPassword = keysData.HashedPassword;
                                settings.EncryptedDataKey = keysData.EncryptedDataKey;
                                settings.RecoveryEncryptedDataKey = keysData.RecoveryEncryptedDataKey;
                            }
                        }
                        catch { /* Silent fail */ }
                    }
                }

                SaveSettings(settings);
                _currentProfile = oldProfile;
            }
        }

        public static void DeleteProfile(string profileName, bool deleteDatabase = false)
        {
            if (string.IsNullOrEmpty(profileName)) return;

            try
            {
                // Delete Settings File
                string sFile = GetSettingsFilePath(profileName);
                if (File.Exists(sFile)) File.Delete(sFile);
                
                // Also check legacy location just in case migration hadn't happened
                string oldSFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profileName}.json");
                if (File.Exists(oldSFile)) File.Delete(oldSFile);

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
                    string path = GetProfilesPath();
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
                    string sFile = GetSettingsFilePath(profile);
                    if (File.Exists(sFile)) File.Delete(sFile);
                    
                    string oldSFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profile}.json");
                    if (File.Exists(oldSFile)) File.Delete(oldSFile);
                }

                string pFile = GetProfilesPath();
                if (File.Exists(pFile)) File.Delete(pFile);
                
                string oldPFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFile);
                if (File.Exists(oldPFile)) File.Delete(oldPFile);
                
                string oldFile = Path.Combine(GetAppDataFolder(), "settings.json");
                if (File.Exists(oldFile)) File.Delete(oldFile);

                string legacyOldFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(legacyOldFile)) File.Delete(legacyOldFile);

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

            // [SIDE-CAR KEYS] Save a copy of encryption info in a secure location (isolated from DB)
            string? dbPath = settings.DatabasePath;
            if (string.IsNullOrEmpty(dbPath))
            {
                dbPath = GetDatabasePath();
            }

            if (!string.IsNullOrEmpty(dbPath) && !string.IsNullOrEmpty(settings.HashedPassword))
            {
                try
                {
                    var keysOnly = new Settings
                    {
                        HashedPassword = settings.HashedPassword,
                        EncryptedDataKey = settings.EncryptedDataKey,
                        RecoveryEncryptedDataKey = settings.RecoveryEncryptedDataKey,
                        DatabasePath = dbPath
                    };
                    string keysJson = JsonSerializer.Serialize(keysOnly, new JsonSerializerOptions { WriteIndented = true });
                    // Güvenlik: DB dizini yerine LocalAppData altında izole tutuluyor
                    string keysPath = Path.Combine(GetAppDataFolder(), $"recovery_{_currentProfile}.keys");
                    File.WriteAllText(keysPath, keysJson);

                    // Eski güvensiz konumdaki .keys dosyasını temizle (varsa)
                    string oldKeysPath = dbPath + ".keys";
                    if (File.Exists(oldKeysPath))
                    {
                        File.Delete(oldKeysPath);
                    }
                }
                catch { /* Logging would be good here but let's keep it robust */ }
            }
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
                string sFile = GetSettingsFilePath(profileName);
                if (File.Exists(sFile)) File.Delete(sFile);

                string oldSFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"settings_{profileName}.json");
                if (File.Exists(oldSFile)) File.Delete(oldSFile);

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

        public static string? GetGeminiApiKey()
        {
            var settings = LoadSettings();
            return settings.GeminiApiKey;
        }

        public static void SetGeminiApiKey(string? apiKey)
        {
            var settings = LoadSettings();
            settings.GeminiApiKey = apiKey;
            SaveSettings(settings);
        }

        public static void RevertSettingsToDefaults()
        {
            SaveSettings(new Settings());
        }

        private static string _languagePreference = "tr";

        public static string GetLanguagePreference()
        {
            try
            {
                var settingsFile = Path.Combine(GetAppDataFolder(), "global_settings.json");
                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    if (dict.TryGetValue("Language", out string? lang))
                        return lang;
                }
            }
            catch { }
            return _languagePreference;
        }

        public static void SetLanguagePreference(string lang)
        {
            _languagePreference = lang;
            try
            {
                var settingsFile = Path.Combine(GetAppDataFolder(), "global_settings.json");
                Dictionary<string, string> dict = new();
                if (File.Exists(settingsFile))
                {
                    string existingJson = File.ReadAllText(settingsFile);
                    dict = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson) ?? new Dictionary<string, string>();
                }
                dict["Language"] = lang;
                string newJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, newJson);
            }
            catch { }
        }

        private static string _themePreference = "Sistem";

        public static string GetThemePreference()
        {
            try
            {
                var settingsFile = Path.Combine(GetAppDataFolder(), "global_settings.json");
                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    if (dict.TryGetValue("Theme", out string? theme))
                        return theme;
                }
            }
            catch { }
            return _themePreference;
        }

        public static void SetThemePreference(string theme)
        {
            _themePreference = theme;
            try
            {
                var settingsFile = Path.Combine(GetAppDataFolder(), "global_settings.json");
                Dictionary<string, string> dict = new();
                if (File.Exists(settingsFile))
                {
                    string existingJson = File.ReadAllText(settingsFile);
                    dict = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson) ?? new Dictionary<string, string>();
                }
                dict["Theme"] = theme;
                string newJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, newJson);
            }
            catch { }
        }

        public static ApiProviderType GetProviderForCategory(string? category)
        {
            var settings = LoadSettings();
            return (category ?? "").Trim() switch
            {
                "Döviz" => settings.DovizProvider,
                "Altın" => settings.AltinProvider,
                "Hisse Senedi" => settings.HisseProvider,
                "Hisse" => settings.HisseProvider,
                "Kripto Para" => settings.KriptoProvider,
                "Kripto" => settings.KriptoProvider,
                "Fon" => settings.FonProvider,
                "BES" => settings.DigerProvider,
                "Diğer" => settings.DigerProvider,
                _ => settings.PricingApiProvider
            };
        }

        public static void SaveCategoryProviders(
            ApiProviderType doviz, ApiProviderType altin, ApiProviderType hisse,
            ApiProviderType kripto, ApiProviderType fon, ApiProviderType diger)
        {
            var settings = LoadSettings();
            settings.DovizProvider = doviz;
            settings.AltinProvider = altin;
            settings.HisseProvider = hisse;
            settings.KriptoProvider = kripto;
            settings.FonProvider = fon;
            settings.DigerProvider = diger;

            // Genel provider'ı en çok kullanılana göre ayarla (Yahoo varsa Yahoo)
            if (hisse == ApiProviderType.YahooFinance || doviz == ApiProviderType.YahooFinance)
                settings.PricingApiProvider = ApiProviderType.YahooFinance;
            else if (doviz == ApiProviderType.GenelPara || altin == ApiProviderType.GenelPara)
                settings.PricingApiProvider = ApiProviderType.GenelPara;
            else
                settings.PricingApiProvider = ApiProviderType.Manual;

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

        // ==================== TAX SETTINGS ====================

        public static decimal GetTaxRateForCategory(string? category)
        {
            var settings = LoadSettings();
            return (category ?? "").Trim() switch
            {
                "Hisse Senedi" => settings.TaxRateHisseSenedi,
                "Döviz" => settings.TaxRateDöviz,
                "Altın" => settings.TaxRateAltın,
                "Kripto Para" => settings.TaxRateKripto,
                _ => 0m
            };
        }

        public static decimal GetDividendTaxRate()
        {
            var settings = LoadSettings();
            return settings.TaxRateTemettü;
        }

        public static void SaveTaxSettings(
            decimal hisse, decimal mkyo, decimal temettü,
            decimal döviz, decimal altın, decimal kripto, decimal yurtDışı)
        {
            var settings = LoadSettings();
            settings.TaxRateHisseSenedi = hisse;
            settings.TaxRateMKYO = mkyo;
            settings.TaxRateTemettü = temettü;
            settings.TaxRateDöviz = döviz;
            settings.TaxRateAltın = altın;
            settings.TaxRateKripto = kripto;
            settings.TaxRateYurtDışı = yurtDışı;
            SaveSettings(settings);
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

            // 4. Generate a Salt for Key Derivation
            byte[] saltBytes = CryptoProvider.CreateSalt();
            settings.PasswordSalt = Convert.ToBase64String(saltBytes);
            
            // 5. Encrypt the DEK with the user's password (Salted)
            string passwordKey = CryptoProvider.DeriveKeyFromPassword(plainPassword, settings.PasswordSalt);
            settings.EncryptedDataKey = CryptoProvider.Encrypt(masterDek, passwordKey);
            
            // 6. Generate a Salt for Recovery Key Derivation
            byte[] recoverySaltBytes = CryptoProvider.CreateSalt();
            settings.RecoverySalt = Convert.ToBase64String(recoverySaltBytes);

            // 7. Encrypt the DEK with the recovery code (Salted)
            string recoveryKey = CryptoProvider.DeriveKeyFromPassword(recoveryCode, settings.RecoverySalt);
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
                // Load the DEK into memory
                string passwordKey;
                bool needsSaltUpgrade = string.IsNullOrEmpty(settings.PasswordSalt);

                if (needsSaltUpgrade)
                {
                    // Legacy unsalted derivation
                    passwordKey = CryptoProvider.DeriveKeyFromPasswordLegacy(inputPassword);
                }
                else
                {
                    // Modern salted derivation
                    passwordKey = CryptoProvider.DeriveKeyFromPassword(inputPassword, settings.PasswordSalt!);
                }
                
                ActiveDataKey = CryptoProvider.Decrypt(settings.EncryptedDataKey!, passwordKey);
                
                // Seamlessly upgrade the hash to PBKDF2 if it was old
                if (needsUpgrade)
                {
                    settings.HashedPassword = HashPassword(inputPassword);
                }

                // Seamlessly upgrade key derivation to salted if it was unsalted
                if (needsSaltUpgrade && !string.IsNullOrEmpty(ActiveDataKey))
                {
                    byte[] saltBytes = CryptoProvider.CreateSalt();
                    settings.PasswordSalt = Convert.ToBase64String(saltBytes);
                    
                    string newPasswordKey = CryptoProvider.DeriveKeyFromPassword(inputPassword, settings.PasswordSalt);
                    settings.EncryptedDataKey = CryptoProvider.Encrypt(ActiveDataKey, newPasswordKey);
                    
                    // Also upgrade the RecoveryEncryptedDataKey if one exists
                    // We don't have the plaintext recovery code, but we know the RecoveryEncryptedDataKey
                    // Wait, we CANNOT re-encrypt the recovery code because we don't know it!
                    // But if we change the salt, we must re-encrypt the RecoveryKey.
                    // This means the user MUST generate a new recovery code, or we store a separate salt for recovery.
                    // Since storing a separate salt is complex, and the recovery code is rarely used,
                    // we'll keep the recovery key derivation unsalted if they don't change their password,
                    // OR we could introduce RecoveryPasswordSalt. Let's add RecoveryPasswordSalt to Settings.
                }

                // Always save to ensure .keys file exists and is up to date
                SaveSettings(settings);

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

            // Generate a new salt for the new password
            byte[] newSaltBytes = CryptoProvider.CreateSalt();
            settings.PasswordSalt = Convert.ToBase64String(newSaltBytes);

            // Re-encrypt the existing DEK with the new password
            string newPasswordKey = CryptoProvider.DeriveKeyFromPassword(newPassword, settings.PasswordSalt);
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

            string recoveryKey;
            if (string.IsNullOrEmpty(settings.RecoverySalt))
            {
                // Legacy unsalted recovery derivation
                recoveryKey = CryptoProvider.DeriveKeyFromPasswordLegacy(recoveryCode);
            }
            else
            {
                // Modern salted recovery derivation
                recoveryKey = CryptoProvider.DeriveKeyFromPassword(recoveryCode, settings.RecoverySalt);
            }
            
            string decryptedDek = CryptoProvider.Decrypt(settings.RecoveryEncryptedDataKey, recoveryKey);

            // If decryption failed, it returns the cipherText itself
            if (decryptedDek == settings.RecoveryEncryptedDataKey)
                return false;

            // Decryption succeeded, we have the Master DEK
            
            // Generate a new salt for the new password
            byte[] newSaltBytes = CryptoProvider.CreateSalt();
            settings.PasswordSalt = Convert.ToBase64String(newSaltBytes);

            // 1. Re-encrypt DEK with the new password
            string newPasswordKey = CryptoProvider.DeriveKeyFromPassword(newPassword, settings.PasswordSalt);
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
