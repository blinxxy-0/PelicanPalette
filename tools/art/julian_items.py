"""Draws the two items of Julian Vane's story (1.2), pixel by pixel, to
[CP] Pelican Palette/assets/julian/items.png (96x48):
  - (0, 0) 16x16: the Old Catalogue, the booklet from Sable's first show,
    cream and worn, a little framed picture on its cover
  - (16, 0) 32x32: the framed review, a newspaper page in a wooden frame
    (wall decor, the reward at 10 hearts)
  - (48, 0), (64, 0), (80, 0) 16x16: the Fair painting contest's ribbons,
    blue for first, red for second, yellow for third (wall decor)
  - (48, 16) 32x32: the villagers' contest entries, a stack of painted
    canvases (PixelLab, art/back_room/fair_entries.png), at the Fair
Outlines are dark shades of their own colours, never black.

Usage: julian_items.py <out.png>
"""
import os, sys
from PIL import Image

CREAM, CREAM_LIGHT, CREAM_SHADE, CREAM_EDGE = (232, 214, 176), (246, 234, 206), (204, 182, 140), (128, 104, 72)
RUST, TEAL = (168, 84, 50), (64, 128, 128)
INK, INK_LIGHT = (84, 64, 58), (150, 132, 116)
PAPER, PAPER_SHADE = (238, 232, 216), (210, 202, 184)
WOOD, WOOD_LIGHT, WOOD_DARK = (140, 88, 52), (176, 120, 72), (86, 50, 30)


def rect(px, x0, y0, x1, y1, colour):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = colour + (255,)


def catalogue(px):
    # The booklet, slightly tilted by a pixel at its spine, corner worn.
    rect(px, 3, 2, 12, 14, CREAM_EDGE)
    rect(px, 4, 3, 11, 13, CREAM)
    rect(px, 4, 3, 11, 3, CREAM_LIGHT)
    rect(px, 4, 13, 11, 13, CREAM_SHADE)
    rect(px, 4, 3, 4, 13, CREAM_SHADE)                 # the spine
    px[12, 2] = (0, 0, 0, 0)                           # a worn corner
    px[11, 3] = CREAM_SHADE + (255,)
    # The cover picture: a tiny framed landscape.
    rect(px, 6, 5, 10, 9, RUST)
    rect(px, 7, 6, 9, 7, TEAL)
    rect(px, 7, 8, 9, 8, (110, 150, 80))
    # The title, two lines of ink.
    rect(px, 6, 11, 10, 11, INK)
    rect(px, 7, 12, 9, 12, INK_LIGHT)


def review(px, ox):
    # A wooden frame with a newspaper page in it.
    rect(px, ox + 2, 1, ox + 29, 30, WOOD_DARK)
    rect(px, ox + 3, 2, ox + 28, 29, WOOD)
    rect(px, ox + 3, 2, ox + 28, 2, WOOD_LIGHT)
    rect(px, ox + 3, 2, ox + 3, 29, WOOD_LIGHT)
    rect(px, ox + 5, 4, ox + 26, 27, PAPER_SHADE)
    rect(px, ox + 5, 4, ox + 25, 26, PAPER)
    # The masthead and the headline.
    rect(px, ox + 7, 6, ox + 23, 6, INK)
    rect(px, ox + 7, 9, ox + 19, 10, INK)
    # A small picture of a river, top left, and columns of text.
    rect(px, ox + 7, 13, ox + 13, 18, (120, 150, 170))
    rect(px, ox + 7, 16, ox + 13, 18, (96, 138, 82))
    rect(px, ox + 8, 15, ox + 12, 15, (170, 196, 214))
    for y in range(13, 25, 2):
        rect(px, ox + 15, y, ox + 23, y, INK_LIGHT)
    for y in range(20, 25, 2):
        rect(px, ox + 7, y, ox + 13, y, INK_LIGHT)


RIBBONS = [((70, 110, 200), (40, 66, 140), (130, 170, 235)),     # first: blue
           ((200, 60, 56), (128, 34, 36), (236, 120, 106)),     # second: red
           ((232, 186, 56), (156, 112, 30), (250, 222, 120))]   # third: yellow


def ribbon(px, ox, colours):
    base, dark, light = colours
    # Two tails hanging below, notched at the ends.
    for x0 in (ox + 5, ox + 9):
        rect(px, x0, 9, x0 + 2, 14, base)
        rect(px, x0, 9, x0, 14, dark)
        px[x0 + 1, 14] = (0, 0, 0, 0)
    # The rosette: a ring of points around a round middle.
    for y in range(1, 11):
        for x in range(ox + 3, ox + 13):
            dx, dy = x - (ox + 7.5), y - 5.5
            r = (dx * dx + dy * dy) ** 0.5
            if r <= 4.8:
                points = r > 3.6 and (x + y) % 2 == 0
                px[x, y] = (dark if r > 4.2 or points else light if r < 1.8 else base) + (255,)
    px[ox + 7, 4] = (255, 255, 255, 255)


# The villagers' contest entries: the user's pick of the PixelLab stacks
# (art/back_room/entries_batch.json), 32x32, leaning against the fence by
# Julian's table at the Fair.
ENTRIES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "art", "back_room", "fair_entries.png")


def entries(im, ox, oy):
    stack = Image.open(ENTRIES).convert("RGBA")
    stack = stack.crop(stack.getbbox())
    im.alpha_composite(stack, (ox + (32 - stack.width) // 2, oy + 32 - stack.height))

def main():
    out = sys.argv[1]
    im = Image.new("RGBA", (96, 48), (0, 0, 0, 0))
    px = im.load()
    catalogue(px)
    review(px, 16)
    for i, colours in enumerate(RIBBONS):
        ribbon(px, 48 + 16 * i, colours)
    entries(im, 48, 16)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
