"""The overlook: a raised bluff, one level high, in the Vale's south-east corner.
(docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md, section 2)

patch_file lays it on the committed map (build_vale.VALE_PIECES; the layout is the
constants under "the overlook as built"): the bluff x 88-95, y 56-73, its steps up the
south face, the overlook bench and the easels' row at its north-west, Lupini's lake
painting spot along its west edge, the shed, the Three Trees and the stump of the fourth,
the lakeside path wandering along its west foot and a trail along its south foot to the
steps, with the picnic table by the lake's corner. Re-runnable: the box is cleared and its
corners set outright each run; the lake spot's pieces are picked up wherever they stand.

How its edges are drawn (Task 4), learned from the vanilla plateaus (read off their
stacks, 2026-11-08; docs/terrain_painting.md, "A raised bluff"):

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
    The shadow under the face runs on into a skirt of dark grass, which the painter's
    own edge pieces end, wavy, below it and beside the end column (Town ledge x 26-38
    y 10-12, Forest x 93-95 y 71-72): the fade piece 404 went plain white in winter.
  - Below a west side, a skirt of dark grass, wider lower down (the Forest's terraces);
    a path beside it keeps a corner of grass between (dark grass has no piece against
    sand) and moves out round it.
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
  lay_steps(m, ctx, x, y)       the flight up a face whose top row is y, left column x,
                                snowed on in winter (our own copies of its tiles)
  lay_lake_corner(m, ctx)       the lake's south-east corner below the bluff, on the diagonal

python tools/vale_overlook.py <GildedVale.tmx> <exports>
  lays the overlook (patch_file).
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
FLIGHT_FROM = STAIR_SPOTS[3]  # the lower terrace's east flight (84, 48): its tiles are copied
SEED = 85
# The landing above a flight's top step, corners (column from the flight's left, rows
# above its top): full width at the step, tapering off up the bluff, never a lone corner
# or a checkerboard.
LANDING = [(1, 0), (2, 0), (3, 0), (1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (2, 3), (3, 3), (2, 4)]
FADE_WEST = {356: 355, 406: 405}   # the shading's left-end pieces (Forest (94,72) 405)
SKIRT_WIDENS = 3             # the side's skirt widens to two tiles this many rows below its first


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


def _inside(x, y):
    """Nothing is laid outside the overlook's box (vale_patch.OVERLOOK_BOX)."""
    bx0, by0, bx1, by1 = OVERLOOK_BOX
    assert bx0 <= x <= bx1 and by0 <= y <= by1, f"{(x, y)} is outside the overlook's box {OVERLOOK_BOX}"


def _laid(m, x, y):
    _inside(x, y)
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
    # (from row y: the rim sets no corner above its own row, so the tiles above it stay as
    # they are; one of them, under the terrace cliff's shading, isn't a plain piece)
    _paint(m, ctx, {(cx, y + 1): "G" for cx in range(x0 + 1, x1 + 2)}, (x0 - 1, y, x1 + 1, y + 1))
    for x in range(x0, x1 + 1):
        _clear(m, x, y)
        m.g["Buildings"][y][x] = RIM_NW if x == x0 else RIM[(x - x0 - 1) % 2]


def _clear_of_sand(V, corners, x, clear, cy):
    """On corner row cy, the `clear` corners west of x kept clear of sand (dark grass's,
    and a corner of grass outside it: dark grass has no piece against sand). A path
    there moves out west by as many corners (it keeps its width), never onto water."""
    run = [cx for cx in range(x - clear, x) if V[cy][cx] == "S"]
    for cx in range(x - clear, x):
        corners.setdefault((cx, cy), "G")
        if corners[(cx, cy)] == "S":
            corners[(cx, cy)] = "G"
    if run:
        west = x - clear - 1
        while west >= 0 and V[cy][west] == "S":
            west -= 1
        for k in range(len(run)):
            if west - k >= 0 and V[cy][west - k] != "W":
                corners[(west - k, cy)] = "S"


