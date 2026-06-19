# Code Summarizer for Windows

A native Windows desktop application that explains, summarizes, validates, and reviews code using an Ollama model running entirely on your computer.

## Privacy properties

- Sends model requests only to `http://127.0.0.1:11434`.
- Never sends code to a cloud service.
- Does not write pasted code or model responses to disk.
- Detects and optionally masks common credentials before analysis.
- Sensitive Mode enforces masking and hides raw model output.

The default release produced by `publish.ps1` is the restricted assessment build. It
also disables app clipboard export, requires Windows screen-capture exclusion, bounds
input and response sizes, and compiles Sensitive Mode so it cannot be disabled.

## Features

- Summarize code
- Explain code for a junior developer
- Scan for security and quality risks
- Perform model-guided validation
- Copy individual sections or a complete report
- Cancel long-running analysis
- Discover locally installed Ollama models

Validation is AI-guided. It does not replace a compiler, parser, or language-specific linter.

## Requirements for users

The application itself is self-contained; users do not need to install .NET, Python, Node.js, or Rust.

Ollama and at least one local model are still required because Ollama is the AI engine:

```powershell
ollama pull qwen2.5-coder:7b
```

Smaller computers can use a smaller model, such as `qwen2.5-coder:1.5b`.

## Development

Requires the .NET 8 SDK or newer on Windows.

```powershell
dotnet build
dotnet run
dotnet run --project tests\CodeSummarizer.Windows.Tests.csproj
```

## Create the downloadable version

Run:

```powershell
.\publish.ps1
```

This creates a self-contained 64-bit restricted Windows application, SHA-256 file, and
portable ZIP under `artifacts`.
If Inno Setup 6 is installed on the development computer, the script also creates
`CodeSummarizer-Windows-Setup.exe`. End users do not need Inno Setup.

## Architecture

```text
WPF interface
  -> secret scan and optional redaction
  -> mode-specific prompt
  -> Ollama on localhost
  -> structured JSON parser
  -> local result display
```

The project intentionally uses only the .NET platform libraries. There are no runtime NuGet dependencies.

## Security assessment

Run `.\audit.ps1` to produce test results, binary/source hash manifests, build metadata,
an SPDX component inventory, and copies of the security documentation. Start with
[AUDIT_READINESS.md](AUDIT_READINESS.md) and [DEPLOYMENT.md](DEPLOYMENT.md).

The restricted build is an assessment candidate. It is not an authorization to process
classified information. Ollama, model weights, the Windows endpoint, signing, scans,
and the organization's RMF/ATO decision remain separate requirements.

## License

GPL-3.0-or-later, matching the original Code Summarizer project.
