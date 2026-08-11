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

        var startItem = new ToolStripMenuItem("Старт", null, (_, _) =>
        {
            try { StartSelected(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DpiTray", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }) { Name = "start" };

        var stopItem = new ToolStripMenuItem("Стоп", null, (_, _) =>
        {
            _runner.Stop();
            RefreshStatus();
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
        RefreshStatus();
        _tray.ShowBalloonTip(2000, "DpiTray", $"Запущено: {strategy.Name}", ToolTipIcon.Info);
    }

    private void ToggleStartStop()
    {
        if (_runner.IsRunning)
        {
            _runner.Stop();
            RefreshStatus();
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

    private void RefreshStatus()
    {
        var running = _runner.IsRunning;
        _tray.Icon = running ? _iconRunning : _iconStopped;
        _tray.Text = running ? "DpiTray — запущен" : "DpiTray — остановлен";

        if (_menu.Items["start"] is ToolStripMenuItem startItem)
            startItem.Enabled = !running;
        if (_menu.Items["stop"] is ToolStripMenuItem stopItem)
            stopItem.Enabled = running;
    }

    private void ExitApp()
    {
        _statusTimer.Stop();
        _runner.Stop();
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
