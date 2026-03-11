using System;
using System.Globalization;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF.Helpers
{
    public static class UIHelper
    {
        private static bool _isFormattingAmount = false;

        public static void FormatAmountTextBox(TextBox? textBox)
        {
            if (textBox == null || _isFormattingAmount) return;

            string text = textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            // Save the selection/caret index
            int caretIndex = textBox.CaretIndex;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Remove non-digit characters except for comma and minus sign
            // Ignore dots as they are used for thousands separators
            string cleanText = "";
            bool hasDecimal = false;
            foreach (char c in text)
            {
                if (char.IsDigit(c) || c == '-')
                {
                    cleanText += c;
                }
                else if (c == ',')
                {
                    if (!hasDecimal)
                    {
                        cleanText += ',';
                        hasDecimal = true;
                    }
                }
            }

            // Separate integer and decimal parts
            var parts = cleanText.Split(',');
            string integerPart = parts[0];
            string decimalPart = parts.Length > 1 ? "," + parts[1] : "";

            // Avoid errors with negative sign only or empty integer
            if (integerPart == "" || integerPart == "-")
            {
                if (text != cleanText)
                {
                    _isFormattingAmount = true;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = text.StartsWith("-") ? 1 : 0;
                    _isFormattingAmount = false;
                }
                return;
            }

            if (long.TryParse(integerPart, out long parsedInteger))
            {
                // Format integer with dots as thousands separator (e.g. 1.234)
                string formattedText = string.Format(CultureInfo.InvariantCulture, "{0:N0}", parsedInteger).Replace(",", ".");
                
                string resultText = formattedText + decimalPart;
                
                if (textBox.Text != resultText)
                {
                    // Calculate caret offset based on added/removed thousands separators before the caret
                    string textBeforeCaretOld = text.Substring(0, Math.Min(caretIndex, text.Length));
                    int oldSeparators = textBeforeCaretOld.Length - textBeforeCaretOld.Replace(".", "").Replace(",", "").Length;

                    _isFormattingAmount = true;
                    textBox.Text = resultText;
                    
                    // Restore caret based on original digit position
                    string textBeforeCaretNew = resultText.Substring(0, Math.Min(resultText.Length, caretIndex));
                    int newSeparators = textBeforeCaretNew.Length - textBeforeCaretNew.Replace(".", "").Replace(",", "").Length;

                    int newCaret = caretIndex + (newSeparators - oldSeparators);
                    textBox.SelectionStart = Math.Max(0, Math.Min(newCaret, resultText.Length));
                    textBox.SelectionLength = selectionLength;
                    _isFormattingAmount = false;
                }
            }
        }

        public static bool TryParseAmount(string text, out decimal amount)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                amount = 0;
                return false;
            }

            // Remove thousands separators
            string cleanText = text.Replace(".", "");
            // Use TR culture format: replace comma with dot to parse as InvariantCulture
            cleanText = cleanText.Replace(",", ".");
            return decimal.TryParse(cleanText, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }

        private static bool _isFormattingIban = false;

        public static string FormatIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return string.Empty;
            
            // Allow numbers and letters, avoid Turkish specific issue by picking only standard ascii range if needed
            string cleanText = new string(iban.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
            
            string formattedText = "";
            for (int i = 0; i < cleanText.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formattedText += " ";
                formattedText += cleanText[i];
            }
            return formattedText;
        }

        public static void FormatIbanTextBox(TextBox? textBox)
        {
            if (textBox == null || _isFormattingIban) return;

            string text = textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            int caretIndex = textBox.CaretIndex;
            string textBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length));
            int charsBeforeCaret = textBeforeCaret.Count(char.IsLetterOrDigit);

            string formattedText = FormatIban(text);

            if (textBox.Text != formattedText)
            {
                _isFormattingIban = true;
                textBox.Text = formattedText;
                
                int newCaret = 0;
                int alphanumericsEncountered = 0;
                while (newCaret < formattedText.Length && alphanumericsEncountered < charsBeforeCaret)
                {
                    if (char.IsLetterOrDigit(formattedText[newCaret]))
                    {
                        alphanumericsEncountered++;
                    }
                    newCaret++;
                }

                // If caret lands exactly on a space, skip it so user types after the space.
                if (newCaret < formattedText.Length && formattedText[newCaret] == ' ')
                {
                    newCaret++;
                }

                textBox.SelectionStart = newCaret;
                textBox.SelectionLength = 0;
                _isFormattingIban = false;
            }
        }

        public static decimal CalculateCardDebt(FinTrack.Data.AppDbContext db, int cardId)
        {
            return CalculateCardDebt(db, new[] { cardId });
        }

        public static decimal CalculateCardDebt(FinTrack.Data.AppDbContext db, IEnumerable<int> cardIds)
        {
            var transactions = db.Transactions
                .Include(t => t.Category)
                .Where(t => t.CreditCardAccountId != null && cardIds.Contains(t.CreditCardAccountId.Value))
                .ToList();

            decimal debt = 0;
            foreach (var t in transactions)
            {
                if (t.Category?.Type == FinTrack.Core.Models.TransactionType.Expense) debt += t.Amount;
                else if (t.Category?.Type == FinTrack.Core.Models.TransactionType.Income || t.Category?.Type == FinTrack.Core.Models.TransactionType.Transfer) debt -= t.Amount;
            }

            // Add investment debt
            var invTxs = db.InvestmentTransactions
                .Where(t => t.LinkedCreditCardAccountId != null && cardIds.Contains(t.LinkedCreditCardAccountId.Value))
                .ToList();

            foreach (var it in invTxs)
            {
                if (it.Type == FinTrack.Core.Models.InvestmentTransactionType.Buy) debt += it.TotalCost;
                else if (it.Type == FinTrack.Core.Models.InvestmentTransactionType.Sell) debt -= it.TotalCost;
            }

            return debt;
        }

        public static bool CheckCardLimit(FinTrack.Data.AppDbContext db, int cardId, decimal addedDebt, System.Windows.Window owner)
        {
            var card = db.CreditCardAccounts.Include(c => c.ParentCard).FirstOrDefault(c => c.Id == cardId);
            if (card == null) return true;

            // 1. Check individual card limit (sub-limit)
            if (card.Limit > 0)
            {
                decimal currentIndividualDebt = CalculateCardDebt(db, cardId);
                if (currentIndividualDebt + addedDebt > card.Limit)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Kendi kart limitiniz aşılıyor!\n\n" +
                        $"Kart: {card.CardLabel}\n" +
                        $"Mevcut Borç: ₺{currentIndividualDebt:N2}\n" +
                        $"Yeni İşlem: ₺{addedDebt:N2}\n" +
                        $"Kart Limiti: ₺{card.Limit:N2}\n\n" +
                        $"Yine de devam edilsin mi?",
                        "Kart Limiti Aşımı", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    
                    if (result != System.Windows.MessageBoxResult.Yes) return false;
                }
            }

            // 2. If it's a linked card, check the core (Parent) card's consolidated limit
            if (card.ParentCardId.HasValue)
            {
                var parentId = card.ParentCardId.Value;
                var parent = db.CreditCardAccounts.Find(parentId);
                
                if (parent != null && parent.Limit > 0)
                {
                    // Calculate debt for the whole family (Parent + all children)
                    var familyIds = db.CreditCardAccounts
                        .Where(c => c.Id == parentId || c.ParentCardId == parentId)
                        .Select(c => c.Id)
                        .ToList();

                    decimal familyDebt = CalculateCardDebt(db, familyIds);
                    decimal newFamilyTotal = familyDebt + addedDebt;

                    if (newFamilyTotal > parent.Limit)
                    {
                        var result = System.Windows.MessageBox.Show(
                            $"Ana kart (Asıl Kart) limiti aşılıyor!\n\n" +
                            $"Ana Kart: {parent.BankName} - {parent.CardLabel}\n" +
                            $"Tüm Kartlar Toplam Borç: ₺{familyDebt:N2}\n" +
                            $"Yeni İşlem: ₺{addedDebt:N2}\n" +
                            $"Yeni Toplam: ₺{newFamilyTotal:N2}\n" +
                            $"Ana Kart Limiti: ₺{parent.Limit:N2}\n\n" +
                            $"Limit aşımıyla kaydetmek istiyor musunuz?",
                            "Ana Kart Limit Aşımı", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

                        return result == System.Windows.MessageBoxResult.Yes;
                    }
                }
            }

            return true;
        }
    }
}
