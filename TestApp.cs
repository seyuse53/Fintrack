using System;
using System.IO;
using System.Text.Json;
using FinTrack.Core.Services;

class Program {
    static void Main() {
        var profiles = SettingsManager.GetProfiles();
        SettingsManager.DeleteProfile("T3", false);
        SettingsManager.CreateProfile("T3", @"C:\Users\kadir\Documents\FinTrack\fintrack_K_private.db");
        string sJson = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings_T3.json"));
        Console.WriteLine(sJson);
    }
}
