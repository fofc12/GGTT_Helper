using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ZeroWubiLens;

/// <summary>
/// 注册系统级全局热键（默认 Alt+Z）。使用 message-only 窗口接收 WM_HOTKEY，
/// 不创建可见窗口、不抢焦点。
/// </summary>
internal sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0xB011;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private HwndSource? _source;
    private bool _registered;

    public event Action? Pressed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public bool Register(HotkeySettings hk)
    {
        var p = new HwndSourceParameters("ZeroWubiLensHotkey")
        {
            Width = 0,
            Height = 0,
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0,
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);

        uint mods = MOD_NOREPEAT;
        if (hk.Alt) mods |= MOD_ALT;
        if (hk.Ctrl) mods |= MOD_CONTROL;
        if (hk.Shift) mods |= MOD_SHIFT;
        if (hk.Win) mods |= MOD_WIN;

        uint vk = KeyToVk(hk.Key);

        _registered = RegisterHotKey(_source.Handle, HOTKEY_ID, mods, vk);
        if (!_registered)
            AppPaths.Log($"[Hotkey] RegisterHotKey failed (err={Marshal.GetLastWin32Error()}). 可能与其它程序冲突。");
        return _registered;
    }

    private static uint KeyToVk(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0x5A; // Z
        key = key.Trim();
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                return c;
            if (c == '/') return 0xBF; // VK_OEM_2
        }
        return char.ToUpperInvariant(key[0]);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            if (_registered)
                UnregisterHotKey(_source.Handle, HOTKEY_ID);
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
