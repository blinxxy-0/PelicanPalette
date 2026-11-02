"""Draws 1.3's frame styles beyond the collection's (docs/design/1.3_plan.md
section 5), the way set_frames.py draws those: the plain frame recoloured
by lightness, each with a small touch of its own.
  - common: pine, oak, birch, painted white
  - uncommon: walnut, red lacquer, rope
  - legendary: prismatic, Qi's
Writes:
  - ArtStudio/assets/furniture.png, widened to 1280x48: each style's 2x2 and
    3x3 framed paintings, 80 pixels apart from x 560 (sprite indices
    35/37, 40/42, ... 75/77)
  - ArtStudio/assets/items.png: each style's frame item at sprites 67 to 75,
    from the Picture Frame at sprite 8
Safe to run again: it redraws the same places from the plain originals.

Usage: frame_styles.py <ArtStudio/assets dir>
"""
import colorsys
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from set_frames import recolour

FIRST_X, FIRST_ITEM, WIDTH = 560, 67, 1280

# Darkest to lightest.
STYLES = [
    ("pine", [(110, 72, 40), (160, 110, 60), (200, 150, 86), (226, 184, 118), (240, 208, 150), (250, 228, 182)], "knots"),
    ("oak", [(64, 38, 22), (104, 64, 34), (142, 94, 50), (172, 122, 66), (196, 148, 86), (220, 178, 116)], "pegs"),
    ("birch", [(150, 144, 132), (196, 190, 176), (222, 216, 202), (236, 232, 220), (246, 244, 236), (252, 250, 244)], "bark"),
    ("white", [(150, 150, 156), (190, 190, 196), (214, 214, 220), (230, 230, 234), (242, 242, 246), (252, 252, 254)], None),
    ("walnut", [(36, 20, 14), (60, 34, 22), (86, 52, 32), (112, 72, 44), (138, 94, 58), (164, 118, 76)], "studs"),
    ("red-lacquer", [(70, 12, 16), (112, 22, 26), (152, 32, 34), (186, 48, 44), (212, 72, 60), (232, 104, 84)], "gold"),
    ("rope", [(96, 70, 40), (136, 104, 64), (170, 136, 88), (196, 164, 112), (218, 190, 140), (236, 214, 170)], "twist"),
    ("prismatic", [(70, 70, 90), (110, 110, 130), (150, 150, 170), (190, 190, 205), (220, 220, 232), (245, 245, 252)], "rainbow"),
    ("qi", [(40, 14, 58), (68, 24, 96), (98, 40, 134), (132, 64, 170), (168, 100, 204), (200, 140, 230)], "diamond"),
]
GOLD, GOLD_DARK = (236, 196, 84), (176, 128, 40)
BRASS = (214, 178, 96)


def border(tile):
    """The frame's pixels: everything opaque that isn't the pale canvas inside."""
    px = tile.load()
    w, h = tile.size
    inside = px[w // 2, h // 2][:3]
    return [(x, y) for y in range(h) for x in range(w) if px[x, y][3] and px[x, y][:3] != inside]


def touch(tile, kind, dark):
    px = tile.load()
    w, h = tile.size
    frame = border(tile)
    corners = ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2))
    if kind == "knots":
        for x, y in ((w // 3, 1), (2 * w // 3, h - 2), (1, h // 2)):
            px[x, y] = dark + (255,)
    elif kind == "pegs":
        for x, y in corners:
            px[x, y] = dark + (255,)
    elif kind == "bark":
        for i, (x, y) in enumerate(frame):
            if (x * 7 + y * 3) % 11 == 0:
                px[x, y] = (70, 64, 58, 255)
    elif kind == "studs":
        for x, y in corners:
            px[x, y] = BRASS + (255,)
    elif kind == "gold":
        for x, y in corners:
            px[x, y] = GOLD + (255,)
        px[w // 2, 0] = GOLD + (255,)
        px[w // 2 - 1, 0] = GOLD_DARK + (255,)
    elif kind == "twist":
        # Diagonal strands along the border, like a rope's lay.
        for x, y in frame:
            r, g, b, a = px[x, y]
            if (x + y) % 3 == 0:
                px[x, y] = (max(0, r - 40), max(0, g - 36), max(0, b - 30), a)
    elif kind == "rainbow":
        # Each border pixel takes a hue by its place round the frame, keeping
        # its lightness.
        cx, cy = (w - 1) / 2, (h - 1) / 2
        import math
        for x, y in frame:
            r, g, b, a = px[x, y]
            hue = (math.atan2(y - cy, x - cx) / (2 * math.pi)) % 1.0
            light = (r + g + b) / (3 * 255)
            rr, gg, bb = colorsys.hls_to_rgb(hue, 0.25 + 0.55 * light, 0.75)
            px[x, y] = (int(rr * 255), int(gg * 255), int(bb * 255), a)
    elif kind == "diamond":
        cx = w // 2
        for dx, dy, c in ((0, 0, GOLD), (-1, 1, GOLD_DARK), (1, 1, GOLD_DARK), (0, 1, GOLD), (0, 2, GOLD_DARK)):
            if 0 <= cx + dx < w and dy < h:
                px[cx + dx, dy] = c + (255,)
    return tile


def main():
    assets = sys.argv[1]
    furniture_path = os.path.join(assets, "furniture.png")
    furniture = Image.open(furniture_path).convert("RGBA")
    if furniture.width < WIDTH:
        wider = Image.new("RGBA", (WIDTH, furniture.height), (0, 0, 0, 0))
        wider.paste(furniture.crop((0, 0, min(furniture.width, FIRST_X), furniture.height)), (0, 0))
        furniture = wider
    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16

    def at(index):
        return (index % per_row * 16, index // per_row * 16)

    for n, (name, palette, kind) in enumerate(STYLES):
        ox = FIRST_X + 80 * n
        for (sx, size), dx in (((0, 32), 0), ((32, 48), 32)):
            tile = recolour(furniture.crop((sx, 0, sx + size, size)).copy(), palette)
            furniture.paste((0, 0, 0, 0), (ox + dx, 0, ox + dx + size, size))
            furniture.paste(touch(tile, kind, palette[0]), (ox + dx, 0))
        x, y = at(8)
        icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), palette, keep_inside=False)
        if kind in ("rainbow", "diamond", "gold", "studs"):
            icon = touch(icon, kind, palette[0])
        tx, ty = at(FIRST_ITEM + n)
        items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
        items.alpha_composite(icon, (tx, ty))
    furniture.save(furniture_path)
    items.save(items_path)
    print("wrote", furniture_path, "and", items_path)


if __name__ == "__main__":
    main()
