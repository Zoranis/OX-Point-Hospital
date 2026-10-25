<#
.SYNOPSIS
    Stops and removes the OxBox services and their firewall rule.

.DESCRIPTION
    Run it as administrator. The package folder, and the database in server\data, are kept;
    delete the folder yourself if the data is no longer needed.
#>
$ErrorActionPreference = 'Stop'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script as administrator (right-click PowerShell, "Run as administrator").'
}

foreach ($name in 'OxBox', 'OxBoxSimulator') {
    if (Get-Service $name -ErrorAction SilentlyContinue) {
        Stop-Service $name -Force
        sc.exe delete $name | Out-Null
        Write-Host "Removed service $name"
    }
}

Get-NetFirewallRule -DisplayName 'OxBox web app (TCP 5029)' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Write-Host 'Done. The data in server\data was kept.'
