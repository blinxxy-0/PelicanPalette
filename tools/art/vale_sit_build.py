"""Sitting frames for the Vale residents who sit on a bench in their schedules (the overlook
bench, Town's side bench facing west: Lupini on Sundays, Emmett on Saturdays).

The game's own seated pose: Alex's frame 39 (Characters/Alex), side on, facing left, on a
seat with his legs out and his hands in his lap (art/vale_residents_sprites/ref_sit_alex39.png).
Its head and shoulders are his standing-left frame (12) exactly, a pixel right and two rows
up, down to the waist (standing rows 2 to 22). So each resident keeps the head and chest of
their own standing-left frame, moved the same way, and sits on Alex's seated legs, in their
own colours:

  Emmett   Alex's sheet is his (tools/art/emmett_walk_build.py): the legs take the same
           recolour (the jacket his worn brown coat, dark trousers, scuffed boots).
  Lupini   the legs in his trousers and shoes, his frock coat's tails over the seat behind.

Appends a fifth row to each sheet (frames 16 to 19): 16 sitting facing left (west), 17
sitting facing right (east, 16 mirrored); 18 and 19 empty. The whole figure sits a row
lower than Alex's frame, so Emmett's hair (a row taller than Alex's) stays in the frame.

Usage: vale_sit_build.py <ref_sit_alex39.png> <sprites.png> lupini|emmett [out.png]
       (out defaults to the sheet itself; the sheet's walk rows are kept as they are)
"""
import sys
import numpy as np
from PIL import Image

ref, sheet, who = sys.argv[1], sys.argv[2], sys.argv[3]
out = sys.argv[4] if len(sys.argv) > 4 else sheet
alex = np.asarray(Image.open(ref).convert("RGBA")).astype(int)        # 16 x 32
assert alex.shape[:2] == (32, 16), alex.shape
s = np.asarray(Image.open(sheet).convert("RGBA")).astype(np.uint8)
walk = s[:128].copy()
stand = walk[96:128, 0:16].astype(int)                                 # standing, facing left

DROP = 1          # the whole seated figure a row lower than Alex's frame
CUT = 22          # standing rows up to here are kept (they're Alex's seated rows, moved)
SX, SY = 1, -2    # standing (x, y) -> seated (x + SX, y + SY), before the drop

EMMETT = {
    (16, 36, 16): (42, 22, 16), (43, 74, 25): (86, 50, 32), (79, 139, 32): (124, 78, 46),
    (255, 218, 36): (124, 78, 46), (189, 163, 33): (86, 50, 32),         # stripes: coat
    (10, 12, 52): (34, 22, 20), (39, 46, 135): (70, 52, 46), (78, 90, 179): (96, 74, 62),
    (11, 10, 38): (26, 16, 16),
    (42, 41, 50): (50, 30, 22), (227, 216, 173): (172, 122, 80), (178, 169, 159): (136, 92, 58),
    (119, 112, 129): (100, 66, 42),
}
OUT_D, OUT, COAT_D, COAT = (38, 18, 32), (53, 25, 45), (73, 23, 59), (107, 52, 69)
TROUSER = (69, 37, 43)
LUPINI = {
    (16, 36, 16): OUT_D, (43, 74, 25): COAT_D, (79, 139, 32): COAT,
    (255, 218, 36): COAT, (189, 163, 33): COAT_D,
    (10, 12, 52): OUT_D, (39, 46, 135): OUT, (78, 90, 179): TROUSER, (11, 10, 38): OUT_D,
    (42, 41, 50): OUT_D, (227, 216, 173): OUT, (178, 169, 159): OUT, (119, 112, 129): OUT,
}
SKIN_LUPINI = {(255, 202, 148): (237, 196, 174), (226, 130, 90): (220, 151, 139),
               (117, 48, 38): OUT_D}
recolour = EMMETT if who == "emmett" else {**LUPINI, **SKIN_LUPINI}

sit = np.zeros((32, 16, 4), np.uint8)
# Alex's seated legs and lap, below the kept rows, recoloured
for y in range(32):
    for x in range(16):
        if alex[y, x, 3]:
            Y = y + DROP
            if Y < 32 and Y > CUT + SY + DROP:
                c = tuple(alex[y, x, :3])
                sit[Y, x, :3] = recolour.get(c, c)
                sit[Y, x, 3] = 255
# their own head and chest
for y in range(CUT + 1):
    for x in range(16):
        if stand[y, x, 3]:
            Y, X = y + SY + DROP, x + SX
            if 0 <= Y < 32 and 0 <= X < 16:
                sit[Y, X] = stand[y, x]
            elif X == 16 and 0 <= Y < 32:            # off the right edge: close the outline
                sit[Y, 15] = stand[y, x]


def put(x, y, c):
    sit[y, x, :3] = c
    sit[y, x, 3] = 255


if who == "lupini":
    # his frock coat's tails, falling over the seat behind him
    for y, xs in ((22, (12, 13)), (23, (12, 13, 14)), (24, (12, 13, 14)), (25, (13, 14))):
        for x in xs:
            put(x, y, COAT_D if x < 14 else OUT_D)
    put(14, 22, OUT_D)

sit_r = sit[:, ::-1]
row = np.zeros((32, 64, 4), np.uint8)
row[:, 0:16] = sit
row[:, 16:32] = sit_r
Image.fromarray(np.concatenate([walk, row])).save(out)
