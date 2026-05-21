using System;
using FinTrack.Core.Services;

namespace TestApp {
    class Program {
        static void Main() {
            try {
                SettingsManager.CreateProfile(""TestProfile"", @""C:\temp\test.db"");
                Console.WriteLine(""Success. Profiles: "" + string.Join("", "", SettingsManager.GetProfiles()));
            } catch (Exception ex) {
                Console.WriteLine(""Error: "" + ex.Message);
            }
        }
    }
}
