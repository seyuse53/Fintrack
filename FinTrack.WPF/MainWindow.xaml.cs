using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using FinTrack.WPF.Helpers;
using FinTrack.WPF.Views;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF
{
    public partial class MainWindow : Window
    {
        public static int GlobalSelectedMonth { get; private set; }
        public static int GlobalSelectedYear { get; private set; }
        
        private readonly AppDbContext _context;
        private bool _isInitializingFilters = true;
        private DashboardView _dashboardView = null!;
        private BudgetView _budgetView = null!;
        private ReportsView _reportsView = null!;
        private CardsView _cardsView = null!; // Added CardsView declaration
        private AccountsView _accountsView = null!;
        private InvestmentsView _investmentsView = null!;
        private SettingsView _settingsView = null!;
        private UserControl _currentView = null!;

        // Search fields
        private CancellationTokenSource? _searchCts;
        private System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;

        public MainWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;

            // Global keyboard shortcut for search (Ctrl+K)
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            Loaded += async (_, _) =>
            {
                // Display current version
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.4";
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

            SetupInitialFilters();
        }

        private void SetupInitialFilters()
        {
            var culture = new System.Globalization.CultureInfo("tr-TR");
            var months = culture.DateTimeFormat.MonthNames.Where(m => !string.IsNullOrEmpty(m)).Select(m => char.ToUpper(m[0]) + m.Substring(1)).ToArray();
            MonthFilter.ItemsSource = months;
            
            int currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 5, 10).ToList();
            YearFilter.ItemsSource = years;

            GlobalSelectedMonth = DateTime.Now.Month;
            GlobalSelectedYear = currentYear;

            MonthFilter.SelectedIndex = GlobalSelectedMonth - 1;
            YearFilter.SelectedItem = GlobalSelectedYear;

            _isInitializingFilters = false;
        }

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingFilters) return;
            
            GlobalSelectedMonth = MonthFilter.SelectedIndex + 1;
            if (YearFilter.SelectedItem is int year)
            {
                GlobalSelectedYear = year;
            }

            // Reload current view if it depends on data
            if (_currentView is DashboardView dv)
                await dv.LoadDataAsync();
            else if (_currentView is BudgetView bv)
                await bv.InitializeAsync(_context);
            else if (_currentView is ReportsView rv)
                await rv.LoadDataAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                // Current version of the app from assembly
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.4";
                
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
            Dispatcher.Invoke(() =>
            {
                // Close all other open windows for security
                var windows = System.Windows.Application.Current.Windows;
                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    var w = windows[i];
                    if (w != this)
                    {
                        try { w.Close(); } catch { }
                    }
                }

                LockScreenOverlay.Visibility = Visibility.Visible;
                AppContentContainer.Visibility = Visibility.Collapsed;
                LockPasswordInput.Clear();
                LockErrorText.Visibility = Visibility.Collapsed;
                LockPasswordInput.Focus();
            });
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            VerifyAndUnlock();
        }

        private void LockPasswordInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                VerifyAndUnlock();
            }
        }

        private void VerifyAndUnlock()
        {
            string password = LockPasswordInput.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                LockErrorText.Text = "Lütfen uygulama şifrenizi girin.";
                LockErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (FinTrack.Core.Services.SettingsManager.VerifyPasswordAndLoadKey(password))
            {
                LockScreenOverlay.Visibility = Visibility.Collapsed;
                AppContentContainer.Visibility = Visibility.Visible;
                LockPasswordInput.Clear();
                FinTrack.WPF.Services.AutoLockService.MarkUnlocked();
            }
            else
            {
                LockErrorText.Text = "Hatalı şifre girdiniz.";
                LockErrorText.Visibility = Visibility.Visible;
                LockPasswordInput.Clear();
                LockPasswordInput.Focus();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
            else if (view is CardsView cv) 
                await cv.InitializeAsync(_context);
            else if (view is AccountsView av)
                await av.InitializeAsync(_context);
            else if (view is InvestmentsView iv)
                await iv.InitializeAsync(_context);
                
            // Sadece finansal verilerin olduğu pencerelerde filtreyi göster
            if (view == _dashboardView)
                GlobalDateFilterPanel.Visibility = Visibility.Visible;
            else
                GlobalDateFilterPanel.Visibility = Visibility.Collapsed;
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
                else if (_currentView is AccountsView av)
                    await av.LoadAccountsAsync();
                else if (_currentView is CardsView cv)
                    await cv.LoadCardsAsync();
                else if (_currentView is InvestmentsView iv)
                    await iv.LoadInvestmentsAsync();
            }
        }

        // ==================== GLOBAL SEARCH ====================

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+K → Focus search box
            if (e.Key == Key.K && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
            SearchBoxBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // #3498DB
        }

        private void SearchResultsPopup_Closed(object sender, EventArgs e)
        {
            // Restore placeholder if empty
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
                SearchBoxBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 219, 219)); // #D5DBDB
            }
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchResultsPopup.IsOpen = false;
                SearchTextBox.Text = "";
                SearchClearButton.Visibility = Visibility.Collapsed;
                SearchPlaceholder.Visibility = Visibility.Visible;
                SearchBoxBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 219, 219)); // #D5DBDB

                // Return focus to main content
                MainContentArea.Focus();
                e.Handled = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();

            // Toggle placeholder and clear button
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            SearchClearButton.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Collapsed : Visibility.Visible;

            // Cancel any previous pending search
            _searchDebounceTimer?.Stop();

            if (query.Length < 2)
            {
                SearchResultsPopup.IsOpen = false;
                return;
            }

            // Debounce: wait 300ms before executing search
            _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += async (s, _) =>
            {
                _searchDebounceTimer.Stop();
                await ExecuteSearchAsync(query);
            };
            _searchDebounceTimer.Start();
        }

        private void SearchClear_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SearchResultsPopup.IsOpen = false;
            SearchClearButton.Visibility = Visibility.Collapsed;
            SearchPlaceholder.Visibility = Visibility.Visible;
            SearchBoxBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 219, 219)); // #D5DBDB
        }

        private async Task ExecuteSearchAsync(string query)
        {
            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // Show loading state
                SearchResultsPopup.IsOpen = true;
                SearchLoading.Visibility = Visibility.Visible;
                SearchNoResults.Visibility = Visibility.Collapsed;
                SearchResultsScroll.Visibility = Visibility.Collapsed;

                var groups = new List<SearchResultGroup>();

                // 1. Search Transactions
                var transactionResults = await SearchTransactionsAsync(query, token);
                if (token.IsCancellationRequested) return;
                if (transactionResults.Count > 0)
                {
                    groups.Add(new SearchResultGroup
                    {
                        GroupTitle = $"💸 İŞLEMLER ({transactionResults.Count})",
                        Results = transactionResults
                    });
                }

                // 2. Search Bank Accounts
                var bankResults = await SearchBankAccountsAsync(query, token);
                if (token.IsCancellationRequested) return;
                if (bankResults.Count > 0)
                {
                    groups.Add(new SearchResultGroup
                    {
                        GroupTitle = $"🏦 HESAPLAR ({bankResults.Count})",
                        Results = bankResults
                    });
                }

                // 3. Search Credit Cards
                var cardResults = await SearchCreditCardsAsync(query, token);
                if (token.IsCancellationRequested) return;
                if (cardResults.Count > 0)
                {
                    groups.Add(new SearchResultGroup
                    {
                        GroupTitle = $"💳 KREDİ KARTLARI ({cardResults.Count})",
                        Results = cardResults
                    });
                }

                // 4. Search Investment Assets
                var investmentResults = await SearchInvestmentsAsync(query, token);
                if (token.IsCancellationRequested) return;
                if (investmentResults.Count > 0)
                {
                    groups.Add(new SearchResultGroup
                    {
                        GroupTitle = $"📈 YATIRIMLAR ({investmentResults.Count})",
                        Results = investmentResults
                    });
                }

                // Update UI
                SearchLoading.Visibility = Visibility.Collapsed;

                if (groups.Count == 0)
                {
                    SearchNoResults.Visibility = Visibility.Visible;
                    SearchResultsScroll.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SearchNoResults.Visibility = Visibility.Collapsed;
                    SearchResultsScroll.Visibility = Visibility.Visible;
                    SearchResultsList.ItemsSource = groups;
                }
            }
            catch (OperationCanceledException) { /* Search was cancelled, ignore */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                SearchLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<List<SearchResult>> SearchTransactionsAsync(string query, CancellationToken token)
        {
            // Load transactions with related data into memory (because Description is encrypted)
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                    .ThenInclude(c => c!.ParentCategory)
                .Include(t => t.CreditCardAccount)
                .Include(t => t.BankAccount)
                .OrderByDescending(t => t.Date)
                .ToListAsync(token);

            var lowerQuery = query.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            return transactions
                .Where(t =>
                    (t.Description != null && t.Description.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery)) ||
                    (t.Category?.FullDisplayName != null && t.Category.FullDisplayName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery)) ||
                    t.Amount.ToString("N2").Contains(query) ||
                    t.DisplayAmount.ToString("N2").Contains(query) ||
                    t.Date.ToString("dd.MM.yyyy").Contains(query) ||
                    t.Date.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")).ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    (t.CreditCardAccount != null && t.CreditCardAccount.DisplayName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery)) ||
                    (t.BankAccount != null && ($"{t.BankAccount.BankName} {t.BankAccount.AccountName}").ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery)))
                .Take(8)
                .Select(t =>
                {
                    string icon = t.Category?.Type == TransactionType.Income ? "💰" :
                                  t.Category?.Type == TransactionType.Transfer ? "🔄" : "💸";
                    string subtitle = t.Category?.FullDisplayName ?? "";
                    if (t.CreditCardAccount != null)
                        subtitle += $" · {t.CreditCardAccount.DisplayName}";
                    else if (t.BankAccount != null)
                        subtitle += $" · {t.BankAccount.BankName}";
                    subtitle += $" · {t.Date:dd MMM yyyy}";

                    return new SearchResult
                    {
                        Icon = icon,
                        Title = string.IsNullOrEmpty(t.Description) ? (t.Category?.FullDisplayName ?? "İşlem") : t.Description,
                        Subtitle = subtitle,
                        Amount = t.FormattedAmount,
                        AmountColor = t.ForegroundColor,
                        ResultType = "Transaction",
                        EntityId = t.Id
                    };
                })
                .ToList();
        }

        private async Task<List<SearchResult>> SearchBankAccountsAsync(string query, CancellationToken token)
        {
            var lowerQuery = query.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            var accounts = await _context.BankAccounts
                .Where(a => a.IsActive)
                .ToListAsync(token);

            return accounts
                .Where(a =>
                    a.BankName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    a.AccountName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    (a.IBAN != null && a.IBAN.Replace(" ", "").ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery.Replace(" ", ""))))
                .Take(5)
                .Select(a => new SearchResult
                {
                    Icon = "🏦",
                    Title = $"{a.BankName} - {a.AccountName}",
                    Subtitle = a.IBAN != null ? UIHelper.FormatIban(a.IBAN) : "IBAN belirtilmemiş",
                    ResultType = "BankAccount",
                    EntityId = a.Id
                })
                .ToList();
        }

        private async Task<List<SearchResult>> SearchCreditCardsAsync(string query, CancellationToken token)
        {
            var lowerQuery = query.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            var cards = await _context.CreditCardAccounts
                .Where(c => c.IsActive)
                .ToListAsync(token);

            return cards
                .Where(c =>
                    c.BankName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    c.CardLabel.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    c.DisplayName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery))
                .Take(5)
                .Select(c => new SearchResult
                {
                    Icon = "💳",
                    Title = c.DisplayName,
                    Subtitle = c.Limit > 0 ? $"Limit: ₺{c.Limit:N2}" : "Limit belirtilmemiş",
                    ResultType = "CreditCard",
                    EntityId = c.Id
                })
                .ToList();
        }

        private async Task<List<SearchResult>> SearchInvestmentsAsync(string query, CancellationToken token)
        {
            var lowerQuery = query.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            var assets = await _context.InvestmentAssets.ToListAsync(token);

            return assets
                .Where(a =>
                    a.Name.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    a.Symbol.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery) ||
                    (a.Category != null && a.Category.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(lowerQuery)))
                .Take(5)
                .Select(a => new SearchResult
                {
                    Icon = "📈",
                    Title = $"{a.Name} ({a.Symbol})",
                    Subtitle = $"{a.Category ?? "Yatırım"} · Miktar: {a.TotalAmount:N4}",
                    Amount = $"Ort: ₺{a.AverageCost:N2}",
                    AmountColor = "#2C3E50",
                    ResultType = "Investment",
                    EntityId = a.Id
                })
                .ToList();
        }

        private async void SearchResultItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SearchResult result)
            {
                SearchResultsPopup.IsOpen = false;

                switch (result.ResultType)
                {
                    case "Transaction":
                        var transaction = await _context.Transactions
                            .Include(t => t.Category)
                                .ThenInclude(c => c!.ParentCategory)
                            .Include(t => t.CreditCardAccount)
                            .Include(t => t.BankAccount)
                            .FirstOrDefaultAsync(t => t.Id == result.EntityId);
                        if (transaction != null)
                        {
                            var editWindow = new EditTransactionWindow(_context, transaction)
                            {
                                Owner = this
                            };
                            if (editWindow.ShowDialog() == true)
                            {
                                await RefreshCurrentView();
                            }
                        }
                        break;

                    case "BankAccount":
                        var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == result.EntityId);
                        if (account != null)
                        {
                            var accountTxs = await _context.Transactions
                                .Include(t => t.Category)
                                    .ThenInclude(c => c!.ParentCategory)
                                .Where(t => t.BankAccountId == account.Id)
                                .OrderByDescending(t => t.Date)
                                .ToListAsync();

                            var detailWin = new AccountDetailWindow(account, accountTxs, _context)
                            {
                                Owner = this
                            };
                            detailWin.ShowDialog();
                            await RefreshCurrentView();
                        }
                        break;

                    case "CreditCard":
                        var card = await _context.CreditCardAccounts.FirstOrDefaultAsync(c => c.Id == result.EntityId);
                        if (card != null)
                        {
                            var period = card.GetStatementPeriod(DateTime.Now);
                            var familyIds = new List<int> { card.Id };
                            var childIds = await _context.CreditCardAccounts
                                .Where(c => c.ParentCardId == card.Id)
                                .Select(c => c.Id)
                                .ToListAsync();
                            familyIds.AddRange(childIds);

                            var cardTxs = await _context.Transactions
                                .Include(t => t.Category)
                                .Include(t => t.CreditCardAccount)
                                .Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0))
                                .OrderByDescending(t => t.Date)
                                .ToListAsync();
                            decimal total = cardTxs.Sum(t => t.Amount);

                            var cardDetailWin = new CardDetailWindow(
                                card.DisplayName,
                                $"{period.Start:dd MMM yyyy} - {period.End:dd MMM yyyy} Ekstresi",
                                cardTxs, total, _context, card)
                            {
                                Owner = this
                            };
                            cardDetailWin.ShowDialog();
                            await RefreshCurrentView();
                        }
                        break;

                    case "Investment":
                        // Switch to Investments view
                        await SwitchToView(_investmentsView, "Yatırım Portföyü", InvestmentsButton);
                        break;
                }
            }
        }

        private void SearchResultItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
                border.Background = new SolidColorBrush(Color.FromRgb(235, 245, 251)); // #EBF5FB
        }

        private void SearchResultItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
                border.Background = Brushes.Transparent;
        }

        private async Task RefreshCurrentView()
        {
            if (_currentView is DashboardView dv)
                await dv.LoadDataAsync();
            else if (_currentView is BudgetView bv)
                await bv.InitializeAsync(_context);
            else if (_currentView is ReportsView rv)
                await rv.LoadDataAsync();
            else if (_currentView is AccountsView av)
                await av.LoadAccountsAsync();
            else if (_currentView is CardsView cv)
                await cv.LoadCardsAsync();
            else if (_currentView is InvestmentsView iv)
                await iv.LoadInvestmentsAsync();
        }
    }
}
