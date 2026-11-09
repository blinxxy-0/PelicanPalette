"""Checks for the Gilded Vale's in-place map patches (the waterfall, the overlook and
the hidden sculpture; docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md).

Run as: python tools/test_vale_pieces.py
The vanilla exports come from VALE_EXPORTS (default: .devgame/exports_vale beside the
main checkout). Each test_* runs in turn; the script exits non-zero on the first failure.
"""
import contextlib, io, os, shutil, subprocess, sys, tempfile, traceback

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import build_vale
from build_vale import VALE_PIECES, W, H, WATERFALLS
from vale_patch import BOXES, context, corner_grid, load, repaint_terrain, save

TMX = os.path.join(ROOT, "[CP] Gilded Vale (dev)", "assets", "GildedVale.tmx")
BASE = "e2a31f5"            # the branch's starting commit
EXPORTS = os.environ.get("VALE_EXPORTS", "C:/Users/blinx/Desktop/MuseStardew/.devgame/exports_vale")
CRLF, LF = bytes([13, 10]), bytes([10])
SEASON_SHEETS =[f"{s}_GildedValeBuildings.png" for s in ("spring", "summer", "fall", "winter")]


def run(cmd):
    p = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True)
    return p.stdout + p.stderr


def load_at(rev):
    """The TMX as committed at a revision."""
    from vale_patch import ValeMap
    text = subprocess.run(["git", "show", f"{rev}:{os.path.relpath(TMX, ROOT).replace(os.sep, '/')}"],
                          cwd=ROOT, capture_output=True, check=True).stdout.decode("utf-8")
    with tempfile.TemporaryDirectory() as d:
        path = os.path.join(d, "old.tmx")
        open(path, "w", encoding="utf-8", newline="\n").write(text)
        return load(path)


def all_tiles():
    return [(x, y) for y in range(H) for x in range(W)]


def box_tiles(box):
    x0, y0, x1, y1 = box
    return [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)]


def in_any_box(x, y):
    return any(x0 <= x <= x1 and y0 <= y <= y1 for x0, y0, x1, y1 in BOXES.values())


def saved_bytes(m):
    with tempfile.TemporaryDirectory() as d:
        path = os.path.join(d, "out.tmx")
        save(m, path)
        return open(path, "rb").read()


def run_piece_on_copy(name, times):
    """Runs a piece's patch_file `times` times on a scratch copy of the assets folder;
    returns the TMX and the four season sheets as bytes."""
    import importlib
    with tempfile.TemporaryDirectory() as d:
        assets = os.path.join(d, "assets")
        shutil.copytree(os.path.dirname(TMX), assets)
        tmx = os.path.join(assets, os.path.basename(TMX))
        for _ in range(times):
            importlib.import_module(name).patch_file(tmx, EXPORTS)
        return [open(os.path.join(assets, f), "rb").read() for f in [os.path.basename(TMX)] + SEASON_SHEETS]


def test_round_trip():
    # load then save, unchanged, gives the committed file byte for byte (the checkout may
    # carry CRLF line ends, autocrlf; the committed file and every save are LF)
    assert saved_bytes(load(TMX)) == open(TMX, "rb").read().replace(CRLF, LF)


def test_outside_boxes_unchanged():
    old, new = load_at(BASE), load(TMX)
    for layer in new.g:
        for x, y in all_tiles():
            if not in_any_box(x, y):
                assert old.g[layer][y][x] == new.g[layer][y][x], (layer, x, y)
    assert {k: v for k, v in old.props.items() if not in_any_box(k[1], k[2])} == \
           {k: v for k, v in new.props.items() if not in_any_box(k[1], k[2])}


def test_no_town_sheet_on_back():
    # town sheet gids 2040-4343 on Back go white or blue in winter
    new, old = load(TMX), load_at(BASE)
    for name in ("falls", "overlook"):
        for x, y in box_tiles(BOXES[name]):
            v = new.g["Back"][y][x]
            assert not 2040 <= v < 4344 or old.g["Back"][y][x] == v, (name, x, y, v)   # new ground only


def test_idempotent():
    # every piece in build_vale.VALE_PIECES, run twice on a scratch copy of the
    # assets folder, leaves the TMX and the four season sheets byte-identical
    for name in VALE_PIECES:
        once = run_piece_on_copy(name, times=1)
        twice = run_piece_on_copy(name, times=2)
        assert once == twice, name


