using System.Diagnostics;

namespace DpiTray;

internal sealed class WinwsRunner
{
    private readonly string _binDir;
    private Process? _process;

    public WinwsRunner(string binDir) => _binDir = binDir;

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

        var exe = Path.Combine(_binDir, "winws.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException("Не найден winws.exe", exe);

        if (!File.Exists(Path.Combine(_binDir, "cygwin1.dll")))
            throw new FileNotFoundException("Не найден cygwin1.dll рядом с winws.exe");

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = _binDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        foreach (var arg in strategy.ExpandArgs(_binDir, listsDir))
            psi.ArgumentList.Add(arg);

        _process = Process.Start(psi)
                   ?? throw new InvalidOperationException("Не удалось запустить winws.exe");
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
            _process?.Dispose();
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
}
