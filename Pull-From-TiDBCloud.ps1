<#
.SYNOPSIS
    Script kéo (pull/export) dữ liệu từ TiDB Cloud (Render) về MySQL cục bộ (Laragon).
.DESCRIPTION
    Hỗ trợ 2 chế độ:
    1. ImportToLocal: Kéo dữ liệu từ Cloud và nạp thẳng vào CSDL MySQL local (tự động sao lưu bản local trước).
    2. ExportOnly: Chỉ tải dữ liệu về thành tệp .sql để bạn lưu trữ hoặc kiểm tra.
.EXAMPLE
    .\Pull-From-TiDBCloud.ps1
.EXAMPLE
    .\Pull-From-TiDBCloud.ps1 -HostName "gateway01.ap-southeast-1.prod.aws.tidbcloud.com" -Port 4000 -User "xxxxxx.root" -Password "Secret" -Database "chess_db"
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [string]$HostName,

    [Parameter(Mandatory = $false)]
    [int]$Port = 4000,

    [Parameter(Mandatory = $false)]
    [string]$User,

    [Parameter(Mandatory = $false)]
    [string]$Password,

    [Parameter(Mandatory = $false)]
    [string]$Database = "chess_db",

    [Parameter(Mandatory = $false)]
    [string]$LocalHost = "localhost",

    [Parameter(Mandatory = $false)]
    [int]$LocalPort = 3306,

    [Parameter(Mandatory = $false)]
    [string]$LocalUser = "root",

    [Parameter(Mandatory = $false)]
    [string]$LocalPassword = "",

    [Parameter(Mandatory = $false)]
    [string]$LocalDatabase = "chess_db",

    [Parameter(Mandatory = $false)]
    [ValidateSet("ImportToLocal", "ExportOnly")]
    [string]$Mode
)

Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "     CÔNG CỤ KÉO DỮ LIỆU TỪ RENDER (TiDB CLOUD) VỀ MÁY CỤC BỘ            " -ForegroundColor Yellow
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Tìm đường dẫn mysqldump.exe và mysql.exe
$mysqldumpExe = $null
$mysqlExe = $null

$dumpCheck = Get-Command "mysqldump.exe" -ErrorAction SilentlyContinue
if ($dumpCheck) { $mysqldumpExe = $dumpCheck.Source }

$mysqlCheck = Get-Command "mysql.exe" -ErrorAction SilentlyContinue
if ($mysqlCheck) { $mysqlExe = $mysqlCheck.Source }

$laragonPaths = @(
    "D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql\mysql-8.4.3-winx64\bin",
    "C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin",
    "C:\laragon\bin\mysql\mysql-8.0.30-winx64\bin"
)

foreach ($p in $laragonPaths) {
    if (-not $mysqldumpExe -and (Test-Path (Join-Path $p "mysqldump.exe"))) {
        $mysqldumpExe = Join-Path $p "mysqldump.exe"
    }
    if (-not $mysqlExe -and (Test-Path (Join-Path $p "mysql.exe"))) {
        $mysqlExe = Join-Path $p "mysql.exe"
    }
}

if (-not $mysqldumpExe) {
    $laragonBase = "D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql"
    if (Test-Path $laragonBase) {
        $found = Get-ChildItem -Path $laragonBase -Filter "mysqldump.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $mysqldumpExe = $found.FullName }
    }
}

if (-not $mysqlExe) {
    $laragonBase = "D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql"
    if (Test-Path $laragonBase) {
        $found = Get-ChildItem -Path $laragonBase -Filter "mysql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $mysqlExe = $found.FullName }
    }
}

