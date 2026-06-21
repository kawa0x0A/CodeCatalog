namespace CodeCatalog.Models;

public class ProjectEntry
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> ProjectTypes { get; set; } = new();
    public bool IsGitRepo { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();
    public bool IsBuilt { get; set; }
}
