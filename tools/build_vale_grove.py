"""Builds the Gilded Grove (the Vale painters' guide design, section 5): a small, secret,
Secret-Woods-like map, a painting of the first painters' favourite place, always in fall.

  python tools/build_vale_grove.py [<vanilla exports dir>]

Writes "[CP] Gilded Vale (dev)/assets/GildedGrove.tmx" and its own sheet, GildedGrove.png.

How it's made (docs/terrain_painting.md):
- The ground is painted like the Vale's: terrain on the grid's corners, each cell the game's own
  stack for its corners (tools/vale_terrain.py), the pool's banks by build_vale's shore rules.
- The woods round the edge are the Secret Woods' own canopy, learned from the vanilla maps: for
  every cell near the woods, the stack (all four layers) the game uses where the woods lie the
  same way round it, chosen to join its neighbours as the game's own do.
- The map uses the game's spring_ sheets. The location's context holds the season at fall
  (data/vale_guide.json, SeasonOverride), so the game shows the fall sheets all year.
- Along the north edge the Grove ends in a drop, and below it the Vale is a painted panorama;
  the hidden easel stands at the edge, looking at it (plein air paints what's in front of an easel).

The panorama, the frame home, both easels and the echoes are PLACEHOLDER art until the art
batch's picks land: each is one entry in PIECES (and PANORAMA), so the final art drops in there.
"""
import collections, math, os, random, re, subprocess, sys, tempfile
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import build_vale as bv
from render_tmx import layer_data
from vale_terrain import TOWN_SHEETS, Table

ASSETS = os.path.join(ROOT, "[CP] Gilded Vale (dev)", "assets")
EXPORTS = os.environ.get("VALE_EXPORTS", "C:/Users/blinx/Desktop/MuseStardew/.devgame/exports_vale")
VALE_TMX = os.path.join(ASSETS, "GildedVale.tmx")
OUT_TMX = os.path.join(ASSETS, "GildedGrove.tmx")
SHEET = "GildedGrove.png"            # no season prefix: the Grove's own art never changes
SHEET_FIRST = 40000
SHEET_COLS = 22

W, H = 34, 29
ARRIVAL = (12, 11)                   # GildedGrove.ArrivalTile, and Data/Locations' DefaultArrivalTile
LAYERS = ["Back", "Buildings", "Paths", "Front", "AlwaysFront"]
CONTEXT = "blinxxy.MuseArt_GroveContext"
BLOCKER = 126                        # the outdoors sheet's empty tile (index 125): solid, invisible

# The view: the Vale below the drop, along the north edge (x, y, width, height in tiles).
PANORAMA = (6, 0, 22, 7)
# The pieces, each 1 x 2 tiles: the bottom tile (its foot) is solid and carries the action;
# the top goes on Front, so you walk behind it (tall things: feet only).
#   name: (foot x, foot y, Action)
PIECES = {
    "frame": (5, 13, "blinxxy.MuseArt_GroveFrame"),
    "hidden_easel": (16, 8, "blinxxy.MuseArt_HiddenEasel"),
    "founders_easel": (9, 21, "blinxxy.MuseArt_FoundersEasel"),
    "echo_agnes": (22, 9, "blinxxy.MuseArt_Echo agnes"),
    "echo_hollis": (8, 10, "blinxxy.MuseArt_Echo hollis"),
    "echo_mabel": (17, 20, "blinxxy.MuseArt_Echo mabel"),
    "echo_bram": (25, 20, "blinxxy.MuseArt_Echo bram"),
}
PIECE_ORDER = list(PIECES)
# Art on the sheet that isn't laid on the map (the mod draws it), each 16 x 32, in the columns after
# the pieces:
#   founders_canvas: the Founders' easel with a finished canvas on it, drawn over the easel for a
#   player who has offered it a masterpiece (GildedGrove.FinishedCanvasSource, column 7). Only the
#   canvas is drawn (rows 0-17), so the rest must match founders_easel.
EXTRAS = ["founders_canvas"]
# The echoes' second frames (the idle motion: a brush, a blade or a piece of glass moving), each 16 x 32,
# in the columns after EXTRAS, top row and foot row like the pieces. Each echo's tiles (the top on Front,
# the foot on Buildings) are TMX animations: frame 0 is the tile itself, frame 1 is here.
ECHO_FRAMES = [n for n in PIECE_ORDER if n.startswith("echo_")]
ECHO_FRAME_FIRST_COL = len(PIECE_ORDER) + len(EXTRAS)
ECHO_FRAME_MS = 700
# The pool: corners of its water (grid corners), its top corners cut on 45-degree diagonals
# (west, east), its south shore flat. (The game's pieces turn a short diagonal into a staircase on
# the west side; a longer one there joins up.)
POOL = (14, 13, 20, 17, 0, 2)
# Old trees: Paths markers (9 oak, 10 maple), planted by the game on load.
TREES = [((10, 16), 10), ((13, 22), 9), ((24, 16), 10), ((22, 23), 9)]