def lay_side(m, ctx, x, y0, y1, widens=SKIRT_WIDENS):
    """The bluff's west side, down column x from y0 (under the corner) to y1. The side
    tile's left edge is see-through: the lower ground under it is the painter's; the
    plateau's grass starts on the corners right of it.
    Below it a skirt of dark grass, as the Forest's terraces have below their sides
    (doc "A raised bluff"): the side's outer corners dark, rounded off at both ends by the
    painter's edge pieces, a tile wider from `widens` rows down (SKIRT_WIDENS; a bulge that
    narrowed again below needs D D / G D, whose only pieces carry the lip). Dark
    grass has no pieces against sand (the table's few are over town-sheet overlays), so a
    path beside it keeps a corner of grass between: a path along the side is pushed out
    round the skirt, and its edge no longer runs beside the lip's in two straight lines."""
    corners = {(x + 1, cy): "G" for cy in range(y0, y1 + 2)}
    V = _corners(m)
    b0, b1 = y0 + widens, y1
    for cy in range(y0, y1 + 2):
        corners[(x, cy)] = "D" if y0 < cy <= y1 else "G"
        if b0 <= cy <= b1:
            corners[(x - 1, cy)] = "D"
        _clear_of_sand(V, corners, x, 2 if b0 - 1 <= cy <= b1 + 1 else 1, cy)
    _paint(m, ctx, corners, (min(cx for cx, _ in corners) - 1, y0 - 1, x + 1, y1 + 1))
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
    from vale_feature_fixes import SEASONS
    src = {se: Image.open(os.path.join(ctx.exports, "sheets_" + se, "spring_outdoorsTileSheet.png")).convert("RGBA")
           for se in SEASONS}
    cols = src["spring"].width // 16

    def piece(se, g):
        k = g - 1                                       # the outdoors sheet is first, at gid 1
        return vf.unband(src[se].crop((k % cols * 16, k // cols * 16, k % cols * 16 + 16, k // cols * 16 + 16)))

    slots = own_slots(m, ctx, [{se: piece(se, g) for se in SEASONS} for g in gids])
    return dict(zip(gids, slots))


def own_slots(m, ctx, wants):
    """Slots in the Vale's four seasonal sheets for tiles {season: 16 x 16 image}, in
    order: a slot already holding the same pixels in every season is reused, others go
    at the sheets' end (and the map's tileset is told how big the sheets now are)."""
    from vale_sculpture import Sheets, re_tileset
    from vale_feature_fixes import FEATURE_FIRST, SEASONS
    sheets = Sheets(ctx.assets)
    index = {}
    for i in range(sheets.end):
        index.setdefault(sheets.im["spring"].crop(sheets.box(FEATURE_FIRST + i)).tobytes(), []).append(i)
    out, new = [], []
    for want in wants:
        slot = next((i for i in index.get(want["spring"].tobytes(), [])
                     if all(sheets.im[se].crop(sheets.box(FEATURE_FIRST + i)).tobytes() == want[se].tobytes()
                            for se in SEASONS)), None)
        if slot is None:
            slot = next((k for k, w in new if all(w[se].tobytes() == want[se].tobytes() for se in SEASONS)), None)
        if slot is None:
            slot = sheets.end + len(new)
            new.append((slot, want))
        out.append(FEATURE_FIRST + slot)
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


# Snow on a flight's treads, in vale_feature_fixes.snowed()'s colours: the lit snow, its
# edge, and its shaded front edge (never black).
SNOW_LIGHT, SNOW_MID, SNOW_SHADE = (240, 246, 252, 255), (214, 226, 240, 255), (168, 186, 212, 255)
TREAD_LUM = 125     # a row of the flight whose stone is lighter than this is a tread (darker: a riser)


def snow_treads(im):
    """The flight's art (all its tiles together) with snow lying on every tread, as
    snowed() lays it on a feature's top: the stairs art is a stack of slabs, each a lit
    tread (4 rows, its nose the lightest) over a dark riser and a shadow line. The
    treads go white, their ends and first row a shade of the snow, their nose the
    snow's shaded edge; the risers stay stone. (Winter only: the terraces' flights,
    outside the overlook, keep their bare winter look.)"""
    im = im.copy()
    px = im.load()
    W, H = im.size
    solid = lambda x, y: 0 <= x < W and 0 <= y < H and px[x, y][3] > 0
    lum = []
    for y in range(H):
        row = [px[x, y] for x in range(W) if px[x, y][3]]
        lum.append(sum(0.3 * r + 0.59 * g + 0.11 * b for r, g, b, a in row) / len(row) if row else 0)
    tread = [l > TREAD_LUM for l in lum]
    out = im.copy()
    op = out.load()
    for y in range(H):
        if not tread[y]:
            continue
        nose = y + 1 >= H or not tread[y + 1]
        first = y == 0 or not tread[y - 1]
        for x in range(W):
            if not solid(x, y):
                continue
            end = not solid(x - 1, y) or not solid(x + 1, y) or not solid(x, y - 1)
            op[x, y] = SNOW_SHADE if nose else SNOW_MID if (end or first) else SNOW_LIGHT
    return out


def flight_pieces(m, ctx):
    """The lower terrace's flight (FLIGHT_FROM: treads on Back2, side walls on
    Buildings2) as this module's own copies in the Vale's seasonal sheets: the same art
    in spring, summer and fall, snow on its treads in winter (snow_treads). Their gid
    -> our gid. The terraces' flights keep the shared originals."""
    from PIL import Image
    from vale_sculpture import Sheets
    from vale_feature_fixes import FEATURE_FIRST, SEASONS
    fx, ft = FLIGHT_FROM
    cells = [(i, j, "Back2" if i in (1, 2) else "Buildings2") for j in range(6) for i in range(4)]
    gids = [m.g[layer][ft + j][fx + i] for i, j, layer in cells]
    sheets = Sheets(ctx.assets)
    art = {}
    for se in SEASONS:
        im = Image.new("RGBA", (64, 96))
        for (i, j, _), g in zip(cells, gids):
            if FEATURE_FIRST <= g < FEATURE_FIRST + 10000:
                im.paste(sheets.im[se].crop(sheets.box(g)), (i * 16, j * 16))
        art[se] = snow_treads(im) if se == "winter" else im
    wants = [{se: art[se].crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)) for se in SEASONS} for i, j, _ in cells]
    slots = own_slots(m, ctx, wants)
    return {g: v for g, v in zip(gids, slots) if FEATURE_FIRST <= g < FEATURE_FIRST + 10000}


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
    top..top+4 from x0 to x1, its shading on top+3..top+5 and a skirt of dark grass
    below that. x0 is the west side's column: the side turns into the face there with
    the face's rounded west end. stairs_x: a flight's left column (lay_steps), so the lip
    beside it is plain and the ground beside its top step is left to the landing."""
    rnd = random.Random(SEED * 1000 + top * 100 + x0)
    # The shading under the face is dark grass to the painter (the table reads 327 as
    # G G / D D and 356 and 406 as all D), so its corners are D, and a row more below it:
    # under the Town ledge's shading (Town x 26-38 y 10-12) the dark grass runs on and
    # the painter's own edge pieces end it, wavy, a row or two down, and in winter the
    # band fades into that pale ground instead of stopping in a straight line on the
    # snow. At the west end the same edge pieces end it under and beside the end column,
    # as the game ends it (Forest x 93-95 y 71-72). (The fade piece 404 there went plain
    # white in winter: a white box beside the side's corner.) Below the skirt a row of
    # grass corners (top+7): dark grass has no real piece against sand (the table's
    # D D / S S, 635, is light grass with a straight sand edge), so a path along the foot
    # starts a row further down.
    corners = {(cx, cy): "D" if top + 4 <= cy <= top + 6 else "G"
               for cx in range(x0 + 1, x1 + 2) for cy in range(top, top + 8)}
    # Its west end melts into the ground as the Forest's does (x 93-96 y 70-74): the dark
    # grass runs on a corner past the face's end column, under its foot, and the band's
    # first column takes its pieces' own left-end fades (FADE_WEST: 355, 405, pale with
    # the shade dithered in from the right, in winter too; it's 404 that goes white).
    # With grass there, the band ended in a straight vertical side on the snow.
    corners.update({(x0, cy): "D" if top + 4 <= cy <= top + 6 else "G" for cy in range(top, top + 8)})
    V0 = _corners(m)
    for cy in range(top + 3, top + 8):
        _clear_of_sand(V0, corners, x0 + 1, 2, cy)
    # (repaint no further west than the corners set: the lake's bank lies beyond them)
    V = _paint(m, ctx, corners, (min(cx for cx, _ in corners) - 1, top - 1, x1 + 1, top + 7))
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    end = west_end_pieces(m, ctx)
    band = {top + 3: ("G", "G", "D", "D"), top + 4: ("D",) * 4, top + 5: ("D",) * 4}
    for x in range(x0, x1 + 1):
        for y in range(top, top + 5):
            _clear(m, x, y)
        if x == x0:
            for r in range(5):
                m.g["Buildings"][top + r][x] = end[r]
            continue        # its open west edge: the painter's ground stays under it
        beside = stairs_x is not None and stairs_x - 2 <= x <= stairs_x + 5
        m.g["Buildings"][top][x] = CLIFF_LIP[0][0] if beside or rnd.random() < CLIFF_LIP[0][1] else CLIFF_LIP[1][0]
        if stairs_x is None or not stairs_x <= x <= stairs_x + 3:
            m.g["Back"][top][x] = GRASS      # grass behind the edge, never shore
        for k, gid in enumerate(CLIFF_FACE[x % 2]):
            if gid in CLIFF_FACE_VARIANT and rnd.random() < 0.08:
                gid = CLIFF_FACE_VARIANT[gid]
            m.g["Buildings"][top + 1 + k][x] = gid
        m.g["Back"][top + 1][x] = m.g["Back"][top + 2][x] = GRASS
        for k, opts in enumerate(CLIFF_SHADE):
            y = top + 3 + k
            # the main piece only (the variant 404 goes plain white in winter), and only on
            # the band's own corners: where it meets other ground the painter's edge stays
            # (on under a flight's treads too: the band runs on beneath it)
            if pat(x, y) == band[y]:
                m.g["Back"][y][x] = FADE_WEST.get(opts[0], opts[0]) if x == x0 + 1 else opts[0]
                _clear(m, x, y) if y >= top + 5 else _laid(m, x, y)


def lay_steps(m, ctx, x, y):
    """The flight up a south face (lay_face) whose top row is y, columns x..x+3: the
    lower terrace's flight copied tile for tile (treads on Back2, walkable; side walls on
    Buildings2, over the face, which stays solid under them), snowed on in winter
    (flight_pieces). Above its ragged top step a sand landing that tapers off up the
    bluff like a worn trail (LANDING), so neither a grass line nor a patch with square
    corners shows. At its foot the face's shadow and its dark-grass skirt run on under
    it; the path from its foot is the caller's, below the skirt and the grass row under
    it (corner rows y + 8 on: lay_face)."""
    fx, ft = FLIGHT_FROM
    corners = {(x + i, y - j): "S" for i, j in LANDING}
    V = _paint(m, ctx, corners, (x - 1, y - 5, x + 4, y))
    own = flight_pieces(m, ctx)
    for j in range(-1, 8):
        for i in range(4):
            sy, ty = ft + j, y + j
            _inside(x + i, ty)
            if i in (1, 2):
                if -1 <= j <= 6:
                    m.g["Buildings"][ty][x + i] = m.g["Front"][ty][x + i] = 0
                m.g["Back2"][ty][x + i] = own.get(m.g["Back2"][sy][fx + i], m.g["Back2"][sy][fx + i])
            else:
                m.g["Buildings2"][ty][x + i] = own.get(m.g["Buildings2"][sy][fx + i], m.g["Buildings2"][sy][fx + i])
    for j in range(6):                        # the rows with the flight's art
        for i in range(4):
            _laid(m, x + i, y + j)
    pat = lambda cx, cy: (V[cy][cx], V[cy][cx + 1], V[cy + 1][cx], V[cy + 1][cx + 1])
    for i in (0, 3):
        # beside the top step, under the walls and the lip: the landing's own edge
        stack = vf.main_stack(pat(x + i, y), ctx.table)
        if stack and not stack[1]:
            m.g["Back"][y][x + i] = stack[0]
    for i in (1, 2):
        m.g["Back"][y][x + i] = bv.DIRT       # what shows between the top step's stones


def lay_lake_corner(m, ctx):
    """The lake's south-east corner, below the bluff (LAKE_SE): its east bank came down
    square onto its south bank, the corner piece leaving a notch. Cut on the diagonal
    instead, as the Town river's south-east shore steps (Town x 78-84 y 74-80: plain water
    on the corner's inner tile, 261 / 214 on the diagonal)."""
    cx, cy = LAKE_SE
    V = _corners(m)
    if V[cy][cx] == "W" and V[cy + 1][cx] != "W" and V[cy][cx + 1] != "W":
        _paint(m, ctx, {(cx, cy): "S"}, (cx - 2, cy - 2, cx + 1, cy + 1))
    # the context lookup lays reeds on the corner's inner tile and the cycle runs on into
    # the turn in a jog: the Town shore's own stacks there instead (Town (82,75), and
    # (83,74) above its diagonal; Town's (83,73) 5322 above that left a square step under
    # our short run's cap, so the painter's piece stays there)
    for (x, y), stack in ([((cx - 1, cy - 1), (bv.WATER, 0)), ((cx, cy - 1), LAKE_SE_END)]
                          + [((cx, cy - 2 - k), s) for k, s in enumerate(LAKE_SE_TURN)]):
        m.g["Back"][y][x], m.g["Buildings"][y][x] = stack
        _laid(m, x, y)


