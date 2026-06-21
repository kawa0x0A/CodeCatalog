using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace CodeCatalog.Views;

public partial class CloneDialog : Window
{
    public string? ResultUrl { get; private set; }
    public string? ResultTargetDir { get; private set; }

    public CloneDialog()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "クローン先フォルダを選択"
        });

        var folder = folders.FirstOrDefault();
        if (folder?.TryGetLocalPath() is { } path)
            TargetBox.Text = path;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UrlBox.Text) || string.IsNullOrWhiteSpace(TargetBox.Text))
            return;

        ResultUrl = UrlBox.Text.Trim();
        ResultTargetDir = TargetBox.Text.Trim();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
