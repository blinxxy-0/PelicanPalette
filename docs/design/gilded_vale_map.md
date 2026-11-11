# The Gilded Vale: the map (DRAFT, for approval)

1.4, step 1 of the build order. Decided so far (2026-11-03): terraced, with a river down to a lake; reached by Pam's bus. Everything else here is a proposal.

**The sketch:** `art/gilded_vale/layout_v1.png` (drawn by `tools/art/vale_layout.py`, in tile coordinates). It's a schematic for positions, not map art.

## At a glance
- **Size:** 96 by 76 tiles. About half of Pelican Town (130 by 110), a little bigger than the Beach (104 by 50). Every place is under a minute's walk from the bus.
- **Shape:** a bowl. Three terraces step down from the bus stop in the north to a lake in the south, and the far side of the lake rises again to the overlook. Woods all round the edge.
- **One way in:** the bus, like the Desert. No road on to anywhere else (room for one later).
- **Interiors:** seven (section 5).

## 1. The terraces, north to south

### The upper terrace: arriving, and the homes of people who work

| Place | Tiles (x, y) | Notes |
|---|---|---|
| **The bus** | 8 to 17, 2 to 5 | Pam's bus, parked at the end of the road from the west, as in the Desert |
| **The Vale's sign** | 24, 8 | Old and hand-painted; repainted after Lupini's 8-heart event |
| **The Vale House** | 26 to 38, 11 to 20 | Bea's inn: the first building you reach. A tea garden in front, tables out from spring to fall |
| **The pottery** | 46 to 56, 12 to 20 | Tobin's. The kiln chimney smokes on firing days. Its clay pit is on the riverbank below it (57, 25) |
| **The upper bridge** | 64, 17 | Over the river, to Lupini's side |
| **Lupini's atelier** | 75 to 86, 16 to 24 | On the bluff by the waterfall, looking south over the whole Vale. A small, overgrown garden |
| **The graveyard** | 3 to 12, 10 to 22 | The old Painter's clearing, up a trail from the bus road's turning circle into the western woods. A low stone wall and gate, seven graves (Bram's carved stones, his own plain one, Tobin's clay markers) with an urn at each, and a bench (built 2026-11-09, `tools/vale_graveyard.py`). The hidden easel moved to the Gilded Grove (section 8) |

### The middle terrace: the Vale's heart

| Place | Tiles | Notes |
|---|---|---|
| **The stairs down** | 30, 29 and 72, 31 | One from the Vale House, one from Lupini's side |
| **The Exhibition Hall** | 39 to 54, 31 to 38 | Built against the cliff, facing the plaza. Closed and dusty until Lupini's 8-heart event |
| **The plaza** | 36 to 56, 38 to 48 | Paved and round, benches, lamps, the noticeboard by the Hall's door |
| **The old tree** | 38 to 43, 39 to 44 | The Vale's landmark (section 3) |
| **The fountain** | 48 to 51, 42 to 45 | Small and old, still running |
| **Fern's cottage and seedhouse** | 10 to 24, 32 to 37 | Against the cliff, looking over the sculpture garden |
| **The sculpture garden** | 6 to 30, 37 to 49 | Overgrown statues among hedges and flowers; seasonal (section 4) |
| **The middle bridge** | 60, 42 | Plaza to the Old Studios |
| **The Old Studios** | 68 to 88, 34 to 39 | New: a row of three empty artists' studios, boarded up. A weathered sign by them lists who worked there. One opens for Lupini's 6-heart event, and stays open |

### The lower terrace: music, water, and the view