# --- the overlook as built (Task 5) ----------------------------------------------------
# The bluff: its north rim on row 55, its west side down column 88 (rows 56-65), its south
# face from row 66, so its top is x 89-95, y 56-65. The rim is further north than the plan
# drew: the steps take the south face (no vanilla flight climbs south) and the face takes six
# rows, and only a top this deep holds the bench, the easels' row, the painting spot, the
# shed and the Three Trees with none of their crowns over the rest (searched: with the rim
# on 56 no arrangement fits). North of the rim one row of the lower ground stays, under the
# terrace cliff's shading, as the Forest's terraces step.
RIM_Y = 55
SIDE_X = 88
FACE_TOP = 66
SIDE_WIDENS = 6              # the side's skirt is two tiles wide from row 62 (the path beside it wanders above)
STAIRS_X = 90                # the flight's left wall; treads on x 91-92
# The overlook bench, at the north-west corner. Town and the Forest have no bench seen
# from behind, so it's Town's side bench facing west (vale_decor "bench_w"), its back to
# the easels, looking out over the lake: its top on Front, its two seat rows on Buildings
# (as Town lays it). BENCH is its upper seat tile. It alone stands against the side's lip:
# a step further in, the lake behind the lip is out of its view, and the top has no column
# to spare (the painting spot, the lane and the shed take the rest).
BENCH = (89, 58)
BENCH_FACING = 3             # west, over the lake
# x0, x1, row: where the 10-heart scene's painters stand, beside the bench; their easels go
# a row further north, at the edge (row 57), the woods beyond it thinned so they look out
# over the Vale.
EASELS = (90, 92, 58)
# Lupini's lake painting spot (build_vale.PICKS spot_lake_*): left tile and base row of
# each 2 x 2 piece, a tile in from the west side, under the bench, over the lake. He stands
# between them, facing it.
STOOL = (90, 60)
ROCK = (90, 63)
LUPINI_STAND = (90, 61)
# The shed, where the third easel is kept: the game's own little plank hut (outdoors sheet,
# Town numbering): its plank roof with the vent (sheet x 22-24, rows 69-70) over its walls
# with the window and the door (x 15-17, rows 76-77). The sheet keeps the two halves apart,
# but they meet pixel for pixel (the sign's cords run on from the roof's row into the wall's),
# and every season is in the sheets (snow on the roof in winter). x0, y0, x1, y1; y1 its base.
# Its door opens onto the steps' landing.
SHED = (93, 61, 95, 64)
SHED_ART = ((1748, 1749, 1750), (1773, 1774, 1775), (1916, 1917, 1918), (1941, 1942, 1943))
# The Three Trees (Paths markers: 9 oak, 10 maple, 11 pine) and the stump of the fourth, at
# the north-east, inward: a tree's crown is 3 wide and stands 2-5 rows above its trunk, so
# these hang out over the rim and the lower ground, on the map, clear of the bench, the
# easels' row and the shed's roof. The woods behind them (rows 53-54) go, so they stand apart.
TREES = ((94, 56, 10), (93, 58, 9), (94, 60, 11))
STUMP = (93, 60, 128)        # the game's own stump with its roots (outdoors 128, Forest (33,2)): a map tile, it can't be chopped
THIN = (88, 53, 95, 54)      # the woods north of the rim that go
# The woods that ring the Vale, round the bluff: down the east edge below the face, and
# along the south edge where the trail, the picnic table and the steps' foot leave room
# (the trail along the face's foot takes the bottom rows between them).
FRINGE = ((95, 71, 10), (95, 73, 9), (95, 75, 11), (87, 72, 9), (84, 74, 11),
          (81, 73, 10), (82, 74, 10), (81, 75, 11), (83, 75, 10))
