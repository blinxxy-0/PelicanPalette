"""The graveyard in the old Painter's clearing, west of the Vale House (the painters' guide
design, section 6: docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md).

A low stone wall with an iron gate in its north side, seven graves in two rows and a bench.
The old dirt trail, which led nowhere, now runs from the bus road's turning circle to the
gate and ends just inside it. The clearing's ring of trees stays round the outside of the wall.

- The wall (the user's pick W2, art/gilded_vale/props/vale_graveyard_v1): a ring of one-tile
  pieces on Buildings, x 5-13, y 13-23 (RECT): wall_left and wall_right at the corners of the
  north and south runs, wall_mid between, wall_side down the west side and the same piece
  mirrored down the east, so both sides show their dark face toward the inside. The gate is
  two tiles wide (GATE) between two gateposts (1x2: the foot in the ring, the top on Front over
  the row above). It stands open: each leaf swung back into the graveyard against the wall's
  inner face beside its post, so it's drawn over that wall tile, mirrored (the gate's own art,
  each half flipped about its hinge). The gate's two tiles are open ground.
- The graves (GRAVES: id -> the foot tile of its stone). Each is a stone or marker, 1x2, its top
  on Front over the walkable row behind it (you walk behind it) and its foot on Buildings, with
  the tile action `blinxxy.MuseArt_Grave <id>` (ArtStudio/Vale/ValeGraves.cs), and an urn on
  Buildings on the tile at its foot. The back row are the artists, under Bram's carved
  headstones, and Bram himself under the plain stone nobody could carve; the front row, staggered
  between them, are the clay markers of the people who died after him: Ada, who made her own,
  and Hester and Amos, whose markers Tobin made (KIND). Wildflowers grow by Amos's urn ("Something
  nobody planted flowers at its foot").
- The bench (the game's own, vale_decor PROPS["bench"]) faces the graves from beside the gate;
  Lupini stands beside it on Mondays (data/lupini_schedule.json, LUPINI_STAND).
- Inside and on the wall: no trees, bushes or long grass, and none beside the west wall, where
  the woods' crowns hid it (WEST_WOODS) (the clearing's markers that fell there go; vale_decor.py still lays them, so its shared generator draws the same everywhere
  else), and the Back tiles carry NoSpawn All and blinxxy.MuseArt_KeepClear, so nothing spawns
  there and ArtStudio/Vale/ValeGrounds.cs sweeps an older save's trees out.
- The ground (vale_patch.repaint_terrain): the old trail's sand goes to grass, and TRAIL's
  corners are sand: up from the gate to the turning circle, and a ragged end just inside.

Every piece's winter version (snow on it) is its own art; spring's art stands in spring,
summer and fall. The tiles go in the Vale's four seasonal sheets through vale_overlook.own_slots:
added at the sheets' end on the first run, found by their pixels on later ones, so nothing
already on the sheets moves.

Run as: python tools/vale_graveyard.py <GildedVale.tmx> <exports>   (build_vale.VALE_PIECES)
Re-runnable: what this lays is cleared and laid again the same.
"""
import contextlib, io, os, sys
from PIL import Image, ImageOps

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv
from vale_patch import GRAVEYARD_BOX as BOX, context, corner_grid, load, repaint_terrain, save

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, bv.ART, "props", "vale_graveyard_v1")
SEASONS = ("spring", "summer", "fall", "winter")

RECT = (5, 13, 13, 23)               # the wall's ring, x0 y0 x1 y1 (inclusive)
POSTS = (8, 11)                      # the gateposts' columns, in the north run
GATE = ((9, 13), (10, 13))           # the gate's two open tiles
GATE_INSIDE = (10, 14)               # just inside it: ValeGraves.GraveyardGateTile, where the Grove's frame sets you down
GRAVE_ACTION = "blinxxy.MuseArt_Grave"
# id: the foot tile of its stone. The back row (tops on row 17, urns on 19), then the front row
# (tops on 20, urns on 22), one between each two of the back row's, so every stone has open ground
# on both sides to stand on and read it.
GRAVES = {"agnes": (6, 18), "hollis": (8, 18), "mabel": (10, 18), "bram": (12, 18),
          "ada": (7, 21), "hester": (9, 21), "amos": (11, 21)}
KIND = {"agnes": "headstone_agnes", "hollis": "headstone_hollis", "mabel": "headstone_mabel", "bram": "stone_bram",
        "ada": "marker_ada", "hester": "marker_hester", "amos": "marker_amos"}
