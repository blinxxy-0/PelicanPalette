"""Draws the Gilded Vale's layout sketch (docs/design/gilded_vale_map.md).

A planning schematic in tile coordinates, not map art: terraces, the river and
lake, buildings, paths and labels. 96 x 76 tiles.

Usage: python tools/art/vale_layout.py [out.png] [--scale n]
"""
import sys
from PIL import Image, ImageDraw, ImageFont

W, H = 96, 76
S = 10  # pixels per tile
for i, a in enumerate(sys.argv):
    if a == "--scale":
        S = int(sys.argv[i + 1])
OUT = next((a for a in sys.argv[1:] if a.endswith(".png")), "art/gilded_vale/layout_v1.png")

UPPER, MIDDLE, LOWER, BLUFF = (176, 196, 128), (196, 212, 146), (214, 224, 164), (170, 188, 122)
CLIFF, WOODS, WATER, PATH = (128, 112, 92), (92, 130, 84), (96, 160, 200), (222, 202, 160)
BUILD, ROOF, TEXT, GARDEN = (240, 232, 214), (150, 84, 70), (40, 34, 30), (150, 186, 110)

img = Image.new("RGB", (W * S, H * S), MIDDLE)
d = ImageDraw.Draw(img)
try:
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", max(10, S + 1))
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", max(8, S - 1))
except OSError:
    font = small = ImageFont.load_default()


def P(pts):
    return [(x * S, y * S) for x, y in pts]


def rect(x, y, w, h, fill, outline=None):
    d.rectangle([x * S, y * S, (x + w) * S - 1, (y + h) * S - 1], fill=fill, outline=outline)


def label(x, y, text, f=None, anchor="mm"):
    d.text((x * S, y * S), text, fill=TEXT, font=f or font, anchor=anchor)


# Terraces: upper (north), middle, lower (south); cliff lines between them.
cliff_a = [(0, 30), (14, 31), (30, 30), (44, 31), (58, 30), (72, 32), (96, 31)]
cliff_b = [(0, 51), (16, 50), (30, 49), (46, 50), (62, 51), (80, 52), (96, 51)]
d.polygon(P([(0, 0), (W, 0)] + cliff_a[::-1]), fill=UPPER)
d.polygon(P(cliff_b + [(W, H), (0, H)]), fill=LOWER)
for line in (cliff_a, cliff_b):
    d.line(P(line), fill=CLIFF, width=int(S * 1.2))

# Woods round the edges (the Vale is a bowl); the bus road comes in from the west.
rect(0, 0, 96, 2, WOODS)
rect(0, 0, 5, 76, WOODS)
rect(91, 0, 5, 76, WOODS)
rect(0, 73, 96, 3, WOODS)

# The southern overlook: a bluff on the far side of the lake, as high as the upper terrace.
d.polygon(P([(80, 58), (91, 56), (91, 73), (78, 73), (76, 66)]), fill=BLUFF, outline=CLIFF, width=S)

# River: waterfall in the north-east, down two cascades, into the lake.
river = [(82, 2), (80, 8), (72, 13), (64, 18), (60, 25), (58, 31), (60, 38), (61, 44), (62, 51), (64, 57)]
d.line(P(river), fill=WATER, width=int(S * 2.4), joint="curve")
d.ellipse(P([(75, 3), (87, 11)]), fill=WATER)  # the pool under the waterfall
d.polygon(P([(56, 57), (66, 55), (78, 57), (80, 63), (74, 70), (62, 71), (54, 66)]), fill=WATER)  # the lake

# Paths.
paths = [
    [(0, 6), (12, 6), (24, 8), (32, 21), (31, 30), (31, 33), (38, 40), (46, 42), (60, 42), (68, 41), (80, 41)],
    [(24, 8), (50, 10), (64, 17), (74, 19), (80, 25)],
    [(64, 17), (73, 24), (73, 33), (72, 38)],
    [(31, 36), (20, 38), (12, 44)],
    [(46, 42), (46, 49), (40, 52), (37, 55)],
    [(46, 42), (53, 46), (54, 53), (60, 55), (66, 55), (76, 56), (82, 60), (86, 64)],
    [(36, 56), (26, 58), (20, 60)],
    [(8, 6), (5, 14), (6, 20)],
]
for p in paths:
    d.line(P(p), fill=PATH, width=int(S * 1.6), joint="curve")

