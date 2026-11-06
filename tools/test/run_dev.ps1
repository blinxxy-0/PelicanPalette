# Restarts the dev copy of the game on one Mods folder under the dev root,
# rebuilds the mod into it, starts a throwaway farm, then sends each step
# ("command|seconds to wait"; ';' separates several commands in one step)
# through the mod's dev_commands.txt inbox. Prints the log lines matching
# -Pattern. Never touches the player's own game.
#   tools/test/run_dev.ps1 -Steps "museart_zoomtest|6" -Pattern "PASS|FAIL"
param([string[]]$Steps = @(), [string]$Pattern = "PASS|FAIL|ERROR  Pelican", [string]$ModsDir = "devmods")
. "$PSScriptRoot\common.ps1"
$dev = Join-Path $DevRoot $ModsDir
$inbox = "$dev\PelicanPalette\PelicanPalette\dev_commands.txt"
Stop-DevGame "$DevRoot\devgame.pid"
Set-Location $Repo
$b = dotnet build ArtStudio/ArtStudio.csproj "-p:GameModsPath=$dev"; if ($LASTEXITCODE -ne 0) { $b | Select-String " error " | Select-Object -First 5; throw "Build failed" }
$id = & "$Repo\tools\launch_test.ps1" -ModsPath $dev
$id | Out-File "$DevRoot\devgame.pid"
Start-Sleep 4
Set-Content $inbox "museart_devgame"
Start-Sleep 20
foreach ($step in $Steps) {
    $parts = $step.Split('|')
    Set-Content $inbox ($parts[0] -replace ';', "`n")
    Start-Sleep ([int]$parts[1])
}
Show-LogLines (Get-DevLog $ModsDir) $Pattern
