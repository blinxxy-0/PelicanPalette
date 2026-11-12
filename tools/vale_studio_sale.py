"""The Old Studios' sale (docs/design/vale_newcomers.md, sections 3 and 4; approved by the
user 2026-11-08): two of the boarded studios go up for sale on a fixed calendar, Robin
renovates them, and the newcomers move in.

    Studio II (the Corrie twins)          Studio III (Tess Brand)
    Fall 1, Y2     For Sale notice         Summer 1, Y3   For Sale notice
    Winter 1, Y2   Sold                    Fall 1, Y3     Sold
    Winter 15, Y2  Robin at work           Fall 15, Y3    Robin at work
    Spring 1, Y3   renovated, fresh gold   Winter 1, Y3   renovated, the plaque bare
                                           Winter 7, Y3   Tess's carved III

Everything here is drawn by hand in the Vale's style, on the studios' own art (no
PixelLab): the notice pinned over the old numeral (rain-streaked in fall, a lip of snow
in winter), the Sold strip pasted across it, Robin's scaffolding and timber, and the two
renovated fronts: boards off, roofs mended, walls cleaned, the windows glazed (the
Corries' own stained glass in II's), II's numeral in fresh gold, III's plaque sanded bare
and then carved, ungilded, by Tess.

What it writes:
- spring_GildedValeStudios.png and its summer_, fall_ and winter_ siblings: only the
  tiles each state changes (diffed against the map as it is), and the timber piles.
- the tileset z_blinxxy.MuseArt_GildedValeStudios (firstgid 31300) in GildedVale.tmx,
  patched in place; nothing else in the map changes. Every state is a conditional
  EditMap in the content pack, so the map itself stays as the studios are now.
- data/vale_studios.json in the dev pack: the EditMaps on the calendar (the dynamic
  token ValeDate, content.json), the plaques' words for each state, Robin's days in the
  Vale, and her letter. Its text goes in i18n/default.json (keys vale.studios.sale.*).

Map rules (docs/terrain_painting.md, and the user's): the scaffolding stands on each
studio's own front (inside its solid bottom rows), and the timber in a yard well clear
of the lane: II's across the lane on the grass facing it (x 82-83), III's in the strip
beside its east wall (x 94). Only a pile's bottom row is solid; its top is on Front.

Usage: python tools/vale_studio_sale.py [preview]
    preview: also writes every state, all four seasons, to docs/media/review/studio_sale/ (ignored by git) for checking.
"""
import json, math, os, random, re, sys
from collections import Counter
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import vale_studio_signs as signs

PACK = "[CP] Gilded Vale (dev)"
ASSETS = os.path.join(PACK, "assets")
SEASONS = ("spring", "summer", "fall", "winter")
FIRST = 31300                          # after the signs (31200 + 16)
TILESET = "z_blinxxy.MuseArt_GildedValeStudios"
SHEET = "spring_GildedValeStudios.png"
SHEET_COLS = 16
VALE = "Maps/blinxxy.MuseArt_GildedVale"
KEY = "blinxxy.MuseArt_Vale.studios."  # their right-click words, Strings/StringsFromMaps

TOP = 30                               # the studios' top row (their roofs)

# ---- the calendar: ValeDate = (Year - 1) * 112 + season * 28 + day (content.json) ----

def date(year, season, day):
    return (year - 1) * 112 + SEASONS.index(season) * 28 + day


# Each studio: its left tile, the tiles whose words change (the wall each side of the
# door, where vale_studio_signs.py put them), and its states, each from (and until) a date.
STUDIOS = {
    "2": {
        "left": 82, "words": [(85, 38), (87, 38)],
        "states": [
            ("forsale", date(2, "fall", 1)),
            ("sold", date(2, "winter", 1)),
            ("works", date(2, "winter", 15)),
            ("renovated", date(3, "spring", 1)),
        ],
    },
    "3": {
        "left": 88, "words": [(90, 38), (93, 38)],
        "states": [
            ("forsale", date(3, "summer", 1)),
            ("sold", date(3, "fall", 1)),
            ("works", date(3, "fall", 15)),
            ("new", date(3, "winter", 1)),
            ("carved", date(3, "winter", 7)),
        ],
    },
}

# Measured on each studio's art, in its block's pixels (the 6 x 9 tiles from x left, row
# TOP; II's art sits 4 px in and down, III's fills its block).
GEOM = {
    "2": {
        "notice": (68, 92),                 # the old numeral's middle
        "plaque": (56, 89, 81, 96),         # the plaque's face (vale_studio_signs.py's box)
        "window": (21, 89, 49, 119),        # its frame, outside edge
        "window_clear": (16, 87, 52, 122),  # the boards and their shadow reach this far
        "wall": (10, 86, 87, 136),          # the plastered front
        "gable": [(48, 61), (15, 81), (82, 81)],
        "keep": [(56, 98, 82, 137), (54, 86, 84, 97)],  # the door and the plaque
        "ridge": 47,
        "patch": (54, 18, 86, 63),          # the boarded hole in the roof's right face
        "holes": [(25, 24, 35, 37), (6, 50, 16, 64)],   # holes in its left face
        "scaffold": (15, 51),               # the poles' x, either side of the window
        "ladder": 83,                       # its left rail, right of the door
        "base": (184, 177, 152),            # the plaster
    },
    "3": {
        "notice": (66, 88),
        "plaque": (52, 85, 81, 91),
        "window": (19, 91, 44, 120),
        "window_clear": (16, 89, 48, 127),
        "wall": (7, 82, 90, 136),
        "gable": [(48, 58), (12, 79), (85, 79)],
        "keep": [(52, 96, 82, 137), (49, 80, 85, 94)],
        "ridge": 47,
        "patch": (54, 10, 91, 59),
        "holes": [],
        "scaffold": (14, 48),
        "ladder": 84,
        "base": (200, 190, 170),
    },
}

# The timber Robin brings: (left, bottom) of each pile, its size in tiles.
TIMBER = {"2": ((82, 44), (2, 2)), "3": ((94, 38), (1, 2))}

INK = (52, 40, 34, 255)
OUTLINE = (54, 30, 23, 255)
SNOW, SNOW_D = (246, 250, 255, 255), (200, 214, 232, 255)


# ---- reading the map --------------------------------------------------------------

