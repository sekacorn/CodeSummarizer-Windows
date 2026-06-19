using System.Text;
using System.Text.RegularExpressions;
using CodeSummarizer.Windows.Models;

namespace CodeSummarizer.Windows.Services;

public sealed partial class SecretScanner
{
    public static IReadOnlyList<SecretFinding> Scan(string code)
    {
        var findings = new List<SecretFinding>();
        AddMatches(findings, code, AwsKeyRegex(), "AWS Access Key", _ => "AKIA***************");
        AddMatches(findings, code, JwtRegex(), "JWT Token", _ => "eyJ…[masked]");
        AddCapturedMatches(findings, code, CredentialRegex(), "Credential assignment", 2,
            m => $"{m.Groups[1].Value}=***");
        AddMatches(findings, code, PemRegex(), "PEM Private Key", _ => "-----BEGIN PRIVATE KEY-----");
        AddCapturedMatches(findings, code, BearerRegex(), "Bearer Token", 1,
            _ => "Authorization: Bearer ***");
        AddMatches(findings, code, GitHubTokenRegex(), "GitHub Token", _ => "gh*_***");
        AddMatches(findings, code, GoogleApiKeyRegex(), "Google API Key", _ => "AIza***");
        AddMatches(findings, code, OpenAiStyleKeyRegex(), "API Key", _ => "sk-***");
        AddCapturedMatches(findings, code, AzureStorageKeyRegex(), "Azure Storage Account Key", 1,
            _ => "AccountKey=***");

        return findings
            .OrderBy(f => f.Start)
            .ThenByDescending(f => f.Length)
            .ToArray();
    }

    public static string Redact(string code, IReadOnlyList<SecretFinding> findings)
    {
        if (findings.Count == 0)
            return code;

        var ranges = findings
            .Select(f => (Start: f.Start, End: f.Start + f.Length))
            .OrderBy(r => r.Start)
            .ToList();
        var merged = new List<(int Start, int End)>();

        foreach (var range in ranges)
        {
            if (merged.Count == 0 || range.Start > merged[^1].End)
                merged.Add(range);
            else
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, range.End));
        }

        var result = new StringBuilder(code);
        foreach (var range in merged.OrderByDescending(r => r.Start))
        {
            result.Remove(range.Start, range.End - range.Start);
            result.Insert(range.Start, "***REDACTED***");
        }

        return result.ToString();
    }

    private static void AddMatches(List<SecretFinding> findings, string code, Regex regex,
        string kind, Func<Match, string> preview)
    {
        foreach (Match match in regex.Matches(code))
            findings.Add(new(kind, match.Index, match.Length, preview(match)));
    }

    private static void AddCapturedMatches(List<SecretFinding> findings, string code, Regex regex,
        string kind, int groupIndex, Func<Match, string> preview)
    {
        foreach (Match match in regex.Matches(code))
        {
            var value = match.Groups[groupIndex];
            findings.Add(new(kind, value.Index, value.Length, preview(match)));
        }
    }

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)]
    private static partial Regex AwsKeyRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtRegex();

    [GeneratedRegex("(?i)(password|passwd|secret|client_secret|api_key|apikey|access_token|refresh_token|private_key|connection_string|sas_token)\\s*[=:]\\s*[\"']([^\"']{8,})[\"']", RegexOptions.Compiled)]
    private static partial Regex CredentialRegex();

    [GeneratedRegex(@"-----BEGIN (?:RSA |EC )?PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC )?PRIVATE KEY-----", RegexOptions.Compiled)]
    private static partial Regex PemRegex();

    [GeneratedRegex(@"(?i)authorization:\s*bearer\s+([a-zA-Z0-9_\-.]{20,})", RegexOptions.Compiled)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"gh[pousr]_[A-Za-z0-9]{36,255}", RegexOptions.Compiled)]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex(@"AIza[0-9A-Za-z\-_]{35}", RegexOptions.Compiled)]
    private static partial Regex GoogleApiKeyRegex();

    [GeneratedRegex(@"sk-[A-Za-z0-9_-]{20,}", RegexOptions.Compiled)]
    private static partial Regex OpenAiStyleKeyRegex();

    [GeneratedRegex(@"(?i)AccountKey=([A-Za-z0-9+/]{40,}={0,2})", RegexOptions.Compiled)]
    private static partial Regex AzureStorageKeyRegex();
}
