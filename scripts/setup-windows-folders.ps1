param(
    [string]$AppDir = "C:\ReportV2",
    [string]$DataDir = "D:\ReportV2"
)

$ErrorActionPreference = "Stop"

$paths = @(
    $AppDir,
    (Join-Path $AppDir "scripts"),
    (Join-Path $AppDir "Templates"),
    (Join-Path $AppDir "Templates\ToaThuocV2"),
    (Join-Path $AppDir "Documents"),
    (Join-Path $AppDir "logs"),
    (Join-Path $AppDir "data"),
    (Join-Path $AppDir "data\state"),
    $DataDir,
    (Join-Path $DataDir "output"),
    (Join-Path $DataDir "output\prescriptions"),
    (Join-Path $DataDir "tmp")
)

foreach ($path in $paths) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Write-Host "Created ReportV2 folders:"
$paths | ForEach-Object { Write-Host " - $_" }
Write-Host ""
Write-Host "Next:"
Write-Host " - Copy the published Worker files to $AppDir"
Write-Host " - Copy appsettings.json to $AppDir"
Write-Host " - Copy Aspose.Wordsfor.NET.lic to $AppDir"
Write-Host " - Copy Word templates to $AppDir\Templates"
