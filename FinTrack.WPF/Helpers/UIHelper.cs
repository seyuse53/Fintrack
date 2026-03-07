using System;
using System.Globalization;
using System.Windows.Controls;

namespace FinTrack.WPF.Helpers
{
    public static class UIHelper
    {
        private static bool _isFormattingAmount = false;

        public static void FormatAmountTextBox(TextBox textBox)
        {
            if (textBox == null || _isFormattingAmount) return;

            string text = textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            // Save the selection/caret index
            int caretIndex = textBox.CaretIndex;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Remove non-digit characters except for comma, dot and minus sign
            string cleanText = "";
            bool hasDecimal = false;
            foreach (char c in text)
            {
                if (char.IsDigit(c) || c == '-')
                {
                    cleanText += c;
                }
                else if (c == ',' || c == '.')
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
    }
}
