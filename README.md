# Muse Art Studio (Stardew Valley mod)

A spin-off of Muse's painting game: paint pixel-art still lifes at an easel against the light, level a Painting skill, and sell or gift what you make. The plan lives in `C:\Users\blinx\.claude\plans\hey-i-m-still-super-quizzical-pine.md`.

- `ArtStudio/`: the SMAPI mod (`blinxxy.MuseArt`). `dotnet build` deploys it to the game's `Mods` folder.
  - `Core/`: game-free rules: canvas, judge, mixing bar, numbers. Shared with the sim.
  - `Painting/`: items, references, per-item pixel art, Harmony patches.
  - `Menus/`: the easel (setup → painting → results).
  - `Shop/`: the art shop's counter, commission board and deliveries.
- `[CP] Art Studio/`: the Content Patcher pack: the shop's map, stock and Forest patch, and Sable (`data/sable*.json`; brief in `docs/npc_brief.md`).
- `ArtStudio.Sim/`: calibration. `dotnet run -c Release -- refs.json`
- `tools/`: test-game launch/stop scripts, placeholder art, PNG export. `tools/art/` builds Sable's portraits and sprites from the PixelLab picks (`build_sable_*.py`) and compares them with vanilla (`compare_vanilla.py`).

## Testing

Use the **Stardew Art Studio (test)** desktop shortcut. It runs SMAPI on the game's own `Mods` folder (Art Studio, Content Patcher, SpaceCore, GMCM). It does not use the CurseForge modpack. **Start a new farm there:** loading a modpack save without its mods and then sleeping would save it in a broken state.

Getting started in game:
- Craft an **Easel** (30 wood, 10 fiber) and **Small Canvases** (5 fiber, 2 wood). You know both recipes from the start.
- Place the easel and right-click it.
- More recipes unlock with the Painting skill: canvases, picture frames, brushes, and pigments ground from clay, flowers, berries and gems.

Debug build:
- **F8** opens the easel anywhere.
- In the SMAPI console:
  - `museart_supplies`: easel, canvases, brushes, frames, every pigment jar
  - `museart_level <n>`
  - `museart_tastes [item]`: every villager's taste for each quality
  - `museart_paint ...`, `museart_free`, `museart_pigments ...`

From the title screen:
- `museart_snap [level]` renders every easel screen to `Mods/ArtStudio/snapshots/` and writes a `layout_report.txt` of overlapping or spilling text.
- `museart_export_refs` writes `refs.json` for the sim.
- `museart_devgame` starts a throwaway farm that is never saved. Then:
  - `museart_devscene` sets up the farmhouse.
  - `museart_selftest`, then `museart_selftest sell`, then `museart_selftest sold` run the end-to-end checks.
  - `museart_shoptest` checks the art shop; `museart_npctest` checks Sable (data, gift tastes, every schedule route, the counter).
  - `museart_where` logs where Sable is; `museart_day <n>` (early morning) switches the day to try another schedule.
  - `museart_festival <id> [main|dance]` jumps to a festival (e.g. `spring24 dance` dances with Sable); `museart_festspot <map> <x> <y>` checks a festival spot is walkable and clear.
  - `museart_advance [n]` clicks through dialogue, to play events unattended.
- Debug builds also run commands written to `Mods/ArtStudio/dev_commands.txt`.
- `tools/launch_test.ps1 [-ModsPath dir]`, `tools/stop_test.ps1` and `tools/screenshot_test.ps1` drive a test copy of the game.
