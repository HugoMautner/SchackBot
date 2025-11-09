<#
  build-win.ps1 - publish SchackBot.UciAdapter as single-file self-contained exe for Windows.
  Run from SchackBot/scripts:  .\build-win.ps1 -Runtime win-x64 [-Zip]
#>

param(
    [string]$Runtime = "win-x64",
    [switch]$Zip
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Resolve-Path "$ScriptDir\.."
$ProjectPath = Join-Path $RootDir "engine\src\SchackBot.UciAdapter\SchackBot.UciAdapter.csproj"

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project not found at: $ProjectPath"
    exit 2
}

$OutDir = Join-Path $RootDir "publish\$Runtime"
Write-Host "Publishing project for runtime $Runtime -> $OutDir"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

dotnet publish $ProjectPath -c Release -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutDir

Write-Host "Build finished: $OutDir"

if ($Zip) {
    $zipPath = Join-Path $RootDir "publish\$Runtime.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $zipPath
    Write-Host "Zipped to: $zipPath"
}

Write-Host "Done."
