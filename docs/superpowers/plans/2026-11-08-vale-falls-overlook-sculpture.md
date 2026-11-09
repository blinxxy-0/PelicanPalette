# The Vale's waterfall, overlook and hidden sculpture: implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the three unbuilt pieces of the Gilded Vale's outdoor map (the north-east waterfall and basin, the south-east overlook bluff, the hidden sculpture in the parterre's south-west corner) by patching the committed TMX in place.

**Architecture:** One re-runnable patch script per piece in `tools/`, each with `patch_file(tmx_path, exports)` that loads the committed map, lays only its own box, and saves it. A shared module, `tools/vale_patch.py`, does the loading and saving, and repaints terrain inside a box using the same painters `build_vale.py` uses. `build_vale.py` calls the three patches on the map it has just written, so a rebuild keeps them.

**Tech Stack:** Python 3 with Pillow (no pytest on this machine: tests are plain `python` scripts with `assert`), Tiled TMX, Content Patcher, SMAPI dev game.

**Spec:** `docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md` (read it first; this plan argues from it). Also read `docs/terrain_painting.md` in full before Task 3.

## Global Constraints

- Terrain follows `docs/terrain_painting.md` exactly: water shapes (section 5), shore turns by context (6), two-row north banks and the vertical bank cycle (7), cliffs by rule and waterfall pieces (8), every season checked, winter especially (9).
- Patch the committed TMX in place. `build_vale.py` doesn't reproduce it, so never rebuild the committed map to get these pieces in.
- Stay out of the west woods (x 0-18, y 8-28) and the Old Studios (x 66-94, y 30-40), including the Old Studios sign at x 77-81, y 42-46 (another branch's work). The vale-map-fixes session also edits this TMX: keep edits inside the three boxes below and expect a merge.
- Props stay well inside their area, never on walls or straddling edges. Only a tall thing's bottom row is solid; its top goes on Front so you walk behind it, and the ground painted round it stays underfoot.
- No PixelLab without asking. If a piece can't be made from the game's own tiles, stop, say roughly how many generations it needs, and ask the user (the allowance is low and kept for characters).
- Never launch a game over the user's running game. Test in a separate `--mods-path` copy under `.devgame/` and check the log's "Mods go here" line.
- Commit only this work's files, on branch `claude/infallible-jang-a66ee9`. Never release. Ask the user before merging.
- Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

**The three boxes** (inclusive tile rectangles; the map is 96 x 76):
- `FALLS_BOX = (74, 0, 89, 13)`: the rim cliff, the fall and the basin. Stops above the atelier's roof (y 14).
- `OVERLOOK_BOX = (80, 54, 95, 75)`: the bluff, its steps, the lake's east shore below it.
- `SCULPTURE_BOX = (6, 43, 7, 45)`: the 2 x 2 corner pocket (x 6-7, y 44-45) and the tile above it, (7,43), from which you reach it.

## Review Focus

1. **Winter.** Town-sheet tiles on Back go blank white or a different blue in winter, and cliff-shading variants go plain. The new ground must use only pieces that survive winter. Test: `test_no_town_sheet_on_back` (Task 1, run from Task 2 on).
2. **Walking behind tall things.** The farmer must pass behind the hidden statue's head and the shed's roof, and never be stopped by a top. Test: `test_sculpture_feet_only` (Task 2), `test_overlook_feet_only` (Task 5).
3. **Re-running a patch.** Running a patch a second time, or a rebuild followed by the patches, must not stack a second copy (new sheet slots appended again, trees doubled). Test: `test_idempotent` (Task 1, extended by each piece).
4. **Residents left standing in the wrong place.** A moved bench or painting spot must not leave a schedule pointing at a solid tile, water or an unreachable spot. Test: the schedule check added to `vale_routes.py` (Task 1).
5. **Other sessions' areas.** Nothing outside the three boxes changes, except the map properties (`BrookSounds`) and this work's TileData objects. Test: `test_outside_boxes_unchanged` (Task 1).

---

### Task 1: Exports, the shared patch module and the checks

**Files:**
- Create: `tools/vale_patch.py`
- Create: `tools/test_vale_pieces.py`
- Modify: `tools/vale_routes.py` (add the schedule check)
- Modify: `tools/build_vale.py:268` (the hook after `write(...)`)

**Interfaces:**
- Produces, in `tools/vale_patch.py`:
  - `class ValeMap` with `text: str`, `g: dict[str, list[list[int]]]` (every tile layer, rows of gids), `props: dict[tuple[str, int, int], dict[str, str]]` (TileData objects by layer, x, y), and `map_props: dict[str, str]`.
  - `load(tmx_path: str) -> ValeMap` and `save(m: ValeMap, tmx_path: str) -> None`. `save` rewrites the layers' CSV, the map `<properties>`, and the TileData object groups exactly as `vale_yards._patch` writes them (`newline="\n"`).
  - `class Ctx` with `exports: str`, `assets: str`, `table` (a `vale_terrain.Table`), `tget(layer: str, x: int, y: int) -> int` (the Town export's tiles) and `paths_first: int`; `context(tmx_path: str, exports: str) -> Ctx`.
  - `corner_grid(g, ground: dict) -> list[list[str]]`: the (H+1) x (W+1) corner terrain (`G`, `D`, `S`, `W`) read back from each tile's `(Back, Buildings)` stack through `ground` (`build_vale.GROUND` after `Table.corners` is loaded). Where a stack isn't known, take its corners from the known neighbours.
  - `repaint_terrain(m: ValeMap, V, box: tuple[int, int, int, int], ctx: Ctx) -> None`: runs `build_vale.paint_shores`, `vertical_banks` and `bank_depth` on a copy of the whole grid with corners `V`, then copies back only the tiles inside `box` (Back and Buildings).
  - `BOXES = {"falls": FALLS_BOX, "overlook": OVERLOOK_BOX, "sculpture": SCULPTURE_BOX}` with the values above.
- Produces, in `tools/build_vale.py`: `VALE_PIECES: list[str]` (module names, empty for now). After `write(...)`, `main()` calls `importlib.import_module(name).patch_file(tmx_path, exports)` for each.

- [ ] **Step 1: Gather the vanilla exports into one folder**

The exports are split over three dev copies. Copy into `C:/Users/blinx/Desktop/MuseStardew/.devgame/exports_vale/` (`.devgame/` is gitignored):
- `Town.tmx` and its `spring_*` sheets from `.devgame/devmods/PelicanPalette/PelicanPalette/exports/`
- `Backwoods.tmx`, `Forest.tmx`, `Mountain.tmx`, `Railroad.tmx`, `Woods.tmx` and the sheets from `.devgame/devmods_doors/PelicanPalette/PelicanPalette/exports/`

Then make one render folder per season, `.devgame/exports_vale/sheets_<season>/`. Each holds that season's `Maps_<season>_<sheet>.png` (from `devmods_mapfix`, which has all four seasons) renamed `spring_<sheet>.png`, plus our own `<season>_GildedValeBuildings.png` and `<season>_GildedValeRoad.png` renamed `spring_*`.

Check: `python tools/render_tmx.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx" <scratchpad>/w.png --sheets .devgame/exports_vale/sheets_winter --region 56 0 40 20 --scale 2` writes a snowy render of the north-east.

- [ ] **Step 2: Write the failing checks in `tools/test_vale_pieces.py`**

Run as `python tools/test_vale_pieces.py`, with `VALE_EXPORTS` set to the folder above. Each `test_*` function runs in turn and the script exits non-zero on the first failure. `BASE = "e2a31f5"` is the branch's starting commit.

```python
def test_round_trip():
    # load then save, unchanged, gives the committed file byte for byte
    assert saved_bytes(load(TMX)) == open(TMX, "rb").read()

def test_outside_boxes_unchanged():
    old, new = load_at(BASE), load(TMX)
    for layer in new.g:
        for x, y in all_tiles():
            if not in_any_box(x, y):
                assert old.g[layer][y][x] == new.g[layer][y][x], (layer, x, y)
    assert {k: v for k, v in old.props.items() if not in_any_box(k[1], k[2])} == \
           {k: v for k, v in new.props.items() if not in_any_box(k[1], k[2])}

def test_no_town_sheet_on_back():
    # town sheet gids 2040-4343 on Back go white or blue in winter
    new, old = load(TMX), load_at(BASE)
    for name in ("falls", "overlook"):
        for x, y in box_tiles(BOXES[name]):
            v = new.g["Back"][y][x]
            assert not 2040 <= v < 4344 or old.g["Back"][y][x] == v, (name, x, y, v)   # new ground only

def test_idempotent():
    # every piece in build_vale.VALE_PIECES, run twice on a scratch copy of the
    # assets folder, leaves the TMX and the four season sheets byte-identical
    for name in VALE_PIECES:
        once = run_piece_on_copy(name, times=1)
        twice = run_piece_on_copy(name, times=2)
        assert once == twice, name

def test_routes():
    out = run(["python", "tools/vale_routes.py", TMX])
    assert out.rstrip().endswith("0 blocked"), out
```

- [ ] **Step 3: Add the schedule check to `vale_routes.py`**

After the places, read every `GildedVale <x> <y>` in `[CP] Gilded Vale (dev)/data/*_schedule.json`. Print `ok` or `BLOCKED` for each, using the same test as the places: not solid, not water, and reachable from the bus stop. Count failures into the same "N blocked" total. A schedule spot that is meant to be a chair or bench seat, such as the bench Lupini sits on, counts as reachable if a neighbouring tile is reachable.

- [ ] **Step 4: Run the checks to see what fails**

Run: `python tools/test_vale_pieces.py`
Expected: `test_round_trip` FAILS (`vale_patch` doesn't exist yet). The others are untested so far.

- [ ] **Step 5: Implement `tools/vale_patch.py` and the `VALE_PIECES` hook**

Loading and saving follow `vale_yards._patch` and `vale_feature_fixes.span_in`. `context()` builds `vale_terrain.Table(exports)` and loads `build_vale.GROUND` exactly as `paint_ground` does (`table.corners` plus `COBBLE_PIN`).

- [ ] **Step 6: Prove `repaint_terrain` is safe on ground that hasn't changed**

Add `test_repaint_unchanged`: `repaint_terrain(m, corner_grid(m.g, GROUND), box, ctx)` over two stretches of existing bank, the river's straight run (58, 30, 66, 36) and the lake's east shore (78, 56, 86, 66), changes no tile.

If it does change tiles, find out why before going on: the committed map was painted from the same rules, so a difference means `corner_grid` read something wrong. Fix `corner_grid` until it changes nothing; never loosen the test.

- [ ] **Step 7: Run every check**

Run: `python tools/test_vale_pieces.py` and `python tools/vale_routes.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx"`
Expected: all pass and `0 blocked` (`test_idempotent` passes trivially, since there are no pieces yet). If a schedule spot is already blocked today, fix that spot and say so in the commit.

- [ ] **Step 8: Commit**

```bash
git add tools/vale_patch.py tools/test_vale_pieces.py tools/vale_routes.py tools/build_vale.py
git commit -m "Gilded Vale: a shared in-place map patcher, and checks for schedule spots and untouched ground"
```

---

### Task 2: The hidden sculpture

**Files:**
- Create: `tools/vale_sculpture.py`
- Modify: `tools/build_vale.py` (append `"vale_sculpture"` to `VALE_PIECES`)
- Modify: `tools/vale_feature_fixes.py` (only if the pocket's existing Back2 tiles must be listed in SOLID/FRONT to stay consistent; prefer not)
- Modify: `[CP] Gilded Vale (dev)/i18n/default.json`, `[CP] Gilded Vale (dev)/content.json` (the `Strings/StringsFromMaps` entries)
- Modify: `tools/vale_routes.py` (PLACES)
- Writes: the GildedVale.tmx and the four `<season>_GildedValeBuildings.png` sheets
- Test: `tools/test_vale_pieces.py`

**Interfaces:**
- Consumes: `vale_patch.load/save/context`, `BOXES["sculpture"]`.
- Produces: `vale_sculpture.patch_file(tmx_path: str, exports: str) -> None`. The message key is `blinxxy.MuseArt_Vale.garden.hidden_statue`, with i18n key `vale.garden.hidden_statue`.

- [ ] **Step 1: Write the failing tests**

```python
def test_sculpture_feet_only():
    m = load(TMX)
    feature = lambda v: 20000 <= v < 30000
    assert all(feature(m.g["Buildings"][45][x]) for x in (6, 7))      # the feet: solid
    assert any(feature(m.g["Front"][44][x]) for x in (6, 7))           # the head and hedge top: over you
    assert not m.g["Buildings"][44][6] and not m.g["Buildings"][44][7] # nothing solid up there
    assert m.props[("Buildings", 7, 45)]["Action"] == "Message blinxxy.MuseArt_Vale.garden.hidden_statue"
```

Also add the place `"hidden sculpture": (7, 44)` to `vale_routes.PLACES`: you stand behind the head, reached from (7,43), and click the foot below.

- [ ] **Step 2: Run them to see them fail**

Run: `python tools/test_vale_pieces.py`
Expected: FAIL in `test_sculpture_feet_only`.

- [ ] **Step 3: Implement `tools/vale_sculpture.py`**

The art is made from the committed sheets' own pixels, using the stacked-tile reading of `vale_feature_fixes._patch` (`whole`, `region`):
- **The statue's head:** the top 16 px of the west statue (the region at x 6-7, y 41-43), so its head and shoulders show on row 44.
- **The hedge:** the hedge bed's leaf pixels only (x 8-11, y 44-45; a colour mask that keeps the greens and drops path, soil and shadow, as in the approved mockup `sculpture_v2.png`). They fill rows 44-45 round and over the statue, so the statue reads as swallowed.
- **Seasons:** the spring art in the spring, summer and fall sheets. The winter art is the same cut from the winter sheet's tiles (the parterre's winter art, already in `winter_GildedValeBuildings.png`), so it's snowed on like its neighbours.

New art goes in new slots at the sheets' end, the way `_patch` adds them, and the tileset's `tilecount` and image height are updated. **To stay re-runnable:** if the pocket's Front or Buildings tiles are already slots this script added (feature gids past the sheets' original end), paint over those slots instead of adding more.

Layers: the feet on Buildings at (6,45) and (7,45); the tops on Front at (6,44) and (7,44); the grass under them stays on Back2. The `Action` TileData is on Buildings at (6,45) and (7,45).

Inscription copy (i18n `vale.garden.hidden_statue`): `Carved into the plinth, mostly under leaves: "For the one who kept the hedges square."` Add the `Strings/StringsFromMaps` entry next to the hall's in `content.json`.

- [ ] **Step 4: Run the patch and every check**

Run: `python tools/vale_sculpture.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx" .devgame/exports_vale`, then `python tools/test_vale_pieces.py` and `vale_routes.py`.
Expected: all pass; `0 blocked`. `hidden sculpture (7, 44)` is ok, and Fern's spots (17,44), (14,42) and (25,43) are ok.

If (7,44) can't be reached from (7,43), stop and tell the user. Opening the pocket means moving the west statue's plinth or the hedge bed, and that's their call.

- [ ] **Step 5: Render the four seasons and look**

For each season, run `render_tmx.py ... --sheets .devgame/exports_vale/sheets_<season> --region 3 38 12 10 --scale 4 --grid`. Compare with the spring mockup. The statue must read as a head in a hedge, with no path or soil pixels pasted in, and in winter snow lies on the hedge like on its neighbours. Send the four renders, side by side, to the user with SendUserFile.

- [ ] **Step 6: Commit**

```bash
git add tools/vale_sculpture.py tools/build_vale.py tools/vale_routes.py tools/test_vale_pieces.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx" "[CP] Gilded Vale (dev)/assets/"*_GildedValeBuildings.png "[CP] Gilded Vale (dev)/i18n/default.json" "[CP] Gilded Vale (dev)/content.json"
git commit -m "Gilded Vale: a gardener's statue swallowed by the hedge in the parterre's south-west corner"
```

---

### Task 3: The waterfall and its basin

**Files:**
- Create: `tools/vale_falls.py`
- Modify: `tools/build_vale.py` (append `"vale_falls"` to `VALE_PIECES`)
- Modify: `tools/vale_routes.py` (PLACES)
- Writes: the GildedVale.tmx
- Test: `tools/test_vale_pieces.py`

**Interfaces:**
- Consumes: `vale_patch` (all of it), `build_vale.cliff`'s constants (`CLIFF_LIP`, `CLIFF_FACE`, `CLIFF_FACE_VARIANT`, `CLIFF_SHADE`), `build_vale.FALLS`, `WATERFALLS`, `OUTDOORS2`, `SHORE_DECOR`, `LILY_PADS`.
- Produces: `vale_falls.patch_file(tmx_path: str, exports: str) -> None`, and the constants `NOTCH_X = 80` (the fall's first water column; water x 80-83, the river's width today) and `BASIN_V = (78, 6, 86, 12)` (the basin's water corners).

- [ ] **Step 1: Read the vanilla stacks before painting**

Dump and render, side by side, these vanilla spots: the Town falls (Town x 89-97, y 0-14) and a tall Mountain cliff face. From them, decide three things and write them as comments at the top of `vale_falls.py`:
- How a cliff taller than our six rows repeats its face. Expected: the middle face rows repeat in the column-parity pattern.
- Which falling-water rows of `FALLS` repeat to make the fall taller, and which stay as the top lip and the foam at the foot.
- How a cliff ends against woods, with no corner turned. Expected: the cliff's end columns as `stairs()` takes them (Town x 18 and x 23), with trees in front.

- [ ] **Step 2: Write the failing tests**

```python
def test_falls_laid():
    m = load(TMX)
    assert any(m.g["Front"][y][x] in WATERFALLS or m.g["AlwaysFront"][y][x] in WATERFALLS
               for x in range(78, 86) for y in range(0, 8))                # falling water over the notch
    assert "82 8 0" in m.map_props["BrookSounds"] and "60 3 0" in m.map_props["BrookSounds"]
    for x in range(76, 88):                                                # the rim: solid all the way across
        assert any(m.g["Buildings"][y][x] for y in range(0, 7)), x
```

Add the place `"falls basin bank": (82, 13)` (sand below the basin, above the atelier's roof).

- [ ] **Step 3: Run them to see them fail**

Expected: FAIL in `test_falls_laid` and in `vale_routes` (the place is water or bank today).

- [ ] **Step 4: Implement `tools/vale_falls.py`**

In order:
1. **The corners.** Read `corner_grid`. Set the basin's corners to `W` over `BASIN_V`. Keep the river's run west along y 8-12. The basin's sides are straight with square south-facing corners (section 5); its north side is the cliff foot, with water under the face as in `cliff(keep_water=True)`.
2. **The ground.** Call `repaint_terrain(m, V, FALLS_BOX, ctx)`.
3. **The rim cliff.** Lay it by the cliff rule across x 76-87, from y 0 down to its shading at about y 7, taller by the repeat from Step 1. Its west end disappears into the trees at x 74-76; add oak/pine markers on Paths in front of the end if the woods there are thin. Its east end stops against the orchard's trees (`vale_decor.py` RESERVED (87,2,94,10) is not entered).
4. **The fall.** Lay `waterfall()`'s pieces over the notch, at `x = NOTCH_X - 2` (the same offset as the cascades: water x 60-63 has its falls at 58). Repeat the falling rows down to the basin.
5. **River stones.** Put 3 to 5 of the game's own shore rocks (from `SHORE_DECOR`, never `LILY_PADS`, read off vanilla where they sit in the shallows) in the basin's shallows and on its banks, at least one tile apart.
6. **Spray.** The falls' own foam at the foot. Add mist only if the render shows the foot bare; if so, use the vanilla falls' foam rows on AlwaysFront first, before any light effect.
7. **Sound.** Set `m.map_props["BrookSounds"]` to `"60 3 0 82 8 0"`. Check the game's `AmbientLocationSounds` constants in the game DLL. If it has a waterfall sound type, use it for the falls source instead of `0` and update the test.

Run the patch twice: the second run must change nothing (`test_idempotent` covers this).

- [ ] **Step 5: Run every check**

Expected: all pass; `0 blocked`. These stay ok: the atelier door (75,24), Lupini's garden (86,18), the clay pit (51,24), and the atelier painting spot, stool (79,16) and rock (79,19).

- [ ] **Step 6: Render the four seasons and audit at 3x**

For each season, run `render_tmx.py --region 56 0 40 20 --scale 3 --grid`, and also the Town falls at the same zoom. Check against section 9: no hard lines or stray rock, banks two rows deep where the land is above the water, the bank cycle down the vertical sides, and nothing white or a different blue in winter. Fix and repeat until clean, then send the four renders to the user.

- [ ] **Step 7: Commit**

```bash
git add tools/vale_falls.py tools/build_vale.py tools/vale_routes.py tools/test_vale_pieces.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx"
git commit -m "Gilded Vale: the river comes in over a tall waterfall into a stone basin above the atelier"
```

---

### Task 4: How a raised bluff looks from the north (research, then a test strip)

**Files:**
- Create: `tools/vale_overlook.py` (the edge-laying functions only, for now)
- Modify: `docs/terrain_painting.md` (a short section: the bluff's lip, its side cliff and its steps)

**Interfaces:**
- Produces, in `tools/vale_overlook.py`:
  - `lay_lip(m: ValeMap, ctx: Ctx, x0: int, x1: int, y: int) -> None`: the bluff's north edge, toward the lake.
  - `lay_side(m: ValeMap, ctx: Ctx, x: int, y0: int, y1: int) -> None`: its west side.
  - `lay_steps(m: ValeMap, ctx: Ctx, x: int, y: int) -> None`: the flight up from the lakeside path at the bluff's north-west.

- [ ] **Step 1: Read the vanilla plateaus**

Render and dump the stacks of the raised ground in the exported Backwoods, Railroad, Mountain and Forest maps, wherever lower ground lies north of higher ground or a plateau's side runs north to south. Note which pieces draw:
- a plateau's north rim seen from below it
- its side cliff
- the end where the two meet

**The bluff is one level high**: as tall as our terrace cliffs, so the same face height.

- [ ] **Step 2: Choose the steps**

First choice: the existing stairs art, `art/gilded_vale/stairs/` (`build_vale.STAIRS_PICK`, laid by `lay_stairs`). It's already fitted to a one-level cliff, so it needs no new art. It fits only where the climb runs up a south-facing face. If the bluff's steps have to climb a face the camera sees differently, read how vanilla does it (the Railroad's and the Mountain's steps up to higher ground).

- [ ] **Step 3: Lay a test strip on a scratch copy and render it**

On a copy of the TMX (never the committed one), lay a 6-tile strip of lip, a 6-tile side and the steps at the bluff's planned corner, around (82-88, 56-62). Render all four seasons at 3x beside the vanilla spots they came from.

- [ ] **Step 4: Show the user, and stop if vanilla can't do it**

Send the strip and the vanilla comparison with SendUserFile, and wait for the user's word before Task 5.

If no vanilla pieces draw a believable north rim or side, stop. Tell the user what's missing, and roughly how many PixelLab generations it would take (terrain doc section 1, and section 8 on how the stairs were inpainted), then ask.

- [ ] **Step 5: Write the terrain doc section and commit**

Write a short section in `docs/terrain_painting.md` on the pieces and stacks chosen and why, then commit.

```bash
git add tools/vale_overlook.py docs/terrain_painting.md
git commit -m "Gilded Vale: how a raised bluff's rim, side and steps are drawn (learned from the vanilla plateaus)"
```

---

### Task 5: The overlook

**Files:**
- Modify: `tools/vale_overlook.py` (add `patch_file`)
- Modify: `tools/build_vale.py` (append `"vale_overlook"` to `VALE_PIECES`; `PICKS["spot_lake_stool"]` and `PICKS["spot_lake_rock"]` to the new spot, for the rebuild path)
- Modify: `tools/vale_decor.py:21` (RESERVED: add `OVERLOOK_BOX`, and relabel `(84, 25, 88, 26)` as "the atelier's bench"), `tools/vale_decor.py:229-234` (the picnic spot, onto the bluff), `tools/vale_decor.py:267` (flowers: keep them off the bluff's edge and steps)
- Modify: `[CP] Gilded Vale (dev)/data/lupini_schedule.json` (Fri 900, Sun 1400), `[CP] Gilded Vale (dev)/data/emmett_schedule.json` (Sat 1300)
- Modify: `tools/vale_routes.py` (PLACES)
- Writes: the GildedVale.tmx (and the season sheets if the shed needs new art slots)
- Test: `tools/test_vale_pieces.py`

**Interfaces:**
- Consumes: `lay_lip`, `lay_side` and `lay_steps` (Task 4); `vale_patch`; the `vale_decor` prop tables (`"bench"`, the side benches, `"picnic"`).
- Produces: `vale_overlook.patch_file(tmx_path: str, exports: str) -> None`; `BENCH = (x, y)`, the seat tile of the overlook bench, which the schedules and `vale_routes` use; and `SHED = (x0, y0, x1, y1)`, the shed's tiles (inclusive, y1 its base row). The tests import both from `vale_overlook`.

**Layout** (from the approved plan, `overlook_plan.png`; adjust by a tile or two only to fit the pieces Task 4 chose):
- The bluff's top is about x 85-95, y 61-75, with its west side above the lake's east shore. The steps go up from the lakeside path near (82,58).
- A fringe of trees stays along x 93-95 and y 73-75. The woods of today inside the bluff go, except these.
- The bench is at the north-west corner of the top, about (86,62), facing north. Use the game's back-facing bench if Town or Forest has one. Otherwise use the side bench facing west (toward the lake), and say so in the report.
- The easels' row: x 87-89 on the bench's row, kept clear of props and trees, with a clear walk from the top of the steps to it (for Sable in the 10-heart scene).
- The Three Trees: oak/maple/pine markers on Paths at about (91,63), (93,62) and (92,65). The stump of the fourth at about (94,64) is a decorative tile from the game's own sheets, not a ResourceClump (it must not chop away).
- The shed: about 3 x 3, at about (87-89, 67-69), its door facing the easels. Use the game's own pieces if a small shed exists in the exports, read the way `tools/art/vale_studio_art.py` built the Old Studios. If none fits, stop and ask about PixelLab, with a count.
- The lake painting spot: the stool and paint rock (moved from (88,58) and (88,61), tiles and their `PaintSpot` TileData both) to the bluff's north edge, about x 88-90, y 61-62, looking over the lake.
- The picnic table: on the bluff, about (90,70).

- [ ] **Step 1: Write the failing tests**

```python
def test_overlook_feet_only():
    m = load(TMX)
    bx, by = BENCH
    assert m.g["Buildings"][by][bx] or m.g["Front"][by][bx]                   # the bench is there
    for x in range(87, 90):                                                    # the easels' row is open
        assert not m.g["Buildings"][by][x] and not m.g["Paths"][by][x], x
    x0, y0, x1, y1 = SHED                                                      # its tiles, inclusive; y1 is its base row
    for y in range(y0, y1 - 1):                                                # the roof: over you, not solid
        assert all(not m.g["Buildings"][y][x] for x in range(x0, x1 + 1)), y
    assert all(m.g["Buildings"][y1][x] for x in range(x0, x1 + 1))             # the walls' foot: solid

def test_schedules_moved():
    lu = json.load(open(LUPINI_SCHEDULE)); em = json.load(open(EMMETT_SCHEDULE))
    assert f"1400 blinxxy.MuseArt_GildedVale {BENCH[0]} {BENCH[1]}" in lu["Sun"]
    assert f"1300 blinxxy.MuseArt_GildedVale {BENCH[0]} {BENCH[1]}" in em["Sat"]
    assert "88 59" not in lu["Fri"]
```

In `vale_routes.PLACES`: set `"overlook bench"` to the tile in front of `BENCH`, rename the old one `"atelier bench": (86, 27)`, add `"overlook easels": (88, BENCH[1])` and `"overlook steps top"`, and move `"picnic table"`. All are reachable from the bus stop.

- [ ] **Step 2: Run them to see them fail**

Expected: FAIL in `test_overlook_feet_only` and `test_schedules_moved`.

- [ ] **Step 3: Implement `patch_file` in `tools/vale_overlook.py`**

In order:
1. Corners and ground for the bluff's top (grass), then `repaint_terrain` for `OVERLOOK_BOX`, so the lake's east shore below the side cliff keeps its two-row banks.
2. The lip, side and steps (Task 4).
3. Clear today's trees, bushes and props inside the bluff (Paths markers, the picnic table at (89,64)), keeping the fringe.
4. Lay the bench, trees, stump, shed, painting spot and picnic table. Tall things are solid only on their bottom row, with their tops on Front (or AlwaysFront for the shed's roof rows two or more above its base, as `place_buildings` does).
5. Write the TileData: the two `PaintSpot` actions at their new tiles, and none left at the old ones.

Re-runnable: everything laid is computed from the layout above, never from what's on the map already.

- [ ] **Step 4: Move the schedules**

- Lupini, Sun: `1400 blinxxy.MuseArt_GildedVale <BENCH> 0`
- Emmett, Sat: `1300 blinxxy.MuseArt_GildedVale <BENCH> 0`
- Lupini, Fri: `900 blinxxy.MuseArt_GildedVale <the stand tile beside the moved stool> <facing the lake>`

Use the side bench's facing instead of `0` if that's the bench used. Update `PICKS` in `build_vale.py` and the `vale_decor.py` lines listed above so a rebuild agrees.

- [ ] **Step 5: Run every check**

Expected: all pass; `0 blocked`. The jetty (48,66) and the lakeside bench (47,60) are still ok.

- [ ] **Step 6: Render the four seasons and audit at 3x**

Run `render_tmx.py --region 70 50 26 26 --scale 3 --grid` for each season. Check against section 9, and compare with the approved plan's layout. Send the four renders to the user.

- [ ] **Step 7: Commit**

```bash
git add tools/vale_overlook.py tools/build_vale.py tools/vale_decor.py tools/vale_routes.py tools/test_vale_pieces.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx" "[CP] Gilded Vale (dev)/data/lupini_schedule.json" "[CP] Gilded Vale (dev)/data/emmett_schedule.json"
git commit -m "Gilded Vale: the overlook, a bluff across the lake with its bench, the Three Trees and the shed"
```

(Add the season sheets to the commit too, if the shed added slots.)

---

### Task 6: In game, all four seasons

**Files:** none changed unless a check fails. A fix goes back into the piece's own script, then is re-patched and re-tested.

- [ ] **Step 1: Make a test copy**

Copy `.devgame/devmods_mapfix` to `.devgame/devmods_vale_pieces`. Replace its `[CP] Gilded Vale (dev)` folder with this worktree's. Check that no game is running from that folder, then launch it with `tools/launch_test.ps1 -ModsPath <that folder>`. Confirm that the log's "Mods go here" line shows it.

- [ ] **Step 2: Walk and shoot each season**

For each of spring, summer, fall and winter (`museart_season <s>`):
- `museart_warp blinxxy.MuseArt_GildedVale` to each piece, then `museart_menu mapshot 0.5`.
- Walk behind the hidden statue's head and right-click its foot: the inscription shows.
- Walk round the basin's bank: you hear the falls, and the water animates.
- Climb the steps, sit-test the bench spot, and walk behind the shed.
- Use `museart_route` in segments under 40 tiles to each new place.
- On a Sunday after 1400, Lupini is at the overlook bench.

Stop only the test copy you started.

- [ ] **Step 3: Send the mapshots to the user**

Send them with SendUserFile, one image per piece with its four seasons side by side.

---

### Task 7: Docs, hand-over, and the merge question

**Files:**
- Modify: `docs/design/gilded_vale_map.md`: section 1 (the overlook row, one level high), section 2 (the river and the waterfall as built), section 8 (the hidden sculpture as built), and the progress notes.
- Modify: `docs/terrain_painting.md`: a short section on the taller waterfall (the face and fall rows repeated, the cliff ending in woods), and `vale_patch.repaint_terrain` as the way to change terrain in place.
- Create: `C:\Users\blinx\AppData\Local\Claude-Data\claude_pzs8sxrjxfjjc\side-session-notes\local_a410de16-21bf-4b6b-8b5c-156c4fed34b1\vale_falls_overlook_sculpture.md`: for others editing the TMX, the three boxes, the new sheet slots, and "rerun `python tools/vale_<piece>.py` after any rebuild".

- [ ] **Step 1: Update the docs** as listed above.

- [ ] **Step 2: Run every check one last time**

Run: `python tools/test_vale_pieces.py` and `vale_routes.py`
Expected: all pass; `0 blocked`.

- [ ] **Step 3: Commit, and leave the hand-over note**

```bash
git add docs/design/gilded_vale_map.md docs/terrain_painting.md
git commit -m "Docs: the Vale's waterfall, overlook and hidden sculpture as built"
```

Then write the note in the shared notes folder.

- [ ] **Step 4: Ask the user before merging**

Ask whether to merge into `main`. The vale-map-fixes branch also edits `GildedVale.tmx`, so a merge conflict there is expected. Resolve it by re-running the three patch scripts on the merged map, never by hand-merging the CSV.
