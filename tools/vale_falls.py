"""The waterfall: the river comes into the Vale over a tall fall in the north-east,
through a notch in a rim cliff across the corner, into a stone basin above Lupini's
atelier, and leaves west along the cliff's foot as before.
(docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md, section 1)

What the vanilla maps say (read off their stacks, 2026-11-08):

- A cliff taller than ours repeats its middle face rows. The Mountain's quarry cliff
  (Mountain x 109-128, y 1-9) is 8 rows: its lip, one row of 468/469, two of 493/494,
  four of 518/519, then 543/544, by column parity (with the odd variant stone). So ours
  is the cliff rule (build_vale.cliff) with its two middle rows doubled: lip on y 0,
  468 / 493 / 493 / 518 / 518 / 543 on y 1-6, the shading below on y 7.
- The fall is the Town falls (build_vale.FALLS, Town x 91-97, y 0-6). Its rows are:
  the spray over the river above the edge (sheet row 4), the curved lip (row 5), the
  falling water (rows 6 and 7), then on Front the fall's foot (row 8), the foam (row 9)
  and the ripples (row 10), with the deep pool's shadow on AlwaysFront. The lip, the
  spray and the foot stay once; the falling water (sheet row 6) repeats. Its animation
  is the texture scrolling down 4 px a frame (17 frames), so a row of it stacked
  straight on itself breaks at the seam. Each repeated row is the same tiles started 4
  frames on (16 px: one row of the texture), the rows above the repeat 4 more again;
  the seam is then as smooth as the game's own (6.3 against 4.9 mean px difference,
  11.2 stacked plainly). The animations are added to the map's Waterfalls tileset on
  the frame tile they start from, so a static render shows the right picture too.
  The wet rock behind (outdoors2, Buildings) repeats its middle row the same way.
- A cliff ends, with no corner turned, the way the Town ledge ends at its stairs:
  the stairs' side walls are the cliff's end columns (build_vale.stairs). The west end
  is the right wall (Town x 22, rows 43-47: a rounded left edge), the east end the
  left wall (Town x 19, rows 44-48: a rounded right edge). Made taller, they repeat
  only the row whose rounded edge runs on into itself (END_ROWS), and the west wall
  is laid without the cast shadow it throws on the Town's stair treads (BAND_W).
  Trees stand in front: the woods at the top edge, a pine added below them at the
  west end, and the orchard's own trees at the east end.

- A river under a cliff keeps its bank: the Town pool's runs along its cliff's foot and
  under its falls (Town x 92-97 y 4-5). So the river's north bank runs on along the
  rim's foot (BASIN_V), under the fall on Town's stacks (POOL_DARK, POOL_BANK, the wet
  rock's last row), and turns down into the basin's east bank (bank_end).

Laid in order: the corners (the notch and the basin, sand round them), the ground by
the painter's rules (vale_patch.repaint_terrain), the bank's end, the cliff, the fall,
the river stones, and the sound (AmbientLocationSounds.sound_waterfall_big = 5, as the Town
falls has it).

Run as: python tools/vale_falls.py <GildedVale.tmx> <exports>   (build_vale.VALE_PIECES)
Re-runnable: the cliff's cells are put back to plain ground for their corners first,
so each run paints from the same start; the west wall's pieces reuse their own slots
in the Vale's seasonal sheets.
"""
import os, random, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv
from build_vale import (CLIFF_FACE, CLIFF_FACE_VARIANT, CLIFF_LIP, CLIFF_SHADE, FALLS, GRASS,
                        OUTDOORS2, LILY_PADS, WATERFALLS)
from vale_patch import FALLS_BOX, context, corner_grid, load, repaint_terrain, save