PICNIC = (88, 72)            # vale_decor "picnic", at the bluff's foot by the lake's corner: no room on top
FLOWERS = ((92, 60, 308), (93, 59, 258))
# The lakeside path along the bluff's west foot: its east edge (the last sand corner) on
# each corner row. It wanders a corner in and out, kept a corner of grass from the skirt.
# (North of the bluff, rows 54-55, it stays as it is: x 85-87.)
PATH_EAST = {56: 86, 57: 86, 58: 86, 59: 85, 60: 86, 61: 85, 62: 85, 63: 84, 64: 84,
             65: 85, 66: 85, 67: 86, 68: 87, 69: 86, 70: 86, 71: 85, 72: 86, 73: 86}
PATH_NARROW = {56: 85, 57: 84, 58: 84}  # where it still runs apart from the lake's sand: its west edge
PATH_MEETS_LAKE = range(59, 72)  # rows where it runs into the lake's sand (the grass between goes)
# The trail along the south foot to the flight: sand corners (x0, x1) by row. It dips under
# the picnic table and rises to the flight's foot (corner row 74, below the skirt and its row
# of grass: lay_face), so neither of its edges runs straight; its west end leaves a tile of
# grass for the woods by the lake's corner.
SOUTH_PATH = {74: ((86, 87), (91, 93)), 75: ((86, 93),), 76: ((87, 91),)}
CLEAR_BOX = (84, 55, 95, 75)  # what's cleared of props, markers and decor before laying


