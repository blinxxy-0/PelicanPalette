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

Usage: tess_walk_build.py <Emily's sheet (Characters/Emily)> <out.png>   (the four walking rows)"""
import sys
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA").crop((0, 0, 64, 128))
px = im.load()

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
            # Emily's hair clip (the same reds as her hands and arms, high on her head): hair
            if (y % 32 <= 9 and c[:3] == (122, 0, 74)) or (y % 32 <= 11 and c[:3] == (198, 54, 43)):
                n = (44, 30, 30) if c[:3] == (122, 0, 74) else (66, 46, 40)
            # the dress's last two rows above its hem line (rows 27-28 of a frame): trousers
            if n in APRON and y % 32 in (27, 28):
                n = CANVAS[0] if n == (156, 100, 60) else CANVAS[1]
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
im.save(out)
print("wrote", out)
