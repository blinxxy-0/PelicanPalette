"""Patches the committed Gilded Vale map in place, for the pieces added after
build_vale.py stopped reproducing it exactly (its rebuild doesn't give the committed
map back, 2026-11-08): the waterfall, the overlook and the hidden sculpture.

Shared by tools/vale_falls.py, vale_overlook.py and vale_sculpture.py:
- load() / save(): the map's tile layers, TileData objects and map properties, written
  back as vale_yards._patch writes them, so loading and saving changes nothing.
- context(): the vanilla ground table and Town's tiles, as build_vale.paint_ground has them.
- corner_grid(): the terrain on the corners of the tile grid, read back from the tiles.
- repaint_terrain(): the painter's shore, bank and depth rules over one box of the map.
- BOXES: the only places a piece may change (inclusive tile rectangles, x0 y0 x1 y1).
"""
import collections, contextlib, copy, io, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv

FALLS_BOX = (74, 0, 89, 13)          # the rim cliff, the fall and the basin
OVERLOOK_BOX = (80, 54, 95, 75)      # the bluff, its steps, the lake's east shore below it
SCULPTURE_BOX = (6, 43, 7, 45)       # the corner pocket (x 6-7, y 44-45) and the tile above it
BOXES = {"falls": FALLS_BOX, "overlook": OVERLOOK_BOX, "sculpture": SCULPTURE_BOX}


class ValeMap:
    def __init__(self, text, g, props, map_props):
        self.text = text                 # the file as loaded
        self.g = g                       # layer -> rows of gids
        self.props = props               # (layer, x, y) -> TileData properties
        self.map_props = map_props       # the map's own properties


def _layer(text, name):
    return re.search(r'(<layer id="\d+" name="' + name + r'"[^>]*>\s*<data encoding="csv">\n)(.*?)(\n\s*</data>)', text, re.S)


def _layer_names(text):
    return re.findall(r'<layer id="\d+" name="([^"]+)"', text)


def _map_props(text):
    return re.search(r'(<map [^>]*>\s*<properties>\n)(.*?)(\n  </properties>)', text, re.S)