# --- the test strip (Task 4): a scratch copy only -------------------------------------

STRIP_CORNER = (88, 59)       # the bluff's north-west corner (the rim's row)
STRIP_SIDE_TO = 65            # the side's last row; the face's lip row is the next
STRIP_STAIRS_X = 90
STRIP_BOX = (86, 57, 95, 75)  # what the strip clears of props, trees and decor first
LAKE_SE = (84, 71)            # the lake's south-east water corner (OVERLOOK_BOX), cut by lay_lake_corner
# The east bank's last pieces above the cut, upward: the rock run's cycle carried on under
# its cap to its last two pieces, as Town's shore at x 85 ends (rows 67-69: (5226,236),
# (1230,236), then 261 and 214). Town's other ending, 211 (x 83 row 74), closes a run of
# 5229 / 5306 / 5322 that this short run has no room for: alone under a plain piece it was a
# broken triangle with a cut. (Fixed, not the painter's: re-chosen each run against what the
# last run left, the piece under the cap flipped between 5290 and 236.)
LAKE_SE_TURN = ((1230, 236), (5226, 236))
LAKE_SE_END = (1230, 261)     # under it, where the bank turns onto the diagonal (Town (85,69), (83,75)); the
                              # diagonal's 214 follows (with two 214s it was a ruler-straight line)


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
    # a trail from below the flight's foot west to the lakeside path's sand (below the
    # face's dark-grass skirt and the grass row under it: lay_face, lay_steps)
    _paint(m, ctx, {(i, j): "S" for i in range(cx - 2, STRIP_STAIRS_X + 4) for j in (top + 8, top + 9)},
           (cx - 3, top + 6, STRIP_STAIRS_X + 4, 75))
    lay_lake_corner(m, ctx)
    save(m, tmx_path)
    print("overlook strip: wrote", tmx_path)


