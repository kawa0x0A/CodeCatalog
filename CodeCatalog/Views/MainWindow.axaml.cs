using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CodeCatalog.ViewModels;

namespace CodeCatalog.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => (DataContext as MainWindowViewModel)?.SaveCatalog();

        if (DataContext is MainWindowViewModel vm)
            Wire(vm);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm2)
                Wire(vm2);
        };
    }

    private void Wire(MainWindowViewModel vm)
    {
        vm.RequestCloneDialog = async () =>
        {
            var dialog = new CloneDialog();
            await dialog.ShowDialog(this);
            if (dialog.ResultUrl is { } url && dialog.ResultTargetDir is { } dir)
                return (url, dir);
            return null;
        };

        vm.RequestRunDialog = async (projectTypes, projectPath) =>
        {
            var dialog = new RunDialog(projectTypes, projectPath);
            await dialog.ShowDialog(this);
            if (dialog.ResultConfiguration is { } config)
                return (config, dialog.ResultArgs ?? string.Empty, dialog.ResultLaunchMode ?? "Terminal");
            return null;
        };

    }

    private void OnConfigDebugClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm) vm.Configuration = "Debug";
    }

    private void OnConfigReleaseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm) vm.Configuration = "Release";
    }

    private async void OnAddProjectFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "プログラムのフォルダを選択",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder?.TryGetLocalPath() is { } path && DataContext is MainWindowViewModel vm)
            vm.AddProjectFolder(path);
    }
}
