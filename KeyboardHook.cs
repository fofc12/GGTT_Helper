using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZeroWubiLens;

/// <summary>
/// 低级键盘钩子，仅在悬浮窗显示期间挂载：
///   - 按下 Esc -> 关闭悬浮窗（并吞掉该 Esc，避免影响编辑器）；
///   - 松开 Alt -> 关闭悬浮窗（“松开快捷键即消失”）。
/// 钩子在挂载它的线程（UI 线程）上回调，事件可直接操作界面。
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_ESCAPE = 0x1B;
    private const int VK_MENU = 0x12;   // Alt
    private const int VK_LMENU = 0xA4;
    private const int VK_RMENU = 0xA5;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    /// <summary>按下 Esc。</summary>
    public event Action? EscapePressed;

    /// <summary>松开 Alt。</summary>
    public event Action? AltReleased;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public bool IsInstalled => _hookId != IntPtr.Zero;

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;
        using var proc = Process.GetCurrentProcess();
        using var module = proc.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
            GetModuleHandle(module.ModuleName), 0);
        if (_hookId == IntPtr.Zero)
            AppPaths.Log($"[Hook] install failed err={Marshal.GetLastWin32Error()}");
    }

    public void Uninstall()
    {
        if (_hookId == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg = wParam.ToInt32();
            int vk = (int)data.vkCode;

            if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && vk == VK_ESCAPE)
            {
                EscapePressed?.Invoke();
                return new IntPtr(1); // swallow Esc so editor is untouched
            }

            if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) &&
                (vk == VK_MENU || vk == VK_LMENU || vk == VK_RMENU))
            {
                AltReleased?.Invoke();
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