PAINT_SPOT = "blinxxy.MuseArt_PaintSpot"


def _spot_pieces(m):
    """Lupini's lake painting spot as it stands in the box: its two 2 x 2 pieces (the
    stool above the rock), each [(dx, dy, layer, gid)] from its left base tile, found by
    their PaintSpot actions (wherever reachable_spot, or an earlier run, put them)."""
    bx0, by0, bx1, by1 = OVERLOOK_BOX
    bases = sorted((y, x) for (layer, x, y), p in m.props.items()
                   if layer == "Buildings" and p.get("Action") == PAINT_SPOT and bx0 <= x <= bx1 and by0 <= y <= by1)
    lefts = [(y, x) for y, x in bases if (y, x - 1) not in bases]
    assert len(lefts) == 2 and len(bases) == 4, f"the lake spot's pieces in the box: {bases}"
    pieces = []
    for y, x in lefts:          # by row: the stool first, then the rock
        cells = []
        for dx in (0, 1):
            cells.append((dx, 0, "Buildings", m.g["Buildings"][y][x + dx]))
            for layer in ("Front2", "Front"):
                if m.g[layer][y - 1][x + dx]:
                    cells.append((dx, -1, layer, m.g[layer][y - 1][x + dx]))
        pieces.append(cells)
    return pieces


