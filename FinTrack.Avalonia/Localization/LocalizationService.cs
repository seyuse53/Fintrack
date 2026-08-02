using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using FinTrack.Core.Services;
using FinTrack.Core.Helpers;

namespace FinTrack.Avalonia.Localization
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private readonly ResourceManager _resourceManager;

        public event PropertyChangedEventHandler? PropertyChanged;

        private int _languageVersion = 0;
        public int LanguageVersion
        {
            get => _languageVersion;
            set
            {
                _languageVersion = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageVersion)));
            }
        }

        private LocalizationService()
        {
            _resourceManager = new ResourceManager("FinTrack.Avalonia.Resources.Strings", Assembly.GetExecutingAssembly());
            var lang = SettingsManager.GetLanguagePreference();
            var specificCultureCode = lang switch
            {
                "tr" => "tr-TR",
                "en" => "en-US",
                _ => lang
            };
            var culture = new CultureInfo(specificCultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        [IndexerName("Item")]
        public string this[string key]
        {
            get
            {
                var val = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
                return string.IsNullOrEmpty(val) ? key : val;
            }
        }

        public static string GetString(string key) => Instance[key];

        public void SetLanguage(string languageCode)
        {
            AppLogger.Info($"[LocalizationService] Changing language to: {languageCode}");
            SettingsManager.SetLanguagePreference(languageCode);
            
            var specificCultureCode = languageCode switch
            {
                "tr" => "tr-TR",
                "en" => "en-US",
                _ => languageCode
            };
            var culture = new CultureInfo(specificCultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            // Increment version to trigger property changed for all converter bindings
            LanguageVersion++;
            
            // Avalonia indexer bindings react to "Item[]"
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]")); 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty)); 
        }

        public static void ToggleLanguage()
        {
            var current = SettingsManager.GetLanguagePreference();
            var next = (current == "tr" || current == "tr-TR") ? "en" : "tr";
            AppLogger.Info($"[LocalizationService] ToggleLanguage clicked. Current: {current}, Next: {next}");
            Instance.SetLanguage(next);
        }
    }
}
