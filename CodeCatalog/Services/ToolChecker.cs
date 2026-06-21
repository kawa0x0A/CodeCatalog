using System.Diagnostics;

namespace CodeCatalog.Services;

/// <summary>
/// ビルド・実行に必要なツールが PATH に存在するか確認する。
/// 結果はプロセス内でキャッシュし、起動コマンドの重複実行を防ぐ。
/// </summary>
public static class ToolChecker
{
    private static readonly Dictionary<string, bool> _cache = new();

    public static string? GetWarning(IList<string> projectTypes)
    {
        foreach (var type in projectTypes)
        {
            var (command, displayName) = type switch
            {
                ".NET" or "Visual Studio Solution" => ("dotnet", ".NET SDK"),
                "Node.js"                          => ("node",   "Node.js"),
                "Python"                           => ("python", "Python"),
                "Rust"                             => ("cargo",  "Rust (cargo)"),
                "Go"                               => ("go",     "Go"),
                "Java (Maven)"                     => ("mvn",    "Java / Maven"),
                "Java/Kotlin (Gradle)"             => ("java",   "Java"),
                _                                  => (null, null)
            };

            if (command is not null && !IsAvailable(command))
                return $"{displayName} がインストールされていないか、PATH に見つかりません。";
        }
        return null;
    }

    private static bool IsAvailable(string command)
    {
        if (_cache.TryGetValue(command, out var hit)) return hit;

        try
        {
            // where (Windows) / which (macOS, Linux) でコマンドを検索
            var finder = OperatingSystem.IsWindows() ? "where" : "which";
            var psi = new ProcessStartInfo(finder, command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            return _cache[command] = proc?.ExitCode == 0;
        }
        catch
        {
            return _cache[command] = false;
        }
    }
}
