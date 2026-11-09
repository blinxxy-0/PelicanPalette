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

## Fern (approved by the user, 2026-11-08)

Leah's own sheet, recoloured, with a hat drawn on: `tools/art/fern_walk_build.py`.

- `fern_edit_b.png`: Higgsfield's edit of Leah's sheet into Fern (`ref_fern_portrait_x8.png`
  for who she is). It looked right at full size, but read back as one orange smudge
  (`fern_rb_vs_leah_x4.png`): her straw, blonde, mustard and tan are all alike at 16x32. So
  it's only the guide for her hat and dungarees. (`tools/art/walk_readback.py --char-only`
  came from trying: colours chosen from the character alone, by octree.)
- `fern_walk_sheet.png`: Leah's sheet (`ref_walk_leah_x12.png`, she has Fern's braid and
  small build) with her hair sandy blonde, her skin tanned, green-brown eyes, the green top
  a cornflower shirt, the trousers mustard dungarees (a low bib and straps in front, crossed
  straps behind), brown boots; a straw sun hat with a band and a white flower, set two rows
  low so less of Leah's hair shows (the user); facing left is facing right mirrored.
  That's `[CP] Gilded Vale (dev)/assets/fern/sprites.png`.

Previews: `fern_walk_vs_leah_x5.png`, `fern_walk_preview.gif`.

    python tools/art/fern_walk_build.py art/vale_residents_sprites/ref_walk_leah_x12.png "[CP] Gilded Vale (dev)/assets/fern/sprites.png"

## Emmett (approved by the user, 2026-11-08)

Alex's own sheet, recoloured, with his hair, beard and bandana drawn by hand:
`tools/art/emmett_walk_build.py`.

- `emmett_edit_a.png`: Higgsfield's edit of Alex's sheet into Emmett
  (`ref_emmett_portrait_x8.png` for who he is). It read back noisy, a patchy coat and
  smeared faces (`emmett_rb_vs_alex_x4.png`), so as with Fern it's only the guide.
- `emmett_walk_sheet.png`: Alex's sheet (`ref_walk_alex.png`; lean, a jacket) with
  - his eyes blue (Alex's are already lid, pupil, white, iris), the jacket a worn brown coat
    open over a drawn oatmeal waistcoat, dark trousers, scuffed boots;
  - his hair drawn after his portrait, in its colours (the user: Alex's "is boring and
    doesnt reflect the portrait"): shaggy, a messy fringe, locks to his jaw;
  - his head a pixel higher with a row more of face, for a beard with its outline (the
    user), a moustache in a lighter brown where the user dotted it, so it reads apart from
    the beard;
  - his red bandana (the user: "missing the bandana"): pointed in front, knotted behind,
    and in profile under his beard, its point draped over his chest;
  - facing left is facing right mirrored.

  That's `[CP] Gilded Vale (dev)/assets/emmett/sprites.png`.

Previews: `emmett_walk_vs_portrait_x5.png`, `emmett_walk_vs_alex_x5.png`,
`emmett_face_closeup_x14.png`, `emmett_walk_preview.gif`.

    python tools/art/emmett_walk_build.py art/vale_residents_sprites/ref_walk_alex.png "[CP] Gilded Vale (dev)/assets/emmett/sprites.png"

## Sitting on a bench (Lupini and Emmett, 2026-11-09)

A fifth row on their sheets (64x160): frame 16 sits facing left (west), 17 is it mirrored
(east), 18 and 19 are empty. `tools/art/vale_sit_build.py` builds it from the game's own
seated pose, Alex's frame 39 (`alex_sit_left`, his Hospital waiting-room chair):
`ref_sit_alex39.png`. That frame's head and chest are Alex's standing-left frame exactly, a
pixel right and two rows up, so each of them keeps their own standing-left head and chest,
moved the same way, on Alex's seated legs in their colours (Emmett's are Alex's recoloured
as in his walk sheet; Lupini's in his trousers, his frock coat's tails over the seat). No
PixelLab generations.

Used at the overlook bench (89, 58), Town's side bench facing west: `lupini_sit_left` and
`emmett_sit_left` (`16/16/16//offset -16 24`) in `data/lupini.json` and `data/emmett.json`,
on their Sunday and Saturday afternoons. The offset is in world pixels (4 to a sprite
pixel). An NPC arriving by schedule stops a little off the tile (here 2 right and 4 down),
so the offset was set from the arrivals seen in game: they sit mid-seat, their feet over its
front edge. `ingame_overlook_bench_sit.png` is that check (Lupini left, Emmett right).

    python tools/art/vale_sit_build.py art/vale_residents_sprites/ref_sit_alex39.png "[CP] Gilded Vale (dev)/assets/lupini/sprites.png" lupini
    python tools/art/vale_sit_build.py art/vale_residents_sprites/ref_sit_alex39.png "[CP] Gilded Vale (dev)/assets/emmett/sprites.png" emmett