def test_routes():
    out = run([sys.executable, "tools/vale_routes.py", TMX])
    assert out.rstrip().endswith("0 blocked"), out


def test_repaint_unchanged():
    # repainting stretches of existing bank from their own corners changes no tile: the
    # river's straight run (its top, from the cap down; further down, the cycle of the
    # long bank re-phases against the cliff lip and the log bridge laid after the ground)
    # and the lake's east shore
    ctx = context(TMX, EXPORTS)
    for box in ((57, 10, 67, 16), (78, 56, 86, 66)):
        m = load(TMX)
        before = {l: [row[:] for row in rows] for l, rows in m.g.items()}
        repaint_terrain(m, corner_grid(m.g, build_vale.GROUND), box, ctx)
        for layer in m.g:
            for x, y in all_tiles():
                assert before[layer][y][x] == m.g[layer][y][x], (box, layer, x, y)


def test_repaint_changes_only_the_box():
    # a changed corner repaints the tiles round it inside the box, and nothing outside
    ctx = context(TMX, EXPORTS)
    m = load(TMX)
    V = corner_grid(m.g, build_vale.GROUND)
    grass = ("G",) * 4
    box = BOXES["overlook"]
    spot = next((x, y) for x, y in box_tiles(box)
                if all(build_vale.GROUND.get((m.g["Back"][y + j][x + i], m.g["Buildings"][y + j][x + i])) == grass
                       for i in (-1, 0, 1) for j in (-1, 0, 1) if 0 <= x + i < W and 0 <= y + j < H)
                and x - 1 >= box[0] and y - 1 >= box[1] and x + 1 <= box[2] and y + 1 <= box[3])
    before = {l: [row[:] for row in rows] for l, rows in m.g.items()}
    V[spot[1]][spot[0]] = "S"                      # the tile's own top-left corner goes to sand
    repaint_terrain(m, V, box, ctx)
    changed = {(x, y) for x, y in all_tiles() if before["Back"][y][x] != m.g["Back"][y][x]
               or before["Buildings"][y][x] != m.g["Buildings"][y][x]}
    assert spot in changed, spot
    assert all(in_any_box(x, y) and box[0] <= x <= box[2] and box[1] <= y <= box[3] for x, y in changed), changed


def test_sculpture_feet_only():
    m = load(TMX)
    feature = lambda v: 20000 <= v < 30000
    assert all(feature(m.g["Buildings"][45][x]) for x in (6, 7))      # the feet: solid
    assert any(feature(m.g["Front"][44][x]) for x in (6, 7))           # the head and hedge top: over you
    assert not m.g["Buildings"][44][6] and not m.g["Buildings"][44][7] # nothing solid up there
    assert m.props[("Buildings", 7, 45)]["Action"] == "Message blinxxy.MuseArt_Vale.garden.hidden_statue"


def test_falls_laid():
    m = load(TMX)
    assert any(m.g["Front"][y][x] in WATERFALLS or m.g["AlwaysFront"][y][x] in WATERFALLS
               for x in range(78, 86) for y in range(0, 8))                # falling water over the notch
    # the brook, and the falls' own big-waterfall sound (AmbientLocationSounds.sound_waterfall_big = 5,
    # as the Town falls has it: "94 5 5")
    assert "82 8 5" in m.map_props["BrookSounds"] and "60 3 0" in m.map_props["BrookSounds"]
    for x in range(76, 88):                                                # the rim: solid all the way across
        assert any(m.g["Buildings"][y][x] for y in range(0, 7)), x


def test_falls_animated():
    # every animated waterfall tile the falls use is animated in the map's tileset: the
    # rows repeated to make the fall taller are the game's own tiles, their animation
    # started a few frames on so each row runs on from the one above it
    import re
    m = load(TMX)
    ts = re.search(r'<tileset firstgid="5939" name="v16_Waterfalls".*?</tileset>', m.text, re.S).group(0)
    anims = {int(t): re.findall(r'<frame tileid="(\d+)"', body)
             for t, body in re.findall(r'<tile id="(\d+)">(.*?)</tile>', ts, re.S)}
    frames = {int(f) for fs in anims.values() for f in fs}
    used = {m.g[l][y][x] - 5939 for l in ("Front", "AlwaysFront") for x, y in box_tiles(BOXES["falls"])
            if m.g[l][y][x] in WATERFALLS}
    assert used
    for t in used:
        assert t in anims or t not in frames, t       # an animation frame never stands alone
    for t, fs in anims.items():
        assert int(fs[0]) == t and len(fs) == 17, t


