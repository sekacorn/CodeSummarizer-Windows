using System.Text.Json.Serialization;

namespace CodeSummarizer.Windows.Models;

public sealed class CodeAnalysis
{
    [JsonPropertyName("is_valid")]
    public bool? IsValid { get; set; }

    [JsonPropertyName("syntax_errors")]
    public List<SyntaxIssue> SyntaxErrors { get; set; } = [];

    [JsonPropertyName("summary")]
    public List<string> Summary { get; set; } = [];

    [JsonPropertyName("walkthrough")]
    public List<string> Walkthrough { get; set; } = [];

    [JsonPropertyName("inputs")]
    public List<string> Inputs { get; set; } = [];

    [JsonPropertyName("outputs")]
    public List<string> Outputs { get; set; } = [];

    [JsonPropertyName("side_effects")]
    public List<string> SideEffects { get; set; } = [];

    [JsonPropertyName("risks")]
    public List<Risk> Risks { get; set; } = [];

    [JsonPropertyName("junior_explanation")]
    public string JuniorExplanation { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public sealed class Risk
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = "low";
}

public sealed class SyntaxIssue
{
    [JsonPropertyName("line")]
    public int? Line { get; set; }

    [JsonPropertyName("column")]
    public int? Column { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "error";
}
