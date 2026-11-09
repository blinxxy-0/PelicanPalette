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
