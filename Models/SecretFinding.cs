namespace CodeSummarizer.Windows.Models;

public sealed record SecretFinding(string Kind, int Start, int Length, string Preview);

public sealed record AnalysisResponse(string ModelOutput, bool Redacted, IReadOnlyList<SecretFinding> Findings);

public sealed record AnalysisSection(string Title, string Content);
