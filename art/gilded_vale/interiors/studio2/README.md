# Studio II, the Corrie twins' (built 2026-11-09, approved by the user)

Plan approved first (`plan.png`, `plan_hearts.png`); renders `render.png` (as they move in) and
`render_hearts.png` (every heart stage at once).

- **PixelLab** (Pro Flash, one image each, 5 generations; 30 in all), styled on the Vale's
  approved furniture of the same size (Tobin's counter, the atelier's tables, the pottery's
  shelves): `pixellab/glass_counter`, `cutting_bench`, `light_table`, `mabel_bench`,
  `glass_racks`, `modern_kiln`. The user (2026-11-09): the hand-drawn first versions "looked bad".
- **Still hand-drawn** (`tools/art/vale_corrie_art.py`), to redo with PixelLab once the allowance
  refills on **Nov 26**: the studio window and Mabel's window, the glass lamp, Opal's drawing board
  and pinned designs, the leaning panels, the offcut bins; and the floor decals (the line, the
  colour pool, dropped sketches), which may stay. Mabel's bench in use is the PixelLab bench with
  her book and a panel drawn on; redo it as an edit of that bench.
- The city lamps are the game's own Modern Lamp (Data/Furniture 1447).

Rebuild: `python tools/art/vale_corrie_art.py`, then
`python tools/vale_studio2.py <exports dir>` (the base map, and data/vale_studio2.json: the door
from Spring 1, Year 3, the heart stages as EditMaps, the right-click texts).
