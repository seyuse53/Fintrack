using System;
using System.IO;
using System.Linq;

namespace FinTrack.Core.Services
{
    public static class BackupService
    {
        public static void PerformBackup()
        {
            try
            {
                var (enabled, backupDir, maxCount, maxAgeDays) = SettingsManager.GetBackupSettings();

                if (!enabled || string.IsNullOrEmpty(backupDir) || !Directory.Exists(backupDir))
                    return; // Backup is mostly disabled or directory is invalid

                // Get current DB path
                string currentDbPath = SettingsManager.GetDatabasePath();
                if (!File.Exists(currentDbPath))
                    return;

                // Create backup file name with timestamp
                string dbFileName = Path.GetFileNameWithoutExtension(currentDbPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"{dbFileName}_{timestamp}.db.bak";
                string backupFilePath = Path.Combine(backupDir, backupFileName);

                // Perform raw file copy (this copies the fully encrypted SQLCipher database as-is)
                File.Copy(currentDbPath, backupFilePath, true);

                // Run cleanup
                CleanupOldBackups(backupDir, dbFileName, maxCount, maxAgeDays);
            }
            catch (Exception ex)
            {
                // In a desktop app on exit, we shouldn't throw to avoid crashing the exit process
                // Ideally this would be logged to a file if a logger existed
                System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
            }
        }

        private static void CleanupOldBackups(string backupDir, string dbFileNamePrefix, int maxCount, int maxAgeDays)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(backupDir);
                
                // Get all backup files for this specific profile/db
                var backupFiles = directoryInfo.GetFiles($"{dbFileNamePrefix}_*.db.bak")
                                               .OrderByDescending(f => f.CreationTime)
                                               .ToList();

                DateTime cutoffDate = DateTime.Now.AddDays(-maxAgeDays);

                for (int i = 0; i < backupFiles.Count; i++)
                {
                    var file = backupFiles[i];

                    // Determine if the file should be deleted
                    bool deleteByCount = i >= maxCount;
                    bool deleteByAge = file.CreationTime < cutoffDate;

                    if (deleteByCount || deleteByAge)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete old backup {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup cleanup failed: {ex.Message}");
            }
        }
    }
}
