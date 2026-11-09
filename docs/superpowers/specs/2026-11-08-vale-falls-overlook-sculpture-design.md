# The Gilded Vale: the waterfall, the overlook and the hidden sculpture (design)

2026-11-08. Three pieces of the Vale's outdoor map (`Maps/blinxxy.MuseArt_GildedVale`, `[CP] Gilded Vale (dev)/assets/GildedVale.tmx`) that the map doc (`docs/design/gilded_vale_map.md`) planned and nobody built. Agreed with the user in a brainstorming session and approved by them on 2026-11-08; the mockups shown are throwaway (scratchpad), shape and position only.

## Intent

Each piece is something the Vale's lore already talks about but the map doesn't show. The bar is that they look like they were always there: the game's own terrain rules, every season, nothing a resident's line contradicts.

**The user's standing rule for this work:** the right answer is the one that fits the lore best. Read the lore (the briefs, `docs/vale_events.md`, the Vale's `i18n/default.json`, the concept sheet `Downloads/Gilded_Vale_Design_Notes.md`) before choosing.

## Constraints (all three)

- **Terrain:** read `docs/terrain_painting.md` before painting anything, and follow it exactly. The user rejected the waterfall mockup because it ignored those rules ("a million awful sharp lines, random segments of yellow rock... it doesn't understand the multiple layers that go into a river structure"). Water shapes (section 5), the two-row north banks and the vertical bank cycle (section 7), cliffs by rule and waterfall pieces (section 8), shore turns by exact context (section 6), and the winter check (section 9) all apply.
- **Patch the committed TMX in place.** `build_vale.py` doesn't reproduce it. Write the changes as a script (in `tools/`, like `vale_yards.py` and `vale_feature_fixes.py`) that patches the committed map and can be re-run, and hook it into `build_vale.py` so a rebuild keeps it.
- **Stay out of other sessions' areas:** the west woods (x 0 to 18, y 8 to 28: the Painter's clearing) and the Old Studios (x 66 to 94, y 30 to 40). The standing vale-map-fixes session also edits this TMX, so keep edits contained to the three areas and expect a merge.
- **Props:** well inside their area, never on walls or straddling edges. Only a tall thing's bottom row is solid; its top goes on Front so you walk behind it. The ground painted round it stays underfoot (`vale_feature_fixes.py` FRONT/GROUND).
- **No PixelLab without asking.** The allowance is low (319 generations on 2026-11-07, resets 2026-11-26) and the user wants it kept for characters. If a piece can't be done from the game's own tiles, stop, say how many generations, and ask.
- **Testing:** never launch over the user's running game. Use a separate `--mods-path` copy under `.devgame/` and check the log's "Mods go here" line.

## 1. The waterfall (north-east, above Lupini's atelier)

**Why:** it's where the river comes into the Vale, and the lore leans on it:
- Emmett: "the wind comes down off the falls" of an evening, and "the river's got a better voice than me".
- The Hall's 'Spiral': "carved from river stone found below the falls".
- Lupini's 10-heart event (draft): "the sun comes up over the falls" from the overlook.
- The map doc: the atelier is "on the bluff by the waterfall".

Today the river just runs off the top edge at about x 80 to 84, then turns west along y 7 to 12 and south down x 60 to 63. The only falls are the two cascades over the terrace cliffs.

**Decided:**
- **One tall fall, as tall as the map allows.** A rim cliff across the north-east corner, from the top edge (y 0) down to about y 8. The river pours over it through a notch at about x 80 to 85. The fall is the river's own width (4 tiles of water, like the cascades).
- **The cliff's west end disappears into the woods** (trees already stand along the top edge at x 60 to 76) rather than turning a corner. **Its east end stops short of the old orchard** (`vale_decor.py`: fruit trees at (88,3), (92,3), (90,6), (88,9), (93,9), RESERVED (87,2,94,10)) and ends against its trees the same way, at about x 87. The orchard stays as it is. *(My call: it keeps everything already there; the falls only need the cliff either side of the notch.)*
- **A small basin at the foot,** not round. The river widens a couple of tiles each side under the fall, with water shapes the game has pieces for: straight banks, north-facing corners cut at 45 degrees, south-facing corners square. The river then leaves west along the foot of the cliff, joining today's river.
- **River stones** in the shallows and on the basin's banks (the 'Spiral' stone). Use the game's own pieces, not drawn blobs.
- **Spray:** the game's own foam and splash where the water lands. A soft drifting mist on top is wanted. Its form is the builder's call: tile animation, or a light effect in `ArtStudio/Vale/` (Debug only).
- **Sound:** the map's `BrookSounds` property gets a brook source at the falls.
- **Art:** the game's own waterfall pieces (the Town falls, Town x 91 to 97, y 0 to 6; the Waterfalls tiles are already animated in our tileset), made taller by repeating the falling-water rows, over a cliff built by our cliff rule. No PixelLab unless that can't be made to look right (then ask).

