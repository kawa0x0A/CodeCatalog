namespace CodeCatalog.Services;

public static class ProjectRunner
{
    /// <summary>プロジェクト種別・設定・引数からターミナルで実行するコマンドを返す。</summary>
    public static (string command, string args)? GetRunCommand(
        IList<string> projectTypes,
        string projectPath,
        string configuration = "Debug",
        string launchArgs = "")
    {
        foreach (var type in projectTypes)
        {
            var result = type switch
            {
                ".NET" or "Visual Studio Solution" => DotNet(configuration, launchArgs),
                "Node.js"                          => NodeJs(launchArgs),
                "Python"                           => Python(projectPath, launchArgs),
                "Rust"                             => Rust(configuration, launchArgs),
                "Go"                               => Go(launchArgs),
                "Java (Maven)"                     => Maven(launchArgs),
                "Java/Kotlin (Gradle)"             => Gradle(projectPath, launchArgs),
                _                                  => ((string, string)?)null
            };
            if (result is not null) return result;
        }
        return null;
    }

    private static (string, string) DotNet(string config, string args)
    {
        var a = $"run -c {config}";
        if (!string.IsNullOrWhiteSpace(args)) a += $" -- {args}";
        return ("dotnet", a);
    }

    private static (string, string) NodeJs(string args)
    {
        var a = string.IsNullOrWhiteSpace(args) ? "start" : $"start -- {args}";
        return ("npm", a);
    }

    private static (string, string)? Python(string projectPath, string args)
    {
        // よくあるエントリーポイントを探す
        foreach (var ep in new[] { "main.py", "app.py", "run.py", "manage.py", "__main__.py" })
        {
            if (File.Exists(Path.Combine(projectPath, ep)))
            {
                var a = string.IsNullOrWhiteSpace(args) ? ep : $"{ep} {args}";
                return ("python", a);
            }
        }
        // 見つからなければパッケージとして実行
        var fallback = string.IsNullOrWhiteSpace(args) ? "-m ." : $"-m . {args}";
        return ("python", fallback);
    }

    private static (string, string) Rust(string config, string args)
    {
        var a = config == "Release" ? "run --release" : "run";
        if (!string.IsNullOrWhiteSpace(args)) a += $" -- {args}";
        return ("cargo", a);
    }

    private static (string, string) Go(string args)
    {
        var a = string.IsNullOrWhiteSpace(args) ? "run ." : $"run . {args}";
        return ("go", a);
    }

    private static (string, string) Maven(string args)
    {
        var a = string.IsNullOrWhiteSpace(args)
            ? "exec:java"
            : $"exec:java -Dexec.args=\"{args}\"";
        return ("mvn", a);
    }

    private static (string, string) Gradle(string projectPath, string args)
    {
        var gradlew = Path.Combine(projectPath,
            OperatingSystem.IsWindows() ? "gradlew.bat" : "gradlew");
        var cmd = File.Exists(gradlew) ? gradlew : "gradle";
        var a = string.IsNullOrWhiteSpace(args) ? "run" : $"run --args=\"{args}\"";
        return (cmd, a);
    }
}
