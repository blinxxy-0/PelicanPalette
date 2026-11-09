"""Draws the Carved Ash Frame (1.4): the frame Tess carves and sells at her counter in the
Gilded Vale's Studio III, from her 8-heart event (docs/tess_brief.md; the user, 2026-11-09).
Rare, Thrift (ArtStudio/Painting/Frames.cs): she wastes nothing.

Made the way frame_styles.py makes the 1.3 frames: the plain frame (furniture.png's first 2x2
and 3x3) recoloured by lightness into pale ash, with chisel notches cut along its border; its
1x1 the way frame_1x1.py makes theirs; its item icon from the Picture Frame's (items.png 8).
furniture.png is full (80 tiles a row), so the ash frame's framed paintings have a sheet of
their own:

  ArtStudio/assets/furniture_more.png (80x64, 5 tiles a row):
      sprite 0  the 2x2 frame        sprite 2  the 3x3 frame        sprite 15  the 1x1 frame
  ArtStudio/assets/items.png: the frame item at sprite 76

Usage: ash_frame.py <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from set_frames import recolour
from frame_1x1 import ring

# Darkest to lightest: pale ash, a little grey.
ASH = [(98, 84, 66), (142, 126, 100), (180, 164, 134), (206, 192, 162), (226, 214, 188), (242, 234, 214)]
ITEM_SPRITE = 76


def notches(tile, dark):
    """Chisel cuts along the frame: every sixth pixel on the border's middle line a little
    darker, like a row of gouge marks (subtle: the frame still reads as a frame)."""
    px = tile.load()
    w, h = tile.size
    inside = px[w // 2, h // 2][:3]
    frame = {(x, y) for y in range(h) for x in range(w) if px[x, y][3] and px[x, y][:3] != inside}
    for x in range(4, w - 4, 6):
        for y in (1, h - 2):
            if (x, y) in frame:
                px[x, y] = dark + (255,)
    for y in range(4, h - 4, 6):
        for x in (1, w - 2):
            if (x, y) in frame:
                px[x, y] = dark + (255,)
    return tile


def main():
    assets = sys.argv[1]
    plain = Image.open(os.path.join(assets, "furniture.png")).convert("RGBA")
    out = Image.new("RGBA", (80, 64), (0, 0, 0, 0))
    medium = notches(recolour(plain.crop((0, 0, 32, 32)).copy(), ASH), ASH[1])
    large = notches(recolour(plain.crop((32, 0, 80, 48)).copy(), ASH), ASH[1])
    out.alpha_composite(medium, (0, 0))
    out.alpha_composite(large, (32, 0))
    out.alpha_composite(ring(medium), (0, 48))
    out.save(os.path.join(assets, "furniture_more.png"))

    items_path = os.path.join(assets, "items.png")
    items = Image.open(items_path).convert("RGBA")
    per_row = items.width // 16
    x, y = 8 % per_row * 16, 8 // per_row * 16
    icon = recolour(items.crop((x, y, x + 16, y + 16)).copy(), ASH, keep_inside=False)
    tx, ty = ITEM_SPRITE % per_row * 16, ITEM_SPRITE // per_row * 16
    items.paste((0, 0, 0, 0), (tx, ty, tx + 16, ty + 16))
    items.alpha_composite(icon, (tx, ty))
    items.save(items_path)
    print("wrote furniture_more.png and the item icon at sprite", ITEM_SPRITE)


if __name__ == "__main__":
    main()