# ---- the woods ----

def forest_mask():
    """The woods' cells (their canopy included): straight runs, a corner cut on the diagonal and a
    bump on the east side that tapers in and out a tile a row. Only shapes the Secret Woods itself
    has, so every cell near them has the game's own stack (squared steps and corners it hasn't got
    came out patchy). The panorama's sides are framed by woods down to the drop."""
    F = set()
    for y in range(H):
        for x in range(W):
            bump = 0
            if (x <= 3 or x >= 30 or y >= 25
                    or (x <= 5 and y <= 6) or (x >= 28 and y <= 6)         # framing the view
                    or (x - 30) + (y - 25) >= -2                          # the south-east corner, cut
                    or x >= 30 - bump):                                   # the east bump
                F.add((x, y))
    px, py, pw, ph = PANORAMA
    return {c for c in F if not (px <= c[0] < px + pw and py <= c[1] < py + ph)}


def woods_band(F, r):
    """Cells within r (Chebyshev) of the woods, the map's edge counting as woods."""
    out = []
    for y in range(H):
        for x in range(W):
            if min(x, y, W - 1 - x, H - 1 - y) < r or any((i, j) in F for i in range(x - r, x + r + 1) for j in range(y - r, y + r + 1)):
                out.append((x, y))
    return out


CANOPY = {r * 25 + c + 1 for r in range(37, 43) for c in range(15, 22)}   # Town gids of the woods' canopy block


def load_stacks(exports):
    """Every vanilla map's cells as four-layer stacks in Town gids (None where a sheet isn't Town's)."""
    grids = []
    # (Only the Secret Woods: the Forest's canopy meets water and bushes, which came along with it.)
    for name in ("Woods",):
        path = os.path.join(exports, name + ".tmx")
        if not os.path.exists(path):
            continue
        root = ET.parse(path).getroot()
        remap = []
        for ts in root.findall("tileset"):
            image = os.path.basename(ts.find("image").get("source").replace("\\", "/"))
            if image in TOWN_SHEETS:
                remap.append((int(ts.get("firstgid")), int(ts.get("tilecount")), TOWN_SHEETS[image][0]))

        def to_town(v, remap=remap):
            if v == 0:
                return 0
            for first, count, town_first in remap:
                if first <= v < first + count:
                    return town_first + v - first
            return None
        layers = {l.get("name"): l for l in root.findall("layer")}
        w, h = int(layers["Back"].get("width")), int(layers["Back"].get("height"))
        data = {n: layer_data(layers[n].find("data")) if n in layers else [0] * (w * h) for n in ("Back", "Buildings", "Front", "AlwaysFront")}
        cells = []
        for i in range(w * h):
            st = tuple(to_town(data[n][i]) for n in ("Back", "Buildings", "Front", "AlwaysFront"))
            cells.append(None if None in st or st[0] == 0 else st)
        grids.append((name, w, h, cells))
    return grids


