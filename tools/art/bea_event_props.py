"""PLACEHOLDER props for Bea Thistle's heart events (docs/vale_events.md; built by
tools/build_bea_events.py), drawn here until the art pass replaces them. No base-game pixels.

Writes [CP] Gilded Vale (dev)/assets/bea/event_props.png, a sheet of 16 x 16 tiles, ten across:
  row 0:  0 the hotel group's folder, lying on the counter's front edge (left of the tile)
          1 a guest book lying open on the counter (the same place)
          2 a stack of guest books, on the floor
          3 (empty)
          4 dust
  row 1: 10 a trunk, its lid open, books inside     11 the same trunk, shut
  tall (16 x 32, rows 0-1): 5 a dressmaker's dummy, 6 a broken easel, 7 a low shelf of guest books
          with a card (the upstairs hall, after her 10 hearts), 8 the inn's new sign, 9 an old desk
and [CP] Gilded Vale (dev)/assets/bea/attic_sketch.png, 32 x 32: the folded sketch from the attic
(the Vale House from the plaza, signed with an initial), framed, for the furniture keepsake.

Outlines are a darker shade of the colour they border, never black (vanillafy.selout).

Usage: python tools/art/bea_event_props.py
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout, outline_shade

PACK = "[CP] Gilded Vale (dev)"
OUT = os.path.join(PACK, "assets", "bea")

WOOD, WOOD_L, WOOD_D = (138, 90, 50), (176, 122, 69), (104, 64, 36)
LEATHER, LEATHER_L = (118, 66, 42), (150, 92, 58)
BRASS, BRASS_L = (196, 150, 64), (232, 196, 108)
CREAM, CREAM_D = (240, 230, 206), (212, 198, 170)
PLUM, PLUM_L = (118, 56, 88), (150, 84, 118)
GREEN, BLUE, OCHRE = (78, 112, 70), (70, 88, 140), (184, 134, 64)
LINEN, LINEN_D = (220, 204, 178), (188, 168, 140)
INK = (96, 82, 74)


class Canvas:
    def __init__(self, w, h):
        self.im = Image.new("RGBA", (w, h))
        self.px = self.im.load()

    def dot(self, x, y, c, a=255):
        if 0 <= x < self.im.width and 0 <= y < self.im.height:
            self.px[x, y] = tuple(c) + (a,)

    def fill(self, x0, y0, x1, y1, c, a=255):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, c, a)

    def box(self, x0, y0, x1, y1, c):
        """A filled rectangle with its edge a darker shade of its own colour."""
        self.fill(x0, y0, x1, y1, c)
        edge = outline_shade(c)
        for x in range(x0, x1 + 1):
            self.dot(x, y0, edge)
            self.dot(x, y1, edge)
        for y in range(y0, y1 + 1):
            self.dot(x0, y, edge)
            self.dot(x1, y, edge)

    def hline(self, x0, x1, y, c):
        for x in range(x0, x1 + 1):
            self.dot(x, y, c)

    def vline(self, x, y0, y1, c):
        for y in range(y0, y1 + 1):
            self.dot(x, y, c)


def folder(c, ox, oy):
    # flat leather folder seen from the front, lying on the counter's front edge
    c.box(ox + 2, oy + 11, ox + 11, oy + 14, LEATHER)
    c.hline(ox + 3, ox + 10, oy + 12, LEATHER_L)
    c.dot(ox + 9, oy + 13, BRASS_L)                      # the clasp
    c.hline(ox + 3, ox + 10, oy + 15, outline_shade(LEATHER))


def open_book(c, ox, oy):
    c.box(ox + 1, oy + 11, ox + 12, oy + 15, PLUM)       # the cover, under the pages
    c.fill(ox + 2, oy + 11, ox + 6, oy + 13, CREAM)
    c.fill(ox + 7, oy + 11, ox + 11, oy + 13, CREAM)
    c.vline(ox + 6, oy + 11, oy + 13, CREAM_D)           # the gutter
    c.hline(ox + 3, ox + 5, oy + 12, INK)                # a signature
    c.dot(ox + 9, oy + 12, INK)                          # a little sketch
    c.dot(ox + 8, oy + 12, GREEN)


def book_stack(c, ox, oy):
    for i, (col, x0, x1) in enumerate(((PLUM, 3, 12), (GREEN, 4, 12), (BLUE, 3, 11), (OCHRE, 4, 12))):
        y = oy + 14 - i * 3
        c.box(ox + x0, y - 2, ox + x1, y, col)
        c.hline(ox + x0 + 1, ox + x1 - 2, y - 1, CREAM)   # the pages' edge
    c.dot(ox + 5, oy + 1, CREAM_D)                        # dust on top


def dust(c, ox, oy):
    for x, y, r in ((5, 10, 3), (10, 9, 2), (8, 6, 2), (3, 6, 1), (12, 5, 1)):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r + 1:
                    c.dot(ox + x + dx, oy + y + dy, (206, 196, 184), 120)


def trunk(c, ox, oy, open_):
    c.box(ox + 1, oy + 8, ox + 14, oy + 15, LEATHER)
    c.fill(ox + 2, oy + 9, ox + 13, oy + 9, LEATHER_L)
    for x in (ox + 4, ox + 11):
        c.vline(x, oy + 9, oy + 14, BRASS)                # the bands
    c.dot(ox + 7, oy + 10, BRASS_L)
    c.dot(ox + 8, oy + 10, BRASS_L)                       # the lock
    if open_:
        c.box(ox + 1, oy + 1, ox + 14, oy + 6, LEATHER)   # the lid, standing up behind
        c.fill(ox + 2, oy + 2, ox + 13, oy + 5, (92, 50, 34))
        c.fill(ox + 2, oy + 7, ox + 13, oy + 8, (92, 50, 34))   # the inside
        c.fill(ox + 3, oy + 6, ox + 5, oy + 8, PLUM)
        c.fill(ox + 6, oy + 7, ox + 8, oy + 8, GREEN)
        c.fill(ox + 9, oy + 6, ox + 11, oy + 8, BLUE)     # books inside
    else:
        c.box(ox + 1, oy + 6, ox + 14, oy + 8, LEATHER_L)  # the shut lid


def dummy(c, ox, oy):
    # a dressmaker's dummy: shoulders, a pinched waist, hips, on a pole and three feet
    rows = {3: (5, 10), 4: (3, 12), 5: (2, 13), 6: (2, 13), 7: (3, 12), 8: (3, 12), 9: (4, 11), 10: (4, 11),
            11: (5, 10), 12: (5, 10), 13: (4, 11), 14: (3, 12), 15: (3, 12), 16: (3, 12), 17: (4, 11)}
    for y, (x0, x1) in rows.items():
        c.hline(ox + x0, ox + x1, oy + y, LINEN)
        c.hline(ox + x1 - 2, ox + x1, oy + y, LINEN_D)        # its shaded side
    c.hline(ox + 5, ox + 10, oy + 11, PLUM)                   # a tape measure round the waist
    c.dot(ox + 10, oy + 12, PLUM)
    c.dot(ox + 10, oy + 13, PLUM)
    c.box(ox + 6, oy + 1, ox + 9, oy + 2, WOOD)               # the neck knob
    c.vline(ox + 7, oy + 18, oy + 27, WOOD_D)
    c.vline(ox + 8, oy + 18, oy + 27, WOOD)                   # the pole
    for i in range(4):
        c.dot(ox + 6 - i, oy + 27 + i, WOOD)
        c.dot(ox + 9 + i, oy + 27 + i, WOOD)                  # the feet
    c.vline(ox + 8, oy + 28, oy + 30, WOOD_D)


def easel(c, ox, oy):
    # two legs standing, the third snapped, a torn canvas slumped on it
    for i in range(26):
        c.dot(ox + 4 + i // 6, oy + 4 + i, WOOD)
        c.dot(ox + 11 - i // 6, oy + 4 + i, WOOD)
    for i in range(9):
        c.dot(ox + 13 - i // 3, oy + 21 + i, WOOD_D)      # the broken leg, lying
    c.box(ox + 3, oy + 7, ox + 12, oy + 17, CREAM)        # the canvas
    c.fill(ox + 4, oy + 12, ox + 11, oy + 16, (176, 200, 214))
    c.fill(ox + 5, oy + 14, ox + 9, oy + 16, GREEN)
    for i in range(5):
        c.dot(ox + 8 + i // 2, oy + 8 + i, CREAM_D)       # the tear
    c.box(ox + 2, oy + 17, ox + 13, oy + 18, WOOD_L)      # the ledge


def shelf(c, ox, oy):
    # a low bookcase against the wall: its top over the wainscot, its base on the floor
    c.box(ox + 1, oy + 9, ox + 14, oy + 31, WOOD)
    c.fill(ox + 3, oy + 11, ox + 12, oy + 19, WOOD_D)
    c.fill(ox + 3, oy + 21, ox + 12, oy + 29, WOOD_D)
    c.hline(ox + 2, ox + 13, oy + 20, WOOD_L)
    books = (PLUM, GREEN, BLUE, OCHRE, PLUM_L, GREEN, BLUE)
    for i, col in enumerate(books):
        x = ox + 3 + i
        c.vline(x, oy + 13 + (i % 2), oy + 19, col)
        c.vline(x + (2 if i < 5 else -5), oy + 23 + (i % 3 == 0), oy + 29, books[(i + 3) % 7])
    c.box(ox + 5, oy + 4, ox + 10, oy + 8, CREAM)         # the card on top
    c.hline(ox + 6, ox + 9, oy + 6, PLUM)                 # 'Read us. Jam permitted.'


def sign(c, ox, oy):
    c.vline(ox + 7, oy + 13, oy + 30, WOOD_D)
    c.vline(ox + 8, oy + 13, oy + 30, WOOD)               # the post
    c.box(ox + 1, oy + 3, ox + 14, oy + 13, WOOD)         # the board
    c.box(ox + 2, oy + 4, ox + 13, oy + 12, CREAM)
    for y in (6, 8, 10):
        c.hline(ox + 4, ox + 11 - (y == 10) * 3, oy + y, PLUM)   # the lettering
    c.dot(ox + 12, oy + 10, PLUM_L)
    c.dot(ox + 12, oy + 9, GREEN)                         # a thistle
    c.fill(ox + 6, oy + 30, ox + 9, oy + 30, WOOD_D)      # its foot, sunk in the ground


def desk(c, ox, oy):
    c.box(ox + 1, oy + 15, ox + 14, oy + 18, WOOD_L)      # the top
    c.box(ox + 1, oy + 19, ox + 14, oy + 27, WOOD)
    c.box(ox + 3, oy + 20, ox + 12, oy + 23, WOOD_L)      # the drawer, pulled half out
    c.dot(ox + 7, oy + 21, BRASS_L)
    c.dot(ox + 8, oy + 21, BRASS_L)
    c.vline(ox + 2, oy + 28, oy + 31, WOOD_D)
    c.vline(ox + 13, oy + 28, oy + 31, WOOD_D)
    c.box(ox + 9, oy + 11, ox + 13, oy + 14, CREAM)       # papers on top
    c.dot(ox + 4, oy + 13, INK)
    c.dot(ox + 4, oy + 14, INK)                           # an inkwell


def sketch():
    """The Vale House from the plaza in pencil, on paper folded in four, in a plain frame."""
    c = Canvas(32, 32)
    c.box(2, 2, 29, 29, WOOD)
    c.box(4, 4, 27, 27, CREAM)
    c.vline(15, 5, 26, CREAM_D)
    c.hline(5, 26, 15, CREAM_D)                           # the folds
    pencil = (120, 112, 106)
    for x in range(8, 24):
        c.dot(x, 12, pencil)                              # the eaves
    for i in range(5):
        c.dot(8 + i, 12 - i, pencil)
        c.dot(19 - i, 12 - i, pencil)
        c.dot(19 + i, 7 + i, pencil)                      # two gables
        c.dot(12 + i, 7 + i, pencil)
    for y in range(13, 24):
        c.dot(8, y, pencil)
        c.dot(23, y, pencil)
    c.hline(8, 23, 23, pencil)
    for x, y in ((10, 15), (19, 15), (10, 19), (19, 19)):
        c.fill(x, y, x + 2, y + 1, pencil)                # windows
    c.fill(15, 19, 16, 22, pencil)                        # the door
    c.dot(24, 25, INK)
    c.dot(25, 24, INK)
    c.dot(25, 26, INK)                                    # an initial
    return selout(c.im)


def main():
    c = Canvas(160, 32)
    folder(c, 0, 0)
    open_book(c, 16, 0)
    book_stack(c, 32, 0)
    dust(c, 64, 0)
    dummy(c, 80, 0)
    easel(c, 96, 0)
    shelf(c, 112, 0)
    sign(c, 128, 0)
    desk(c, 144, 0)
    trunk(c, 0, 16, True)
    trunk(c, 16, 16, False)
    os.makedirs(OUT, exist_ok=True)
    selout(c.im).save(os.path.join(OUT, "event_props.png"))
    sketch().save(os.path.join(OUT, "attic_sketch.png"))
    print("wrote", os.path.join(OUT, "event_props.png"), "and attic_sketch.png")


if __name__ == "__main__":
    main()
