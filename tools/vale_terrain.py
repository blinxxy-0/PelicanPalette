"""Learns the game's own ground (grass, dirt, water, and the tiles that join
them) from vanilla outdoor maps, so the Gilded Vale is painted like the game's
maps instead of from pasted chunks (the user, 2026-11-04: "the map looks very
choppy").

What is learned is a STACK per map cell: its Back tile and its Buildings
overlay. The game builds shorelines in layers (water on Back, sand edges laid
over it on Buildings, pieces from both outdoor sheets), so a Back tile alone
doesn't say what a cell looks like. Gids are put into the Town map's tileset
numbering, so a learned stack can be written straight into a map that uses
Town's tilesets.

Corners: each corner of a cell is light grass (G), dark grass (D), dirt or
sand (S) or water (W). Plain stacks (one Back tile all of one terrain by its
colour, nothing on Buildings) are known outright; every other stack's corners
are followed out from them, since neighbouring cells share the corners along
their common edge. Painting is then corner (Wang) autotiling: set terrain on
the grid's corners and lay, in each cell, a stack whose four corners match,
picked by how often the game uses it.

As a module: Table(exports dir). As a script: prints the coverage.
Usage: python tools/vale_terrain.py <vanilla exports dir>
"""
import collections, os, sys
import xml.etree.ElementTree as ET
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_tmx import layer_data

MAPS = ["Town", "Forest", "Mountain", "Woods", "Backwoods", "Railroad"]
# Town's tilesets, by image, as build_vale.py writes them: (first gid, columns, tile count).
TOWN_SHEETS = {"spring_outdoorsTileSheet": (1, 25, 1975), "paths": (1976, 4, 64), "spring_town": (2040, 32, 2304),
               "spring_outdoorsTileSheet2": (4344, 16, 1120), "spring_Shadows": (5464, 19, 475),
               "spring_Waterfalls": (5939, 36, 900)}
GROUND_SHEETS = ("spring_outdoorsTileSheet", "spring_outdoorsTileSheet2")
# Pelican Town's cobbled paving (the square and its lanes), on the town sheet: its plain
# tiles are cobble (C), so its curbed edges and corners are learned like any other edge.
# (the user, 2026-11-06: dirt paths everywhere are "kinda jank"; the village gets paving.)
COBBLE = {2776, 2777, 2808, 2809, 2681, 2686, 2810, 3035, 3036}
LAYERS = ("Back", "Buildings")


def corner_class(rgb):
    r, g, b = rgb
    if b > r + 10 and b >= g - 6:
        return "W"
    if r > g + 12 and r > b + 40:
        return "S"
    if g >= r:
        return "G" if 0.3 * r + 0.59 * g + 0.11 * b > 118 else "D"
    return "?"


