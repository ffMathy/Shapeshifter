# Switches Windows to the light or dark mode (apps and the shell) with transparency effects on, as Settings >
# Personalisation > Colours does, and tells running windows, so the next app started sees the new mode.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('light', 'dark')]
    [string] $Mode
)

$ErrorActionPreference = 'Stop'
$key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
if (-not (Test-Path $key)) { New-Item -Path $key -Force | Out-Null }
$light = if ($Mode -eq 'light') { 1 } else { 0 }
Set-ItemProperty -Path $key -Name AppsUseLightTheme -Value $light -Type DWord
Set-ItemProperty -Path $key -Name SystemUsesLightTheme -Value $light -Type DWord
Set-ItemProperty -Path $key -Name EnableTransparency -Value 1 -Type DWord

# WM_SETTINGCHANGE "ImmersiveColorSet", as Settings broadcasts it. Only for windows already running: each capture starts a
# fresh process, which reads the registry, so a failed broadcast is a warning, not a failed pass.
try {
    Add-Type -Namespace Shapeshifter -Name Broadcast -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
public static extern System.IntPtr SendMessageTimeout(System.IntPtr hWnd, uint msg, System.UIntPtr wParam, string lParam, uint flags, uint timeout, out System.UIntPtr result);
'@
    $result = [UIntPtr]::Zero
    [Shapeshifter.Broadcast]::SendMessageTimeout([IntPtr]0xffff, 0x1A, [UIntPtr]::Zero, 'ImmersiveColorSet', 2, 5000, [ref] $result) | Out-Null
}
catch {
    Write-Warning "Could not broadcast the change of app mode: $_"
}
Start-Sleep -Seconds 2
Get-ItemProperty -Path $key | Select-Object AppsUseLightTheme, SystemUsesLightTheme, EnableTransparency | Format-List
