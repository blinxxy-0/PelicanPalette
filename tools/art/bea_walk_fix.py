"""Bea's walk sheet (Higgsfield's edit of Leah's sheet, read back on its 12px grid and
cleaned): the eyes and the back of her outfit fixed by hand (the user, 2026-11-08: "the back
of some of the outfits and the eyes need a little fixing").

  front  two eyes, 1x2 each, where the read-back left stray white pixels; rosy cheeks
  sides  one eye, 1x2, a pixel in from the face's front edge
  back   the apron's front was showing through: the torso is the dress again, with the
         apron's straps, waistband and bow seen from behind

Usage: bea_walk_fix.py <clean 64x128.png> <out.png>"""
import sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA")
px = im.load()
SKIN, BLUSH = (249, 185, 131, 255), (236, 140, 112, 255)
BROW, LID, EYE = (56, 14, 18, 255), (176, 128, 112, 255), (42, 6, 10, 255)
WHITE, IRIS = (250, 244, 236, 255), (112, 58, 34, 255)
DRESS, DRESS_D = (128, 56, 80, 255), (79, 28, 35, 255)
CREAM, CREAM_D = (245, 219, 179, 255), (216, 174, 111, 255)
BOB = (0, 1, 0, 1)                                    # walk frames 1 and 3 sit a pixel lower

for f, o in enumerate(BOB):
    X, Y = f * 16, o
    # front (row 0)
    # eyes as Leah's: 2x2 each, the lid and pupil over the white and iris, a brow above
    # (the user, 2026-11-08: "missing a brow and whites")
    for x in (6, 7, 8, 9):                             # a row of forehead, so the brows show
        px[X + x, Y + 9] = SKIN
    for x in (7, 8):
        px[X + x, Y + 10] = SKIN
        px[X + x, Y + 11] = SKIN
        px[X + x, Y + 12] = SKIN
    for outer, inner in ((5, 6), (10, 9)):
        px[X + outer, Y + 10] = BROW
        px[X + inner, Y + 10] = BROW
        px[X + outer, Y + 11] = LID
        px[X + inner, Y + 11] = EYE
        px[X + outer, Y + 12] = WHITE
        px[X + inner, Y + 12] = IRIS
    px[X + 5, Y + 13] = BLUSH
    px[X + 10, Y + 13] = BLUSH
    # facing right (row 1): one eye, the white behind the iris, its brow above
    for rows, back, front, skin in ((32, 9, 10, 11), (96, 6, 5, 4)):   # and facing left (row 3), its mirror
        y = rows + Y
        px[X + back, y + 9] = BROW
        px[X + front, y + 9] = BROW
        px[X + back, y + 10] = LID
        px[X + front, y + 10] = EYE
        px[X + back, y + 11] = WHITE
        px[X + front, y + 11] = IRIS
        px[X + skin, y + 10] = SKIN
        px[X + skin, y + 11] = SKIN
        px[X + front, y + 8] = SKIN                     # forehead over the brow
    # back (row 2)
    B = 64 + Y
    for y in range(14, 24):
        for x in range(5, 11):
            if y == 14 and px[X + x, B + y][0] < 120:  # the hair's last row stays
                continue
            px[X + x, B + y] = DRESS_D if (x == 10 or y == 23) else DRESS
    for y, d in zip(range(15, 21), (0, 1, 2, 2, 1, 0)):   # the straps, crossed on her back
        px[X + 5 + d, B + y] = CREAM
        px[X + 10 - d, B + y] = CREAM
    for x in range(5, 11):                             # the waistband
        px[X + x, B + 21] = CREAM
    px[X + 7, B + 21] = CREAM_D                        # the bow: knot, then its tails
    px[X + 8, B + 21] = CREAM_D
    px[X + 7, B + 22] = CREAM
    px[X + 8, B + 22] = CREAM
    px[X + 6, B + 23] = CREAM_D
    px[X + 9, B + 23] = CREAM_D
im.save(out)
