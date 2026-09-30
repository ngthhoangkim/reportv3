# Huong dan setup ReportV2 tren Windows

Tai lieu nay dung de setup may Windows chay worker `.NET 8` cho cac luong:

- `backfill --year 2026 --resume`
- `sync-new --resume` sau khung gio HIS sync
- `generate-one` de chay tay khi mot session/case bi loi

Khong dua secret, connection string that, hoac file license Aspose vao git. Cac file do chi nam tren may Windows chay that.

## 1. Cai .NET

Neu may Windows dung de dev/chay lenh tu source:

- Cai `.NET 8 SDK`

Neu may Windows chi dung de chay app da publish:

- Co the cai `.NET 8 Runtime`
- Hoac publish dang self-contained, khi do may Windows khong can cai .NET runtime rieng

Kiem tra:

```powershell
dotnet --info
```

## 2. Tao cau truc thu muc

Cau truc khuyen nghi:

```text
C:\ReportV2\
  ReportV2.Worker.exe
  appsettings.json
  Aspose.Wordsfor.NET.lic
  run-backfill-2026.ps1
  run-sync-new.ps1
  run-generate-one.ps1
  Templates\
    ToaThuocV2\
      TT_MAT_1.docx
      TT_MAT_2.docx
  Documents\
  logs\
  data\
    state\

D:\ReportV2\
  output\
    prescriptions\
  tmp\
```

Chay lenh tao thu muc tu repo:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\setup-windows-folders.ps1
```

Neu may chi co o `C:`, chay:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\setup-windows-folders.ps1 -DataDir C:\ReportV2
```

## 3. Publish app de dua qua Windows

Tu thu muc root cua repo:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

Sau do copy toan bo file trong:

```text
publish\ReportV2\
```

sang:

```text
C:\ReportV2\
```

Mac dinh script publish dang tao ban self-contained, tuc la may Windows co the chay `ReportV2.Worker.exe` ma khong can cai .NET runtime rieng.

Neu may Windows da cai `.NET 8 Runtime` va muon goi publish nhe hon:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -FrameworkDependent
```

## 4. Dua config, license, template qua

Copy cac file nay thu cong vao may Windows:

- `Aspose.Wordsfor.NET.lic` -> `C:\ReportV2\Aspose.Wordsfor.NET.lic`
- `appsettings.json` that -> `C:\ReportV2\appsettings.json`
- Template bao cao -> `C:\ReportV2\Templates\...`
- Template toa thuoc -> `C:\ReportV2\Templates\ToaThuocV2\TT_MAT_1.docx` va `TT_MAT_2.docx`

Dung file nay lam mau:

```text
deploy\appsettings.windows.template.json
```

Roi sua placeholder thanh thong tin that tren may Windows.

Cac config quan trong:

- `ConnectionStrings:HisDb`: connection vao HIS SQL Server.
- `Aspose:LicensePath`: `C:\ReportV2\Aspose.Wordsfor.NET.lic`.
- `ReportV2:SourceImageDir`: duong dan UNC toi may/share anh, vi du `\\server\share`.
- `ReportV2:FallbackImageDir`: share phu neu co.
- `ReportV2:OutputPath`: noi luu PDF tam truoc khi upload.
- `ReportV2:TempPath`: noi giai nen xu ly anh/file tam.
- `ReportV2:StatePath`: noi luu cursor de resume backfill/sync.
- `Upload:BaseUrl`: service upload AWS/S3 hien tai.
- `Upload:CleanupAfterUpload`: de `true` de upload xong thi xoa PDF local.

Tai khoan Windows dung de chay worker phai co quyen doc image share. Neu share can account domain, Task Scheduler cung phai chay bang dung account do.

## 5. Test chay tay

CDHA:

```powershell
cd C:\ReportV2
.\ReportV2.Worker.exe generate-one --source cdha --file-num YOUR_FILE_NUM --session-id YOUR_SESSION_ID --dry-run
```

Toa thuoc:

```powershell
cd C:\ReportV2
.\ReportV2.Worker.exe generate-one --source prescription --file-num YOUR_FILE_NUM --session-id YOUR_SESSION_ID --progress-id YOUR_PROGRESS_ID --dry-run
```

Khi config da dung, bo `--dry-run` de chay that.

## 6. Chay backfill nam 2026

Chay thu truoc:

```powershell
powershell.exe -ExecutionPolicy Bypass -File C:\ReportV2\run-backfill-2026.ps1 -DryRun
```

Chay that:

```powershell
powershell.exe -ExecutionPolicy Bypass -File C:\ReportV2\run-backfill-2026.ps1
```

Luong nay dung cursor:

```text
C:\ReportV2\data\state\backfill-2026.json
```

Khong xoa cursor nay khi backfill chua xong, vi no giup resume tiep neu bi dung giua chung.

## 7. Dat lich chay du lieu moi

HIS sync vao khoang `02:00` va `14:00`, nen dat worker chay sau do:

- Hang ngay `02:30`
- Hang ngay `14:30`

Task Scheduler:

```text
Program/script:
powershell.exe

Arguments:
-ExecutionPolicy Bypass -File "C:\ReportV2\run-sync-new.ps1"

Start in:
C:\ReportV2
```

Task nay nen chay bang account co quyen SQL Server, quyen doc image share, va quyen goi upload API.

## 8. Kiem soat dung luong

Worker da co co che tranh phinh folder:

- `tmp` duoc xoa sau moi record va don file cu khi worker start.
- `output` xoa PDF sau khi upload thanh cong neu `Upload:CleanupAfterUpload=true`.
- `logs` duoc don theo so ngay va gioi han dung luong `ReportV2:MaxLogsMegabytes`.
- `data\state` chi giu file cursor nho nhu `backfill-2026.json` va `sync-new.json`.

Neu upload loi, PDF se duoc giu lai trong `output` de debug/retry. Sau khi sua loi, co the chay lai bang `generate-one` hoac resume worker.

## 9. Checklist truoc khi chay that

Kiem tra cac muc nay truoc khi chay real data:

- `C:\ReportV2\appsettings.json` ton tai va da dien gia tri that.
- `C:\ReportV2\Aspose.Wordsfor.NET.lic` ton tai.
- Template Word da nam trong `C:\ReportV2\Templates`.
- Account Windows doc duoc duong dan image share.
- Account Windows connect duoc HIS SQL Server.
- May Windows goi duoc upload API.
- `Upload:CleanupAfterUpload` dang la `true`.
