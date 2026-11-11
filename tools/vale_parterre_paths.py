"""The sculpture garden's paths, cleared (the user, 2026-11-11: "clear the grass off the sculpture
garden's other paths too"). The parterre was painted (build_vale.py FEATURES "parterre", a PixelLab
inpaint) with its light grass spilling over the gravel walks: tufts and whole flower-dotted
patches lying across them, so the paths read as half buried. This clears them, in the garden's
own pixels, in all four seasons: the beds, the hedges, the statues, the grass against the walls
and the vines on them stay as they are.

How (docs/terrain_painting.md: textured ground, a natural edge, never a ruler line or a black one):
- The walks' shape is the gravel as painted (spring), closed with a disc (CLOSE_R px): every gap
  in it narrower than the disc, where grass has spilled in from an edge or lies across a walk,
  becomes walk. Nothing that isn't grass is touched (hedges, flowers, stone, statues, shadow).
- Within EDGE_KEEP px of the walk's outline the painted tufts stay, so the edge keeps the
  painting's own tufted grass line, as the game's dirt paths have.
- A cleared pixel takes the season's own gravel: a pick from the colours that season's art
  has on the walks' inside (spring and summer orange, fall brown, winter the snow on them), by
  a hash of its position, so the ground stays textured and seasonal.
- Only the garden's own tiles change (each used once on the map, inside the garden), in the
  four <season>_GildedValeBuildings.png sheets. The map's tiles don't change.

Re-runnable: when it has run, it records a hash of the garden's own tiles in all four sheets
(DONE); if they still match, it does nothing (a second pass would widen the walks again). A
rebuild that paints the garden afresh no longer matches, so the walks are cleared again.

Run as: python tools/vale_parterre_paths.py <GildedVale.tmx> [exports]   (build_vale.VALE_PIECES)
"""
import hashlib, json, os, re, sys, zlib
import numpy as np
from PIL import Image
from scipy import ndimage

GARDEN = (6, 38, 27, 46)                 # inclusive tiles: inside the parterre's walls, below the cottage
LAYERS = ("Back2", "Buildings", "Front", "Front2")
SHEET = "z_blinxxy.MuseArt_GildedValeBuildings"
SEASONS = ("spring", "summer", "fall", "winter")
CLOSE_R = 6                              # px: the widest spill of grass across a walk that's cleared
GRAIN_PCT = 22                           # % of a laid walk's pixels that are a grain, not the ground colour
RELAY_EDGE = 3                           # px: the painted edge kept round the walk when it's laid again
DONE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "art", "gilded_vale", "parterre_paths_done.json")
EDGE_KEEP = 1                            # px: the painted tufts kept along the walk's edge


def gravel(c):
    """The parterre's gravel in spring: warm orange, its yellow edge pixels, and its shadowed
    pixels (the birdbath's)."""
    r, g, b, a = (int(v) for v in c)
    if a < 200:
        return False
    if r > 140 and r >= g - 5 and b < g - 30:
        return True
    return 90 < r <= 140 and r > g + 20 and g > b + 15


def grass(c):
    """The painted lawn's light greens (a few specks over the walks), not the hedges' darker
    leaves and their highlights, nor flowers, stone, statues or the birdbath's water."""
    r, g, b, a = (int(v) for v in c)
    return a >= 200 and r >= 60 and g >= 120 and g > r + 15 and g > b + 40


def hedge(c):
    r, g, b, a = (int(v) for v in c)
    return a >= 200 and r < 70 and g > r + 10


# The walks, as painted (tiles, x0 y0 x1 y1; "v" runs north-south, "h" east-west): the central
# one from the cottage to the south wall, the main walk, and the narrow ones round the four beds.
WALKS = [("v", (11.5, 38.8, 13.5, 41.6)), ("v", (11.5, 43.6, 13.5, 46.4)),
         ("h", (7.6, 41.4, 27.5, 43.8)),
         ("v", (7.4, 39.2, 8.6, 41.6)), ("v", (7.4, 43.6, 8.6, 45.8)), ("h", (7.6, 39.0, 12.0, 40.0)),
         ("v", (18.0, 39.2, 19.2, 41.6)), ("v", (18.0, 43.6, 19.2, 45.6)),
         ("v", (23.4, 39.2, 24.6, 41.6)), ("v", (23.4, 43.6, 24.6, 45.6)),
         ("h", (18.2, 39.0, 24.4, 40.0)), ("h", (18.2, 45.0, 24.4, 46.0))]


