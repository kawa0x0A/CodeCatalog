namespace CodeCatalog.Services;

/// <summary>
/// ビルド成果物が存在するかどうかを簡易チェックする。
/// プロセス起動は行わず、フォルダ/ファイルの存在確認のみ。
/// </summary>
public static class BuildChecker
{
    public static bool QuickCheck(IList<string> projectTypes, string projectPath, string configuration)
    {
        foreach (var type in projectTypes)
        {
            return type switch
            {
                ".NET" or "Visual Studio Solution" => CheckDotNet(projectPath, configuration),
                "Rust"                             => CheckRust(projectPath, configuration),
                "Go"                               => CheckGo(projectPath),
                // Node.js / Python / Java はビルド成果物の場所が一定でないためスキップ
                _                                  => true
            };
        }
        return true;
    }

    // bin/{config}/ 以下に .dll か .exe があるか
    private static bool CheckDotNet(string projectPath, string configuration)
    {
        var binDir = Path.Combine(projectPath, "bin", configuration);
        if (!Directory.Exists(binDir)) return false;
        return Directory.EnumerateFiles(binDir, "*.dll", SearchOption.AllDirectories).Any()
            || Directory.EnumerateFiles(binDir, "*.exe", SearchOption.AllDirectories).Any();
    }

    // target/debug/ または target/release/ が存在するか
    private static bool CheckRust(string projectPath, string configuration)
    {
        var dir = Path.Combine(projectPath, "target", configuration.ToLowerInvariant());
        return Directory.Exists(dir);
    }

    // プロジェクトルートに実行ファイルがあるか
    private static bool CheckGo(string projectPath)
    {
        var name = Path.GetFileName(projectPath);
        return File.Exists(Path.Combine(projectPath, name + ".exe"))
            || File.Exists(Path.Combine(projectPath, name));
    }
}
