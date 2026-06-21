using System.Text.Json;
using CodeCatalog.Models;

namespace CodeCatalog.Services;

public class CatalogData
{
    public List<ProjectEntry> Projects { get; set; } = new();
}

public static class CatalogStore
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CodeCatalog");

    private static readonly string StorePath = Path.Combine(AppDataDir, "catalog.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static CatalogData Load()
    {
        try
        {
            if (!File.Exists(StorePath)) return new CatalogData();
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<CatalogData>(json, JsonOptions) ?? new CatalogData();
        }
        catch
        {
            return new CatalogData();
        }
    }

    public static void Save(CatalogData data)
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(StorePath, JsonSerializer.Serialize(data, JsonOptions));
    }
}
