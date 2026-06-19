[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$publishDirectory = Join-Path $artifacts $Runtime
$zipPath = Join-Path $artifacts "CodeSummarizer-Windows-$Runtime.zip"

dotnet publish (Join-Path $root "CodeSummarizer.Windows.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal

$innoCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

if ($Runtime -eq "win-x64" -and $innoCandidates.Count -gt 0) {
    & $innoCandidates[0] (Join-Path $root "installer.iss")
    Write-Host "Installer created in $artifacts"
} else {
    Write-Host "Portable package created: $zipPath"
    Write-Host "Install Inno Setup 6 on the development machine to also build Setup.exe."
}
