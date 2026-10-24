# Launches a test copy of Stardew (SMAPI, the game's own Mods folder — never
# the user's CurseForge modpack) and moves its window to the left monitor.
# Writes the process ID to stdout so the caller can stop exactly this one.
param([int]$MonitorX = -1920, [int]$MonitorY = 0, [string]$ModsPath = '')

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley'
# -ModsPath runs a separate dev copy (e.g. while the user has the test game open).
$argList = if ($ModsPath) { @('--mods-path', "`"$ModsPath`"") } else { @() }
$proc = if ($argList) { Start-Process -FilePath "$game\StardewModdingAPI.exe" -WorkingDirectory $game -ArgumentList $argList -PassThru }
        else { Start-Process -FilePath "$game\StardewModdingAPI.exe" -WorkingDirectory $game -PassThru }

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win {
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hgt, uint flags);
}
'@

for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 1
    $proc.Refresh()
    if ($proc.MainWindowHandle -ne [IntPtr]::Zero -and $proc.MainWindowTitle -like 'Stardew Valley*') {
        # SWP_NOSIZE | SWP_NOZORDER
        [Win]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::Zero, $MonitorX + 320, $MonitorY + 180, 0, 0, 0x0001 -bor 0x0004) | Out-Null
        break
    }
}
$proc.Id
