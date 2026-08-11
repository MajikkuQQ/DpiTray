using System.Diagnostics;
using System.Security.Principal;

namespace DpiTray;

internal static class Program
{
    private const string MutexName = "Global\\DpiTray_SingleInstance_Mutex";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);

        if (!IsAdministrator())
        {
            RelaunchAsAdmin();
            return;
        }

        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("DpiTray уже запущен.", "DpiTray",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        RuntimePaths.EnsureRuntimeLayout(out var binDir, out var listsDir, out var strategiesDir, out var logDir);
        var configPath = Path.Combine(RuntimePaths.GetRuntimeRoot(), "config.json");

        // Миграция старого конфига из папки exe (если был)
        var oldConfig = Path.Combine(RuntimePaths.GetAppDirectory(), "config.json");
        if (!File.Exists(configPath) && File.Exists(oldConfig))
            File.Copy(oldConfig, configPath, overwrite: false);

        if (!File.Exists(Path.Combine(binDir, "winws.exe")))
        {
            MessageBox.Show(
                "Не найден winws.exe в runtime.\nЗапустите build.bat и убедитесь, что рядом с DpiTray.exe есть папка bin.",
                "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Мягкая подготовка WinDivert из ASCII-пути. Не блокируем запуск:
        // winws сам поднимает драйвер при Старт, если службы ещё нет.
        var windivertWarn = WinDivertHelper.EnsureReady(binDir);
        if (!string.IsNullOrEmpty(windivertWarn))
            MessageBox.Show(windivertWarn, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        var config = AppConfig.Load(configPath);
        Application.Run(new TrayApp(binDir, listsDir, strategiesDir, configPath, logDir, config));
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void RelaunchAsAdmin()
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = RuntimePaths.GetAppDirectory()
            });
        }
        catch
        {
            MessageBox.Show("Нужны права администратора (UAC).", "DpiTray",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
