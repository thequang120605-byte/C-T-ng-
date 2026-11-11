<#
.SYNOPSIS
    Script dong bo du lieu co tuong tu MySQL cuc bo len TiDB Cloud.
.DESCRIPTION
    Ho tro 2 che do:
    1. Full: Dong bo toan bo co so du lieu (tat ca bang, cau truc va du lieu).
    2. Delta: Chi dong bo tai khoan TrongNguyen34 va QuocTrung.
.EXAMPLE
    .\Sync-To-TiDBCloud.ps1
.EXAMPLE
    .\Sync-To-TiDBCloud.ps1 -HostName "gateway01.ap-southeast-1.prod.aws.tidbcloud.com" -Port 4000 -User "xxxxxx.root" -Password "Secret" -Database "chess_db" -Mode "Full"
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
    [ValidateSet("Full", "Delta")]
    [string]$Mode
)

Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "       CONG CU DONG BO CO SO DU LIEU CO TUONG LEN TiDB CLOUD             " -ForegroundColor Yellow
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Tim duong dan mysql.exe
$mysqlExe = $null

$cmdCheck = Get-Command "mysql.exe" -ErrorAction SilentlyContinue
if ($cmdCheck) {
    $mysqlExe = $cmdCheck.Source
}

if (-not $mysqlExe) {
    $laragonPaths = @(
        "D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe",
        "C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe",
        "C:\laragon\bin\mysql\mysql-8.0.30-winx64\bin\mysql.exe"
    )
    foreach ($p in $laragonPaths) {
        if (Test-Path $p) {
            $mysqlExe = $p
            break
        }
    }
}

if (-not $mysqlExe) {
    $laragonBase = "D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql"
    if (Test-Path $laragonBase) {
        $found = Get-ChildItem -Path $laragonBase -Filter "mysql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $mysqlExe = $found.FullName }
    }
}

if (-not $mysqlExe) {
    Write-Host "[!] Khong tim thay cong cu 'mysql.exe' tren may." -ForegroundColor Red
    Write-Host "    Ban co the dung Cach 2: Copy file SQL dan vao SQL Editor tren web TiDB Cloud." -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Da tim thay MySQL Client: $mysqlExe" -ForegroundColor Green

# 2. Thu thap thong tin ket noi TiDB Cloud neu chua truyen tham so
if ([string]::IsNullOrWhiteSpace($HostName)) {
    $HostName = Read-Host "Nhap Host TiDB Cloud (vi du: gateway01.ap-southeast-1.prod.aws.tidbcloud.com)"
}

if ([string]::IsNullOrWhiteSpace($HostName)) {
    Write-Host "[!] Host khong duoc de trong!" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($User)) {
    $User = Read-Host "Nhap User TiDB Cloud (vi du: 3rxxxxx.root)"
}

if ([string]::IsNullOrWhiteSpace($User)) {
    Write-Host "[!] User khong duoc de trong!" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $secPass = Read-Host "Nhap Password TiDB Cloud" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPass)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
}

if ([string]::IsNullOrWhiteSpace($Mode)) {
    Write-Host ""
    Write-Host "Chon che do dong bo:" -ForegroundColor Cyan
    Write-Host "  [1] Dong bo TOAN BO co so du lieu (Full Sync - Khuyen dung)" -ForegroundColor White
    Write-Host "  [2] Chi dong bo tai khoan TrongNguyen34 va QuocTrung (Delta Sync)" -ForegroundColor White
    $choice = Read-Host "Nhap lua chon (1 hoac 2, mac dinh la 1)"
    if ($choice -eq "2") {
        $Mode = "Delta"
    } else {
        $Mode = "Full"
    }
}

# 3. Xac dinh file SQL tuong ung
$rootDir = $PSScriptRoot
$sqlFile = ""
if ($Mode -eq "Delta") {
    $sqlFile = Join-Path $rootDir "ChessCore\Database\sync_users_activity_delta.sql"
    Write-Host "`n>>> Che do duoc chon: DELTA SYNC (TrongNguyen34 & QuocTrung)" -ForegroundColor Yellow
} else {
    $sqlFile = Join-Path $rootDir "ChessCore\Database\sync_tidb_cloud_full.sql"
    Write-Host "`n>>> Che do duoc chon: FULL SYNC (Toan bo co so du lieu chess_db)" -ForegroundColor Yellow
}

if (-not (Test-Path $sqlFile)) {
    Write-Host "[!] Khong tim thay file SQL tai: $sqlFile" -ForegroundColor Red
    exit 1
}

$fileInfo = Get-Item $sqlFile
$sizeKb = [math]::Round($fileInfo.Length / 1024, 1)
Write-Host "[OK] Tep SQL: $($fileInfo.Name) ($sizeKb KB)" -ForegroundColor Green

# 4. Thuc thi nap vao TiDB Cloud
Write-Host ""
Write-Host "Dang ket noi va day du lieu len TiDB Cloud ($($HostName):$Port / $Database)..." -ForegroundColor Cyan

$pinfo = New-Object System.Diagnostics.ProcessStartInfo
$pinfo.FileName = $mysqlExe
$pinfo.Arguments = "-h $($HostName) -P $($Port) -u $($User) -p$($Password) -D $($Database) --ssl-mode=REQUIRED --default-character-set=utf8mb4"
$pinfo.RedirectStandardInput = $true
$pinfo.RedirectStandardOutput = $true
$pinfo.RedirectStandardError = $true
$pinfo.UseShellExecute = $false
$pinfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($pinfo)

$reader = [System.IO.File]::OpenText($sqlFile)
$writer = $process.StandardInput
while (-not $reader.EndOfStream) {
    $line = $reader.ReadLine()
    $writer.WriteLine($line)
}
$reader.Close()
$writer.Close()

$stdout = $process.StandardOutput.ReadToEnd()
$stderr = $process.StandardError.ReadToEnd()
$process.WaitForExit()

if ($process.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "==========================================================================" -ForegroundColor Green
    Write-Host "   DONG BO DU LIEU LEN TiDB CLOUD THANH CONG!                             " -ForegroundColor Green
    Write-Host "==========================================================================" -ForegroundColor Green
    Write-Host "[OK] Tai khoan TrongNguyen34 va QuocTrung da duoc dong bo len Cloud." -ForegroundColor White
    Write-Host "[OK] Toan bo van dau, nuoc di, lich su co the va bai hoc da cap nhat." -ForegroundColor White
    Write-Host "[OK] Ban co the truy cap web tren Render de kiem tra ngay lap tuc." -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "==========================================================================" -ForegroundColor Red
    Write-Host "   [!] DONG BO THAT BAI. MA LOI: $($process.ExitCode)                     " -ForegroundColor Red
    Write-Host "==========================================================================" -ForegroundColor Red
    if ($stderr) {
        Write-Host "Chi tiet loi:" -ForegroundColor Red
        Write-Host $stderr -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "HUONG DAN DU PHONG (Cach 2):" -ForegroundColor Cyan
    Write-Host "  1. Mo trinh duyet vao https://tidbcloud.com va dang nhap." -ForegroundColor White
    Write-Host "  2. Chon Cluster cua ban -> Mo tab 'SQL Editor'." -ForegroundColor White
    Write-Host "  3. Mo file: $sqlFile" -ForegroundColor White
    Write-Host "  4. Copy toan bo noi dung -> Dan vao SQL Editor -> Nhan 'Run'." -ForegroundColor White
    Write-Host ""
}