def _design(m, ctx):
    """The corners of the bluff's top, the lakeside path along its west foot and the trail
    along its south foot, set outright (so every run paints from the same terrain). The
    edges (lay_lip, lay_side, lay_face, lay_steps) then set their own."""
    V = _corners(m)
    want = {}
    for cy in range(RIM_Y + 1, 74):                    # the top, and under the face: grass
        for cx in range(SIDE_X, 97):                    # (the edges then set their own)
            want[(cx, cy)] = "G"
    for cy, east in PATH_EAST.items():
        # sand from the path's west edge (where it meets the lake's sand, the lake's) to its
        # east edge; grass from there to the side (the side's skirt is lay_side's)
        west = PATH_NARROW.get(cy)
        if west is None:
            west = east
            while V[cy][west - 1] == "S" or (cy in PATH_MEETS_LAKE and V[cy][west - 1] == "G"):
                west -= 1
        for cx in range(west, east + 1):
            assert V[cy][cx] != "W", (cx, cy)
            want[(cx, cy)] = "S"
        for cx in range(east + 1, SIDE_X):
            want[(cx, cy)] = "G"
    for cy in (74, 75, 76):                             # the south foot: grass, and the trail
        for cx in range(84, 97):
            want[(cx, cy)] = "G"
        for x0, x1 in SOUTH_PATH[cy]:
            for cx in range(x0, x1 + 1):
                want[(cx, cy)] = "S"
    return want


def _lay_cells(m, x, y, cells):
    for dx, dy, layer, gid in cells:
        _inside(x + dx, y + dy)
        m.g[layer][y + dy][x + dx] = gid


