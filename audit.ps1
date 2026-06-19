[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [string]$CertificateThumbprint
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$evidence = Join-Path $root "artifacts\audit-evidence\$timestamp"
$publishDirectory = Join-Path $root "artifacts\$Runtime-restricted"
New-Item -ItemType Directory -Path $evidence -Force | Out-Null

$testOutput = & dotnet run --project (Join-Path $root "tests\CodeSummarizer.Windows.Tests.csproj") -c Release 2>&1
$testOutput | Set-Content -LiteralPath (Join-Path $evidence "test-results.txt") -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw "Security regression tests failed." }

$publishArguments = @{ Runtime = $Runtime; SkipInstaller = $true }
if ($CertificateThumbprint) { $publishArguments.CertificateThumbprint = $CertificateThumbprint }
& (Join-Path $root "publish.ps1") @publishArguments
if ($LASTEXITCODE -ne 0) { throw "Restricted publish failed." }

$trackedChanges = @(git -C $root status --porcelain --untracked-files=no)
$commit = (git -C $root rev-parse HEAD).Trim()
$executable = Join-Path $publishDirectory "CodeSummarizer.exe"
$signature = Get-AuthenticodeSignature -LiteralPath $executable

$publishPrefix = $publishDirectory.TrimEnd('\') + '\'
$binaryFiles = Get-ChildItem -LiteralPath $publishDirectory -File -Recurse | ForEach-Object {
    [ordered]@{
        path = $_.FullName.Substring($publishPrefix.Length).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$binaryFiles | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence "binary-file-manifest.json") -Encoding utf8

$sourceFiles = git -C $root ls-files | ForEach-Object {
    $relativePath = $_
    $fullPath = Join-Path $root $relativePath
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        [ordered]@{
            path = $relativePath.Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}
$sourceFiles | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence "source-file-manifest.json") -Encoding utf8

$depsPath = Join-Path $root "obj\Release\net8.0-windows\$Runtime\CodeSummarizer.deps.json"
$depsContent = Get-Content -Raw $depsPath
$runtimeMatch = [regex]::Match($depsContent, '"runtimepack\.Microsoft\.NETCore\.App\.Runtime\.' + [regex]::Escape($Runtime) + '"\s*:\s*"([^"]+)"')
$runtimeVersion = if ($runtimeMatch.Success) { $runtimeMatch.Groups[1].Value } else { "8.0 (exact runtime pack not resolved)" }
$namespaceId = [guid]::NewGuid().ToString()
$sbom = [ordered]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "CodeSummarizer-Windows-$commit"
    documentNamespace = "https://codesummarizer.invalid/spdx/$commit/$namespaceId"
    creationInfo = [ordered]@{
        created = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        creators = @("Tool: CodeSummarizer-audit.ps1", "Organization: Code Summarizer project")
    }
    packages = @(
        [ordered]@{ name = "Code Summarizer for Windows"; SPDXID = "SPDXRef-CodeSummarizer"; versionInfo = "1.1.0"; downloadLocation = "NOASSERTION"; filesAnalyzed = $false; licenseConcluded = "GPL-3.0-or-later"; licenseDeclared = "GPL-3.0-or-later" },
        [ordered]@{ name = "Microsoft .NET Windows Desktop Runtime"; SPDXID = "SPDXRef-DotNetRuntime"; versionInfo = $runtimeVersion; downloadLocation = "https://dotnet.microsoft.com/"; filesAnalyzed = $false; licenseConcluded = "NOASSERTION"; licenseDeclared = "NOASSERTION" },
        [ordered]@{ name = "Ollama"; SPDXID = "SPDXRef-Ollama"; versionInfo = "DEPLOYMENT-SPECIFIC"; downloadLocation = "NOASSERTION"; filesAnalyzed = $false; licenseConcluded = "NOASSERTION"; licenseDeclared = "NOASSERTION" },
        [ordered]@{ name = "Local model weights"; SPDXID = "SPDXRef-Model"; versionInfo = "DEPLOYMENT-SPECIFIC"; downloadLocation = "NOASSERTION"; filesAnalyzed = $false; licenseConcluded = "NOASSERTION"; licenseDeclared = "NOASSERTION" }
    )
    relationships = @(
        [ordered]@{ spdxElementId = "SPDXRef-DOCUMENT"; relationshipType = "DESCRIBES"; relatedSpdxElement = "SPDXRef-CodeSummarizer" },
        [ordered]@{ spdxElementId = "SPDXRef-CodeSummarizer"; relationshipType = "DEPENDS_ON"; relatedSpdxElement = "SPDXRef-DotNetRuntime" },
        [ordered]@{ spdxElementId = "SPDXRef-CodeSummarizer"; relationshipType = "DEPENDS_ON"; relatedSpdxElement = "SPDXRef-Ollama" },
        [ordered]@{ spdxElementId = "SPDXRef-Ollama"; relationshipType = "DEPENDS_ON"; relatedSpdxElement = "SPDXRef-Model" }
    )
}
$sbom | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence "sbom.spdx.json") -Encoding utf8

$metadata = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    gitCommit = $commit
    trackedWorkingTreeClean = ($trackedChanges.Count -eq 0)
    trackedChanges = $trackedChanges
    buildProfile = "restricted"
    governmentModeCompiled = $true
    runtimeIdentifier = $Runtime
    dotnetRuntime = $runtimeVersion
    operatingSystem = [Environment]::OSVersion.VersionString
    powershellVersion = $PSVersionTable.PSVersion.ToString()
    executableSha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
    authenticodeStatus = $signature.Status.ToString()
    signerCertificate = $signature.SignerCertificate.Subject
    testsPassed = $true
    externalComponentsRequiringSeparateApproval = @("Ollama", "selected model weights", "Windows endpoint baseline")
}
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence "build-metadata.json") -Encoding utf8

$documents = @("AUDIT_READINESS.md", "SECURITY.md", "PRIVACY.md", "THREAT_MODEL.md", "DEPLOYMENT.md", "ARCHITECTURE.md", "LICENSE")
$documentDirectory = Join-Path $evidence "documents"
New-Item -ItemType Directory -Path $documentDirectory -Force | Out-Null
foreach ($document in $documents) { Copy-Item -LiteralPath (Join-Path $root $document) -Destination $documentDirectory }

Write-Host "Audit evidence created: $evidence"
Write-Host "Authenticode status: $($signature.Status)"
if ($signature.Status -ne "Valid") { Write-Warning "Signing remains a mandatory pre-deployment action." }
