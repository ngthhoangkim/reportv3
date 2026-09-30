param(
    [Parameter(Mandatory = $true)]
    [string]$FileNum,

    [int]$SessionId,

    [int]$ProgressId,

    [string]$Source = "cdha",

    [string]$AppDir = "C:\ReportV2",

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Set-Location $AppDir
New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = ".\logs\generate-one-$FileNum-$timestamp.log"
$arguments = @("generate-one", "--file-num", $FileNum, "--source", $Source)

if ($PSBoundParameters.ContainsKey("SessionId")) {
    $arguments += @("--session-id", $SessionId)
}

if ($PSBoundParameters.ContainsKey("ProgressId")) {
    $arguments += @("--progress-id", $ProgressId)
}

if ($DryRun) {
    $arguments += "--dry-run"
}

& ".\ReportV2.Worker.exe" @arguments *> $logPath
exit $LASTEXITCODE
