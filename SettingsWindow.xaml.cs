using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ZeroWubiLens;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    private bool _saved;

    internal SettingsWindow(Settings settings)
    {
        _settings = settings;
        InitializeComponent();

        Loaded += (_, _) => Populate();
    }

    private void Populate()
    {
        // hotkey
        ChkAlt.IsChecked = _settings.Hotkey.Alt;
        ChkCtrl.IsChecked = _settings.Hotkey.Ctrl;
        ChkShift.IsChecked = _settings.Hotkey.Shift;
        ChkWin.IsChecked = _settings.Hotkey.Win;

        for (char c = 'A'; c <= 'Z'; c++) CboKey.Items.Add(c.ToString());
        CboKey.Items.Add("/");
        CboKey.Text = _settings.Hotkey.Key.ToUpperInvariant();

        // web
        ChkWebEnabled.IsChecked = _settings.Web.Enabled;
        CboZoom.Text = _settings.Web.ZoomFactor.ToString("F1");
        ChkHideChrome.IsChecked = _settings.Web.HideChrome;
        TxtProxy.Text = _settings.Web.Proxy ?? "";

        // capture / popup
        ChkClipFallback.IsChecked = _settings.UseClipboardFallback;
        TxtCompactW.Text = _settings.Popup.CompactWidthDip.ToString("F0");
        TxtCompactH.Text = _settings.Popup.CompactHeightDip.ToString("F0");
        TxtExpandW.Text = _settings.Popup.ExpandedWidthDip.ToString("F0");
        TxtExpandH.Text = _settings.Popup.ExpandedHeightDip.ToString("F0");
        TxtOffset.Text = _settings.Popup.OffsetBelowPx.ToString();

        // dict
        TxtDictPath.Text = _settings.DictPath;
        TxtRepoPath.Text = _settings.ZeroXiRepoPath;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        // hotkey
        _settings.Hotkey.Alt = ChkAlt.IsChecked == true;
        _settings.Hotkey.Ctrl = ChkCtrl.IsChecked == true;
        _settings.Hotkey.Shift = ChkShift.IsChecked == true;
        _settings.Hotkey.Win = ChkWin.IsChecked == true;
        _settings.Hotkey.Key = CboKey.Text.Trim();

        // web
        _settings.Web.Enabled = ChkWebEnabled.IsChecked == true;
        if (double.TryParse(CboZoom.Text, out var z)) _settings.Web.ZoomFactor = z;
        _settings.Web.HideChrome = ChkHideChrome.IsChecked == true;
        _settings.Web.Proxy = TxtProxy.Text.Trim();

        // capture / popup
        _settings.UseClipboardFallback = ChkClipFallback.IsChecked == true;
        if (double.TryParse(TxtCompactW.Text, out var cw)) _settings.Popup.CompactWidthDip = cw;
        if (double.TryParse(TxtCompactH.Text, out var ch)) _settings.Popup.CompactHeightDip = ch;
        if (double.TryParse(TxtExpandW.Text, out var ew)) _settings.Popup.ExpandedWidthDip = ew;
        if (double.TryParse(TxtExpandH.Text, out var eh)) _settings.Popup.ExpandedHeightDip = eh;
        if (int.TryParse(TxtOffset.Text, out var off)) _settings.Popup.OffsetBelowPx = off;
        _settings.ZeroXiRepoPath = TxtRepoPath.Text.Trim();

        _settings.Save();
        _saved = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BrowseDict(object? sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "YAML 词典 (*.dict.yaml)|*.dict.yaml|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Title = "选择零西五笔词库",
        };
        if (dlg.ShowDialog(this) == true)
        {
            TxtDictPath.Text = dlg.FileName;
            _settings.DictPath = dlg.FileName;
        }
    }

    private void BrowseRepo(object? sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择 zeroxi-wubi 仓库目录",
            UseDescriptionForTitle = true,
            SelectedPath = TxtRepoPath.Text,
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtRepoPath.Text = dlg.SelectedPath;
            _settings.ZeroXiRepoPath = dlg.SelectedPath;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_saved)
        {
            AppPaths.Log("[Settings] dialog cancelled, no save");
        }
        base.OnClosed(e);
    }
}
