# The painters' guide, the Gilded Grove and the graveyard: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lupini's painting spots become a find-them-all quest from the old painters' guide. Its reward is a warp into the Gilded Grove, a secret painted map. The Painter's clearing becomes the graveyard of the Vale's past residents.

**Architecture:** C# in `ArtStudio/Vale/` (Debug only, `#if DEBUG`) holds the rules: spots, the guide, the quest, the splash, graves, the Grove's actions, and the Founders' easel. Data goes in the dev content pack: the guide item, the Grove's location and its always-fall location context, the Grove fish, and i18n. Maps: the Vale's TMX is patched in place with the map-gaps branch's `tools/vale_patch.py`, and the Grove's TMX is written by a new builder script. Checks are in-game `[PASS]`/`[FAIL]` lines (a new `museart_guidetest` command) plus offline Python map checks, the repo's two test styles.

**Tech Stack:** C# (SMAPI 4, Harmony, Stardew 1.6), Content Patcher (dev pack), Python 3 + Pillow for TMX and art scripts, PixelLab where art needs it.

**Spec:** `docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md`. Read it first; this plan argues from it.

## Global Constraints

- Debug only: every C# file wrapped in `#if DEBUG`, data only in `[CP] Gilded Vale (dev)`. Never released. Changes to released code (frames) go behind `#if DEBUG`.
- **No new paint colours or pigments.** No restoring paintings in the Grove.
- Amphitheatre: spot 8 sits outside the bowl, **at least 2 tiles clear of x 29–45, y 49–59**, not on the seats, the stage or its approach. Show the user a render before placing it.
- No name, stone or echo for the anonymous painter. The Corrie twins are alive: never buried.
- Props stay well inside yards: nothing on walls, nothing straddling edges. Only a tall thing's bottom row is solid; its top goes on Front.
- Read `docs/terrain_painting.md` before painting terrain. Patch `GildedVale.tmx` in place (`tools/vale_patch.py`), never by rebuilding with `build_vale.py`.
- Check every map change in all four seasons (offline renders with `tools/render_tmx.py`; in game with `museart_menu mapshot`).
- Never launch a game over the user's running game. Use `run_dev.ps1 -ModsDir devmods_guide` (`docs/development.md`) with `PELICAN_DEV_ROOT=C:/Users/blinx/Desktop/MuseStardew/.devgame`, and check the log's "Mods go here" line names `devmods_guide`.
- All player-facing text is approved by the user as one batch (Task 1) before it is committed. All new art is shown as candidates (Task 2) before it's used, with a rough PixelLab generation count before any credits are spent.
- Commit only this work's files on `claude/vale-guide-grove`. Never push to main or release without the user's OK.

## Review Focus

1. **Co-op:** a farmhand finds their own spots, gets their own guide and warp, and can make their own offering. The host's flags never stand in for theirs. (Task 3 and Task 8 checks run under `run_mp.ps1`.)
2. **Using the guide when the player isn't free** (an event, a festival, an open menu, the bus arriving, a splash already playing): nothing happens, and above all no warp. (Task 4 check.)
3. **A lost guide:** if it was trashed, touching the board again gives a new one without restarting the quest. (Task 4 check.)
4. **Offering the wrong thing** at the Founders' easel: a framed masterpiece, a non-masterpiece, a stack, or a second offering. The item is kept, a line explains, and no frame is given. (Task 8 check.)
5. **Leaving the Grove:** the frame always returns you to a walkable tile at the graveyard gate, even if the gate tile is blocked by a placed object or an NPC. Passing out in the Grove at 2am behaves like passing out anywhere else. (Task 7 check.)

---

### Task 0: Merge order (gate, no code)

The Vale's map and the Vale House interior are being changed on other branches. This plan's map tasks build on them.
- [ ] **Step 1:** Ask the user to merge `claude/infallible-jang-a66ee9` (waterfall, overlook, hidden sculpture; brings `tools/vale_patch.py` and `tools/test_vale_pieces.py`) and `claude/jovial-blackwell-676837` (the Old Studios roll in i18n `vale.studios.sign`) into `main` once their sessions are done. Tasks 1 to 4 and 8 can start before this. Tasks 5, 6, 9 and 10 need it.
- [ ] **Step 2:** Once both are merged: `git rebase main` on `claude/vale-guide-grove`, then run `python tools/test_vale_pieces.py` and `python tools/vale_routes.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx"`. Expected: all pass, `0 blocked`.

