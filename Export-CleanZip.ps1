param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$ZipOutput = "D:\Desktop\CoTuong_Pixel_Offline_V3_Source.zip"
)

$ErrorActionPreference = "Stop"

if (Test-Path $ZipOutput) {
    Remove-Item $ZipOutput -Force
}

$tempDir = Join-Path $env:TEMP ("CoTuong_Clean_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$excludeFolders = @("bin", "obj", ".vs", ".git", ".gradle", ".idea")
$excludeExts = @(".apk", ".keystore", ".jks", ".user", ".suo", ".zip")

Write-Host "Filtering clean source files from: $SourceDir"

$files = Get-ChildItem -Path $SourceDir -Recurse -File

foreach ($f in $files) {
    $rel = $f.FullName.Substring($SourceDir.Length).TrimStart('\', '/')
    $firstFolder = ($rel -split '[\\/]', 2)[0]
    
    if ($excludeFolders -contains $firstFolder) {
        continue
    }

    if ($excludeExts -contains $f.Extension.ToLower()) {
        continue
    }

    $destPath = Join-Path $tempDir $rel
    $destFolder = [System.IO.Path]::GetDirectoryName($destPath)
    if (-not (Test-Path $destFolder)) {
        New-Item -ItemType Directory -Path $destFolder -Force | Out-Null
    }

    Copy-Item -Path $f.FullName -Destination $destPath -Force
}

Write-Host "Compressing to $ZipOutput ..."
Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $ZipOutput -CompressionLevel Optimal

Remove-Item -Path $tempDir -Recurse -Force
Write-Host "EXPORT FINISHED SUCCESSFULLY:"
Get-Item $ZipOutput | Select-Object Name, Length, LastWriteTime
