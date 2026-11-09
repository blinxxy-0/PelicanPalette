"""Tess's walk sheet: Emily's own sheet (small, a chin-length bob, a long dress) recoloured
into Tess, the way Fern's (Leah's) and Emmett's (Alex's) were made: the game's own pixels,
no generation (docs/tess_brief.md, Look).

  hair      Emily's blues to near-black brown
  face      Emily's face and hands to pale, the shading rosier (wind-reddened cheeks)
  eyes      to grey
  arms      Emily's bare arms have their own pinks: they become the moss-green jumper's sleeves
  dress     to Bram's too-big leather apron, brown, nearly to her boots
  hem       the dress's last rows below the apron: canvas trousers
  shoes     to brown boots
  clip      Emily's hair clip, painted out to hair
  sawdust   a pale fleck in her hair, front and back

The whole sheet, not only the walking rows, since she marries (the marriage batch,
docs/tess_marriage.md): frame 32 is her kiss (Emily's, facing right) and 50 her sleeping
frame. Emily's own dance and party frames come along recoloured; Tess never uses them.
With --wedding, the apron comes off for the wedding (Characters/TessBrand_Wedding): its
leather on her body becomes the jumper, below the waist her canvas trousers.

Usage: tess_walk_build.py <Emily's sheet (Characters/Emily)> <out.png> [--wedding]"""
import sys
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
WEDDING = "--wedding" in sys.argv
im = Image.open(src).convert("RGBA")
orig = im.copy()
px = im.load()
EMILY_HAIR = {(2, 48, 139), (20, 84, 200), (27, 142, 246), (113, 220, 251), (39, 14, 104), (23, 8, 63)}

MAP = {
    # hair, darkest to lightest, and its outline
    (2, 48, 139): (44, 30, 30), (20, 84, 200): (66, 46, 40), (27, 142, 246): (96, 68, 54),
    (113, 220, 251): (132, 102, 80), (39, 14, 104): (28, 18, 20), (23, 8, 63): (34, 20, 22),
    # face and hands: pale, rosy in the shade
    (252, 178, 131): (250, 214, 186), (242, 102, 81): (226, 146, 124), (122, 0, 74): (112, 52, 50),
    # eyes
    (214, 121, 0): (110, 120, 128), (183, 169, 195): (176, 176, 180),
    (241, 220, 255): (238, 234, 228), (244, 228, 255): (244, 240, 234),
    # arms -> the jumper's sleeves (moss green)
    (253, 191, 185): (126, 152, 92), (249, 128, 114): (90, 118, 66), (198, 54, 43): (54, 76, 44),
    # the dress -> the leather apron
    (226, 46, 30): (156, 100, 60), (200, 7, 24): (126, 78, 46), (151, 0, 25): (100, 60, 36),
    (93, 0, 40): (54, 32, 22),
    # shoes -> boots
    (0, 0, 65): (54, 34, 24), (0, 11, 126): (98, 64, 40),
}
APRON = {(156, 100, 60), (126, 78, 46), (100, 60, 36)}
CANVAS = [(176, 160, 122), (146, 130, 98)]
for y in range(im.height):
    for x in range(im.width):
        c = px[x, y]
        if c[3]:
            n = MAP.get(c[:3], c[:3])
            # Emily's arm pinks are skin, not sleeve, wherever they're up at head height in her other
            # frames (a raised hand in the kiss, her sleeping face): skin there. (Not in the four
            # walking rows, which stay exactly as approved.)
            if y >= 128 and y % 32 <= 15 and c[:3] in ((253, 191, 185), (249, 128, 114), (198, 54, 43)):
                n = {(253, 191, 185): (250, 214, 186), (249, 128, 114): (226, 146, 124), (198, 54, 43): (112, 52, 50)}[c[:3]]
            # her other frames' clip is orange, the colour her eyes are elsewhere: high up, it's hair
            if y >= 128 and y % 32 <= 9 and c[:3] == (214, 121, 0):
                n = (66, 46, 40)
            # ...and lower, by her jaw, it's an earring (Tess wears none): hair
            if y >= 128 and 13 <= y % 32 <= 16 and c[:3] == (214, 121, 0):
                n = (66, 46, 40)
            # the clip's pale highlight, at the top right of her hair (not the sleep marks above it)
            if y >= 128 and c[:3] == (236, 233, 223) and any(
                    orig.getpixel((x + dx, y + dy))[:3] in EMILY_HAIR
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= x + dx < 64 and 0 <= y + dy < im.height):
                n = (96, 68, 54)
            # Emily's hair clip (the same reds as her hands and arms, high on her head): hair
            if (y % 32 <= 9 and c[:3] == (122, 0, 74)) or (y % 32 <= 11 and c[:3] == (198, 54, 43)):
                n = (44, 30, 30) if c[:3] == (122, 0, 74) else (66, 46, 40)
            # the dress's last two rows above its hem line (rows 27-28 of a frame): trousers
            if n in APRON and y % 32 in (27, 28):
                n = CANVAS[0] if n == (156, 100, 60) else CANVAS[1]
            # the wedding: no apron; the jumper down to her waist (row 21 of a frame), trousers below
            if WEDDING and n in APRON:
                light, mid, dark = n == (156, 100, 60), n == (126, 78, 46), n == (100, 60, 36)
                if y % 32 <= 21:
                    n = (126, 152, 92) if light else (90, 118, 66) if mid else (54, 76, 44)
                else:
                    n = (176, 160, 122) if light else (146, 130, 98) if mid else (116, 102, 76)
            px[x, y] = n + (255,)

# sawdust: a pale fleck or two in her hair (front and back frames)
DUST = (196, 170, 128, 255)
HAIR = {(66, 46, 40), (96, 68, 54), (44, 30, 30)}
for row in (0, 2):
    for f in range(4):
        X, Y = f * 16, row * 32
        for (dx, dy) in ((10, 7),):
            if px[X + dx, Y + dy][:3] in HAIR:
                px[X + dx, Y + dy] = DUST

if WEDDING:
    # The wedding shows frames 36-38 (standing; turned to you; turned to you, hands together):
    # Emily's are her white dress. Tess's are her own, apron off: standing (0) and turned left to
    # you (12) for both of the others.
    def frame(f):
        return im.crop((f % 4 * 16, f // 4 * 32, f % 4 * 16 + 16, f // 4 * 32 + 32))
    for to, piece in ((36, frame(0)), (37, frame(12)), (38, frame(12))):
        im.paste(piece, (to % 4 * 16, to // 4 * 32))
im.save(out)
print("wrote", out)