class Map:
    def __init__(self):
        self.path = os.path.join(ASSETS, "GildedVale.tmx")
        self.text = open(self.path, encoding="utf-8").read()
        self.layers = signs.read_layers(self.text)
        self.W = self.layers["Back"][0]
        self.record = json.load(open(signs.RECORD))
        self.sheets = {}
        for s in SEASONS:
            self.sheets[s] = {
                "build": Image.open(os.path.join(ASSETS, f"{s}_GildedValeBuildings.png")).convert("RGBA"),
                "signs": Image.open(os.path.join(ASSETS, f"{s}_GildedValeSigns.png")).convert("RGBA"),
            }

    def get(self, layer, x, y):
        return self.layers[layer][1][y * self.W + x]

    def tile(self, season, gid):
        if signs.FIRST <= gid < signs.FIRST + 100:
            sheet, i, cols = self.sheets[season]["signs"], gid - signs.FIRST, signs.SHEET_COLS
        elif signs.BUILD_FIRST <= gid < signs.BUILD_FIRST + 10000:
            sheet, i, cols = self.sheets[season]["build"], gid - signs.BUILD_FIRST, signs.BUILD_COLS
        else:
            return None
        return sheet.crop(((i % cols) * 16, (i // cols) * 16, (i % cols) * 16 + 16, (i // cols) * 16 + 16))

    def cells(self, n):
        """The studio's building tiles: (x, y) -> (layer, gid on the map now)."""
        left = STUDIOS[n]["left"]
        out = {}
        for y in range(TOP, 39):
            for x in range(left, left + 6):
                for layer in ("AlwaysFront", "Front", "Buildings"):
                    g = self.get(layer, x, y)
                    if (signs.BUILD_FIRST <= g < signs.BUILD_FIRST + 10000) or (signs.FIRST <= g < signs.FIRST + 100):
                        out[(x, y)] = (layer, g)
                        break
        return out

    def block(self, n, season, numerals=True):
        """The studio as it stands, 96 x 144 px. numerals=False: with the plaque's own
        tiles from before vale_studio_signs.py drew the old gold numeral on."""
        left = STUDIOS[n]["left"]
        im = Image.new("RGBA", (96, 144))
        for (x, y), (layer, g) in self.cells(n).items():
            key = f"{x},{y}"
            if not numerals and key in self.record:
                g = self.record[key][1]
            t = self.tile(season, g)
            if t:
                im.alpha_composite(t, ((x - left) * 16, (y - TOP) * 16))
        return im


# ---- small drawing helpers ----------------------------------------------------------

def lum(c):
    return (c[0] * 30 + c[1] * 59 + c[2] * 11) // 100


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (c[3] if len(c) > 3 else 255,)


def mix(a, b, t):
    return tuple(int(a[k] * (1 - t) + b[k] * t) for k in range(3)) + (255,)


def is_snow(c):
    return c[3] and lum(c) > 205 and c[2] >= c[0] - 4


TINY = dict(signs.TINY)
TINY.update({
    "F": ["111", "100", "110", "100", "100"], "R": ["110", "101", "110", "101", "101"],
    "A": ["010", "101", "111", "101", "101"], "E": ["111", "100", "110", "100", "111"],
})


def text(px, s, x, y, col, drip=None, rnd=None):
    for ch in s:
        for r, row in enumerate(TINY[ch]):
            for c, v in enumerate(row):
                if v == "1":
                    px[x + c, y + r] = col
        if drip and rnd.random() < 0.6:                    # rain: the ink has run
            dx = rnd.randint(0, 2)
            px[x + dx, y + 5] = drip
        x += 4


# ---- the notices ------------------------------------------------------------------

PAPER = {
    "spring": ((236, 230, 210, 255), (196, 186, 160, 255)),
    "summer": ((236, 222, 184, 255), (198, 180, 136, 255)),   # sun-yellowed
    "fall": ((224, 220, 206, 255), (178, 172, 156, 255)),     # damp
    "winter": ((238, 236, 228, 255), (196, 192, 182, 255)),
}
PW, PH = 17, 14


def notice(im, centre, season, sold):
    """A paper notice pinned over the numeral: FOR SALE in ink, and for Sold, a red strip
    pasted across it."""
    cx, cy = centre
    x0, y0 = cx - PW // 2, cy - 7
    rnd = random.Random(f"{cx}{season}{sold}")
    px = im.load()
    paper, edge = PAPER[season]
    for y in range(y0 + 1, y0 + PH + 1):                    # its shadow on the wall
        for x in range(x0 + 1, x0 + PW + 1):
            if px[x, y][3]:
                px[x, y] = shade(px[x, y], 0.72)
    for y in range(y0, y0 + PH):
        for x in range(x0, x0 + PW):
            px[x, y] = edge if x in (x0, x0 + PW - 1) or y in (y0, y0 + PH - 1) else paper
    px[x0 + PW - 1, y0 + PH - 1] = shade(edge, 0.85)        # a curled corner
    px[x0 + PW - 2, y0 + PH - 2] = edge
    ink = INK if season != "summer" else (92, 74, 60, 255)  # sun-faded
    drip = (120, 110, 112, 255) if season == "fall" else None
    text(px, "FOR", x0 + 3, y0 + 2, ink, drip, rnd)
    text(px, "SALE", x0 + 1, y0 + 8, ink, drip, rnd)
    if season == "fall":                                    # rain streaks down the paper
        for x in rnd.sample(range(x0 + 1, x0 + PW - 1), 4):
            for y in range(y0 + rnd.randint(1, 4), y0 + PH - 1):
                if px[x, y] == paper:
                    px[x, y] = shade(paper, 0.9)
    # the pin
    px[cx, y0] = (168, 40, 40, 255)
    px[cx, y0 + 1] = (110, 24, 24, 255)
    if sold:
        RED, RED_D, WHITE = (182, 46, 42, 255), (126, 30, 30, 255), (248, 240, 232, 255)
        if season == "summer":
            RED, RED_D = (196, 78, 64, 255), (140, 52, 44, 255)
        sy = y0 + 4
        for y in range(sy, sy + 7):
            for x in range(x0 - 1, x0 + PW + 1):
                px[x, y] = RED_D if y == sy + 6 else RED
        text(px, "SOLD", cx - 7, sy + 1, WHITE)
        px[x0 - 1, sy] = shade(RED, 0.8)                    # its ends lifting a little
        px[x0 + PW, sy + 6] = shade(RED_D, 0.8)
    if season == "winter":                                  # a lip of snow on the top edge
        for x in range(x0, x0 + PW):
            px[x, y0 - 1] = SNOW
            px[x, y0] = SNOW if (x - x0) % 6 else SNOW_D
            if rnd.random() < 0.35:
                px[x, y0 - 2] = SNOW
        px[cx, y0 + 1] = (110, 24, 24, 255)


# ---- the renovated fronts ---------------------------------------------------------

def roof_face(im, g, side):
    """The pixels of one face of the roof: opaque, above the gable, beside the ridge."""
    px = im.load()
    ridge = g["ridge"]
    out = set()
    for y in range(0, 84):
        for x in range(96):
            if not px[x, y][3] or (side == "left" and x > ridge - 2) or (side == "right" and x < ridge + 3):
                continue
            out.add((x, y))
    # trim the edges: the face's outline and the eave are not slates
    edge = set()
    for (x, y) in out:
        for dx in range(-2, 3):
            for dy in range(-2, 3):
                if (x + dx, y + dy) not in out:
                    edge.add((x, y))
    face = out - edge
    # and the gable (plaster) under the eave
    gable = polygon(g["gable"], grow=4)
    return {p for p in face if p not in gable and lum(px[p]) < 200 or is_snow(px[p])} - gable


def polygon(points, grow=0):
    m = Image.new("L", (96, 144))
    d = ImageDraw.Draw(m)
    (ax, ay), (bx, by), (cx, cy) = points
    d.polygon([(ax, ay - grow), (bx - grow, by + grow // 2), (cx + grow, cy + grow // 2)], fill=255)
    mp = m.load()
    return {(x, y) for y in range(144) for x in range(96) if mp[x, y]}


def courses(im, face, exclude):
    """How the slates lie on a face: their courses run down the slope, (y - slope * x -
    phase) mod height == 0 along each joint. The fit that makes the joints darkest."""
    px = im.load()
    pts = {p: lum(px[p]) for p in face if p not in exclude}
    best = None
    for slope in (0.5, 0.55, 0.6, 0.625, 2 / 3, 0.7, 0.75, -0.5, -0.55, -0.6, -0.625, -2 / 3, -0.7, -0.75):
        for height in range(6, 11):
            for ph10 in range(0, height * 10, 5):
                phase = ph10 / 10
                joint = [l for (x, y), l in pts.items() if (y - slope * x - phase) % height < 1]
                rest = [l for (x, y), l in pts.items() if (y - slope * x - phase) % height >= 1]
                score = sum(joint) / len(joint) - sum(rest) / len(rest)
                if best is None or score < best[0]:
                    best = (score, slope, height, phase)
    return best[1:]


def warm(c):
    return c[0] - c[2] > 14 and c[0] >= c[1]


def slate_palette(im, face, exclude):
    """The face's own colours, without moss, wood or brick: its slates (the commonest of
    its lighter two thirds) and its joints (the commonest of its darker quarter, past the
    very darkest, which is the eave's shadow)."""
    px = im.load()
    cols = sorted((px[p][:3] for p in face if p not in exclude and not warm(px[p])
                   and not (px[p][1] - px[p][0] > 18 and px[p][1] > px[p][2])), key=lum)
    k = len(cols)
    joint = Counter(cols[k // 20: k // 4]).most_common(1)[0][0]
    tones = [c for c, _ in Counter(cols[k * 35 // 100: k * 97 // 100]).most_common(4)]
    if sum(lum(c) for c in tones) / len(tones) > 200:
        # snow: its faint blue lines, where the slates show through
        joint = Counter(c for c in cols if 150 < lum(c) < 236).most_common(1)[0][0]
    return tones, joint


def reslate(im, face, region, seed):
    """New slates over the region, whole slates at a time, in the face's own courses and
    colours: Robin's mended patch."""
    px = im.load()
    slope, height, phase = courses(im, face, region)
    tones, joint = slate_palette(im, face, region)
    snow = sum(lum(c) for c in tones) / len(tones) > 200
    width = 11
    rnd = random.Random(seed)

    def cell(x, y):
        v = y - slope * x - phase
        c = math.floor(v / height)
        u = (x if slope > 0 else -x) + (c % 2) * width // 2 + 200
        return c, u // width, v - c * height, u % width

    cells = {cell(x, y)[:2] for (x, y) in region}
    colour = {}
    for (x, y) in face:
        c, j, t, w = cell(x, y)
        if (c, j) not in cells:
            continue
        if (c, j) not in colour:
            colour[(c, j)] = tones[rnd.randrange(len(tones))]
        base = colour[(c, j)]
        if snow:
            # under snow: one even white, the courses only showing as faint lines
            px[x, y] = (joint if t < 1 or w == 0 else tones[0]) + (255,)
            continue
        if t < 1 or w == 0:
            px[x, y] = joint + (255,)
        elif t < 2:
            px[x, y] = shade(base, 1.1)
        elif t >= height - 1.5:
            px[x, y] = shade(base, 0.88)
        else:
            px[x, y] = base + (255,) if rnd.random() > 0.06 else shade(base, 0.94)


def mend_roof(im, n, season):
    g = GEOM[n]
    px = im.load()
    right, left = roof_face(im, g, "right"), roof_face(im, g, "left")
    x0, y0, x1, y1 = g["patch"]
    box = {(x, y) for (x, y) in right if x0 <= x <= x1 and y0 <= y <= y1}
    # the boards: the biggest patch of warm wood in the box, filled across (the gaps
    # between the planks), and a pixel round it
    wood = {(x, y) for (x, y) in box if warm(px[x, y])}
    board = set()
    while wood:
        seed = wood.pop()
        part, todo = {seed}, [seed]
        while todo:
            x, y = todo.pop()
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q in wood:
                    wood.discard(q)
                    part.add(q)
                    todo.append(q)
        if len(part) > len(board):
            board = part
    if len(board) < 60 or (n == "2" and season == "winter"):
        board = set()                                    # (II in winter: under the snow; that's its chimney)
    rows = {}
    for (x, y) in board:
        rows.setdefault(y, []).append(x)
    board = {(x, y) for y, xs in rows.items() for x in range(min(xs), max(xs) + 1)}
    board = {(x + dx, y + dy) for (x, y) in board for dx in (-1, 0, 1) for dy in (-1, 0, 1)} & right
    if board:
        reslate(im, right, board, n + season + "r")
    if season != "winter":
        for k, (hx0, hy0, hx1, hy1) in enumerate(g["holes"]):
            dark = {(x, y) for (x, y) in left if hx0 <= x <= hx1 and hy0 <= y <= hy1 and lum(px[x, y]) < 62}
            hole = {(x + dx, y + dy) for (x, y) in dark for dx in (-1, 0, 1) for dy in (-1, 0, 1)} & left
            if hole:
                reslate(im, left, hole, n + season + str(k))


def clean_wall(im, n, season):
    """Cracks, moss, flaking patches and the stones showing through, plastered over."""
    g = GEOM[n]
    px = im.load()
    base = g["base"]
    rnd = random.Random(n + season)
    keep = g["keep"]
    x0, y0, x1, y1 = g["wall"]
    area = {(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)}
    area |= polygon(g["gable"])
    for (x, y) in sorted(area):
        c = px[x, y]
        if not c[3] or any(a <= x <= b and c_ <= y <= d for a, c_, b, d in keep):
            continue
        if season == "winter" and is_snow(c) and y > y1 - 8:
            continue                                     # (snow banked at the foot stays)
        dist = abs(c[0] - base[0]) + abs(c[1] - base[1]) + abs(c[2] - base[2])
        if dist > 30:
            r = rnd.random()
            px[x, y] = base + (255,) if r > 0.07 else shade(base, 0.95)


def window(im, box, kind, season, n):
    """A window in a new frame: the Corries' stained glass (II), clear glass (III), or
    for Robin's fortnight, the frame with no glass in it yet."""
    x0, y0, x1, y1 = box
    px = im.load()
    d = ImageDraw.Draw(im)
    WOOD, WOOD_L, WOOD_D = (126, 86, 56, 255), (160, 116, 76, 255), (92, 60, 40, 255)
    if n == "2" and kind != "empty":
        WOOD, WOOD_L, WOOD_D = (66, 92, 84, 255), (98, 128, 116, 255), (44, 62, 58, 255)   # painted, slate green
    # the sill, then the frame
    d.rectangle((x0 - 1, y1 + 1, x1 + 1, y1 + 2), fill=WOOD_L)
    d.line((x0 - 1, y1 + 3, x1 + 1, y1 + 3), fill=shade(GEOM[n]["base"] + (255,), 0.72))
    d.rectangle((x0, y0, x1, y1), fill=WOOD, outline=OUTLINE)
    d.line((x0 + 1, y0 + 1, x1 - 1, y0 + 1), fill=WOOD_L)
    gx0, gy0, gx1, gy1 = x0 + 3, y0 + 3, x1 - 3, y1 - 3        # the glass
    mx = (gx0 + gx1) // 2                                     # the mullion
    ty = gy0 + (gy1 - gy0) * 4 // 9                           # the transom
    panes = [(gx0, gy0, mx - 1, ty - 1), (mx + 2, gy0, gx1, ty - 1),
             (gx0, ty + 2, mx - 1, gy1), (mx + 2, ty + 2, gx1, gy1)]
    d.rectangle((gx0 - 1, gy0 - 1, gx1 + 1, gy1 + 1), fill=WOOD_D)
    for k, (a, b, c, e) in enumerate(panes):
        if kind == "empty":
            d.rectangle((a, b, c, e), fill=(40, 34, 32, 255))
            d.line((a, b, c, b), fill=(28, 22, 20, 255))
        elif kind == "clear":
            clear_pane(px, a, b, c, e)
        else:
            stained_pane(px, a, b, c, e, k)
    d.line((mx, gy0, mx, gy1), fill=WOOD)
    d.line((mx + 1, gy0, mx + 1, gy1), fill=WOOD_D)
    d.line((gx0, ty, gx1, ty), fill=WOOD)
    d.line((gx0, ty + 1, gx1, ty + 1), fill=WOOD_D)
    if season == "winter":
        for x in range(x0 - 1, x1 + 2):
            px[x, y1 + 1] = SNOW
            px[x, y1] = SNOW if x % 4 else SNOW_D
            px[x, y0] = SNOW
            if x % 3:
                px[x, y0 - 1] = SNOW


def clear_pane(px, a, b, c, e):
    DARK, MID, HI = (56, 78, 98, 255), (86, 116, 140, 255), (168, 198, 212, 255)
    for y in range(b, e + 1):
        for x in range(a, c + 1):
            px[x, y] = MID if (x - a) + (y - b) < (c - a + e - b) * 0.6 else DARK
    # a reflection, corner to corner
    w, h = c - a, e - b
    for t in range(max(w, h) + 1):
        x, y = a + 1 + t * (w - 2) // max(w, h), e - 2 - t * (h - 3) // max(w, h)
        if a <= x <= c and b <= y <= e:
            px[x, y] = HI
            if x + 1 <= c:
                px[x + 1, y] = HI


GLASS = [(186, 52, 60), (228, 170, 64), (56, 92, 178), (62, 150, 98), (150, 80, 160), (226, 120, 60)]
LEAD = (48, 44, 50, 255)


def stained_pane(px, a, b, c, e, k):
    """Leaded diamonds of coloured glass, a different arrangement in each pane, the
    middle one gold (Mabel's colours; docs/corrie_twins_brief.md)."""
    rnd = random.Random(k * 7 + 3)
    for y in range(b, e + 1):
        for x in range(a, c + 1):
            u, v = (x - a) + (y - b), (x - a) - (y - b)
            if u % 5 == 0 or v % 5 == 0:
                px[x, y] = LEAD
                continue
            cell = (u // 5, v // 5)
            col = GLASS[(cell[0] * 3 + cell[1] * 5 + k) % len(GLASS)]
            mid = ((a + c) // 2, (b + e) // 2)
            if abs(x - mid[0]) + abs(y - mid[1]) <= 2:
                col = (240, 206, 96)
            hi = u % 5 == 1 and v % 5 in (1, 2)                     # a glint at each top corner
            px[x, y] = shade(col + (255,), 1.25) if hi else col + (255,)
    for x in range(a, c + 1):                                        # leaded border
        px[x, b] = LEAD
        px[x, e] = LEAD
    for y in range(b, e + 1):
        px[a, y] = LEAD
        px[c, y] = LEAD


DOOR_II = (61, 102, 78, 137)          # II's door leaf, inside its frame


def repaint_door(im, season):
    """II's weathered door, painted the green of its new window frame, with a brass knob."""
    px = im.load()
    ramp = [(30, 44, 40), (44, 62, 58), (58, 82, 74), (76, 104, 94), (98, 128, 116)]
    x0, y0, x1, y1 = DOOR_II
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c = px[x, y]
            if not c[3] or (season == "winter" and is_snow(c)):
                continue
            px[x, y] = ramp[min(len(ramp) - 1, lum(c) * len(ramp) // 150)] + (255,)
    kx, ky = x1 - 3, (y0 + y1) // 2 + 2
    px[kx, ky] = (222, 178, 80, 255)
    px[kx + 1, ky] = (160, 118, 50, 255)
    px[kx, ky + 1] = (160, 118, 50, 255)


def plaque(im, n, kind, season):
    """II's plaque repainted with its numeral in fresh gold; III's sanded bare, or carved
    by Tess (plain, no gold: docs/design/vale_newcomers.md, section 4)."""
    x0, y0, x1, y1 = GEOM[n]["plaque"]
    px = im.load()
    if kind == "gold":
        BOARD, BOARD_L = (70, 48, 36, 255), (96, 68, 48, 255)
    else:
        BOARD, BOARD_L = (182, 142, 96, 255), (204, 168, 120, 255)  # fresh-sanded pine
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = BOARD_L if y == y0 else BOARD
            if kind != "gold" and (y - y0) == 3 and (x * 7) % 11 == 0:
                px[x, y] = shade(BOARD, 0.9)                         # the grain
    if kind == "bare":
        pass
    else:
        numeral = "II" if n == "2" else "III"
        w = len(numeral) * 4 - 1
        nx, ny = x0 + (x1 - x0 + 1 - w) // 2, y0 + (y1 - y0 + 1 - 5) // 2
        cells = [(nx + k * 4 + c, ny + r) for k, ch in enumerate(numeral)
                 for r, row in enumerate(signs.FONT[ch]) for c, v in enumerate(row) if v == "1"]
        if kind == "gold":
            for cx, cy in cells:
                px[cx + 1, cy + 1] = (36, 22, 16, 255)
            for cx, cy in cells:
                px[cx, cy] = (232, 194, 96, 255)
            for cx, cy in cells[::4]:
                px[cx, cy] = (252, 230, 156, 255)                   # catching the light
        else:
            for cx, cy in cells:
                px[cx, cy] = (98, 66, 40, 255)                       # the cut
                if (cx + 1, cy + 1) not in cells:
                    px[cx + 1, cy + 1] = (222, 190, 140, 255)       # its far wall in the light
    if season == "winter":
        for x in range(x0 - 1, x1 + 2):
            px[x, y0 - 1] = SNOW
            if x % 5:
                px[x, y0 - 2] = SNOW


def renovate(m, n, season, kind):
    """kind: "renovated" (II), "new" or "carved" (III)."""
    im = m.block(n, season, numerals=False)
    g = GEOM[n]
    mend_roof(im, n, season)
    # the old window and its boards go; the wall's made good round the new one
    px = im.load()
    a, b, c, e = g["window_clear"]
    for y in range(b, e + 1):
        for x in range(a, c + 1):
            if px[x, y][3] and not (season == "winter" and y > 130 and is_snow(px[x, y])):
                px[x, y] = g["base"] + (255,)
    clean_wall(im, n, season)
    window(im, g["window"], "stained" if n == "2" else "clear", season, n)
    if n == "2":
        repaint_door(im, season)
    plaque(im, n, {"renovated": "gold", "new": "bare", "carved": "carved"}[kind], season)
    return im


# ---- Robin's fortnight ------------------------------------------------------------

POLE, POLE_D, POLE_L = (170, 128, 82, 255), (118, 84, 52, 255), (198, 156, 104, 255)
PLANK, PLANK_D = (200, 158, 102, 255), (140, 100, 62, 255)


def scaffold(im, n, season):
    """Two standards either side of the window, a working platform under the eaves and
    one at the window's middle, a brace; a ladder right of the door to the roof."""
    g = GEOM[n]
    px = im.load()
    d = ImageDraw.Draw(im)
    pa, pb = g["scaffold"]
    wall_top = g["wall"][1]
    bottom = 137
    top = wall_top - 6
    for x in (pa, pb):
        d.rectangle((x - 1, top, x + 2, bottom), fill=OUTLINE)
        d.rectangle((x, top, x + 1, bottom), fill=POLE)
        d.line((x + 1, top, x + 1, bottom), fill=POLE_D)
    mid = (g["window"][1] + g["window"][3]) // 2 + 2
    for y in (wall_top + 1, mid):
        d.rectangle((pa - 3, y - 1, pb + 4, y + 3), fill=OUTLINE)
        d.rectangle((pa - 2, y, pb + 3, y + 1), fill=PLANK)
        d.line((pa - 2, y + 2, pb + 3, y + 2), fill=PLANK_D)
        for x in range(pa, pb, 9):                          # the planks' butt joints
            px[x + 5, y] = PLANK_D
        if season == "winter":
            for x in range(pa - 2, pb + 4):
                px[x, y - 1] = SNOW
                if x % 3:
                    px[x, y - 2] = SNOW
    # a brace across the lower bay
    for t in range(0, 101):
        x = pa + 2 + (pb - pa - 3) * t // 100
        y = bottom - 2 - (bottom - mid - 6) * t // 100
        px[x, y] = POLE_D
        px[x, y - 1] = POLE
    # the ladder, leaning on the eave right of the door
    lx = g["ladder"]
    for x in (lx, lx + 4):
        d.line((x, top - 4, x, bottom), fill=OUTLINE)
        d.line((x + 1, top - 4, x + 1, bottom), fill=POLE)
    for y in range(top, bottom, 4):
        d.line((lx + 1, y, lx + 4, y), fill=POLE_D)
    if season == "winter":
        for y in range(top, bottom, 4):
            px[lx + 2, y - 1] = SNOW
            px[lx + 3, y - 1] = SNOW


def works(m, n, season):
    """Sold notice still up, the window boards off, the scaffolding up."""
    im = m.block(n, season, numerals=True)
    g = GEOM[n]
    px = im.load()
    a, b, c, e = g["window_clear"]
    for y in range(b, e + 1):
        for x in range(a, c + 1):
            p = px[x, y]
            # the boards and the old frame go; the ivy round them stays
            if p[3] and (warm(p) or lum(p) < 80 or (season == "winter" and is_snow(p) and y < e - 4))                     and not (p[1] > p[0] + 8 and p[1] > p[2]):
                px[x, y] = g["base"] + (255,)
    window(im, g["window"], "empty", season, n)
    notice(im, g["notice"], season, sold=True)
    scaffold(im, n, season)
    return im


def timber(w, h, season):
    """A stack of sawn planks on two bearers, end on, w x h tiles."""
    W, H = w * 16, h * 16
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    px = im.load()
    sh = Image.new("RGBA", (W, H))
    ImageDraw.Draw(sh).ellipse((1, H - 7, W - 2, H - 1), fill=(0, 0, 0, 60))
    im.alpha_composite(sh)
    rnd = random.Random(w * 10 + h)
    if w >= 2:
        # planks running across: their faces, layer on layer, the ends to the right
        layers = 5
        top = H - 6 - layers * 4
        for k in range(layers):
            y = top + k * 4
            inset = 2 if k % 2 else 0
            d.rectangle((2 + inset, y, W - 4 - inset, y + 4), fill=OUTLINE)
            d.rectangle((3 + inset, y + 1, W - 5 - inset, y + 3), fill=PLANK)
            d.line((3 + inset, y + 3, W - 5 - inset, y + 3), fill=PLANK_D)
            for x in range(3 + inset, W - 5 - inset):
                if rnd.random() < 0.12:
                    px[x, y + 2] = PLANK_D
            px[W - 5 - inset, y + 1] = POLE_L                    # the sawn end
        for bx in (6, W - 10):                                   # the bearers
            d.rectangle((bx, H - 6, bx + 3, H - 3), fill=POLE_D, outline=OUTLINE)
        snow_top = top
        x_range = range(2, W - 3)
    else:
        # one tile wide: planks end on, stood in a stack (the sawn ends towards you)
        top = 4
        d.rectangle((2, top, W - 3, H - 4), fill=OUTLINE)
        for k, x in enumerate(range(3, W - 3, 3)):
            d.rectangle((x, top + 1, x + 1, H - 9), fill=PLANK)
            d.line((x + 1, top + 1, x + 1, H - 9), fill=PLANK_D)
        for y in range(H - 8, H - 4, 2):                         # the ends
            for x in range(3, W - 3, 3):
                d.rectangle((x, y, x + 1, y + 1), fill=POLE_L)
                px[x + 1, y + 1] = POLE_D
        snow_top = top
        x_range = range(2, W - 2)
    if season == "winter":
        for x in x_range:
            px[x, snow_top - 1] = SNOW
            px[x, snow_top] = SNOW if x % 4 else SNOW_D
            if rnd.random() < 0.4:
                px[x, snow_top - 2] = SNOW
    elif season == "fall":
        for x in rnd.sample(list(x_range), 3):                   # a leaf or two
            px[x, snow_top] = (196, 110, 40, 255)
    return im


# ---- the states, as images ----------------------------------------------------------

def state_image(m, n, state, season):
    g = GEOM[n]
    if state in ("forsale", "sold"):
        im = m.block(n, season, numerals=True)
        notice(im, g["notice"], season, sold=state == "sold")
        return im
    if state == "works":
        return works(m, n, season)
    return renovate(m, n, season, state)


# ---- the sheet, the map's tileset, the content pack -------------------------------------

class I18n:
    """This pack's i18n/default.json has comments in it, so it's edited as text: this
    tool's keys (vale.studios.sale.*) go in a block of their own at the end, replaced on
    every run."""
    PREFIX = "vale.studios.sale."
    HEADER = "  // The Old Studios' sale and Robin's job (tools/vale_studio_sale.py writes these)"

    def __init__(self):
        self.path = os.path.join(PACK, "i18n", "default.json")
        self.data = {}

    def add(self, key, text):
        self.data[key] = text
        return "{{i18n:" + key + "}}"

    def save(self):
        raw = open(self.path, encoding="utf-8", newline="").read()
        eol = "\r\n" if "\r\n" in raw else "\n"
        lines = raw.rstrip().split(eol)
        lines = [l for l in lines if l != self.HEADER and not l.lstrip().startswith('"' + self.PREFIX)]
        while lines and not lines[-1].strip():
            lines.pop()
        assert lines[-1].strip() == "}"
        body = lines[:-1]
        while body and not body[-1].strip():
            body.pop()
        if not body[-1].rstrip().endswith(","):
            body[-1] = body[-1].rstrip() + ","
        block = [""] + [self.HEADER] + [f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)}," for k, v in self.data.items()]
        block[-1] = block[-1][:-1]
        with open(self.path, "w", encoding="utf-8", newline="") as f:
            f.write(eol.join(body + block + ["}"]) + eol)


class Sheet:
    def __init__(self):
        self.tiles = []            # [{season: 16x16}]
        self.index = {}

    def add(self, per_season):
        key = b"".join(per_season[s].tobytes() for s in SEASONS)
        if key not in self.index:
            self.index[key] = len(self.tiles)
            self.tiles.append(per_season)
        return self.index[key]

    def save(self):
        rows = -(-len(self.tiles) // SHEET_COLS)
        for s in SEASONS:
            out = Image.new("RGBA", (SHEET_COLS * 16, rows * 16))
            for k, t in enumerate(self.tiles):
                out.alpha_composite(t[s], ((k % SHEET_COLS) * 16, (k // SHEET_COLS) * 16))
            out.save(os.path.join(ASSETS, SHEET.replace("spring_", s + "_")))
        return rows


def crop(im, cx, cy):
    return im.crop((cx * 16, cy * 16, cx * 16 + 16, cy * 16 + 16))


def build(preview=False):
    m = Map()
    sheet = Sheet()
    strings = I18n()
    patches = {}                   # (studio, state) -> MapTiles
    previews = {}
    for n, studio in STUDIOS.items():
        left = studio["left"]
        cells = m.cells(n)
        before = {s: m.block(n, s, numerals=True) for s in SEASONS}
        for state, _ in studio["states"]:
            imgs = {s: state_image(m, n, state, s) for s in SEASONS}
            previews[(n, state)] = imgs
            tiles = []
            for (x, y), (layer, _) in sorted(cells.items(), key=lambda c: (c[0][1], c[0][0])):
                cx, cy = x - left, y - TOP
                new = {s: crop(imgs[s], cx, cy) for s in SEASONS}
                if all(new[s].tobytes() == crop(before[s], cx, cy).tobytes() for s in SEASONS):
                    continue
                tiles.append({"Position": {"X": x, "Y": y}, "Layer": layer,
                              "SetTilesheet": TILESET, "SetIndex": sheet.add(new)})
            # new pixels where the studio had no tile would be lost: check there are none
            for s in SEASONS:
                for cy in range(9):
                    for cx in range(6):
                        if (left + cx, TOP + cy) not in cells and crop(imgs[s], cx, cy).getbbox():
                            raise SystemExit(f"studio {n} {state} {s}: art outside its tiles at {left + cx},{TOP + cy}")
            patches[(n, state)] = tiles
        # Robin's timber, for the works fortnight
        (tx, ty), (w, h) = TIMBER[n]
        arts = {s: timber(w, h, s) for s in SEASONS}
        previews[(n, "timber")] = arts
        for j in range(h):
            for i in range(w):
                idx = sheet.add({s: crop(arts[s], i, j) for s in SEASONS})
                bottom = j == h - 1
                patches[(n, "works")].append({
                    "Position": {"X": tx + i, "Y": ty - h + 1 + j},
                    "Layer": "Buildings" if bottom else "Front",
                    "SetTilesheet": TILESET, "SetIndex": idx,
                    **({"SetProperties": {"Action": f"Message {KEY}sale.timber"}} if bottom else {}),
                })
    rows = sheet.save()
    patch_tmx(m, rows)
    write_pack(patches, strings)
    strings.save()
    print(f"{len(sheet.tiles)} tiles on {SHEET} ({rows} rows); "
          + ", ".join(f"{n} {s}: {len(t)}" for (n, s), t in patches.items()))
    if preview:
        write_previews(m, previews)


def patch_tmx(m, rows):
    text = m.text
    ts = (f'  <tileset firstgid="{FIRST}" name="{TILESET}" tilewidth="16" tileheight="16" '
          f'tilecount="{rows * SHEET_COLS}" columns="{SHEET_COLS}">\n'
          f'    <image source="{SHEET}" width="{SHEET_COLS * 16}" height="{rows * 16}" />\n  </tileset>\n')
    text = re.sub(r'  <tileset firstgid="%d" name="%s".*?</tileset>\n' % (FIRST, TILESET), "", text, flags=re.S)
    i = text.index("  <layer ")
    text = text[:i] + ts + text[i:]
    if text != m.text:
        open(m.path, "w", encoding="utf-8").write(text)


# ---- the content pack ---------------------------------------------------------------------

WORDS = {
    ("2", "forsale"): "A notice is pinned over the old gold II: FOR SALE. Enquire at the Vale House.",
    ("2", "sold"): "SOLD, pasted across the notice. Somebody's bought Studio II.",
    ("2", "works"): "SOLD. Chalked on the scaffold pole: ROBIN'S CARPENTRY. MIND THE PLANKS.",
    ("2", "renovated"): "Studio II, the numeral new in gold. Under it, freshly painted: Opal & Jet Corrie, glass.",
    ("3", "forsale"): "A notice is pinned over the old gold III: FOR SALE. Enquire at the Vale House.",
    ("3", "sold"): "SOLD, pasted across the notice. Somebody's bought Studio III.",
    ("3", "works"): "SOLD. Chalked on the scaffold pole: ROBIN'S CARPENTRY. MIND THE PLANKS.",
    ("3", "new"): "The plaque's been sanded down to bare wood. The old gold numeral's gone. A card tucked in the door frame says: T. Brand.",
    ("3", "carved"): "Studio III, cut into the bare wood by hand. No gold. Under it, a card in the door frame: T. Brand, woodcarver.",
}
TIMBER_WORDS = "Sawn planks, stacked on bearers to keep them off the wet. Stencilled on the end of every one: ROBIN."
LETTER = ("Hi! A heads-up from the shop.^^I've taken a job in the Gilded Vale: one of the Old Studios has sold, "
          "and the buyer wants it ready by the end of the month. I'll be out there on weekdays.^^"
          "If I'm not behind the counter, leave your order and the money in the box. "
          "Building on the farm still comes first.^   -Robin")   # one page: her name had spilled onto a second (the user, 2026-11-11)
LETTER_TITLE = "Robin's away on a job"
BOOK = "Her order book lies open beside the box."
NOTE = ("Robin's not here. A note on the counter: 'Out in the Gilded Vale on a job, back this evening. "
        "Orders in the box, I'll see to them. -R'")

# Robin's weekdays in the Vale: the fortnight before each move-in, festivals left out
# (Winter 25, the Feast of the Winter Star; Fall 16, the Fair). Mon-Fri only.
ROBIN_DAYS = {"2": ("winter", 2, [15, 16, 17, 18, 19, 22, 23, 24, 26]),
              "3": ("fall", 3, [15, 17, 18, 19, 22, 23, 24, 25, 26])}
# where she works: hammering at the scaffold (facing it), then at her timber
ROBIN_SPOTS = {"2": ((84, 39, 0), (84, 44, 3)), "3": ((89, 39, 0), (94, 39, 0))}
HAMMER = "blinxxy.MuseArt_robin_hammer"


def when_between(a, b=None):
    return {f"Query: {{{{ValeDate}}}} >= {a}" + (f" AND {{{{ValeDate}}}} < {b}" if b else ""): "true"}


def write_pack(patches, strings):
    changes = []
    entries = {}
    for n, studio in STUDIOS.items():
        states = studio["states"]
        for k, (state, start) in enumerate(states):
            end = states[k + 1][1] if k + 1 < len(states) else None
            key = f"vale.studios.sale.plaque_{n}_{state}"
            entries[f"{KEY}sale.plaque_{n}_{state}"] = strings.add(key, WORDS[(n, state)])
            tiles = [dict(t) for t in patches[(n, state)]]
            for (x, y) in studio["words"]:
                for t in tiles:
                    if (t["Position"]["X"], t["Position"]["Y"]) == (x, y) and t["Layer"] == "Buildings":
                        t["SetProperties"] = {"Action": f"Message {KEY}sale.plaque_{n}_{state}"}
                        break
                else:
                    tiles.append({"Position": {"X": x, "Y": y}, "Layer": "Buildings",
                                  "SetProperties": {"Action": f"Message {KEY}sale.plaque_{n}_{state}"}})
            changes.append({
                "LogName": f"Studio {'II' if n == '2' else 'III'}: {state}",
                "Action": "EditMap", "Target": VALE,
                "When": when_between(start, end),
                "MapTiles": tiles,
            })
    entries[f"{KEY}sale.timber"] = strings.add("vale.studios.sale.timber", TIMBER_WORDS)
    changes.insert(0, {"Action": "EditData", "Target": "Strings/StringsFromMaps", "Entries": entries})

    # Robin
    sched = []
    for n, (season, year, days) in ROBIN_DAYS.items():
        (hx, hy, hf), (tx, ty, tf) = ROBIN_SPOTS[n]
        # Off at six (it's three hours' walk: up through town to the bus stop's NPC way
        # through, then the Vale's lane), hammering at the scaffold, at the timber after
        # lunch, and home for her own evening (her vanilla days end "1930 ScienceHouse 16 5"
        # and "2100 ... 21 4 1 robin_sleep"). On Winter 16 she keeps her Night Market evening.
        work = (f"600 blinxxy.MuseArt_GildedVale {hx} {hy} {hf} {HAMMER}/"
                f"1230 blinxxy.MuseArt_GildedVale {tx} {ty} {tf}/"
                f"1330 blinxxy.MuseArt_GildedVale {hx} {hy} {hf} {HAMMER}/")
        evening = {"winter_16": r'1530 Beach 13 31 1 "Strings\schedules\Robin:winter_16.000"/2330 ScienceHouse 21 4 1 robin_sleep'}
        home = "1530 ScienceHouse 16 5 0/2100 ScienceHouse 21 4 1 robin_sleep"
        sched.append({
            "LogName": f"Robin in the Vale (Studio {'II' if n == '2' else 'III'})",
            "Action": "EditData", "Target": "Characters/schedules/Robin",
            "When": {"Year": str(year)},
            "Entries": {f"{season}_{d}": work + evening.get(f"{season}_{d}", home) for d in days},
        })
    changes += sched
    changes.append({"Action": "EditData", "Target": "Data/animationDescriptions",
                    "Entries": {HAMMER: "24/24 25 26 26 27 27 27 27 27 27 27 27/24"}})
    changes.append({
        "LogName": "Robin's way to the Vale (the residents' NPC warps, while her jobs are still to come)",
        "Action": "EditMap", "Target": "Maps/BusStop", "Priority": "Late",
        "When": when_between(0, date(3, "winter", 1)),
        "TextOperations": [{"Operation": "Append", "Target": ["MapProperties", "NPCWarp"],
                            "Value": "23 9 blinxxy.MuseArt_GildedVale 3 8", "Delimiter": " "}],
    })
    changes.append({
        "Action": "EditMap", "Target": VALE, "When": when_between(0, date(3, "winter", 1)),
        "MapProperties": {"NPCWarp": "3 7 BusStop 23 10"},
    })
    mail_id = "blinxxy.MuseArt_RobinValeJob"
    changes.append({"Action": "EditData", "Target": "Data/mail", "Entries": {
        mail_id + "2": strings.add("vale.studios.sale.letter", LETTER) + "[#]" + strings.add("vale.studios.sale.letter.title", LETTER_TITLE),
        mail_id + "3": "{{i18n:vale.studios.sale.letter}}[#]{{i18n:vale.studios.sale.letter.title}}",
    }})
    trig = {}
    for n, (season, year, days) in ROBIN_DAYS.items():
        trig[mail_id + n] = {"Id": mail_id + n, "Trigger": "DayStarted",
                             "Condition": f"YEAR {year} {year}, SEASON {season}, DAY_OF_MONTH {' '.join(str(d) for d in range(days[0], 27))}",
                             "Actions": [f"AddMail Current {mail_id}{n} now"]}
    changes.append({"Action": "EditData", "Target": "Data/TriggerActions", "Entries": trig})
    changes.append({"Action": "EditData", "Target": "Mods/blinxxy.MuseArt/ValeStrings",
                    "Entries": {"robin.away": strings.add("vale.studios.sale.robin_away", NOTE),
                                "robin.book": strings.add("vale.studios.sale.robin_book", BOOK)}})

    head = ("// The Old Studios' sale: generated by tools/vale_studio_sale.py (edit that, not this).\n"
            "// Studio II: For Sale Fall 1 Y2, Sold Winter 1, Robin's scaffolding Winter 15, renovated Spring 1 Y3.\n"
            "// Studio III: For Sale Summer 1 Y3, Sold Fall 1, Robin's scaffolding Fall 15, renovated Winter 1 Y3\n"
            "// (its plaque bare), Tess's carved III from Winter 7. ValeDate is content.json's dynamic token.\n"
            "// Robin works in the Vale on weekdays in each fortnight; a farm build or house upgrade wins, as in\n"
            "// the game (NPC.updateConstructionAnimation ignores her schedule that day), and her counter takes\n"
            "// orders while she's out (ArtStudio/Vale/ValeStudios.cs).\n")
    path = os.path.join(PACK, "data", "vale_studios.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(head + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")


def write_previews(m, previews):
    out = os.path.join("docs", "media", "review", "studio_sale")
    os.makedirs(out, exist_ok=True)
    for (n, state), imgs in previews.items():
        w = max(i.width for i in imgs.values())
        h = max(i.height for i in imgs.values())
        sheet = Image.new("RGBA", (w * 4 + 12, h), (60, 70, 60, 255))
        for k, s in enumerate(SEASONS):
            sheet.alpha_composite(imgs[s], (k * (w + 4), 0))
        sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(os.path.join(out, f"{n}_{state}.png"))


if __name__ == "__main__":
    build(preview="preview" in sys.argv[1:])
