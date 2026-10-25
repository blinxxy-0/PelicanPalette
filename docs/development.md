# Pelican Palette: development notes

For players, see the [README](../README.md).

The mod was called Art Studio until its 1.0.0 release; the code project, its `ArtStudio/` folder and the `blinxxy.MuseArt` IDs keep the old name, so saves made before the rename still load.

A spin-off of Muse's painting game: paint pixel-art still lifes at an easel against the light, level a Painting skill, and sell or gift what you make. The plan lives in `C:\Users\blinx\.claude\plans\hey-i-m-still-super-quizzical-pine.md`.

- `ArtStudio/`: the SMAPI mod (`blinxxy.MuseArt`). `dotnet build` deploys it to the game's `Mods` folder.
  - `Core/`: game-free rules: canvas, judge, mixing bar, numbers. Shared with the sim.
  - `Painting/`: items, references, per-item pixel art, Harmony patches.
  - `Menus/`: the easel (setup → painting → results).
  - `Shop/`: the art shop's counter, commission board and deliveries.
- `[CP] Pelican Palette/`: the Content Patcher pack: the shop's map, stock and Forest patch, and Sable (`data/sable*.json`; brief in `docs/npc_brief.md`).
- `ArtStudio.Sim/`: calibration. `dotnet run -c Release -- refs.json`
- `tools/`: test-game launch/stop scripts, placeholder art, PNG export. `tools/art/` builds Sable's portraits and sprites from the PixelLab picks (`build_sable_*.py`) and compares them with vanilla (`compare_vanilla.py`).
  - `build_sable_events.py` writes the heart events (`data/sable_events.json`), `build_sable_wedding.py` the wedding with gender-neutral vows, `build_spouse_areas.py` the spouse room and patio maps, `build_shop_interior.py` the shop.

## Testing

Use the **Stardew Art Studio (test)** desktop shortcut. It runs SMAPI on the game's own `Mods` folder (Art Studio, Content Patcher, SpaceCore, GMCM). It does not use the CurseForge modpack. **Start a new farm there:** loading a modpack save without its mods and then sleeping would save it in a broken state.

Getting started in game:
- Craft an **Easel** (30 wood, 10 fiber) and **Small Canvases** (5 fiber, 2 wood). You know both recipes from the start.
- Place the easel and right-click it.
- More recipes unlock with the Painting skill: canvases, picture frames, brushes, and pigments ground from clay, flowers, berries and gems.

Debug build:
- **F8** opens the easel anywhere, once switched on with `museart_devkeys` (off by default).
- In the SMAPI console:
  - `museart_supplies`: easel, canvases, brushes, frames, every pigment jar
  - `museart_level <n>`
  - `museart_tastes [item]`: every villager's taste for each quality
  - `museart_paint ...`, `museart_free`, `museart_pigments ...`

From the title screen:
- `museart_snap [level]` renders every easel screen to `Mods/ArtStudio/snapshots/` and writes a `layout_report.txt` of overlapping or spilling text.
- `museart_export_refs` writes `refs.json` for the sim.
- `museart_mp host` (in one dev copy) and `museart_mp join` (in a second dev copy with its own Mods folder) start a two-player game on this machine: a throwaway farm with a cabin, hosted invite-only, and a farmhand. Then `museart_mptest host-setup` (host), `client` (farmhand), `host-check` (host) and `client-collect` (farmhand) run the co-op checks.
- `museart_devgame` starts a throwaway farm that is never saved. Then:
  - `museart_devscene` sets up the farmhouse.
  - `museart_selftest`, then `museart_selftest sell`, then `museart_selftest sold` run the end-to-end checks.
  - `museart_shoptest` checks the art shop; `museart_npctest` checks Sable (data, gift tastes, every schedule route, the counter).
  - `museart_where` logs where Sable is; `museart_day <n>` (early morning) switches the day to try another schedule.
  - `museart_festival <id> [main|dance]` jumps to a festival (e.g. `spring24 dance` dances with Sable); `museart_festspot <map> <x> <y>` checks a festival spot is walkable and clear.
  - `museart_advance [n] [answer]` clicks through dialogue (picking an answer at questions), to play events unattended; `museart_event <id>` plays an event directly.
  - `museart_eventpaths` walks every step of Sable's events against the real maps; `museart_route <loc> x1 y1 x2 y2` prints walkable `move` commands; `museart_dialoguefit` checks every dialogue page fits its box; `museart_snapdialogue` renders the event dialogue boxes.
  - `museart_portrait` saves Sable's portrait of the farmer; `museart_marry [patio]` and `museart_wedding` (throwaway farm only) show the spouse room, patio and wedding.
- Debug builds also run commands written to `Mods/ArtStudio/dev_commands.txt`.
- `tools/launch_test.ps1 [-ModsPath dir]`, `tools/stop_test.ps1` and `tools/screenshot_test.ps1` drive a test copy of the game.

## Release, mod page and wiki

- **Release zip:** `dotnet build ArtStudio/ArtStudio.csproj -c Release -p:EnableModDeploy=false` writes `ArtStudio/bin/Release/net6.0/ArtStudio <version>.zip`. Without the flag the build also deploys, so leave it on while a test game is running.
- **Media** (debug build, in a dev game; written to `Mods/ArtStudio/ArtStudio/media/`):
  - `museart_frame <name>` saves this copy's next frame, with any open menu laid over the world. Use zoom 1.
  - `museart_zoom`, `museart_time`, `museart_farmer off`, `museart_place` and `museart_seen` stage a shot.
  - `museart_scenario <name>` opens one of `museart_snap`'s easel states in the game.
  - `museart_signtext <text>` renders a heading in the game's bold font.
  - `museart_wikidata` dumps the items, recipes, stock, pigments, brushes and skill, with item icons.
- **Repository folders:** copy the results into `docs/media/` (`screens/`, `signtext/`, `icons/`, `wikidata.json`).
- **Page art:** `python tools/art/page_art.py docs/media` draws the wooden headings and the banner into `docs/media/page/`.
- **Nexus page:** `release/nexus/description.bbcode`, with an upload checklist in `release/nexus/README.md`.
- **Wiki:** `python tools/build_wiki.py` writes the GitHub wiki to `docs/wiki/`. The tables come from `docs/media/wikidata.json`, so re-dump it after changing items or prices. Publish it by pushing `docs/wiki/` to the repository's `.wiki.git`.

