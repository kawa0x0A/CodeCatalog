using CommunityToolkit.Mvvm.ComponentModel;
using CodeCatalog.Models;

namespace CodeCatalog.ViewModels;

public partial class ProjectEntryViewModel : ObservableObject
{
    public ProjectEntry Model { get; }

    public ProjectEntryViewModel(ProjectEntry model)
    {
        Model = model;
    }

    public string Name => Model.Name;
    public string Path => Model.Path;
    public bool IsGitRepo => Model.IsGitRepo;

    public string ProjectTypesText =>
        Model.ProjectTypes.Count > 0 ? string.Join(", ", Model.ProjectTypes) : "(種別不明)";

    public string LastModifiedText =>
        Model.LastModifiedUtc == DateTime.MinValue
            ? "-"
            : Model.LastModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public IReadOnlyList<string> Tags => Model.Tags;

    public bool IsBuilt
    {
        get => Model.IsBuilt;
        set { Model.IsBuilt = value; OnPropertyChanged(); }
    }
}