if (-not $mysqldumpExe) {
    Write-Host "[!] Không tìm thấy công cụ 'mysqldump.exe' trên máy." -ForegroundColor Red
    Write-Host "    Vui lòng kiểm tra lại đường dẫn cài đặt Laragon / MySQL." -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Đã tìm thấy mysqldump: $mysqldumpExe" -ForegroundColor Green
if ($mysqlExe) {
    Write-Host "[OK] Đã tìm thấy mysql client: $mysqlExe" -ForegroundColor Green
}

# 2. Thu thập thông tin kết nối TiDB Cloud (Render)
if ([string]::IsNullOrWhiteSpace($HostName)) {
    $HostName = Read-Host "Nhập Host TiDB Cloud (ví dụ: gateway01.ap-southeast-1.prod.aws.tidbcloud.com)"
}
if ([string]::IsNullOrWhiteSpace($HostName)) {
    Write-Host "[!] Host không được để trống!" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($User)) {
    $User = Read-Host "Nhập User TiDB Cloud (ví dụ: 3rxxxxx.root)"
}
if ([string]::IsNullOrWhiteSpace($User)) {
    Write-Host "[!] User không được để trống!" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $secPass = Read-Host "Nhập Password TiDB Cloud" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPass)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
}

# 3. Chọn chế độ
if ([string]::IsNullOrWhiteSpace($Mode)) {
    Write-Host ""
    Write-Host "Chọn phương thức xử lý dữ liệu:" -ForegroundColor Cyan
    Write-Host "  [1] Tự động tải về và nạp trực tiếp vào MySQL Local (Khuyên dùng)" -ForegroundColor White
    Write-Host "  [2] Chỉ xuất ra file .sql (Export Only)" -ForegroundColor White
    $choice = Read-Host "Nhập lựa chọn (1 hoặc 2, mặc định là 1)"
    if ($choice -eq "2") {
        $Mode = "ExportOnly"
    } else {
        $Mode = "ImportToLocal"
    }
}

# 4. Tạo thư mục và tên file lưu trữ
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$exportDir = Join-Path $PSScriptRoot "ChessCore\Database"
if (-not (Test-Path $exportDir)) {
    New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
}

$dumpFile = Join-Path $exportDir "tidb_cloud_dump_$timestamp.sql"

Write-Host ""
Write-Host "Đang xuất dữ liệu từ TiDB Cloud ($($HostName):$Port / $Database)..." -ForegroundColor Cyan

# Tham số tối ưu cho TiDB Cloud dump
$dumpArgs = @(
    "-h", $HostName,
    "-P", $Port,
    "-u", $User,
    "-p$Password",
    "--ssl-mode=REQUIRED",
    "--default-character-set=utf8mb4",
    "--skip-column-statistics",
    "--no-tablespaces",
    "--single-transaction",
    "--databases", $Database,
    "--result-file=$dumpFile"
)

$dumpProcess = Start-Process -FilePath $mysqldumpExe -ArgumentList $dumpArgs -NoNewWindow -Wait -PassThru

if ($dumpProcess.ExitCode -ne 0 -or -not (Test-Path $dumpFile) -or (Get-Item $dumpFile).Length -eq 0) {
    Write-Host ""
    Write-Host "==========================================================================" -ForegroundColor Red
    Write-Host "   [!] XUẤT DỮ LIỆU TỪ TiDB CLOUD THẤT BẠI! Mã lỗi: $($dumpProcess.ExitCode)" -ForegroundColor Red
    Write-Host "==========================================================================" -ForegroundColor Red
    Write-Host "Vui lòng kiểm tra lại: Host, Port, User, Password và quyền truy cập mạng." -ForegroundColor Yellow
    exit 1
}

$dumpSizeKb = [math]::Round((Get-Item $dumpFile).Length / 1024, 1)
Write-Host "[OK] Đã xuất thành công tệp SQL: $dumpFile ($dumpSizeKb KB)" -ForegroundColor Green

