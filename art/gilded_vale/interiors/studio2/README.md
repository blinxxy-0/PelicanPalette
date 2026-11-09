# Studio II, the Corrie twins' (built 2026-11-09, reworked with the user the same day)

Plan approved first (`plan.png`, `plan_hearts.png`); renders `render.png` (as they move in) and
`render_hearts.png` (every heart stage at once: each follows the heart event that changes the room).

Where each piece comes from (`tools/build_vale_buildings.py`, spec "studio2", its `picks`):

- **PixelLab, Pro Flash** (one image each, 5 generations; 35 in all), styled on the Vale's approved
  furniture of the same size: `pixellab/glass_counter`, `cutting_bench`, `light_table`, `mabel_bench`,
  `glass_racks`, `modern_kiln`, and `drafting_table` (styled on Mabel's bench: the user wanted it
  "as detailed as" the bench beside it).
- **Higgsfield** (`gpt_image_2_6`, the six PixelLab pieces as the style reference), shrunk to their
  footprints by `tools/art/object_fit.py` (a majority vote per pixel, not averaging): `higgsfield/`
  `design_sketches_3`, `glass_lamp_3`, `offcut_bins_2`, `mabel_bench_used_1`. Shrinking loses detail:
  if any of these looks soft next to the PixelLab ones, redo it in PixelLab the drafting table's way.
- **Drawn in code** (`tools/art/vale_corrie_art.py`):
  - the big window and Mabel's window (Jet's 10 hearts) and the small window behind the racks: drawn
    the way the studio's outside glass is, with its own code (`tools/vale_studio_sale.py`: the frame
    painted slate green, leaded diamonds; Mabel's a leaded sunrise). The user: generated windows were
    "bad imitations of pixel art"; "even the glass outside their studio is better".
  - the line down the middle: lead came inlaid across the seam (`split_line`, `split_line_b`, kept
    exactly as drawn by `AS_DRAWN`);
  - the colour the racks throw on the floor (`colour_pool`), the dropped sketches.
- **The game's own:** the city lamps (Modern Lamp, Data/Furniture 1447), the beds, the ladder, the rug.
- Opal's half papered in the game's wallpaper 97 on planks, Jet's in 103 on its tiles (flooring 9).
- Every Studio II piece's near-black outline is warmed to the Vale's dark brown as the sheet is
  packed (`warm_outline`: the user, "this giant black outline").

Rebuild: `python tools/art/vale_corrie_art.py`, then `python tools/vale_studio2.py <exports dir>`
(the map, and data/vale_studio2.json), then `python tools/build_corrie_events.py` (the events read
the stage tiles from data/vale_studio2.json).