# the statues (inclusive tiles): west, east (and the row its shadow falls on), south, and the
# hidden gardener in the hedge (tools/vale_sculpture.py)
STATUES = [(6, 40, 7, 43), (25, 40, 25, 43), (16, 44, 16, 46), (6, 44, 7, 45)]


def wobble(n, seed):
    """A walk's edge, wavering: a slow wave and a pixel of jitter (never a ruler line)."""
    import math
    return int(round(1.2 * math.sin(n / 4.7 + seed * 1.9) + 0.6 * math.sin(n / 1.7 + seed)))


def disc(r):
    y, x = np.ogrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= r * r


def layers(text):
    out = {}
    for name in LAYERS:
        m = re.search(rf'name="{name}"[^>]*>\s*<data encoding="csv">(.*?)</data>', text, re.S)
        out[name] = [[int(v) for v in r.strip().rstrip(",").split(",")] for r in m.group(1).strip().split("\n")]
    return out


def patch_file(tmx_path, exports=None):
    assets = os.path.dirname(os.path.abspath(tmx_path))
    text = open(tmx_path, encoding="utf-8").read()
    first = int(re.search(rf'firstgid="(\d+)" name="{SHEET}"', text).group(1))
    g = layers(text)
    names = re.findall(r'<layer id="\d+" name="([^"]+)"', text)
    # every use of each of our gids anywhere on the map: only those used once, in the garden, change
    uses = {}
    for name in names:
        m = re.search(rf'name="{re.escape(name)}"[^>]*>\s*<data encoding="csv">(.*?)</data>', text, re.S)
        for y, row in enumerate(m.group(1).strip().split("\n")):
            for x, v in enumerate(row.strip().rstrip(",").split(",")):
                v = int(v)
                if first <= v < first + 10000:
                    uses.setdefault(v, []).append((x, y))
    x0, y0, x1, y1 = GARDEN
    own = {v for v, at in uses.items() if len(at) == 1 and x0 <= at[0][0] <= x1 and y0 <= at[0][1] <= y1}
    paths = {s: os.path.join(assets, f"{s}_GildedValeBuildings.png") for s in SEASONS}
    sheets = {s: np.array(Image.open(p).convert("RGBA")) for s, p in paths.items()}
    spring = sheets["spring"].copy()                 # (as painted, for every season's decisions)
    W, H = (x1 - x0 + 1) * 16, (y1 - y0 + 1) * 16

    def garden_hash():
        h = hashlib.sha1()
        for season in SEASONS:
            for gid in sorted(own):
                i = gid - first
                h.update(sheets[season][(i // 32) * 16:(i // 32) * 16 + 16, (i % 32) * 16:(i % 32) * 16 + 16].tobytes())
        return h.hexdigest()
    if os.path.exists(DONE) and json.load(open(DONE)).get("hash") == garden_hash():
        print("parterre paths: already cleared")
        return 0

    def cell(sheet, gid):
        i = gid - first
        return (i // 32) * 16, (i % 32) * 16           # row, column of the tile in the sheet (px)

    # the garden as painted, layer by layer, in spring: what's there, top layer last
    stack = {name: np.zeros((H, W, 4), np.uint8) for name in LAYERS}
    for name in LAYERS:
        for ty in range(y1 - y0 + 1):
            for tx in range(x1 - x0 + 1):
                gid = g[name][y0 + ty][x0 + tx]
                if first <= gid < first + 10000:
                    r, c = cell(None, gid)
                    stack[name][ty * 16:ty * 16 + 16, tx * 16:tx * 16 + 16] = spring[r:r + 16, c:c + 16]
    top = np.zeros((H, W, 4), np.uint8)
    top_layer = np.full((H, W), "", object)
    for name in LAYERS:
        opaque = stack[name][..., 3] >= 200
        top[opaque] = stack[name][opaque]
        top_layer[opaque] = name
    is_gravel = np.array([[gravel(tuple(p)) for p in row] for row in top])
    is_grass = np.array([[grass(tuple(p)) for p in row] for row in top])
    is_bare = top[..., 3] < 200          # nothing painted: the map's own lawn shows (Back)
    # above the beds' row is the cottage's and the cold frames' foot (their wood isn't gravel),
    # except where the central walk leaves the cottage door
    north = np.zeros((H, W), bool)
    north[:int((39 - y0) * 16), :] = True
    north[:, int((11.5 - x0) * 16):int((13.5 - x0) * 16)] = False
    is_gravel &= ~north

    # the beds (their hedges and what grows between them) are never touched: the hedges'
    # leaves, closed over the bed and filled, and a pixel round that
    is_hedge = np.array([[hedge(tuple(p)) for p in row] for row in top])
    beds = ndimage.binary_fill_holes(ndimage.binary_closing(is_hedge, structure=disc(8), border_value=0))
    beds = ndimage.binary_dilation(beds, structure=disc(1))
    # each walk as a band of its own typical width (the gravel's median edges along it), its
    # edges wavering a pixel or two, so it runs on through where the lawn had covered it
    walk = np.zeros((H, W), bool)
    for kind, (ax0, ay0, ax1, ay1) in WALKS:
        X0, Y0 = int((ax0 - x0) * 16), int((ay0 - y0) * 16)
        X1, Y1 = int((ax1 - x0) * 16), int((ay1 - y0) * 16)
        box = is_gravel[Y0:Y1, X0:X1] if kind == "v" else is_gravel[Y0:Y1, X0:X1].T
        lo, hi = [], []
        for line in box:
            xs = np.nonzero(line)[0]
            if len(xs) >= 3:
                lo.append(xs.min())
                hi.append(xs.max())
        if len(lo) < len(box) * 0.3:
            continue                     # too little gravel along it to call it a walk
        a, b = int(np.median(lo)), int(np.median(hi))
        for n in range(len(box)):
            wa = a + wobble(n, 1)
            wb = b + wobble(n, 2)
            if kind == "v":
                walk[Y0 + n, X0 + max(wa, 0):X0 + wb + 1] = True
            else:
                walk[Y0 + max(wa, 0):Y0 + wb + 1, X0 + n] = True
    walk |= is_gravel
    walk = ndimage.binary_closing(walk, structure=disc(CLOSE_R), border_value=0)
    inner = ndimage.binary_erosion(walk, structure=disc(EDGE_KEEP), border_value=0)
    # (and the stray blades of grass painted along the old edge: any green, outside the beds)
    def blade(p):
        r, g_, b, a = (int(v) for v in p)
        if a < 200:
            return False
        if g_ > r + 8 and g_ > b + 8:
            return True                  # a green blade
        return r > 80 and g_ > 80 and b < 75 and abs(r - g_) < 35   # its olive shade on the gravel
    is_green = np.array([[blade(p) for p in row] for row in top])
    clear = inner & ~is_gravel & (is_grass | is_bare | is_green) & ~beds
    # and the walk's old edge, now inside it (its shaded gravel and the grass blades in it), laid
    # again with the rest: everything but the outer RELAY_EDGE px, the birdbath's shadow kept
    is_shadow = np.array([[gravel(tuple(p)) and not (int(p[0]) > 140) for p in row] for row in top])
    keep = np.zeros((H, W), bool)                    # (only the birdbath casts one)
    keep[int((41 - y0) * 16):int((44 - y0) * 16), int((14 - x0) * 16):int((17.5 - x0) * 16)] = True
    is_shadow &= keep
    relay = ndimage.binary_erosion(walk, structure=disc(RELAY_EDGE), border_value=0) & is_gravel & ~is_shadow & ~beds
    clear |= relay
    # the statues on their plinths, and the shadows they cast, stay exactly as painted
    for sx0, sy0, sx1, sy1 in STATUES:
        clear[(sy0 - y0) * 16:(sy1 - y0 + 1) * 16, (sx0 - x0) * 16:(sx1 - x0 + 1) * 16] = False

    # each season's gravel, from the walks' inside as that season paints it
    # each season's gravel, the way the painting lays it: one ground colour with sparse lighter
    # and darker grains, read off the walks' inside in that season's art (the commonest colour,
    # and the next few close to it, in their proportions)
    core = ndimage.binary_erosion(is_gravel & ~ndimage.binary_dilation(~is_gravel, structure=disc(2)),
                                  structure=disc(1), border_value=0) & (top_layer == "Back2")
    ys, xs = np.nonzero(core)
    def season_top(season):
        img = np.zeros((H, W, 4), np.uint8)
        for name in LAYERS:
            for ty in range(y1 - y0 + 1):
                for tx in range(x1 - x0 + 1):
                    gid = g[name][y0 + ty][x0 + tx]
                    if first <= gid < first + 10000:
                        r, c = cell(None, gid)
                        t = sheets[season][r:r + 16, c:c + 16]
                        o = t[..., 3] >= 200
                        img[ty * 16:ty * 16 + 16, tx * 16:tx * 16 + 16][o] = t[o]
        return img
    grains = {}
    for season in SEASONS:
        counts = {}
        for c in season_top(season)[ys, xs]:
            counts[tuple(int(v) for v in c)] = counts.get(tuple(int(v) for v in c), 0) + 1
        ranked = sorted(counts.items(), key=lambda kv: -kv[1])
        ground = ranked[0][0]
        near = [(c, n) for c, n in ranked[1:12] if sum(abs(a - b) for a, b in zip(c[:3], ground[:3])) < 70]
        grains[season] = (ground, near)

    def grain(season, wx, wy):
        ground, near = grains[season]
        h = zlib.crc32(f"{wx},{wy}".encode())
        if not near or h % 100 >= GRAIN_PCT:
            return ground
        total = sum(n for _, n in near)
        pick = (h >> 8) % total
        for c, n in near:
            pick -= n
            if pick < 0:
                return c
        return ground

    changed = 0
    cy, cx = np.nonzero(clear)
    for Y, X in zip(cy, cx):
        ty, tx, py, px = Y // 16, X // 16, Y % 16, X % 16
        here = [(name, g[name][y0 + ty][x0 + tx]) for name in LAYERS]
        ours = [(name, gid) for name, gid in here if gid in own]
        base = next(((n, gid) for n, gid in here if first <= gid < first + 10000), None)
        if base is None or base[1] not in own:
            continue                     # no tile of the garden's own under it to paint the gravel on
        if top_layer[Y, X] and g[top_layer[Y, X]][y0 + ty][x0 + tx] not in own:
            continue                     # the grass is on a piece that isn't the garden's own
        for season in SEASONS:
            colour = grain(season, x0 * 16 + X, y0 * 16 + Y)
            sheet = sheets[season]
            # the gravel on the layer the ground shows from here (the top one, if it's gravel
            # already and ours; else the lowest of ours); the grass over it on any layer above
            # taken off (a layer above that isn't ours, or isn't grass, stays)
            target = base
            if relay[Y, X] and top_layer[Y, X]:
                target = (top_layer[Y, X], g[top_layer[Y, X]][y0 + ty][x0 + tx])
            r, c = cell(None, target[1])
            sheet[r + py, c + px] = colour
            for name, gid in ours:
                if name == target[0]:
                    continue
                r, c = cell(None, gid)
                q = tuple(int(v) for v in spring[r + py, c + px])
                if q[3] >= 200 and q[1] > q[0] + 8 and q[1] > q[2] + 8:
                    sheet[r + py, c + px] = (0, 0, 0, 0)
        changed += 1
    for season, p in paths.items():
        Image.fromarray(sheets[season]).save(p)
    os.makedirs(os.path.dirname(DONE), exist_ok=True)
    with open(DONE, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"hash": garden_hash(), "pixels": changed}, f)
        f.write("\n")
    print(f"parterre paths: {changed} px cleared, in {len(own)} tiles of the garden's own")
    return changed


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
