using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace ZeroWubiLens;

internal sealed class CaptureResult
{
    public string Char { get; set; } = "";
    public Rect CaretPx { get; set; } = Rect.Empty;
    public string Method { get; set; } = "";
    public bool Ok => Char.Length > 0;
}

/// <summary>
/// 取得光标左侧的一个字符。首选 Windows UI Automation（不动剪贴板、不改正文）；
/// 失败且允许时，回退到“临时复制”方案。
/// </summary>
internal static class CharacterCapture
{
    public static CaptureResult Capture(bool allowClipboard)
    {
        var result = new CaptureResult();

        if (TryUia(out var ch, out var rect))
        {
            result.Char = ch;
            result.Method = "UIA";
            result.CaretPx = rect.IsEmpty ? GetCaretRectPx() : rect;
            return result;
        }

        if (allowClipboard && TryClipboard(out ch))
        {
            result.Char = ch;
            result.Method = "Clipboard";
            result.CaretPx = GetCaretRectPx();
            return result;
        }

        result.CaretPx = GetCaretRectPx();
        return result;
    }

    private static bool TryUia(out string ch, out Rect rect)
    {
        ch = "";
        rect = Rect.Empty;
        try
        {
            var fe = AutomationElement.FocusedElement;
            if (fe is null) return false;
            if (!fe.TryGetCurrentPattern(TextPattern.Pattern, out var patObj)) return false;

            var tp = (TextPattern)patObj;
            var sel = tp.GetSelection();
            if (sel is null || sel.Length == 0) return false;

            var caret = sel[0].Clone();
            // collapse to the active end (caret), then extend one char to the left
            caret.MoveEndpointByRange(TextPatternRangeEndpoint.Start, caret, TextPatternRangeEndpoint.End);
            int moved = caret.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
            if (moved == 0) return false;

            var text = (caret.GetText(8) ?? "").TrimEnd('\r', '\n', ' ', '\t');
            if (text.Length == 0) return false;

            ch = LastChar(text);
            if (ch.Length == 0) return false;

            var rects = caret.GetBoundingRectangles();
            if (rects is { Length: > 0 } && rects[0].Width > 0)
                rect = rects[0];

            return true;
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Capture] uia: {ex.Message}");
            return false;
        }
    }

    private static bool TryClipboard(out string ch)
    {
        ch = "";
        string? saved = null;
        try
        {
            try { if (System.Windows.Clipboard.ContainsText()) saved = System.Windows.Clipboard.GetText(); }
            catch { /* ignore */ }

            // Shift+Left to select the char to the left
            Key(VK_LSHIFT, false);
            Key(VK_LEFT, false);
            Key(VK_LEFT, true);
            Key(VK_LSHIFT, true);
            Thread.Sleep(35);

            // Ctrl+C
            Key(VK_CONTROL, false);
            Key(0x43, false); // C
            Key(0x43, true);
            Key(VK_CONTROL, true);
            Thread.Sleep(60);

            string got = "";
            try { if (System.Windows.Clipboard.ContainsText()) got = System.Windows.Clipboard.GetText(); }
            catch { /* ignore */ }

            // collapse selection back to the original caret position
            Key(VK_RIGHT, false);
            Key(VK_RIGHT, true);

            got = got.TrimEnd('\r', '\n', ' ', '\t');
            if (got.Length > 0) ch = LastChar(got);
            return ch.Length > 0;
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Capture] clip: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                if (saved is not null) System.Windows.Clipboard.SetText(saved);
            }
            catch { /* ignore */ }
        }
    }

    private static string LastChar(string s)
    {
        if (s.Length == 0) return "";
        if (s.Length >= 2 && char.IsLowSurrogate(s[^1]) && char.IsHighSurrogate(s[^2]))
            return s.Substring(s.Length - 2, 2);
        return s[^1].ToString();
    }

    public static Rect GetCaretRectPx()
    {
        try
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            uint tid = NativeMethods.GetWindowThreadProcessId(fg, IntPtr.Zero);
            var gti = new NativeMethods.GUITHREADINFO
            {
                cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
            };
            if (NativeMethods.GetGUIThreadInfo(tid, ref gti) && gti.hwndCaret != IntPtr.Zero)
            {
                var p = new NativeMethods.POINT(gti.rcCaret.Left, gti.rcCaret.Top);
                ClientToScreen(gti.hwndCaret, ref p);
                return new Rect(p.X, p.Y,
                    Math.Max(1, gti.rcCaret.Width), Math.Max(8, gti.rcCaret.Height));
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Capture] caret: {ex.Message}");
        }

        NativeMethods.GetCursorPos(out var cp);
        return new Rect(cp.X, cp.Y, 1, 18);
    }

    // ---- SendInput helpers ----
    private const int VK_LSHIFT = 0xA0;
    private const int VK_CONTROL = 0x11;
    private const int VK_LEFT = 0x25;
    private const int VK_RIGHT = 0x27;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
        public int padding1;
        public int padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private static void Key(int vk, bool up)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = (ushort)vk,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativeMethods.POINT lpPoint);
}
