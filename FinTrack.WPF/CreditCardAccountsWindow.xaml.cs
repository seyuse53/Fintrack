using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF
{
    public partial class CreditCardAccountsWindow : Window
    {
        private readonly AppDbContext _db;

        public CreditCardAccountsWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            LoadCards();
        }

        private void LoadCards()
        {
            var cards = _db.CreditCardAccounts
                           .Include(c => c.ParentCard)
                           .OrderBy(c => c.BankName)
                           .ThenBy(c => c.CardLabel)
                           .ToList();
            CardsGrid.ItemsSource = cards;
            LoadParentOptions();
        }

        private void BankNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadParentOptions();
        }

        private void LimitBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FinTrack.WPF.Helpers.UIHelper.FormatAmountTextBox(sender as TextBox);
        }

        private void LoadParentOptions(int? currentParentId = null)
        {
            if (_db == null || ParentCardCombo == null) return;
            string bank = BankNameBox.Text.Trim();

            // Potential parents must be in the same bank and NOT be linked to another parent themselves
            // (We only support 1-level hierarchy for simplicity)
            var options = _db.CreditCardAccounts
                .Where(c => c.BankName == bank && c.ParentCardId == null)
                .ToList();

            if (CardsGrid.SelectedItem is CreditCardAccount selected)
            {
                options = options.Where(c => c.Id != selected.Id).ToList();
            }

            var list = new System.Collections.Generic.List<CreditCardAccount>
            {
                new CreditCardAccount { Id = -1, CardLabel = "-- Yok (Ana Kart) --", BankName = bank }
            };
            list.AddRange(options);

            ParentCardCombo.ItemsSource = list;

            if (currentParentId.HasValue && currentParentId.Value != 0)
                ParentCardCombo.SelectedValue = currentParentId.Value;
            else
                ParentCardCombo.SelectedIndex = 0;
        }

        private void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardsGrid.SelectedItem is CreditCardAccount card)
            {
                BankNameBox.Text = card.BankName;
                CardLabelBox.Text = card.CardLabel;
                LimitBox.Text = card.Limit.ToString("N2");
                StatementDayBox.Text = card.StatementDay.ToString();
                PaymentDueDayBox.Text = card.PaymentDueDay.ToString();
                
                LoadParentOptions(card.ParentCardId);

                DeleteCardBtn.IsEnabled = true;
                ToggleActiveBtn.IsEnabled = true;
                UpdateCardBtn.IsEnabled = true;
            }
            else
            {
                BankNameBox.Clear();
                CardLabelBox.Clear();
                LimitBox.Clear();
                StatementDayBox.Clear();
                PaymentDueDayBox.Clear();
                DeleteCardBtn.IsEnabled = false;
                ToggleActiveBtn.IsEnabled = false;
                UpdateCardBtn.IsEnabled = false;
            }
        }

        private void AddCard_Click(object sender, RoutedEventArgs e)
        {
            string bank = BankNameBox.Text.Trim();
            string label = CardLabelBox.Text.Trim();

            if (string.IsNullOrEmpty(bank) || string.IsNullOrEmpty(label))
            {
                MessageBox.Show("Banka adı ve kart etiketi boş olamaz.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(StatementDayBox.Text) || string.IsNullOrWhiteSpace(PaymentDueDayBox.Text) ||
                !int.TryParse(StatementDayBox.Text, out int sDay) || sDay < 1 || sDay > 31 ||
                !int.TryParse(PaymentDueDayBox.Text, out int pDay) || pDay < 1 || pDay > 31)
            {
                MessageBox.Show("Kesim günü ve Son ödeme günü zorunludur ve 1 ile 31 arasında geçerli bir sayı olmalıdır.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!FinTrack.WPF.Helpers.UIHelper.TryParseAmount(LimitBox.Text, out decimal limit))
            {
                limit = 0;
            }

            // Check duplicate
            bool exists = _db.CreditCardAccounts
                             .Any(c => c.BankName == bank && c.CardLabel == label);
            if (exists)
            {
                MessageBox.Show("Bu kart zaten kayıtlı.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? parentId = (int?)ParentCardCombo.SelectedValue;
            if (parentId == -1) parentId = null;

            _db.CreditCardAccounts.Add(new CreditCardAccount
            {
                BankName = bank,
                CardLabel = label,
                Limit = limit,
                StatementDay = sDay,
                PaymentDueDay = pDay,
                ParentCardId = parentId,
                IsActive = true
            });
            _db.SaveChanges();

            BankNameBox.Clear();
            CardLabelBox.Clear();
            LimitBox.Clear();
            StatementDayBox.Clear();
            PaymentDueDayBox.Clear();
            LoadCards();
        }

        private void UpdateCard_Click(object sender, RoutedEventArgs e)
        {
            if (CardsGrid.SelectedItem is not CreditCardAccount card) return;

            string bank = BankNameBox.Text.Trim();
            string label = CardLabelBox.Text.Trim();

            if (string.IsNullOrEmpty(bank) || string.IsNullOrEmpty(label))
            {
                MessageBox.Show("Banka adı ve kart etiketi boş olamaz.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(StatementDayBox.Text) || string.IsNullOrWhiteSpace(PaymentDueDayBox.Text) ||
                !int.TryParse(StatementDayBox.Text, out int sDay) || sDay < 1 || sDay > 31 ||
                !int.TryParse(PaymentDueDayBox.Text, out int pDay) || pDay < 1 || pDay > 31)
            {
                MessageBox.Show("Kesim günü ve Son ödeme günü zorunludur ve 1 ile 31 arasında geçerli bir sayı olmalıdır.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!FinTrack.WPF.Helpers.UIHelper.TryParseAmount(LimitBox.Text, out decimal limit))
            {
                limit = 0;
            }

            var entity = _db.CreditCardAccounts.Find(card.Id);
            if (entity == null) return;

            int? parentId = (int?)ParentCardCombo.SelectedValue;
            if (parentId == -1) parentId = null;

            entity.BankName = bank;
            entity.CardLabel = label;
            entity.Limit = limit;
            entity.StatementDay = sDay;
            entity.PaymentDueDay = pDay;
            entity.ParentCardId = parentId;
            _db.SaveChanges();

            LoadCards();
            MessageBox.Show("Kart bilgileri güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            if (CardsGrid.SelectedItem is not CreditCardAccount card) return;

            var entity = _db.CreditCardAccounts.Find(card.Id);
            if (entity == null) return;

            entity.IsActive = !entity.IsActive;
            _db.SaveChanges();
            LoadCards();
        }

        private void DeleteCard_Click(object sender, RoutedEventArgs e)
        {
            if (CardsGrid.SelectedItem is not CreditCardAccount card) return;

            var result = MessageBox.Show(
                $"{card.BankName} – {card.CardLabel} kartını silmek istiyor musunuz?\n" +
                "Bu kartla ilişkili işlemler korunacak (kart bilgisi temizlenecek).",
                "Kart Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var entity = _db.CreditCardAccounts.Find(card.Id);
            if (entity == null) return;

            _db.CreditCardAccounts.Remove(entity);
            _db.SaveChanges();
            LoadCards();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