class Woods:
    """What the game puts round its woods: keyed by the woods' shape over a cell's 5 x 5 (then 3 x 3)
    neighbourhood, the stacks used there; and which stacks it puts beside which."""

    def __init__(self, exports):
        self.ctx5, self.ctx3 = collections.defaultdict(collections.Counter), collections.defaultdict(collections.Counter)
        self.right, self.down = collections.Counter(), collections.Counter()
        for name, w, h, cells in load_stacks(exports):
            wood = [[c is not None and any(v in CANOPY for v in c[1:]) for x, c in enumerate(cells[y * w:(y + 1) * w])] for y in range(h)]
            if not any(any(r) for r in wood):
                continue
            def f(x, y):
                return wood[y][x] if 0 <= x < w and 0 <= y < h else True      # off the map is woods
            for y in range(h):
                for x in range(w):
                    c = cells[y * w + x]
                    if c is None:
                        continue
                    k5 = tuple(f(x + i, y + j) for j in range(-2, 3) for i in range(-2, 3))
                    if not any(k5):
                        continue
                    self.ctx5[k5][c] += 1
                    self.ctx3[tuple(f(x + i, y + j) for j in range(-1, 2) for i in range(-1, 2))][c] += 1
                    if x + 1 < w and cells[y * w + x + 1]:
                        self.right[(c, cells[y * w + x + 1])] += 1
                    if y + 1 < h and cells[(y + 1) * w + x]:
                        self.down[(c, cells[(y + 1) * w + x])] += 1

    def lay(self, g, F, skip):
        """Lays the woods' stacks on every cell within two of the woods (not in skip)."""
        def f(x, y):
            return (x, y) in F if 0 <= x < W and 0 <= y < H else True
        stats = collections.Counter()
        for y in range(H):
            for x in range(W):
                if (x, y) in skip:
                    continue
                k5 = tuple(f(x + i, y + j) for j in range(-2, 3) for i in range(-2, 3))
                if not any(k5):
                    continue
                cands, lvl = self.ctx5.get(k5), 5
                if not cands:
                    cands, lvl = self.ctx3.get(tuple(f(x + i, y + j) for j in range(-1, 2) for i in range(-1, 2))), 3
                if not cands:
                    stats["none"] += 1
                    continue
                left = tuple(g[l][y][x - 1] for l in ("Back", "Buildings", "Front", "AlwaysFront")) if x else None
                up = tuple(g[l][y - 1][x] for l in ("Back", "Buildings", "Front", "AlwaysFront")) if y else None

                def score(c):
                    s = math.log(1 + cands[c])
                    if left:
                        s += 1.5 * math.log(1 + self.right[(left, c)])
                    if up:
                        s += 1.5 * math.log(1 + self.down[(up, c)])
                    return s
                best = max(cands, key=score)
                for l, v in zip(("Back", "Buildings", "Front", "AlwaysFront"), best):
                    g[l][y][x] = v
                stats[lvl] += 1
        print("woods cells by context (5 = full 5x5):", dict(stats))


# ---- the ground ----

