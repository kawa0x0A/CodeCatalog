using Avalonia.Controls;
using Avalonia.Interactivity;
using CodeCatalog.Services;

namespace CodeCatalog.Views;

public partial class RunDialog : Window
{
    private readonly IList<string> _projectTypes;
    private readonly string _projectPath;

    public string? ResultConfiguration { get; private set; }
    public string? ResultArgs { get; private set; }
    public string? ResultLaunchMode { get; private set; }

    // デザイナー用
    public RunDialog() : this(Array.Empty<string>(), string.Empty) { }

    public RunDialog(IList<string> projectTypes, string projectPath)
    {
        _projectTypes = projectTypes;
        _projectPath = projectPath;
        InitializeComponent();
        UpdatePreview();
    }

    private string SelectedConfig => ReleaseRadio.IsChecked == true ? "Release" : "Debug";

    private void OnConfigChanged(object? sender, RoutedEventArgs e) => UpdatePreview();

    private void OnArgsChanged(object? sender, TextChangedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        var info = ProjectRunner.GetRunCommand(
            _projectTypes, _projectPath,
            SelectedConfig,
            ArgsBox?.Text ?? string.Empty);

        CommandPreview.Text = info is { } i
            ? $"> {i.command} {i.args}"
            : "(対応する実行コマンドが見つかりません)";

        if (RunButton is not null)
            RunButton.IsEnabled = info is not null;
    }

    private void OnRunClick(object? sender, RoutedEventArgs e)
    {
        ResultConfiguration = SelectedConfig;
        ResultArgs = ArgsBox.Text ?? string.Empty;
        ResultLaunchMode = BackgroundRadio.IsChecked == true ? "Background" : "Terminal";
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
