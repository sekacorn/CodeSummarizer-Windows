using System.Net;

namespace CodeSummarizer.Windows.Services;

public static class SecurityPolicy
{
#if GOVERNMENT_MODE
    public const bool IsRestrictedBuild = true;
#else
    public const bool IsRestrictedBuild = false;
#endif

    public const int MaximumInputCharacters = 100_000;
    public const int MaximumModelResponseBytes = 5 * 1024 * 1024;
    public const int MaximumModelListBytes = 1024 * 1024;
    public const int MaximumErrorDetailCharacters = 2_000;
    public static readonly Uri OllamaEndpoint = new("http://127.0.0.1:11434/");

    public static string BuildLabel => IsRestrictedBuild ? "RESTRICTED BUILD" : "WINDOWS NATIVE";

    public static void ValidateOllamaEndpoint()
    {
        if (!OllamaEndpoint.IsLoopback || OllamaEndpoint.HostNameType != UriHostNameType.IPv4 ||
            !IPAddress.TryParse(OllamaEndpoint.Host, out var address) || !IPAddress.IsLoopback(address) ||
            OllamaEndpoint.Scheme != Uri.UriSchemeHttp || OllamaEndpoint.Port != 11434)
        {
            throw new InvalidOperationException("The compiled Ollama endpoint violates the localhost-only security policy.");
        }
    }
}
