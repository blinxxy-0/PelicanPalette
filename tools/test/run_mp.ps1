# Two-player dev game: restarts the two dev copies (Mods folders devmods and
# devmods_mp under the dev root, or -HostMods and -GuestMods), rebuilds into
# both, hosts on one and joins with the other, then sends each step
# ("H:command|seconds" for the host, "C:command|seconds" for the farmhand).
# Never touches the player's game.
param([string[]]$Steps = @(), [string]$Pattern = "PASS|FAIL|ERROR  Pelican|Hosting|Joined|Joining",
    [string]$HostMods = "devmods", [string]$GuestMods = "devmods_mp")
. "$PSScriptRoot\common.ps1"
$roles = @{ H = $HostMods; C = $GuestMods }
foreach ($r in $roles.Keys) { Stop-DevGame "$DevRoot\devgame_$r.pid" }
Stop-DevGame "$DevRoot\devgame.pid"
Set-Location $Repo
foreach ($r in $roles.Keys) {
    $b = dotnet build ArtStudio/ArtStudio.csproj "-p:GameModsPath=$DevRoot\$($roles[$r])"
    if ($LASTEXITCODE -ne 0) { $b | Select-String " error " | Select-Object -First 5; throw "Build failed" }
}
$start = Get-Date
$x = @{ H = -1920; C = -1100 }      # window positions (the left monitor)
foreach ($r in @("H", "C")) {
    $id = & "$Repo\tools\launch_test.ps1" -ModsPath "$DevRoot\$($roles[$r])" -MonitorX $x[$r]
    $id | Out-File "$DevRoot\devgame_$r.pid"
    Start-Sleep 3
}
function Send($role, $cmd) { Set-Content "$DevRoot\$($roles[$role])\PelicanPalette\PelicanPalette\dev_commands.txt" ($cmd -replace ';', "`n") }
Start-Sleep 4
Send "H" "museart_mp host"
Start-Sleep 20
Send "C" "museart_mp join"
Start-Sleep 30
foreach ($step in $Steps) {
    $role, $rest = $step.Split(':', 2)
    $cmd, $wait = $rest.Split('|')
    Send $role $cmd
    Start-Sleep ([int]$wait)
}
foreach ($r in @("H", "C")) {
    $log = Get-DevLog $roles[$r] $start
    "---- $(if ($r -eq 'H') { 'host' } else { 'farmhand' }) ($($log.Name)) ----"
    Show-LogLines $log $Pattern
}
