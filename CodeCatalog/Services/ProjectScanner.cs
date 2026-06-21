using CodeCatalog.Models;

namespace CodeCatalog.Services;

/// <summary>
/// 指定フォルダ配下を再帰的に走査し、「プロジェクトらしきフォルダ」を検出する。
/// .sln / .csproj / .git / package.json などの目印(マーカー)で判定する。
/// </summary>
public static class ProjectScanner
{
    // この名前のフォルダは中に入らない(ビルド成果物やパッケージキャッシュなど、ノイズの元)
    private static readonly string[] SkipDirNames =
    {
        "bin", "obj", "node_modules", ".git", ".vs", ".idea",
        "packages", "dist", "build", "target", "__pycache__", ".venv", "venv"
    };

    // (ファイル名 or *拡張子, 表示ラベル) の対応表
    private static readonly (string Marker, string Label)[] Markers =
    {
        ("*.sln", "Visual Studio Solution"),
        ("*.csproj", ".NET"),
        ("package.json", "Node.js"),
        ("pyproject.toml", "Python"),
        ("requirements.txt", "Python"),
        ("pom.xml", "Java (Maven)"),
        ("build.gradle", "Java/Kotlin (Gradle)"),
        ("Cargo.toml", "Rust"),
        ("go.mod", "Go"),
    };

    /// <summary>
    /// rootPath 配下を最大 maxDepth まで走査し、見つかったプロジェクトを順次返す。
    /// </summary>
    public static IEnumerable<ProjectEntry> Scan(string rootPath, int maxDepth = 6)
    {
        if (!Directory.Exists(rootPath))
            yield break;

        foreach (var entry in ScanDirectory(rootPath, 0, maxDepth))
            yield return entry;
    }

    private static IEnumerable<ProjectEntry> ScanDirectory(string dir, int depth, int maxDepth)
    {
        if (depth > maxDepth)
            yield break;

        DirectoryInfo info;
        try
        {
            info = new DirectoryInfo(dir);
            if (SkipDirNames.Contains(info.Name, StringComparer.OrdinalIgnoreCase))
                yield break;
        }
        catch
        {
            yield break;
        }

        var detectedTypes = DetectMarkers(dir);
        var isGit = Directory.Exists(Path.Combine(dir, ".git"));

        if (detectedTypes.Count > 0 || isGit)
        {
            yield return new ProjectEntry
            {
                Path = dir,
                Name = info.Name,
                ProjectTypes = detectedTypes,
                IsGitRepo = isGit,
                LastModifiedUtc = GetLastModified(dir)
            };

            // プロジェクトを検出したら、その下にはこれ以上潜らない
            // (node_modules の中の package.json などを誤検出しないため)
            yield break;
        }

        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(dir);
        }
        catch
        {
            // アクセス権がないフォルダなどは無視
            yield break;
        }

        foreach (var sub in subDirs)
        {
            foreach (var found in ScanDirectory(sub, depth + 1, maxDepth))
                yield return found;
        }
    }

    private static List<string> DetectMarkers(string dir)
    {
        var found = new List<string>();
        try
        {
            var files = Directory.EnumerateFiles(dir)
                .Select(Path.GetFileName)
                .Where(f => f is not null)
                .Select(f => f!)
                .ToList();

            foreach (var (marker, label) in Markers)
            {
                var isWildcard = marker.StartsWith('*');
                var match = isWildcard
                    ? files.Any(f => f.EndsWith(marker[1..], StringComparison.OrdinalIgnoreCase))
                    : files.Any(f => string.Equals(f, marker, StringComparison.OrdinalIgnoreCase));

                if (match && !found.Contains(label))
                    found.Add(label);
            }
        }
        catch
        {
            // アクセス権エラーなどは無視して空リストを返す
        }

        return found;
    }

    private static DateTime GetLastModified(string dir)
    {
        try
        {
            return Directory.GetLastWriteTimeUtc(dir);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
