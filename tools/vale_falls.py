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
  left wall (Town x 19, rows 44-48: a rounded right edge), with their middle rows
  repeated as the face's are. Trees stand in front: the woods at the top edge, a pine
  added below them at the west end, and the orchard's own trees at the east end.

Laid in order: the corners (the notch and the basin, sand round them), the ground by
the painter's rules (vale_patch.repaint_terrain), the cliff, the fall, the river
stones, and the sound (AmbientLocationSounds.sound_waterfall_big = 5, as the Town
falls has it).

Run as: python tools/vale_falls.py <GildedVale.tmx> <exports>   (build_vale.VALE_PIECES)
Re-runnable: the cliff's cells are put back to plain ground for their corners first,
so each run paints from the same start.
"""
import os, random, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv
from build_vale import (CLIFF_FACE, CLIFF_FACE_VARIANT, CLIFF_LIP, CLIFF_SHADE, FALLS, GRASS,
                        OUTDOORS2, LILY_PADS, WATERFALLS)
from vale_patch import FALLS_BOX, context, corner_grid, load, repaint_terrain, save

NOTCH_X = 80                 # the fall's first water column (water x 80-83, the river's width)
BASIN_V = (78, 6, 86, 12)    # the basin's water corners
CLIFF_X0, CLIFF_X1 = 76, 87  # the rim's end columns: into the woods west, against the orchard east
FOOT = 6                     # the face's last row; its shading is on FOOT + 1
FACE_ROWS = (0, 1, 1, 2, 2, 3)          # CLIFF_FACE's rows for y 1-6 (its middle rows doubled)
END_ROWS = (0, 1, 2, 2, 3, 3, 4)        # the end walls' Town rows for y 0-6 (middle rows doubled)
WEST_END = (22, 43)                     # Town x 22 from row 43: the stairs' right wall
EAST_END = (19, 44)                     # Town x 19 from row 44: the stairs' left wall
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
STONES = [(85, 8, 1248), (78, 9, 1248), (80, 11, 1250), (85, 11, 1250)]
SOUND = "82 8 5"
SEED = 81

WF_FIRST = 5939                         # the Waterfalls tileset (Town numbering)


def cliff_cells():
    return [(x, y) for y in range(0, FOOT + 1) for x in range(CLIFF_X0, CLIFF_X1 + 1)]


def notch(x):
    return NOTCH_X <= x < NOTCH_X + 4


def design(V, H, W):
    """The corners: the cliff's own corners (inside its columns, above its foot) set
    outright, so every run paints from the same terrain; the notch and the basin water;
    sand round all new water. The orchard's corners (x 88 from y 2) aren't touched."""
    for y in range(0, FOOT + 2):
        for x in range(CLIFF_X0 + 1, CLIFF_X1 + 1):
            V[y][x] = "S"
    for y in range(0, FOOT + 2):
        for x in range(NOTCH_X, NOTCH_X + 5):
            V[y][x] = "W"
    x0, y0, x1, y1 = BASIN_V
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            V[y][x] = "W"
    # Its north-west corner, where the land is above the water, is cut on the diagonal
    # (section 5): the river's north bank runs on into it, as the inner bend did.
    V[y0][x0] = "S"
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


def lay_cliff(m, V, ctx):
    """The rim, by the cliff rule (build_vale.cliff), its middle rows doubled, its ends
    the stairs' walls. Where the water runs under the face (the notch, the basin, its
    banks) the water stays; a bank's overlay under the face's last row moves to Back2,
    under the face, so its rocky edge still shows below the face's open foot."""
    rnd = random.Random(SEED)
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
    for x in range(CLIFF_X0, CLIFF_X1 + 1):
        end = WEST_END if x == CLIFF_X0 else EAST_END if x == CLIFF_X1 else None
        # the lip: grass behind the edge piece, never shore (the water of the notch stays)
        r = rnd.random()
        if end:
            m.g["Buildings"][0][x] = ctx.tget("Buildings", end[0], end[1] + END_ROWS[0])
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
                gid = ctx.tget("Buildings", end[0], end[1] + END_ROWS[1 + k])
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
        # the shading's last row, on grass only (sand keeps its own ground; banks stay)
        y = FOOT + 1
        if set(pat(x, y)) <= set("GD"):
            m.g["Back"][y][x] = CLIFF_SHADE[2][0]


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
        m.g["Buildings"][FOOT][x] = 0                         # the pool, where the face's last row was
        m.g["Back2"][FOOT][x] = 0
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
    V = corner_grid(m.g, bv.GROUND)
    design(V, H, W)
    reset(m, V, ctx)
    repaint_terrain(m, V, FALLS_BOX, ctx)
    fence_ground(m, V, ctx)
    lay_cliff(m, V, ctx)
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
