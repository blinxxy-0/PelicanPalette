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

## Tobin (approved by the user, 2026-11-08)

The same way, from Clint's sheet: the vanilla man nearest his broad build, beard and work apron.

1. `ref_walk_clint_x12.png`: Clint's walk sheet on tan, at 12x. `ref_tobin_portrait_x8.png`:
   Tobin's approved portrait.
2. `tobin_edit_b.png`: Higgsfield's edit (the second of two: clearer straps on his back, more
   grey at his temples). It drew the apron's crossed straps and waist tie from behind itself.
3. `tobin_walk_readback.png`: `tools/art/walk_readback.py`.
4. `tobin_walk_sheet.png`: `tools/art/tobin_walk_fix.py` gives him eyes laid out as Bea's
   (brow; lid and pupil; white and iris), where the read-back left dark smudges. That's
   `[CP] Gilded Vale (dev)/assets/tobin/sprites.png`.

Previews: `tobin_walk_vs_clint_x4.png`, `tobin_walk_preview.gif`, `tobin_faces_x9.png`.

    python tools/art/walk_readback.py art/vale_residents_sprites/tobin_edit_b.png tobin_rb.png
    python tools/art/tobin_walk_fix.py tobin_rb.png "[CP] Gilded Vale (dev)/assets/tobin/sprites.png"

## Lupini (approved by the user, 2026-11-08)

Built round his own sprite, the painter on the Night Market boat (the user: "it didn't use
lupini's existing front sprite at all").

1. `ref_lupini_ingame_x16.png`: his sprite from `night_market_tilesheet_objects`, at x16.
   `ref_walk_harvey_x12.png`: Harvey's walk sheet at 12x (tall, glasses, moustache).
2. `lupini_edit_b.png`: Higgsfield's edit of Harvey's sheet into Lupini, his sprite as the
   reference (the other edit came out on a dark background the read-back can't clear).
3. `lupini_walk_readback.png`: `tools/art/walk_readback.py`.
4. `lupini_walk_sheet.png`: `tools/art/lupini_walk_build.py`:
   - front: his sprite's own head and body, exactly; the edit's walking legs below (on the
     boat his legs are one block behind the rail);
   - facing left: the edit's body in his sprite's colours, with a head drawn from his
     sprite (the edit's had a lump on the forehead and a nose that moved every frame);
     plum hair receding only a little, glasses, a nose, his beard; his bow tie added;
   - facing right: facing left mirrored, as the game's own sheets are;
   - from behind: hair all over, a rose shine on the crown (the edit made him balder).

   That's `[CP] Gilded Vale (dev)/assets/lupini/sprites.png`.

Previews: `lupini_walk_vs_ingame_x5.png`, `lupini_walk_preview.gif`.

    python tools/art/walk_readback.py art/vale_residents_sprites/lupini_edit_b.png lupini_rb.png
    python tools/art/lupini_walk_build.py art/vale_residents_sprites/ref_lupini_ingame_x16.png lupini_rb.png "[CP] Gilded Vale (dev)/assets/lupini/sprites.png"
