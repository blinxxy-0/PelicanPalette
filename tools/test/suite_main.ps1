# The main test suite: one throwaway farm (Mods folder devmods), every test
# command in turn with the seconds to wait after it. Writes the PASS/FAIL
# lines to suite_main_out.txt in the dev root.
. "$PSScriptRoot\common.ps1"
$out = "$DevRoot\suite_main_out.txt"
$steps = @(
 "museart_selftest|12","museart_selftest sell|3","museart_selftest sold|4",
 "museart_shoptest|10","museart_shoptest premium|4",
 "museart_techtest|25","museart_zoomtest|6","museart_modetest|8","museart_modetest restore|3",
 "museart_frametest|20",
 "museart_devscene|8","museart_varnishtest|10",
 "museart_palettetest|6","museart_palettetest check|3",
 "museart_painttest|8","museart_painttest check|3",
 "museart_knowtest|6","museart_knowtest close|3","museart_knowtest check|5",
 "museart_recordtest|8","museart_portfoliotest|10","museart_blanktest|8","museart_portraittest|15",
 "museart_creaturetest|15","museart_animaltest|10","museart_oldtest|15","museart_collectiontest|15",
 "museart_wingtest|15","museart_ordertest|10","museart_booktest|15","museart_juliantest|15",
 "museart_margottest|35","museart_boardtest|10","museart_livingtest|12","museart_npctest|35","museart_outfittest|10",
 "museart_watchtest|6","museart_watchtest check|5",
 "museart_trapdoortest|6","museart_trapdoortest down|5","museart_trapdoortest check|4",
 "museart_automatetest|6","museart_automatetest check|4","museart_automatetest cured|4",
 "museart_gallerytest record|6","museart_gallerytest consign|6","museart_warp Farm 64 15|5","museart_gallerytest view|8",
 "museart_polytest|10","museart_updatecard|4")
& "$PSScriptRoot\run_dev.ps1" -ModsDir devmods -Steps $steps -Pattern "\[PASS\]|\[FAIL\]|ERROR  Pelican" | Out-File -Encoding utf8 $out
"Results in $out"
