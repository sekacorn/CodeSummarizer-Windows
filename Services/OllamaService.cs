using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CodeSummarizer.Windows.Models;

namespace CodeSummarizer.Windows.Services;

public sealed class OllamaService : IDisposable
{
    private static readonly Uri BaseAddress = new("http://127.0.0.1:11434/");
    private readonly HttpClient _httpClient;
    private readonly SecretScanner _secretScanner;

    public OllamaService(SecretScanner secretScanner)
    {
        _secretScanner = secretScanner;
        _httpClient = new HttpClient
        {
            BaseAddress = BaseAddress,
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            await EnsureSuccessAsync(response, null, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<ModelsPayload>(cancellationToken: cancellationToken);
            var models = payload?.Models.Select(m => m.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray() ?? [];
            if (models.Length == 0)
                throw new InvalidOperationException("No Ollama models are installed. Run: ollama pull qwen2.5-coder:7b");
            return models;
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            throw new InvalidOperationException("Ollama is not running. Start Ollama and then refresh the model list.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Ollama did not respond in time.", ex);
        }
    }

    public async Task<AnalysisResponse> AnalyzeAsync(string language, string code, string model,
        string mode, bool redact, CancellationToken cancellationToken = default)
    {
        var findings = _secretScanner.Scan(code);
        var codeToSend = redact ? _secretScanner.Redact(code, findings) : code;
        var prompt = PromptBuilder.Build(language, mode, codeToSend);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/generate", new
            {
                model,
                prompt,
                stream = false,
                format = "json"
            }, cancellationToken);
            await EnsureSuccessAsync(response, model, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<GeneratePayload>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned an empty response.");
            return new(payload.Response, redact && findings.Count > 0, findings);
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            throw new InvalidOperationException("Ollama is not running. Start Ollama and try again.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Analysis timed out. Try a smaller model or a shorter code sample.", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string? model,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound && model is not null)
            throw new InvalidOperationException($"Model '{model}' was not found. Run: ollama pull {model}");
        throw new InvalidOperationException($"Ollama returned {(int)response.StatusCode}: {detail}");
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed class ModelsPayload
    {
        public List<ModelPayload> Models { get; set; } = [];
    }

    private sealed class ModelPayload
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class GeneratePayload
    {
        public string Response { get; set; } = string.Empty;
    }
}
