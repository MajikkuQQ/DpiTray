using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DpiTray;

internal sealed class WinwsRunner
{
    private readonly string _binDir;
    private readonly string _logDir;
    private Process? _process;
    private string? _lastLogPath;

    public WinwsRunner(string binDir, string logDir)
    {
        _binDir = binDir;
        _logDir = logDir;
    }

    public string? LastLogPath => _lastLogPath;

    public bool IsRunning
    {
        get
        {
            if (_process != null && !_process.HasExited)
                return true;
            return Process.GetProcessesByName("winws").Length > 0;
        }
    }

    public void Start(StrategyDefinition strategy, string listsDir)
    {
        Stop();

        var exe = Path.GetFullPath(Path.Combine(_binDir, "winws.exe"));
        var cygwin = Path.GetFullPath(Path.Combine(_binDir, "cygwin1.dll"));
        var windivertDll = Path.GetFullPath(Path.Combine(_binDir, "WinDivert.dll"));
        var windivertSys = Path.GetFullPath(Path.Combine(_binDir, "WinDivert64.sys"));

        EnsureFile(exe, "winws.exe");
        EnsureFile(cygwin, "cygwin1.dll");
        EnsureFile(windivertDll, "WinDivert.dll");
        EnsureFile(windivertSys, "WinDivert64.sys");

        if (ContainsNonAscii(_binDir) || ContainsNonAscii(listsDir))
            throw new InvalidOperationException(
                "Путь runtime содержит не-ASCII символы:\n" + _binDir +
                "\n\nDpiTray должен работать из C:\\ProgramData\\DpiTray");

        Directory.CreateDirectory(_logDir);
        _lastLogPath = Path.Combine(_logDir, "winws-last.log");
        var argsList = strategy.ExpandArgs(_binDir, listsDir).ToList();

        // Проверяем все файлы из аргументов (--hostlist=..., --dpi-desync-*=...)
        foreach (var missing in FindMissingArgFiles(argsList))
            throw new FileNotFoundException(
                "Не найден файл, нужный стратегии \"" + strategy.Id + "\":\n" + missing,
                missing);

        File.WriteAllText(_lastLogPath,
            "[" + DateTime.Now.ToString("s") + "] start" + Environment.NewLine +
            "exe=" + exe + Environment.NewLine +
            "cwd=" + _binDir + Environment.NewLine +
            "args=" + Environment.NewLine +
            string.Join(Environment.NewLine, argsList) + Environment.NewLine +
            "----" + Environment.NewLine,
            Encoding.UTF8);

        // НЕ редиректить stdout/stderr — cygwin/winws падает после init
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = _binDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        foreach (var arg in argsList)
            psi.ArgumentList.Add(arg);

        try
        {
            _process = Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Не удалось запустить winws.exe:\n" + exe + "\n\n" + ex.Message, ex);
        }

        if (_process == null)
            throw new InvalidOperationException("Не удалось запустить winws.exe:\n" + exe);

        Thread.Sleep(1000);
        if (_process.HasExited || Process.GetProcessesByName("winws").Length == 0)
        {
            var code = _process.HasExited ? _process.ExitCode.ToString() : "?";
            File.AppendAllText(_lastLogPath, "FAILED exit=" + code + Environment.NewLine, Encoding.UTF8);
            throw new InvalidOperationException(
                "winws сразу завершился (код " + code + ").\n" +
                "Попробуйте другую стратегию.\nЛог: " + _lastLogPath);
        }

        File.AppendAllText(_lastLogPath, "OK pid=" + _process.Id + Environment.NewLine, Encoding.UTF8);
    }

    public void Stop()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch { }
        finally
        {
            try { _process?.Dispose(); } catch { }
            _process = null;
        }

        foreach (var p in Process.GetProcessesByName("winws"))
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

    private static void EnsureFile(string path, string label)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "Не найден обязательный файл " + label + ":\n" + path,
                path);
    }

    private static IEnumerable<string> FindMissingArgFiles(IEnumerable<string> args)
    {
        foreach (var arg in args)
        {
            var idx = arg.IndexOf('=');
            if (idx <= 0 || idx >= arg.Length - 1)
                continue;

            var key = arg[..idx];
            var val = arg[(idx + 1)..].Trim('"');

            // Только аргументы, которые реально указывают на файлы
            if (!key.Contains("hostlist", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("ipset", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("fake-", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("pattern", StringComparison.OrdinalIgnoreCase)
                && !key.EndsWith("-file", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("quic", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("tls", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("discord", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("stun", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("split-seqovl-pattern", StringComparison.OrdinalIgnoreCase))
            {
                // hostlist without domain list keyword handled above via hostlist
                if (!Regex.IsMatch(key, "hostlist|ipset|fake|pattern|quic|tls|discord|stun", RegexOptions.IgnoreCase))
                    continue;
            }

            // значения-доменов/чисел пропускаем
            if (!val.Contains('/') && !val.Contains('\\') && !val.Contains('.'))
                continue;
            if (val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!val.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                && !val.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)
                && !val.EndsWith(".list", StringComparison.OrdinalIgnoreCase))
                continue;

            var normalized = val.Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(normalized))
                yield return normalized;
        }
    }

    private static bool ContainsNonAscii(string path) => path.Any(c => c > 127);
}