# 5. Nếu chọn ImportToLocal
if ($Mode -eq "ImportToLocal") {
    if (-not $mysqlExe) {
        Write-Host "[!] Không tìm thấy mysql.exe để nạp vào Local. Bạn có thể tự nạp file $dumpFile qua phpMyAdmin / HeidiSQL." -ForegroundColor Yellow
        exit 0
    }

    Write-Host ""
    Write-Host ">>> Đang sao lưu CSDL Local trước khi nạp dữ liệu mới..." -ForegroundColor Yellow
    $backupLocalFile = Join-Path $exportDir "local_backup_before_pull_$timestamp.sql"
    $backupArgs = @(
        "-h", $LocalHost,
        "-P", $LocalPort,
        "-u", $LocalUser
    )
    if (-not [string]::IsNullOrEmpty($LocalPassword)) {
        $backupArgs += "-p$LocalPassword"
    }
    $backupArgs += @(
        "--default-character-set=utf8mb4",
        "--databases", $LocalDatabase,
        "--result-file=$backupLocalFile"
    )
    
    $localBackupProcess = Start-Process -FilePath $mysqldumpExe -ArgumentList $backupArgs -NoNewWindow -Wait -PassThru -ErrorAction SilentlyContinue
    if ($localBackupProcess.ExitCode -eq 0 -and (Test-Path $backupLocalFile)) {
        Write-Host "[OK] Đã sao lưu CSDL Local an toàn tại: $backupLocalFile" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host ">>> Đang nạp dữ liệu từ Cloud vào MySQL Local ($LocalHost:$LocalPort / $LocalDatabase)..." -ForegroundColor Cyan

    $importPinfo = New-Object System.Diagnostics.ProcessStartInfo
    $importPinfo.FileName = $mysqlExe
    $importPassParam = if ([string]::IsNullOrEmpty($LocalPassword)) { "" } else { "-p$LocalPassword" }
    $importPinfo.Arguments = "-h $LocalHost -P $LocalPort -u $LocalUser $importPassParam -D $LocalDatabase --default-character-set=utf8mb4"
    $importPinfo.RedirectStandardInput = $true
    $importPinfo.RedirectStandardOutput = $true
    $importPinfo.RedirectStandardError = $true
    $importPinfo.UseShellExecute = $false
    $importPinfo.CreateNoWindow = $true

    $importProcess = [System.Diagnostics.Process]::Start($importPinfo)
    $reader = [System.IO.File]::OpenText($dumpFile)
    $writer = $importProcess.StandardInput
    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        $writer.WriteLine($line)
    }
    $reader.Close()
    $writer.Close()

    $importProcess.WaitForExit()

    if ($importProcess.ExitCode -eq 0) {
        Write-Host ""
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host "   🎉 ĐỒNG BỘ DỮ LIỆU TỪ RENDER VỀ MÁY LOCAL THÀNH CÔNG RỰC RỠ!          " -ForegroundColor Green
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host "[✓] Toàn bộ ván đấu, nước đi đã chơi trên Render đã có trên máy tính của bạn." -ForegroundColor White
        Write-Host "[✓] CSDL MySQL cục bộ ($LocalDatabase) đã được cập nhật đồng bộ hoàn toàn." -ForegroundColor White
        Write-Host "[✓] Bạn có thể mở web localhost:5000 để kiểm tra và xem lại các ván đã chơi." -ForegroundColor White
        Write-Host ""
    } else {
        $errOut = $importProcess.StandardError.ReadToEnd()
        Write-Host "[!] Nạp vào local gặp lỗi: $errOut" -ForegroundColor Red
        Write-Host "    Bạn có thể mở tệp $dumpFile và chạy bằng HeidiSQL hoặc phpMyAdmin." -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "==========================================================================" -ForegroundColor Green
    Write-Host "   ĐÃ XUẤT FILE SQL DỮ LIỆU TỪ CLOUD THÀNH CÔNG!                         " -ForegroundColor Green
    Write-Host "==========================================================================" -ForegroundColor Green
    Write-Host "Tệp lưu tại: $dumpFile" -ForegroundColor White
    Write-Host "Bạn có thể dùng HeidiSQL, Navicat, DBeaver hoặc phpMyAdmin để mở và kiểm tra." -ForegroundColor White
    Write-Host ""
}
