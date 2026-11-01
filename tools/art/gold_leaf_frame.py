"""Draws the gold-leaf frame (1.3, the finished portfolio's reward), the way
moonlit_frame.py draws the Moonlit one: the plain frames recoloured, here in
gold leaf, with a crest over the middle of the top and a bright bead in each
corner.
  - ArtStudio/assets/furniture.png, widened to 240x48: the gold-leaf 2x2 and
    3x3 framed paintings at (160, 0) and (192, 0) (sprite indices 10 and 12)
  - ArtStudio/assets/items.png: the Gold-Leaf Frame item at sprite 41, from
    the Picture Frame at sprite 8
Safe to run again: it redraws the same places from the plain originals.

Usage: gold_leaf_frame.py <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import moonlit_frame

# Darkest to lightest: a warm brown outline up to pale gold.
GOLD = [(82, 46, 18), (128, 80, 22), (178, 122, 30), (220, 168, 52), (244, 206, 96), (255, 238, 168)]
BEAD, BEAD_LIGHT = (255, 238, 168), (255, 252, 226)


def recolour(tile, keep_inside=True):
    saved = moonlit_frame.MOONLIT
    moonlit_frame.MOONLIT = GOLD
    try:
        return moonlit_frame.recolour(tile, keep_inside)
    finally:
        moonlit_frame.MOONLIT = saved


def crest_and_beads(tile):
    """A little diamond crest over the middle of the top border, and a bead
    in each corner of the border (the top-left one is the light's)."""
    px = tile.load()
    w, h = tile.size
    cx = w // 2
    for dx, dy, c in ((0, 0, BEAD_LIGHT), (-1, 1, BEAD), (1, 1, BEAD), (0, 2, BEAD), (0, 1, BEAD_LIGHT)):
        px[cx + dx - 1, dy] = c + (255,)
        px[cx + dx, dy] = c + (255,)
    for sx, sy in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        px[sx, sy] = (BEAD_LIGHT if (sx, sy) == (1, 1) else BEAD) + (255,)
    return tile


def main():
    assets = sys.argv[1]
    furniture_path = os.path.join(assets, "furniture.png")
    furniture = Image.open(furniture_path).convert("RGBA")
    if furniture.width < 240:
        wider = Image.new("RGBA", (240, furniture.height), (0, 0, 0, 0))
        wider.paste(furniture.crop((0, 0, 160, furniture.height)), (0, 0))
        furniture = wider
    for (sx, size), ox in (((0, 32), 160), ((32, 48), 192)):
        tile = recolour(furniture.crop((sx, 0, sx + size, size)).copy())
        furniture.paste((0, 0, 0, 0), (ox, 0, ox + size, size))
        furniture.paste(crest_and_beads(tile), (ox, 0))
    furniture.save(furniture_path)

    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16

    def at(index):
        return (index % per_row * 16, index // per_row * 16)
    x, y = at(8)
    icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), keep_inside=False)
    ipx = icon.load()
    # (The brush's tip reaches into the plain icon's top left corner.)
    for yy in range(3):
        for xx in range(2):
            ipx[xx, yy] = (0, 0, 0, 0)
    for px_, py_, c in ((7, 1, BEAD_LIGHT), (8, 1, BEAD_LIGHT), (6, 2, BEAD), (9, 2, BEAD), (7, 3, BEAD), (8, 3, BEAD)):
        ipx[px_, py_] = c + (255,)
    tx, ty = at(41)
    items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
    items.alpha_composite(icon, (tx, ty))
    items.save(items_path)
    print("wrote", furniture_path, "and", items_path)


if __name__ == "__main__":
    main()
