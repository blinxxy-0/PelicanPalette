# Tobin's, Lupini's, Fern's and Emmett's portraits (placeholder art, Higgsfield)

Approved by the user 2026-11-08, made the way Bea's were (`../bea/higgsfield/README.md`).

- **Bases:** Higgsfield `gpt_image_2_6`, with Bea's approved neutral (`base_x16` of hers) as the FRAMING reference (the first faces came out too small) and `ref_portraits_x4.png` (Leah, Margot, Bea, Julian) for style; prompts asked for clean, simple shapes like the game's villagers (the user: earlier Fern and Emmett were "too busy"). Lupini follows his in-game look, the painter on the Night Market boat (`lupini_ingame_sprite_x16.png`, from `night_market_tilesheet_objects`), not his brief's description. Picks: `tobin_4`, `lupini_6`, `fern_5`, `emmett_5`; read back with `tools/art/pixel_readback.py` (a 16 px grid), 6 px of air above the head as Leah's.
- **Expressions:** face-only edits of each base (`expr/`), the face area taken from each (Fern's taller, for her chin). Redone where the user circled problems; Lupini's angry keeps his base glasses and eyes and takes only the brows and mouth from the edit.
- **Sheets** (`<name>_portraits_sheet.png` -> `[CP] Gilded Vale (dev)/assets/<name>/portraits.png`), 2 across: 0 neutral, 1 happy, 2 sad, 3 unique (Tobin dry, Lupini theatrical, Fern a cheeky closed-mouth grin, Emmett his crooked smirk), 4 love, 5 angry, as their dialogue tags them.
