using Avalonia.Controls;

namespace FinTrack.Avalonia.Views;

public partial class AddProfileView : UserControl
{
    public AddProfileView()
    {
        InitializeComponent();
    }

    private async void LinkExistingProfile_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Veritabanı Dosyası Seç (.db)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new global::Avalonia.Platform.Storage.FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } } }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            string path = file.Path.LocalPath;
            
            if (DataContext is FinTrack.Avalonia.ViewModels.AddProfileViewModel vm)
            {
                vm.LinkExistingProfile(path);
            }
        }
    }
}
