# Bea's portraits (placeholder art, Higgsfield)

Approved by the user 2026-11-08. The cheap PixelLab route (`../pixellab/`) was rejected: the face was the wrong size and it didn't feel like Stardew.

- **Base:** Higgsfield `gpt_image_2_6`, 1:1, with `ref_portraits_x5.png` (Leah, Margot and Julian at the game's scale) as the reference; prompt in the session notes: "the same 64x64 pixel grid with big chunky square pixels... close in on the face: the head fills most of the frame, the hair touches the top edge, the eyes at the vertical middle...", Bea from `docs/bea_brief.md` without the pencil (Margot has one). Picked `v2_a.png`, read back with `tools/art/pixel_readback.py` (a forced 16 px grid), then shifted down 6 px for Leah's air above the head (`bea_portrait_neutral.png`).
- **Expressions:** `gpt_image_2_6` edits of `base_x16.png` (the neutral at 16x) "change ONLY her facial expression to ..."; read back the same way, and only the face area (x 17-46, y 20-50) taken from each edit, so hair and clothes are pixel-identical (`expr/`).
- **Sheet:** `bea_portraits_sheet.png` -> `[CP] Gilded Vale (dev)/assets/bea/portraits.png`: 2 across, slots 0 neutral, 1 happy, 2 sad, 3 dry half-smile (her unique face, as her dialogue uses $u), 4 blush, 5 angry, 6 surprised.
