using System.Diagnostics;

namespace CodeCatalog.Services;

public static class BuildRunner
{
    /// <summary>プロジェクト種別と構成からビルドコマンドを返す。対応するものがなければ null。</summary>
    public static (string command, string args)? GetBuildCommand(
        IList<string> projectTypes,
        string projectPath,
        string configuration = "Debug")
    {
        foreach (var type in projectTypes)
        {
            var result = type switch
            {
                ".NET" or "Visual Studio Solution" => ("dotnet", $"build -c {configuration}"),
                "Node.js"                          => ("npm", "run build"),
                "Python"                           => ("pip", "install -r requirements.txt"),
                "Java (Maven)"                     => ("mvn", "package -q"),
                "Java/Kotlin (Gradle)"             => GetGradleCommand(projectPath),
                "Rust"                             => ("cargo", configuration == "Release" ? "build --release" : "build"),
                "Go"                               => ("go", "build ./..."),
                _                                  => ((string, string)?)null
            };
            if (result is not null) return result;
        }
        return null;
    }

    private static (string, string) GetGradleCommand(string projectPath)
    {
        var gradlew = Path.Combine(projectPath,
            OperatingSystem.IsWindows() ? "gradlew.bat" : "gradlew");
        return File.Exists(gradlew) ? (gradlew, "build") : ("gradle", "build");
    }

    /// <summary>ビルドコマンドを非同期で実行し、出力を逐次 onOutput へ通知する。</summary>
    public static async Task<int> RunAsync(
        string command,
        string args,
        string workingDir,
        Action<string> onOutput,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(command, args)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("プロセスを起動できませんでした");

        proc.OutputDataReceived += (_, e) => { if (e.Data is not null) onOutput(e.Data); };
        proc.ErrorDataReceived  += (_, e) => { if (e.Data is not null) onOutput(e.Data); };

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(cancellationToken);
        return proc.ExitCode;
    }
}
