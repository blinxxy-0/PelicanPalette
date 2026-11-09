"""The overlook: a raised bluff, one level high, in the Vale's south-east corner.
(docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md, section 2)

For now, only how its edges are drawn (Task 4), learned from the vanilla plateaus
(read off their stacks, 2026-11-08; docs/terrain_painting.md, "A raised bluff"):

- The camera only sees faces that look south. Everywhere else a raised piece of ground
  is drawn as a lit lip on its own edge tile: the edge tile's outer part is see-through
  (the lower ground shows), its inner part is the plateau's grass, and a yellow-lit
  line runs between. No rock shows on a north or a side edge, however high it is.
  - North rim (lower ground north): 414 and 415, alternating (the Mountain quarry's
    south rim, Mountain x 112-129 y 35; Railroad x 35-51 y 11; Forest x 113-119 y 54).
  - West side (lower ground west): 392 and 417, alternating, 392 first under the
    corner, 417 last (Forest x 95 y 60-66; Railroad x 68 y 12-28; Town x 22 y 41-42).
    They carry a 1-2 px cast shadow down their left edge, as the face's west end below
    them carries a wider one; both are laid from de-shadowed copies (unbanded), so the
    side doesn't end its shadow in a step where it meets the end, and no grey line runs
    down it on winter snow.
  - Their outer corner: 440 (Town (22,40); Forest (105,58), (96,58); Mountain (101,6)).
  - Where a west side meets a south face: 442 on the face's lip row, then the face's
    rounded west end, 446 / 492 / 517 / 542 (Town x 22 y 43-47, Forest x 95 y 67-71,
    Railroad x 68 y 29-31). It's the Town stairs' right wall, so the de-shadowed
    copies vale_falls made of it are used (their cast shadow is meant for stair treads).
    Beside it the shadow under the face fades (404 beside the painter's 376, as at
    Town (23-24,48)) instead of ending square.
  Town's little raised lawn east of its stairs (x 22-24, y 40-47) has all of it in eight
  tiles: corner, rim, side, the turn into the face, the face's end.
- No vanilla flight climbs south. Every flight onto higher ground climbs a south face
  (Railroad x 51-56 y 14-19, Mountain x 2-6 y 15-19 and x 8-11 y 0-2, Town x 19-22,
  x 73-76 and x 105-108, Forest x 4-7 y 30-32, Backwoods x 12-16 y 31-37): the steps'
  risers have to face the camera. So the bluff's steps go in a south face, by our cliff
  rule, one level high, with the Vale's own stairs art (build_vale.STAIRS_PICK, fitted
  to exactly that face). The flight is copied from the one already laid on the lower
  terrace (build_vale.STAIR_SPOTS[3]). (A flight climbing south through the rim would
  need new art: PixelLab, with nothing in the game to copy.)

The pieces go on Buildings: the bluff's top is walked on, so its edges are walls (the
strip checks: the top is reached only by the flight). The ground under each edge is
painted first by the painter's rules (vale_patch.repaint_terrain): the lower ground's
terrain on the outer corners, grass on the plateau's, dark grass under the face's shadow.
The corners are read off the map once and kept on it (m.overlook_V), and every tile an
edge is laid on is left alone by later painting (m.overlook_laid): the table counts many
rim, face and shading stacks as plain ground and would repaint them.

The de-shadowed copies (unbanded) go in the Vale's four seasonal sheets; the first run
adds them in a new row, later runs find them by their pixels.

  lay_lip(m, ctx, x0, x1, y)    the north rim on row y; x0 is the north-west corner
  lay_side(m, ctx, x, y0, y1)   the west side down column x
  lay_face(m, ctx, x0, x1, top) a south face by the cliff rule, its west end at x0
  lay_steps(m, ctx, x, y)       the flight up a face whose top row is y, left column x

python tools/vale_overlook.py --strip <scratch GildedVale.tmx> <exports>
  lays the test strip at the bluff's north-west corner on a scratch copy (never the
  committed map).
"""
import os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv
from build_vale import CLIFF_FACE, CLIFF_FACE_VARIANT, CLIFF_LIP, CLIFF_SHADE, GRASS, STAIR_SPOTS
import vale_falls as vf
from vale_patch import OVERLOOK_BOX, context, corner_grid, load, repaint_terrain, save

RIM_NW = 440                 # the north-west outer corner, on the rim's row
RIM = (414, 415)             # the north rim, alternating from the corner: 414 first
SIDE = (392, 417)            # the west side, alternating from under the corner: 392 first
WEST_END = (22, 43)          # Town x 22 from row 43: 442 (lip row) 446 492 517 542 (face rows)
END_SHADE = 404              # the shadow's last row beside the face's west end
FLIGHT_FROM = STAIR_SPOTS[3]  # the lower terrace's east flight (84, 48): its tiles are copied
SEED = 85


