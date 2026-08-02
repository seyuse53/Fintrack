using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Core.Services;
using System.Collections.Generic;

namespace FinTrack.Avalonia.Views
{
    public partial class ImportPreviewWindow : Window
    {
        public ImportPreviewWindow()
        {
            InitializeComponent();
        }

        public ImportPreviewWindow(int? cardId, int? accountId, List<GeminiParsedTransaction> parsedTransactions) : this()
        {
            var viewModel = new ImportPreviewViewModel(cardId, accountId, parsedTransactions);
            viewModel.CloseAction = () => Close();
            DataContext = viewModel;
        }
    }
}
