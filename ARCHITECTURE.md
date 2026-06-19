# Security Architecture

## Components and trust boundary

```text
Managed Windows endpoint
  Code Summarizer (WPF/.NET, standard user)
    -> in-memory secret scan and redaction
    -> mode-specific prompt construction
    -> HTTP only to 127.0.0.1:11434
  Ollama service (separately approved component)
    -> separately approved model weights
```

The application and Ollama share a host but are separate security components. The app
does not treat loopback as cryptographic authentication. Host controls must ensure only
the approved Ollama binary can listen on port 11434.

## Input path

1. User enters a snippet and selects a language, mode, and installed model.
2. Input-size policy is checked.
3. Regex scanners identify likely credentials.
4. Restricted builds redact all findings.
5. Prompt instructions treat pasted code as untrusted data.
6. A non-streaming JSON request is sent to fixed IPv4 loopback.

## Output path

1. The HTTP body is read through a byte limit.
2. The Ollama envelope is deserialized.
3. The model response is parsed into the expected analysis structure.
4. Values are displayed as inert WPF text.
5. Restricted builds do not expose raw output or app-provided clipboard export.

## Dependencies

- Self-contained Microsoft .NET 8 Windows Desktop Runtime.
- Windows APIs used by WPF and display-affinity protection.
- Ollama and model weights, supplied and approved by the deployment owner.
- No third-party runtime NuGet packages.

## Privileges and persistence

The app requests no elevation and has no persistence, service, scheduled task, remote
listener, updater, or startup registration. The optional installer uses per-user
installation by default; an organization may replace it with its managed software
distribution mechanism.
