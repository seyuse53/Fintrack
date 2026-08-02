using Avalonia.Controls;
using Avalonia.Interactivity;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class CategoryDetailWindow : Window
{
    public CategoryDetailWindow()
    {
        InitializeComponent();
    }

    public CategoryDetailWindow(DashboardBudgetItem item) : this()
    {
        DataContext = item;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
