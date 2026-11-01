"""Draws the Master's Easel (1.3, the Painting Mastery item): the easel
recoloured in gold leaf, like the gold-leaf frame, with a gem at the top.
  - ArtStudio/assets/bigcraftables.png, widened to 64x32: the Master's
    Easel at sprite index 3 (x 48), from the easel at index 0
Safe to run again: it redraws the same place from the easel.

Usage: masters_easel.py <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

# The easel's wood, darkest to lightest, and the gold it becomes.
WOOD_TO_GOLD = {
    (62, 39, 36): (82, 46, 18),
    (90, 51, 34): (150, 98, 26),
    (125, 98, 58): (200, 146, 40),
    (162, 133, 59): (236, 190, 70),
    (190, 159, 52): (250, 222, 120),
}
GEM, GEM_LIGHT = (60, 199, 180), (190, 250, 240)


def main():
    assets = sys.argv[1]
    path = os.path.join(assets, "bigcraftables.png")
    sheet = Image.open(path).convert("RGBA")
    if sheet.width < 64:
        wider = Image.new("RGBA", (64, sheet.height), (0, 0, 0, 0))
        wider.paste(sheet.crop((0, 0, 48, sheet.height)), (0, 0))
        sheet = wider
    easel = sheet.crop((0, 0, 16, 32)).copy()
    px = easel.load()
    for y in range(32):
        for x in range(16):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in WOOD_TO_GOLD:
                px[x, y] = WOOD_TO_GOLD[(r, g, b)] + (a,)
    # A gem on the top of the mast.
    top = next(y for y in range(32) if any(px[x, y][3] for x in range(16)))
    mid = next(x for x in range(16) if px[x, top][3])
    px[mid, top] = GEM_LIGHT + (255,)
    if top + 1 < 32 and px[mid, top + 1][3]:
        px[mid, top + 1] = GEM + (255,)
    sheet.paste((0, 0, 0, 0), (48, 0, 64, 32))
    sheet.alpha_composite(easel, (48, 0))
    sheet.save(path)
    print("wrote", path)


if __name__ == "__main__":
    main()
