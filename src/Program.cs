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

        var root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var binDir = Path.Combine(root, "bin");
        var listsDir = Path.Combine(root, "lists");
        var strategiesDir = Path.Combine(root, "strategies");
        var configPath = Path.Combine(root, "config.json");

        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(listsDir);
        Directory.CreateDirectory(strategiesDir);

        if (!File.Exists(Path.Combine(binDir, "winws.exe")))
        {
            MessageBox.Show(
                "Не найден bin\\winws.exe.\nПересоберите проект через build.bat — он скачает runtime автоматически.",
                "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        try
        {
            WinDivertHelper.EnsureInstalled(binDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не удалось проверить/установить WinDivert:\n" + ex.Message,
                "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        var config = AppConfig.Load(configPath);
        Application.Run(new TrayApp(binDir, listsDir, strategiesDir, configPath, config));
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
                WorkingDirectory = AppContext.BaseDirectory
            });
        }
        catch
        {
            MessageBox.Show("Нужны права администратора (UAC).", "DpiTray",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
