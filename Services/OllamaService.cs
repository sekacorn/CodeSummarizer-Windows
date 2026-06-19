using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
using CodeSummarizer.Windows.Models;

namespace CodeSummarizer.Windows.Services;

public sealed class OllamaService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    public OllamaService()
    {
        SecurityPolicy.ValidateOllamaEndpoint();
        _httpClient = new HttpClient
        {
            BaseAddress = SecurityPolicy.OllamaEndpoint,
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/tags", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessAsync(response, null, cancellationToken);
            var payload = await ReadJsonAsync<ModelsPayload>(response.Content, SecurityPolicy.MaximumModelListBytes, cancellationToken);
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
        if (code.Length > SecurityPolicy.MaximumInputCharacters)
            throw new InvalidOperationException($"Code is limited to {SecurityPolicy.MaximumInputCharacters:N0} characters per analysis.");

        var findings = SecretScanner.Scan(code);
        var codeToSend = redact ? SecretScanner.Redact(code, findings) : code;
        var prompt = PromptBuilder.Build(language, mode, codeToSend);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/generate")
            {
                Content = JsonContent.Create(new { model, prompt, stream = false, format = "json" })
            };
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessAsync(response, model, cancellationToken);
            var payload = await ReadJsonAsync<GeneratePayload>(response.Content, SecurityPolicy.MaximumModelResponseBytes, cancellationToken)
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

        var detail = await ReadBoundedTextAsync(response.Content, SecurityPolicy.MaximumErrorDetailCharacters, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound && model is not null)
            throw new InvalidOperationException($"Model '{model}' was not found. Run: ollama pull {model}");
        throw new InvalidOperationException($"Ollama returned {(int)response.StatusCode}: {detail}");
    }

    public void Dispose() => _httpClient.Dispose();

    private static async Task<T?> ReadJsonAsync<T>(HttpContent content, int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long length && length > maximumBytes)
            throw new InvalidOperationException($"Ollama response exceeded the {maximumBytes:N0}-byte safety limit.");

        await using var input = await content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81_920];
        var total = 0;
        int bytesRead;
        while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += bytesRead;
            if (total > maximumBytes)
                throw new InvalidOperationException($"Ollama response exceeded the {maximumBytes:N0}-byte safety limit.");
            output.Write(buffer, 0, bytesRead);
        }

        return JsonSerializer.Deserialize<T>(output.ToArray(), JsonOptions);
    }

    private static async Task<string> ReadBoundedTextAsync(HttpContent content, int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var text = await content.ReadAsStringAsync(cancellationToken);
        return text.Length <= maximumCharacters ? text : text[..maximumCharacters] + " [truncated]";
    }

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
