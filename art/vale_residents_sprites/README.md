# Vale residents' walk sprites

Stardew NPC sheets: 64x128, 4x4 frames of 16x32 (rows: down, right, up, left; four walk
frames each). Placeholders from Higgsfield, kept to the game's look by working from a real
vanilla sheet.

## Bea (approved by the user, 2026-11-08)

1. `ref_walk_leah_x12.png`: Leah's walk sheet on tan, at 12x (768x1536).
   `ref_bea_portrait_x8.png`: Bea's approved portrait, for who she is.
2. `bea_edit_b.png`: Higgsfield (gpt_image_2_6, 768x1536 at 2k) edits Leah's sheet into Bea,
   keeping every frame's pose. Drawing a sheet from nothing had placed the frames badly;
   editing a real one kept the walk.
3. `bea_walk_readback.png`: `tools/art/walk_readback.py` reads it back to 64x128 (12px cells,
   one colour each), clears the background, reduces the colours, outlines the silhouette.
4. `bea_walk_sheet.png`: `tools/art/bea_walk_fix.py` fixes by hand what the read-back lost
   (the user: "the back of some of the outfits and the eyes need a little fixing"; then
   "missing a brow and whites"):
   - eyes laid out as Leah's: brow, lid and pupil, white and brown iris;
   - the back row shows the dress, with her apron's crossed straps, waistband and bow.

   That's `[CP] Gilded Vale (dev)/assets/bea/sprites.png`.

`bea_walk_vs_leah_x4.png` and `bea_walk_preview.gif` are the previews the user approved from.

Rebuild:

    python tools/art/walk_readback.py art/vale_residents_sprites/bea_edit_b.png bea_rb.png
    python tools/art/bea_walk_fix.py bea_rb.png "[CP] Gilded Vale (dev)/assets/bea/sprites.png"
