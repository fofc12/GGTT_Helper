using System.Diagnostics;
using System.Windows;

namespace ZeroWubiLens;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private Settings _settings = new();
    private LookupSources? _sources;
    private RecentCache? _recent;
    private Task<LocalWubiDictionary>? _dictTask;
    private LocalWubiDictionary? _dict;
    private HotkeyService? _hotkey;
    private MainWindow? _popup;
    private VocabularyWorkshopWindow? _workshop;
    private System.Windows.Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "ZeroWubiLens_SingleInstance_8e0f7a12", out bool isNew);
        if (!isNew)
        {
            System.Windows.MessageBox.Show("零西五笔工坊已在运行（看系统托盘）。", "零西五笔工坊");
            Shutdown();
            return;
        }

        AppPaths.EnsureCache();
        _settings = Settings.Load();
        _sources = new LookupSources(_settings.Web);
        _recent = new RecentCache(_settings.RecentCacheSize);

        // 后台预载词库，开机后用户开始打字时通常已就绪
        _dictTask = Task.Run(() => LocalWubiDictionary.LoadFrom(_settings.ResolvedDictPath));

        _popup = new MainWindow(_settings, _sources);

        _hotkey = new HotkeyService();
        _hotkey.Pressed += OnHotkey;
        bool ok = _hotkey.Register(_settings.Hotkey);

        SetupTray(ok);
        if (e.Args.Any(arg => string.Equals(arg, "--workshop", StringComparison.OrdinalIgnoreCase)))
            OpenVocabularyWorkshop();

        AppPaths.Log("[App] started");
    }

    private LocalWubiDictionary Dict()
    {
        if (_dict is not null) return _dict;
        _dict = _dictTask is not null
            ? _dictTask.Result
            : LocalWubiDictionary.LoadFrom(_settings.ResolvedDictPath);
        return _dict;
    }

    private void OnHotkey()
    {
        try
        {
            if (_popup is { IsVisible: true })
            {
                if (!_popup.IsExpanded) _popup.Expand();
                else _popup.HidePopup();
                return;
            }

            var cap = CharacterCapture.Capture(_settings.UseClipboardFallback);
            if (!cap.Ok)
            {
                AppPaths.Log("[Hotkey] 未取得光标左侧汉字");
                return;
            }

            var res = Dict().Lookup(cap.Char);
            _recent?.Record(res);
            _popup?.ShowCompact(res, cap.CaretPx, cap.Method);
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Hotkey] {ex}");
        }
    }

    private void SetupTray(bool hotkeyOk)
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Visible = true,
            Text = "零西五笔工坊 v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) + " (Alt+Z)",
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("打开词库工坊…", null, (_, _) => OpenVocabularyWorkshop());
        menu.Items.Add("设置…", null, (_, _) => OpenSettings());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("词库状态", null, (_, _) => ShowStatus());
        menu.Items.Add("打开缓存目录", null, (_, _) => OpenCacheDir());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Shutdown());
        _tray.ContextMenuStrip = menu;

        if (!hotkeyOk)
            _tray.ShowBalloonTip(4000, "零西五笔工坊",
                "热键 Alt+Z 注册失败，可能与其它程序冲突。",
                System.Windows.Forms.ToolTipIcon.Warning);
    }

    private void OpenVocabularyWorkshop()
    {
        try
        {
            if (_workshop is null || !_workshop.IsVisible)
            {
                _workshop = new VocabularyWorkshopWindow(_settings);
                _workshop.Show();
            }
            else
            {
                _workshop.Activate();
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Workshop] open failed: {ex}");
            System.Windows.MessageBox.Show(ex.Message, "零西五笔工坊");
        }
    }

    private void ShowStatus()
    {
        var d = Dict();
        string msg = d.Loaded
            ? $"词库已载入：{d.EntryCount} 条\n{d.Path}"
            : $"词库未载入：{d.Error}\n{d.Path}";
        System.Windows.MessageBox.Show(msg, "零西五笔工坊 · 词库状态");
    }

    private static void OpenCacheDir()
    {
        try
        {
            AppPaths.EnsureCache();
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AppPaths.CacheDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Tray] open cache: {ex.Message}");
        }
    }

    private void OpenSettings()
    {
        var dlg = new SettingsWindow(_settings);
        dlg.ShowDialog();
        if (_sources is not null)
            _sources = new LookupSources(_settings.Web);
        // hotkey / 代理 / 词库路径 需要重启生效，由设置窗提示
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _hotkey?.Dispose();
            if (_tray is not null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            _mutex?.Dispose();
        }
        catch { /* ignore */ }
        AppPaths.Log("[App] exit");
        base.OnExit(e);
    }
}
