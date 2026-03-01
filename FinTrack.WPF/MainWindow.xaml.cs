using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using FinTrack.WPF.Views;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF
{
    public partial class MainWindow : Window
    {
        private readonly AppDbContext _context;
        private DashboardView _dashboardView = null!;
        private BudgetView _budgetView = null!;
        private ReportsView _reportsView = null!;
        private CardsView _cardsView = null!; // Added CardsView declaration
        private AccountsView _accountsView = null!;
        private InvestmentsView _investmentsView = null!;
        private SettingsView _settingsView = null!;
        private UserControl _currentView = null!;

        public MainWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;

            Loaded += async (_, _) =>
            {
                // Display current version
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.1";
                VersionText.Text = $"v{currentVersion}";

                // Pre-initialize views
                _dashboardView = new DashboardView();
                _budgetView = new BudgetView();
                _reportsView = new ReportsView();
                _cardsView = new CardsView(); // Initialized CardsView
                _accountsView = new AccountsView();
                _investmentsView = new InvestmentsView();
                _settingsView = new SettingsView();

                // Show Dashboard by default
                await SwitchToView(_dashboardView, "Ana Ekran (Özet)", DashboardButton);
                
                // Backup Prompt
                CheckAndPromptForBackup();

                // Start global Auto-Lock watcher
                FinTrack.WPF.Services.AutoLockService.OnLockTriggered += AutoLockService_OnLockTriggered;
                FinTrack.WPF.Services.AutoLockService.Start();

                // Check for Updates
                _ = CheckForUpdatesAsync();
            };
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                // Current version of the app from assembly
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.1";
                
                var githubService = new GitHubReleaseService();
                var result = await githubService.CheckForUpdatesAsync(currentVersion);

                if (result.IsUpdateAvailable)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var updateWindow = new UpdateAvailableWindow(currentVersion, result)
                        {
                            Owner = this
                        };
                        updateWindow.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during update check: {ex.Message}");
            }
        }

        private void AutoLockService_OnLockTriggered()
        {
            // Show the lock screen on the UI thread
            Dispatcher.Invoke(() =>
            {
                var lockScreen = new FinTrack.WPF.Views.LockScreenWindow
                {
                    Owner = this
                };
                
                // ShowDialog halts execution of the main thread until the window is closed
                lockScreen.ShowDialog();
            });
        }

        private void CheckAndPromptForBackup()
        {
            var backupSettings = FinTrack.Core.Services.SettingsManager.GetBackupSettings();
            
            // Eğer yedekleme açıksa veya daha önce bir klasör seçilmişse sorma.
            // Ayrıca, kullanıcı daha önce "Hayır" dediyse klasör yolu özel bir değer (örn: "DECLINED") olabilir, ama şimdilik boş olup olmadığına bakacağız.
            // Daha akıllı bir yöntem: Eğer ayar dosyasına yeni eklendiyse varsayılan değerler gelir (Enabled = false, Directory = null)
            // Biz sadece klasör boşsa ve daha önce sorulmadığından emin olmak için basit bir kontrol yapıyoruz.
            
            // Not: İdeal olarak Settings dosyasına "HasPromptedForBackup" gibi bir boolean eklenebilir, 
            // ama Settings şemasını değiştirmeden mevcut Directory özelliğini kullanarak bir bayrak oluşturabiliriz.
            if (string.IsNullOrEmpty(backupSettings.Directory))
            {
                var dialog = new GeneralConfirmWindow(
                    "Otomatik Yedekleme Kurulumu",
                    "Veri güvenliğiniz için otomatik yedekleme sistemini aktifleştirmek ister misiniz?\n\nBu özellik sayesinde, uygulamayı her kapattığınızda veritabanınızın güvenli bir kopyası belirlediğiniz klasöre (örn: OneDrive, Google Drive) kopyalanır.",
                    "Evet, Aktifleştir",
                    "Hayır, İstemiyorum"
                )
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    // Ayarlar sekmesini aç ve Bulut/Yedekleme tabını seç (Index 1)
                    _settingsView.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_settingsView.Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is TabControl tabControl)
                        {
                            tabControl.SelectedIndex = 1; // 1. indeks "Bulut ve Yedekleme" tabıdır (0: Güvenlik, 1: Bulut, 2: API, 3: Klavuz, 4: Hakkında)
                        }
                    }));
                    
                    SwitchToView(_settingsView, "Genel Ayarlar", SettingsButton).ConfigureAwait(false);
                }
                else
                {
                    // Bir daha sormaması için kalıcı bir "reddedildi" bayrağı olarak geçersiz bir dizin kaydedebiliriz veya şimdilik böyle bırakabiliriz.
                    // Şemayı bozmamak adına şimdilik boş bırakıyoruz, kullanıcı her girişte değil, belki ilerde tekrar sorulur (veya sadece 1 kere sorulması için "DECLINED" yazılabilir).
                     FinTrack.Core.Services.SettingsManager.SaveBackupSettings(false, "USER_DECLINED", 5, 30);
                }
            }
        }

        private async Task SwitchToView(UserControl view, string title, Button activeButton)
        {
            if (_currentView == view) return;

            _currentView = view;
            MainContentArea.Content = view;
            ViewTitle.Text = title;

            // Update sidebar button styles
            ResetSidebarButtons();
            SetActiveButtonStyle(activeButton);

            // Initialize or Refresh data
            if (view is DashboardView dv)
                await dv.InitializeAsync(_context);
            else if (view is BudgetView bv)
                await bv.InitializeAsync(_context);
            else if (view is ReportsView rv)
                await rv.InitializeAsync(_context);
            else if (view is CardsView cv) // Added initialization for CardsView
                await cv.InitializeAsync(_context);
            else if (view is AccountsView av)
                await av.InitializeAsync(_context);
            else if (view is InvestmentsView iv)
                await iv.InitializeAsync(_context);
        }

        private void ResetSidebarButtons()
        {
            var transparent = Brushes.Transparent;
            var defaultFore = new SolidColorBrush(Color.FromRgb(236, 240, 241)); // #ECF0F1

            DashboardButton.Background = transparent;
            DashboardButton.Foreground = defaultFore;

            BudgetButton.Background = transparent;
            BudgetButton.Foreground = defaultFore;

            ReportsButton.Background = transparent;
            ReportsButton.Foreground = defaultFore;

            CardsButton.Background = transparent; // Reset style for CardsButton
            CardsButton.Foreground = defaultFore; // Reset style for CardsButton

            AccountsButton.Background = transparent;
            AccountsButton.Foreground = defaultFore;

            InvestmentsButton.Background = transparent;
            InvestmentsButton.Foreground = defaultFore;

            SettingsButton.Background = transparent;
            SettingsButton.Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)); // #95A5A6
        }

        private void SetActiveButtonStyle(Button button)
        {
            if (button == null) return;
            button.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)); // #34495E
            button.Foreground = Brushes.White;
        }

        private async void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            await SwitchToView(_dashboardView, "Ana Ekran (Özet)", DashboardButton);
        }

        private async void BudgetButton_Click(object sender, RoutedEventArgs e)
        {
            await SwitchToView(_budgetView, "Bütçe Yönetimi", BudgetButton);
        }

        private async void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            await SwitchToView(_reportsView, "Gelişmiş Raporlar", ReportsButton);
        }

        private async void InvestmentsButton_Click(object sender, RoutedEventArgs e)
        {
            await _investmentsView.LoadInvestmentsAsync();
            await SwitchToView(_investmentsView, "Yatırım Portföyü", InvestmentsButton);
        }

        private async void CardsButton_Click(object sender, RoutedEventArgs e)
        {
            await _cardsView.LoadCardsAsync(); // Refresh before showing
            await SwitchToView(_cardsView, "Kredi Kartları", CardsButton); // Switched to CardsView
        }

        private async void AccountsButton_Click(object sender, RoutedEventArgs e)
        {
            await _accountsView.LoadAccountsAsync();
            await SwitchToView(_accountsView, "Banka Hesapları", AccountsButton);
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await SwitchToView(_settingsView, "Ayarlar", SettingsButton);
        }


        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddTransactionWindow(_context);
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                // Refresh active view
                if (_currentView is DashboardView dv)
                    await dv.LoadDataAsync();
                else if (_currentView is BudgetView bv)
                    await bv.InitializeAsync(_context);
                else if (_currentView is ReportsView rv)
                    await rv.LoadDataAsync();
            }
        }
    }
}
