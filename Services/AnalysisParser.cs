using System.Text.Json;
using CodeSummarizer.Windows.Models;

namespace CodeSummarizer.Windows.Services;

public static class AnalysisParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static CodeAnalysis Parse(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
            throw new FormatException("The model returned no analysis.");

        var json = ExtractJson(modelOutput);
        CodeAnalysis analysis;
        try
        {
            analysis = JsonSerializer.Deserialize<CodeAnalysis>(json, Options)
                ?? throw new FormatException("The model returned an empty JSON object.");
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The model did not return valid structured JSON: {ex.Message}", ex);
        }

        analysis.Summary ??= [];
        analysis.Walkthrough ??= [];
        analysis.Inputs ??= [];
        analysis.Outputs ??= [];
        analysis.SideEffects ??= [];
        analysis.Risks ??= [];
        analysis.SyntaxErrors ??= [];
        analysis.Confidence = Math.Clamp(analysis.Confidence, 0, 1);
        return analysis;
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine >= 0 && lastFence > firstLine)
                trimmed = trimmed[(firstLine + 1)..lastFence].Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }
}