def noise(scale, seed):
    """Smooth value noise in 0..1 (build_vale's)."""
    r = random.Random(seed)
    grid = [[r.random() for _ in range(W // scale + 3)] for _ in range(H // scale + 3)]

    def at(x, y):
        fx, fy = x / scale, y / scale
        x0, y0 = int(fx), int(fy)
        tx, ty = fx - x0, fy - y0
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = grid[y0][x0] * (1 - tx) + grid[y0][x0 + 1] * tx
        b = grid[y0 + 1][x0] * (1 - tx) + grid[y0 + 1][x0 + 1] * tx
        return a * (1 - ty) + b * ty
    return at


def paint_ground(g, table, F):
    """The ground by corners: light grass in the sunny middle, the woods' darker grass round it,
    and the pool with its sand rim and two-deep banks."""
    bv.W, bv.H = W, H                       # build_vale's shore rules, on this map
    bv.NO_DEPTH_ROWS = set()
    bv.GROUND.update(table.corners)
    rnd = random.Random(7)
    V = [["D"] * (W + 1) for _ in range(H + 1)]

    def around(x, y, r):
        return [(i, j) for j in range(max(0, y - r), min(H + 1, y + r + 1)) for i in range(max(0, x - r), min(W + 1, x + r + 1))]

    # The woods' own ground runs two cells out from them (Woods.lay) and ends in their dark grass,
    # so every corner within three cells of the woods is dark grass too, and the seam is the game's.
    # The dark grass reaches a little further in here and there (soft noise), so it doesn't meet
    # the light in a ruler-straight line.
    near = set(woods_band(F, 3))
    wider = {r: set(woods_band(F, r)) for r in (4, 5, 6)}
    reach = noise(3, 21)
    for y in range(H + 1):
        for x in range(W + 1):
            r = 3 + int(reach(x, y) * 4)                       # 3 to 6
            band = near if r == 3 else wider[min(r, 6)]
            if not any((i, j) in band for i in (x - 1, x) for j in (y - 1, y)):
                V[y][x] = "G"
    # the pool: a rectangle, its top corners cut on a clean diagonal, a flat south shore
    x0, y0, x1, y1, cut_w, cut_e = POOL
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if (y - y0) + (x - x0) >= cut_w and (y - y0) + (x1 - x) >= cut_e:
                V[y][x] = "W"
    # a sand rim round the water, one to two tiles wide, with room above and beside the banks
    for x, y in [(x, y) for y in range(H + 1) for x in range(W + 1) if V[y][x] != "W" and any(V[j][i] == "W" for i, j in around(x, y, 1))]:
        V[y][x] = "S"
    for x, y in [(x, y) for y in range(H + 1) for x in range(W + 1)
                 if V[y][x] in "GD" and rnd.random() < 0.45 and any(V[j][i] == "W" for i, j in around(x, y, 2))]:
        V[y][x] = "S"
    deepen = []
    for y in range(1, H):
        for x in range(1, W):
            if V[y][x] == "S" and V[y + 1][x] == "W":
                deepen += [(x, y - 1), (x, y - 2)]
            if V[y][x] == "S" and V[y][x + 1] == "W":
                deepen += [(x - 1, y), (x - 2, y)]
            if V[y][x] == "S" and V[y][x - 1] == "W":
                deepen += [(x + 1, y), (x + 2, y)]
    for x, y in deepen:
        if 0 <= x <= W and 0 <= y <= H and V[y][x] in "GD":
            V[y][x] = "S"
    # dark grass never touches sand or water directly: light grass between (the pool is placed
    # clear of the woods' band, so this never reaches it)
    for y in range(H + 1):
        for x in range(W + 1):
            if V[y][x] == "D" and any(V[j][i] in "SW" for i, j in around(x, y, 2)):
                assert not any((i, j) in near for i in (x - 1, x) for j in (y - 1, y)), f"the pool's sand reaches the woods at {x},{y}"
                V[y][x] = "G"
    # no lone corners, no checkerboard diagonals
    for _ in range(2):
        for y in range(1, H):
            for x in range(1, W):
                n = {V[y - 1][x], V[y + 1][x], V[y][x - 1], V[y][x + 1]}
                if len(n) == 1 and V[y][x] not in n and "W" not in n | {V[y][x]}:
                    V[y][x] = n.pop()
    for _ in range(3):
        for y in range(H):
            for x in range(W):
                tl, tr, bl, br = V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1]
                if tl == br and tr == bl and tl != tr:
                    keep = next(c for c in "WSGD" if c in (tl, tr))
                    V[y][x] = V[y][x + 1] = V[y + 1][x] = V[y + 1][x + 1] = keep

    # lay the tiles by their corners (build_vale.paint_ground's rules)
    for y in range(H):
        for x in range(W):
            pat = (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
            if len(set(pat)) == 1:
                bare = [c for c in table.by_pattern.get(pat, []) if c[0][1] == 0][:2 if pat == ("G",) * 4 else 1]
                stack = bare[0][0] if len(bare) == 1 or rnd.random() < bare[0][1] / (bare[0][1] + bare[1][1]) else bare[1][0]
            elif pat in bv.SHORE_PIN:
                stack = bv.SHORE_PIN[pat]
            elif "W" in pat:
                cands = [c for c in table.by_pattern.get(pat, []) if c[0][1] not in bv.LILY_PADS] or table.by_pattern.get(pat, [])
                bare = [c for c in cands if c[0][1] == 0]
                stack = (bare[0][0] if bare and bare[0][1] >= cands[0][1] * 0.25 else cands[0][0]) if cands and cands[0][1] >= 5 else None
            else:
                bare = [c for c in table.by_pattern.get(pat, []) if c[0][1] == 0]
                stack = bare[0][0] if bare else table.pick(pat, rnd, top=1)
            if stack is None:
                best = min((q for q in table.by_pattern if set(q) <= set(pat) and table.by_pattern[q][0][1] >= 5),
                           key=lambda q: sum(a != b for a, b in zip(q, pat)), default=("G",) * 4)
                stack = table.pick(best, rnd, top=1)
            g["Back"][y][x], g["Buildings"][y][x] = stack
    bv.paint_shores(g, V, table)
    bv.vertical_banks(g, V, table)
    bv.bank_depth(g)
    # The game's contexts sometimes offer a pier's posts standing in open water; not here.
    for y in range(H):
        for x in range(W):
            if g["Back"][y][x] in PIER_POSTS or g["Buildings"][y][x] in PIER_POSTS:
                g["Back"][y][x], g["Buildings"][y][x] = (1230, 0)
    return V


PIER_POSTS = set(range(1717, 1723))


# ---- the Grove's own sheet: the panorama and the pieces (PLACEHOLDERS) ----

def render_vale_fall(exports):
    """The Vale in fall, as a picture (tools/render_tmx.py over the fall sheets)."""
    out = os.path.join(tempfile.gettempdir(), "grove_vale_fall.png")
    subprocess.run([sys.executable, os.path.join(HERE, "render_tmx.py"), VALE_TMX, out,
                    "--sheets", os.path.join(exports, "sheets_fall"), "--scale", "1"], check=True, capture_output=True)
    return Image.open(out).convert("RGBA")


def panorama_art(exports):
    """PLACEHOLDER: the Vale seen from above in gold light, the drop's grassy lip along its foot.
    A band of the village, shrunk, warmed and hazed with distance. Replace with the art batch's piece."""
    pw, ph = PANORAMA[2] * 16, PANORAMA[3] * 16
    vale = render_vale_fall(exports).crop((8 * 16, 12 * 16, 88 * 16, 44 * 16))
    pic = vale.resize((pw, ph - 6), Image.BOX)
    pic = ImageEnhance.Color(pic).enhance(0.8)
    gold = Image.new("RGBA", pic.size, (255, 200, 110, 255))
    pic = Image.blend(pic, gold, 0.28)
    haze = Image.new("RGBA", pic.size)
    d = ImageDraw.Draw(haze)
    for y in range(pic.height):                      # farther (higher) is paler
        d.line([(0, y), (pw, y)], fill=(255, 236, 190, int(150 * (1 - y / pic.height) ** 1.6)))
    pic.alpha_composite(haze)
    art = Image.new("RGBA", (pw, ph))
    art.paste(pic, (0, 0))
    # the lip: the Grove's grass ending at the drop, with a dark edge
    lip = ImageDraw.Draw(art)
    lip.rectangle([0, ph - 6, pw, ph], fill=(214, 120, 40, 255))
    lip.line([(0, ph - 6), (pw, ph - 6)], fill=(110, 52, 26, 255), width=2)
    for x in range(0, pw, 3):
        lip.point((x, ph - 7 - (x * 7 % 3 == 0)), fill=(232, 150, 52, 255))
    return art


def easel_sprite():
    return Image.open(os.path.join(ROOT, "ArtStudio", "assets", "bigcraftables.png")).convert("RGBA").crop((0, 0, 16, 32))


def piece_art(name, frame=0):
    """PLACEHOLDERS, 16 x 32 each: the art batch's pieces replace these one by one. The echoes also
    have a second frame (frame=1): the working arm lifted a little, as a brush moves."""
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    if name == "frame":
        # a gilt frame on a little stand; through it, faint as a sketch, a low wall and a gate
        d.rectangle([1, 2, 14, 25], fill=(232, 220, 196, 255))
        d.line([(3, 19), (12, 19)], fill=(150, 140, 130, 255))
        d.line([(3, 17), (6, 17)], fill=(150, 140, 130, 255))
        d.line([(9, 17), (12, 17)], fill=(150, 140, 130, 255))
        d.line([(7, 14), (7, 19)], fill=(150, 140, 130, 255))
        d.line([(8, 14), (8, 19)], fill=(150, 140, 130, 255))
        d.rectangle([1, 2, 14, 25], outline=(196, 148, 40, 255))
        d.rectangle([0, 1, 15, 26], outline=(124, 84, 24, 255))
        d.line([(4, 27), (3, 31)], fill=(110, 70, 30, 255), width=2)
        d.line([(11, 27), (12, 31)], fill=(110, 70, 30, 255), width=2)
    elif name == "hidden_easel":
        im = easel_sprite()
    elif name == "founders_easel":
        # an old, empty easel: the stand without its canvas, greyed by the years
        im = easel_sprite()
        px = im.load()
        for y in range(2, 18):
            for x in range(1, 15):
                r, g_, b, a = px[x, y]
                if a and not (r > g_ + 20 and r > b + 20 and r < 200):        # keep the wood, drop the canvas
                    px[x, y] = (0, 0, 0, 0)
        im = ImageEnhance.Color(im).enhance(0.45)
    elif name == "founders_canvas":
        # the same easel, holding a finished canvas: a little golden landscape (any painting, not yours)
        im = piece_art("founders_easel")
        d = ImageDraw.Draw(im)
        d.rectangle([2, 4, 13, 16], fill=(246, 214, 140, 255))          # gold sky
        d.ellipse([9, 5, 12, 8], fill=(255, 244, 196, 255))             # low sun
        d.polygon([(2, 12), (6, 9), (10, 11), (13, 9), (13, 16), (2, 16)], fill=(204, 120, 48, 255))   # fall hills
        d.rectangle([5, 13, 10, 14], fill=(110, 150, 170, 255))         # the pool
        d.line([(4, 8), (4, 12)], fill=(92, 52, 30, 255))               # a tree
        d.ellipse([2, 6, 6, 9], fill=(176, 72, 34, 255))
    else:
        # an echo: loose pale linework of a figure at work (Task 9 draws the real ones)
        tint = {"echo_agnes": (240, 200, 120), "echo_hollis": (200, 160, 110), "echo_mabel": (150, 200, 230), "echo_bram": (190, 170, 150)}[name]
        line = tint + (210,)
        d.ellipse([5, 5, 10, 10], outline=line)
        d.line([(7, 11), (7, 22)], fill=line)
        if frame == 0:
            d.line([(7, 14), (12, 17)], fill=line)
        else:
            d.line([(7, 14), (12, 14)], fill=line)
            d.point([(13, 13)], fill=line)                      # the tool's tip, lifted
        d.line([(7, 14), (3, 18)], fill=line)
        d.line([(7, 22), (4, 30)], fill=line)
        d.line([(7, 22), (10, 30)], fill=line)
    return im


def build_sheet(exports):
    pan = panorama_art(exports)
    sheet = Image.new("RGBA", (SHEET_COLS * 16, (PANORAMA[3] + 2) * 16))
    sheet.paste(pan, (0, 0))
    for i, name in enumerate(PIECE_ORDER + EXTRAS):
        sheet.paste(piece_art(name), (i * 16, PANORAMA[3] * 16))
    for i, name in enumerate(ECHO_FRAMES):
        sheet.paste(piece_art(name, 1), ((ECHO_FRAME_FIRST_COL + i) * 16, PANORAMA[3] * 16))
    sheet.save(os.path.join(ASSETS, SHEET))
    return sheet


def sheet_gid(col, row):
    return SHEET_FIRST + row * SHEET_COLS + col


# ---- the map ----

def vanilla_tilesets():
    """The game's sheets, copied from the Vale's map with their tile properties (water, passable)."""
    text = open(VALE_TMX, encoding="utf-8").read()
    keep = ("Landscape", "Paths", "Town", "v16_landscape2")
    return [t for t in re.findall(r"  <tileset .*?</tileset>", text, re.S) if re.search(r'name="([^"]+)"', t).group(1) in keep]


def animated_tiles():
    """{local tile id: [frame local ids]} for the echoes' top and foot tiles."""
    out = {}
    ph = PANORAMA[3]
    for i, name in enumerate(ECHO_FRAMES):
        col = PIECE_ORDER.index(name)
        for row in (ph, ph + 1):
            out[row * SHEET_COLS + col] = [row * SHEET_COLS + col, row * SHEET_COLS + ECHO_FRAME_FIRST_COL + i]
    return out


def write(g, props, tile_props, sheet):
    out = ['<?xml version="1.0" encoding="utf-8"?>',
           f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" '
           f'tilewidth="16" tileheight="16" infinite="0" nextlayerid="{2 * len(LAYERS) + 1}" nextobjectid="1000">',
           '  <properties>']
    out += [f'    <property name="{k}" type="string" value="{v}" />' for k, v in props.items()]
    out.append('  </properties>')
    out += vanilla_tilesets()
    out.append(f'  <tileset firstgid="{SHEET_FIRST}" name="z_blinxxy.MuseArt_GildedGrove" tilewidth="16" tileheight="16" '
               f'tilecount="{(sheet.width // 16) * (sheet.height // 16)}" columns="{sheet.width // 16}">')
    out.append(f'    <image source="{SHEET}" width="{sheet.width}" height="{sheet.height}" />')
    for tid, frames in sorted(animated_tiles().items()):
        out.append(f'    <tile id="{tid}">')
        out.append('      <animation>')
        out += [f'        <frame tileid="{f}" duration="{ECHO_FRAME_MS}" />' for f in frames]
        out.append('      </animation>')
        out.append('    </tile>')
    out.append('  </tileset>')
    for i, layer in enumerate(LAYERS):
        out.append(f'  <layer id="{i + 1}" name="{layer}" width="{W}" height="{H}">')
        out.append('    <data encoding="csv">')
        out.append(",\n".join(",".join(str(v) for v in row) for row in g[layer]))
        out.append('    </data>')
        out.append('  </layer>')
    oid = 1
    for i, layer in enumerate(LAYERS):
        ps = sorted((x, y, p) for (l, x, y), p in tile_props.items() if l == layer)
        if not ps:
            continue
        out.append(f'  <objectgroup id="{len(LAYERS) + 1 + i}" name="{layer}" visible="false">')
        for x, y, p in ps:
            out.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            out.append('      <properties>')
            out += [f'        <property name="{k}" type="string" value="{v}" />' for k, v in p.items()]
            out.append('      </properties>')
            out.append('    </object>')
            oid += 1
        out.append('  </objectgroup>')
    out.append('</map>')
    open(OUT_TMX, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
    print("wrote", OUT_TMX)


def main():
    exports = sys.argv[1] if len(sys.argv) > 1 else EXPORTS
    g = {l: [[0] * W for _ in range(H)] for l in LAYERS}
    F = forest_mask()
    table = Table(exports)
    paint_ground(g, table, F)

    px, py, pw, ph = PANORAMA
    view = {(x, y) for y in range(py, py + ph) for x in range(px, px + pw)}
    Woods(exports).lay(g, F, skip=view)

    # the panorama: the picture on Back, an invisible wall over it (you can't walk off the drop)
    sheet = build_sheet(exports)
    for x, y in view:
        g["Back"][y][x] = sheet_gid(x - px, y - py)
        g["Buildings"][y][x] = BLOCKER
        g["Front"][y][x] = g["AlwaysFront"][y][x] = 0
    # the woods above the drop are thick: nothing walkable beside the view, nor on the edge of the map
    for y in range(H):
        for x in range(W):
            edge = x in (0, W - 1) or y in (0, H - 1)
            if ((x, y) in F and y < py + ph or edge) and not g["Buildings"][y][x]:
                g["Buildings"][y][x] = BLOCKER

    tile_props = {}
    for i, name in enumerate(PIECE_ORDER):
        x, y, action = PIECES[name]
        g["Buildings"][y][x] = sheet_gid(i, ph + 1)
        g["Front"][y - 1][x] = sheet_gid(i, ph)
        tile_props[("Buildings", x, y)] = {"Action": action}

    paths_first = 1976
    for (x, y), kind in TREES:
        g["Paths"][y][x] = paths_first + kind

    props = {"Outdoors": "T", "LocationContext": CONTEXT}
    write(g, props, tile_props, sheet)


if __name__ == "__main__":
    main()
