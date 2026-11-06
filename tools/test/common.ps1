# Shared by the test scripts: where the repository is, and where the dev
# copies of the game keep their Mods folders, PID files and suite output.
# The dev root defaults to .devgame in the repository (ignored by git); set
# PELICAN_DEV_ROOT to keep it somewhere else. See docs/development.md for
# which mods go in each Mods folder.
$Repo = (Resolve-Path "$PSScriptRoot\..\..").Path
$DevRoot = if ($env:PELICAN_DEV_ROOT) { $env:PELICAN_DEV_ROOT } else { Join-Path $Repo ".devgame" }
New-Item -ItemType Directory -Force $DevRoot | Out-Null

# Stops a dev copy by its recorded PID, but only if it's SMAPI running on a
# Mods folder under the dev root (never the player's own game).
function Stop-DevGame([string]$PidFile) {
    $old = Get-Content $PidFile -ErrorAction SilentlyContinue
    if (-not $old) { return }
    $p = Get-CimInstance Win32_Process -Filter "ProcessId=$($old.Trim())"
    if ($p -and $p.Name -eq 'StardewModdingAPI.exe' -and $p.CommandLine -like "*$DevRoot*") {
        Stop-Process -Id $old.Trim() -Force; Start-Sleep 2
    }
}

# The newest SMAPI log written by the dev copy on the given Mods folder
# (SMAPI names the Mods path in the log's first lines).
function Get-DevLog([string]$ModsDir, [datetime]$Since = [datetime]::MinValue) {
    Get-ChildItem "$env:APPDATA\StardewValley\ErrorLogs" -Filter "SMAPI-latest*" |
        Where-Object { $_.LastWriteTime -gt $Since } |
        Sort-Object LastWriteTime -Descending |
        Where-Object { (Get-Content $_.FullName -TotalCount 3) -match ("\\" + [regex]::Escape($ModsDir) + "\s*$") } |
        Select-Object -First 1
}

function Show-LogLines($Log, [string]$Pattern) {
    if (-not $Log) { "(no log found)"; return }
    Select-String -Path $Log.FullName -Pattern $Pattern | Where-Object { $_.Line -notmatch 'SpaceCore' } |
        ForEach-Object { $_.Line.Substring(0, [Math]::Min(260, $_.Line.Length)) }
}
