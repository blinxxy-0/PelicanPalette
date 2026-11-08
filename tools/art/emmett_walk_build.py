"""Emmett's walk sheet: Alex's own sheet (lean, short brown hair, a jacket) recoloured into
Emmett, with a beard and an oatmeal waistcoat drawn on. Higgsfield's edit of
it (emmett_edit_a.png) read back noisy, a patchy coat and smeared faces, so as with Fern
it's only the guide; the game's own pixels are better.

  hair       drawn after his portrait: shaggy, a messy fringe, locks to his jaw; a short beard
  eyes       Alex's green to bright blue (his eyes are already lid, pupil, white, iris)
  coat       Alex's green jacket to travel-worn brown, open over an oatmeal waistcoat
  neckerchief  the jacket's yellow collar, faded red (its stripes lower down: coat)
  trousers   dark; boots scuffed brown
  facing left is facing right mirrored, as the game's own sheets are

Usage: emmett_walk_build.py <Alex's sheet: art/vale_residents_sprites/ref_walk_alex.png> <out.png>"""
import sys
import numpy as np
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
a = np.asarray(Image.open(src).convert("RGBA").crop((0, 0, 64, 128))).astype(int)
H, W = 128, 64
o_ = np.zeros((H, W, 4), np.uint8)
MAP = {
    # hair
    (65, 24, 8): (92, 54, 24), (107, 42, 25): (148, 96, 48), (146, 61, 31): (190, 138, 74),
    (180, 89, 41): (222, 176, 104),
    # eyes
    (2, 44, 1): (22, 34, 84), (22, 97, 32): (64, 126, 214), (157, 164, 130): (172, 140, 118),
    # jacket to coat; its yellow collar to the neckerchief
    (16, 36, 16): (42, 22, 16), (43, 74, 25): (86, 50, 32), (79, 139, 32): (124, 78, 46),
    (255, 218, 36): (212, 66, 52), (189, 163, 33): (156, 42, 38),
    # jeans to dark trousers
    (10, 12, 52): (34, 22, 20), (39, 46, 135): (70, 52, 46), (78, 90, 179): (96, 74, 62),
    (11, 10, 38): (26, 16, 16),
    # shoes to scuffed boots
    (42, 41, 50): (50, 30, 22), (227, 216, 173): (172, 122, 80), (178, 169, 159): (136, 92, 58),
    (119, 112, 129): (100, 66, 42),
}
TRIM = {(255, 218, 36): (124, 78, 46), (189, 163, 33): (86, 50, 32)}   # the jacket's stripes, coat
for y in range(H):
    fy = y % 32
    for x in range(W):
        if a[y, x, 3]:
            c = tuple(a[y, x, :3])
            bob = (0, 1, 0, 1)[x // 16]
            # the yellow trim is his neckerchief only at the collar; down the jacket it's coat
            row, fx = y // 32, x % 16
            at_neck = fy <= 15 + bob and (row in (1, 3) or 5 <= fx <= 10)
            n = TRIM[c] if c in TRIM and not at_neck else MAP.get(c, c)
            o_[y, x, :3] = n
            o_[y, x, 3] = 255

BOB = (0, 1, 0, 1)
COAT = {(86, 50, 32), (124, 78, 46)}
OAT, OAT_D = (210, 190, 152), (172, 150, 114)
BEARD, BEARD_D = (132, 62, 32), (111, 50, 29)
SKIN = {(255, 202, 148), (226, 130, 90)}
def put(x, y, c):
    o_[y, x, :3] = c; o_[y, x, 3] = 255
for f, o in enumerate(BOB):
    X = f * 16
    # front: the waistcoat down the middle of his open coat, the neckerchief's point
    for y in range(16 + o, 22 + o):
        for x in range(6, 10):
            if tuple(o_[y, X + x, :3]) in COAT:
                put(X + x, y, OAT_D if x in (6, 9) else OAT)
    put(X + 7, 16 + o, (212, 66, 52)); put(X + 8, 16 + o, (156, 42, 38))
    # front: beard along the jaw and chin
    put(X + 5, 12 + o, BEARD_D); put(X + 10, 12 + o, BEARD_D)
    for x in range(6, 10):
        put(X + x, 13 + o, BEARD)
    put(X + 6, 13 + o, BEARD_D); put(X + 9, 13 + o, BEARD_D)
    # facing right: beard on the jaw, the waistcoat's edge at the front of the coat
    Y = 32 + o
    put(X + 6, Y + 12, BEARD_D); put(X + 7, Y + 12, BEARD)
    for x in range(6, 11):
        if o_[Y + 13, X + x, 3]:
            put(X + x, Y + 13, BEARD if x > 6 else BEARD_D)
    for y in range(17, 22):
        if tuple(o_[Y + y, X + 9, :3]) in COAT:
            put(X + 9, Y + y, OAT)
# his hair, drawn here after his portrait (the user, 2026-11-08: Alex's "is boring and
# doesnt reflect the portrait"): shaggy and overgrown, tufts on top, a messy fringe over
# his forehead, locks over his ears to his jaw, lighter streaks through the sandy brown.
# The face's own pixels (brows, eyes, beard) stay where they were.
# (the hair: his portrait's own colours)
INK = {"#": (56, 18, 25), "1": (111, 50, 29), "2": (165, 86, 41), "3": (210, 128, 62),
       "4": (232, 160, 88), "s": (255, 202, 148), "g": (226, 130, 90), "f": (117, 48, 38),
       "h": (172, 140, 118), "P": (22, 34, 84), "w": (244, 252, 251), "L": (64, 126, 214),
       "B": (165, 86, 41)}                                 # the beard: his hair's brown
INK["M"] = (200, 116, 56)   # his moustache, a lighter brown than the beard so it reads apart
                            # from it (the user, 2026-11-08: "he has a mustasche that you're
                            # not drawing", where they put red dots)
# a pixel higher than Alex's head, with a row more of face below the eyes, so his beard can
# show with its outline along the jaw, round the chin, a moustache (the user, 2026-11-08:
# "his face isn't long enough for you to be able to see the beard outline he should have")
FRONT = [".....#...##.....",
         "....#3#.#43#....",
         "...#23443443#...",
         "..#2334444433#..",
         ".#233434443432#.",
         ".#2334333433322#",
         "#22332s323s2332#",
         ".#23s##ss##s32#.",
         ".#23fhPssPhg32#.",
         ".#2fgwLssLwsf2#.",
         "..#1MssMMssM1#..",
         "...#BMMssMMB#...",
         "....#BB11BB#....",
         "......#BB#......"]                 # the chin, over his collar
RIGHT = [".....#.##.......",
         "...##3#343#.....",
         "..#23444433#....",
         ".#2334444433#...",
         ".#23343344332#..",
         "#233243342333#..",
         "#2233232ggs32#..",
         ".#2232ss##g12#..",
         ".#2321gshPsf....",
         ".#232ssswLssf...",
         ".#2321BBsMMf....",
         ".#221#BBBM1f....",
         "..#1#g#BBBB#...."]
BACK = ["....#..##.#.....",
        "...#3##34#3#....",
        "..#234443443#...",
        ".#23344444433#..",
        ".#23434434343#..",
        "#2334334433432#.",
        "#2333233323332#.",
        "#2232323232322#.",
        ".#22121212122#..",
        ".#2212121212#...",
        "..#12121211#....",
        "...#1#11#1#.....",
        "....#1#1#1#....."]
TOP = 1
for row, rows, clear_to in ((0, FRONT, 13), (1, RIGHT, 13), (2, BACK, 13)):
    assert all(len(r) == 16 for r in rows)
    for f, o in enumerate(BOB):
        X, Y = f * 16, row * 32
        o_[Y:Y + clear_to + 1 + o, X:X + 16] = 0     # rows past clear_to are drawn over
        for j, line in enumerate(rows):
            for x, ch in enumerate(line):
                if ch != ".":
                    put(X + x, Y + TOP + j + o, INK[ch])
# his bandana, faded red, plain to see (the user: "hes also missing the bandana"): a band
# round his neck under the beard; in front its point over the waistcoat, behind its knot
RED, RED_D = (204, 62, 50), (150, 40, 36)
for f, o in enumerate(BOB):
    X = f * 16
    for x in range(5, 11):
        put(X + x, 15 + o, RED_D if x in (5, 10) else RED)       # front: the band
    for x in range(6, 10):
        put(X + x, 16 + o, RED)                                  # its point
    put(X + 7, 17 + o, RED_D); put(X + 8, 17 + o, RED_D)
    for x in range(5, 11):
        put(X + x, 64 + 14 + o, RED_D if x in (5, 10) else RED)  # behind: the band
    put(X + 7, 64 + 15 + o, RED); put(X + 8, 64 + 15 + o, RED_D) # its knot
    put(X + 7, 64 + 16 + o, RED_D); put(X + 9, 64 + 16 + o, RED_D)
    # facing right: his neck under the beard, then the band round his throat a row lower,
    # its point draped over his chest (the user: it was "attached to his beard hair
    # instead of under it and draped onto the shirt")
    Y = 32 + o
    put(X + 6, Y + 14, (86, 50, 32)); put(X + 7, Y + 14, (226, 130, 90))
    put(X + 8, Y + 14, (226, 130, 90)); put(X + 9, Y + 14, (117, 48, 38))
    o_[Y + 14, X + 10] = 0
    put(X + 5, Y + 15, RED_D)
    for x in range(6, 10):
        put(X + x, Y + 15, RED)
    put(X + 10, Y + 15, RED_D)
    put(X + 9, Y + 16, RED); put(X + 10, Y + 16, RED_D)
    put(X + 9, Y + 17, RED_D)
# facing left: facing right mirrored
for f in range(4):
    X = f * 16
    o_[96:128, X:X + 16] = o_[32:64, X:X + 16][:, ::-1]
Image.fromarray(o_).save(out)