NOTCH_X = 80                 # the fall's first water column (water x 80-83, the river's width)
# The basin's water corners. Its top is row 8: the river's north bank (the depth row under
# the face's last row, the rock row on y 7) runs on along the cliff's foot to the fall's
# pool, as the Town pool's bank runs along its cliff (Town x 92-97 y 4-5), and turns up
# into the pool under the foam. (With the water up to the face, row 6, the bank's diagonal
# up to it sat under the face's last row, and the rock row stopped in a cut at x 77.) Its
# east bank is corner x 85, not 86, so it doesn't line up under the cliff's east end.
BASIN_V = (78, 8, 85, 12)
# The sand's east edge (corners on x 87, by row): grass under the cliff's east end, so the
# sand east of the basin doesn't run on straight down from the cliff's edge; the sand
# widens to two tiles lower down, the way the painter's rim varies (doc section 4). The
# orchard (x 88 on) stays as it is.
EAST_SAND = {7: "G", 8: "G", 9: "G", 10: "S", 11: "S", 12: "S"}
CLIFF_X0, CLIFF_X1 = 76, 87  # the rim's end columns: into the woods west, against the orchard east
FOOT = 6                     # the face's last row; its shading is on FOOT + 1
FACE_ROWS = (0, 1, 1, 2, 2, 3)          # CLIFF_FACE's rows for y 1-6 (its middle rows doubled)
# The end walls' Town rows for y 0-6. Their rounded outer edge only runs on where one
# piece's bottom meets the next one's top: 446 -> 492 -> 517 -> 517 does (edge 5 px in
# -> 5, 1 -> 1, 1 -> 1); 492 on 492 or 517 on 492 steps 4 px in (a sawtooth). So the
# 517 row, which runs on into itself, is the one repeated, three times.
END_ROWS = (0, 1, 2, 3, 3, 3, 4)
WEST_END = (22, 43)                     # Town x 22 from row 43: the stairs' right wall
EAST_END = (19, 44)                     # Town x 19 from row 44: the stairs' left wall
# The west wall's pieces carry a soft cast shadow down their left edge (3-6 px, alpha 89,
# 66 in winter): in Town it falls on the stair treads; on open grass or snow it read as a
# flat bar. The east wall has none. So the west wall is laid from copies of the game's
# own pieces, each season's, with that band taken off, in the Vale's seasonal sheet.
BAND_W = 6                              # how far in from the left edge the band can reach
# The fall's rows on ours (y 0-8): (Town falls row, animation frames on). Sheet row 6
# (Town row 2) is the falling water repeated; the rows above it start 4 frames on each.
FALL_ROWS = ((0, 8), (1, 8), (2, 8), (2, 4), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0))
ROCK_ROWS = (0, 1, 2, 2, 2, 3)          # the wet rock behind it (Town rows), y 0-5; y 6 is the pool
FALL_X = NOTCH_X - 2                    # the falls' first column (Town x 91 has a fence: from x 92)
WOODS = [(75, 4, 11)]                   # (x, y, Paths marker): a pine before the west end
# River stones in the shallows, at least a tile apart: the game's own, as the Town river has
# them by its falls (Town (95,1) the boulder 1248, (94,0) (93,8) (95,10) the pebbles 1250).
# They're water tiles, not the shore overlays (SHORE_DECOR's only stones are the Forest's one
# pale rock 1299 and pebbles 1325; vanilla puts 1248/1250 in rivers).
STONES = [(84, 9, 1248), (78, 10, 1248), (80, 11, 1250), (83, 12, 1250)]
SOUND = "82 8 5"
SEED = 81

WF_FIRST = 5939                         # the Waterfalls tileset (Town numbering)
POOL_DARK = 1281                        # the Town pool's dark water, under its fall's foot (Town x 92-97 y 4)
POOL_BANK = 212                         # the bank's top over it there
DEPTH_END = (1245, 186)                 # the depth row's east end (bank_end)


def cliff_cells():
    return [(x, y) for y in range(0, FOOT + 1) for x in range(CLIFF_X0, CLIFF_X1 + 1)]


def notch(x):
    return NOTCH_X <= x < NOTCH_X + 4


