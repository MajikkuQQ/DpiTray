using System.Diagnostics;

namespace DpiTray;

/// <summary>
/// Отдельный Telegram WS Proxy (официальный релиз Flowseal/tg-ws-proxy).
/// Не смешивается с winws; запускается независимо из трея.
/// </summary>
internal static class TgProxyHelper
{
    public static string GetProxyDir()
        => Path.Combine(RuntimePaths.GetRuntimeRoot(), "tgproxy");

    public static string GetExePath()
        => Path.Combine(GetProxyDir(), "TgWsProxy_windows.exe");

    public static bool IsInstalled()
        => File.Exists(GetExePath()) && new FileInfo(GetExePath()).Length > 1_000_000;

    public static bool IsRunning()
        => Process.GetProcessesByName("TgWsProxy_windows").Length > 0
           || Process.GetProcessesByName("TgWsProxy").Length > 0;

    public static void Start()
    {
        if (!IsInstalled())
            throw new FileNotFoundException(
                "TgWsProxy не найден.\nЗапусти build.bat (скачает официальный релиз) или scripts\\fetch-tgproxy.ps1",
                GetExePath());

        if (IsRunning())
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = GetExePath(),
            WorkingDirectory = GetProxyDir(),
            UseShellExecute = true
        });
    }

    public static void Stop()
    {
        foreach (var name in new[] { "TgWsProxy_windows", "TgWsProxy" })
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    p.Kill(entireProcessTree: true);
                    p.WaitForExit(3000);
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
    }
}
