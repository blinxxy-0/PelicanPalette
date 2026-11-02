"""Builds Julian Vane's winter outfit (1.3, docs/design/1.3_plan.md section 5):
a long camel wool overcoat, collar up, and a charcoal scarf, for his winter
visits outdoors, as Sable's and Margot's winter clothes are built
(build_sable_outfits.py):

  assets/julian/sprites_winter.png     his 64x128 walking sheet
  assets/julian/portraits_winter.png   his 128x256 portraits

From his everyday sheets and PixelLab Pixen edits of them (2026-11-02,
art/julian/winter/winter_batch.json; picks in art/julian/winter/picks: the
walk sheet, and the neutral portrait, which every expression takes its coat
from). Only the pixels the edit really changed are taken, so his face,
glasses and hair stay as drawn; on each portrait, the expression's own face
wins above the jaw.

Usage: build_julian_outfits.py <art/julian/winter/picks dir> <assets/julian dir>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_sable_outfits import outfit_mask, merge
from vanillafy import quantize, recolour_black

WALK, PORTRAIT = "winter_walk_2_0.png", "winter_portrait_2_0.png"
COLLAR_LINE = 42        # from here down, the portrait is clothing
JAW_LINE = 40           # above here, an expression's own face wins
COLOURS = 24


def main():
    picks, assets = sys.argv[1], sys.argv[2]
    walk = Image.open(os.path.join(assets, "sprites.png")).convert("RGBA")
    edited_walk = Image.open(os.path.join(picks, WALK)).convert("RGBA")
    dressed = merge(walk, edited_walk, outfit_mask(walk, edited_walk))
    quantize(recolour_black(dressed), COLOURS).save(os.path.join(assets, "sprites_winter.png"))

    everyday = Image.open(os.path.join(assets, "portraits.png")).convert("RGBA")
    neutral = everyday.crop((0, 0, 64, 64))
    edited = Image.open(os.path.join(picks, PORTRAIT)).convert("RGBA").resize((64, 64), Image.NEAREST)
    mask = outfit_mask(neutral, edited, COLLAR_LINE)
    out = everyday.copy()
    unchanged = []
    for f in range(everyday.width // 64 * (everyday.height // 64)):
        x0, y0 = (f % 2) * 64, (f // 2) * 64
        frame = everyday.crop((x0, y0, x0 + 64, y0 + 64))
        pf, pn = frame.load(), neutral.load()
        own = {(x, y) for y in range(JAW_LINE) for x in range(64) if pf[x, y] != pn[x, y]}
        frame_dressed = merge(frame, edited, mask - own)
        pd = frame_dressed.load()
        unchanged += [(x0 + x, y0 + y) for y in range(64) for x in range(64) if pd[x, y] == pf[x, y]]
        out.paste(frame_dressed, (x0, y0))
    # The palette is cut down for the coat; what it didn't touch keeps its
    # exact colours.
    reduced = quantize(recolour_black(out), 34)
    pr, po = reduced.load(), everyday.load()
    for x, y in unchanged:
        pr[x, y] = po[x, y]
    reduced.save(os.path.join(assets, "portraits_winter.png"))
    print("wrote sprites_winter.png and portraits_winter.png in", assets)


if __name__ == "__main__":
    main()
