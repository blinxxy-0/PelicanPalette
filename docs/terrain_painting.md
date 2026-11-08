# Painting Stardew terrain that looks like the base game

How the Gilded Vale's ground, water, cliffs and stairs were made to look like vanilla Stardew, and every rule learned the hard way (November 2026). **Read this before building any outdoor map, or before changing the Vale's.** Each rule below came from a mistake the user had to point out, usually more than once.

The code is `tools/build_vale.py` (the map) and `tools/vale_terrain.py` (what it learns from the vanilla maps). The map is `[CP] Gilded Vale (dev)/assets/GildedVale.tmx`: a separate content pack that Debug builds deploy and **releases never include** (ArtStudio.csproj), until the user says otherwise. Build it with `python tools/build_vale.py <Town.tmx export> "[CP] Gilded Vale (dev)/assets" <vanilla exports dir>`.

---

## 1. The standard

The bar is "as smooth as the base game". That means:
- **No pasted chunks.** Don't cut pieces out of vanilla maps and drop them in. They bring their old surroundings (sand, grass, shadows) and show seams. The user's first reaction was "the map looks very choppy because you've just taken pictures from different parts of the game and added them in."
- **No ruler-straight edges** where vanilla has texture: bank tops, grass edges, path ends.
- **No right angles in water outlines**, no square notches, and no "sharktooth" steps.
- **Every season must be right**, not just spring. Check winter especially (section 9).
- When the game's own pieces can't do something (new stairs, say), **use PixelLab** rather than forcing vanilla pieces to fit. The user, after the Town stairs kept getting worse: "lets maybe not only use the game's own shapes. I've told you you can use pixellab multiple times."

## 2. How Stardew maps work (the parts that matter)