DATA = os.path.join(ROOT, "[CP] Gilded Vale (dev)", "data")
LUPINI_SCHEDULE = os.path.join(DATA, "lupini_schedule.json")
EMMETT_SCHEDULE = os.path.join(DATA, "emmett_schedule.json")


def load_jsonc(path):
    """A Content Patcher data file: JSON with // comment lines."""
    import json
    lines = [l for l in open(path, encoding="utf-8").read().split("\n") if not l.lstrip().startswith("//")]
    return json.loads("\n".join(lines))


def test_overlook_feet_only():
    from vale_overlook import BENCH, EASELS, SHED
    m = load(TMX)
    bx, by = BENCH
    assert m.g["Buildings"][by][bx] or m.g["Front"][by][bx]                   # the bench is there
    ex0, ex1, ey = EASELS
    assert ey == by
    for x in range(ex0, ex1 + 1):                                              # the easels' row is open
        assert not m.g["Buildings"][by][x] and not m.g["Paths"][by][x], x
    x0, y0, x1, y1 = SHED                                                      # its tiles, inclusive; y1 is its base row
    for y in range(y0, y1):                                                    # the roof and upper wall: over you, not solid
        assert all(not m.g["Buildings"][y][x] for x in range(x0, x1 + 1)), y
        assert all(m.g["Front"][y][x] or m.g["AlwaysFront"][y][x] for x in range(x0, x1 + 1)), y
    for y in range(y0, y1 - 1):                                                # two or more rows above the base: AlwaysFront
        assert all(m.g["AlwaysFront"][y][x] for x in range(x0, x1 + 1)), y
    assert all(m.g["Buildings"][y1][x] for x in range(x0, x1 + 1))             # the walls' foot: solid


def test_overlook_paint_spot_moved():
    # the lake painting spot's two pieces are on the bluff, their PaintSpot actions with
    # them, none left where reachable_spot had put them (88,58) and (88,61)
    from vale_overlook import ROCK, STOOL
    m = load(TMX)
    spot = {(x, y) for (layer, x, y), p in m.props.items()
            if layer == "Buildings" and p.get("Action") == "blinxxy.MuseArt_PaintSpot" and in_any_box(x, y)}
    want = {(STOOL[0] + i, STOOL[1]) for i in (0, 1)} | {(ROCK[0] + i, ROCK[1]) for i in (0, 1)}
    assert spot == want, spot
    assert all(m.g["Buildings"][y][x] for x, y in want)


def test_schedules_moved():
    from vale_overlook import BENCH, LUPINI_STAND
    lu, em = load_jsonc(LUPINI_SCHEDULE), load_jsonc(EMMETT_SCHEDULE)
    assert f"1400 blinxxy.MuseArt_GildedVale {BENCH[0]} {BENCH[1]}" in lu["Sun"]
    assert f"1300 blinxxy.MuseArt_GildedVale {BENCH[0]} {BENCH[1]}" in em["Sat"]
    assert "88 59" not in lu["Fri"]
    assert f"900 blinxxy.MuseArt_GildedVale {LUPINI_STAND[0]} {LUPINI_STAND[1]} 3" in lu["Fri"]   # beside his stool, facing the lake


def crown(x, r):
    """The tiles a tree's crown covers (a Paths-marker tree is 3 wide, 6 tall over its trunk)."""
    return {(x + i, r - k) for i in (-1, 0, 1) for k in range(2, 6)}


