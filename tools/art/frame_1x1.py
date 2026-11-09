"""Draws the 1x1 frames small paintings hang in (1.3): each frame style's
2x2 frame (row 0 of ArtStudio/assets/furniture.png) reduced to 16 pixels,
in the row three under its 2x2 frame's top (y 48 for the first band, y 144
for the second), so a style's 1x1 sprite index is its 2x2 index + 240 (80
tiles a row, three rows). The outer ring keeps
each style's colours and marks; the middle is covered by the picture
(Painting/ArtHooks.cs draws it in the 14x14 inside, a cell to a pixel).

Run after frame_styles.py, set_frames.py and gold_leaf_frame.py, which
draw the 2x2 frames. Safe to run again.

Usage: frame_1x1.py <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

DOWN = 48          # from a 2x2 frame's top to its 1x1's
# Each style's 2x2 sprite index (ArtItems.AddFurniture): plain, moonlit,
# gold leaf, the museum's four set frames, then the nine more styles, then
# the second band's (frame_styles.py): the Founders' Frame.
SMALL = [0, 5, 10] + [15 + 5 * n for n in range(4)] + [35 + 5 * n for n in range(9)] + [480]


def ring(big):
    """A 16x16 frame: a 1-pixel ring taken from the 2x2 frame's main wood,
    two pixels in from its edge (outside it are a dark outline and a pale
    highlight, which melt away at this size), along each side so a style's pattern
    carries over; the bottom and right a shade darker, for depth."""
    src = big.load()
    out = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = out.load()
    for y in range(16):
        for x in range(16):
            if 0 < x < 15 and 0 < y < 15:
                continue
            sx = 2 if x == 0 else 29 if x == 15 else 2 * x
            sy = 2 if y == 0 else 29 if y == 15 else 2 * y
            r, g, b, a = src[sx, sy]
            if x == 15 or y == 15:
                r, g, b = int(r * 0.85), int(g * 0.85), int(b * 0.85)
            px[x, y] = (r, g, b, 255)
    return out


def main():
    path = os.path.join(sys.argv[1], "furniture.png")
    sheet = Image.open(path).convert("RGBA")
    per_row = sheet.width // 16
    bottom = max(index // per_row * 16 for index in SMALL) + DOWN + 16
    if sheet.height < bottom:
        taller = Image.new("RGBA", (sheet.width, bottom), (0, 0, 0, 0))
        taller.paste(sheet, (0, 0))
        sheet = taller
    for index in SMALL:
        x, y = index % per_row * 16, index // per_row * 16
        frame = ring(sheet.crop((x, y, x + 32, y + 32)))
        sheet.paste((0, 0, 0, 0), (x, y + DOWN, x + 16, y + DOWN + 16))
        sheet.alpha_composite(frame, (x, y + DOWN))
    sheet.save(path)
    print("wrote", len(SMALL), "1x1 frames into", path, sheet.size)


if __name__ == "__main__":
    main()