| Place | Tiles | Notes |
|---|---|---|
| **The amphitheatre** | 29 to 45, 49 to 59 | Built into the cliff: the stone seats **are** the drop between the terraces, stepping down to a stage. Overgrown until Emmett's 6-heart event |
| **Emmett's cottage** | 18 to 26, 54 to 60 | Beside the amphitheatre |
| **The wildflower meadow** | 6 to 28, 61 to 72 | Fern's. Forage, bees, the 2-heart event |
| **The riverside walk** | 53, 50 to 66, 56 | Stairs down from the plaza, along the river to the lake |
| **The lake** | 54 to 80, 55 to 71 | Fishing; the lower bridge over the inlet (64, 56) |
| **The overlook** | 88 to 95, 55 to 73 (built, 2026-11-08) | A bluff across the lake, one level high. The top is about 7 by 9 tiles, its rim on row 55. Steps climb its south face, from a trail along its foot (the game has no flight that climbs south through a rim). A bench at the north-west edge, the Town side bench facing west over the lake. The easels (x 90 to 92 on row 58, their painters' row; the box grows to start at row 53, and the woods at rows 53 to 54 behind them were thinned) are beside it, a plank shed with its door facing south (93 to 95, 61 to 64; right-click its front: three folded easels, the third never used), the Three Trees and a stump at the north-east (they can't be felled, and grow back if missing: `blinxxy.MuseArt_KeepTree`, `ValeGrounds.cs`), a picnic table at the bluff's foot (88 to 90, 72 to 74). Lupini's 10-heart event at dawn |

## 2. The river

- **Where it runs:** a **waterfall** in the north-east, into a pool below Lupini's atelier. Then south-west past the pottery, down a **cascade** over each cliff, and into the lake.
- **The waterfall, as built (2026-11-08):** `tools/vale_falls.py`, in the box x 74 to 89, y 0 to 13. A cliff runs along the map's top, x 76 to 87, seven rows tall (shading on row 7), with the river's four water columns (x 80 to 83) dropping over a notch in it. The fall is nine rows (spray, lip, falling water, foam), taller than the Town's seven. Its falling rows repeat, and each repeat starts its animation a few frames on, so the water runs on from the row above (16 new tile animations in the map's Waterfalls tileset). The pool below is a basin about x 78 to 85, y 8 to 12, with two boulders and two pebbles in it. The cliff has no corners to turn: both ends are the Town stairs' side walls, and woods hide them (a pine at (75, 4), the maple at (75, 1), the top edge's trees). The orchard (x 88 and east) is untouched. The sound is the Town falls' big waterfall (BrookSounds `82 8 5`).
- **Bridges:** three, one on each terrace. They're the only ways across, so the river splits the Vale into a west side (the inn, Fern, the amphitheatre) and an east side (Lupini, the Old Studios, the overlook).
- **Fishing (built 2026-11-08):** the river has the Forest's river fish and the lake the Mountain lake's, plus five fish of the Vale's own: the Claybank Loach (the river), the Cascade Trout (the cascade pools, in rain), the Falls Sculpin (the waterfall basin, at night), the Lakeblue Shiner and the rare Gilt Carp (the lake). Design: `docs/superpowers/specs/2026-11-08-vale-fish-design.md`; data: `[CP] Gilded Vale (dev)/data/vale_fish.json`; check: `museart_fishtest`.
- **To check:** which waterfall and cascade tiles the game has (the Mountain's lake, the Forest's river). The waterfall may need a small animated piece of our own.

## 3. The old tree (new idea, for approval)

The plaza's landmark is a huge old tree whose leaves turn **gold in fall**. That's where the Vale's name comes from: the artists who settled there called it the Gilded Vale after it.
- **Seasons:** blossom in spring, deep green in summer, gold in fall, bare and snowy in winter. A custom tree, four versions.
- **On Fall 7,** the Open House is held under it, at its most golden.
- **A paintable view** in every season.

## 4. Seasons

- **The ground, paths, cliffs, water and woods** come from the game's own outdoor tilesheets, so they change with the seasons by themselves (the concept sheet asks for strong seasons).
- **Our own pieces need four versions:** the old tree; the sculpture garden's statues (flowers in spring, vines in summer, leaves in fall, snow in winter, as the sheet describes); snow on the roofs in winter.
- **Lamps** on the paths and in the plaza light at dusk.