### Task 1: The text batch (gate)

**Files:**
- Create: `docs/superpowers/specs/2026-11-08-vale-guide-text.md`

- [ ] **Step 1:** Draft every player-facing line the spec names, in the voice of `docs/vale_events.md`. One heading per group, each line with its future i18n key:
  - `vale.guide.spot.<id>.name` and `.line` for the 8 spot ids (Task 3).
  - `vale.guide.plain_stool`: the spec's pre-guide line, verbatim.
  - `vale.guide.board`, `vale.guide.take` (Bea's line), `vale.guide.again` (a replacement guide).
  - `vale.guide.page.title`, `vale.guide.page.unfound`.
  - `vale.guide.last_page`: the Grove described in words when the quest completes.
  - `vale.guide.step_in`: "Step into the last page?" verbatim. `vale.guide.not_here`: "The page only seems to come alive in the Vale." verbatim.
  - `quest.painters_guide.title`, `.description`, `.objective` (with `{{found}}` of 8).
  - `vale.grave.<id>` for `hester`, `amos`, `ada`, `agnes`, `hollis`, `mabel`, `bram`: the name, what they made, the summers in the Vale, and an epitaph. Ada's mentions "not yet". Bram's is only his scratched name.
  - `vale.echo.<id>` for `agnes`, `hollis`, `mabel`, `bram`: each memory answers its epitaph.
  - `vale.grove.frame`, `vale.grove.founders.ask`, `.taken`, `.thanks`, `.not_masterpiece`, `.framed`.
  - `tobin.dialogue.urns` (4 hearts) and `lupini.dialogue.bram` (one line about Bram).
- [ ] **Step 2:** Send the file to the user and wait. Apply their edits. That file is the source for every i18n string below. Commit it.

### Task 2: The art batch (gate)

**Files:**
- Create: `art/gilded_vale/props/vale_guide_v1/` (candidates), `art/gilded_vale/props/vale_guide_v1/candidates.png` (a contact sheet, each candidate at 3x beside a vanilla tombstone and easel for scale)

- [ ] **Step 1:** List the pieces with sizes in tiles, and the PixelLab generations each would take (or "hand-drawn"): four carved headstones, one plain stone, three glazed clay markers, seven urns in different glazes, the low wall and gate, four echoes (two frames of idle motion each), the Founders' easel (empty, and with a covered canvas), the hidden easel, the return frame, the Vale panorama, the guide icon, the painted-fish icon, and the Founders' Frame (in the style of `tools/art/frame_styles.py`). The splash is drawn in code (Task 3); it needs no art.
- [ ] **Step 2:** Tell the user the total generation count and wait for their OK before spending credits. Then make the candidates and send the contact sheet. Apply their picks, add winter versions of the outdoor graveyard pieces (snow, like `SEASONAL` in `tools/build_vale.py`), and commit the chosen art.

### Task 3: Spots that you find, and the splash

**Files:**
- Create: `ArtStudio/Vale/PaintersGuide.cs`, `ArtStudio/Vale/PaintSplash.cs`, `ArtStudio/Vale/GuideTests.cs`
- Modify: `ArtStudio/Vale/GildedVale.cs` (the PaintSpot action; register the new classes), `[CP] Gilded Vale (dev)/i18n/default.json`

**Interfaces:**
- Produces, in `PaintersGuide` (static):
  - `record Spot(string Id, Color[] Palette)`, and `Spot[] Spots`, with ids in this order: `rooftops`, `northlight`, `wildflowers`, `lakewindow`, `parterre`, `falls`, `tree`, `stage`. The palettes are the spec's section 4 table, 3 to 5 colours each.
  - `const string GuideTakenFlag = ModEntry.Id + "_GuideTaken"`, `const string GroveOpenFlag = ModEntry.Id + "_GroveOpen"`, `string FoundFlag(string spotId)` returning `$"{ModEntry.Id}_SpotFound_{spotId}"`. All are `mailReceived` flags, so they're per player.
  - `enum TouchResult { NoGuide, FirstFind, Again, Completed }`
  - `TouchResult Touch(Farmer who, string spotId, bool effects = true)`: the rules only. `effects:false` skips the splash and dialogue, for tests.
  - `int FoundCount(Farmer who)`
- Produces, in `PaintSplash`: `void Play(Color[] palette, bool grand)`, `bool Playing`.
- The tile action becomes `blinxxy.MuseArt_PaintSpot <spotId>`. With no id (an old rock tile), it does nothing and returns false.

- [ ] **Step 1: Write the failing checks** in `GuideTests.cs`, a `museart_guidetest` command in the style of `ValeTests.Check`. It saves and restores the player's flags around the run.
  - `guide: no guide, no find`: clear the flags. `Touch(who,"rooftops",false) == NoGuide` and `FoundCount(who) == 0`.
  - `guide: first find`: add `GuideTakenFlag`. `Touch(...,"rooftops",false) == FirstFind` and `FoundCount == 1`.
  - `guide: a second touch doesn't count`: `== Again` and `FoundCount == 1`.
  - `guide: the eighth completes`: touching the other 7 returns `FirstFind` six times, then `Completed`. Afterwards `GroveOpenFlag` is set and `FoundCount == 8`.
  - `guide: an unknown id is refused`: `Touch(...,"nowhere",false)` throws `ArgumentException`.
- [ ] **Step 2: Run it and see it fail.** `run_dev.ps1 -ModsDir devmods_guide -Steps "museart_devgame|40","museart_flag blinxxy.MuseArt_ValeOpen|5","museart_guidetest|10" -Pattern "guide"`. Expected: a build error (`PaintersGuide` doesn't exist).
- [ ] **Step 3: Implement `PaintersGuide.Touch`, `FoundCount` and the spot table**, and change the PaintSpot action in `GildedVale.cs`: `NoGuide` shows `vale.guide.plain_stool`; `FirstFind` plays the splash and then shows the name and line; `Again` shows the line; `Completed` plays the grand splash, sets `GroveOpenFlag`, completes the quest (Task 4) and shows `vale.guide.last_page`. A touch while `PaintSplash.Playing` is ignored.
- [ ] **Step 4: Implement `PaintSplash`.** It's a HUD overlay drawn on `RenderedHud`: 12 to 20 blots in the palette that grow, hold and drip away over 1.5 seconds (3 seconds and every spot's colours when `grand`). It holds the player still (`Game1.player.CanMove = false`, restored when done) and plays a vanilla magic sound cue (the implementer picks; `wand` is a candidate).
- [ ] **Step 5: Run it and see it pass.** The same command. Expected: 5 `[PASS] guide:` lines, no `[FAIL]`. Touch a stool by hand in that game and watch the splash once (the checks can't see it).
- [ ] **Step 6: Commit.** `git add ArtStudio/Vale "[CP] Gilded Vale (dev)/i18n/default.json"` and commit with the message `Gilded Vale: painting spots are found, not easels; the paint splash`.

### Task 4: The guide, the quest and the board

**Files:**
- Modify: `ArtStudio/Vale/PaintersGuide.cs`, `ArtStudio/Vale/GuideTests.cs`, `tools/build_vale_house.py` (the board, near the guest-book case at (11, 18)), `[CP] Gilded Vale (dev)/assets/ValeHouse.tmx` (rebuilt by that script), `[CP] Gilded Vale (dev)/content.json` (include the new data file), i18n
- Create: `[CP] Gilded Vale (dev)/data/vale_guide.json` (Data/Objects: the guide; Data/Quests: the quest)

**Interfaces:**
- Consumes: Task 3's `PaintersGuide` members.
- Produces: `const string GuideItemId = ModEntry.Id + "_PaintersGuide"` (an object that can't be gifted, sold or dropped; the implementer checks how 1.6 marks that, for example context tags), `const string QuestId = ModEntry.Id + "_PaintersGuide"` (a Basic quest; its `currentObjective` is updated on each find), the tile action `blinxxy.MuseArt_TakeGuide`, `void TakeGuide(Farmer who)`, and `bool UseGuide(Farmer who)` (returns false and shows nothing when the player isn't free).

- [ ] **Step 1: Write the failing checks:**
  - `guide: taking it`: `TakeGuide` adds `GuideTakenFlag`, puts one `GuideItemId` in the bag, and adds `QuestId`.
  - `guide: a lost guide comes back`: remove the item, then call `TakeGuide`. The item is back, the quest isn't added twice, and `FoundCount` is unchanged.
  - `guide: the objective counts`: after one `Touch`, the quest's `currentObjective` contains `1`.
  - `guide: outside the Vale`: warp to the Farm. `UseGuide(who) == false` and the location is unchanged.
  - `guide: not while busy`: open any menu, then call `UseGuide`. Expected `false`, no warp.
  - `guide: in the Vale, open`: with `GroveOpenFlag` set and the player free in the Vale, `UseGuide` shows a question dialogue.
- [ ] **Step 2:** Run (`-Pattern "guide"`). Expected: the new checks `[FAIL]` or fail to build.
- [ ] **Step 3:** Implement. Use before completion: a letter-style text page (`LetterViewerMenu` with `vale.guide.page.title`, then each spot's name, ticked, or `vale.guide.page.unfound`). After completion in the Vale: `createQuestionDialogue(vale.guide.step_in)`, and yes calls `GildedGrove.WarpIn` (Task 7; until then, a stub that logs). Outside the Vale: `vale.guide.not_here`. Add the board to `build_vale_house.py` as a wall piece with `Action blinxxy.MuseArt_TakeGuide`, then rebuild `ValeHouse.tmx`. Add the guide's object, quest and icon to `vale_guide.json`.
- [ ] **Step 4:** Run. Expected: all `guide:` checks pass. Then run `museart_valetest`: the Vale House checks still pass.
- [ ] **Step 5: Commit:** `Gilded Vale: the old painters' guide and its quest`.

### Task 5: The spots on the map (needs Task 0)

**Files:**
- Create: `tools/vale_spots.py` (uses `vale_patch.load/save`; its own entry in `vale_patch.BOXES` for each spot it touches), `tools/test_vale_guide_map.py` (plain `test_*` functions, run as a script like `tools/test_vale_pieces.py`)
- Modify: `[CP] Gilded Vale (dev)/assets/GildedVale.tmx`, `tools/vale_routes.py` (`PLACES`), `[CP] Gilded Vale (dev)/data/lupini_schedule.json` (only if a spot moves), `ArtStudio/Vale/GuideTests.cs`

- [ ] **Step 1: Write the failing offline checks:**
  - `test_rocks_have_no_action`: no TileData with Action starting `blinxxy.MuseArt_PaintSpot` is on a paint-rock tile.
  - `test_every_spot_action_has_a_known_id`: every PaintSpot action has exactly one argument, from the 8 ids.
  - `test_existing_spots_present`: `rooftops`, `northlight`, `wildflowers` and `lakewindow` are each on exactly one stool tile.
  - `test_clearing_spot_gone`: no PaintSpot action and no stool or rock piece within x 3–12, y 10–22.
  - `test_only_spot_boxes_changed`: compared with `main`, tiles change only inside this script's boxes.
- [ ] **Step 2:** Run `python tools/test_vale_guide_map.py`. Expected: FAIL on the first test.
- [ ] **Step 3:** Implement `vale_spots.py`: set each stool's action to `blinxxy.MuseArt_PaintSpot <id>`, remove the rocks' actions, and remove the clearing's stool and rock (its PICKS entries `spot_clearing_*` in `build_vale.py` too). The lake spot stays where the overlook build put it. Add the four stools to `PLACES`.
- [ ] **Step 4:** Add the in-game check `guide: spots on the map`: for each id present in the Vale map, `Touch` through the tile action gives the expected result. Run the offline tests, `vale_routes.py`, and `museart_guidetest`. Expected: all pass, `0 blocked`.
- [ ] **Step 5: Commit:** `Gilded Vale: the four spots carry their names; the clearing's spot goes`.

### Task 6: The graveyard (needs Tasks 0, 1 and 2)

**Files:**
- Create: `tools/vale_graveyard.py` (a `graveyard` box in `vale_patch.BOXES`, covering x 2–13, y 8–23, plus the trail's join to the turning circle), `ArtStudio/Vale/ValeGraves.cs`
- Modify: `GildedVale.tmx`, `tools/build_vale.py` (the pieces in `PICKS`/`VALE_PIECES` and their winter art in `SEASONAL`), `[CP] Gilded Vale (dev)/assets/spring_GildedValeBuildings.png` and its winter sibling, `tools/vale_routes.py`, `tools/test_vale_guide_map.py`, `GuideTests.cs`, `lupini_schedule.json`, `tobin_dialogue.json`, `lupini_dialogue.json`, i18n

**Interfaces:**
- Produces: the tile action `blinxxy.MuseArt_Grave <id>` (ids as in Task 1), and `static readonly Point GraveyardGateTile` (in `ValeGraves`), the tile just inside the gate, used by Task 7.

- [ ] **Step 1: Write the failing offline checks:**
  - `test_graves`: exactly 7 `Grave` actions, one per id, all inside the wall. Four are carved stones, Bram's is the plain stone, and Ada's, Hester's and Amos's are clay markers. Each grave has an urn piece on the tile at its foot.
  - `test_wall_closed_but_gate`: the wall's ring is solid except the gate. The gate tile is walkable from the bus stop (`vale_routes` reachability).
  - `test_nothing_on_the_wall`: no prop's tiles overlap the wall's tiles or straddle its edge.
  - `test_tall_feet_only`: each stone's and marker's top row is on Front, and only its bottom row is on Buildings.
  - `test_trail_joins_road`: a walkable path of trail or cobble runs from the turning circle to the gate.
  - `test_four_seasons`: winter art exists for every new piece.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Implement `vale_graveyard.py`. Keep the ring of trees (`vale_decor.py` around line 181) where it doesn't land inside the wall. Use the vanilla bench (`vale_decor.py` `prop("bench", ...)`). Then `ValeGraves.cs`: the action shows `vale.grave.<id>`. Lupini's `Mon`: `900` at a walkable tile beside the bench, facing it, instead of `10 16`. Tobin's line goes in at 4 hearts. Lupini gets one line about Bram. Add `"graveyard gate"` and `"graveyard bench"` to `PLACES`.
- [ ] **Step 4:** Add the in-game checks `graves: seven stones read` (each action's text matches its i18n key) and `graves: Lupini's Monday tile walkable`. Run the offline tests, `vale_routes.py`, `museart_guidetest` and `museart_valetest` (schedules). Expected: all pass. Render all four seasons with `tools/render_tmx.py` and send the renders to the user.
- [ ] **Step 5: Commit:** `Gilded Vale: the graveyard in the old Painter's clearing`.

### Task 7: The Gilded Grove: the map, the warp and the way back

**Files:**
- Create: `tools/build_vale_grove.py`, `[CP] Gilded Vale (dev)/assets/GildedGrove.tmx`, `ArtStudio/Vale/GildedGrove.cs`
- Modify: `vale_guide.json` (Data/LocationContexts: `blinxxy.MuseArt_GroveContext` with `SeasonOverride: "fall"` and always-sun weather; Data/Locations: `blinxxy.MuseArt_GildedGrove` with `CreateOnLoad` and that context), `content.json` (load the map), `PaintersGuide.cs` (replace the stub), `GuideTests.cs`, `tools/test_vale_guide_map.py`

**Interfaces:**
- Consumes: `ValeGraves.GraveyardGateTile` (Task 6). Until Task 6 is in, use `(8, 23)`, the trail's foot, and replace it then.
- Produces: `const string Location = ModEntry.Id + "_GildedGrove"`, `Point ArrivalTile`, `void WarpIn(Farmer who)` (flash, sparkles and a paint wash, then a warp), and the tile actions `blinxxy.MuseArt_GroveFrame`, `blinxxy.MuseArt_HiddenEasel`, `blinxxy.MuseArt_FoundersEasel` and `blinxxy.MuseArt_Echo <id>` (the last three implemented in Tasks 8 and 9; registered here as no-ops).

- [ ] **Step 1: Write the failing checks.** Offline:
  - `test_grove_closed`: no walkable tile on the map's border.
  - `test_grove_reachable`: the arrival tile, the frame, the hidden easel, the Founders' easel and the four echo tiles are all reachable from the arrival tile.
  - `test_grove_size`: about 30×25 (each side within 25–35).

  In game:
  - `grove: always fall`: in each of the four game seasons (`Game1.season` set per check), `Game1.getLocationFromName(Location).GetSeason() == Season.Fall`.
  - `grove: one view`: `Views.SubjectFor(grove)` ends with `:fall` whatever the season.
  - `grove: warp in`: `WarpIn` lands on `ArrivalTile`.
  - `grove: the frame home`: the frame action lands in the Vale on `GraveyardGateTile`. With an object placed on that tile, it lands on the nearest walkable tile instead.
- [ ] **Step 2:** Run both. Expected: FAIL.
- [ ] **Step 3:** Implement `build_vale_grove.py`, writing the TMX from vanilla fall tiles with a warm tint (the implementer picks how: tinted copies of the fall sheets, or the location's lighting). Include the drop and the panorama piece (Task 2) along one edge, the pool (water tiles; fishable), old trees, and the frame. Woods close every edge. Then `GildedGrove.cs`: warp in and out, plus the paint motes, a looping ambient effect drawn while the player is in the Grove.
- [ ] **Step 4:** Run both and walk it once in the dev game. Expected: all `grove:` checks pass. Then run a pass-out check: set the time to 1:50am in the Grove and wait. The farmer passes out and wakes normally, with no crash.
- [ ] **Step 5: Commit:** `Gilded Vale: the Gilded Grove, reached through the guide's last page`.

### Task 8: The hidden easel, the Founders' easel and the Founders' Frame

**Files:**
- Modify: `ArtStudio/Vale/GildedGrove.cs`, `ArtStudio/Painting/ArtHooks.cs` (`OpenEasel` gains `int startingSparks = 0`, passed to the easel menu's `_sparksCaught`), `ArtStudio/Menus/EaselMenu*.cs` (accept it), `ArtStudio/Painting/ArtItems.cs` and `Frames.cs` (`founders` appended to `MoreFrameIds` and `Styles` **inside `#if DEBUG`**, as `(FrameRarity.Epic, FrameTrait.Sparks)`), `tools/art/frame_styles.py` (the founders art, at the end of the sheet so no released sprite index moves), `ArtStudio/i18n/default.json` (`item.frame.founders` and `.description`), `GuideTests.cs`, `GildedGrove.tmx` (the two easels' tiles)

**Interfaces:**
- Consumes: Task 7's actions. `PaintingData.IsMasterpiece(Item)`. `ArtItems.MoreFrame("founders")`.
- Produces: `const string FoundersFlag = ModEntry.Id + "_FoundersOffered"` (per player), and `bool Offer(Farmer who, Item? item)`.

- [ ] **Step 1: Write the failing checks:**
  - `founders: not a masterpiece`: `Offer(who, plainPainting) == false`, the item is still in the bag, no frame is given, and `FoundersFlag` is unset.
  - `founders: framed`: `Offer(who, framedMasterpiece) == false` and the item is kept.
  - `founders: a masterpiece`: `Offer(who, masterpiece) == true`. The painting is gone from the bag, one Founders' Frame is added, and `FoundersFlag` is set.
  - `founders: only once`: a second masterpiece gives `false`, and the item is kept.
  - `founders: in the frames table`: `Frames.Of(ArtItems.MoreFrame("founders")) == (Epic, Sparks)`.
  - `hidden easel: a spark to start`: opening the hidden easel gives an easel menu whose caught sparks start at 1.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Implement. The Founders' easel draws its covered-canvas piece when the local player has `FoundersFlag` (a per-player map override or a drawn sprite: the implementer's call). The hidden easel calls `ArtHooks.OpenEasel(who, tile, startingSparks: 1)`. That's the spec's "a little more likely" for masterpieces: you start one spark towards Inspired (`PaintingRules.SparksForInspired`).
- [ ] **Step 4:** Run, and run it again in co-op: `run_mp.ps1` with the farmhand offering after the host. Expected: all pass, and each player gets their own frame. Then do a Release build (`dotnet build ArtStudio/ArtStudio.csproj -c Release -p:EnableModDeploy=false`) and check that the frames table has no `founders` entry in it (grep the built DLL's frame list, or check `Frames.Styles.Count` with a Release-only assertion).
- [ ] **Step 5: Commit:** `Gilded Vale: the hidden easel and the Founders' easel (Debug-only frame)`.

### Task 9: The painted echoes and the Grove fish

**Files:**
- Modify: `GildedGrove.tmx` (four animated echo tiles from Task 2's art, each with `Action blinxxy.MuseArt_Echo <id>`), `GildedGrove.cs`, `vale_guide.json` (the painted fish's Data/Objects and Data/Fish entries, and the Grove's `Fish` list in Data/Locations; follow `data/vale_fish.json` on branch `claude/vale-fish` for field style, but this fish is not one of its five), `GuideTests.cs`, `tools/test_vale_guide_map.py`, i18n

- [ ] **Step 1: Write the failing checks:**
  - `echoes: four`: exactly four `Echo` actions in the Grove, with ids `agnes`, `hollis`, `mabel` and `bram`, each showing its `vale.echo.<id>` text.
  - `grove fish: only here`: the Grove's location fish data includes the painted fish's id; `blinxxy.MuseArt_GildedVale`'s doesn't.
  - Offline `test_echo_tiles_animated`: each echo tile has a TMX animation of at least 2 frames.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Implement: the echo action shows the memory, and the fish's data and icon are added. Echoes have no transparency and no spooky sound (the spec's anti-goal).
- [ ] **Step 4:** Run. Expected: pass. Catch the fish once in the dev game (`museart_` fishing helpers, or by hand).
- [ ] **Step 5: Commit:** `Gilded Vale: the painted echoes and the Grove's fish`.

### Task 10: The four new spots, and the docs (needs Task 0)

**Files:**
- Modify: `tools/vale_spots.py`, `GildedVale.tmx`, `tools/build_vale.py` (the stools and rocks in `PICKS`), `tools/vale_routes.py`, `tools/test_vale_guide_map.py`, `docs/design/gilded_vale_map.md`

- [ ] **Step 1: Write the failing offline checks:**
  - `test_all_eight_spots`: each of the 8 ids is on exactly one stool tile.
  - `test_stage_spot_clear`: the `stage` stool and rock are at least 2 tiles from every tile in x 29–45, y 49–59. Neither is on the seats, the stage, or the stage's approach lane.
  - `test_parterre_spot`: the `parterre` stool is inside the Lost Parterre's box (x 5–28, y 39–47) and at least 3 tiles from the hidden statue.
  - `test_falls_spot`: the `falls` stool is beside the basin, inside the map-gaps `FALLS_BOX`, and walkable to.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Render the proposed `stage` spot in context and send it to the user before placing it (the Global Constraint). Then place `parterre`, `falls`, `tree` and `stage`, and add them to `PLACES`.
- [ ] **Step 4:** Run the offline tests, `vale_routes.py`, and the full `museart_guidetest`, with all 8 spots found by walking to each stool. Expected: all pass, and the eighth touch opens the Grove. Render all four seasons and send them to the user.
- [ ] **Step 5:** Update `docs/design/gilded_vale_map.md`: the clearing is now the graveyard; the hidden easel is in the Grove; and the discoveries table, the views list and the progress notes change to match.
- [ ] **Step 6: Commit:** `Gilded Vale: all eight painting spots; the map doc brought up to date`.
