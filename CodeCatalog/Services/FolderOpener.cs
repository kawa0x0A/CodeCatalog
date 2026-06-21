using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeCatalog.Services;

public static class FolderOpener
{
    public static void OpenInFileManager(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Start("explorer.exe", $"\"{path}\"");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Start("open", $"\"{path}\"");
            else
                Start("xdg-open", $"\"{path}\"");
        }
        catch { }
    }

    public static void OpenInVsCode(string path)
    {
        try
        {
            Start("code", $"\"{path}\"");
        }
        catch { }
    }

    /// <summary>ターミナルを開き、指定コマンドを実行する。アプリ終了後もウィンドウを維持する。</summary>
    public static void RunInTerminal(string workingDir, string command, string args)
    {
        var fullCmd = $"{command} {args}".Trim();
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows Terminal: new-tab でコマンド実行、終了後も残す
                if (TryStart("wt.exe",
                    $"new-tab --startingDirectory \"{workingDir}\" -- cmd /K \"{fullCmd}\""))
                    return;
                // PowerShell フォールバック
                if (TryStart("pwsh.exe",
                    $"-NoExit -Command \"Set-Location '{workingDir}'; {fullCmd}\""))
                    return;
                // cmd.exe フォールバック
                Start("cmd.exe", $"/K \"{fullCmd}\"", workingDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // osascript でターミナルを開いてコマンドを実行
                var script = $"tell app \"Terminal\" to do script \"cd '{workingDir}' && {fullCmd}\"";
                Start("osascript", $"-e \"{script}\"");
            }
            else
            {
                Start("x-terminal-emulator", $"-e bash -c \"{fullCmd}; exec bash\"", workingDir);
            }
        }
        catch { }
    }

    /// <summary>ターミナルを開かずにバックグラウンドでコマンドを実行する。</summary>
    public static void RunInBackground(string workingDir, string command, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(command, args)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch { }
    }

    public static void OpenTerminal(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows Terminal が入っていれば優先して使う
                if (TryStart("wt.exe", $"--startingDirectory \"{path}\""))
                    return;
                // フォールバック: PowerShell
                if (TryStart("pwsh.exe", $"-NoExit -Command \"Set-Location '{path}'\""))
                    return;
                // 最終フォールバック: cmd.exe
                Start("cmd.exe", $"/K cd /d \"{path}\"", path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Start("open", $"-a Terminal \"{path}\"");
            }
            else
            {
                Start("x-terminal-emulator", string.Empty, path);
            }
        }
        catch { }
    }

    private static bool TryStart(string fileName, string arguments, string? workingDirectory = null)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = true
            };
            if (workingDirectory is not null)
                psi.WorkingDirectory = workingDirectory;
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Start(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true
        };
        if (workingDirectory is not null)
            psi.WorkingDirectory = workingDirectory;
        Process.Start(psi);
    }
}
