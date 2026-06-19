[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$GeneralPurpose,
    [string]$CertificateThumbprint,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$profile = if ($GeneralPurpose) { "general" } else { "restricted" }
$governmentMode = if ($GeneralPurpose) { "false" } else { "true" }
$publishDirectory = Join-Path $artifacts "$Runtime-$profile"
$zipPath = Join-Path $artifacts "CodeSummarizer-Windows-$Runtime-$profile.zip"
$hashPath = "$zipPath.sha256"

dotnet publish (Join-Path $root "CodeSummarizer.Windows.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true `
    -p:GovernmentMode=$governmentMode

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$executable = Join-Path $publishDirectory "CodeSummarizer.exe"
if ($CertificateThumbprint) {
    $signTool = Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
    if (-not $signTool) {
        $signTool = Get-ChildItem (Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin") `
            -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
            Sort-Object FullName -Descending |
            Select-Object -ExpandProperty FullName -First 1
    }
    if (-not $signTool) { throw "signtool.exe was not found." }
    & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $executable
    if ($LASTEXITCODE -ne 0) { throw "Authenticode signing failed." }
}

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $hashPath -Value "$zipHash  $(Split-Path $zipPath -Leaf)" -Encoding ascii

$signature = Get-AuthenticodeSignature -LiteralPath $executable
Write-Host "Profile: $profile"
Write-Host "Executable signature: $($signature.Status)"
Write-Host "Portable package: $zipPath"
Write-Host "SHA-256: $zipHash"

$innoCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

if (-not $SkipInstaller -and $Runtime -eq "win-x64" -and $innoCandidates.Count -gt 0) {
    & $innoCandidates[0] "/DSourceDir=$publishDirectory" "/DProfileName=$profile" (Join-Path $root "installer.iss")
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }
    Write-Host "Installer created in $artifacts"
} elseif (-not $SkipInstaller) {
    Write-Host "Inno Setup 6 is not installed; no Setup.exe was created."
}

if ($profile -eq "restricted" -and $signature.Status -ne "Valid") {
    Write-Warning "The restricted artifact is unsigned and must not be promoted to an operational environment until organization-approved signing is complete."
}