AMOS_FLOWERS = ((10, 22, 258), (12, 22, 308))     # beside his urn (the game's tufts, on Back2, underfoot)
BENCH = (6, 14)                      # the bench's top-left tile: its back on row 14, its seat on 15, facing the graves
LUPINI_STAND = (9, 15)               # beside the seat, by the gate
LUPINI_FACING = 2                    # looking down over the graves, as the bench does
# The trail's sand corners (corner (x, y) is tile (x, y)'s top-left), by corner row: from the
# turning circle's south-west curb down to the gate, then a ragged end on the landing inside it.
# Its top tiles, (10, 11) and (11, 11), keep grass along their top edge, so the trail starts a
# step from the road (as the old trail did, and Town's trails do from the paving): tile (11, 11)
# lies under road tiles (11, 10) and (12, 11). Sand on a corner the road's tiles share met their
# grass in a square notch.
TRAIL = {12: (10, 11), 13: (9, 10, 11), 14: (9, 10, 11), 15: (10,)}
KEEP_CLEAR = "blinxxy.MuseArt_KeepClear"
# The woods' inner column runs right beside the west wall (x 4): its trees' crowns, 3 tiles wide,
# hid the whole wall in game. Those trees go (the woods behind, x 0-3, still close it in), and the
# column gets NoSpawn Tree, so none grows back and an older save's are swept (ValeGrounds.cs).
WEST_WOODS = [(RECT[0] - 1, y) for y in range(RECT[1], RECT[3] + 1)]
TREE_MARKERS = (9, 10, 11)           # Paths: oak, maple, pine

# piece name -> the art files it's cut from (each with its <name>_winter.png)
SOURCES = {}
for _who, _kind in KIND.items():
    SOURCES[_kind + ".top"] = SOURCES[_kind + ".foot"] = [_kind]
    SOURCES["urn_" + _who] = ["urn_" + _who]
for _w in ("wall_left", "wall_mid", "wall_right", "wall_side"):
    SOURCES[_w] = [_w]
SOURCES["wall_side_east"] = ["wall_side"]            # mirrored: both side runs show their dark face inside
SOURCES["gatepost.top"] = SOURCES["gatepost.foot"] = ["gatepost"]
SOURCES["wall_mid+gate_left"] = SOURCES["wall_mid+gate_right"] = ["wall_mid", "gate"]


def _art(name, season):
    return Image.open(os.path.join(ART, name + ("_winter" if season == "winter" else "") + ".png")).convert("RGBA")


def tiles():
    """{piece name: {season: 16 x 16 image}} for every tile of the graveyard's art."""
    out = {}
    for name in SOURCES:
        out[name] = {}
        for se in SEASONS:
            base, _, part = name.partition(".")
            if "+" in name:
                # an open gate's leaf, swung back against the wall's inner face: the wall, and over
                # it that half of the gate flipped about its hinge (the left leaf hangs on the left
                # post, so it lies west of it; the right one east of the right post)
                im = _art("wall_mid", se)
                gate = _art("gate", se)
                half = gate.crop((0, 0, 16, 16)) if name.endswith("left") else gate.crop((16, 0, 32, 16))
                im.alpha_composite(ImageOps.mirror(half))
            elif name == "wall_side_east":
                im = ImageOps.mirror(_art("wall_side", se))
            elif part:
                im = _art(base, se)
                im = im.crop((0, 0, 16, 16)) if part == "top" else im.crop((0, 16, 16, 32))
            else:
                im = _art(base, se)
            assert im.size == (16, 16), (name, se, im.size)
            out[name][se] = im
    return out


def layout():
    """[(x, y, layer, piece name)] for everything this lays from its own art."""
    x0, y0, x1, y1 = RECT
    cells = []
    for x in range(x0, x1 + 1):
        for y, left, right in ((y0, "wall_left", "wall_right"), (y1, "wall_left", "wall_right")):
            if (x, y) in GATE:
                continue
            if y == y0 and x in POSTS:
                cells += [(x, y, "Buildings", "gatepost.foot"), (x, y - 1, "Front", "gatepost.top")]
            elif y == y0 and x == POSTS[0] - 1:
                cells.append((x, y, "Buildings", "wall_mid+gate_left"))
            elif y == y0 and x == POSTS[1] + 1:
                cells.append((x, y, "Buildings", "wall_mid+gate_right"))
            else:
                cells.append((x, y, "Buildings", left if x == x0 else right if x == x1 else "wall_mid"))
    for y in range(y0 + 1, y1):
        cells += [(x0, y, "Buildings", "wall_side"), (x1, y, "Buildings", "wall_side_east")]
    for who, (x, y) in GRAVES.items():
        cells += [(x, y - 1, "Front", KIND[who] + ".top"), (x, y, "Buildings", KIND[who] + ".foot"),
                  (x, y + 1, "Buildings", "urn_" + who)]
    return cells


