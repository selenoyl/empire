param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "publish/$Runtime"
$zipPath = Join-Path $artifactsRoot "Empire-$Version-$Runtime.zip"

Write-Host "==> Restoring"
dotnet restore (Join-Path $repoRoot "Empire.sln")

Write-Host "==> Testing"
dotnet test (Join-Path $repoRoot "Empire.sln") -c $Configuration

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

Write-Host "==> Publishing Game (.exe)"
dotnet publish (Join-Path $repoRoot "Game/Game.csproj") `
  -c $Configuration `
  -r $Runtime `
  --self-contained $selfContainedValue `
  -p:PublishSingleFile=false `
  -p:Version=$Version `
  -o $publishDir

Write-Host "==> Preparing logs folder"
New-Item -ItemType Directory -Path (Join-Path $publishDir "logs") -Force | Out-Null

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "==> Creating zip: $zipPath"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath

Write-Host "Done. Launch: $publishDir/Game.exe"
