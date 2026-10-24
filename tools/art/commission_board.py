"""The art shop's commission board (48x32: 3x2 tiles, hung on the back wall).

A framed corkboard with pinned requests, drawn in vanilla's own colours:
the cork and paper from Pierre's notes board, the frame and its wall shadow
from his framed price list. The papers are commissions: written notes, one
pencilled thumbnail sketch of a landscape, and a couple of sticky notes.

Usage: commission_board.py <out.png>
"""
import sys
from PIL import Image

W, H = 48, 29             # drawn 3px down in the 48x32 tile block, clear of the ceiling trim
TOP = 3
OUTLINE = (77, 12, 4)
WOOD_LIGHT, WOOD, WOOD_DARK = (176, 122, 38), (158, 105, 26), (124, 60, 4)
CORK_A, CORK_B, CORK_SHADE = (216, 137, 62), (231, 160, 78), (165, 106, 56)
PAPER, PAPER_EDGE, INK, PIN = (255, 253, 233), (232, 220, 190), (112, 108, 104), (70, 66, 72)
SHADOW = (0, 0, 0, 70)


def main():
    out = sys.argv[1]
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    def rect(x0, y0, x1, y1, colour):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                px[x, y] = colour if len(colour) == 4 else colour + (255,)

    # Shadow on the wall (right and bottom), then the frame.
    rect(2, 2, W - 1, H - 1, SHADOW)
    rect(0, 0, W - 2, H - 2, OUTLINE)
    rect(1, 1, W - 3, H - 3, WOOD)
    rect(1, 1, W - 3, 1, WOOD_LIGHT)                  # light along the top edge
    rect(1, 1, 1, H - 3, WOOD_LIGHT)
    rect(1, H - 3, W - 3, H - 3, WOOD_DARK)           # shade along the bottom and right
    rect(W - 3, 2, W - 3, H - 3, WOOD_DARK)
    rect(3, 3, W - 5, H - 5, WOOD_DARK)               # inner edge
    for y in range(4, H - 5):                         # cork, in vanilla's 2px checker
        for x in range(4, W - 5):
            px[x, y] = (CORK_A if ((x // 2) + (y // 2)) % 2 else CORK_B) + (255,)
    rect(4, 4, W - 6, 4, CORK_SHADE)                  # the frame's shadow on the cork
    rect(4, 4, 4, H - 6, CORK_SHADE)

    def note(x, y, w, h, lines=True, pin=PIN):
        rect(x + 1, y + 1, x + w, y + h, CORK_SHADE)  # its shadow on the cork
        rect(x, y, x + w - 1, y + h - 1, PAPER)
        rect(x, y + h - 1, x + w - 1, y + h - 1, PAPER_EDGE)
        if lines:
            for ly in range(y + 3, y + h - 2, 2):
                end = x + w - 2 - ((ly * 3) % 3)
                for lx in range(x + 2, end):
                    if (lx + ly) % 5 != 0:            # little gaps between words
                        px[lx, ly] = INK + (255,)
        px[x + w // 2, y] = pin + (255,)
        px[x + w // 2, y + 1] = pin + (255,)

    note(7, 7, 9, 11)
    note(19, 6, 11, 10, lines=False, pin=(200, 60, 60))
    # The thumbnail: a pencilled landscape with a little colour, like a request sketch.
    sky, hill, dark, sun = (150, 200, 230), (120, 180, 80), (80, 140, 60), (255, 203, 106)
    rect(21, 8, 27, 11, sky)
    rect(25, 9, 26, 10, sun)
    rect(21, 12, 27, 13, hill)
    rect(23, 11, 25, 11, hill)
    rect(21, 13, 22, 13, dark)
    note(33, 8, 8, 10, pin=(80, 110, 200))
    note(12, 19, 8, 6)
    rect(24, 19, 27, 22, (251, 177, 226))             # sticky notes
    rect(29, 20, 32, 23, (255, 203, 106))
    rect(36, 20, 39, 23, (130, 210, 120))
    tiles = Image.new("RGBA", (48, 32), (0, 0, 0, 0))
    tiles.alpha_composite(im, (0, TOP))
    tiles.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
