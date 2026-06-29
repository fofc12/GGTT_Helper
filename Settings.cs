using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroWubiLens;

internal sealed class HotkeySettings
{
    public bool Alt { get; set; } = true;
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public string Key { get; set; } = "Z";
}

internal sealed class PopupSettings
{
    public double CompactWidthDip { get; set; } = 330;
    public double CompactHeightDip { get; set; } = 168;
    public double ExpandedWidthDip { get; set; } = 960;
    public double ExpandedHeightDip { get; set; } = 660;
    public int OffsetBelowPx { get; set; } = 6;
}

internal sealed class WebSource
{
    public string Name { get; set; } = "";
    public string Template { get; set; } = "";
}

internal sealed class WebSettings
{
    public bool Enabled { get; set; } = true;
    public double ZoomFactor { get; set; } = 1.0;
    public bool HideChrome { get; set; } = true;
    public string Proxy { get; set; } = "";
    public string PrimaryTemplate { get; set; } = "https://hantang.github.io/search-wubi/?char={char}";
    public List<WebSource> Fallbacks { get; set; } = new();
}

internal sealed class Settings
{
    public string DictPath { get; set; } = "%APPDATA%\\Rime\\zero_wubi86_base.dict.yaml";
    public string ZeroXiRepoPath { get; set; } = "";
    public HotkeySettings Hotkey { get; set; } = new();
    public bool UseClipboardFallback { get; set; }
    public PopupSettings Popup { get; set; } = new();
    public WebSettings Web { get; set; } = new();
    public int RecentCacheSize { get; set; } = 200;

    [JsonIgnore]
    public string ResolvedDictPath
    {
        get
        {
            var p = Environment.ExpandEnvironmentVariables(DictPath);
            if (!System.IO.Path.IsPathRooted(p))
                p = System.IO.Path.Combine(AppPaths.BaseDir, p);
            if (System.IO.File.Exists(p))
                return p;

            foreach (var name in new[] { "zero_wubi86_base.dict.yaml", "bm.txt" })
            {
                var fallback = System.IO.Path.Combine(AppPaths.BaseDir, name);
                if (System.IO.File.Exists(fallback))
                {
                    AppPaths.Log($"[Dict] primary not found ({p}), using fallback: {fallback}");
                    return fallback;
                }
            }

            var missingFallback = System.IO.Path.Combine(AppPaths.BaseDir, "bm.txt");
            AppPaths.Log($"[Dict] primary not found ({p}), no bundled dictionary found.");
            return missingFallback;
        }
    }

    [JsonIgnore]
    public string ResolvedZeroXiRepoPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ZeroXiRepoPath))
                return FindZeroXiRepoPath(AppPaths.BaseDir) ?? AppPaths.BaseDir;

            var p = Environment.ExpandEnvironmentVariables(ZeroXiRepoPath);
            if (!System.IO.Path.IsPathRooted(p))
                p = System.IO.Path.Combine(AppPaths.BaseDir, p);
            return p;
        }
    }

    private static string? FindZeroXiRepoPath(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var rimeSource = System.IO.Path.Combine(dir.FullName, "integrations", "rime", "source");
            if (Directory.Exists(rimeSource))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var json = File.ReadAllText(AppPaths.SettingsFile);
                var s = JsonSerializer.Deserialize<Settings>(json, JsonOpts);
                if (s is not null)
                    return s;
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Settings] load failed: {ex.Message}");
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
            File.WriteAllText(AppPaths.SettingsFile, json);
        }
        catch (Exception ex)
        {
            AppPaths.Log($"[Settings] save failed: {ex.Message}");
        }
    }
}