def design(V):
    """The box's corners: the old trail's sand goes to grass, then TRAIL's corners are sand."""
    x0, y0, x1, y1 = BOX
    for cy in range(y0 + 1, y1 + 1):            # (the box's inner corners: a corner on its edge is
        for cx in range(x0 + 1, x1 + 1):        # shared with tiles outside it, which aren't repainted)
            if V[cy][cx] == "S":
                V[cy][cx] = "G"
    for cy, xs in TRAIL.items():
        for cx in xs:
            V[cy][cx] = "S"
    return V


def _inside_box(x, y):
    x0, y0, x1, y1 = BOX
    assert x0 <= x <= x1 and y0 <= y <= y1, f"({x}, {y}) is outside the graveyard's box {BOX}"


def patch_map(m, ctx):
    from vale_decor import PROPS
    from vale_overlook import own_slots
    x0, y0, x1, y1 = RECT
    mine = {(x, y) for x, y, _, _ in layout()} | {(BENCH[0] + dx, BENCH[1] + dy) for dx, dy, _, _ in PROPS["bench"]}

    # Clear the ring and everything inside it: markers (the clearing's trees, bushes and long grass
    # that fell here), props and their actions, an earlier run's pieces, and the game's invisible
    # wall (outdoors 103, blank), which the painter's dark-grass stacks carried into open grass
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            _inside_box(x, y)
            for layer in ("Paths", "Front", "Front2", "AlwaysFront", "Buildings2"):
                m.g[layer][y][x] = 0
            b = m.g["Buildings"][y][x]
            if b == 103 or (b and (m.g["Back"][y][x], b) not in bv.GROUND):
                m.g["Buildings"][y][x] = 0
            m.props.pop(("Buildings", x, y), None)
            m.props.pop(("Paths", x, y), None)
    for x, y in WEST_WOODS:
        _inside_box(x, y)
        if m.g["Paths"][y][x] - ctx.paths_first in TREE_MARKERS:
            m.g["Paths"][y][x] = 0
        m.props.setdefault(("Back", x, y), {}).update({"NoSpawn": "Tree"})
    for x, y in mine:
        m.g["Back2"][y][x] = 0              # (no flower tufts under a stone, an urn, the wall or the bench)
    for x in POSTS:
        m.g["Front"][y0 - 1][x] = 0          # (the posts' tops)

    # The ground: the old trail goes, the new one runs from the road to the gate
    with contextlib.redirect_stdout(io.StringIO()):
        V = design(corner_grid(m.g, bv.GROUND))
        repaint_terrain(m, V, BOX, ctx)
    # (repaint_terrain puts back a ground stack it finds, and the dark grass's (404, 103) carries the
    # invisible wall: so it's cleared again here, inside the wall, after the repaint)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if m.g["Buildings"][y][x] == 103:
                m.g["Buildings"][y][x] = 0

    # The wall, the gate, the graves and their urns, from the Vale's seasonal sheets
    cells = layout()
    art = tiles()
    names = sorted({name for _, _, _, name in cells})
    gids = dict(zip(names, own_slots(m, ctx, [art[n] for n in names])))
    for x, y, layer, name in cells:
        _inside_box(x, y)
        m.g[layer][y][x] = gids[name]
    for who, (x, y) in GRAVES.items():
        m.props[("Buildings", x, y)] = {"Action": f"{GRAVE_ACTION} {who}"}
    bx, by = BENCH
    for dx, dy, layer, gid in PROPS["bench"]:
        _inside_box(bx + dx, by + dy)
        m.g[layer][by + dy][bx + dx] = gid
    for x, y, gid in AMOS_FLOWERS:
        m.g["Back2"][y][x] = gid

    # Nothing grows or spawns in it, and an older save's trees there are swept away
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            m.props.setdefault(("Back", x, y), {}).update({"NoSpawn": "All", KEEP_CLEAR: "T"})


def patch_file(tmx_path, exports):
    with contextlib.redirect_stdout(io.StringIO()):
        ctx = context(tmx_path, exports)
    m = load(tmx_path)
    patch_map(m, ctx)
    save(m, tmx_path)
    print("graveyard: wrote", tmx_path)


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2])