def test_overlook_trees_and_fringe():
    # the Three Trees stand inward, their crowns on the map and clear of the bench, the
    # easels' row and the shed's roof; the stump is beside them; a fringe of woods stays
    # along the east and south edges, clear of the trail and the steps' foot
    from vale_overlook import BENCH, EASELS, FRINGE, SHED, STUMP, TREES
    m, paths_first = load(TMX), 1976
    x0, y0, x1, y1 = SHED
    shed = {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)}
    ex0, ex1, ey = EASELS
    keep_clear = shed | {(BENCH[0], BENCH[1] + d) for d in (-1, 0, 1)} | {(x, ey) for x in range(ex0, ex1 + 1)}
    for x, r, k in TREES:
        assert m.g["Paths"][r][x] == paths_first + k, (x, r)
        assert 91 <= x <= 94, (x, r)
        assert all(0 <= cx < W for cx, _ in crown(x, r)), (x, r)
        assert not crown(x, r) & keep_clear, (x, r, crown(x, r) & keep_clear)
    sx, sy, _ = STUMP
    assert min(abs(sx - x) + abs(sy - r) for x, r, _ in TREES) <= 2
    east = [(x, y) for x, y, _ in FRINGE if x == 95]
    south = [(x, y) for x, y, _ in FRINGE if y >= 72]
    assert len(east) >= 3 and len(south) >= 4, FRINGE
    for x, y, k in FRINGE:
        assert m.g["Paths"][y][x] == paths_first + k, (x, y)
    # nothing stands in the woods north of the rim, above the easels
    assert not any(m.g["Paths"][y][x] for y in (53, 54) for x in range(88, 96))


def test_overlook_props_inside():
    # the painting spot and the flowers stand at least a tile in from the side and the
    # face's lip; the bench alone stands against the side, facing over the lake
    from vale_overlook import FACE_TOP, FLOWERS, RIM_Y, ROCK, SIDE_X, STOOL
    for sx, sy in (STOOL, ROCK):
        assert sx >= SIDE_X + 2 and sy - 1 >= RIM_Y + 2 and sy <= FACE_TOP - 2, (sx, sy)
    for x, y, _ in FLOWERS:
        assert x >= SIDE_X + 2 and RIM_Y + 2 <= y <= FACE_TOP - 2, (x, y)


def test_overlook_lake_corner():
    # the lake's east bank ends as Town's shore at x 85 does (rows 67-70): the rock run's
    # cycle carries on under the cap to its last two pieces, (5226,236) and (1230,236), then
    # 261 turns it onto the diagonal's 214 (no 211: Town closes a run with that only after
    # 5229 / 5306 / 5322, and alone under a plain piece it was a broken triangle)
    m = load(TMX)
    assert (m.g["Back"][68][84], m.g["Buildings"][68][84]) == (5226, 236)
    assert (m.g["Back"][69][84], m.g["Buildings"][69][84]) == (1230, 236)
    assert (m.g["Back"][70][84], m.g["Buildings"][70][84]) == (1230, 261)
    assert (m.g["Back"][71][83], m.g["Buildings"][71][83]) == (1230, 214)


def test_decor_rebuild_unchanged_outside_overlook():
    # vale_decor shares one random generator: the rebuild's decor outside the overlook's
    # box is what it was before Task 5 (b17e01a), tile for tile
    import importlib.util
    old_src = subprocess.run(["git", "show", "b17e01a:tools/vale_decor.py"], cwd=ROOT,
                             capture_output=True, check=True).stdout.decode("utf-8")
    grids = []
    for src in (old_src, open(os.path.join(HERE, "vale_decor.py"), encoding="utf-8").read()):
        mod = importlib.util.module_from_spec(importlib.util.spec_from_loader("decor_copy", loader=None))
        exec(compile(src, "vale_decor_copy", "exec"), mod.__dict__)
        g = {l: [[0] * W for _ in range(H)] for l in build_vale.LAYERS}
        for row in g["Back"]:
            row[:] = [176] * W
        with contextlib.redirect_stdout(io.StringIO()):
            mod.decor(g, {(176, 0): ("G",) * 4}, 1976, W, H)
        grids.append(g)
    old, new = grids
    for layer in old:
        for x, y in all_tiles():
            if not in_any_box(x, y):
                assert old[layer][y][x] == new[layer][y][x], (layer, x, y)


if __name__ == "__main__":
    for name, fn in [(n, f) for n, f in list(globals().items()) if n.startswith("test_") and callable(f)]:
        try:
            with contextlib.redirect_stdout(io.StringIO()) if name.startswith("test_repaint") else contextlib.nullcontext():
                fn()
        except Exception:
            traceback.print_exc()
            print("FAIL", name)
            sys.exit(1)
        print("ok  ", name)
