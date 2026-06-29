using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Color = System.Windows.Media.Color;

namespace ZeroWubiLens;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly LookupSources _sources;
    private readonly KeyboardHook _hook = new();
    private Task? _wvInit;
    private bool _wvReady;
    private bool _expanded;
    private string _currentChar = "";
    private Rect _caretPx = Rect.Empty;

    internal bool IsExpanded => _expanded;

    private const string CleanupJs =
        "(function(){function clean(){try{" +
        "if(!document.getElementById('zwl-style')){var s=document.createElement('style');s.id='zwl-style';" +
        "s.textContent='.md-header,.md-tabs,.md-sidebar,.md-footer,.md-banner,.md-search,.md-top{display:none!important}.md-main__inner{margin-top:0!important}.md-content,.md-grid{max-width:100%!important}.md-content__inner{margin:0!important;padding-top:.3rem!important;overflow-x:visible!important;max-width:none!important}.md-content__inner:before{display:none!important}body{overflow-x:auto!important}.md-content__inner table{width:auto!important;max-width:none!important}.md-content img,.md-content canvas,.md-content svg{max-width:none!important}';" +
        "(document.head||document.documentElement).appendChild(s);}" +
        "var inner=document.querySelector('.md-content__inner');if(!inner)return;" +
        "inner.querySelectorAll('form').forEach(function(f){var b=f.closest('section')||f;b.style.setProperty('display','none','important');});" +
        "inner.querySelectorAll('section,p').forEach(function(e){var x=e.textContent||'';if(x.indexOf('说明')>=0&&x.indexOf('支持汉字')>=0){var b=e.closest('section')||e;b.style.setProperty('display','none','important');}});" +
        "var h1=inner.querySelector('h1');if(h1)h1.style.setProperty('display','none','important');" +
        "var g=inner.querySelector('canvas,svg,img[src*=wbcx],img[src*=wb98],img[src*=png],img[src*=gif],figure');" +
        "if(g){g.scrollIntoView({block:'center'});}else{var secs=inner.querySelectorAll('section');for(var i=0;i<secs.length;i++){if(getComputedStyle(secs[i]).display!=='none'){secs[i].scrollIntoView({block:'start'});break;}}}" +
        "var w=Math.max((document.documentElement.scrollWidth||0),(document.body||0).scrollWidth||0);if(w>window.innerWidth){window.scrollTo(w-window.innerWidth-20,window.scrollY);}" +
        "}catch(e){}}clean();[200,500,1000,2000,3500,5000].forEach(function(d){setTimeout(clean,d);});})();";

    private const string ScrollJs =
        "(function(){function go(){try{var inner=document.querySelector('.md-content__inner');" +
        "if(!inner){window.scrollTo(0,0);return;}" +
        "var g=inner.querySelector('canvas,svg,img[src*=wbcx],img[src*=wb98],img[src*=png],img[src*=gif],figure');" +
        "if(g){g.scrollIntoView({block:'center'});}else{inner.scrollIntoView({block:'start'});}" +
        "var w=Math.max((document.documentElement.scrollWidth||0),(document.body||0).scrollWidth||0);if(w>window.innerWidth){window.scrollTo(w-window.innerWidth-20,window.scrollY);}" +
        "}catch(e){}}go();[200,600,1200,2200,3500,5000].forEach(function(d){setTimeout(go,d);});})();";

    internal MainWindow(Settings settings, LookupSources sources)
    {
        _settings = settings;
        _sources = sources;
        InitializeComponent();

        _hook.EscapePressed += () => Dispatcher.BeginInvoke(new Action(HidePopup));
        _hook.AltReleased += () => Dispatcher.BeginInvoke(new Action(OnAltReleased));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        ex |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, ex);
    }

    /// <summary>第一眼：只显示本地编码，秒出，不加载网页。</summary>
    internal void ShowCompact(WubiLookupResult res, Rect caretPx, string method)
    {
        _currentChar = res.Char;
        _caretPx = caretPx;
        _expanded = false;

        CharText.Text = res.Char;
        SourceLabel.Text = string.IsNullOrEmpty(method) ? "" : $"取字: {method}";
        BuildChips(res);
        BuildMnemonics(res);
        BuildCodes(res);

        WebHost.Visibility = Visibility.Collapsed;
        FooterText.Text = _settings.Web.Enabled
            ? "再按 Alt+Z 看拆字图 · 松开 Alt 关闭"
            : "松开 Alt 关闭";

        ApplySizeAndShow(_settings.Popup.CompactWidthDip, _settings.Popup.CompactHeightDip);
        _hook.Install();
    }

    /// <summary>第二次按键：展开大窗，加载拆字图，保持显示直到 Esc。</summary>
    internal void Expand()
    {
        if (_expanded || !_settings.Web.Enabled || string.IsNullOrEmpty(_currentChar)) return;
        _expanded = true;

        WebHost.Visibility = Visibility.Visible;
        WebHint.Visibility = Visibility.Visible;
        WebHint.Text = "拆字图加载中…";
        FooterText.Text = "Esc 关闭";

        ApplySizeAndShow(_settings.Popup.ExpandedWidthDip, _settings.Popup.ExpandedHeightDip);
        _ = LoadWebAsync(_currentChar);
    }

    private void OnAltReleased()
    {
        // 紧凑预览：松开 Alt 即消失；已展开：留在屏幕上给你看图，按 Esc 关。
        if (!_expanded) HidePopup();
    }

    internal void HidePopup()
    {
        _hook.Uninstall();
        try { if (_wvReady) Web.CoreWebView2?.Stop(); } catch { /* ignore */ }
        _expanded = false;
        WebHost.Visibility = Visibility.Collapsed;
        Hide();
    }

    private void ApplySizeAndShow(double widthDip, double heightDip)
    {
        Width = widthDip;
        Height = heightDip;
        if (!IsVisible) Show(); // ShowActivated=False -> 不抢焦点
        var (x, y) = PopupPositioner.Place(_caretPx, widthDip, heightDip, _settings.Popup.OffsetBelowPx);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, x, y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private async Task LoadWebAsync(string ch)
    {
        try
        {
            await EnsureWebAsync();
            if (_settings.Web.ZoomFactor > 0)
                Web.ZoomFactor = _settings.Web.ZoomFactor;
            Web.CoreWebView2.Navigate(_sources.PrimaryUrl(ch));
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Web] {ex.Message}");
            WebHint.Visibility = Visibility.Visible;
            WebHint.Text = "拆字图加载失败";
        }
    }

    private Task EnsureWebAsync()
    {
        if (_wvReady) return Task.CompletedTask;
        return _wvInit ??= InitWebAsync();
    }

    private async Task InitWebAsync()
    {
        CoreWebView2Environment env;
        var proxy = _settings.Web.Proxy?.Trim();
        if (!string.IsNullOrEmpty(proxy))
        {
            var opts = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = $"--proxy-server={proxy}",
            };
            env = await CoreWebView2Environment.CreateAsync(null, AppPaths.WebView2DataDir, opts);
            AppPaths.Log($"[Web] using proxy {proxy}");
        }
        else
        {
            env = await CoreWebView2Environment.CreateAsync(null, AppPaths.WebView2DataDir);
        }
        await Web.EnsureCoreWebView2Async(env);
        Web.DefaultBackgroundColor = System.Drawing.Color.White;
        Web.CoreWebView2.NavigationCompleted += async (_, args) =>
        {
            if (args.IsSuccess)
            {
                WebHint.Visibility = Visibility.Collapsed;
                try { await Web.CoreWebView2.ExecuteScriptAsync(_settings.Web.HideChrome ? CleanupJs : ScrollJs); }
                catch { /* ignore */ }
            }
            else
            {
                WebHint.Visibility = Visibility.Visible;
                WebHint.Text = "拆字图加载失败（联网后重试）";
            }
        };
        _wvReady = true;
    }

    private void BuildChips(WubiLookupResult res)
    {
        KeyChips.Children.Clear();
        if (!res.Found) return;

        foreach (var key in res.FullCode)
        {
            char up = char.ToUpperInvariant(key);
            var zone = WubiZones.Of(key);
            var border = new Border
            {
                Background = new SolidColorBrush(WubiZones.ColorOf(zone)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = $"{up} 键 · {WubiZones.Label(zone)}区",
                Child = new TextBlock
                {
                    Text = up.ToString(),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                },
            };
            KeyChips.Children.Add(border);
        }
    }

    private void BuildMnemonics(WubiLookupResult res)
    {
        MnemonicText.Inlines.Clear();
        if (!res.Found) return;

        var seen = new HashSet<char>();
        foreach (var key in res.FullCode)
        {
            char up = char.ToUpperInvariant(key);
            if (!seen.Add(up)) continue;
            var mnem = KeyMnemonics.Get(up);
            if (mnem is null) continue;

            if (MnemonicText.Inlines.Count > 0)
                MnemonicText.Inlines.Add(new System.Windows.Documents.Run("  "));

            var zone = WubiZones.Of(key);
            MnemonicText.Inlines.Add(new System.Windows.Documents.Run(up + "·")
            {
                Foreground = new SolidColorBrush(WubiZones.ColorOf(zone)),
                FontWeight = FontWeights.Bold,
            });
            MnemonicText.Inlines.Add(new System.Windows.Documents.Run(mnem)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xBB)),
            });
        }
    }

    private void BuildCodes(WubiLookupResult res)
    {
        CodePanel.Children.Clear();

        var labelBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x91, 0xA0));
        var jianBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
        var fullBrush = new SolidColorBrush(Color.FromRgb(0x7F, 0xD1, 0xFF));

        if (!res.Found)
        {
            var tb = new TextBlock { FontSize = 13 };
            tb.Inlines.Add(new System.Windows.Documents.Run("本地词库未收录")
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x8A, 0x8A)),
            });
            CodePanel.Children.Add(tb);
            return;
        }

        void Add(string label, string code, SolidColorBrush valueBrush)
        {
            var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            tb.Inlines.Add(new System.Windows.Documents.Run(label + " ")
            {
                Foreground = labelBrush,
                FontSize = 12.5,
            });
            tb.Inlines.Add(new System.Windows.Documents.Run(code.ToUpperInvariant())
            {
                Foreground = valueBrush,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
            });
            CodePanel.Children.Add(tb);
        }

        if (res.Jian1 is not null) Add("一简", res.Jian1, jianBrush);
        if (res.Jian2 is not null) Add("二简", res.Jian2, jianBrush);
        if (res.Jian3 is not null) Add("三简", res.Jian3, jianBrush);
        Add("全码", res.FullCode, fullBrush);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _hook.Dispose();
        base.OnClosing(e);
    }
}