**Must keep working:** the atelier and its yard (the roof starts about y 14), Lupini's atelier painting spot (stool 79,16; rock 79,19), the upper bridge at about (64,22), the river's run west and south, and the clay pit. Every route in `vale_routes.py`.

## 2. The overlook (south-east, across the lake)

**Why:** it's the Vale's best view and the lore keeps sending people there:
- Lupini's introduction: "You'll want the view from the overlook, then the tea, then the view again".
- Lupini (Sun8): "Come and sit with me at the overlook sometime, early, before anyone's awake. That's the best light there is."
- The atelier's 'Three Trees': "They grow up by the overlook. He swears there used to be four."
- The Hall's 'Jade Hills' was "painted from the overlook".
- Lupini's 10-heart event (draft, `docs/vale_events.md` near line 280): at the overlook bench at dawn, two easels at the edge facing the Vale; "There's a third easel in the shed"; Sable comes up the path.
- The concept sheet: "a raised viewpoint overlooking the Vale, with a waterfall, bridge, or lake view".

Today the lake reaches x 84 between y 57 and 70; its east shore is at x 80 at y 58 and x 82 to 84 below. East of it is a strip of open ground: about x 81 to 89 at y 53 to 60, narrowing to x 85 to 89 from y 61 to 70. It holds the sand path from the stairs at (85,48 to 54), the picnic table (89,64) and Lupini's lake painting spot (stool 88,58; rock 88,61, where `reachable_spot()` moved them). Woods fill x 90 to 95 and y 72 to 75. The "overlook bench" in the schedules is a bench at (86,27) by the atelier.