## 5. Interiors (seven)

| Interior | Size | Notes |
|---|---|---|
| **The Vale House** | Like the Saloon | The tea room and bar downstairs; Bea's rooms and guest rooms upstairs (you can stay the night) |
| **The pottery** | Like the Blacksmith | The workshop and kiln at the front, Tobin's rooms behind |
| **Lupini's atelier** | Like Leah's cottage | One big room, a north window, his nine canon paintings on the walls |
| **The Exhibition Hall** | Like the Community Center's main room | Dust sheets before Lupini's 8-heart event; then the Vale's gallery, with displays that change |
| **Fern's cottage** | Like Leah's | With the seedhouse as a glass room on one side |
| **Emmett's cottage** | Small | One room: instruments, maps, a bed |
| **One of the Old Studios** | Small | Empty, an easel and the unfinished painting (Lupini's 6-heart event); the backward painting |

All of them decoratable rooms, built the way Margot's were: straight walls, the game's wallpaper and flooring.

## 6. Getting there

- **The ticket:** at the Bus Stop's ticket machine, a question: the Desert or the Vale. **500g**, the same as the Desert (to decide).
- **When it opens:** after the bus is repaired, with a letter from Lupini after the Night Market (1.4 plan, section 2).
- **Arriving:** the bus drives in from the west and stops at the road's end, as in the Desert's arrival. Going home: talk to Pam at the bus.
- **With Central Station installed:** the Vale is a destination there instead, and our ticket question isn't shown.

## 7. Views to paint

**The eight painting spots,** each the paint rock Lupini wipes his brushes on. Each had a stool with his paint box beside it, which carried the action; the user didn't like the stool and kit, so they went (2026-11-11), and the rock is touched instead: with the old painters' guide in hand, it's found (`tools/vale_spots.py`; the painters' guide design, `docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md`, section 4). You paint the view with an easel of your own, the usual way.

| # | Spot | Stool, now gone (left foot) | Where |
|---|---|---|---|
| 1 | The Rooftops | 43, 4 | The high street, by its hedge |
| 2 | The North Light | 79, 16 | Lupini's garden, by the atelier |
| 3 | The Wildflower Field | 28, 65 | The meadow |
| 4 | The Lake Window | 90, 60 | Up on the overlook, over the lake |
| 5 | The Lost Parterre | 25, 44 | The sculpture garden, just inside its east gate, looking west down the hedges |
| 6 | The Falls | 72, 4 | The river's north bank, just west of the falls' cliff, looking east at the fall's lip (up on the bank above the fall, well away from the North Light's stool) |
| 7 | Under the Gilded Tree | 43, 66 | The tree's lawn, east of it, between the canopy and the lakeside sand |
| 8 | The Old Stage | 25, 53 | Under the cliff west of the amphitheatre, looking at it from the side: 3 tiles clear of its footprint, off the seats, the stage and the plaza (a skill may use the amphitheatre later) |

**And the hidden easel** in the Gilded Grove (section 8): the Vale from above, in gold light. One view, not four, since the Grove has no seasons.

They go into the painting record, and a portfolio page of their own, **The Vale** (to decide).

## 8. Where the discoveries go

The concept sheet's hidden things (its section 7), placed:

| Discovery | Where |
|---|---|
| A hidden easel overlooking the Vale | **The Gilded Grove,** a painting the Vale's first painters made of their favourite place, which you step inside: a small map of its own (`blinxxy.MuseArt_GildedGrove`), always in fall's gold light. The easel stands at its edge over a painted panorama of the Vale. You reach it only by the old painters' guide, once all eight spots are found: using the guide anywhere in the Vale asks "Step into the last page?", and a painted frame at the Grove's edge brings you back to the graveyard's gate |
| A painting hung backward | The open Old Studio |
| A weathered sign listing former studios | By the Old Studios, across the lane from the foot of Lupini's stairs (78-80, 43-45). Built 2026-11-08 with the studios' carved numerals (I, II, III) on their plaques and the signed house rules inside studio I: `tools/vale_studio_signs.py` |
| A sculpture almost hidden by plants | The sculpture garden's south-west corner, behind a hedge. **Built:** a head and shoulders in the hedge at (6 to 7, 44 to 45). It is the west statue's own head, between two bushes cut from the lower bed beside it. The head and the hedge's top are on Front, so you walk behind them; only the hedge's foot (6 to 7, 45) is solid. Examine it from (7, 44) for the message `vale.garden.hidden_statue`. All four seasons (the winter hedge keeps its snow caps) |
| A sketch tucked in a desk | The Vale House attic (Bea's 4-heart event) |
| A bench that gives Lupini a special line | The overlook: the bench at (89, 58), the Town side bench facing west over the lake. **Built:** a solid seat tile, as the Vale's other benches are. In game the farmer sits on it, and so do Lupini (Sundays from 2pm) and Emmett (Saturdays from 1pm): their sheets' frame 16, Alex's seated pose with their own heads (`tools/art/vale_sit_build.py`), drawn with `offset -16 24` so they sit mid-seat with their feet over its front edge (both seen in game, 2026-11-09) |
| A mural that changes with the story | The Exhibition Hall's west wall: faded, then repainted after Lupini's 8-heart event |
| An abandoned exhibition room | The Hall's back room, before it reopens |
| The old painters' guide to the viewpoints | The Vale House noticeboard, by the guest books. Built: take it from the board (`blinxxy.MuseArt_TakeGuide`); it names the eight spots (section 7) |

## 9. Building it (proposed 2026-11-03)

**How the terrain is painted, and every rule for it: `docs/terrain_painting.md`.** Read it before changing the Vale's ground, water, cliffs or stairs.

**No new ground tilesets.** The ground, paths, cliffs, water, waterfalls, trees, bushes and fences all come from the game's own sheets (`outdoorsTileSheet`, `outdoorsTileSheet2`, `Waterfalls`, `town`, `paths`, each in four seasons). That's most of the map, it matches the game's palette exactly, and the seasons come for free. A PixelLab tileset would look a little off next to the farmer and lose the seasons.

**New art only where the Vale is itself:**

| Piece | How |
|---|---|
| **Signature buildings:** the Vale House, the Exhibition Hall, Lupini's atelier, the pottery, Fern's cottage and glass seedhouse | PixelLab, the art shop's way: a vanilla building cropped from the game's maps as the style reference (Pro Flash `style_image`, or map-object inpainting on a crop of the Town for buildings up to 192px), 3 or 4 candidates each, then our refining pass (shingles, eaves, outlines as a darker shade of their neighbour) and a mockup in a vanilla scene at 3x |
| **Smaller buildings:** Emmett's cottage, the Old Studios | First try: built from the game's own house pieces on the `town` sheet, recoloured, so they cost nothing. PixelLab if they look copied |
| **The old tree** | One PixelLab render, then three recolours for the other seasons (Pixen edit keeps the shape) |
| **Statues, amphitheatre seats, the bus shelter, the sign** | Small: PixelLab objects, or drawn by script from vanilla stone tiles (like the shop's wall bins) |

**Progress (2026-11-03):**
- **The greybox is built** (`tools/build_vale.py` → `[CP] Gilded Vale (dev)/assets/GildedVale.tmx` (a Debug-only content pack, never released), location `blinxxy.MuseArt_GildedVale`; in a dev game, `museart_warp blinxxy.MuseArt_GildedVale 20 7`). It stamps the vanilla Town's own cliffs, stairs, river, waterfalls and bridges, so the seasons change by themselves (shots: `art/gilded_vale/greybox_seasons.png`). Every area is reachable from the bus, checked with `museart_route` in segments. Simplified for now: the river runs straight down x 56 to 64, the cliffs are straight, the lake has no banks, and there's no overlook bluff or amphitheatre yet.
- **The building guides** (`tools/art/vale_building_guides.py`) were redone after the user's review: front gables like the game's own houses, about 60% roof (`art/gilded_vale/buildings/guides_vs_vanilla.png`). Fern's seedhouse follows the farm's greenhouse (the user: use the game's own one as the reference). First candidates: `batch_v2.json`.

**Progress (2026-11-08):** three pieces patched onto the committed map, each by its own script that works on the map in place (`tools/vale_falls.py`, `vale_overlook.py`, `vale_sculpture.py`, on `build_vale.VALE_PIECES`, with `tools/vale_patch.py` under them and `tools/test_vale_pieces.py` guarding them).
- **The north-east waterfall:** a taller cliff and fall than the Town's, ending in woods (section 2).
- **The south-east overlook:** the bluff, its steps, bench, easels, shed and Three Trees (section 1).
- **The hidden sculpture:** in the garden's south-west corner (section 8).
- **Changes from the design,** as built: the steps climb the bluff's south face, not up from (82, 58). The bench is the Town side bench facing west, not north. The shed's door faces south. The picnic table is at the bluff's foot. The top is about 7 by 9 tiles with its rim on row 55. The bluff is one level high, not as high as the upper terrace.
- **Rebuilds and merges:** a rebuild keeps them: `build_vale.py` runs `VALE_PIECES` on the map after it writes it (though it doesn't reproduce the committed map itself). On a merge conflict in `GildedVale.tmx`, take ONE side's TMX together with ALL FOUR `<season>_GildedValeBuildings.png` from that same side, then re-run `python tools/vale_sculpture.py`, `vale_falls.py` and `vale_overlook.py` on it, each as `<script> "[CP] Gilded Vale (dev)/assets/GildedVale.tmx" <exports>`. Never mix one side's TMX with the other side's sheets (the pieces' slot numbers live in both). Don't hand-merge the tile data. Then run `python tools/test_vale_pieces.py` (after a merge of main, set `VALE_BASE` to main's side of the merge for `test_outside_boxes_unchanged`).
- **Seen in game, and not:** only Lupini was seen reaching the bench (Task 6, on a Sunday; he stands on the seat, there is no sit pose yet). Emmett's visit to the bench was not seen. Everything else was checked in a test copy in all four seasons.

**Progress (2026-11-09):** the clearing is the graveyard (section 1); the painters' guide finds the eight painting spots, all built (section 7); the hidden easel is in the Gilded Grove (section 8).

**The order:**
1. **Greybox** (no art, no generations): `tools/build_vale.py` writes the TMX from vanilla tiles, with plain boxes for the buildings. A debug warp in, then walk it in-game in all four seasons: terraces, stairs, bridges, distances.
2. **Building sizes** from the greybox, measured against vanilla (a house in town is about 8 to 10 tiles wide).
3. **Exteriors,** one at a time, candidates shown before choosing.
4. **Props and the tree.**
5. **Interiors,** from the game's interior tiles, as Margot's were.
6. **The bus route** and the dressing pass.

**Cost:** a Pro Flash building is 5 to 9 generations a candidate; four candidates for each of six buildings is about 150 to 200 generations. 3,569 were left on 2026-11-03 (resets 2026-11-26).
- **The rules:** vanilla palette and sizes; no props on walls or straddling edges; yard props well inside their plots; walls block along their whole face.
- **Checking:** the map shot (`museart_menu mapshot`) at each step, in all four seasons.

## 10. Open questions

1. **The layout:** as sketched, or move things?
2. **The old tree** as the Vale's landmark and the origin of its name?
3. **The Old Studios** (new): a row of three empty studios, one of them open, as the seventh interior?
4. **The amphitheatre built into the cliff,** its seats as the drop between terraces?
5. **The ticket:** 500g, like the Desert?
6. **A Vale portfolio page?** Only as a bonus outside the portfolio's count: a new counted page would un-finish completed books (the gold-leaf frame and the Summit scene, 1.3).