def load(tmx_path):
    text = open(tmx_path, encoding="utf-8").read()
    g = {name: [list(map(int, row.rstrip(",").split(","))) for row in _layer(text, name).group(2).split("\n")]
         for name in _layer_names(text)}
    props = {}
    for layer, body in re.findall(r'<objectgroup id="\d+" name="([^"]+)" visible="false">(.*?)</objectgroup>', text, re.S):
        for ox, oy, p in re.findall(r'<object id="\d+" name="TileData" x="(\d+)" y="(\d+)"[^>]*>\s*<properties>(.*?)</properties>', body, re.S):
            props[(layer, int(ox) // 16, int(oy) // 16)] = dict(re.findall(r'<property name="([^"]+)" type="string" value="([^"]*)" />', p))
    map_props = dict(re.findall(r'<property name="([^"]+)" type="string" value="([^"]*)" />', _map_props(text).group(2)))
    return ValeMap(text, g, props, map_props)


def save(m, tmx_path):
    text = m.text
    layer_names = _layer_names(text)
    for name in reversed(layer_names):     # last layer first, so the earlier offsets hold
        mt = _layer(text, name)
        csv = ",\n".join(",".join(str(v) for v in row) for row in m.g[name])
        text = text[:mt.start(2)] + csv + text[mt.end(2):]
    mp = _map_props(text)
    body = "\n".join(f'    <property name="{k}" type="string" value="{v}" />' for k, v in m.map_props.items())
    text = text[:mp.start(2)] + body + text[mp.end(2):]
    # the object groups, rewritten as build_vale.py write() writes them
    out, oid = [], 1
    for i, layer in enumerate(layer_names):
        props = sorted((x, y, p) for (l, x, y), p in m.props.items() if l == layer)
        if not props:
            continue
        out.append(f'  <objectgroup id="{len(layer_names) + 1 + i}" name="{layer}" visible="false">')
        for x, y, p in props:
            out.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            out.append('      <properties>')
            out += [f'        <property name="{k}" type="string" value="{v}" />' for k, v in p.items()]
            out.append('      </properties>')
            out.append('    </object>')
            oid += 1
        out.append('  </objectgroup>')
    start = text.find("  <objectgroup")
    end = text.rfind("</map>")
    text = (text[:start] if start >= 0 else text[:end]) + "\n".join(out) + "\n" + text[end:]
    open(tmx_path, "w", encoding="utf-8", newline="\n").write(text)


class Ctx:
    def __init__(self, exports, assets, table, town, paths_first):
        self.exports, self.assets, self.table, self.paths_first = exports, assets, table, paths_first
        self._town = town

    def tget(self, layer, x, y):
        """A tile of the vanilla Town map (the exports' Town.tmx)."""
        return self._town[layer][y][x] if layer in self._town else 0


def context(tmx_path, exports):
    """What the painter needs: the vanilla ground table, with build_vale.GROUND loaded
    exactly as paint_ground loads it, and Town's tiles."""
    from vale_terrain import Table
    table = Table(exports)
    bv.GROUND.update(table.corners)
    bv.GROUND.update({stack: pat for pat, stack in bv.COBBLE_PIN.items()})
    town, _, paths_first = bv.load_town(os.path.join(exports, "Town.tmx"))
    return Ctx(exports, os.path.dirname(os.path.abspath(tmx_path)), table, town, paths_first)


def corner_grid(g, ground):
    """The (H+1) x (W+1) terrain on the grid's corners, from each tile's (Back, Buildings)
    stack: a corner takes the vote of the tiles round it. A stack the table doesn't know
    (a prop, a building, a cliff piece) says nothing; its corners come from the known
    neighbours, a ring at a time. Grass is where nothing is known at all."""
    h, w = len(g["Back"]), len(g["Back"][0])
    votes = [[collections.Counter() for _ in range(w + 1)] for _ in range(h + 1)]
    for y in range(h):
        for x in range(w):
            pat = ground.get((g["Back"][y][x], g["Buildings"][y][x]))
            if pat:
                for (cx, cy), t in zip(((x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)), pat):
                    votes[cy][cx][t] += 1
    V = [[v.most_common(1)[0][0] if v else None for v in row] for row in votes]
    while any(t is None for row in V for t in row):
        fill = {}
        for y in range(h + 1):
            for x in range(w + 1):
                if V[y][x] is None:
                    near = collections.Counter(V[y + dy][x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                                               if 0 <= x + dx <= w and 0 <= y + dy <= h and V[y + dy][x + dx])
                    if near:
                        fill[(x, y)] = near.most_common(1)[0][0]
        if not fill:
            fill = {(x, y): "G" for y in range(h + 1) for x in range(w + 1) if V[y][x] is None}
        for (x, y), t in fill.items():
            V[y][x] = t
    return V


def repaint_terrain(m, V, box, ctx):
    """Lays the ground for corners V inside box (x0, y0, x1, y1 inclusive), by the painter's
    own rules: each tile whose corners changed gets the game's main stack for them, then
    build_vale's paint_shores, vertical_banks and bank_depth run over a copy of the whole
    grid (so a bank's neighbours are what they are in the map) and only the box is copied
    back. Back and Buildings of a tile are written back only where the old stack is
    ground, or the corners make it shore or water; props and decor are never overwritten."""
    x0, y0, x1, y1 = box
    ground = bv.GROUND
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    work = copy.deepcopy(m.g)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            old = ground.get((work["Back"][y][x], work["Buildings"][y][x]))
            if old is None or old == pat(x, y):
                continue
            stack = bv.SHORE_PIN.get(pat(x, y)) or bv.COBBLE_PIN.get(pat(x, y))
            if stack is None:
                found = ctx.table.by_pattern.get(pat(x, y), [])
                bare = [c for c in found if c[0][1] == 0]
                stack = (bare or found or [(None, 0)])[0][0]
            if stack:
                work["Back"][y][x], work["Buildings"][y][x] = stack
    with contextlib.redirect_stdout(io.StringIO()):       # (paint_shores prints its stats)
        bv.paint_shores(work, V, ctx.table)
        bv.vertical_banks(work, V, ctx.table)
    bv.bank_depth(work)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            old = (m.g["Back"][y][x], m.g["Buildings"][y][x])
            if old in ground or "W" in pat(x, y):
                m.g["Back"][y][x], m.g["Buildings"][y][x] = work["Back"][y][x], work["Buildings"][y][x]
