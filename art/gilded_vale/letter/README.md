# The Gilded Vale's letter paper (Lupini's letter opening the Vale)

Made 2026-11-08 at the user's ask: "a higgsfield pixel art design, something that fits the gilded vale, like gold cracks and foliage", after the game's own letters (the Wizard's starry one, Emily's pink one).

- **Generated:** Higgsfield `gpt_image_2_6`, 1280x720 (the letter's 320x180 at 4x), quality high, with the Wizard's and Emily's letters cropped from screenshots as references (`generated_*.png`).
  - **A1-A3:** ivory parchment, a thin brown border with curled corners, gold kintsugi cracks bright at the edges and faint in the middle, ivy with gilded leaves and small violet and ochre flowers in the corners and along the bottom.
  - **B1-B3:** A2 redrawn with the top-left corner and the top and left edges kept clear, since the game writes the letter from the top-left; the foliage in a low band along the bottom and up the bottom-right.
- **Read back** onto the game's 320x180 grid, one colour per 4x4 cell (`candidate_*.png`). The game's own letter sheet (`LooseSprites/letterBG`) is 320x180 a background, so these drop straight in.
- **`candidates_with_letter.png`:** each one with Lupini's letter laid out as the game draws it: its letter font (`LooseSprites/font_bold`, read from the game's files) at 3x, 32 px in, 54 px a line.
- **In the pack:** the chosen one is `[CP] Gilded Vale (dev)/assets/ValeLetter.png` (B1, the user's pick, 2026-11-08), loaded as `Mods/blinxxy.MuseArt/ValeLetter` and used with `[letterbg Mods/blinxxy.MuseArt/ValeLetter 0]` (`data/vale_letter.json`).
