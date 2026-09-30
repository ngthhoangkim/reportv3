param(
    [string]$AppDir = "C:\ReportV2",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Set-Location $AppDir
New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = ".\logs\sync-new-$timestamp.log"
$arguments = @("sync-new", "--resume")

if ($DryRun) {
    $arguments += "--dry-run"
}

& ".\ReportV2.Worker.exe" @arguments *> $logPath
exit $LASTEXITCODE
