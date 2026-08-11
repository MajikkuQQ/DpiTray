using System.Diagnostics;

namespace DpiTray;

internal sealed class TrayApp : ApplicationContext
{
    private readonly string _binDir;
    private readonly string _listsDir;
    private readonly string _configPath;
    private readonly string _logDir;
    private readonly AppConfig _config;
    private readonly WinwsRunner _runner;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly List<StrategyDefinition> _strategies;
    private Icon? _iconRunning;
    private Icon? _iconStopped;

    public TrayApp(
        string binDir,
        string listsDir,
        string strategiesDir,
        string configPath,
        string logDir,
        AppConfig config)
    {
        _binDir = binDir;
        _listsDir = listsDir;
        _configPath = configPath;
        _logDir = logDir;
        _config = config;
        _runner = new WinwsRunner(binDir, logDir);
        _strategies = StrategyDefinition.LoadAll(strategiesDir);

        if (_strategies.Count == 0)
        {
            MessageBox.Show("В папке strategies нет JSON-стратегий.", "DpiTray",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        var legacy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "general", "general-alt", "simple-https", "simple-fake-alt",
            "fake-tls-auto", "discord-alt"
        };
        if (legacy.Contains(_config.SelectedStrategy))
        {
            _config.SelectedStrategy = "recommended";
            _config.Save(_configPath);
        }

        if (!_strategies.Any(s => s.Id.Equals(_config.SelectedStrategy, StringComparison.OrdinalIgnoreCase))
            && _strategies.Count > 0)
        {
            _config.SelectedStrategy = _strategies[0].Id;
            _config.Save(_configPath);
        }

        _iconRunning = IconFactory.CreateStatusIcon(true);
        _iconStopped = IconFactory.CreateStatusIcon(false);
        _menu = BuildMenu();

        _tray = new NotifyIcon
        {
            Icon = _iconStopped,
            Visible = true,
            Text = "DpiTray — остановлен",
            ContextMenuStrip = _menu
        };
        _tray.DoubleClick += (_, _) => ToggleStartStop();

        _statusTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();

        SyncAutoStartMenu();
        SyncTgMenu();
        RefreshStatus();

        if (_config.AutoStart && _config.AutoStartStrategy)
        {
            try { StartSelected(); }
            catch (Exception ex)
            {
                MessageBox.Show("Автозапуск стратегии не удался:\n" + ex.Message,
                    "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var strategiesItem = new ToolStripMenuItem("Стратегии");

        foreach (var strategy in _strategies)
        {
            var item = new ToolStripMenuItem(strategy.Name)
            {
                Tag = strategy.Id,
                Checked = strategy.Id.Equals(_config.SelectedStrategy, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => SelectStrategy(strategy.Id);
            strategiesItem.DropDownItems.Add(item);
        }

        if (strategiesItem.DropDownItems.Count == 0)
            strategiesItem.DropDownItems.Add(new ToolStripMenuItem("(нет стратегий)") { Enabled = false });

        menu.Items.Add(strategiesItem);
        menu.Items.Add(new ToolStripSeparator());

        var startItem = new ToolStripMenuItem("Старт (zapret + TG)", null, (_, _) =>
        {
            try { StartSelected(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }) { Name = "start" };

        var stopItem = new ToolStripMenuItem("Стоп (всё)", null, (_, _) =>
        {
            StopAll();
        }) { Name = "stop" };

        var autoItem = new ToolStripMenuItem("Автозапуск с Windows")
        {
            Name = "autostart",
            CheckOnClick = true
        };
        autoItem.CheckedChanged += (_, _) =>
        {
            _config.AutoStart = autoItem.Checked;
            AutoStartHelper.SetEnabled(autoItem.Checked);
            _config.Save(_configPath);
        };

        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(autoItem);
        menu.Items.Add(new ToolStripSeparator());

        var tgItem = new ToolStripMenuItem("Telegram (TgWsProxy)") { Name = "tgroot" };

        var tgTogether = new ToolStripMenuItem("Запускать вместе со Старт")
        {
            Name = "tgTogether",
            CheckOnClick = true,
            Checked = _config.StartTgWithZapret
        };
        tgTogether.CheckedChanged += (_, _) =>
        {
            _config.StartTgWithZapret = tgTogether.Checked;
            _config.Save(_configPath);
        };

        tgItem.DropDownItems.Add(tgTogether);
        tgItem.DropDownItems.Add(new ToolStripSeparator());
        tgItem.DropDownItems.Add(new ToolStripMenuItem("Старт только TgWsProxy", null, (_, _) =>
        {
            try
            {
                EnsureTgProxyPresent();
                TgProxyHelper.Start();
                RefreshStatus();
                _tray.ShowBalloonTip(3000, "DpiTray",
                    "TgWsProxy запущен.\nTelegram → Настройки → Данные → Прокси:\nMTProto 127.0.0.1:1443\n(secret смотри в окне TgWsProxy)",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }));
        tgItem.DropDownItems.Add(new ToolStripMenuItem("Стоп только TgWsProxy", null, (_, _) =>
        {
            TgProxyHelper.Stop();
            RefreshStatus();
        }));
        menu.Items.Add(tgItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Открыть лог winws", null, (_, _) =>
        {
            var path = _runner.LastLogPath ?? Path.Combine(_logDir, "winws-last.log");
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            else
                MessageBox.Show("Лог ещё не создан. Сначала нажмите Старт.", "DpiTray");
        }));
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => ExitApp()));
        return menu;
    }

    private void EnsureTgProxyPresent()
    {
        if (TgProxyHelper.IsInstalled())
            return;

        var appCopy = Path.Combine(RuntimePaths.GetAppDirectory(), "tgproxy", "TgWsProxy_windows.exe");
        if (!File.Exists(appCopy))
            return;

        Directory.CreateDirectory(TgProxyHelper.GetProxyDir());
        File.Copy(appCopy, TgProxyHelper.GetExePath(), overwrite: true);
    }

    private void SelectStrategy(string id)
    {
        _config.SelectedStrategy = id;
        _config.Save(_configPath);

        if (_menu.Items[0] is ToolStripMenuItem strategiesRoot)
        {
            foreach (ToolStripItem item in strategiesRoot.DropDownItems)
            {
                if (item is ToolStripMenuItem mi)
                    mi.Checked = string.Equals(mi.Tag as string, id, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (_runner.IsRunning)
        {
            try { StartSelected(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private StrategyDefinition GetSelectedStrategy()
    {
        return _strategies.FirstOrDefault(s =>
                   s.Id.Equals(_config.SelectedStrategy, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException("Стратегия не выбрана или не найдена.");
    }

    private void StartSelected()
    {
        var strategy = GetSelectedStrategy();
        _runner.Start(strategy, _listsDir);

        var tip = "Запущено: " + strategy.Name;

        if (_config.StartTgWithZapret)
        {
            try
            {
                EnsureTgProxyPresent();
                TgProxyHelper.Start();
                tip += "\n+ TgWsProxy (127.0.0.1:1443)";
            }
            catch (Exception ex)
            {
                tip += "\nTgWsProxy не стартовал: " + ex.Message;
            }
        }

        RefreshStatus();
        _tray.ShowBalloonTip(3000, "DpiTray", tip, ToolTipIcon.Info);
    }

    private void StopAll()
    {
        _runner.Stop();
        if (_config.StartTgWithZapret || TgProxyHelper.IsRunning())
            TgProxyHelper.Stop();
        RefreshStatus();
    }

    private void ToggleStartStop()
    {
        if (_runner.IsRunning || TgProxyHelper.IsRunning())
        {
            StopAll();
            return;
        }

        try { StartSelected(); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SyncAutoStartMenu()
    {
        if (_menu.Items["autostart"] is not ToolStripMenuItem autoItem)
            return;

        var enabled = _config.AutoStart || AutoStartHelper.IsEnabled();
        _config.AutoStart = enabled;
        autoItem.Checked = enabled;
        if (enabled)
            AutoStartHelper.SetEnabled(true);
    }

    private void SyncTgMenu()
    {
        if (_menu.Items["tgroot"] is not ToolStripMenuItem tgRoot)
            return;
        if (tgRoot.DropDownItems["tgTogether"] is ToolStripMenuItem together)
            together.Checked = _config.StartTgWithZapret;
    }

    private void RefreshStatus()
    {
        var zapret = _runner.IsRunning;
        var tg = TgProxyHelper.IsRunning();
        var any = zapret || tg;

        _tray.Icon = any ? _iconRunning : _iconStopped;

        var parts = new List<string>();
        if (zapret) parts.Add("zapret");
        if (tg) parts.Add("TG");
        _tray.Text = parts.Count == 0
            ? "DpiTray — остановлен"
            : "DpiTray — " + string.Join(" + ", parts);

        if (_menu.Items["start"] is ToolStripMenuItem startItem)
            startItem.Enabled = !zapret;
        if (_menu.Items["stop"] is ToolStripMenuItem stopItem)
            stopItem.Enabled = any;
    }

    private void ExitApp()
    {
        _statusTimer.Stop();
        StopAll();
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        _iconRunning?.Dispose();
        _iconStopped?.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Dispose();
            _tray.Dispose();
            _menu.Dispose();
            _iconRunning?.Dispose();
            _iconStopped?.Dispose();
        }
        base.Dispose(disposing);
    }
}
