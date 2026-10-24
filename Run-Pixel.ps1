param([string]$AvdName = '', [string]$SdkPath = '')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'ChessCore.Mobile.csproj'
$candidates = @($SdkPath, $env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, "$env:LOCALAPPDATA\Android\Sdk", 'D:\Android\Sdk', 'D:\Android', "${env:ProgramFiles(x86)}\Android\android-sdk")
$sdk = $candidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'platform-tools\adb.exe')) -and (Test-Path (Join-Path $_ 'emulator\emulator.exe')) } | Select-Object -First 1
if (-not $sdk) { throw 'Android SDK not found. Run with -SdkPath "D:\Android\Sdk" (use your real SDK directory).' }
$adb = Join-Path $sdk 'platform-tools\adb.exe'
$emulator = Join-Path $sdk 'emulator\emulator.exe'
$avds = @(& $emulator -list-avds | Where-Object { $_.Trim() })
if (-not $AvdName) { $AvdName = $avds | Where-Object { $_ -match 'Pixel' } | Select-Object -First 1 }
if (-not $AvdName -or $avds -notcontains $AvdName) {
    Write-Host ('Available AVDs: ' + ($avds -join ', '))
    throw 'Create a Pixel 6/7/8 AVD in Android Studio > Device Manager first, or pass its exact name with -AvdName.'
}
function Find-AvdSerial {
    foreach ($line in @(& $adb devices)) {
        if ($line -match '^(emulator-\d+)\s+device\s*$') {
            $candidateSerial = $Matches[1]
            $name = @(& $adb -s $candidateSerial emu avd name 2>$null) | Where-Object { $_ -ne 'OK' } | Select-Object -First 1
            if ($name -and $name.Trim() -eq $AvdName) { return $candidateSerial }
        }
    }
    return $null
}
& $adb start-server
$serial = Find-AvdSerial
if (-not $serial) {
    Write-Host "Starting $AvdName ..."
    Start-Process -FilePath $emulator -ArgumentList @('-avd', $AvdName, '-no-snapshot-load')
}
$timer = [System.Diagnostics.Stopwatch]::StartNew()
while ($timer.Elapsed.TotalSeconds -lt 180) {
    $serial = Find-AvdSerial
    if ($serial) {
        $boot = @(& $adb -s $serial shell getprop sys.boot_completed 2>$null) -join ''
        if ($boot.Trim() -eq '1') { break }
    }
    Start-Sleep -Seconds 2
}
if (-not $serial -or $boot.Trim() -ne '1') { throw 'Emulator did not finish booting. Open it in Device Manager and run again.' }
Write-Host "Building Ky Dao Pixel 1.2 on $AvdName ($serial). No backend is needed for AI practice."
& dotnet build $project -f net9.0-android -c Debug -t:Run "-p:AdbTarget=-s $serial" "-p:AndroidSdkDirectory=$sdk"
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE. Copy the last error lines." }
