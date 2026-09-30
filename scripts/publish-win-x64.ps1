param(
    [string]$OutputDir = ".\publish\ReportV2",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$publishArgs = @(
    "publish",
    ".\ReportV2.Worker\ReportV2.Worker.csproj",
    "-c",
    "Release",
    "-r",
    "win-x64",
    "-o",
    $OutputDir
)

if ($FrameworkDependent) {
    $publishArgs += "--self-contained"
    $publishArgs += "false"
}
else {
    $publishArgs += "--self-contained"
    $publishArgs += "true"
}

dotnet @publishArgs

Copy-Item ".\scripts\run-backfill-2026.ps1" $OutputDir -Force
Copy-Item ".\scripts\run-sync-new.ps1" $OutputDir -Force
Copy-Item ".\scripts\run-generate-one.ps1" $OutputDir -Force
Copy-Item ".\deploy\appsettings.windows.template.json" (Join-Path $OutputDir "appsettings.template.json") -Force

Write-Host "Published to $OutputDir"
Write-Host "Do not commit or share the real appsettings.json or Aspose license."
