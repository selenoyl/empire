[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SelfContained,
    [switch]$NoPause
)

$ErrorActionPreference = "Stop"

function Ensure-Command {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    Write-Host "==> $Step"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "publish/$Runtime"
$zipPath = Join-Path $artifactsRoot "Empire-$Version-$Runtime.zip"
$logPath = Join-Path $artifactsRoot "build-release-last.log"

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

$shouldPause = -not $NoPause
$exitCode = 0

try {
    Start-Transcript -Path $logPath -Force | Out-Null

    Ensure-Command -Name "dotnet"

    Write-Host "==> Repository root: $repoRoot"
    Write-Host "==> Build log: $logPath"

    Invoke-DotNet -Step "Restoring" -Arguments @("restore", (Join-Path $repoRoot "Empire.sln"))
    Invoke-DotNet -Step "Testing" -Arguments @("test", (Join-Path $repoRoot "Empire.sln"), "-c", $Configuration)

    $selfContainedValue = if ($SelfContained) { "true" } else { "false" }
    Invoke-DotNet -Step "Publishing Game (.exe)" -Arguments @(
        "publish", (Join-Path $repoRoot "Game/Game.csproj"),
        "-c", $Configuration,
        "-r", $Runtime,
        "--self-contained", $selfContainedValue,
        "-p:PublishSingleFile=false",
        "-p:Version=$Version",
        "-o", $publishDir
    )

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
}
catch {
    $exitCode = 1
    Write-Host "==> FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "See full log: $logPath" -ForegroundColor Yellow
}
finally {
    try { Stop-Transcript | Out-Null } catch { }
    if ($shouldPause) {
        Read-Host "Press Enter to close"
    }
}

exit $exitCode