def design(V, H, W):
    """The corners: the cliff's own corners (inside its columns, above its foot) set
    outright, so every run paints from the same terrain; the notch and the basin water;
    sand round all new water, its east edge as EAST_SAND has it. The orchard's corners
    (x 88 from y 2) aren't touched."""
    for y in range(0, FOOT + 2):
        for x in range(CLIFF_X0 + 1, CLIFF_X1 + 1):
            V[y][x] = "S"
    for y in range(0, FOOT):
        for x in range(NOTCH_X, NOTCH_X + 5):
            V[y][x] = "W"         # the river above the fall; under it, the bank (BASIN_V)
    x0, y0, x1, y1 = BASIN_V
    for y in range(y0, y1 + 1):
        for x in range(x0, CLIFF_X1 + 1):
            V[y][x] = "W" if x <= x1 else "S"     # (set outright: a run before may have had more water)
    for y, t in EAST_SAND.items():
        V[y][CLIFF_X1] = t
    bx0, by0, bx1, by1 = FALLS_BOX
    for y in range(by0, by1 + 2):
        for x in range(bx0, CLIFF_X1 + 1):
            if V[y][x] != "W" and any(V[j][i] == "W" for i in (x - 1, x, x + 1) for j in (y - 1, y, y + 1)
                                      if 0 <= i <= W and 0 <= j <= H):
                V[y][x] = "S"


def main_stack(pat, table):
    """The stack the painter lays for four corners (build_vale.paint_ground's rule):
    plain ground its main tile; a shore the game's commonest piece (its bare version if
    that is common enough), never a lily pad; an edge its main bare piece."""
    stack = bv.SHORE_PIN.get(pat)
    if stack:
        return stack
    found = table.by_pattern.get(pat, [])
    if "W" in pat:
        cands = [c for c in found if c[0][1] not in LILY_PADS] or found
        bare = [c for c in cands if c[0][1] == 0]
        if cands:
            return bare[0][0] if bare and bare[0][1] >= cands[0][1] * 0.25 else cands[0][0]
    bare = [c for c in found if c[0][1] == 0]
    return (bare or found or [(None, 0)])[0][0]


def reset(m, V, ctx):
    """The cliff's cells back to plain ground for their corners, with this piece's own
    layers cleared: what the painter then sees is the same every run."""
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    for x, y in cliff_cells():
        stack = main_stack(pat(x, y), ctx.table)
        if stack:
            m.g["Back"][y][x], m.g["Buildings"][y][x] = stack
        m.g["Back2"][y][x] = 0
        m.g["Paths"][y][x] = 0
    # Ground elsewhere in the box whose corners changed gets the painter's own piece
    # (repaint_terrain would lay the bare one, which for a straight shore is a rarer,
    # rockier piece than the rest of the bank).
    x0, y0, x1, y1 = FALLS_BOX
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            old = bv.GROUND.get((m.g["Back"][y][x], m.g["Buildings"][y][x]))
            if old is not None and old != pat(x, y):
                stack = main_stack(pat(x, y), ctx.table)
                if stack:
                    m.g["Back"][y][x], m.g["Buildings"][y][x] = stack
    for y in range(0, len(FALL_ROWS)):
        for x in range(FALL_X, FALL_X + FALLS[2]):
            for layer in ("Front", "AlwaysFront"):
                if m.g[layer][y][x] in WATERFALLS:
                    m.g[layer][y][x] = 0


