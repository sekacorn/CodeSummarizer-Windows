using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CodeSummarizer.Windows.Models;
using CodeSummarizer.Windows.Services;

namespace CodeSummarizer.Windows.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly OllamaService _ollamaService;
    private string _code = string.Empty;
    private string _selectedLanguage = "C#";
    private string? _selectedModel;
    private bool _redactSecrets = true;
    private bool _sensitiveMode;
    private bool _isBusy;
    private string _statusText = "Start Ollama, then choose a model and paste code.";
    private string? _errorText;
    private string? _warningText;
    private string? _rawOutput;
    private double _confidence;
    private bool _hasResult;
    private bool _hasValidation;
    private string _validationText = string.Empty;
    private string _validationColor = "#155724";
    private CancellationTokenSource? _analysisCancellation;

    public MainViewModel()
    {
        _ollamaService = new OllamaService(new SecretScanner());
        RefreshModelsCommand = new AsyncRelayCommand(_ => RefreshModelsAsync(), _ => !IsBusy);
        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, p => CanAnalyze && p is string);
        ClearCommand = new RelayCommand(_ => Clear(), _ => !IsBusy);
        CancelCommand = new RelayCommand(_ => _analysisCancellation?.Cancel(), _ => IsBusy);
        CopySectionCommand = new RelayCommand(p => CopyText(p as string));
        CopyAllCommand = new RelayCommand(_ => CopyAll(), _ => HasResult);
    }

    public IReadOnlyList<string> Languages { get; } =
    [
        "Ada", "C#", "C", "C++", "Java", "Python", "JavaScript", "TypeScript", "SQL", "VBA",
        "JSON", "CSS", "DAX", "Assembly", "Fortran", "COBOL", "Bash", "PowerShell", "Rust", "Go"
    ];

    public ObservableCollection<string> Models { get; } = [];
    public ObservableCollection<AnalysisSection> Sections { get; } = [];

    public string Code
    {
        get => _code;
        set { if (SetProperty(ref _code, value)) NotifyCommandState(); }
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    public string? SelectedModel
    {
        get => _selectedModel;
        set { if (SetProperty(ref _selectedModel, value)) NotifyCommandState(); }
    }

    public bool RedactSecrets
    {
        get => _redactSecrets;
        set => SetProperty(ref _redactSecrets, SensitiveMode || value);
    }

    public bool SensitiveMode
    {
        get => _sensitiveMode;
        set
        {
            if (!SetProperty(ref _sensitiveMode, value)) return;
            if (value) RedactSecrets = true;
            OnPropertyChanged(nameof(CanToggleRedaction));
            OnPropertyChanged(nameof(ShowRawOutput));
            OnPropertyChanged(nameof(SensitiveModeMessage));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(BusyText));
            NotifyCommandState();
        }
    }

    public string BusyText => IsBusy ? "Analyzing locally…" : "Ready";
    public bool CanAnalyze => !IsBusy && !string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(SelectedModel);
    public bool CanToggleRedaction => !SensitiveMode;

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string? ErrorText { get => _errorText; private set { SetProperty(ref _errorText, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    public string? WarningText { get => _warningText; private set { SetProperty(ref _warningText, value); OnPropertyChanged(nameof(HasWarning)); } }
    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningText);
    public string SensitiveModeMessage => SensitiveMode ? "Sensitive Mode: redaction is enforced and raw model output is hidden." : string.Empty;
    public string? RawOutput { get => _rawOutput; private set { SetProperty(ref _rawOutput, value); OnPropertyChanged(nameof(ShowRawOutput)); } }
    public bool ShowRawOutput => !SensitiveMode && !string.IsNullOrWhiteSpace(RawOutput);
    public double Confidence { get => _confidence; private set { SetProperty(ref _confidence, value); OnPropertyChanged(nameof(ConfidenceText)); } }
    public string ConfidenceText => $"{Confidence:P0}";
    public bool HasResult
    {
        get => _hasResult;
        private set
        {
            if (!SetProperty(ref _hasResult, value)) return;
            OnPropertyChanged(nameof(HasNoResult));
            CopyAllCommand.NotifyCanExecuteChanged();
        }
    }
    public bool HasNoResult => !HasResult;
    public bool HasValidation { get => _hasValidation; private set => SetProperty(ref _hasValidation, value); }
    public string ValidationText { get => _validationText; private set => SetProperty(ref _validationText, value); }
    public string ValidationColor { get => _validationColor; private set => SetProperty(ref _validationColor, value); }

    public AsyncRelayCommand RefreshModelsCommand { get; }
    public AsyncRelayCommand AnalyzeCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand CopySectionCommand { get; }
    public RelayCommand CopyAllCommand { get; }

    public async Task RefreshModelsAsync()
    {
        ErrorText = null;
        StatusText = "Looking for local Ollama models…";
        try
        {
            var models = await _ollamaService.ListModelsAsync();
            Models.Clear();
            foreach (var model in models) Models.Add(model);
            if (SelectedModel is null || !Models.Contains(SelectedModel)) SelectedModel = Models.FirstOrDefault();
            StatusText = $"Connected to Ollama · {Models.Count} model{(Models.Count == 1 ? "" : "s")} available";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            StatusText = "Ollama is unavailable";
        }
    }

    private async Task AnalyzeAsync(object? parameter)
    {
        if (parameter is not string mode || !CanAnalyze) return;
        ErrorText = null;
        WarningText = null;
        HasResult = false;
        HasValidation = false;
        Sections.Clear();
        RawOutput = null;
        IsBusy = true;
        StatusText = $"Running {ModeName(mode)} with {SelectedModel}…";
        _analysisCancellation = new CancellationTokenSource();

        try
        {
            var response = await _ollamaService.AnalyzeAsync(SelectedLanguage, Code, SelectedModel!, mode,
                RedactSecrets, _analysisCancellation.Token);
            RawOutput = response.ModelOutput;
            if (response.Findings.Count > 0)
            {
                var action = response.Redacted ? "masked before analysis" : "detected but not masked";
                WarningText = $"{response.Findings.Count} potential secret(s) {action}: " +
                    string.Join(", ", response.Findings.Select(f => $"{f.Kind} ({f.Preview})"));
            }

            var analysis = AnalysisParser.Parse(response.ModelOutput);
            PopulateResult(analysis);
            StatusText = $"Analysis complete · {ModeName(mode)} · {SelectedModel}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Analysis cancelled";
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            StatusText = "Analysis failed";
        }
        finally
        {
            _analysisCancellation.Dispose();
            _analysisCancellation = null;
            IsBusy = false;
        }
    }

    private void PopulateResult(CodeAnalysis analysis)
    {
        HasValidation = analysis.IsValid.HasValue;
        if (analysis.IsValid.HasValue)
        {
            ValidationText = analysis.IsValid.Value ? "Valid — no blocking errors found" : "Invalid — errors detected";
            ValidationColor = analysis.IsValid.Value ? "#155724" : "#9B1C1C";
            if (analysis.SyntaxErrors.Count > 0)
                Sections.Add(new("Validation details", string.Join(Environment.NewLine, analysis.SyntaxErrors.Select(FormatIssue))));
        }

        AddListSection("Summary", analysis.Summary);
        AddListSection("Walkthrough", analysis.Walkthrough);
        AddListSection("Inputs", analysis.Inputs);
        AddListSection("Outputs", analysis.Outputs);
        AddListSection("Side effects", analysis.SideEffects);
        if (analysis.Risks.Count > 0)
            Sections.Add(new("Risks", string.Join(Environment.NewLine, analysis.Risks.Select(r => $"[{r.Level.ToUpperInvariant()}] {r.Item}"))));
        if (!string.IsNullOrWhiteSpace(analysis.JuniorExplanation))
            Sections.Add(new("Junior explanation", analysis.JuniorExplanation));

        Confidence = analysis.Confidence;
        HasResult = true;
    }

    private void AddListSection(string title, IReadOnlyCollection<string> values)
    {
        if (values.Count > 0) Sections.Add(new(title, string.Join(Environment.NewLine, values.Select(v => $"• {v}"))));
    }

    private static string FormatIssue(SyntaxIssue issue)
    {
        var location = issue.Line is null ? string.Empty : $" Line {issue.Line}{(issue.Column is null ? "" : $":{issue.Column}")}";
        return $"[{issue.Severity.ToUpperInvariant()}]{location} — {issue.Message}";
    }

    private static string ModeName(string mode) => mode switch
    {
        "junior" => "junior explanation",
        "risk" => "risk scan",
        "validate" => "validation",
        _ => "summary"
    };

    private void Clear()
    {
        Code = string.Empty;
        Sections.Clear();
        ErrorText = null;
        WarningText = null;
        RawOutput = null;
        HasResult = false;
        HasValidation = false;
        Confidence = 0;
        StatusText = "Cleared. Paste code to begin another analysis.";
    }

    private static void CopyText(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text)) Clipboard.SetText(text);
    }

    private void CopyAll()
    {
        var report = new StringBuilder();
        foreach (var section in Sections) report.AppendLine(section.Title).AppendLine(section.Content).AppendLine();
        report.AppendLine($"Confidence: {ConfidenceText}");
        CopyText(report.ToString());
        StatusText = "Analysis copied to clipboard";
    }

    private void NotifyCommandState()
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        RefreshModelsCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanAnalyze));
    }

    public void Dispose()
    {
        _analysisCancellation?.Cancel();
        _analysisCancellation?.Dispose();
        _ollamaService.Dispose();
    }
}
