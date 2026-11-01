"""Draws the two items of Julian Vane's story (1.2), pixel by pixel, to
[CP] Pelican Palette/assets/julian/items.png (48x32):
  - (0, 0) 16x16: the Old Catalogue, the booklet from Sable's first show,
    cream and worn, a little framed picture on its cover
  - (16, 0) 32x32: the framed review, a newspaper page in a wooden frame
    (wall decor, the reward at 10 hearts)
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


def main():
    out = sys.argv[1]
    im = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
    px = im.load()
    catalogue(px)
    review(px, 16)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
