[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SelfContained,
    [switch]$PauseOnExit
)

$ErrorActionPreference = "Stop"

function Ensure-Command {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "publish/$Runtime"
$zipPath = Join-Path $artifactsRoot "Empire-$Version-$Runtime.zip"

try {
    Ensure-Command -Name "dotnet"

    Write-Host "==> Repository root: $repoRoot"
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

    Write-Host "==> Done"
    Write-Host "Launch: $publishDir/Game.exe"
    Write-Host "Zip:    $zipPath"
    exit 0
}
catch {
    Write-Error "Release build failed: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($PauseOnExit) {
        Read-Host "Press Enter to close"
    }
}
