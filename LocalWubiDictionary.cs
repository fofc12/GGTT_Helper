using System.IO;
using System.Text;

namespace ZeroWubiLens;

internal sealed class WubiLookupResult
{
    public required string Char { get; init; }
    public required string FullCode { get; init; }
    public required IReadOnlyList<string> SimpleCodes { get; init; }
    public required IReadOnlyList<string> AllCodes { get; init; }
    public string? Jian1 { get; init; }
    public string? Jian2 { get; init; }
    public string? Jian3 { get; init; }
    public bool Found => AllCodes.Count > 0;
}

/// <summary>
/// 解析 Rime 词典（zero_wubi86_base.dict.yaml），格式为：
///   文本\t编码\t权重[\t字根]
/// 注释行以 # 开头；YAML 头部由 --- / ... 包围，且头部行不含制表符。
/// 整库一次性载入内存（约 1MB，万级条目）。
/// </summary>
internal sealed class LocalWubiDictionary
{
    private readonly Dictionary<string, List<(string code, long weight)>> _map = new();
    public bool Loaded { get; private set; }
    public string? Error { get; private set; }
    public int EntryCount { get; private set; }
    public string? Path { get; private set; }

    public static LocalWubiDictionary LoadFrom(string path)
    {
        var dict = new LocalWubiDictionary { Path = path };
        try
        {
            if (!File.Exists(path))
            {
                dict.Error = $"词库文件不存在：{path}";
                AppPaths.Log($"[Dict] {dict.Error}");
                return dict;
            }

            using var reader = new StreamReader(path, Encoding.UTF8, true);
            string? line;
            int count = 0;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;

                int tab = line.IndexOf('\t');
                if (tab <= 0) continue; // header / metadata lines have no tab

                var text = line.Substring(0, tab);
                var rest = line.Substring(tab + 1);

                int tab2 = rest.IndexOf('\t');
                string code = tab2 < 0 ? rest : rest.Substring(0, tab2);
                long weight = 1;
                if (tab2 >= 0)
                {
                    var weightPart = rest.Substring(tab2 + 1);
                    int tab3 = weightPart.IndexOf('\t');
                    if (tab3 >= 0) weightPart = weightPart.Substring(0, tab3);
                    long.TryParse(weightPart.Trim(), out weight);
                }

                code = code.Trim();
                if (code.Length == 0 || !IsCode(code)) continue;

                if (!dict._map.TryGetValue(text, out var list))
                {
                    list = new List<(string, long)>();
                    dict._map[text] = list;
                }
                list.Add((code, weight));
                count++;
            }

            dict.EntryCount = count;
            dict.Loaded = true;
            AppPaths.Log($"[Dict] loaded {count} entries from {path}");
        }
        catch (Exception ex)
        {
            dict.Error = ex.Message;
            AppPaths.Log($"[Dict] load failed: {ex}");
        }
        return dict;
    }

    private static bool IsCode(string s)
    {
        foreach (var c in s)
            if (!((c >= 'a' && c <= 'z') || c == ';'))
                return false;
        return true;
    }

    public WubiLookupResult Lookup(string ch)
    {
        if (_map.TryGetValue(ch, out var list) && list.Count > 0)
        {
            // distinct codes, longest = 全码
            var distinct = list
                .GroupBy(x => x.code)
                .Select(g => (code: g.Key, weight: g.Max(x => x.weight)))
                .ToList();

            int maxLen = distinct.Max(x => x.code.Length);
            var full = distinct
                .Where(x => x.code.Length == maxLen)
                .OrderByDescending(x => x.weight)
                .First().code;

            var simples = distinct
                .Where(x => x.code.Length < maxLen)
                .OrderBy(x => x.code.Length)
                .ThenByDescending(x => x.weight)
                .Select(x => x.code)
                .ToList();

            var all = distinct
                .OrderBy(x => x.code.Length)
                .ThenByDescending(x => x.weight)
                .Select(x => x.code)
                .ToList();

            return new WubiLookupResult
            {
                Char = ch,
                FullCode = full,
                SimpleCodes = simples,
                AllCodes = all,
                Jian1 = simples.FirstOrDefault(c => c.Length == 1),
                Jian2 = simples.FirstOrDefault(c => c.Length == 2),
                Jian3 = simples.FirstOrDefault(c => c.Length == 3),
            };
        }

        return new WubiLookupResult
        {
            Char = ch,
            FullCode = "",
            SimpleCodes = Array.Empty<string>(),
            AllCodes = Array.Empty<string>(),
        };
    }
}
