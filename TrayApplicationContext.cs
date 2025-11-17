namespace WinTrimmy;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly AppSettings _settings;
    private readonly ClipboardMonitor _monitor;
    private readonly ContextMenuStrip _contextMenu;
    private Icon? _currentIcon;

    private ToolStripMenuItem _autoTrimItem = null!;
    private ToolStripMenuItem _lowItem = null!;
    private ToolStripMenuItem _normalItem = null!;
    private ToolStripMenuItem _highItem = null!;
    private ToolStripMenuItem _preserveBlankLinesItem = null!;
    private ToolStripMenuItem _removeBoxDrawingItem = null!;
    private ToolStripMenuItem _launchAtLoginItem = null!;
    private ToolStripLabel _lastActionLabel = null!;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _monitor = new ClipboardMonitor(_settings);
        _monitor.OnClipboardTrimmed += OnClipboardTrimmed;

        _contextMenu = BuildContextMenu();

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "WinTrimmy - Clipboard Command Flattener",
            ContextMenuStrip = _contextMenu
        };
        SetTrayIcon(CreateTrayIcon());

        _trayIcon.DoubleClick += (s, e) => TrimNow();

        _monitor.Start();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        // Header
        var headerLabel = new ToolStripLabel("WinTrimmy")
        {
            Font = new Font(menu.Font, FontStyle.Bold)
        };
        menu.Items.Add(headerLabel);
        menu.Items.Add(new ToolStripSeparator());

        // Last action
        _lastActionLabel = new ToolStripLabel("Ready")
        {
            ForeColor = Color.Gray
        };
        menu.Items.Add(_lastActionLabel);
        menu.Items.Add(new ToolStripSeparator());

        // Auto-trim toggle
        _autoTrimItem = new ToolStripMenuItem("Auto-trim enabled")
        {
            Checked = _settings.AutoTrimEnabled,
            CheckOnClick = true
        };
        _autoTrimItem.Click += (s, e) =>
        {
            _settings.AutoTrimEnabled = _autoTrimItem.Checked;
            _settings.Save();
            UpdateTrayIcon();
        };
        menu.Items.Add(_autoTrimItem);

        // Trim now button
        var trimNowItem = new ToolStripMenuItem("Trim Clipboard Now");
        trimNowItem.Click += (s, e) => TrimNow();
        menu.Items.Add(trimNowItem);

        menu.Items.Add(new ToolStripSeparator());

        // Aggressiveness submenu
        var aggressivenessMenu = new ToolStripMenuItem("Aggressiveness");

        _lowItem = new ToolStripMenuItem("Low")
        {
            Checked = _settings.Aggressiveness == Aggressiveness.Low,
            ToolTipText = "Conservative - only obvious multi-line commands"
        };
        _lowItem.Click += (s, e) => SetAggressiveness(Aggressiveness.Low);

        _normalItem = new ToolStripMenuItem("Normal")
        {
            Checked = _settings.Aggressiveness == Aggressiveness.Normal,
            ToolTipText = "Balanced - typical use cases"
        };
        _normalItem.Click += (s, e) => SetAggressiveness(Aggressiveness.Normal);

        _highItem = new ToolStripMenuItem("High")
        {
            Checked = _settings.Aggressiveness == Aggressiveness.High,
            ToolTipText = "Aggressive - flatten most multi-line text"
        };
        _highItem.Click += (s, e) => SetAggressiveness(Aggressiveness.High);

        aggressivenessMenu.DropDownItems.Add(_lowItem);
        aggressivenessMenu.DropDownItems.Add(_normalItem);
        aggressivenessMenu.DropDownItems.Add(_highItem);
        menu.Items.Add(aggressivenessMenu);

        menu.Items.Add(new ToolStripSeparator());

        // Options
        _preserveBlankLinesItem = new ToolStripMenuItem("Preserve blank lines")
        {
            Checked = _settings.PreserveBlankLines,
            CheckOnClick = true
        };
        _preserveBlankLinesItem.Click += (s, e) =>
        {
            _settings.PreserveBlankLines = _preserveBlankLinesItem.Checked;
            _settings.Save();
        };
        menu.Items.Add(_preserveBlankLinesItem);

        _removeBoxDrawingItem = new ToolStripMenuItem("Remove box drawing characters")
        {
            Checked = _settings.RemoveBoxDrawing,
            CheckOnClick = true
        };
        _removeBoxDrawingItem.Click += (s, e) =>
        {
            _settings.RemoveBoxDrawing = _removeBoxDrawingItem.Checked;
            _settings.Save();
        };
        menu.Items.Add(_removeBoxDrawingItem);

        menu.Items.Add(new ToolStripSeparator());

        // Launch at login
        _launchAtLoginItem = new ToolStripMenuItem("Launch at login")
        {
            Checked = _settings.LaunchAtLogin,
            CheckOnClick = true
        };
        _launchAtLoginItem.Click += (s, e) =>
        {
            _settings.SetLaunchAtLogin(_launchAtLoginItem.Checked);
        };
        menu.Items.Add(_launchAtLoginItem);

        menu.Items.Add(new ToolStripSeparator());

        // About
        var aboutItem = new ToolStripMenuItem("About WinTrimmy");
        aboutItem.Click += (s, e) => ShowAbout();
        menu.Items.Add(aboutItem);

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void SetAggressiveness(Aggressiveness level)
    {
        _settings.Aggressiveness = level;
        _settings.Save();

        _lowItem.Checked = level == Aggressiveness.Low;
        _normalItem.Checked = level == Aggressiveness.Normal;
        _highItem.Checked = level == Aggressiveness.High;
    }

    private void TrimNow()
    {
        _monitor.ForceTrim();
        _lastActionLabel.Text = "Manual trim executed";
    }

    private void OnClipboardTrimmed(string summary)
    {
        _lastActionLabel.Text = $"Trimmed: {summary}";
        _trayIcon.ShowBalloonTip(2000, "WinTrimmy", "Clipboard command flattened", ToolTipIcon.Info);
    }

    private void UpdateTrayIcon()
    {
        SetTrayIcon(CreateTrayIcon());
        var status = _settings.AutoTrimEnabled ? "Active" : "Paused";
        _trayIcon.Text = $"WinTrimmy - {status}";
    }

    private void SetTrayIcon(Icon icon)
    {
        var previous = _currentIcon;
        _trayIcon.Icon = icon;
        _currentIcon = icon;
        previous?.Dispose();
    }

    private static Icon CreateTrayIcon()
    {
        // Create a simple scissors icon programmatically
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.White, 2);
            // Draw scissors shape
            g.DrawLine(pen, 2, 2, 14, 14);
            g.DrawLine(pen, 14, 2, 2, 14);
            g.DrawEllipse(pen, 1, 1, 4, 4);
            g.DrawEllipse(pen, 11, 1, 4, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
            bitmap.Dispose();
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "WinTrimmy v1.0\n\n" +
            "Automatically flattens multi-line shell commands\n" +
            "copied to the clipboard.\n\n" +
            "Based on Trimmy for macOS by @steipete\n\n" +
            "Double-click the tray icon to manually trim.",
            "About WinTrimmy",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void Exit()
    {
        _monitor.Stop();
        _monitor.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Icon = null;
        _currentIcon?.Dispose();
        _currentIcon = null;
        _trayIcon.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitor.Dispose();
            _trayIcon.Icon = null;
            _currentIcon?.Dispose();
            _currentIcon = null;
            _trayIcon.Dispose();
            _contextMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