def _clip(box):
    x0, y0, x1, y1 = box
    bx0, by0, bx1, by1 = OVERLOOK_BOX
    return max(x0, bx0), max(y0, by0), min(x1, bx1), min(y1, by1)


def _corners(m):
    """The overlook's working corners: read off the map's tiles once, before any edge is
    laid, then kept on the map object and changed by each edge in turn. (Read again
    later, the edges' own stacks would vote: the game's maps hold rim, face and shading
    pieces over grass, so the table counts many of them as ground, the shading as dark
    grass.) Call the edges on one loaded map, without other changes in between."""
    if not hasattr(m, "overlook_V"):
        m.overlook_V = corner_grid(m.g, bv.GROUND)
        m.overlook_laid = set()
    return m.overlook_V


def _paint(m, ctx, corners, box):
    """Sets corners {(cx, cy): class} and lays the ground for them in box (clipped to
    the overlook's box) by the painter's rules: each changed ground tile gets the
    painter's own stack (vale_falls.main_stack), then repaint_terrain's shores and banks.
    The edges already laid (m.overlook_laid) and props are left as they are. A corner
    pattern the game has no plain piece for (a lone corner, a checkerboard) is an error:
    the table's only answer would be a fence or a crate."""
    V = _corners(m)
    for (cx, cy), t in corners.items():
        V[cy][cx] = t
    box = _clip(box)
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    x0, y0, x1, y1 = box
    keep = {(x, y): (m.g["Back"][y][x], m.g["Buildings"][y][x]) for x, y in m.overlook_laid}
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if (x, y) in keep:
                continue
            old = bv.GROUND.get((m.g["Back"][y][x], m.g["Buildings"][y][x]))
            if old is not None and old != pat(x, y):
                stack = vf.main_stack(pat(x, y), ctx.table)
                if stack is None or ("W" not in pat(x, y) and stack[1]):
                    raise ValueError(f"no plain piece for corners {pat(x, y)} at {(x, y)}")
                m.g["Back"][y][x], m.g["Buildings"][y][x] = stack
    repaint_terrain(m, V, box, ctx)
    for (x, y), (b, bd) in keep.items():
        m.g["Back"][y][x], m.g["Buildings"][y][x] = b, bd
    return V


def _laid(m, x, y):
    _corners(m)
    m.overlook_laid.add((x, y))


def _clear(m, x, y):
    """A tile an edge goes on: nothing over it or planted on it; later ground painting
    leaves it alone."""
    _laid(m, x, y)
    for layer in ("Back2", "Paths", "Front", "Front2", "AlwaysFront", "Buildings2"):
        m.g[layer][y][x] = 0


def lay_lip(m, ctx, x0, x1, y):
    """The bluff's north edge, toward the lake, on row y from x0 (its north-west corner)
    to x1. The rim tile's top is see-through: the lower ground under it is the painter's;
    the plateau's grass starts on the corners below it."""
    _paint(m, ctx, {(cx, y + 1): "G" for cx in range(x0 + 1, x1 + 2)}, (x0 - 1, y - 1, x1 + 1, y + 1))
    for x in range(x0, x1 + 1):
        _clear(m, x, y)
        m.g["Buildings"][y][x] = RIM_NW if x == x0 else RIM[(x - x0 - 1) % 2]


def lay_side(m, ctx, x, y0, y1):
    """The bluff's west side, down column x from y0 (under the corner) to y1. The side
    tile's left edge is see-through: the lower ground under it is the painter's; the
    plateau's grass starts on the corners right of it."""
    _paint(m, ctx, {(x + 1, cy): "G" for cy in range(y0, y1 + 2)}, (x - 1, y0 - 1, x + 1, y1 + 1))
    side = side_pieces(m, ctx)
    for y in range(y0, y1 + 1):
        _clear(m, x, y)
        m.g["Buildings"][y][x] = side[(y - y0) % 2]


