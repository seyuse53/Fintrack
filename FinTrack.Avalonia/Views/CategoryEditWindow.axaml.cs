using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Avalonia.Views;

public partial class CategoryEditWindow : Window
{
    private readonly int _categoryId;
    private readonly AppDbContext _context = null!;
    private Category? _category;

    public List<Category> AvailableParents { get; set; } = new();
    public Category? SelectedParent { get; set; }

    public CategoryEditWindow()
    {
        InitializeComponent();
    }

    public CategoryEditWindow(AppDbContext context, int categoryId) : this()
    {
        _context = context;
        _categoryId = categoryId;
        DataContext = this;
        LoadCategoryData();
    }

    private void LoadCategoryData()
    {
        try
        {
            _category = _context.Categories.Find(_categoryId);

            if (_category == null)
            {
                Close(false);
                return;
            }

            CategoryNameTextBox.Text = _category.Name;

            // Load potential parents (same Type, top-level, not itself)
            var parents = _context.Categories
                .Where(c => c.IsVisible && c.ParentCategoryId == null && c.Type == _category.Type && c.Id != _category.Id)
                .OrderBy(c => c.Name)
                .ToList();

            parents.Insert(0, new Category { Id = 0, Name = "-- Yok (Ana Kategori) --" });
            AvailableParents = parents;
            ParentCategoryComboBox.ItemsSource = AvailableParents;

            if (_category.ParentCategoryId != null)
            {
                ParentCategoryComboBox.SelectedItem = parents.FirstOrDefault(p => p.Id == _category.ParentCategoryId);
            }
            else
            {
                ParentCategoryComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception)
        {
            Close(false);
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        string newName = CategoryNameTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(newName))
        {
            var parentWindow = this;
            var warnDialog = new ConfirmDialog("Uyarı", "Lütfen bir kategori adı girin.", "Tamam", "");
            await warnDialog.ShowDialog<bool?>(parentWindow);
            return;
        }

        try
        {
            var dbCat = _context.Categories.Find(_categoryId);
            if (dbCat != null)
            {
                dbCat.Name = newName;

                if (ParentCategoryComboBox.SelectedItem is Category selectedParent)
                {
                    dbCat.ParentCategoryId = selectedParent.Id > 0 ? selectedParent.Id : null;
                }

                _context.SaveChanges();
                Close(true);
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ConfirmDialog("Hata", $"Kaydedilirken hata oluştu: {ex.Message}", "Tamam", "");
            await errorDialog.ShowDialog<bool?>(this);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
