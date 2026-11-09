"""Draws 1.3's frame styles beyond the collection's (docs/design/1.3_plan.md
section 5), the way set_frames.py draws those: the plain frame recoloured
by lightness, each with a small touch of its own.
  - common: pine, oak, birch, painted white
  - uncommon: walnut, red lacquer, rope
  - legendary: prismatic, Qi's
  - epic: the Founders' Frame (the Gilded Vale's Founders' easel; Debug-only
    in the mod for now, and PLACEHOLDER art until the art batch's piece lands)
Writes:
  - ArtStudio/assets/furniture.png, widened to 1280x48: each style's 2x2 and
    3x3 framed paintings, 80 pixels apart from x 560 (sprite indices
    35/37, 40/42, ... 75/77). That fills the sheet's first band (80 tiles a
    row), so styles after the ninth go in a second band at y 96 (row 6, under
    Margot's study board), 80 pixels apart from x 0 (sprite indices 480/482,
    ...), and the sheet is made 160 tall for their 1x1s (frame_1x1.py)
  - ArtStudio/assets/items.png: each style's frame item at sprites 67 to 76,
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
FIRST_BAND, SECOND_BAND_Y, HEIGHT = 9, 96, 160    # (ArtItems.MoreFrameFurnitureIndex)

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
    # PLACEHOLDER: old honey-coloured wood, warm as if just carved, with carved notches and a gold leaf.
    ("founders", [(78, 44, 22), (122, 74, 36), (164, 110, 52), (198, 146, 72), (224, 180, 102), (244, 214, 146)], "carved"),
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
        # Birch bark's short dark dashes, running along each side of the frame.
        def edge(x, y):
            return any(not (0 <= x + dx < w and 0 <= y + dy < h) or px[x + dx, y + dy][3] == 0
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        for x, y in frame:
            if edge(x, y):
                continue
            along = x if min(y, h - 1 - y) <= min(x, w - 1 - x) else y
            across = y if along == x else x
            if (along + 2 * across) % 5 in (0, 1) and (along * 3 + across) % 7 < 3:
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
    elif kind == "carved":
        # Notches cut across the wood at even steps round the frame, a gold leaf over the middle of
        # the top, and gold in the corners.
        def inner(x, y):
            return all(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3]
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        for x, y in frame:
            if not inner(x, y):
                continue
            along = x if min(y, h - 1 - y) <= min(x, w - 1 - x) else y
            if along % 4 == 0:
                r, g, b, a = px[x, y]
                px[x, y] = (max(0, r - 46), max(0, g - 40), max(0, b - 28), a)
        # (only on the wood: the item's icon has empty corners round its frame)
        for x, y in corners:
            if px[x, y][3]:
                px[x, y] = GOLD + (255,)
        cx = w // 2
        top = min(y for x, y in frame if x == cx)
        for dx, dy, c in ((0, 0, GOLD), (-1, 0, GOLD_DARK), (0, 1, GOLD_DARK), (-1, 1, GOLD)):
            if 0 <= cx + dx < w and top + dy < h and px[cx + dx, top + dy][3]:
                px[cx + dx, top + dy] = c + (255,)
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
    if furniture.height < HEIGHT:
        taller = Image.new("RGBA", (furniture.width, HEIGHT), (0, 0, 0, 0))
        taller.paste(furniture, (0, 0))
        furniture = taller
    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16

    def at(index):
        return (index % per_row * 16, index // per_row * 16)

    for n, (name, palette, kind) in enumerate(STYLES):
        ox, oy = (FIRST_X + 80 * n, 0) if n < FIRST_BAND else (80 * (n - FIRST_BAND), SECOND_BAND_Y)
        for (sx, size), dx in (((0, 32), 0), ((32, 48), 32)):
            tile = recolour(furniture.crop((sx, 0, sx + size, size)).copy(), palette)
            furniture.paste((0, 0, 0, 0), (ox + dx, oy, ox + dx + size, oy + size))
            furniture.paste(touch(tile, kind, palette[0]), (ox + dx, oy))
        x, y = at(8)
        icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), palette, keep_inside=False)
        if kind:
            icon = touch(icon, kind, palette[0])
        tx, ty = at(FIRST_ITEM + n)
        items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
        items.alpha_composite(icon, (tx, ty))
    furniture.save(furniture_path)
    items.save(items_path)
    print("wrote", furniture_path, "and", items_path)


if __name__ == "__main__":
    main()
