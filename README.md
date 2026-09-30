# ReportV2 .NET

New .NET 8 implementation for the `reportv2` flow.

This project is intentionally scaffolded beside the existing Node service so the flow can be ported step by step.

## Projects

- `ReportV2.Api`: HTTP endpoints compatible with the current Node API shape.
- `ReportV2.Application`: orchestration, throttling, source hash, job state.
- `ReportV2.Core`: shared models and domain enums.
- `ReportV2.Infrastructure`: SQL Server, file share, ZIP, upload clients.
- `ReportV2.Rendering`: Aspose.Words, PDF/image rendering and merge logic.
- `ReportV2.Worker`: realtime worker and backfill runner.
- `ReportV2.Tests`: migration tests against small deterministic units.

## First commands

```bash
dotnet restore
dotnet build
dotnet run --project ReportV2.Api
```

Health check:

```bash
curl http://localhost:5000/health
```

The current worker has a baseline implementation for CDHA reports, prescriptions, upload, cleanup, backfill, scheduled sync, and manual generation. Template fidelity and real Windows output still need validation before replacing the old flow.

## Windows Setup

Deployment/setup notes for the Windows worker machine are in:

- `docs/WINDOWS_SETUP.md`

Config/template helpers:

- `deploy/appsettings.windows.template.json`
- `scripts/setup-windows-folders.ps1`
- `scripts/publish-win-x64.ps1`

## Aspose License

PDF rendering will use `Aspose.Words`. Configure the license path locally, without committing the license file:

```json
{
  "Aspose": {
    "LicensePath": "C:\\ReportV2\\Aspose.Wordsfor.NET.lic"
  }
}
```

License files (`*.lic`) and production appsettings are ignored by git.

## Year Backfill Scaffold

The worker can scan one year in small daily chunks without storing a large local queue:

```bash
dotnet run --project ReportV2.Worker -- backfill --year 2026 --dry-run --resume
```

Current behavior:

- Reads candidate rows from HIS by day.
- Writes only a tiny cursor file: `data/state/backfill-{year}.json`.
- Processes both CDHA reports and prescriptions by default.
- Uses Aspose.Words to render PDF.
- Uploads through the same API shape as the Node service: `{Upload:BaseUrl}/api/v1/s3/upload-multiple`.
- Deletes local output PDFs after successful upload by default (`Upload:CleanupAfterUpload=true`).
- Cleans temporary extracted images/files after each record.
- Cleans stale `tmp`, `logs`, and temporary `data/state/*.tmp` files on each Worker run.
- Keeps required cursor files such as `data/state/backfill-2026.json` and `data/state/sync-new.json`.
- The first renderer is a working baseline; template fidelity should be validated against real Windows output before replacing the old flow.

## Scheduled Sync Scaffold

After the 2026 backfill is done, the same worker can be scheduled to scan only new/changed rows:

```bash
dotnet run --project ReportV2.Worker -- sync-new --dry-run --resume
```

Current behavior:

- Reads `data/state/sync-new.json` when `--resume` is passed.
- Scans from `LastSuccessfulScanTo - SyncNew:LookbackHours` to now.
- Splits the window into `SyncNew:ChunkHours` chunks.
- Stops if a chunk hits `SyncNew:MaxCandidatesPerChunk`, so candidates are not silently skipped.
- Processes both CDHA reports and prescriptions by default.

Recommended Windows Task Scheduler triggers after HIS sync:

- Daily at `02:30`
- Daily at `14:30`

Action:

```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ReportV2\run-sync-new.ps1"
```

Example `run-sync-new.ps1`:

```powershell
$AppDir = "C:\ReportV2"
Set-Location $AppDir
New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null
.\ReportV2.Worker.exe sync-new --resume *> ".\logs\sync-new-$(Get-Date -Format yyyyMMdd-HHmmss).log"
```

Template scripts are included:

- `scripts/run-backfill-2026.ps1`
- `scripts/run-sync-new.ps1`

## Manual Generation Scaffold

For a problematic session/case, run one candidate manually:

```bash
dotnet run --project ReportV2.Worker -- generate-one --file-num 16012083 --session-id 855699 --dry-run
```

Prescription example:

```bash
dotnet run --project ReportV2.Worker -- generate-one --source prescription --file-num 16012083 --session-id 855699 --progress-id 123456 --dry-run
```

Current behavior uses the no-op processor. Rendering/upload will be attached later, but the manual command shape is ready for testing and operations.

Template script:

- `scripts/run-generate-one.ps1`
