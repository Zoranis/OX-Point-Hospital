<#
.SYNOPSIS
    Installs and starts OxBox and the BU simulator as Windows services.

.DESCRIPTION
    Run it as administrator from the folder the package was unpacked to; the services run the
    programs from that folder, so unpack it where it should stay (for example C:\OxBox).
    Running it again updates the services after the folder was moved or replaced.
    Both services start with Windows and restart after a crash. A firewall rule lets other PCs
    reach the web app on port 5029.
#>
$ErrorActionPreference = 'Stop'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script as administrator (right-click PowerShell, "Run as administrator").'
}

# Files from a downloaded zip are marked as coming from the internet; Windows may refuse to run them.
Get-ChildItem $PSScriptRoot -Recurse -File | Unblock-File

$services = @(
    @{ Name = 'OxBoxSimulator'; DisplayName = 'OxBox BU simulator'; Exe = 'simulator\OxBox.BuSimulator.exe'
       Description = 'Stands in for the box control unit (BU) in the OxBox demo. Control page: http://localhost:5080' },
    @{ Name = 'OxBox'; DisplayName = 'OxBox server'; Exe = 'server\OxBox.Web.exe'
       Description = 'OxBox web app and box display, http://localhost:5029' }
)

foreach ($service in $services) {
    $exe = Join-Path $PSScriptRoot $service.Exe
    if (-not (Test-Path $exe)) { throw "Not found: $exe. Run this script from the unpacked package folder." }

    if (Get-Service $service.Name -ErrorAction SilentlyContinue) {
        Stop-Service $service.Name -Force
        sc.exe config $service.Name binPath= "`"$exe`"" start= auto | Out-Null
        Write-Host "Updated service $($service.Name)"
    }
    else {
        New-Service -Name $service.Name -DisplayName $service.DisplayName -Description $service.Description `
            -BinaryPathName "`"$exe`"" -StartupType Automatic | Out-Null
        Write-Host "Installed service $($service.Name)"
    }

    # Restart after a crash: after 5 s, three times a day at most.
    sc.exe failure $service.Name reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
    Start-Service $service.Name
}

$rule = 'OxBox web app (TCP 5029)'
if (-not (Get-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $rule -Direction Inbound -Protocol TCP -LocalPort 5029 -Action Allow | Out-Null
    Write-Host "Added firewall rule '$rule'"
}

Write-Host ''
Write-Host 'OxBox is running. The first start takes a few seconds while it creates the database.'
Write-Host "  Web app:     http://$env:COMPUTERNAME`:5029"
Write-Host "  Box display: http://$env:COMPUTERNAME`:5029/kiosk/BOX-01"
Write-Host '  Simulator:   http://localhost:5080 (on this PC only)'
