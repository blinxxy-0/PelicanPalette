# Opal's and Jet's portraits (placeholder art, Higgsfield), approved by the user 2026-11-09

The other residents' way (`../README.md`, `../../bea/higgsfield/README.md`).

- **First try, PixelLab (rejected):** one 128x192 Pro Flash sheet each, styled on Evelyn's and
  Kent's portraits (12 generations for both). The user: the faces weren't zoomed in enough to
  match the other characters. They're kept only as the who-they-are reference below.
- **Bases:** Higgsfield `gpt_image_2_6`, 1:1, two references: Bea's and Tobin's neutral portraits
  side by side at 16x for the framing and scale, and the PixelLab neutral at 16x for who they are.
  Candidates `opal_1/2.png`, `jet_1/2.png`; the user picked **Opal A** (`opal_1`) and **Jet B**
  (`jet_2`). Read back with `tools/art/pixel_readback.py` (period 16), finished with
  `tools/art/portrait_finish.py` (the tan background cleared from the edges in, 6 px of air above
  the head as the others): `opal_portrait_neutral.png`, `jet_portrait_neutral.png`.
- **Expressions:** `gpt_image_2_6` edits of each neutral at 16x, "change ONLY her/his facial
  expression to ..." (`expr/`); `tools/art/portrait_expressions.py` reads each back and takes only
  the face box (Opal x 13-40, y 26-50; Jet x 14-41, y 21-50), so hair and clothes are identical.
- **Sheets**, 2 across: 0 neutral, 1 happy, 2 sad, 3 unique (Opal laughing out loud; Jet one brow
  up, dry), 4 love, 5 angry. `opal_portraits_sheet.png` -> `[CP] Gilded Vale (dev)/assets/opal/portraits.png`,
  `jet_portraits_sheet.png` -> `.../jet/portraits.png`.

    python tools/art/portrait_expressions.py art/vale_residents_portraits/corrie/opal_portrait_neutral.png "[CP] Gilded Vale (dev)/assets/opal/portraits.png" 13,26,41,51 art/vale_residents_portraits/corrie/expr/opal_{happy,sad,unique,love,angry}.png
    python tools/art/portrait_expressions.py art/vale_residents_portraits/corrie/jet_portrait_neutral.png "[CP] Gilded Vale (dev)/assets/jet/portraits.png" 14,21,42,51 art/vale_residents_portraits/corrie/expr/jet_{happy,sad,unique,love,angry}.png
