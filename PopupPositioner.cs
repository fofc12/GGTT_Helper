using System.Runtime.InteropServices;
using System.Windows;

namespace ZeroWubiLens;

/// <summary>
/// 根据光标的屏幕物理矩形，计算悬浮窗的物理像素位置：默认贴在光标下方，
/// 下方空间不足则翻到上方，并约束在所在显示器的工作区内（支持多显示器/高 DPI）。
/// </summary>
internal static class PopupPositioner
{
    public static (int X, int Y) Place(Rect caretPx, double widthDip, double heightDip, int offsetBelowPx)
    {
        var anchor = new NativeMethods.POINT((int)caretPx.Left, (int)caretPx.Top);
        IntPtr mon = NativeMethods.MonitorFromPoint(anchor, NativeMethods.MONITOR_DEFAULTTONEAREST);

        var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(mon, ref mi))
        {
            // crude fallback to primary screen metrics
            mi.rcWork = new NativeMethods.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        }

        double scale = 1.0;
        if (NativeMethods.GetDpiForMonitor(mon, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0)
            scale = dpiX / 96.0;

        int physW = (int)Math.Round(widthDip * scale);
        int physH = (int)Math.Round(heightDip * scale);
        var work = mi.rcWork;

        int x = (int)caretPx.Left;
        int y = (int)caretPx.Bottom + offsetBelowPx;

        // not enough room below -> flip above the caret
        if (y + physH > work.Bottom)
            y = (int)caretPx.Top - physH - offsetBelowPx;

        if (x + physW > work.Right) x = work.Right - physW;
        if (x < work.Left) x = work.Left;
        if (y + physH > work.Bottom) y = work.Bottom - physH;
        if (y < work.Top) y = work.Top;

        return (x, y);
    }
}