def pure_tiles(exports):
    """Town gid -> class, for ground tiles all of one terrain (four corners and centre agree),
    and the town sheet's plain cobble."""
    out = {}
    for sheet in GROUND_SHEETS:
        first, cols, _ = TOWN_SHEETS[sheet]
        im = Image.open(os.path.join(exports, sheet + ".png")).convert("RGBA")
        for ty in range(im.height // 16):
            for tx in range(cols):
                px = im.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16)).load()
                if any(px[x, y][3] < 255 for x in range(16) for y in range(16)):
                    continue
                cls = set()
                for cx, cy in ((0, 0), (12, 0), (0, 12), (12, 12), (6, 6)):
                    ps = [px[x, y][:3] for x in range(cx, cx + 4) for y in range(cy, cy + 4)]
                    cls.add(corner_class(tuple(sum(c[i] for c in ps) // len(ps) for i in range(3))))
                if len(cls) == 1 and "?" not in cls:
                    out[first + ty * cols + tx] = cls.pop()
    out.update((gid, "C") for gid in COBBLE)
    return out


def load_grids(exports):
    """Each vanilla map as (w, h, cells): a cell is (Back, Buildings) in Town gids, or
    None where a tile is on a sheet Town doesn't have."""
    grids = []
    for name in MAPS:
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
        data = {n: layer_data(layers[n].find("data")) if n in layers else [0] * (w * h) for n in LAYERS}
        cells = []
        for i in range(w * h):
            stack = tuple(to_town(data[n][i]) for n in LAYERS)
            # Shadow-sheet pieces belong to the buildings and trees that cast them; out of
            # place they show as grey smudges (the user, 2026-11-04).
            shadow = any(5464 <= v < 5939 for v in stack if v)
            cells.append(None if None in stack or stack[0] == 0 or shadow else stack)
        grids.append((w, h, cells))
    return grids


# For a neighbour at (dx, dy): which of its corners (TL 0, TR 1, BL 2, BR 3) is which of ours.
SHARED = {(-1, 0): ((1, 0), (3, 2)), (1, 0): ((0, 1), (2, 3)), (0, -1): ((2, 0), (3, 1)), (0, 1): ((0, 2), (1, 3)),
          (-1, -1): ((3, 0),), (1, -1): ((2, 1),), (-1, 1): ((1, 2),), (1, 1): ((0, 3),)}


class Table:
    def __init__(self, exports, passes=6):
        pure = pure_tiles(exports)
        grids = load_grids(exports)
        self.use = collections.Counter(c for _, _, cells in grids for c in cells if c)
        known = {}
        for stack in self.use:
            if stack[1] == 0 and stack[0] in pure:
                known[stack] = (pure[stack[0]],) * 4
        fixed = set(known)
        for _ in range(passes):
            votes = collections.defaultdict(lambda: [collections.Counter() for _ in range(4)])
            for w, h, cells in grids:
                for y in range(h):
                    for x in range(w):
                        t = cells[y * w + x]
                        if t is None or t in fixed:
                            continue
                        for (dx, dy), pairs in SHARED.items():
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < w and 0 <= ny < h and cells[ny * w + nx] in known:
                                n = known[cells[ny * w + nx]]
                                for theirs, ours in pairs:
                                    votes[t][ours][n[theirs]] += 1
            for t, vs in votes.items():
                if all(vs[k] for k in range(4)):
                    known[t] = tuple(vs[k].most_common(1)[0][0] for k in range(4))
        self.corners = known
        # Which stacks the game puts next to which: a corner pattern can have both a
        # square-notch piece and a curved or diagonal one, and only the neighbours tell
        # which belongs (the user, 2026-11-04: corners "flat before the jagged edges").
        self.right = collections.Counter()      # (a, b): b directly right of a
        self.down = collections.Counter()       # (a, b): b directly below a
        for w, h, cells in grids:
            for y in range(h):
                for x in range(w):
                    a = cells[y * w + x]
                    if a is None:
                        continue
                    if x + 1 < w and cells[y * w + x + 1] is not None:
                        self.right[(a, cells[y * w + x + 1])] += 1
                    if y + 1 < h and cells[(y + 1) * w + x] is not None:
                        self.down[(a, cells[(y + 1) * w + x])] += 1
        self.build_contexts(grids)
        self.by_pattern = collections.defaultdict(list)
        for stack, pat in known.items():
            self.by_pattern[pat].append((stack, self.use[stack]))
        for v in self.by_pattern.values():
            v.sort(key=lambda t: -t[1])

    # Context: the terrain on the 4 x 4 grid corners round a cell (its own 2 x 2 in the
    # middle), so a piece is chosen for its neighbourhood, not its four corners alone:
    # a straight bank, a step in a diagonal and a corner have the same four corners but
    # different pieces in the game (the user, 2026-11-04: banks "sharktoothing").
    INNER = ((1, 1), (2, 1), (1, 2), (2, 2))
    SIDES = ((1, 0), (2, 0), (0, 1), (3, 1), (0, 2), (3, 2), (1, 3), (2, 3))
    CORNERS = ((0, 0), (3, 0), (0, 3), (3, 3))
    LEVELS = (INNER + SIDES + CORNERS, INNER + SIDES, INNER)

    def build_contexts(self, grids):
        """For every vanilla cell near water: its context at each level of detail ->
        how often each stack is used there."""
        self.ctx = [collections.defaultdict(collections.Counter) for _ in self.LEVELS]
        for w, h, cells in grids:
            # each grid corner's terrain: the vote of the (up to) four cells round it
            vert = [[None] * (w + 1) for _ in range(h + 1)]
            for vy in range(h + 1):
                for vx in range(w + 1):
                    v = collections.Counter()
                    for cx, cy, k in ((vx - 1, vy - 1, 3), (vx, vy - 1, 2), (vx - 1, vy, 1), (vx, vy, 0)):
                        if 0 <= cx < w and 0 <= cy < h:
                            p = self.corners.get(cells[cy * w + cx])
                            if p:
                                v[p[k]] += 1
                    if v:
                        vert[vy][vx] = v.most_common(1)[0][0]
            for y in range(h):
                for x in range(w):
                    c = cells[y * w + x]
                    if c is None:
                        continue
                    win = {(i, j): vert[min(h, max(0, y - 1 + j))][min(w, max(0, x - 1 + i))]
                           for i in range(4) for j in range(4)}
                    if "W" not in win.values():
                        continue
                    for lvl, keys in enumerate(self.LEVELS):
                        key = tuple(win[k] for k in keys)
                        if None not in key:
                            self.ctx[lvl][key][c] += 1

    def by_context(self, win):
        """The stacks the game uses in this neighbourhood (win: (i, j) -> terrain over
        the 4 x 4 grid corners), at the most detailed level it has any for."""
        for lvl, keys in enumerate(self.LEVELS):
            found = self.ctx[lvl].get(tuple(win[k] for k in keys))
            if found:
                return found, lvl
        return None, None

    def pick(self, pattern, rnd, top=None, min_share=0.0):
        """A stack for a corner pattern, weighted by how often the game uses it.
        min_share drops stacks used less than that share of the commonest."""
        cands = self.by_pattern.get(pattern)
        if not cands:
            return None
        if min_share:
            cands = [c for c in cands if c[1] >= cands[0][1] * min_share]
        if top:
            cands = cands[:top]
        total = sum(w for _, w in cands)
        r = rnd.random() * total
        for stack, w in cands:
            r -= w
            if r <= 0:
                return stack
        return cands[-1][0]


def main():
    t = Table(sys.argv[1])
    print(len(t.corners), "stacks with corners")
    for pair in ("GS", "GD", "SW", "GW"):
        a, b = pair
        have, missing = 0, []
        for n in range(16):
            pat = tuple(a if n >> k & 1 == 0 else b for k in range(4))
            if t.by_pattern.get(pat):
                have += 1
            else:
                missing.append("".join(pat))
        print(pair, have, "/ 16, missing", missing)
    for pat in ("SSSS", "WWWW", "WWSS", "SSWW", "WSWS", "GGWW", "WWGG"):
        print(pat, t.by_pattern.get(tuple(pat), [])[:4])


if __name__ == "__main__":
    main()