**Decided:**
- **A raised bluff in the south-east corner,** across the lake from the terraces, where the plan the user approved put it. It covers about x 84 to 96, y 60 to 76, taking most of today's east and south woods, with its west side above the lake's east shore. A fringe of trees stays along the map's east and south edges, so the bluff still sits inside the woods that ring the Vale *(my call)*. It's reached by **stone steps** up from the lakeside path at its north-west, near the doc's (82,58).
- **The bench at the bluff's north-west corner,** facing north. The lake lies to its left and in front of it (the lake's north-east arm), with the terraces and the falls beyond. It is *the* overlook bench (the concept sheet's "bench that gives Lupini a special line").
- **The Three Trees** on the bluff, and **the stump of a fourth.**
- **A small shed** on the bluff: where the third easel comes from in his 10-heart event.
- **Room at the edge for the 10-heart scene:** two easels (three with Sable) in a row beside the bench, Lupini and the farmer at them, and a clear way up the steps for Sable.
- **Lupini's lake painting spot** moves up onto the bluff, near the edge looking over the lake (his Friday: "The lake at noon is a terrible subject... I paint it every Friday out of spite"). It's placed by `reachable_spot()`.
- **The picnic table** stays on the bluff or beside it.
- **Schedules:** Lupini's Sunday afternoon (`1400 ... 86 27 2`) and Emmett's Saturday (`1300 ... 86 27 2`) move to the new bench. Lupini's Friday (`900 ... 88 59 2`) moves to stand at the new lake spot. The bench at (86,27) stays as an ordinary bench. `vale_routes.py`'s "overlook bench" place moves to the new bench.

**Left to the builder (marked as my calls):**
- **Height: one level** (the middle terrace's height), not the map doc's "as high as the upper terrace". The lore only says raised; two levels in a 20 by 20 corner is a lot of cliff for nothing the top-down camera shows. Update the map doc to match.
- **How a raised bluff looks from the north.** The camera only sees faces that look south. So the bluff's north edge, toward the lake, reads as a rocky lip, and its west side as a side cliff. Learn both from the exported vanilla maps (Backwoods, Railroad, Mountain and Forest plateaus) the way the rest of the terrain was learned. Steps climbing south onto the lip need the same research. If the vanilla pieces can't do it, stop and ask about PixelLab (terrain doc section 1: "use PixelLab rather than forcing vanilla pieces").
- The exact outline, the trees' kinds and spots (oak/maple/pine markers on Paths; a stump as the game's own), the shed's art (from the game's own pieces if possible, as Emmett's cottage and the Old Studios were), and the trail from the south-east.

## 3. The hidden sculpture (the sculpture garden's south-west corner)

**Why:** the concept sheet's "a sculpture almost completely hidden by vegetation", placed by the map doc behind a hedge in the garden's south-west corner. The lore gives the reason. Fern's teacher "kept every hedge square for seventy years". He died eight years ago, and Fern lets the garden grow ("People want the gardens put back how they were. Hedges cut square... The garden's moved on."). So a hedge has grown right over a statue.

The garden today is the Lost Parterre, a painted feature (`build_vale.py` FEATURES "parterre"), inside stone walls at about x 5.5 to 28, y 39 to 47.5. The south-west corner pocket is open grass, only 2 by 2 tiles (x 6 to 7, y 44 to 45). It's boxed in by the west wall, the bottom wall (solid at y 46), the lower-left hedge bed (from x 8) and the west statue's plinth above it at (6,43).

**Decided:**
- **A stone figure of a gardener with shears,** carved by the old artists in honour of Fern's teacher, swallowed by a wild hedge in that corner pocket. Only its head (and perhaps the shears' tips) shows. The hedge he kept square has grown over him.
- **It isn't one of the statues** Fern's 10-heart path reveals (the woman with a book, the boy with a kite, the pair of dancers).
- **Right-click** reads a half-hidden inscription: a short line, in the Vale's lore voice, that names no one (the teacher has no name yet; don't invent one). Add the string to `i18n/default.json` like the interiors' lore lines, shown with the map's existing `Action Message` and `Strings/StringsFromMaps` pattern.
- **Art from the garden's own pieces:** the parterre's statue and hedge pixels, no PixelLab. **Seasons match the garden as built** *(my call)*: the spring art in spring, summer and fall, and in winter the same piece cut from the parterre's existing winter art (`features/winter/parterre_0.png`), so it's snowed on like its neighbours. The garden's greenery doesn't change with the seasons, by an earlier lesson ("Greenery never recolours... changing with the season looked wrong"), and this one statue shouldn't be the exception. The map doc's blossom, vines and leaves for the statues stay a whole-garden idea for later.
- **Collision:** only the hedge's foot and the statue's plinth are solid. The head and the hedge's top go on Front. It stays inside the 2 by 2 pocket, clear of the wall and plinth tiles.

**Must keep working:** the west statue (about 6.5,41.5 to 43.5) and the parterre's paths, Fern's schedule spots in the garden (check every `GildedVale x y` in `data/fern_schedule.json` against new solid tiles), and Fern's 4- and 10-heart scenes there (drafts).

## Done means

- All three built and checked in all four seasons: offline renders at 2 to 3x in sections against vanilla (terrain doc section 9), then in game with `museart_menu mapshot` in each season.
- `python tools/vale_routes.py` passes (0 blocked), with places added for the overlook bench, the basin's bank and the corner by the hidden sculpture.
- No schedule spot on a solid tile.
- `docs/design/gilded_vale_map.md` updated: section 1 (the overlook row, height one level), section 2 (the river and waterfall as built), section 8 (the hidden sculpture as built). Also the progress notes.
- `docs/terrain_painting.md` gets a short section on anything new learned (a taller waterfall, a raised plateau's lip and side cliff).
- Committed on this branch, our own files only. Ask the user before merging. Never release.
