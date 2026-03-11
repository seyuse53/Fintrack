using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace FinTrack.Core.Services
{
    public enum ApiProviderType { Manual = 0, YahooFinance = 1, Custom = 2 }

    public class Settings
    {
        public string HashedPassword { get; set; }
        public string EncryptedDataKey { get; set; }
        public string RecoveryEncryptedDataKey { get; set; }
        public string DatabasePath { get; set; }
        public ApiProviderType PricingApiProvider { get; set; } = ApiProviderType.Manual;
        public string CustomApiUrl { get; set; }

        public bool BackupEnabled { get; set; } = false;
        public string BackupDirectory { get; set; }
        public int MaxBackupCount { get; set; } = 5;
        public int MaxBackupAgeDays { get; set; } = 30;

        public bool AutoLockEnabled { get; set; } = true;
        public int AutoLockTimeoutMinutes { get; set; } = 3;
    }

    class Program {
        static void Main() {
            try {
                string keysFile = @"C:\Users\kadir\Documents\FinTrack\fintrack_Test.db.keys";
                if (File.Exists(keysFile)) {
                    string keysJson = File.ReadAllText(keysFile);
                    var keysData = JsonSerializer.Deserialize<Settings>(keysJson);
                    Console.WriteLine("HashedPassword: " + keysData.HashedPassword);
                } else {
                    Console.WriteLine("File not found");
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }
    }
}
