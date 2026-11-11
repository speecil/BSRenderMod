<#
Usage:
  .\build-all.ps1
  .\build-all.ps1 -GameDirs @{ '1.29.1' = 'D:\Beat Saber 1.29.1'; '1.42.3' = 'D:\Beat Saber 1.42.3' }
#>
param(
    [string[]]$Versions = @('1.29.1', '1.40.8', '1.42.3'),
    [hashtable]$GameDirs = @{},
    [string]$OutDir = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw 'vswhere.exe not found, is Visual Studio (or its Build Tools) installed?' }
$msbuild = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found.' }

$solution = Join-Path $PSScriptRoot 'RenderMod.sln'
$project = Join-Path $PSScriptRoot 'RenderMod'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$results = @()
foreach ($version in $Versions) {
    $config = "Release-$version"
    Write-Host "`n=== Building $config" -ForegroundColor Cyan

    $buildArgs = @($solution, '-nologo', '-v:m', '-restore', "-p:Configuration=$config",
                   '-p:DisableCopyToGame=True', '-p:DisableZipRelease=True')
    if ($GameDirs.ContainsKey($version)) { $buildArgs += "-p:BeatSaberDir=$($GameDirs[$version])" }

    & $msbuild @buildArgs
    if ($LASTEXITCODE -ne 0) {
        $results += [pscustomobject]@{ Version = $version; Result = 'BUILD FAILED'; Zip = '' }
        continue
    }

    $manifest = Get-Content (Join-Path $project "Manifests\manifest.$version.json") -Raw | ConvertFrom-Json
    $modVersion = $manifest.version.Split('+')[0]
    $zip = Join-Path $OutDir "RenderMod-$modVersion-bs$version.zip"

    $staging = Join-Path ([IO.Path]::GetTempPath()) "RenderMod-package-$version"
    Remove-Item -Recurse -Force $staging -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force (Join-Path $staging 'Plugins') | Out-Null
    Copy-Item (Join-Path $project "bin\$config\RenderMod.dll") (Join-Path $staging 'Plugins')

    Remove-Item -Force $zip -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $staging 'Plugins') -DestinationPath $zip
    Remove-Item -Recurse -Force $staging

    $results += [pscustomobject]@{ Version = $version; Result = 'OK'; Zip = $zip }
}

Write-Host ''
$results | Format-Table -AutoSize
if ($results.Result -contains 'BUILD FAILED') { exit 1 }