def unbanded(m, ctx, gids):
    """The game's own pieces (outdoors sheet gids) without the soft cast shadow down
    their left edge (vale_falls.unband), as slots in the Vale's four seasonal sheets:
    vanilla gid -> our gid. A copy already in the sheets (the same pixels in every
    season: vale_falls's west-wall copies of 442-542, or this module's from an earlier
    run) is reused; others go in a new row at the sheets' end."""
    from PIL import Image
    from vale_sculpture import Sheets, re_tileset
    from vale_feature_fixes import FEATURE_FIRST, SEASONS
    sheets = Sheets(ctx.assets)
    src = {se: Image.open(os.path.join(ctx.exports, "sheets_" + se, "spring_outdoorsTileSheet.png")).convert("RGBA")
           for se in SEASONS}
    cols = src["spring"].width // 16

    def piece(se, g):
        k = g - 1                                       # the outdoors sheet is first, at gid 1
        return vf.unband(src[se].crop((k % cols * 16, k // cols * 16, k % cols * 16 + 16, k // cols * 16 + 16)))

    index = {}
    for i in range(sheets.end):
        index.setdefault(sheets.im["spring"].crop(sheets.box(FEATURE_FIRST + i)).tobytes(), []).append(i)
    out, new = {}, []
    for g in gids:
        want = {se: piece(se, g) for se in SEASONS}
        slot = next((i for i in index.get(want["spring"].tobytes(), [])
                     if all(sheets.im[se].crop(sheets.box(FEATURE_FIRST + i)).tobytes() == want[se].tobytes()
                            for se in SEASONS)), None)
        if slot is None:
            slot = sheets.end + len(new)
            new.append((slot, want))
        out[g] = FEATURE_FIRST + slot
    if new:
        sheets.grow(max(slot for slot, _ in new) + 1)
        for slot, want in new:
            for se in SEASONS:
                sheets.im[se].paste(want[se], sheets.box(FEATURE_FIRST + slot)[:2])
        for se in SEASONS:
            sheets.im[se].save(sheets.path(se))
        h = sheets.im["spring"].height
        m.text = re_tileset(m.text, h // 16 * sheets.cols, h)
    return out


def side_pieces(m, ctx):
    """The west side's two pieces, de-shadowed (unbanded): the face's west end below
    them is laid from vale_falls's de-shadowed copies, and a band down the side that
    stopped there would end in a step."""
    u = unbanded(m, ctx, SIDE)
    return tuple(u[g] for g in SIDE)


def west_end_pieces(m, ctx):
    """The face's west end, Town x 22 rows 43-47, by row (0 the lip row), de-shadowed:
    in Town their cast shadow falls on the stair treads; on open ground it read as a
    flat bar (the falls' review, 2026-11-08)."""
    town = [ctx.tget("Buildings", WEST_END[0], WEST_END[1] + r) for r in range(5)]
    u = unbanded(m, ctx, town)
    return {r: u[g] for r, g in enumerate(town)}


def lay_face(m, ctx, x0, x1, top, stairs_x=None):
    """A south face one level high (the terraces' cliff rule, build_vale.cliff) on rows
    top..top+4 from x0 to x1, its shading on top+3..top+5. x0 is the west side's column:
    the side turns into the face there with the face's rounded west end. stairs_x: a
    flight's left column (lay_steps), so the lip beside it is plain and the last shading
    row under its treads is left for the path."""
    rnd = random.Random(SEED * 1000 + top * 100 + x0)
    # The shading under the face is dark grass to the painter (the table reads 327 as
    # G G / D D and 356 and 406 as all D), so its corners are D: then where the face
    # ends, the painter's own dark-grass edge pieces end the shadow, under and just below
    # the end column, as the game ends it (Forest x 93-95 y 71-72), not a cut straight down.
    corners = {(cx, cy): "D" if cy in (top + 4, top + 5) else "G"
               for cx in range(x0 + 1, x1 + 2) for cy in range(top, top + 7)}
    corners.update({(x0, cy): "G" for cy in range(top, top + 7)})
    V = _paint(m, ctx, corners, (x0 - 1, top - 1, x1 + 1, top + 6))
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    end = west_end_pieces(m, ctx)
    for x in range(x0, x1 + 1):
        for y in range(top, top + 5):
            _clear(m, x, y)
        if x == x0:
            for r in range(5):
                m.g["Buildings"][top + r][x] = end[r]
            continue        # its open west edge: the painter's ground stays under it
        beside = stairs_x is not None and stairs_x - 2 <= x <= stairs_x + 5
        m.g["Buildings"][top][x] = CLIFF_LIP[0][0] if beside or rnd.random() < CLIFF_LIP[0][1] else CLIFF_LIP[1][0]
        m.g["Back"][top][x] = GRASS          # grass behind the edge, never shore
        for k, gid in enumerate(CLIFF_FACE[x % 2]):
            if gid in CLIFF_FACE_VARIANT and rnd.random() < 0.08:
                gid = CLIFF_FACE_VARIANT[gid]
            m.g["Buildings"][top + 1 + k][x] = gid
        m.g["Back"][top + 1][x] = m.g["Back"][top + 2][x] = GRASS
        for k, opts in enumerate(CLIFF_SHADE):
            y = top + 3 + k
            if k == 2 and stairs_x is not None and stairs_x + 1 <= x <= stairs_x + 2:
                continue                      # under the flight's last steps
            if set(pat(x, y)) <= set("GD"):
                m.g["Back"][y][x] = opts[0]   # the main piece only: its variants go plain white in winter
                if y == top + 5 and x == x0 + 1:
                    # beside the west end the shadow fades (Town (24,48): 404 beside the
                    # painter's 376); a full 406 there ended in a hard edge on winter snow
                    m.g["Back"][y][x] = END_SHADE
                _clear(m, x, y) if y >= top + 5 else _laid(m, x, y)


def lay_steps(m, ctx, x, y):
    """The flight up a south face (lay_face) whose top row is y, columns x..x+3: the
    lower terrace's flight copied tile for tile (treads on Back2, walkable; side walls on
    Buildings2, over the face, which stays solid under them), with a sand landing above
    its ragged top step and a sand apron below its foot, so neither shows a grass line."""
    fx, ft = FLIGHT_FROM
    corners = {(cx, cy): "S" for cx in range(x + 1, x + 4) for cy in (y - 1, y)}
    corners.update({(cx, cy): "S" for cx in range(x, x + 4) for cy in range(y + 6, y + 9)})
    _paint(m, ctx, corners, (x - 1, y - 3, x + 4, y + 8))
    for j in range(-1, 8):
        for i in range(4):
            sy, ty = ft + j, y + j
            if i in (1, 2):
                if -1 <= j <= 6:
                    m.g["Buildings"][ty][x + i] = m.g["Front"][ty][x + i] = 0
                m.g["Back2"][ty][x + i] = m.g["Back2"][sy][fx + i]
            else:
                m.g["Buildings2"][ty][x + i] = m.g["Buildings2"][sy][fx + i]
    for j in range(6):                        # the rows with the flight's art
        for i in range(4):
            _laid(m, x + i, y + j)
    for i in (1, 2):
        m.g["Back"][y][x + i] = bv.DIRT       # what shows between the top step's stones
        m.g["Back"][y + 5][x + i] = bv.DIRT   # and under the last steps


# --- the test strip (Task 4): a scratch copy only -------------------------------------

STRIP_CORNER = (88, 59)       # the bluff's north-west corner (the rim's row)
STRIP_SIDE_TO = 65            # the side's last row; the face's lip row is the next
STRIP_STAIRS_X = 90
STRIP_BOX = (86, 57, 95, 75)  # what the strip clears of props, trees and decor first


def lay_test_strip(tmx_path, exports):
    import contextlib, io
    committed = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                             "[CP] Gilded Vale (dev)", "assets", "GildedVale.tmx")
    assert os.path.abspath(tmx_path) != os.path.abspath(committed), "the strip goes on a scratch copy"
    with contextlib.redirect_stdout(io.StringIO()):
        ctx = context(tmx_path, exports)
    m = load(tmx_path)
    x0, y0, x1, y1 = STRIP_BOX
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            for layer in ("Paths", "Front", "Front2", "Back2"):
                m.g[layer][y][x] = 0
            if m.g["Buildings"][y][x] and (m.g["Back"][y][x], m.g["Buildings"][y][x]) not in bv.GROUND:
                m.g["Buildings"][y][x] = 0               # the stool, the rocks, the picnic table
            m.props.pop(("Buildings", x, y), None)
    cx, cy = STRIP_CORNER
    top = STRIP_SIDE_TO + 1
    # the plateau's own corners, and the ground below the face, grass (dark patches
    # are for later, and stay off a cliff's shadow)
    grass = {(i, j): "G" for i in range(cx + 1, 97) for j in range(cy + 1, top + 1)}
    grass.update({(i, j): "G" for i in range(cx, 97) for j in range(top + 1, 77)})
    _paint(m, ctx, grass, (cx, cy, 95, 75))
    lay_lip(m, ctx, cx, 95, cy)
    lay_side(m, ctx, cx, cy + 1, STRIP_SIDE_TO)
    lay_face(m, ctx, cx, 95, top, stairs_x=STRIP_STAIRS_X)
    lay_steps(m, ctx, STRIP_STAIRS_X, top)
    # a trail from the flight's foot west to the lakeside path's sand
    _paint(m, ctx, {(i, j): "S" for i in range(cx - 2, STRIP_STAIRS_X + 4) for j in (top + 7, top + 8)},
           (cx - 3, top + 5, STRIP_STAIRS_X + 4, 75))
    save(m, tmx_path)
    print("overlook strip: wrote", tmx_path)


if __name__ == "__main__":
    if sys.argv[1:2] == ["--strip"]:
        lay_test_strip(sys.argv[2], sys.argv[3])
    else:
        sys.exit("usage: vale_overlook.py --strip <scratch GildedVale.tmx> <exports>")
