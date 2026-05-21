using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF.Views
{
    public partial class CategoryEditWindow : Window
    {
        private readonly int _categoryId;
        private Category? _category;

        public CategoryEditWindow(int categoryId)
        {
            InitializeComponent();
            _categoryId = categoryId;
            LoadCategoryData();
        }

        private AppDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            string dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
            string? password = FinTrack.Core.Services.SettingsManager.ActiveDataKey;

            if (string.IsNullOrEmpty(password))
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            else
                optionsBuilder.UseSqlite($"Data Source={dbPath};Password={password}");

            return new AppDbContext(optionsBuilder.Options);
        }

        private void LoadCategoryData()
        {
            try
            {
                using var context = GetDbContext();
                _category = context.Categories.Find(_categoryId);

                if (_category == null)
                {
                    MessageBox.Show("Kategori bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                CategoryNameTextBox.Text = _category.Name;

                // Load potential parents (same Type, top-level, not itself)
                var parents = context.Categories
                    .Where(c => c.IsVisible && c.ParentCategoryId == null && c.Type == _category.Type && c.Id != _category.Id)
                    .OrderBy(c => c.Name)
                    .ToList();

                parents.Insert(0, new Category { Id = 0, Name = "-- Yok (Ana Kategori) --" });
                ParentCategoryComboBox.ItemsSource = parents;

                if (_category.ParentCategoryId != null)
                {
                    ParentCategoryComboBox.SelectedItem = parents.FirstOrDefault(p => p.Id == _category.ParentCategoryId);
                }
                else
                {
                    ParentCategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = CategoryNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Lütfen bir kategori adı girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var context = GetDbContext();
                var dbCat = context.Categories.Find(_categoryId);
                if (dbCat != null)
                {
                    dbCat.Name = newName;

                    if (ParentCategoryComboBox.SelectedItem is Category selectedParent)
                    {
                        dbCat.ParentCategoryId = selectedParent.Id > 0 ? selectedParent.Id : null;
                    }

                    context.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
