using System;
using System.IO;

namespace FinTrack.Core.Helpers
{
    public static class AppLogger
    {
        private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinTrack", "Logs");
        private static readonly string LogFilePath;
        private static readonly object _lock = new object();

        static AppLogger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Create a daily log file
                LogFilePath = Path.Combine(LogDirectory, $"FinTrack_{DateTime.Now:yyyyMMdd}.log");
            }
            catch
            {
                // Fallback if we don't have permission to write to AppData
                LogFilePath = "FinTrack_fallback.log";
            }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            string logMessage = message;
            if (ex != null)
            {
                logMessage += $"\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
                
                Exception? inner = ex.InnerException;
                int depth = 1;
                while (inner != null)
                {
                    logMessage += $"\n  InnerException {depth}: {inner.GetType().Name}: {inner.Message}\n  StackTrace: {inner.StackTrace}";
                    inner = inner.InnerException;
                    depth++;
                }
            }
            WriteLog("ERROR", logMessage);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, logLine);
                }
            }
            catch
            {
                // If logger fails, we don't want to crash the app, just swallow the error
            }
        }
    }
}