# Bridges and stairs.
for bx, by in ((64, 17), (60, 42), (64, 56)):
    rect(bx - 2, by - 1, 4, 2, (170, 130, 90), outline=TEXT)
for sx, sy in ((30, 29), (72, 31), (53, 50), (82, 58)):
    rect(sx, sy, 3, 3, (190, 180, 160), outline=TEXT)
    for k in range(1, 3):
        d.line([(sx * S, (sy + k) * S), ((sx + 3) * S, (sy + k) * S)], fill=TEXT, width=1)

# Sculpture garden and other planted areas.
d.polygon(P([(6, 38), (28, 37), (30, 48), (8, 49)]), fill=GARDEN, outline=TEXT)
for gx, gy in ((10, 41), (15, 44), (20, 40), (24, 45), (12, 47), (26, 41)):
    d.ellipse(P([(gx - 0.8, gy - 0.8), (gx + 0.8, gy + 0.8)]), fill=(200, 200, 200), outline=TEXT)

# The amphitheatre, cut into the lower cliff: seats stepping down to a stage.
for r in (8, 6, 4):
    d.pieslice(P([(37 - r, 52 - r), (37 + r, 52 + r)]), 0, 180, fill=(200, 190, 170), outline=TEXT)
rect(34, 57, 6, 2, (170, 130, 90), outline=TEXT)

# The plaza, the old gilded tree and the fountain.
d.ellipse(P([(36, 38), (56, 48)]), fill=PATH, outline=TEXT)
d.ellipse(P([(38, 39), (43, 44)]), fill=(214, 170, 60), outline=TEXT)
d.ellipse(P([(48, 42), (51, 45)]), fill=WATER, outline=TEXT)

# Buildings: (x, y, w, h, name, number).
buildings = [
    (8, 2, 9, 3, "Bus", "1"),
    (26, 11, 12, 9, "The Vale House", "2"),
    (46, 12, 10, 8, "Pottery", "3"),
    (75, 16, 11, 8, "Lupini's atelier", "4"),
    (39, 31, 15, 7, "Exhibition Hall", "5"),
    (10, 32, 8, 5, "Fern", "6"),
    (19, 32, 5, 4, "Seed", "6"),
    (68, 34, 6, 5, "", "7"),
    (75, 34, 6, 5, "", "7"),
    (82, 34, 6, 5, "", "7"),
    (18, 54, 8, 6, "Emmett", "8"),
]
for x, y, w, h, name, num in buildings:
    rect(x, y, w, h, BUILD, outline=TEXT)
    rect(x, y, w, max(1, h // 3), ROOF)
    if name:
        label(x + w / 2, y + h * 0.66, name, small)
label(81, 40.5, "The Old Studios", small)

# Places with no building.
for x, y, text in (
    (24.5, 8.5, "sign"), (57.5, 25, "clay pit"), (81, 6.5, "waterfall"), (18, 46.5, "Sculpture garden"),
    (46.5, 46.5, "Plaza"), (40.5, 41.5, "tree"), (37, 60.5, "Amphitheatre"), (67, 63, "Lake"),
    (85, 66, "Overlook"), (16, 64, "Wildflower meadow"), (37, 22.5, "tea garden"), (7, 21.5, "Painter's clearing"), (70, 53.2, "Riverside walk"),
):
    label(x, y, text, small)

# Terrace names down the right-hand side.
for x, y, text in ((14, 27, "UPPER TERRACE"), (72, 47.5, "MIDDLE TERRACE"), (22, 69, "LOWER TERRACE")):
    label(x, y, text, font)

# Tile grid every 10 tiles.
for gx in range(0, W + 1, 10):
    d.line([(gx * S, 0), (gx * S, H * S)], fill=(255, 255, 255), width=1)
    label(gx + 0.6, 0.9, str(gx), small, "lm")
for gy in range(10, H + 1, 10):
    d.line([(0, gy * S), (W * S, gy * S)], fill=(255, 255, 255), width=1)
    label(0.4, gy + 0.9, str(gy), small, "lm")

img.save(OUT)
print(OUT, img.size)
