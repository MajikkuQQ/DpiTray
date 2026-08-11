using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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

    /// <summary>
    /// Готовит WinDivert из ASCII-пути. Не бросает fatal: winws сам поднимает драйвер при старте.
    /// Возвращает null при успехе/если можно продолжать, иначе мягкое предупреждение.
    /// </summary>
    public static string? EnsureReady(string binDir)
    {
        binDir = Path.GetFullPath(binDir);

        if (ContainsNonAscii(binDir))
            return "Runtime WinDivert лежит в пути с кириллицей: " + binDir;

        var dll = Path.Combine(binDir, "WinDivert.dll");
        var sys = Path.Combine(binDir, Environment.Is64BitOperatingSystem ? "WinDivert64.sys" : "WinDivert32.sys");
        if (!File.Exists(sys))
            sys = Path.Combine(binDir, "WinDivert64.sys");

        if (!File.Exists(dll))
            return "Не найден WinDivert.dll в " + binDir;
        if (!File.Exists(sys))
            return "Не найден WinDivert64.sys в " + binDir;

        // Уже есть служба / уже running — ок, ничего не трогаем
        if (IsServicePresent() || IsServiceRunning())
            return null;

        // Пытаемся установить: сначала через WinDivertOpen (штатный путь), потом sc
        TryOpenAndClose(binDir);

        if (IsServicePresent() || IsServiceRunning())
            return null;

        TryInstallViaSc(sys);
        TryStartService();

        if (IsServicePresent() || IsServiceRunning())
            return null;

        // Не fatal: winws при Старт сам откроет WinDivert и поставит драйвер из своей папки bin
        return null;
    }

    public static bool IsServicePresent()
    {
        var output = RunCapture("sc.exe", "query " + ServiceName);
        // 1060 = service does not exist
        if (output.Contains("1060", StringComparison.Ordinal))
            return false;
        return output.Contains("SERVICE_NAME", StringComparison.OrdinalIgnoreCase)
               || output.Contains(ServiceName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsServiceRunning()
    {
        var output = RunCapture("sc.exe", "query " + ServiceName);
        return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryOpenAndClose(string binDir)
    {
        SetDllDirectory(binDir);
        try
        {
            // layer NETWORK = 0; filter "false" = открыть handle без захвата пакетов
            var handle = WinDivertOpen("false", 0, 0, 0);
            if (handle != IntPtr.Zero && handle != new IntPtr(-1))
            {
                WinDivertClose(handle);
            }
        }
        catch
        {
            // ignore — ниже fallback через sc / winws
        }
        finally
        {
            SetDllDirectory(null);
        }
    }

    private static void TryInstallViaSc(string sysPath)
    {
        var binPath = @"\??\" + Path.GetFullPath(sysPath);

        // Если служба битая/со старым путём — пересоздаём
        if (IsServicePresent())
        {
            Run("sc.exe", "stop " + ServiceName);
            Run("sc.exe", "delete " + ServiceName);
            Thread.Sleep(300);
        }

        Run("sc.exe",
            "create " + ServiceName +
            " type= kernel start= demand binPath= \"" + binPath + "\" DisplayName= \"WinDivert\"");
    }

    private static void TryStartService()
    {
        Run("sc.exe", "start " + ServiceName);
    }

    private static void Run(string fileName, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default
            });
            p?.WaitForExit(15000);
        }
        catch
        {
            // ignore
        }
    }

    private static string RunCapture(string fileName, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default
            });
            if (p == null) return string.Empty;
            var text = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(10000);
            return text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool ContainsNonAscii(string path) => path.Any(c => c > 127);
}
