using System.Net;

namespace ZeroWubiLens;

internal sealed class LookupSources
{
    private readonly WebSettings _web;

    public LookupSources(WebSettings web)
    {
        _web = web;
    }

    public bool WebEnabled => _web.Enabled;

    public string PrimaryUrl(string ch) =>
        Fill(_web.PrimaryTemplate, ch);

    public IEnumerable<(string Name, string Url)> FallbackUrls(string ch)
    {
        foreach (var f in _web.Fallbacks)
        {
            if (string.IsNullOrWhiteSpace(f.Template)) continue;
            yield return (f.Name, Fill(f.Template, ch));
        }
    }

    private static string Fill(string template, string ch)
    {
        var encoded = WebUtility.UrlEncode(ch) ?? ch;
        return template
            .Replace("{char}", encoded)
            .Replace("{CHAR}", encoded);
    }
}