def patch_file(tmx_path, exports):
    """The overlook on the committed map, in place (build_vale.VALE_PIECES). In order:
    the lake spot's pieces are picked up and everything in CLEAR_BOX cleared of props,
    markers and decor; the corners (_design) and the ground; the rim, side, face and steps
    (one map object, so their working corners carry from one to the next); the lake's
    corner; then the bench, the painting spot, the shed, the trees and the stump, the picnic
    table, the woods that stay, flowers; the PaintSpot actions at the spot's new tiles."""
    import contextlib, io
    from vale_decor import PROPS
    with contextlib.redirect_stdout(io.StringIO()):
        ctx = context(tmx_path, exports)
    m = load(tmx_path)
    stool, rock = _spot_pieces(m)
    x0, y0, x1, y1 = CLEAR_BOX
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            for layer in ("Paths", "Front", "Front2", "AlwaysFront", "Back2", "Buildings2"):
                m.g[layer][y][x] = 0
            if m.g["Buildings"][y][x] and (m.g["Back"][y][x], m.g["Buildings"][y][x]) not in bv.GROUND:
                m.g["Buildings"][y][x] = 0      # props, and an earlier run's edges, shed, bench and stump
            for layer in ("Buildings", "Paths", "Back"):
                m.props.pop((layer, x, y), None)

    # (from x 82: the path's corners move as far west as x 83, and the tiles left of a
    # moved corner change with it)
    _paint(m, ctx, _design(m, ctx), (82, RIM_Y, x1, y1))
    lay_lip(m, ctx, SIDE_X, 95, RIM_Y)
    lay_side(m, ctx, SIDE_X, RIM_Y + 1, FACE_TOP - 1, widens=SIDE_WIDENS)
    lay_face(m, ctx, SIDE_X, 95, FACE_TOP, stairs_x=STAIRS_X)
    lay_steps(m, ctx, STAIRS_X, FACE_TOP)
    lay_lake_corner(m, ctx)

    _lay_cells(m, BENCH[0], BENCH[1] - 1, PROPS["bench_w"])
    for (sx, sy), cells in ((STOOL, stool), (ROCK, rock)):
        _lay_cells(m, sx, sy, cells)
        for dx in (0, 1):
            m.props[("Buildings", sx + dx, sy)] = {"Action": PAINT_SPOT}
    sx0, sy0, sx1, sy1 = SHED
    for j, row in enumerate(SHED_ART):
        y = sy0 + j
        # only its foot is solid; the wall above it on Front, so you walk behind the shed,
        # and the roof (two and more rows above the foot) on AlwaysFront, as place_buildings
        # lays a building's roof: Front sorts by its own row and would draw under you
        layer = "Buildings" if y == sy1 else "Front" if y == sy1 - 1 else "AlwaysFront"
        for i, gid in enumerate(row):
            _inside(sx0 + i, y)
            m.g[layer][y][sx0 + i] = gid
    for x, y, k in TREES + FRINGE:
        _inside(x, y)
        m.g["Paths"][y][x] = ctx.paths_first + k
    x, y, gid = STUMP
    _inside(x, y)
    m.g["Buildings"][y][x] = gid
    _lay_cells(m, PICNIC[0], PICNIC[1], PROPS["picnic"])
    plain = lambda x, y: (set(bv.GROUND.get((m.g["Back"][y][x], m.g["Buildings"][y][x]), "?")) <= {"G", "D"}
                          and not any(m.g[l][y][x] for l in ("Buildings", "Buildings2", "Back2", "Front", "Front2",
                                                              "AlwaysFront", "Paths")))
    for x, y, gid in FLOWERS:
        if plain(x, y):
            m.g["Back2"][y][x] = gid
    tx0, ty0, tx1, ty1 = THIN
    for y in range(ty0, ty1 + 1):
        for x in range(tx0, tx1 + 1):
            _inside(x, y)
            m.g["Paths"][y][x] = 0
    # the woods round the box's edge stay only where their ground is still plain grass
    bx0, by0, bx1, by1 = OVERLOOK_BOX
    for y in range(by0, by1 + 1):
        for x in range(bx0, bx1 + 1):
            if m.g["Paths"][y][x] and (x, y) not in {(t[0], t[1]) for t in TREES + FRINGE}:
                v = m.g["Paths"][y][x]
                m.g["Paths"][y][x] = 0
                if plain(x, y):
                    m.g["Paths"][y][x] = v
    save(m, tmx_path)
    print("overlook: wrote", tmx_path)


if __name__ == "__main__":
    if sys.argv[1:2] == ["--strip"]:
        lay_test_strip(sys.argv[2], sys.argv[3])
    elif len(sys.argv) == 3:
        patch_file(sys.argv[1], sys.argv[2])
    else:
        sys.exit("usage: vale_overlook.py <GildedVale.tmx> <exports>\n"
                 "       vale_overlook.py --strip <scratch GildedVale.tmx> <exports>")
