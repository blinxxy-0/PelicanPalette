# The Gilded Vale on the world map

The Vale's own region on the world map (M), 300x180 like the game's maps (data/vale_worldmap.json in the dev pack). It works like Ridgeside Village's: you see the Vale's map while you're there and the valley's everywhere else.

## How it was made (2026-11-08)

1. `render_tmx.py` renders the real Vale map (`GildedVale.tmx`) from its tiles, one image per season, using the game's own tilesheets decoded from the game's files.
2. The map is shrunk to 2.25 pixels a tile and placed at 42,5 in a 300x180 frame (`trace.png`), with the road running off to the west. The map data uses the same numbers: tile x,y is pixel 42 + 2.25x, 5 + 2.25y.
3. Higgsfield (gpt_image_2_6, 1200x720) redrew the trace in the style of the game's valley and island maps. `readback.py` reads each result back onto the 300x180 grid.
4. **The user picked B, with A's gold ring around the sakura tree.** `ring.py` draws that ring onto B in A's gold colours, giving `spring.png`.
5. Higgsfield repainted `spring.png` for summer, fall and winter, using the season's trace and the game's own seasonal valley map as references. For each season I kept the version whose doors line up with spring's (`seasons_sheet.png`). Fall 1 and winter 1 drifted. The other summer is kept as `summer_alt.png`.

## Files

- `trace.png`: the traced layout.
- `A.png`, `B.png`, `C.png`: the three candidates, with `A_generated.png` and `B_generated.png` at full size; `candidates_sheet.png` compares them.
- `spring.png`, `summer.png`, `fall.png`, `winter.png`: what's in the pack, as `assets/ValeWorldMap.png` and `ValeWorldMap_<season>.png`.
- `*_alt.png`: the other generation for each season, read back.