def lay_cliff(m, V, ctx, west):
    """The rim, by the cliff rule (build_vale.cliff), its middle rows doubled, its ends
    the stairs' walls. Where the water runs under the face (the notch, the basin, its
    banks) the water stays; a bank's overlay under the face's last row moves to Back2,
    under the face, so its rocky edge still shows below the face's open foot."""
    rnd = random.Random(SEED)
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])

    def wall(x, y):
        if x == CLIFF_X0:
            return west[END_ROWS[y]]
        return ctx.tget("Buildings", EAST_END[0], EAST_END[1] + END_ROWS[y])

    for x in range(CLIFF_X0, CLIFF_X1 + 1):
        end = WEST_END if x == CLIFF_X0 else EAST_END if x == CLIFF_X1 else None
        # the lip: grass behind the edge piece, never shore (the water of the notch stays)
        r = rnd.random()
        if end:
            m.g["Buildings"][0][x] = wall(x, 0)
        elif not notch(x):
            m.g["Buildings"][0][x] = CLIFF_LIP[0][0] if r < CLIFF_LIP[0][1] else CLIFF_LIP[1][0]
        if not notch(x):
            m.g["Back"][0][x] = GRASS
        for k, row in enumerate(FACE_ROWS):
            y = 1 + k
            gid = CLIFF_FACE[x % 2][row]
            if gid in CLIFF_FACE_VARIANT and rnd.random() < 0.08:
                gid = CLIFF_FACE_VARIANT[gid]
            if end:
                gid = wall(x, y)
            if y < FOOT and not end:
                # (an end wall is open at its outer edge: the ground there stays the painter's)
                m.g["Back"][y][x] = (1230 if notch(x) else CLIFF_SHADE[0][0] if y == FOOT - 1 else GRASS)
            elif y < FOOT:
                # an end wall is open at its outer edge: the ground beside it runs on under
                # it (where that ground tiles sideways), else the painter's stays
                ox = x - 1 if x == CLIFF_X0 else x + 1
                beside = bv.GROUND.get((m.g["Back"][y][ox], m.g["Buildings"][y][ox]))
                if beside and not m.g["Buildings"][y][ox] and beside[0] == beside[1] and beside[2] == beside[3]:
                    m.g["Back"][y][x] = m.g["Back"][y][ox]
                elif m.g["Buildings"][y][x]:
                    m.g["Back2"][y][x] = m.g["Buildings"][y][x]
            else:
                p = pat(x, y)
                over = m.g["Buildings"][y][x]
                if over:
                    m.g["Back2"][y][x] = over               # a bank's overlay, under the face
                if "W" not in p and set(p) <= set("GD"):
                    m.g["Back"][y][x] = CLIFF_SHADE[1][0]  # the shading, on grass only
            m.g["Buildings"][y][x] = gid
            m.g["Front"][y][x] = 0
        # the shading's last row, on grass only (sand keeps its own ground; banks stay);
        # none under an end wall, where it would be one square tile of shade
        y = FOOT + 1
        if not end and set(pat(x, y)) <= set("GD"):
            m.g["Back"][y][x] = CLIFF_SHADE[2][0]


def unband(tile):
    """A wall piece without its cast shadow: the see-through pixels left of its
    solid edge, in each row, within BAND_W of the tile's left side."""
    tile = tile.copy()
    px = tile.load()
    for y in range(16):
        solid = [x for x in range(16) if px[x, y][3] == 255]
        for x in range(min(solid[0] if solid else 16, BAND_W)):
            if 0 < px[x, y][3] < 255:
                px[x, y] = (0, 0, 0, 0)
    return tile


