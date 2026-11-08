"""Tobin's walk sheet (Higgsfield's edit of Clint's sheet, read back with
tools/art/walk_readback.py): his eyes, which the read-back left as dark smudges, laid out
as Bea's and Leah's (the user, 2026-11-08, on Bea's: "missing a brow and whites"): a brow,
then lid and pupil, then the white and the iris. His back row already shows the apron's
crossed straps and waist tie.

Usage: tobin_walk_fix.py <read-back 64x128.png> <out.png>"""
import sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA")
px = im.load()
SKIN, LID = (147, 69, 40, 255), (97, 45, 25, 255)
BROW, EYE = (30, 17, 17, 255), (22, 13, 14, 255)
WHITE, IRIS = (240, 226, 208, 255), (70, 36, 22, 255)
BOB = (0, 1, 0, 1)                                    # walk frames 1 and 3 sit a pixel lower

for f, o in enumerate(BOB):
    X = f * 16
    # front (row 0): two eyes, 2x2, a brow over each, the nose between
    for outer, inner in ((5, 6), (10, 9)):
        px[X + outer, o + 7] = BROW
        px[X + inner, o + 7] = BROW
        px[X + outer, o + 8] = LID
        px[X + inner, o + 8] = EYE
        px[X + outer, o + 9] = WHITE
        px[X + inner, o + 9] = IRIS
    for x in (7, 8):
        px[X + x, o + 8] = SKIN
        px[X + x, o + 9] = SKIN
    # sides: facing right (row 1) and facing left (row 3), one eye, white behind the iris
    for row, back, front, skin in ((32, 9, 10, 11), (96, 6, 5, 4)):
        y = row + o
        px[X + back, y + 7] = BROW
        px[X + front, y + 7] = BROW
        px[X + back, y + 8] = LID
        px[X + front, y + 8] = EYE
        px[X + back, y + 9] = WHITE
        px[X + front, y + 9] = IRIS
        px[X + skin, y + 8] = SKIN
        px[X + skin, y + 9] = SKIN
im.save(out)
