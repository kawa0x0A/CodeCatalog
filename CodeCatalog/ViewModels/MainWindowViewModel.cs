using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodeCatalog.Models;
using CodeCatalog.Services;

namespace CodeCatalog.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly CatalogData _data;
    private DispatcherTimer? _autoSaveTimer;

    public ObservableCollection<ProjectEntryViewModel> AllProjects { get; } = new();
    public ObservableCollection<ProjectEntryViewModel> FilteredProjects { get; } = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = "準備完了";
    [ObservableProperty] private ProjectEntryViewModel? _selectedProject;
    [ObservableProperty] private bool _isDetailView;
    [ObservableProperty] private string _configuration = "Release";
    [ObservableProperty] private string _buildOutput = string.Empty;
    [ObservableProperty] private bool _isBuilding;
    [ObservableProperty] private string _buildCommandLabel = string.Empty;
    [ObservableProperty] private string _runCommandLabel = string.Empty;
    [ObservableProperty] private string _toolWarning = string.Empty;

    private ProjectEntryViewModel? _previousSelectedProject;

    // リスト画面からのボタン（CommandParameter でプロジェクトを受け取る）
    public IAsyncRelayCommand<ProjectEntryViewModel> BuildProjectCommand { get; }
    public IRelayCommand<ProjectEntryViewModel> RunProjectCommand { get; }  // Release 固定
    public IRelayCommand<ProjectEntryViewModel> ShowDetailCommand { get; }

    // 詳細画面専用コマンド（SelectedProject に対して動作）
    public IAsyncRelayCommand DetailedRunCommand { get; }
    public IAsyncRelayCommand GitPullCommand { get; }
    public IRelayCommand OpenInFileManagerCommand { get; }
    public IRelayCommand OpenInVsCodeCommand { get; }
    public IRelayCommand OpenTerminalCommand { get; }
    public IRelayCommand RemoveProjectCommand { get; }
    public IRelayCommand BackCommand { get; }
    public IAsyncRelayCommand CloneFromGitHubCommand { get; }

    public Func<Task<(string url, string targetDir)?>>? RequestCloneDialog { get; set; }
    public Func<IList<string>, string, Task<(string config, string args, string launchMode)?>>? RequestRunDialog { get; set; }

    public MainWindowViewModel()
    {
        _data = CatalogStore.Load();

        foreach (var project in _data.Projects)
            AllProjects.Add(new ProjectEntryViewModel(project));

        ApplyFilter();

        BuildProjectCommand = new AsyncRelayCommand<ProjectEntryViewModel>(
            p => RunBuildAsync(p!),
            p => p is not null && !IsBuilding);

        RunProjectCommand = new RelayCommand<ProjectEntryViewModel>(
            p => ExecuteRun(p!, "Release", string.Empty),
            p => p?.Model.IsBuilt == true && !IsBuilding);

        ShowDetailCommand = new RelayCommand<ProjectEntryViewModel>(p =>
        {
            SelectedProject = p;
            IsDetailView = true;
        });

        BackCommand = new RelayCommand(() => IsDetailView = false);

        DetailedRunCommand = new AsyncRelayCommand(DetailedRunAsync,
            () => SelectedProject?.Model.IsBuilt == true && !IsBuilding);

        GitPullCommand = new AsyncRelayCommand(GitPullAsync,
            () => SelectedProject?.IsGitRepo == true && !IsBuilding);

        OpenInFileManagerCommand = new RelayCommand(
            () => { if (SelectedProject is not null) FolderOpener.OpenInFileManager(SelectedProject.Path); },
            () => SelectedProject is not null);

        OpenInVsCodeCommand = new RelayCommand(
            () => { if (SelectedProject is not null) FolderOpener.OpenInVsCode(SelectedProject.Path); },
            () => SelectedProject is not null);

        OpenTerminalCommand = new RelayCommand(
            () => { if (SelectedProject is not null) FolderOpener.OpenTerminal(SelectedProject.Path); },
            () => SelectedProject is not null);

        RemoveProjectCommand = new RelayCommand(RemoveProject,
            () => SelectedProject is not null);

        CloneFromGitHubCommand = new AsyncRelayCommand(CloneFromGitHubAsync);

        _ = CheckBuiltStateOnStartupAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public bool IsDebugConfig   => Configuration == "Debug";
    public bool IsReleaseConfig => Configuration == "Release";

    partial void OnConfigurationChanged(string value)
    {
        OnPropertyChanged(nameof(IsDebugConfig));
        OnPropertyChanged(nameof(IsReleaseConfig));
        UpdateBuildCommandLabel(SelectedProject);
        UpdateRunCommandLabel(SelectedProject, value);
    }

    partial void OnIsBuildingChanged(bool value)
    {
        BuildProjectCommand.NotifyCanExecuteChanged();
        RunProjectCommand.NotifyCanExecuteChanged();
        ((AsyncRelayCommand)DetailedRunCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)GitPullCommand).NotifyCanExecuteChanged();
    }

    partial void OnSelectedProjectChanged(ProjectEntryViewModel? value)
    {
        if (_previousSelectedProject is not null)
            _previousSelectedProject.PropertyChanged -= OnSelectedProjectPropertyChanged;

        _previousSelectedProject = value;

        if (value is not null)
            value.PropertyChanged += OnSelectedProjectPropertyChanged;

        BuildOutput = string.Empty;
        ToolWarning = value is null ? string.Empty : (ToolChecker.GetWarning(value.Model.ProjectTypes) ?? string.Empty);
        UpdateBuildCommandLabel(value);
        UpdateRunCommandLabel(value, Configuration);

        ((AsyncRelayCommand)DetailedRunCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)GitPullCommand).NotifyCanExecuteChanged();
        ((RelayCommand)OpenInFileManagerCommand).NotifyCanExecuteChanged();
        ((RelayCommand)OpenInVsCodeCommand).NotifyCanExecuteChanged();
        ((RelayCommand)OpenTerminalCommand).NotifyCanExecuteChanged();
        ((RelayCommand)RemoveProjectCommand).NotifyCanExecuteChanged();
    }

    private void OnSelectedProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _autoSaveTimer.Tick += (_, _) => { _autoSaveTimer?.Stop(); SaveCatalog(); };
        _autoSaveTimer.Start();
    }

    private void UpdateBuildCommandLabel(ProjectEntryViewModel? vm)
    {
        if (vm is null) { BuildCommandLabel = string.Empty; return; }
        var info = BuildRunner.GetBuildCommand(vm.Model.ProjectTypes, vm.Path, Configuration);
        BuildCommandLabel = info is { } i ? $"{i.command} {i.args}" : "(対応するビルドコマンドなし)";
    }

    private void UpdateRunCommandLabel(ProjectEntryViewModel? vm, string config)
    {
        if (vm is null) { RunCommandLabel = string.Empty; return; }
        // 実行はデフォルト Release なので Release で表示
        var info = ProjectRunner.GetRunCommand(vm.Model.ProjectTypes, vm.Path, "Release");
        RunCommandLabel = info is { } i ? $"{i.command} {i.args}" : "(対応する実行コマンドなし)";
    }

    public void AddProjectFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || AllProjects.Any(p => p.Path == path))
            return;

        var entries = ProjectScanner.Scan(path, maxDepth: 0).ToList();
        ProjectEntry entry;
        if (entries.Count > 0)
        {
            entry = entries[0];
        }
        else
        {
            var info = new DirectoryInfo(path);
            entry = new ProjectEntry
            {
                Path = path,
                Name = info.Name,
                IsGitRepo = Directory.Exists(Path.Combine(path, ".git")),
                LastModifiedUtc = Directory.GetLastWriteTimeUtc(path)
            };
        }

        entry.Tags = BuildAutoTags(entry);
        AllProjects.Add(new ProjectEntryViewModel(entry));
        ApplyFilter();
        SaveCatalog();
        StatusText = $"追加しました: {entry.Name}";
    }

    private void RemoveProject()
    {
        if (SelectedProject is null) return;

        var target = SelectedProject;
        SelectedProject = null;
        IsDetailView = false;
        AllProjects.Remove(target);
        FilteredProjects.Remove(target);
        SaveCatalog();
        StatusText = $"「{target.Name}」を削除しました";
    }

    private async Task RunBuildAsync(ProjectEntryViewModel project)
    {
        var info = BuildRunner.GetBuildCommand(project.Model.ProjectTypes, project.Path, Configuration);
        if (info is null)
        {
            BuildOutput = "このプロジェクト種別のビルドコマンドが見つかりません。";
            return;
        }

        var (command, args) = info.Value;
        BuildOutput = $"> {command} {args}\n\n";
        IsBuilding = true;
        StatusText = $"ビルド中: {project.Name}";

        try
        {
            var exitCode = await BuildRunner.RunAsync(
                command, args, project.Path,
                line => Dispatcher.UIThread.Post(() => BuildOutput += line + "\n"));

            var built = exitCode == 0;
            project.IsBuilt = built;
            SaveCatalog();
            RunProjectCommand.NotifyCanExecuteChanged();
            ((AsyncRelayCommand)DetailedRunCommand).NotifyCanExecuteChanged();

            var result = built ? "✓ ビルド成功" : $"✗ ビルド失敗 (exit code: {exitCode})";
            BuildOutput += $"\n{result}";
            StatusText = $"{project.Name}: {result}";
        }
        catch (Exception ex)
        {
            BuildOutput += $"\nエラー: {ex.Message}";
            StatusText = $"ビルドエラー: {ex.Message}";
        }
        finally
        {
            IsBuilding = false;
        }
    }

    private void ExecuteRun(ProjectEntryViewModel project, string configuration, string launchArgs, string launchMode = "Terminal")
    {
        var info = ProjectRunner.GetRunCommand(project.Model.ProjectTypes, project.Path, configuration, launchArgs);
        if (info is null)
        {
            StatusText = "このプロジェクト種別の実行コマンドが見つかりません。";
            return;
        }
        if (launchMode == "Background")
            FolderOpener.RunInBackground(project.Path, info.Value.command, info.Value.args);
        else
            FolderOpener.RunInTerminal(project.Path, info.Value.command, info.Value.args);
        StatusText = $"実行: {info.Value.command} {info.Value.args}";
    }

    private async Task DetailedRunAsync()
    {
        if (SelectedProject is null || RequestRunDialog is null) return;

        var result = await RequestRunDialog(SelectedProject.Model.ProjectTypes, SelectedProject.Path);
        if (result is null) return;

        var (config, args, launchMode) = result.Value;
        Configuration = config;
        ExecuteRun(SelectedProject, config, args, launchMode);
    }

    private async Task GitPullAsync()
    {
        if (SelectedProject is null) return;

        BuildOutput = "> git pull\n\n";
        IsBuilding = true;
        StatusText = $"更新中: {SelectedProject.Name}";

        try
        {
            var exitCode = await BuildRunner.RunAsync(
                "git", "pull", SelectedProject.Path,
                line => Dispatcher.UIThread.Post(() => BuildOutput += line + "\n"));

            var result = exitCode == 0 ? "✓ 更新完了" : $"✗ 更新失敗 (exit code: {exitCode})";
            BuildOutput += $"\n{result}";
            StatusText = $"{SelectedProject.Name}: {result}";

            if (exitCode == 0)
                SelectedProject.Model.LastModifiedUtc = Directory.GetLastWriteTimeUtc(SelectedProject.Path);
        }
        catch (Exception ex)
        {
            BuildOutput += $"\nエラー: {ex.Message}";
            StatusText = $"更新エラー: {ex.Message}";
        }
        finally
        {
            IsBuilding = false;
        }
    }

    private async Task CloneFromGitHubAsync()
    {
        var requestDialog = RequestCloneDialog;
        if (requestDialog is null) return;

        var result = await requestDialog();
        if (result is null) return;

        var (url, targetDir) = result.Value;
        var repoName = Path.GetFileNameWithoutExtension(url.TrimEnd('/').Split('/').Last());
        if (string.IsNullOrWhiteSpace(repoName)) repoName = "repo";
        var clonePath = Path.Combine(targetDir, repoName);

        StatusText = $"クローン中: {repoName} ...";

        try
        {
            var success = await Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo("git", $"clone \"{url}\"")
                    {
                        WorkingDirectory = targetDir,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = System.Diagnostics.Process.Start(psi);
                    proc?.WaitForExit(60_000);
                    return proc?.ExitCode == 0;
                }
                catch { return false; }
            });

            if (!success)
            {
                StatusText = "クローン失敗: git コマンドが見つからないか、URLが無効です";
                return;
            }

            if (Directory.Exists(clonePath))
            {
                AddProjectFolder(clonePath);
                StatusText = $"クローン完了: {clonePath}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"クローンエラー: {ex.Message}";
        }
    }

    private async Task CheckBuiltStateOnStartupAsync()
    {
        var changed = await Task.Run(() =>
        {
            bool any = false;
            foreach (var vm in AllProjects)
            {
                if (!vm.Model.IsBuilt) continue;
                var ok = BuildChecker.QuickCheck(vm.Model.ProjectTypes, vm.Model.Path, "Debug")
                      || BuildChecker.QuickCheck(vm.Model.ProjectTypes, vm.Model.Path, "Release");
                if (!ok) { vm.Model.IsBuilt = false; any = true; }
            }
            return any;
        });

        if (changed)
        {
            SaveCatalog();
            RunProjectCommand.NotifyCanExecuteChanged();
        }
    }

    private static List<string> BuildAutoTags(ProjectEntry entry)
    {
        var tags = new List<string>(entry.ProjectTypes);
        if (entry.IsGitRepo) tags.Add("Git");
        return tags;
    }

    private void ApplyFilter()
    {
        FilteredProjects.Clear();

        IEnumerable<ProjectEntryViewModel> query = AllProjects;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lower = SearchText.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLowerInvariant().Contains(lower) ||
                p.ProjectTypesText.ToLowerInvariant().Contains(lower) ||
                p.Tags.Any(t => t.ToLowerInvariant().Contains(lower)));
        }

        foreach (var p in query.OrderByDescending(p => p.Model.LastModifiedUtc))
            FilteredProjects.Add(p);
    }

    public void SaveCatalog()
    {
        _data.Projects = AllProjects.Select(vm => vm.Model).ToList();
        CatalogStore.Save(_data);
    }
}