def west_wall(m, ctx, exports):
    """The west wall's pieces (Town x 22's, by Town row) without their cast shadow,
    in the Vale's four seasonal sheets: their gids, Town row -> gid. They take this
    piece's own slots if it ran before (read off the wall before it is reset), else
    new ones at the sheets' end."""
    from PIL import Image
    from vale_sculpture import Sheets, re_tileset
    from vale_feature_fixes import FEATURE_FIRST, SEASONS
    sheets = Sheets(ctx.assets)
    rows = sorted(set(END_ROWS))
    first = {r: END_ROWS.index(r) for r in rows}           # the wall's row each piece first sits on
    arts = {}
    for se in SEASONS:
        src = Image.open(os.path.join(exports, "sheets_" + se, "spring_outdoorsTileSheet.png")).convert("RGBA")
        cols = src.width // 16
        for r in rows:
            k = ctx.tget("Buildings", WEST_END[0], WEST_END[1] + r) - 1     # the outdoors sheet is first, at gid 1
            arts[se, r] = unband(src.crop((k % cols * 16, k // cols * 16, k % cols * 16 + 16, k // cols * 16 + 16)))
    # a slot on the wall is reused only if its pixels are already this piece's in every
    # season (never paint over a slot another piece put there); else a new one at the end
    gids, claimed, nxt = {}, set(), sheets.end
    for r in rows:
        v = m.g["Buildings"][first[r]][CLIFF_X0]
        if FEATURE_FIRST <= v < FEATURE_FIRST + sheets.end and v not in claimed and                 all(sheets.im[se].crop(sheets.box(v)).tobytes() == arts[se, r].tobytes() for se in SEASONS):
            gids[r] = v
        else:
            gids[r] = FEATURE_FIRST + nxt
            nxt += 1
        claimed.add(gids[r])
    sheets.grow(max(gids.values()) - FEATURE_FIRST + 1)
    for se in SEASONS:
        for r, gid in gids.items():
            sheets.im[se].paste(arts[se, r], sheets.box(gid)[:2])
    for se in SEASONS:
        sheets.im[se].save(sheets.path(se))
    h = sheets.im["spring"].height
    m.text = re_tileset(m.text, h // 16 * sheets.cols, h)
    return gids


def shift(tid, p, anims):
    """A Waterfalls tile started p frames on: the frame tile its animation is on then."""
    if p == 0 or tid not in anims:
        return tid
    return anims[tid][p]


def lay_fall(m, ctx, anims):
    """The Town falls over the notch, made taller (FALL_ROWS); the wet rock behind it;
    the pool cut out of the face's last row."""
    sx, sy, sw, sh = FALLS
    used = set()
    for y, (row, p) in enumerate(FALL_ROWS):
        for i in range(sw):
            tx = FALL_X + i
            for layer in ("Front", "AlwaysFront"):
                v = ctx.tget(layer, sx + i, sy + row)
                if v in WATERFALLS:
                    t = shift(v - WF_FIRST, p, anims)
                    m.g[layer][y][tx] = WF_FIRST + t
                    if p and t != v - WF_FIRST:
                        used.add((v - WF_FIRST, p))
    for y, row in enumerate(ROCK_ROWS):
        for i in range(sw):
            v = ctx.tget("Buildings", sx + i, sy + row)
            if v in OUTDOORS2 or (row == 0 and v == 126):     # 126: the water spilling over the edge
                m.g["Buildings"][y][FALL_X + i] = v
    for x in range(NOTCH_X, NOTCH_X + 4):
        # where the face's last row was, the bank's depth row runs on under the fall, as the
        # Town pool's bank does (Town x 93-96 y 4: 212 on the pool's dark water 1281, no face
        # row). The depth row's own pieces (185 / 212 on sand) showed through the falling
        # water as a light band.
        m.g["Buildings"][FOOT][x] = POOL_BANK
        m.g["Back2"][FOOT][x] = 0
        m.g["Back"][FOOT][x] = POOL_DARK
    # and over it, as there, the wet rock's last row, where the fall meets the bank (Town
    # x 92-97 y 4, Buildings2): without it the bank's sand showed through the water
    for i in range(sw):
        v = ctx.tget("Buildings2", sx + i, sy + FALL_ROWS[FOOT][0])
        if v in OUTDOORS2:
            m.g["Buildings2"][FOOT][FALL_X + i] = v
    return used


def tileset_anims(text):
    """The Waterfalls tileset's animations: tile id -> its frames' tile ids."""
    ts = re.search(r'<tileset firstgid="5939" name="v16_Waterfalls".*?</tileset>', text, re.S).group(0)
    return {int(t): [int(f) for f in re.findall(r'<frame tileid="(\d+)"', body)]
            for t, body in re.findall(r'<tile id="(\d+)">(.*?)</tile>', ts, re.S)}


def add_anims(text, used, anims):
    """Each (tile, p) in used: the tile's animation started p frames on, on the frame
    tile it starts from. The tileset's tiles stay in id order."""
    m = re.search(r'(<tileset firstgid="5939" name="v16_Waterfalls"[^>]*>\s*<image [^>]*/>\n)(.*?)(  </tileset>)', text, re.S)
    blocks = dict((int(t), b) for b, t in re.findall(r'(    <tile id="(\d+)">.*?</tile>\n)', m.group(2), re.S))
    durations = {}
    for t, body in re.findall(r'<tile id="(\d+)">(.*?)</tile>', m.group(2), re.S):
        durations[int(t)] = re.findall(r'duration="(\d+)"', body)
    for t, p in sorted(used):
        frames = anims[t][p:] + anims[t][:p]
        d = durations[t][p:] + durations[t][:p]
        blocks[frames[0]] = (f'    <tile id="{frames[0]}">\n      <animation>\n'
                             + "".join(f'        <frame tileid="{f}" duration="{dd}" />\n' for f, dd in zip(frames, d))
                             + '      </animation>\n    </tile>\n')
    body = "".join(blocks[k] for k in sorted(blocks))
    return text[:m.start(2)] + body + text[m.end(2):]


def lay_stones(m):
    for x, y, v in STONES:
        assert v not in LILY_PADS
        if (m.g["Back"][y][x], m.g["Buildings"][y][x]) == (bv.WATER, 0):
            m.g["Back"][y][x] = v


def lay_woods(m, ctx):
    for x, y, k in WOODS:
        if not m.g["Buildings"][y][x]:
            m.g["Paths"][y][x] = ctx.paths_first + k


def bank_end(m):
    """The depth row's east end, over the basin's north-east corner where the bank along
    the cliff's foot turns down into the east bank: the game's end piece (Town (97,4): 186
    over the corner where its pool's bank turns down), not plain sand."""
    x, y = BASIN_V[2], BASIN_V[1] - 2
    if bv.GROUND.get((m.g["Back"][y][x], m.g["Buildings"][y][x])) == ("S",) * 4:
        m.g["Back"][y][x], m.g["Buildings"][y][x] = DEPTH_END


def fence_ground(m, V, ctx):
    """Tiles in the box under a prop (Lupini's fence) whose corners changed: the ground
    under the prop follows the corners, the prop stays."""
    x0, y0, x1, y1 = FALLS_BOX
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            b = m.g["Buildings"][y][x]
            if (x, y) in set(cliff_cells()) or not b or (m.g["Back"][y][x], b) in bv.GROUND:
                continue
            if bv.GROUND.get((m.g["Back"][y][x], 0)) not in (None, pat(x, y)):
                stack = main_stack(pat(x, y), ctx.table)
                if stack and stack[1] == 0:
                    m.g["Back"][y][x] = stack[0]


def patch_file(tmx_path, exports):
    import contextlib, io
    with contextlib.redirect_stdout(io.StringIO()):
        ctx = context(tmx_path, exports)
    m = load(tmx_path)
    H, W = len(m.g["Back"]), len(m.g["Back"][0])
    west = west_wall(m, ctx, exports)
    V = corner_grid(m.g, bv.GROUND)
    design(V, H, W)
    reset(m, V, ctx)
    repaint_terrain(m, V, FALLS_BOX, ctx)
    fence_ground(m, V, ctx)
    bank_end(m)
    lay_cliff(m, V, ctx, west)
    anims = tileset_anims(m.text)
    used = lay_fall(m, ctx, anims)
    m.text = add_anims(m.text, used, anims)
    lay_stones(m)
    lay_woods(m, ctx)
    sounds = m.map_props.get("BrookSounds", "").split()
    triples = [" ".join(sounds[i:i + 3]) for i in range(0, len(sounds), 3)]
    if SOUND not in triples:
        triples = [t for t in triples if t.split()[:2] != SOUND.split()[:2]] + [SOUND]
    m.map_props["BrookSounds"] = " ".join(triples)
    save(m, tmx_path)
    print("waterfall: wrote", tmx_path)


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2])