From [Modding:Maps](https://stardewvalleywiki.com/Modding:Maps) and the decompiled game. The wiki blocks automated fetching; read it in the built-in browser.
- **Layers, back to front:** Back, Buildings, Paths, Front, AlwaysFront. `Back2`, `Front2` and the like draw just over their base layer but carry **no collision or properties**. Collision comes from Buildings (any tile there is a wall unless the tileset gives it `Passable T`) and from Back tile properties (`Water`, `Passable`, and so on).
- **Paths layer:** trees, bushes and debris go here as markers (oak 9, maple 10, pine 11 on the `paths` sheet). The game plants real trees on load.
- **Seasonal tilesheets:** any tilesheet whose **file name starts with `spring_`** is swapped for its `summer_`, `fall_` and `winter_` sibling in the same folder (`GameLocation.GetSeasonalTilesheetName`). It works for a mod's own sheets too, if all four files sit side by side. So a map built from the game's `spring_outdoorsTileSheet` and `spring_outdoorsTileSheet2` gets all four seasons for free.
- **The game's map files carry no Tiled terrain definitions.** The wiki says vanilla tilesets have "terrain types", but they don't survive in the files we can export. So the only source for "which tile goes where at an edge" is **how the vanilla maps are actually laid out**.
- **Water shores are layered.** A bank is often a Back water tile with a sand-edge overlay on Buildings, so one cell is a **stack** of (Back, Buildings). Always reason in stacks, never in Back tiles alone.

## 3. The terrain model

`vale_terrain.py` learns everything from exported vanilla maps (Town, Forest, Mountain, Woods, Backwoods, Railroad; `museart_export_map`). All gids use the Town map's tileset numbering: outdoors 1, paths 1976, town 2040, outdoors2 4344, shadows 5464, waterfalls 5939.

- **Corner classes:** each corner of the tile grid is G (light grass), D (dark grass), S (dirt or sand) or W (water). The map is painted as a grid `V[y][x]` of corners, `(W+1) x (H+1)`.
- **A stack's four corners** are learned in two ways. A plain stack (one Back tile all one colour, nothing on Buildings) is known from its pixels. Every other stack is worked out from its plain neighbours, because neighbouring cells share the corners along their common edge. The votes are repeated six times so terrain carries through chains of edge tiles.
- **Excluded:** stacks with any **shadow-sheet** tile (they belong to the trees and buildings that cast them, and show as grey smudges out of place).
- **What the game puts next to what:** `table.right[(a, b)]` and `table.down[(a, b)]` count how often stack b sits right of, or below, stack a.
- **Neighbourhood contexts:** for every vanilla cell near water, the terrain over the **4 x 4 grid corners** around it (its 3 x 3 cells), mapped to the stacks used there (`build_contexts`, `by_context`).

## 4. The order the map is painted in

`paint_ground()` in build_vale.py:
1. **Water shape** (section 5).
2. **Sand rim round all water:** every non-water corner touching water becomes S.
3. **Rare-corner smoothing:** one pass only, and never on designed shapes. It once spread water across the map, and once flattened a designed diagonal.
4. **Rim width** varies between 1 and 2 tiles (noise). At a steady one tile, the grass met the sand in a dead-straight line.
5. **Deepen:** at least 2 tiles of sand above every north bank and beside every side bank, so there's room for the depth row.
6. **Paths:** ragged strokes of S (`stroke`, `rag=0.35`) and the plaza (`area`).
7. **Dark grass:** soft noise patches, kept 2 tiles from sand and water and away from cliffs and their shadow.
8. **No lone corners** (a tuft in the road), and **no checkerboard diagonals** (the game has no tile for them).
9. **Lay tiles by corners:**
   - Plain cells get the game's main tile only (grass: its top two; others: the top one), so nothing speckles.
   - Edges get the game's main bare piece for those corners. Variants next to each other showed hard seams.
   - Shores get the plain version unless an overlay is needed. They never get lily pads, reeds or rocks (`SHORE_DECOR`, `LILY_PADS`).
10. **`paint_shores`:** only at **turns** of the shore, and only on an **exact** 3 x 3 neighbourhood match (section 6).
11. **`vertical_banks`:** every vertical run of bank (section 7).
12. Then, in `main()`: cliffs, waterfalls, bridges, `bank_depth` (the north depth row), stairs and buildings, trees.

## 5. Shaping water so the game has pieces for it

The game only has pieces for certain shapes. Shape the water to fit them, rather than hoping the painter copes.
- **Long straight runs** are best.
- **Corners** facing north (land above water) can be cut on a **clean 45-degree diagonal**, one step per row (`round_corner`, which is a chamfer, not an arc). Uneven arcs broke into lumps and staircases.
- **South-facing corners stay square.** The game has almost no pieces for them (`WWWS`/`WWSW` have 2-3 vanilla uses).
- **Vertical banks step outward going down,** the water widening, never inward. A south-facing step has no piece and came out as fragments.
- **Each step is a short diagonal,** out a tile on two rows running, as in the Town river. A single one-tile jog that drops straight again leaves a **right angle in the water**: "these RIGHT ANGLES ARE WRONG."
- **Space steps apart:** at least 5 rows apart, and 3 rows before the first. A one-row run between two steps reads as a jag. Pick the step rows from the valid combinations; random retries looped forever on a short side.
- **The river** runs at the waterfalls' width (water x 60-63) where it drops over the cliffs, and meanders only where nothing constrains it.

## 6. Shore turns: the 3 x 3 neighbourhood

A cell's four corners can't tell a straight bank from a step in a diagonal from a corner: all can have the same four corners, but the game uses different pieces for each. That was the "sharktoothing".
- `paint_shores` looks up the 4 x 4 corner window around each cell in the vanilla contexts and lays **the stack the game uses there**. Among equal options it takes the one the game most often puts beside what's already left of and above it.
- **Only turns.** Straight runs, where every row or every column of the window is the same (water versus land; grass beyond the sand doesn't count), keep their fixed pieces. By context they came out in broken segments.
- **Only exact matches** (the full 3 x 3). The looser fallbacks guessed at corners the game never draws and made lumps.
- **Plain land** beside a bank only takes the bank's own overlay pieces over a water Back (`WATER_BACKS`). It never takes another map's ground: the Mountain's brown dirt, footprints, or `MOUNTAIN_DIRT` bank tops drawn for darker dirt.
- **Never decorations:** if a context offers only lily pads or reeds, keep the plain piece.

## 7. Banks: two tiles deep, and the vertical cycle

**North banks** (land above water) are **two rows deep**, as at the Mountain pond. The rock row is `(1270, 0)`. Above it, laid over the sand, is a depth row that carries the rock's top and its **scalloped, indented edge**, alternating `(1245, 185)` and `(1245, 212)` (`bank_depth`). A one-row bank left the sand meeting the rock in a straight line.

**Vertical banks are a five-piece cycle.** The Town river and the Forest's shores step through five pieces in order, down every straight run. Together they draw one continuous wavy edge. One piece repeated stacked into disconnected "pillars", and a depth column on every tile read as a row of beads.

| | Land on the left, water right | Water left, land on the right |
|---|---|---|
| The cycle | `(1230,5242) (1230,5258) (5211,5274) (5227,234) (1230,234)` | `(1230,5290) (1230,5306) (5210,5322) (5226,236) (1230,236)` |
| Cap (the tile above a run) | `(5180, 234)` | `(1271, 236)` |
| End piece (before a step) | `(5177, 284)` | `(5178, 285)` or `(1243, 285)` |
| The step, below the end | inner corner `(1267, 0)`; outer corner `(5192, 284)` to its left | inner corner `(1268, 0)`; outer corner `(5195, 285)` to its right |
| Depth pieces at a step | `(1244, 184)` left of the end piece and two left of the inner corner | `(1246, 186)` right of the end piece |

- **The order isn't fixed.** The game skips pieces to land on the right one: **the piece before an end piece is always the cycle's fourth.** `vertical_banks` picks each run's sequence by dynamic programming over `table.down`, from the cap above to whatever is below. The end piece is allowed only last, and only before a step. Counting the cycle blindly left the wrong piece over the end, and a right angle in the water.
- The step's depth pieces carry the rock over the sand so the two **line up**. Without them the lumps sat on sand that didn't meet them: "why do you keep introducing edges that don't have terrain lining up under them."
- A single straight piece per side (`SHORE_PIN`) keeps runs from being changed by context.

## 8. Cliffs, waterfalls, bridges, stairs

- **Cliffs are generated by rule, not stamped** (`cliff`, from the Town ledge north of Pierre's). The edge is `103` (`308` now and then; plain beside stairs). The face alternates two columns, with rare odd stones. The shading rows below are Back `327`, then `356`, then `406`, using the main piece only, since the variants go plain white in winter. Grass goes behind the edge, never shore. Where the river runs under a cliff, the water below the face stays.
- **Waterfalls:** only the falling water and foam (Waterfalls sheet, Front and AlwaysFront) and the wet rock (outdoors2, Buildings), from Town x 91-97, y 0-6. Its first two columns are a fence. Lay it on our own cliff and painted river, and clear the face row the pool covers.
- **Stone bridge:** only the deck rows on Back, and the rails and posts. Narrow it to the river (columns 0, 1, 3, 4, 6, 7 of the Town bridge). Clear shore overlays under the deck and rails only. The posts stand in town-sheet water, which turns a different blue in winter.
- **Log or plank footbridge:** `LOG_DECK` (780-783, `Passable T`) from bank to bank, with plain water under the deck. Some shore tiles under it aren't walkable. A plank bridge also hides awkward joins, like the lake's top bank meeting the river under the lower falls.
- **Stairs are PixelLab art** (`tools/art/vale_stairs.py`):
  1. Render the map with the cliff unbroken (`VALE_NO_STAIRS=1`).
  2. PixelLab Pro inpaint (`inpaint-v3`) a 4 x 9-tile flight into a mask there.
  3. Keep only the grey stone. Drop grass, path and the cliff's rock by colour: rock has blue under 0.3 of its red, the stone about two thirds.
  4. **Seat it on the cliff's foot.** Its bottom step goes on the bottom of the face's last row. Its last steps carry on over the shadow band, as the game's stairs do.
  5. Treads go on Back2 (walkable), side walls on Buildings2 (over our solid cliff).
  6. Paths centre on the flight and **stop just short of it** with a ragged end.
  The game's own Town stairs, cut to fit, never looked right.

## 9. Checking

- **Render every season offline:** `render_tmx.py --sheets` with a folder holding the season's sheets renamed `spring_*`, exported with `museart_export_tex Maps/winter_outdoorsTileSheet ...`. Winter shows what spring hides: town-sheet tiles go blank white or a different blue, cliff-shading variants go plain, and Mountain pieces stand out.
- **Audit in sections at 2-3x** with a tile grid, every part of the map. Don't trust an overview.
- **Compare with vanilla at the same zoom.** Render the equivalent spot of a vanilla map (the Town river x 85-97, the Forest x 0-8 y 89-103, the Mountain pond x 42-56 y 8-17), and **dump the stacks** of both side by side. Most fixes came from reading vanilla's stacks, not from guessing.
- **In game:** `museart_devgame`, `museart_warp blinxxy.MuseArt_GildedVale x y`, `museart_menu mapshot 0.5`, `museart_season <s>`.
  - The season command now resets trees' own season (`Tree.localSeason` via `setSeason`, then `resetTexture`). Without that, a switched winter showed green trees.
  - `museart_route` (it has a 40-tile limit, so test in segments) and `museart_tiles`.
  - A failed route can be the test's fault: check the target isn't in the water or a building.
- **Never launch over the user's game.** Stop only the test copy you started, and rebuild with no game running (the DLL is locked).

## 10. What didn't work (don't try again)

- Stamping chunks of vanilla maps (cliffs, the river, waterfalls and bridges with their surroundings).
- Random second-grass speckle; square path strips.
- Choosing every edge by its four corners alone.
- Choosing edges by vanilla neighbour counts alone (`fit_edges`, kept but unused): the game's curved corners are one-off arrangements, never reused.
- Context lookups with loose fallbacks, and context on straight runs.
- One-row banks; a depth column on every vertical tile; one piece repeated down a vertical bank.
- Single one-tile jogs; arcs for corners; steps facing south.
- A rare-corner smoothing pass over designed shapes.
- The Town stairs cut to fit; stairs art placed as painted (a tile too high).

## 11. Making new terrain

1. Export the vanilla maps if they aren't already (`museart_export_map Town Forest Mountain Woods Backwoods Railroad`), with their tilesheets.
2. Design the shapes for the pieces the game has (section 5).
3. Paint with `paint_ground`. Reuse `vale_terrain.Table`; don't hand-pick tile ids except the rules above.
4. For anything vanilla doesn't have, PixelLab inpaint into a bare render, keeping only the new object's own pixels so the ground stays seasonal.
5. Check every season, in sections, against vanilla, then in game (section 9), **before showing the user.**

## Paved lanes (cobble), added 2026-11-06

The user found dirt paths everywhere "kinda jank", so the village is paved with Pelican Town's own cobble. Timberline Falls uses the same cobble for its footpaths.

- **The terrain class.** Cobble is class `C`. Its plain tiles are `COBBLE` in tools/vale_terrain.py, on the town sheet: 2776, 2777, 2808 and 2809 make a 2 × 2 block of stones, with 2681, 2686, 2810, 3035 and 3036 as worn variants. The curbed edges and corners are learned from Town like any other edge.
- **Two pieces are pinned.** The learner can't place these two (in Town they only sit beside pieces it doesn't know), so `COBBLE_PIN` in build_vale.py fixes them: the plain top edge (GGCC) is 2651, and the top-right corner (GGCG) is 2650.
- **Plain cobble is tiled, not picked.** It is laid by (x % 2, y % 2) from that 2 × 2 block, with about 10% worn stones. Picking the commonest tile repeats one stone everywhere.
- **Lanes are rectangles of grid corners** (`lane()`). The game has no diagonal or ragged cobble. A lane 3 corners wide (2 full tiles plus curbs) is centred on a flight of stairs at sx+2.
- **Cobble never touches dirt or sand.** The curbs meet grass, so any S corner next to a C becomes G. Trails start a step away from the paving, as in Town.
- **Seasons come free.** spring_town swaps to its seasonal versions, and winter keeps the cobble with snowy curbs.
- **Asphalt.** The Bus Stop's road pieces (outdoors sheet 1028–1057) are straight and horizontal only. Timberline makes its turns from rotated copies of them (Tiled flip flags) plus its own sheet. A Vale road with turns would need our own sheet built the same way.

## The bus road (our own sheet), added 2026-11-06

tools/art/vale_road.py draws the road and its turning circle from the Bus Stop's own road tiles: 1029 is the top curb, 1055 the asphalt, 1057 the dashes and 1054 the bottom lip, with grass from 176.

- **The shape.** The road is a signed distance: the straight road joined to a circle with a smooth fillet.
- **The curb.** It is copied layer by layer outward from the asphalt: seam, lip, lit top, face, then shade. How tall it is depends on which way the edge faces: north-facing edges get the tall curb, south- and side-facing edges the thin lip.
- **Seasons.** Every colour is sampled from that season's sheet, so one script makes all four seasons of `<season>_GildedValeRoad.png`.
- **Layout.** The tiles' layout goes to art/gilded_vale/road/layout.json, and build_vale.py `lay_road()` lays it.
- **Building.** Run vale_road.py before build_vale.py whenever the road changes.
- **Dark grass stays 3 tiles clear of the road**, whose tiles carry plain grass.
- **Seasonal custom sheets work in game.** A sheet named spring_* in the pack's assets swaps to its summer_/fall_/winter_ siblings. Checked 2026-11-06.

## Painted features and props (added 2026-11-06)

- **Painting a feature in.** Use tools/art/vale_paint_feature.py: PixelLab inpaint into a render of the bare map, with a strip of a real vanilla map above it in the same picture (features/context_town_park.png or context_town_river.png). Without that strip the results "don't have a Stardew Valley look". With it they copy the game's orange dirt, round bushes, flat greens and stone walls.
- **Cutting it out.** tools/art/vale_feature.py `cutout_diff` keeps only the pixels the painting changed against the bare render: unchanged grass, sand and water go, recoloured water goes, and any flat shade of the ground underneath goes. Light ground that reaches the frame edge is frayed along a smooth wave, never hedges.
- **Collision.** `walkable()` makes a tile walkable if it is mostly path or empty, solid otherwise. Art goes on Back2 if walkable, on Buildings if solid, and on Buildings2 over the map's own Buildings pieces, which are never removed.
- **Dark grass is kept out from under features**, as it is from under the road.
- **Fences use the game's own placeable-fence sprites** (LooseSprites/Fence1 wood, Fence2 stone; 3 × 11 pieces of 16 × 32, one for every position in a fence; see the modding wiki's "How Fences Work"). The map tileset image is `../LooseSprites/Fence1`, loaded straight from the game's content, never copied. Each piece's lower half goes on Buildings and its upper half on Front a row above. Checked in game 2026-11-06.
- **Paths-layer markers** (GameLocation.loadPathsLayerObjectsInArea): 9 oak, 10 maple, 11 pine, 22 tall grass, 24/25/26 large/medium/small bush, and 34 plus a "SpawnTree" TileData property (`fruit <id> 4 4`) for fruit trees. Keep the `4 4` (growth stage on load and on overnight regrowth): without it the game regrows a missing tree as a sapling, and a sapling with anything beside it (the orchard's wild trees) tells the player every morning it "wasn't able to grow". A full-grown fruit tree never does.
- **Building sheet width.** It is 32 columns, so a piece up to 32 tiles wide packs without wrapping.

## Seasons, collision and routes for painted features (2026-11-07)

- **Summer and fall.** Only a feature's bare dirt and sand follow the season: `seasonal()` in tools/art/vale_feature.py, using art/gilded_vale/season_colours.json (learned by tools/art/vale_season_colours.py), applied to large connected ground regions only. Greenery never recolours: hedges, and seedlings under glass, changing with the season looked wrong.
- **Winter** is its own art. tools/art/vale_feature_winter.py runs PixelLab's colour edit to add snow and remove the flowers. The edit takes at most 256 px a side, so wider features go in strips; if a strip comes back unchanged, rerun with VALE_SEED and stitch the good halves. build_vale.py gives the winter art the spring cut-out's exact footprint. Cutting snow against snow left flecks.
- **Collision** (`walkable()`):
  - A tile is solid only if it is mostly object, or dense dark hedge.
  - A wall line running straight through a tile makes it solid, however thin the wall.
  - The game's orange dirt counts as path.
  - Features never lay art over the paving.
- **Routes.** tools/vale_routes.py runs a path search from the bus to 26 key places. Run it after every build; "0 blocked" is the bar.
- **Lupini's painting spots.** They are placed by `reachable_spot()`: on open grass, with ground in front to stand on (paving allowed), clear of every piece, feature and the `RESERVED` yards in vale_decor.py.
- **Props copied from vanilla take every layer**, Back included. The fountain's basin floor is on Back; without it, the paving showed through in winter.
