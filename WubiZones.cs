using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ZeroWubiLens;

/// <summary>
/// 五笔 86 键位分区。每个字母键属于一个笔画区，用于给字根键上色。
/// 横区 GFDSA / 竖区 HJKLM / 撇区 TREWQ / 捺区 YUIOP / 折区 NBVCX。
/// Z 为反查键，不参与编码。
/// </summary>
internal static class WubiZones
{
    public enum Zone { Heng, Shu, Pie, Na, Zhe, None }

    private static readonly Dictionary<char, Zone> Map = BuildMap();

    private static Dictionary<char, Zone> BuildMap()
    {
        var m = new Dictionary<char, Zone>();
        void Add(string keys, Zone z)
        {
            foreach (var k in keys) m[k] = z;
        }
        Add("gfdsa", Zone.Heng); // 横
        Add("hjklm", Zone.Shu);  // 竖
        Add("trewq", Zone.Pie);  // 撇
        Add("yuiop", Zone.Na);   // 捺
        Add("nbvcx", Zone.Zhe);  // 折
        return m;
    }

    public static Zone Of(char key) =>
        Map.TryGetValue(char.ToLowerInvariant(key), out var z) ? z : Zone.None;

    public static string Label(Zone z) => z switch
    {
        Zone.Heng => "横",
        Zone.Shu => "竖",
        Zone.Pie => "撇",
        Zone.Na => "捺",
        Zone.Zhe => "折",
        _ => "",
    };

    public static Color ColorOf(Zone z) => z switch
    {
        Zone.Heng => Color.FromRgb(0x2E, 0x7D, 0x32), // green
        Zone.Shu => Color.FromRgb(0x15, 0x65, 0xC0),  // blue
        Zone.Pie => Color.FromRgb(0xC6, 0x28, 0x28),  // red
        Zone.Na => Color.FromRgb(0xEF, 0x6C, 0x00),   // orange
        Zone.Zhe => Color.FromRgb(0x6A, 0x1B, 0x9A),  // purple
        _ => Color.FromRgb(0x60, 0x60, 0x60),         // grey
    };

    public static SolidColorBrush BrushOf(char key) =>
        new(ColorOf(Of(key)));
}
