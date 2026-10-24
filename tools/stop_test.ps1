# Stops only the test game started by launch_test.ps1 (never the user's modpack session).
$pidFile = "$env:TEMP\museart_test_pid.txt"
if (-not (Test-Path $pidFile)) { "no test game recorded"; exit 0 }
$id = [int](Get-Content $pidFile | Select-Object -First 1)
$p = Get-CimInstance Win32_Process -Filter "ProcessId=$id"
if ($p -and $p.Name -eq 'StardewModdingAPI.exe' -and $p.CommandLine -notmatch 'curseforge') {
    Stop-Process -Id $id -Force -Confirm:$false
    "stopped $id"
} else { "test game $id not running" }
Remove-Item $pidFile -Confirm:$false
