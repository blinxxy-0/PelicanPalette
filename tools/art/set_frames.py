"""Draws the four collection frames (1.3, the museum's restored-painting
sets' rewards), the way gold_leaf_frame.py draws the gold-leaf one: the
plain frames recoloured, each with its own small ornament.
  - ArtStudio/assets/furniture.png, widened to 560x48: each style's 2x2 and
    3x3 framed paintings, 80 pixels apart from x 240 (sprite indices 15/17,
    20/22, 25/27, 30/32)
  - ArtStudio/assets/items.png: each style's frame item at sprites 42 to 45,
    from the Picture Frame at sprite 8
Safe to run again: it redraws the same places from the plain originals.

Usage: set_frames.py <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import moonlit_frame

# Darkest to lightest, and the ornament's two colours.
STYLES = [
    ("driftwood", [(58, 56, 60), (92, 88, 86), (128, 122, 114), (162, 156, 146), (196, 190, 178), (224, 220, 208)], (95, 176, 227), (200, 236, 250)),
    ("iron", [(30, 30, 38), (52, 52, 62), (78, 78, 90), (108, 108, 120), (140, 140, 152), (176, 176, 188)], (194, 120, 70), (230, 170, 110)),
    ("sandstone", [(110, 66, 40), (160, 104, 56), (200, 146, 82), (222, 178, 112), (238, 206, 148), (250, 232, 190)], (60, 199, 180), (190, 250, 240)),
    ("dwarvish", [(52, 34, 26), (92, 58, 34), (136, 88, 44), (176, 122, 56), (206, 156, 74), (232, 192, 108)], (60, 199, 180), (190, 250, 240)),
]


def recolour(tile, palette, keep_inside=True):
    saved = moonlit_frame.MOONLIT
    moonlit_frame.MOONLIT = palette
    try:
        return moonlit_frame.recolour(tile, keep_inside)
    finally:
        moonlit_frame.MOONLIT = saved


def ornament(tile, dark, light):
    """A gem over the middle of the top border, and a stud in each corner."""
    px = tile.load()
    w, h = tile.size
    cx = w // 2
    for dx, dy, c in ((0, 0, light), (-1, 0, dark), (0, 1, dark), (-1, 1, dark)):
        px[cx + dx, dy] = c + (255,)
    for sx, sy in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        px[sx, sy] = light + (255,)
    return tile


def main():
    assets = sys.argv[1]
    furniture_path = os.path.join(assets, "furniture.png")
    furniture = Image.open(furniture_path).convert("RGBA")
    if furniture.width < 560:
        wider = Image.new("RGBA", (560, furniture.height), (0, 0, 0, 0))
        wider.paste(furniture.crop((0, 0, min(furniture.width, 240), furniture.height)), (0, 0))
        furniture = wider
    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16

    def at(index):
        return (index % per_row * 16, index // per_row * 16)

    for n, (name, palette, dark, light) in enumerate(STYLES):
        ox = 240 + 80 * n
        for (sx, size), dx in (((0, 32), 0), ((32, 48), 32)):
            tile = recolour(furniture.crop((sx, 0, sx + size, size)).copy(), palette)
            furniture.paste((0, 0, 0, 0), (ox + dx, 0, ox + dx + size, size))
            furniture.paste(ornament(tile, dark, light), (ox + dx, 0))
        x, y = at(8)
        icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), palette, keep_inside=False)
        ipx = icon.load()
        for yy in range(3):
            for xx in range(2):
                ipx[xx, yy] = (0, 0, 0, 0)
        for px_, py_, c in ((7, 1, light), (8, 1, light), (7, 2, dark), (8, 2, dark)):
            ipx[px_, py_] = c + (255,)
        tx, ty = at(42 + n)
        items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
        items.alpha_composite(icon, (tx, ty))
    furniture.save(furniture_path)
    items.save(items_path)
    print("wrote", furniture_path, "and", items_path)


if __name__ == "__main__":
    main()
