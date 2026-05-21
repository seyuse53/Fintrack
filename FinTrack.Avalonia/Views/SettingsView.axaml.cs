using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            // Wire up events for View-side operations
            vm.ShowConfirmDialog += ShowConfirmDialogAsync;
            vm.RequestFolderSelection += RequestFolderSelectionAsync;
            vm.LoadCategories();
        }
    }

    private async System.Threading.Tasks.Task<bool?> ShowConfirmDialogAsync(string title, string message, string confirmText, string cancelText)
    {
        var parentWindow = (this.VisualRoot as Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (parentWindow != null)
        {
            var dialog = new ConfirmDialog(title, message, confirmText, cancelText);
            return await dialog.ShowDialog<bool?>(parentWindow);
        }
        return null;
    }

    private async System.Threading.Tasks.Task<string?> RequestFolderSelectionAsync(string title)
    {
        var parentWindow = (this.VisualRoot as Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (parentWindow != null)
        {
            var result = await parentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                return result[0].Path.LocalPath;
            }
        }
        return null;
    }

    // ==================== CATEGORY EVENT HANDLERS (code-behind for multi-select) ====================

    private async void HideCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            var selectedItems = VisibleCategoriesListBox.SelectedItems;
            await vm.HideCategoriesCommand.ExecuteAsync(selectedItems);
        }
    }

    private async void ShowCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            var selectedItems = HiddenCategoriesListBox.SelectedItems;
            await vm.ShowCategoriesCommand.ExecuteAsync(selectedItems);
        }
    }

    private async void CategoryEdit_Click(object? sender, RoutedEventArgs e)
    {
        // Get the selected category from the ListBox that triggered the context menu
        Category? category = null;

        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
        {
            if (contextMenu.PlacementTarget is ListBox listBox && listBox.SelectedItem is Category selectedCat)
            {
                category = selectedCat;
            }
        }

        if (category == null) return;

        var db = App.Services?.GetService<AppDbContext>();
        if (db == null) return;

        var parentWindow = (this.VisualRoot as Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (parentWindow != null)
        {
            var editWindow = new CategoryEditWindow(db, category.Id);
            var result = await editWindow.ShowDialog<bool?>(parentWindow);
            if (result == true)
            {
                if (DataContext is SettingsViewModel vm)
                {
                    vm.LoadCategories();
                }
            }
        }
    }
}
