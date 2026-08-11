namespace DpiTray;

internal static class RuntimePaths
{
    /// <summary>
    /// winws (cygwin) часто ломается на путях с кириллицей.
    /// Рабочий runtime всегда держим в ASCII-пути ProgramData.
    /// </summary>
    public static string GetRuntimeRoot()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DpiTray");

    public static string GetAppDirectory()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
            return Path.GetDirectoryName(exe)!;

        return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static void EnsureRuntimeLayout(out string binDir, out string listsDir, out string strategiesDir, out string logDir)
    {
        var appDir = GetAppDirectory();
        var runtimeRoot = GetRuntimeRoot();

        binDir = Path.Combine(runtimeRoot, "bin");
        listsDir = Path.Combine(runtimeRoot, "lists");
        strategiesDir = Path.Combine(runtimeRoot, "strategies");
        logDir = Path.Combine(runtimeRoot, "logs");

        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(listsDir);
        Directory.CreateDirectory(strategiesDir);
        Directory.CreateDirectory(logDir);

        MirrorDirectory(Path.Combine(appDir, "bin"), binDir);
        MirrorDirectory(Path.Combine(appDir, "lists"), listsDir);
        MirrorDirectory(Path.Combine(appDir, "strategies"), strategiesDir);

        // На всякий случай: если рядом с exe пусто, но payload уже был в ProgramData — ок.
        if (!File.Exists(Path.Combine(binDir, "winws.exe")))
        {
            // fallback: старый layout рядом с exe
            var localBin = Path.Combine(appDir, "bin");
            if (File.Exists(Path.Combine(localBin, "winws.exe")))
            {
                MirrorDirectory(localBin, binDir);
            }
        }
    }

    private static void MirrorDirectory(string source, string dest)
    {
        if (!Directory.Exists(source))
            return;

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            var targetDir = Path.GetDirectoryName(target)!;
            Directory.CreateDirectory(targetDir);

            var srcInfo = new FileInfo(file);
            var dstInfo = new FileInfo(target);
            if (!dstInfo.Exists || dstInfo.Length != srcInfo.Length || dstInfo.LastWriteTimeUtc < srcInfo.LastWriteTimeUtc)
            {
                try
                {
                    File.Copy(file, target, overwrite: true);
                }
                catch (IOException)
                {
                    // Файл занят (часто WinDivert64.sys) — оставляем уже лежащую копию
                    if (!File.Exists(target))
                        throw;
                }
            }
        }
    }
}
