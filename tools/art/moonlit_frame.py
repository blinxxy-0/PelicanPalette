"""Draws the Moonlit Frame (1.2, Sable's Night Market stall), from the
plain frames already in the mod's art, recoloured midnight blue and silver,
with a moon and stars on the border:
  - ArtStudio/assets/furniture.png, widened to 160x48: the moonlit 2x2 and
    3x3 framed paintings at (80, 0) and (112, 0), beside the plain ones at
    (0, 0) and (32, 0) (sprite indices 5 and 7)
  - ArtStudio/assets/items.png: the Moonlit Frame item at sprite 40, from
    the Picture Frame at sprite 8
Each colour of the plain frame maps to the moonlit one of the same
lightness, so the shading (and its dark-coloured outline) carries over.
Safe to run again: it redraws the same places from the plain originals.

Usage: moonlit_frame.py <ArtStudio/assets dir>
"""
import os, sys
from PIL import Image

# Darkest to lightest.
MOONLIT = [(22, 26, 58), (36, 44, 92), (58, 70, 128), (110, 124, 170), (168, 180, 210), (214, 222, 240)]
STAR, STAR_LIGHT = (232, 214, 150), (255, 246, 206)


def lightness(c):
    return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def recolour(tile, keep_inside=True):
    """Maps the frame's colours, by lightness, onto MOONLIT. The pale canvas
    inside (the lightest colour, filling the middle) is left alone."""
    px = tile.load()
    w, h = tile.size
    colours = sorted({px[x, y][:3] for y in range(h) for x in range(w) if px[x, y][3] > 0}, key=lightness)
    inside = px[w // 2, h // 2][:3] if keep_inside else None
    frame = [c for c in colours if c != inside]
    step = (len(MOONLIT) - 1) / max(1, len(frame) - 1)
    mapping = {c: MOONLIT[min(len(MOONLIT) - 1, round(i * step))] for i, c in enumerate(frame)}
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in mapping:
                px[x, y] = mapping[(r, g, b)] + (a,)
    return tile


def moon_and_stars(tile):
    """A crescent moon over the middle of the top border, and a little
    four-pointed star in each corner of the border."""
    px = tile.load()
    w, h = tile.size
    cx = w // 2
    # The crescent, 5 wide and 5 tall, opening to the right.
    crescent = ["01110", "11000", "10000", "11000", "01110"]
    for j, row in enumerate(crescent):
        for i, bit in enumerate(row):
            if bit == "1":
                px[cx - 2 + i, j - 1 if j else 0] = (STAR_LIGHT if i < 2 else STAR) + (255,)
    for sx, sy in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        if (sx, sy) in ((1, 1),):
            continue                                # (the highlight corner stays plain)
        px[sx, sy] = STAR_LIGHT + (255,)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            px[sx + dx, sy + dy] = STAR + (255,)
    return tile


def main():
    assets = sys.argv[1]
    furniture_path = os.path.join(assets, "furniture.png")
    furniture = Image.open(furniture_path).convert("RGBA")
    if furniture.width < 160:
        wider = Image.new("RGBA", (160, furniture.height), (0, 0, 0, 0))
        wider.paste(furniture.crop((0, 0, 80, furniture.height)), (0, 0))
        furniture = wider
    for (sx, size), ox in (((0, 32), 80), ((32, 48), 112)):
        tile = recolour(furniture.crop((sx, 0, sx + size, size)).copy())
        furniture.paste(moon_and_stars(tile), (ox, 0))
    furniture.save(furniture_path)

    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16
    def at(index):
        return (index % per_row * 16, index // per_row * 16)
    x, y = at(8)
    icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), keep_inside=False)
    ipx = icon.load()
    # A moon on top.
    # (The brush's tip reaches into the plain icon's top left corner.)
    for yy in range(3):
        for xx in range(2):
            ipx[xx, yy] = (0, 0, 0, 0)
    for px_, py_, c in ((7, 1, STAR_LIGHT), (8, 1, STAR), (6, 2, STAR_LIGHT), (6, 3, STAR_LIGHT), (7, 4, STAR_LIGHT), (8, 4, STAR)):
        ipx[px_, py_] = c + (255,)
    tx, ty = at(40)
    items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
    items.alpha_composite(icon, (tx, ty))
    items.save(items_path)
    print("wrote", furniture_path, "and", items_path)


if __name__ == "__main__":
    main()
