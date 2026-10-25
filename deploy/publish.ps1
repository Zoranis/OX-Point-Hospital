<#
.SYNOPSIS
    Builds the OxBox demo package for a Windows PC without the .NET SDK.

.DESCRIPTION
    Publishes the server and the BU simulator as self-contained win-x64 apps, adds the demo
    settings, the service scripts and the deployment guide, and zips the result to
    artifacts/OxBox-win-x64.zip. Run it from any folder on a PC with the .NET 9 SDK.
#>
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$package = Join-Path $root 'artifacts\OxBox'
$zip = Join-Path $root 'artifacts\OxBox-win-x64.zip'

if (Test-Path $package) { Remove-Item $package -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

$apps = @(
    @{ Project = 'src\OxBox.Web\OxBox.Web.csproj'; Folder = 'server' },
    @{ Project = 'tools\OxBox.BuSimulator\OxBox.BuSimulator.csproj'; Folder = 'simulator' }
)
foreach ($app in $apps) {
    dotnet publish (Join-Path $root $app.Project) -c Release -r win-x64 --self-contained `
        -o (Join-Path $package $app.Folder)
    if ($LASTEXITCODE -ne 0) { throw "Publishing $($app.Project) failed." }
}

# The development settings are never used on the target PC; the demo settings replace them.
Remove-Item (Join-Path $package 'server\appsettings.Development.json') -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot 'appsettings.Production.json') (Join-Path $package 'server')

Copy-Item (Join-Path $PSScriptRoot 'install-service.ps1') $package
Copy-Item (Join-Path $PSScriptRoot 'uninstall-service.ps1') $package
Copy-Item (Join-Path $root 'docs\deployment.md') (Join-Path $package 'README.md')

Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip
Write-Host "Package: $zip"
