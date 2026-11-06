# The rest of the suite: the tests that need a game of their own (married,
# CJB Cheats, each festival, two-player co-op), each on a fresh dev copy.
# Writes the PASS/FAIL lines to suite_extra_out.txt in the dev root.
. "$PSScriptRoot\common.ps1"
$out = "$DevRoot\suite_extra_out.txt"
"" | Out-File -Encoding utf8 $out
function Run($name, $dir, $steps) {
  "==== $name" | Out-File -Append -Encoding utf8 $out
  & "$PSScriptRoot\run_dev.ps1" -ModsDir $dir -Steps $steps -Pattern "\[PASS\]|\[FAIL\]|ERROR  Pelican" | Out-File -Append -Encoding utf8 $out
}
Run "married" "devmods_plain" @("museart_marry|5","museart_marriedtest|8")
Run "married life" "devmods_plain" @("museart_marry|5","museart_marriedlifetest|3","museart_warp Farm 64 15|5","museart_marriedlifetest farm|6")
Run "cjb" "devmods_plain" @("museart_cjbtest|3","museart_cjbtest click|2","museart_cjbtest check|3","museart_cjbtest|3","museart_cjbtest profession|6")
Run "fair" "devmods" @("museart_festival fall16|20","museart_contesttest|15")
Run "spirits eve" "devmods" @("museart_festival fall27|25","museart_festtest|12")
Run "jellies" "devmods" @("museart_festival summer28|25","museart_festtest|12")
Run "stall and winter star" "devmods" @("museart_stalltest|8","museart_stalltest check|5","museart_festtest gift|5")
"==== multiplayer" | Out-File -Append -Encoding utf8 $out
& "$PSScriptRoot\run_mp.ps1" -Steps @("H:museart_mptest host-setup|8","C:museart_mptest client|12","H:museart_mptest host-check|8","C:museart_mptest client-collect|10") -Pattern "\[PASS\]|\[FAIL\]|ERROR  Pelican" | Out-File -Append -Encoding utf8 $out
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'StardewModdingAPI.exe' -and $_.CommandLine -like "*$DevRoot*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
"Results in $out"
