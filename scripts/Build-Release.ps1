$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$app = Join-Path $repoRoot 'src\OpenDeviceToolkit.App\OpenDeviceToolkit.App.csproj'
$out = Join-Path $repoRoot 'artifacts\OpenDeviceToolkit'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found. Install .NET 8 SDK and run this script again.'
}

Write-Host 'Restoring...'
dotnet restore $app

Write-Host 'Building and publishing self-contained x64 executable...'
dotnet publish $app `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    --output $out

Write-Host "Release output: $out"
