using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DpiTray;

internal static class WinDivertHelper
{
    private const string ServiceName = "WinDivert";

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string? lpPathName);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr WinDivertOpen(string filter, int layer, short priority, ulong flags);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool WinDivertClose(IntPtr handle);

    public static bool IsDriverInstalled()
    {
        var output = RunCapture("sc.exe", $"query {ServiceName}");
        return output.Contains("SERVICE_NAME", StringComparison.OrdinalIgnoreCase)
               && !output.Contains("1060", StringComparison.Ordinal);
    }

    public static void EnsureInstalled(string binDir)
    {
        var dll = Path.Combine(binDir, "WinDivert.dll");
        var sys = Path.Combine(binDir, "WinDivert64.sys");

        if (!File.Exists(dll))
            throw new FileNotFoundException("Не найден WinDivert.dll", dll);
        if (!File.Exists(sys))
            throw new FileNotFoundException("Не найден WinDivert64.sys", sys);

        if (IsDriverInstalled())
            return;

        SetDllDirectory(binDir);
        try
        {
            var handle = WinDivertOpen("false", 0, 0, 0);
            if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                WinDivertClose(handle);
            else
                InstallViaSc(sys);
        }
        finally
        {
            SetDllDirectory(null);
        }

        if (!IsDriverInstalled())
            InstallViaSc(sys);

        if (!IsDriverInstalled())
            throw new InvalidOperationException("Драйвер WinDivert не установился.");
    }

    private static void InstallViaSc(string sysPath)
    {
        var binPath = @"\??\" + Path.GetFullPath(sysPath);
        Run("sc.exe", $"create {ServiceName} type= kernel start= demand binPath= \"{binPath}\" DisplayName= \"WinDivert\"");
        Run("sc.exe", $"start {ServiceName}");
    }

    private static void Run(string fileName, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        p?.WaitForExit(15000);
    }

    private static string RunCapture(string fileName, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        if (p == null) return string.Empty;
        var text = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(10000);
        return text;
    }
}
