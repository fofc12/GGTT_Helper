using System.IO;
using System.Text.Json;

namespace ZeroWubiLens;

internal sealed class RecentEntry
{
    public string Char { get; set; } = "";
    public string FullCode { get; set; } = "";
    public List<string> SimpleCodes { get; set; } = new();
    public DateTime LastSeen { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Lightweight LRU-ish cache of recently looked-up characters, persisted to
/// Cache/recent.json. Statistics are recorded silently (never prompts the user).
/// </summary>
internal sealed class RecentCache
{
    private readonly int _max;
    private readonly Dictionary<string, RecentEntry> _entries = new();
    private readonly object _lock = new();

    public RecentCache(int max)
    {
        _max = Math.Max(10, max);
        Load();
    }

    public void Record(WubiLookupResult result)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(result.Char, out var e))
            {
                e = new RecentEntry { Char = result.Char };
                _entries[result.Char] = e;
            }
            e.FullCode = result.FullCode;
            e.SimpleCodes = result.SimpleCodes.ToList();
            e.LastSeen = DateTime.Now;
            e.Count++;

            Trim();
            Save();
        }
    }

    public RecentEntry? Get(string ch)
    {
        lock (_lock)
            return _entries.TryGetValue(ch, out var e) ? e : null;
    }

    private void Trim()
    {
        if (_entries.Count <= _max) return;
        var drop = _entries.Values
            .OrderBy(e => e.LastSeen)
            .Take(_entries.Count - _max)
            .Select(e => e.Char)
            .ToList();
        foreach (var k in drop)
            _entries.Remove(k);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.RecentCacheFile)) return;
            var json = File.ReadAllText(AppPaths.RecentCacheFile);
            var list = JsonSerializer.Deserialize<List<RecentEntry>>(json);
            if (list is null) return;
            foreach (var e in list)
                if (!string.IsNullOrEmpty(e.Char))
                    _entries[e.Char] = e;
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[RecentCache] load failed: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            AppPaths.EnsureCache();
            var list = _entries.Values.OrderByDescending(e => e.LastSeen).ToList();
            var json = JsonSerializer.Serialize(list,
                new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(AppPaths.RecentCacheFile, json);
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[RecentCache] save failed: {ex.Message}");
        }
    }
}
