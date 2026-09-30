param(
    [string]$AppDir = "C:\ReportV2",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Set-Location $AppDir
New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = ".\logs\backfill-2026-$timestamp.log"
$arguments = @("backfill", "--year", "2026", "--resume")

if ($DryRun) {
    $arguments += "--dry-run"
}

& ".\ReportV2.Worker.exe" @arguments *> $logPath
exit $LASTEXITCODE
