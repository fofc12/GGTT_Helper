using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ZeroWubiLens;

internal sealed class VocabularyEntry
{
    public string Code { get; set; } = "";
    public string Text { get; set; } = "";
    public string State { get; set; } = "normal";
    public string Level { get; set; } = "";
    public string Weight { get; set; } = "";
    public string Source { get; set; } = "";
    public int Ordinal { get; set; }
    public string DisplayState => State switch
    {
        "pinned" => "置顶",
        "promoted" => "确认首位",
        "pending" => "候选4",
        "rejected" => "屏蔽",
        _ => "普通",
    };
    public string DisplayLevel => Level switch
    {
        "base" => "L0 基础",
        "modern" => "L1 通用",
        "focus" => "L2 关注领域",
        "personal" => "L3 个人",
        "state" => "状态表",
        _ => Level,
    };
}

internal sealed class VocabularyStore
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private readonly string _repoPath;

    public VocabularyStore(string repoPath)
    {
        _repoPath = repoPath;
    }

    public string SourceDir => Path.Combine(_repoPath, "integrations", "rime", "source");
    private string DictDir => Path.Combine(SourceDir, "dict");
    private string StatePath => Path.Combine(SourceDir, "state", "zero_wubi86_lua_state.tsv");

    public IReadOnlyList<VocabularyEntry> Lookup(string query)
    {
        var entries = LoadEntries();
        query = query.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return entries.Take(120).ToList();

        if (IsCode(query))
            return Sort(entries.Where(item => item.Code == query));

        var exactCodes = entries
            .Where(item => item.Text == query)
            .Select(item => item.Code)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (exactCodes.Count > 0)
            return Sort(entries.Where(item => exactCodes.Contains(item.Code)));

        return Sort(entries.Where(item => item.Text.Contains(query, StringComparison.OrdinalIgnoreCase))).Take(120).ToList();
    }

    public void AddEntry(string text, string code, string level, int weight)
    {
        ValidateTextCode(text, code);
        var path = DictPath(level);
        if (!File.Exists(path))
            throw new FileNotFoundException("词库文件不存在", path);

        var exists = ReadDict(path, level).Any(item => item.Text == text && item.Code == code);
        if (exists)
            return;

        File.AppendAllText(path, $"{text}\t{code}\t{weight}{Environment.NewLine}", Utf8NoBom);
    }

    public void SetState(string text, string code, string state)
    {
        ValidateTextCode(text, code);
        if (state is not ("normal" or "pending" or "promoted" or "pinned" or "rejected"))
            throw new InvalidOperationException($"未知状态：{state}");

        var rows = ReadStateRows()
            .Where(item => !(item.Code == code && item.Text == text))
            .ToList();
        if (state != "normal")
            rows.Add(new VocabularyEntry { Code = code, Text = text, State = state });
        SaveStateRows(rows);
    }

    public string InstallAndDeploy()
    {
        var scriptDir = Path.Combine(_repoPath, "integrations", "rime", "scripts");
        var install = Path.Combine(scriptDir, "install.ps1");
        var deploy = Path.Combine(scriptDir, "deploy.ps1");
        if (!File.Exists(install) || !File.Exists(deploy))
            throw new FileNotFoundException("没有找到 Rime 安装/部署脚本", scriptDir);

        var output = new StringBuilder();
        output.AppendLine(RunPowerShell(install, "-IncludeState"));
        output.AppendLine(RunPowerShell(deploy, ""));
        return output.ToString();
    }

    private List<VocabularyEntry> LoadEntries()
    {
        if (!Directory.Exists(SourceDir))
            throw new DirectoryNotFoundException($"零西源目录不存在：{SourceDir}");

        var entries = new List<VocabularyEntry>();
        foreach (var level in new[] { "base", "modern", "focus", "personal" })
        {
            var path = DictPath(level);
            if (File.Exists(path))
                entries.AddRange(ReadDict(path, level));
        }

        var states = ReadStateRows()
            .GroupBy(item => $"{item.Code}\t{item.Text}")
            .ToDictionary(group => group.Key, group => group.Last().State);

        foreach (var entry in entries)
        {
            if (states.TryGetValue($"{entry.Code}\t{entry.Text}", out var state))
                entry.State = state;
        }

        foreach (var stateEntry in ReadStateRows())
        {
            if (entries.Any(item => item.Code == stateEntry.Code && item.Text == stateEntry.Text))
                continue;
            stateEntry.Level = "state";
            stateEntry.Source = Path.GetFileName(StatePath);
            entries.Add(stateEntry);
        }

        return entries;
    }

    private IEnumerable<VocabularyEntry> ReadDict(string path, string level)
    {
        var inBody = false;
        var ordinal = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            if (!inBody)
            {
                if (line.Trim() == "...")
                    inBody = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < 2)
                continue;

            yield return new VocabularyEntry
            {
                Text = parts[0],
                Code = parts[1],
                Weight = parts.Length >= 3 ? parts[2] : "",
                Level = level,
                Source = Path.GetFileName(path),
                Ordinal = ordinal++,
            };
        }
    }

    private List<VocabularyEntry> ReadStateRows()
    {
        var rows = new List<VocabularyEntry>();
        if (!File.Exists(StatePath))
            return rows;
        foreach (var line in File.ReadLines(StatePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                continue;
            var parts = line.Split('\t');
            if (parts.Length < 3)
                continue;
            rows.Add(new VocabularyEntry { Code = parts[0], Text = parts[1], State = parts[2] });
        }
        return rows;
    }

    private void SaveStateRows(IEnumerable<VocabularyEntry> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        var lines = rows
            .Where(item => !string.IsNullOrWhiteSpace(item.Code) && !string.IsNullOrWhiteSpace(item.Text))
            .Where(item => item.State != "normal")
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Text, StringComparer.Ordinal)
            .Select(item => $"{item.Code}\t{item.Text}\t{item.State}")
            .Distinct()
            .ToArray();
        File.WriteAllLines(StatePath, lines, Utf8NoBom);
    }

    private string DictPath(string level)
    {
        var file = level switch
        {
            "base" => "zero_wubi86_base.dict.yaml",
            "modern" => "zero_wubi86_modern.dict.yaml",
            "personal" => "zero_wubi86_user.dict.yaml",
            "focus" => "zero_wubi86_domain_focus.dict.yaml",
            _ => throw new InvalidOperationException($"未知词库层级：{level}"),
        };
        return Path.Combine(DictDir, file);
    }

    private static IReadOnlyList<VocabularyEntry> Sort(IEnumerable<VocabularyEntry> entries) =>
        entries
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => StateRank(item.State))
            .ThenBy(item => item.Ordinal)
            .ToList();

    private static int StateRank(string state) => state switch
    {
        "pinned" => 0,
        "promoted" => 1,
        "normal" => 2,
        "pending" => 3,
        "rejected" => 9,
        _ => 4,
    };

    private static bool IsCode(string value) =>
        value.Length > 0 && value.All(ch => (ch >= 'a' && ch <= 'z') || ch == ';');

    private static void ValidateTextCode(string text, string code)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("词语不能为空");
        if (!IsCode(code))
            throw new InvalidOperationException("编码必须是小写五笔码");
    }

    private static string RunPowerShell(string script, string arguments)
    {
        var exe = File.Exists(@"X:\Powershell7\pwsh.exe")
            ? @"X:\Powershell7\pwsh.exe"
            : "powershell.exe";
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 PowerShell");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(output + Environment.NewLine + error);
        return output + error;
    }
}
