using System.IO;

namespace ZeroWubiLens;

internal static class AppPaths
{
    public static string BaseDir { get; } =
        AppContext.BaseDirectory;

    public static string SettingsFile { get; } =
        Path.Combine(BaseDir, "settings.json");

    public static string CacheDir { get; } =
        Path.Combine(BaseDir, "Cache");

    public static string RecentCacheFile { get; } =
        Path.Combine(CacheDir, "recent.json");

    public static string WebView2DataDir { get; } =
        Path.Combine(CacheDir, "WebView2");

    public static string LogFile { get; } =
        Path.Combine(CacheDir, "log.txt");

    public static void EnsureCache()
    {
        Directory.CreateDirectory(CacheDir);
    }

    public static void Log(string message)
    {
        try
        {
            EnsureCache();
            File.AppendAllText(LogFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // logging must never throw
        }
    }
}
