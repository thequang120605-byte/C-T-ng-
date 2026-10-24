param(
    [ValidateSet('arm64','x86_64')][string]$Architecture = 'arm64',
    [switch]$LocalHttp
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'ChessCore.Mobile.csproj'
$rid = if ($Architecture -eq 'arm64') { 'android-arm64' } else { 'android-x64' }
$argsList = @('publish', $project, '-f', 'net9.0-android', '-c', 'Release', "-p:RuntimeIdentifier=$rid", '-p:AndroidPackageFormats=apk')
if ($LocalHttp) { $argsList += '-p:AllowLocalHttp=true' }
& dotnet @argsList
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Host "APK directory: $PSScriptRoot\bin\Release\net9.0-android\$rid\publish"
Write-Host 'For distribution, configure your own signing keystore. Do not commit passwords.'
