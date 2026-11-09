"""Offline checks for the painters' guide's maps (the Vale painters' guide design,
docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md):
the Gilded Grove (section 5) and the way back to the Vale (section 3).

Run as: python tools/test_vale_guide_map.py
Each test_* runs in turn; the script exits non-zero on the first failure.
"""
import collections, json, os, re, sys, traceback
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
from render_tmx import layer_data

PACK = os.path.join(ROOT, "[CP] Gilded Vale (dev)")
GROVE_TMX = os.path.join(PACK, "assets", "GildedGrove.tmx")
VALE_TMX = os.path.join(PACK, "assets", "GildedVale.tmx")
GUIDE_JSON = os.path.join(PACK, "data", "vale_guide.json")
GROVE_CS = os.path.join(ROOT, "ArtStudio", "Vale", "GildedGrove.cs")
ECHOES = ("agnes", "hollis", "mabel", "bram")
SOLID_MARKERS = {9, 10, 11, 24, 25, 26}     # Paths: oak, maple, pine; large, medium, small bush
GROVE_SHEET_FIRST = 40000                   # the Grove's own sheet's first gid (build_vale_grove.SHEET_FIRST)


class Map:
    """A TMX's layers, its tile properties (from its tilesets) and its TileData."""

    def __init__(self, path):
        root = ET.parse(path).getroot()
        self.w, self.h = int(root.get("width")), int(root.get("height"))
        self.props = {p.get("name"): p.get("value") for p in root.findall("properties/property")}
        self.layers = {l.get("name"): layer_data(l.find("data")) for l in root.findall("layer")}
        self.tile_props = {}               # gid -> {name: value}
        self.animations = {}               # gid -> [gids of its frames]
        self.paths_first = None
        for ts in root.findall("tileset"):
            first = int(ts.get("firstgid"))
            if ts.get("name") == "Paths":
                self.paths_first = first
            for t in ts.findall("tile"):
                p = {q.get("name"): q.get("value") for q in t.findall("properties/property")}
                if p:
                    self.tile_props[first + int(t.get("id"))] = p
                frames = [int(f.get("tileid")) for f in t.findall("animation/frame")]
                if frames:
                    self.animations[first + int(t.get("id"))] = [first + f for f in frames]
        self.tile_data = {}                # (layer, x, y) -> {name: value}
        for group in root.findall("objectgroup"):
            for o in group.findall("object"):
                if o.get("name") == "TileData":
                    x, y = int(float(o.get("x"))) // 16, int(float(o.get("y"))) // 16
                    self.tile_data[(group.get("name"), x, y)] = {q.get("name"): q.get("value") for q in o.findall("properties/property")}

    def at(self, layer, x, y):
        return self.layers.get(layer, [0] * (self.w * self.h))[y * self.w + x]

    def water(self, x, y):
        return self.tile_props.get(self.at("Back", x, y), {}).get("Water") == "T"

    def walkable(self, x, y):
        if not (0 <= x < self.w and 0 <= y < self.h):
            return False
        b = self.at("Buildings", x, y)
        if b and self.tile_props.get(b, {}).get("Passable") != "T":
            return False
        p = self.at("Paths", x, y)
        if p and self.paths_first and p - self.paths_first in SOLID_MARKERS:
            return False
        return not self.water(x, y)

    def reach(self, start):
        seen, q = {start}, collections.deque([start])
        while q:
            x, y = q.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (x + dx, y + dy)
                if n not in seen and self.walkable(*n):
                    seen.add(n)
                    q.append(n)
        return seen

    def actions(self, name):
        """Tiles whose Buildings Action starts with the action's name: {(x, y): args}."""
        out = {}
        for (layer, x, y), p in self.tile_data.items():
            words = p.get("Action", "").split()
            if layer == "Buildings" and words and words[0] == name:
                out[(x, y)] = words[1:]
        return out


def json_with_comments(path):
    text = re.sub(r"^\s*//.*$", "", open(path, encoding="utf-8").read(), flags=re.M)
    return json.loads(text)


def guide_entries(target):
    data = json_with_comments(GUIDE_JSON)
    return next(c["Entries"] for c in data["Changes"] if c.get("Target") == target)


def cs_point(name):
    m = re.search(name + r"\s*=\s*new\s*(?:Point)?\(\s*(\d+)\s*,\s*(\d+)\s*\)", open(GROVE_CS, encoding="utf-8").read())
    assert m, f"{name} not found in GildedGrove.cs"
    return int(m.group(1)), int(m.group(2))


def grove():
    return Map(GROVE_TMX)


def arrival():
    return cs_point("ArrivalTile")


# ---- The Grove ----

def test_grove_size():
    m = grove()
    assert 25 <= m.w <= 35 and 25 <= m.h <= 35, (m.w, m.h)


def test_grove_closed():
    m = grove()
    border = [(x, y) for x in range(m.w) for y in (0, m.h - 1)] + [(x, y) for y in range(m.h) for x in (0, m.w - 1)]
    open_ = [p for p in border if m.walkable(*p)]
    assert not open_, open_


def echo_feet(m):
    """{id: [foot tiles, left to right]}: an echo is 2 x 3 tiles, and both tiles of its foot carry its action."""
    out = collections.defaultdict(list)
    for p, a in sorted(m.actions("blinxxy.MuseArt_Echo").items()):
        out[" ".join(a)].append(p)
    return dict(out)


def test_grove_actions():
    # one frame home, one hidden easel, one Founders' easel, and an echo for each buried artist,
    # its 2-wide foot both tiles of it
    m = grove()
    assert len(m.actions("blinxxy.MuseArt_GroveFrame")) == 1
    assert len(m.actions("blinxxy.MuseArt_HiddenEasel")) == 1
    assert len(m.actions("blinxxy.MuseArt_FoundersEasel")) == 1
    feet = echo_feet(m)
    assert sorted(feet) == sorted(ECHOES), feet
    for name, tiles in feet.items():
        (x, y) = tiles[0]
        assert tiles == [(x, y), (x + 1, y)], f"{name}: foot {tiles}"


def test_echo_tiles_animated():
    # each echo's six tiles (its foot on Buildings, the two rows above on Front) are TMX animations of
    # 2+ frames, the first frame the tile itself, all at the same speed
    m = grove()
    for name, tiles in echo_feet(m).items():
        x, y = tiles[0]
        for layer, ty in (("Buildings", y), ("Front", y - 1), ("Front", y - 2)):
            for tx in (x, x + 1):
                gid = m.at(layer, tx, ty)
                frames = m.animations.get(gid, [])
                assert len(frames) >= 2 and len(set(frames)) >= 2, f"{name} {layer} {tx},{ty} tile {gid}: {frames}"
                assert frames[0] == gid, f"{name} {layer}: first frame should be the tile itself"
        assert not m.at("Front", x, y - 3) and not m.at("Front", x + 1, y - 3), f"{name}: something over its head"


def piece_tiles(m):
    """{(x, y): name} for every tile of the Grove's pieces: the action tiles on Buildings, and the
    Grove-sheet tiles on Front above them."""
    out = {}
    for name in ("GroveFrame", "HiddenEasel", "FoundersEasel", "Echo"):
        for (x, y), args in m.actions("blinxxy.MuseArt_" + name).items():
            label = f"{name} {' '.join(args)}".strip()
            out[(x, y)] = label
            ty = y - 1
            while ty >= 0 and m.at("Front", x, ty) >= GROVE_SHEET_FIRST:
                out[(x, ty)] = label
                ty -= 1
    return out


def test_tall_feet_only():
    # only a piece's bottom row is solid; the rest is on Front, so you walk behind it
    m = grove()
    for (x, y), name in piece_tiles(m).items():
        if m.at("Buildings", x, y) >= GROVE_SHEET_FIRST:
            assert not m.walkable(x, y), (name, x, y)
        else:
            assert m.walkable(x, y), f"{name}: its top at {x},{y} should be walkable"


def test_pieces_have_room():
    # no piece in the woods or under their canopy (AlwaysFront), in the pool, or on another piece
    m = grove()
    tiles = piece_tiles(m)
    for (x, y), name in tiles.items():
        for i in (-1, 0, 1):
            for j in (-1, 0, 1):
                assert not m.at("AlwaysFront", x + i, y + j), f"{name} at {x},{y}: the woods' canopy beside it"
                f = m.at("Front", x + i, y + j)
                assert not f or f >= GROVE_SHEET_FIRST, f"{name} at {x},{y}: the woods beside it"
        assert not m.water(x, y), (name, x, y)


def test_finished_canvas_on_sheet():
    # GildedGrove.cs draws the Founders' easel's finished canvas from the Grove's sheet: its source
    # tile there is the finished easel's top (the canvas), and the rest of the easel matches the empty one
    from PIL import Image
    text = open(GROVE_CS, encoding="utf-8").read()
    col = int(re.search(r"FinishedCanvasColumn\s*=\s*(\d+)", text).group(1))
    row = int(re.search(r"FinishedCanvasRow\s*=\s*(\d+)", text).group(1))
    sheet = Image.open(os.path.join(PACK, "assets", "GildedGrove.png")).convert("RGBA")
    art = os.path.join(ROOT, "art", "gilded_vale", "props", "vale_guide_final")
    canvas = Image.open(os.path.join(art, "founders_canvas.png")).convert("RGBA")
    empty = Image.open(os.path.join(art, "founders_easel.png")).convert("RGBA")
    got = sheet.crop((col * 16, row * 16, col * 16 + 16, row * 16 + 16))
    assert got.tobytes() == canvas.crop((0, 0, 16, 16)).tobytes(), "the source isn't the finished canvas"
    assert canvas.crop((0, 16, 16, 32)).tobytes() == empty.crop((0, 16, 16, 32)).tobytes(), "the easel's foot differs"
    m = grove()
    (fx, fy), = m.actions("blinxxy.MuseArt_FoundersEasel")
    assert cs_point("FoundersEaselTile") == (fx, fy), (cs_point("FoundersEaselTile"), (fx, fy))
    top = m.at("Front", fx, fy - 1) - GROVE_SHEET_FIRST
    cols = sheet.width // 16
    shown = sheet.crop((top % cols * 16, top // cols * 16, top % cols * 16 + 16, top // cols * 16 + 16))
    assert shown.tobytes() == empty.crop((0, 0, 16, 16)).tobytes(), "the map's easel isn't the empty one"
    (hx, hy), = m.actions("blinxxy.MuseArt_HiddenEasel")
    assert cs_point("HiddenEaselTile") == (hx, hy), (cs_point("HiddenEaselTile"), (hx, hy))


def test_grove_fish_only_here():
    # the painted fish is in the Grove's list and not in the Vale's
    grove_fish = guide_entries("Data/Locations")["blinxxy.MuseArt_GildedGrove"]["Fish"]
    assert any(f.get("ItemId") == "(O)blinxxy.MuseArt.Vale_PaintedFish" for f in grove_fish), grove_fish
    vale = open(os.path.join(PACK, "data", "vale_fish.json"), encoding="utf-8").read()
    assert "PaintedFish" not in vale


def test_grove_reachable():
    m = grove()
    start = arrival()
    assert m.walkable(*start), start
    seen = m.reach(start)
    targets = {}
    for name in ("GroveFrame", "HiddenEasel", "FoundersEasel", "Echo"):
        for p, args in m.actions("blinxxy.MuseArt_" + name).items():
            targets[f"{name} {' '.join(args)}".strip()] = p
    assert len(targets) == 7, targets      # (an echo's two foot tiles count once)
    blocked = {n: p for n, p in targets.items()
               if not any((p[0] + dx, p[1] + dy) in seen for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    assert not blocked, blocked


def test_grove_arrival_agrees():
    # the C# constant and the location data's arrival tile are the same tile
    data = guide_entries("Data/Locations")["blinxxy.MuseArt_GildedGrove"]
    t = data["DefaultArrivalTile"]
    assert (t["X"], t["Y"]) == arrival(), (t, arrival())


def test_grove_context():
    # its own location context (fall all year, always sun), named on the map: Data/Locations
    # has no context field, the game reads the map's LocationContext property
    m = grove()
    assert m.props.get("LocationContext") == "blinxxy.MuseArt_GroveContext", m.props
    assert m.props.get("Outdoors") == "T"
    ctx = guide_entries("Data/LocationContexts")["blinxxy.MuseArt_GroveContext"]
    assert ctx["SeasonOverride"].lower() == "fall"
    assert [w["Weather"] for w in ctx["WeatherConditions"]] == ["Sun"], ctx["WeatherConditions"]
    assert ctx.get("AllowRainTotem") is False


def test_grove_pool_fishable():
    m = grove()
    water = [(x, y) for y in range(m.h) for x in range(m.w) if m.water(x, y)]
    assert len(water) >= 12, len(water)
    seen = m.reach(arrival())
    assert any((x + dx, y + dy) in seen for x, y in water for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))), "no shore to fish from"


def test_grove_no_warps():
    # the frame is the only way out
    m = grove()
    assert "Warp" not in m.props and not any("Warp" in p.get("Action", "") for p in m.tile_data.values())


# ---- The way back ----

GRAVES_CS = os.path.join(ROOT, "ArtStudio", "Vale", "ValeGraves.cs")


def gate_tile():
    """ValeGraves.GraveyardGateTile, where the frame sets you down."""
    t = re.search(r"GraveyardGateTile\s*=\s*new\s*(?:Point)?\(\s*(\d+)\s*,\s*(\d+)\s*\)", open(GRAVES_CS, encoding="utf-8").read())
    assert t, "ValeGraves.GraveyardGateTile not found"
    return int(t.group(1)), int(t.group(2))


def test_return_tile_walkable():
    # the frame home lands on ValeGraves.GraveyardGateTile (GildedGrove has no tile of its own), on open
    # ground in the Vale, just inside the graveyard's gate
    text = open(GROVE_CS, encoding="utf-8").read()
    assert "ReturnTile" not in text and "ValeGraves.GraveyardGateTile" in text, "GildedGrove should land on ValeGraves.GraveyardGateTile"
    x, y = gate_tile()
    vale = Map(VALE_TMX)
    assert vale.walkable(x, y), (x, y)


# ---- The spots on the map (design section 4) ----

GUIDE_CS = os.path.join(ROOT, "ArtStudio", "Vale", "PaintersGuide.cs")
HOUSE_TMX = os.path.join(PACK, "assets", "ValeHouse.tmx")
PAINT_SPOT = "blinxxy.MuseArt_PaintSpot"
FEATURE_FIRST, FEATURE_COLS = 20000, 32         # the Vale's own building sheet (build_vale.BUILD_FIRST, SHEET_COLS)
CLEARING = (3, 10, 12, 22)                      # the old Painter's clearing, now the graveyard's
EXISTING = ("rooftops", "northlight", "wildflowers", "lakewindow")


def guide_ids():
    """The eight spots' ids, in PaintersGuide.Spots."""
    text = open(GUIDE_CS, encoding="utf-8").read()
    start = text.index("Spot[] Spots")
    ids = re.findall(r'new\("(\w+)"', text[start:text.index("};", start)])
    assert len(ids) == 8, ids
    return ids


_PIECES = {}


def spot_pieces(m):
    """{(x, y): 'stool' | 'rock'} for the left foot tile of every painting-spot piece on the
    Vale's map, known by its pixels: the spring sheet's tiles under it are the bottom row of
    the stool's or the rock's art (build_vale PICKS, cut by tile_piece)."""
    from PIL import Image
    if not _PIECES:
        import build_vale as bv
        art = os.path.join(ROOT, "art", "gilded_vale", "props", "vale_props_v2")
        for kind, name in (("stool", "painters_stool_1_3"), ("rock", "paint_rock_clean")):
            im = bv.tile_piece(Image.open(os.path.join(art, name + ".png")).convert("RGBA"), 0)
            _PIECES[kind] = tuple(im.crop((i * 16, 16, i * 16 + 16, 32)).tobytes() for i in (0, 1))
        _PIECES["sheet"] = Image.open(os.path.join(PACK, "assets", "spring_GildedValeBuildings.png")).convert("RGBA")
    sheet = _PIECES["sheet"]
    slots = (sheet.width // 16) * (sheet.height // 16)

    def px(gid):
        i = gid - FEATURE_FIRST
        if not 0 <= i < slots:
            return None
        x, y = i % FEATURE_COLS * 16, i // FEATURE_COLS * 16
        return sheet.crop((x, y, x + 16, y + 16)).tobytes()
    out = {}
    for y in range(m.h):
        for x in range(m.w - 1):
            pair = (px(m.at("Buildings", x, y)), px(m.at("Buildings", x + 1, y)))
            for kind in ("stool", "rock"):
                if pair == _PIECES[kind]:
                    out[(x, y)] = kind
    return out


def spot_actions(m):
    """{(layer, x, y): [args]} for every PaintSpot action, on any layer."""
    out = {}
    for (layer, x, y), p in m.tile_data.items():
        words = p.get("Action", "").split()
        if words and words[0] == PAINT_SPOT:
            out[(layer, x, y)] = words[1:]
    return out


def vale_map():
    return Map(VALE_TMX)


def test_rocks_have_no_action():
    m = vale_map()
    rocks = [p for p, k in spot_pieces(m).items() if k == "rock"]
    assert rocks, "no paint rocks found"
    acting = {(x + dx, y) for x, y in rocks for dx in (0, 1)} & {(x, y) for (_, x, y) in spot_actions(m)}
    assert not acting, f"PaintSpot on a rock: {sorted(acting)}"


def test_every_spot_action_has_a_known_id():
    ids = set(guide_ids())
    bad = {k: a for k, a in spot_actions(vale_map()).items() if k[0] != "Buildings" or len(a) != 1 or a[0] not in ids}
    assert not bad, bad


def test_existing_spots_present():
    # each of the four kept spots is on both foot tiles of exactly one stool, and only there
    m = vale_map()
    stools = [p for p, k in spot_pieces(m).items() if k == "stool"]
    acts = spot_actions(m)
    for sid in EXISTING:
        tiles = sorted((x, y) for (_, x, y), a in acts.items() if a == [sid])
        on = [s for s in stools if tiles == [s, (s[0] + 1, s[1])]]
        assert len(on) == 1, f"{sid}: on {tiles}, stools {stools}"


def test_clearing_spot_gone():
    m = vale_map()
    x0, y0, x1, y1 = CLEARING
    inside = lambda x, y: x0 <= x <= x1 and y0 <= y <= y1
    acts = [k for k in spot_actions(m) if inside(k[1], k[2])]
    pieces = [p for p in spot_pieces(m) if inside(*p) or inside(p[0] + 1, p[1])]
    assert not acts and not pieces, (acts, pieces)
    # nor the pieces' tops, left on Front2 over where they stood
    for x, y in ((8, 14), (9, 14), (9, 17), (10, 17)):
        assert not m.at("Front2", x, y), (x, y, m.at("Front2", x, y))


def _base():
    """What this branch's map is compared with: env VALE_BASE, else where it left main
    (main has moved on since, with map edits of its own)."""
    import subprocess
    if os.environ.get("VALE_BASE"):
        return os.environ["VALE_BASE"]
    p = subprocess.run(["git", "merge-base", "main", "HEAD"], cwd=ROOT, capture_output=True, text=True)
    assert p.returncode == 0 and p.stdout.strip(), p.stderr
    return p.stdout.strip()


def test_only_guide_boxes_changed():
    # compared with main, the map's tiles and TileData change only inside vale_spots' boxes and the
    # graveyard's (vale_patch.BOXES "graveyard")
    import subprocess, tempfile
    from vale_spots import BOXES as SPOT_BOXES
    from vale_patch import BOXES
    rel = os.path.relpath(VALE_TMX, ROOT).replace(os.sep, "/")
    text = subprocess.run(["git", "show", f"{_base()}:{rel}"], cwd=ROOT, capture_output=True, check=True).stdout
    with tempfile.TemporaryDirectory() as d:
        path = os.path.join(d, "old.tmx")
        open(path, "wb").write(text)
        old = Map(path)
    new = vale_map()
    boxes = list(SPOT_BOXES.values()) + [BOXES["graveyard"]]
    inside = lambda x, y: any(x0 <= x <= x1 and y0 <= y <= y1 for x0, y0, x1, y1 in boxes)
    assert set(old.layers) == set(new.layers) and old.props == new.props
    for name in new.layers:
        for y in range(new.h):
            for x in range(new.w):
                if not inside(x, y):
                    assert old.at(name, x, y) == new.at(name, x, y), (name, x, y)
    assert {k: v for k, v in old.tile_data.items() if not inside(k[1], k[2])} == \
           {k: v for k, v in new.tile_data.items() if not inside(k[1], k[2])}


def test_spots_in_routes():
    # vale_routes.PLACES stands in front of each kept stool, on walkable ground
    import vale_routes
    m = vale_map()
    acts = spot_actions(m)
    places = set(vale_routes.PLACES.values())
    for sid in EXISTING:
        x, y = min((x, y) for (_, x, y), a in acts.items() if a == [sid])
        front = [p for p in ((x, y + 1), (x + 1, y + 1)) if p in places]
        assert front and all(m.walkable(*p) for p in front), f"{sid}: in front of ({x}, {y}): {front}"


# ---- The graveyard (design section 6) ----

GRAVE = "blinxxy.MuseArt_Grave"
GRAVE_IDS = ("agnes", "hollis", "mabel", "bram", "ada", "hester", "amos")
# Bram carved the artists' stones; his own is the plain one; the potters' and the rest are clay
GRAVE_ART = {"agnes": "headstone_agnes", "hollis": "headstone_hollis", "mabel": "headstone_mabel", "bram": "stone_bram",
             "ada": "marker_ada", "hester": "marker_hester", "amos": "marker_amos"}
VANILLA_BENCH = {2330, 2331, 2335, 2362, 2363, 2367}     # vale_decor PROPS["bench"]
BUS_STOP = (12, 8)                                         # vale_routes PLACES["bus stop"]
TEXT_MD = os.path.join(ROOT, "docs", "superpowers", "specs", "2026-11-08-vale-guide-text.md")
I18N = os.path.join(PACK, "i18n", "default.json")


class ValeSheets:
    """The Vale's building sheet in its four seasons: a gid's 16 x 16 pixels in each."""

    def __init__(self):
        from PIL import Image
        self.im = {se: Image.open(os.path.join(PACK, "assets", f"{se}_GildedValeBuildings.png")).convert("RGBA")
                   for se in ("spring", "summer", "fall", "winter")}

    def px(self, se, gid):
        i = gid - FEATURE_FIRST
        im = self.im[se]
        if not 0 <= i < (im.width // 16) * (im.height // 16):
            return None
        x, y = i % FEATURE_COLS * 16, i // FEATURE_COLS * 16
        return im.crop((x, y, x + 16, y + 16)).tobytes()


_GY = {}


def graveyard():
    """(map, {(layer, x, y): {piece names}}) for the Vale's map: every graveyard piece laid, known by
    its pixels in all four sheets (vale_graveyard.tiles(): spring's art in spring, summer and fall,
    winter's in winter). A tile can be more than one piece: the three carved headstones' tops are
    the same stone (their carvings are lower down), so they share a slot."""
    if not _GY:
        import vale_graveyard as vg
        m, sheets = vale_map(), ValeSheets()
        art = {name: {se: im.tobytes() for se, im in seasons.items()} for name, seasons in vg.tiles().items()}
        by_spring = collections.defaultdict(list)
        for name, seasons in art.items():
            by_spring[seasons["spring"]].append(name)
        found = {}
        x0, y0, x1, y1 = vg.RECT
        for layer in ("Buildings", "Front", "Front2", "AlwaysFront", "Back2", "Buildings2"):
            for y in range(y0 - 3, y1 + 3):
                for x in range(x0 - 3, x1 + 3):
                    gid = m.at(layer, x, y)
                    for name in by_spring.get(sheets.px("spring", gid), []):
                        if all(sheets.px(se, gid) == art[name][se] for se in art[name]):
                            found.setdefault((layer, x, y), set()).add(name)
        _GY.update(map=m, pieces=found, vg=vg)
    return _GY["map"], _GY["pieces"]


def ring(rect):
    x0, y0, x1, y1 = rect
    return [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1) if x in (x0, x1) or y in (y0, y1)]


def strictly_inside(rect, x, y):
    x0, y0, x1, y1 = rect
    return x0 < x < x1 and y0 < y < y1


def grave_actions(m):
    return {p: a for p, a in m.actions(GRAVE).items()}


def test_graves():
    # exactly seven Grave actions, one per id, on the foot of that person's stone or marker, all inside
    # the wall; each with an urn (glazed in their colour) on the tile at its foot
    import vale_graveyard as vg
    m, pieces = graveyard()
    acts = grave_actions(m)
    assert sorted(" ".join(a) for a in acts.values()) == sorted(GRAVE_IDS), acts
    for (x, y), (gid,) in acts.items():
        assert strictly_inside(vg.RECT, x, y), (gid, x, y)
        assert pieces.get(("Buildings", x, y)) == {GRAVE_ART[gid] + ".foot"}, (gid, x, y, pieces.get(("Buildings", x, y)))
        assert pieces.get(("Buildings", x, y + 1)) == {"urn_" + gid}, (gid, "urn", x, y + 1, pieces.get(("Buildings", x, y + 1)))
    kinds = collections.Counter(GRAVE_ART[g].split("_")[0] for g in GRAVE_IDS)
    assert kinds == {"headstone": 3, "stone": 1, "marker": 3}, kinds


def test_wall_closed_but_gate():
    # the wall's ring is solid all round but for the gate, which is walkable from the bus stop; with
    # the gate shut, nothing inside can be reached
    import vale_graveyard as vg
    m, pieces = graveyard()
    gate = set(vg.GATE)
    for x, y in ring(vg.RECT):
        if (x, y) in gate:
            assert m.walkable(x, y), ("gate", x, y)
        else:
            assert not m.walkable(x, y), ("wall open", x, y)
            names = pieces.get(("Buildings", x, y), set())
            assert names and all(n.startswith(("wall_", "gatepost")) for n in names), ("not wall", x, y, names)
    seen = m.reach(BUS_STOP)
    assert gate <= seen, "the gate isn't reachable from the bus stop"
    gx, gy = gate_tile()
    assert gx in {x for x, _ in gate} and gy == vg.RECT[1] + 1 and (gx, gy) in seen, ("gate tile", (gx, gy))
    shut = Map(VALE_TMX)
    inside_tiles = [(x, y) for x in range(vg.RECT[0] + 1, vg.RECT[2]) for y in range(vg.RECT[1] + 1, vg.RECT[3])]
    walk = shut.walkable
    shut.walkable = lambda x, y: (x, y) not in gate and walk(x, y)
    assert not set(inside_tiles) & shut.reach(BUS_STOP), "the wall has a gap besides the gate"


def test_nothing_on_the_wall():
    # every stone, marker, urn and the bench well inside the wall; the ring carries only the wall's own
    # pieces (nothing on Front over it), and no tree, bush or long grass is on the wall or inside it
    import vale_graveyard as vg
    m, pieces = graveyard()
    x0, y0, x1, y1 = vg.RECT
    props = [(x, y, names) for (layer, x, y), names in pieces.items() if not any(n.startswith(("wall_", "gatepost")) for n in names)]
    assert len(props) == 7 * 3, props               # each stone's top and foot, and its urn
    bench = [(x, y) for y in range(y0 - 2, y1 + 3) for x in range(x0 - 2, x1 + 3)
             if m.at("Buildings", x, y) in VANILLA_BENCH or m.at("Front", x, y) in VANILLA_BENCH]
    assert len(bench) == 6, bench
    for x, y, *name in props + [(x, y, "bench") for x, y in bench]:
        assert strictly_inside(vg.RECT, x, y), (name, x, y)
    for x, y in ring(vg.RECT):
        for layer in ("Front", "Front2", "AlwaysFront", "Back2"):
            assert not m.at(layer, x, y), (layer, x, y)
    posts = pieces_at(pieces, "gatepost.top")
    assert sorted(posts) == sorted((x, y0 - 1) for x, y in pieces_at(pieces, "gatepost.foot")), posts
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            assert not m.at("Paths", x, y), ("marker", x, y, m.at("Paths", x, y) - m.paths_first)
        # nor a tree right beside the west wall: the woods' crowns hid it
        p = m.at("Paths", x0 - 1, y)
        assert not (p and p - m.paths_first in (9, 10, 11)), ("tree beside the west wall", x0 - 1, y)


def test_graveyard_open_inside():
    # every tile strictly inside the wall that isn't a stone's or marker's foot, an urn, or the bench's
    # seat is walkable (no invisible wall, outdoors 103, left in the grass)
    import vale_graveyard as vg
    m, pieces = graveyard()
    x0, y0, x1, y1 = vg.RECT
    solid = {(x, y) for (layer, x, y), names in pieces.items() if layer == "Buildings"}
    solid |= {(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1) if m.at("Buildings", x, y) in VANILLA_BENCH}
    shut = [(x, y) for y in range(y0 + 1, y1) for x in range(x0 + 1, x1) if (x, y) not in solid and not m.walkable(x, y)]
    assert not shut, [(p, m.at("Buildings", *p)) for p in shut]


def test_wall_sides_face_in():
    # the side runs are the same piece, the east one mirrored: both with their dark face toward the inside
    import vale_graveyard as vg
    from PIL import ImageOps
    m, pieces = graveyard()
    x0, y0, x1, y1 = vg.RECT
    art = vg.tiles()
    for se in art["wall_side"]:
        assert art["wall_side_east"][se].tobytes() == ImageOps.mirror(art["wall_side"][se]).tobytes(), se
    for y in range(y0 + 1, y1):
        assert pieces.get(("Buildings", x0, y)) == {"wall_side"}, (x0, y, pieces.get(("Buildings", x0, y)))
        assert pieces.get(("Buildings", x1, y)) == {"wall_side_east"}, (x1, y, pieces.get(("Buildings", x1, y)))


def pieces_at(pieces, name):
    return [(x, y) for (layer, x, y), names in pieces.items() if name in names]


def test_grave_tall_feet_only():
    # each stone's and marker's top row is on Front (walkable: you walk behind it), and only its
    # bottom row is on Buildings; the gateposts too
    import vale_graveyard as vg
    m, pieces = graveyard()
    for (x, y), (gid,) in grave_actions(m).items():
        art = GRAVE_ART[gid]
        assert art + ".top" in pieces.get(("Front", x, y - 1), set()), (gid, x, y - 1)
        assert not m.at("Buildings", x, y - 1) and m.walkable(x, y - 1), (gid, "top solid", x, y - 1)
        assert not m.at("Front", x, y), (gid, "something over its foot")
    for x, y in pieces_at(pieces, "gatepost.foot"):
        assert pieces.get(("Front", x, y - 1)) == {"gatepost.top"} and not m.at("Buildings", x, y - 1), (x, y)


_TERRAIN = {}


def terrain(m):
    """{(x, y): the four corners' terrain} for every tile of plain ground (build_vale.GROUND, as
    vale_patch.context loads it from the vanilla exports)."""
    if not _TERRAIN:
        import contextlib, io
        import build_vale as bv
        from vale_patch import context
        with contextlib.redirect_stdout(io.StringIO()):
            context(VALE_TMX, os.environ.get("VALE_EXPORTS", "C:/Users/blinx/Desktop/MuseStardew/.devgame/exports_vale"))
        _TERRAIN["ground"] = bv.GROUND
    ground = _TERRAIN["ground"]
    return {(x, y): ground.get((m.at("Back", x, y), m.at("Buildings", x, y))) for y in range(m.h) for x in range(m.w)}


def test_trail_joins_road():
    # a walkable trail (tiles with sand corners) runs from beside the bus road's turning circle to the gate
    import vale_graveyard as vg
    m, _ = graveyard()
    ter = terrain(m)
    road = lambda x, y: 30000 <= m.at("Back", x, y) < 31000          # build_vale ROAD_FIRST
    trail = lambda x, y: m.walkable(x, y) and "S" in (ter.get((x, y)) or ())
    start = [(x, y) for (x, y) in ter if trail(x, y) and any(road(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                                                           if 0 <= x + dx < m.w and 0 <= y + dy < m.h)
             and abs(x - vg.GATE[0][0]) <= 4 and y < vg.RECT[1]]
    assert start, "no trail beside the road near the gate"
    seen, q = set(start), collections.deque(start)
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in seen and trail(*n):
                seen.add(n)
                q.append(n)
    assert all(g in seen for g in vg.GATE), ("trail doesn't reach the gate", sorted(seen))
    # and the old trail to nowhere is gone: no sand left inside the wall but the landing at the gate
    x0, y0, x1, y1 = vg.RECT
    sand = [(x, y) for y in range(y0 + 1, y1) for x in range(x0 + 1, x1) if "S" in (ter.get((x, y)) or ())]
    near = lambda x, y: y <= y0 + 2 and min(abs(x - gx) for gx, _ in vg.GATE) <= 1
    assert sand and all(near(x, y) for x, y in sand), sand


def test_four_seasons():
    # every graveyard piece has its own winter art (snow on it), laid in the winter sheet; spring's
    # art is in spring, summer and fall
    import vale_graveyard as vg
    m, pieces = graveyard()
    laid = set().union(*pieces.values())
    for name in laid:
        for src in vg.SOURCES[name]:
            for suffix in ("", "_winter"):
                assert os.path.exists(os.path.join(vg.ART, src + suffix + ".png")), (name, src + suffix)
    art = vg.tiles()
    assert set(art) == laid, (set(art) ^ laid)          # every piece of art is on the map
    from PIL import Image
    for src in {src for name in laid for src in vg.SOURCES[name]}:
        spring, winter = (Image.open(os.path.join(vg.ART, src + sfx + ".png")).convert("RGBA").tobytes() for sfx in ("", "_winter"))
        assert winter != spring, f"{src}: its winter is spring's art"     # (a tile of it may be the same: a gatepost's foot has no snow)


def test_gate_tile_agrees():
    # vale_graveyard's tile just inside the gate is ValeGraves.GraveyardGateTile
    import vale_graveyard as vg
    assert gate_tile() == vg.GATE_INSIDE, (gate_tile(), vg.GATE_INSIDE)


def lupini_monday():
    sched = json_with_comments(os.path.join(PACK, "data", "lupini_schedule.json"))
    first = sched["Mon"].split("/")[0].split()
    return first


def test_lupini_monday():
    # his Monday morning: a walkable tile beside the graveyard bench, reached from the bus stop, inside
    # the wall; the rest of his day unchanged
    import vale_graveyard as vg
    m, _ = graveyard()
    time, where, x, y, facing = lupini_monday()[:5]
    x, y = int(x), int(y)
    assert (time, where) == ("900", "blinxxy.MuseArt_GildedVale"), (time, where)
    assert (x, y) == vg.LUPINI_STAND and facing == str(vg.LUPINI_FACING), ((x, y), facing)
    assert m.walkable(x, y) and (x, y) in m.reach(BUS_STOP), (x, y)
    assert strictly_inside(vg.RECT, x, y)
    seat = [(x + dx, y) for dx in (-1, 1) if m.at("Buildings", x + dx, y) in VANILLA_BENCH]
    assert seat, "not beside the bench's seat"
    sched = json_with_comments(os.path.join(PACK, "data", "lupini_schedule.json"))
    assert sched["Mon"].split("/", 1)[1] == sched["Tue"].split("/", 1)[1], "the rest of Monday changed"


def test_graveyard_in_routes():
    import vale_graveyard as vg
    import vale_routes
    assert vale_routes.PLACES["graveyard gate"] == vg.GATE_INSIDE
    bx, by = vg.BENCH
    assert vale_routes.PLACES["graveyard bench"] == (bx + 1, by + 1) and "graveyard bench" in vale_routes.SEATS


def drafted(key):
    """A line's drafted text in the text doc, with the user's one change: Tobin's T is on the foot."""
    text = open(TEXT_MD, encoding="utf-8").read()
    t = re.search(r"\*\*`" + re.escape(key) + r"`\*\*[^\n]*\n> ([^\n]+)", text)
    assert t, key
    return t.group(1).replace("a small T pressed into the back", "a small T pressed into the foot")


def test_grave_text():
    # the grave lines, Tobin's and Lupini's, verbatim from the draft (the T on the foot), in i18n, and
    # the graves' in ValeStrings (how the dev pack's text reaches the code)
    i18n = json_with_comments(I18N)
    keys = [f"vale.grave.{g}" for g in GRAVE_IDS] + ["tobin.dialogue.urns", "lupini.dialogue.bram"]
    for key in keys:
        assert i18n.get(key) == drafted(key), (key, i18n.get(key), drafted(key))
    for g in ("hester", "amos"):
        assert "into the foot" in i18n[f"vale.grave.{g}"] and "into the back" not in i18n[f"vale.grave.{g}"]
    strings = guide_entries_from(os.path.join(PACK, "data", "vale_shops.json"), "Mods/blinxxy.MuseArt/ValeStrings")
    for g in GRAVE_IDS:
        assert strings.get(f"vale.grave.{g}") == "{{i18n:vale.grave.%s}}" % g, g


def guide_entries_from(path, target):
    data = json_with_comments(path)
    return next(c["Entries"] for c in data["Changes"] if c.get("Target") == target and c.get("Action") == "EditData")


def test_resident_lines_wired():
    # Tobin's urns line (from 4 hearts, once you've read a stone) and Lupini's about Bram (once you've
    # read Bram's): conversation topics added by trigger actions, said once
    tobin = os.path.join(PACK, "data", "tobin_dialogue.json")
    lupini = os.path.join(PACK, "data", "lupini_dialogue.json")
    assert guide_entries_from(tobin, "Characters/Dialogue/TobinClay")["blinxxy.MuseArt_ValeUrns"] == "{{i18n:tobin.dialogue.urns}}"
    assert guide_entries_from(lupini, "Characters/Dialogue/Lupini")["blinxxy.MuseArt_ValeBram"] == "{{i18n:lupini.dialogue.bram}}"
    t = guide_entries_from(tobin, "Data/TriggerActions")["blinxxy.MuseArt_ValeUrnsTalk"]
    assert "PLAYER_HEARTS Current TobinClay 4" in t["Condition"] and "blinxxy.MuseArt_ValeGraveRead" in t["Condition"], t
    assert t["Actions"][0].startswith("AddConversationTopic blinxxy.MuseArt_ValeUrns "), t
    l = guide_entries_from(lupini, "Data/TriggerActions")["blinxxy.MuseArt_ValeBramTalk"]
    assert "blinxxy.MuseArt_ValeGraveRead_bram" in l["Condition"], l
    assert l["Actions"][0].startswith("AddConversationTopic blinxxy.MuseArt_ValeBram "), l
    cs = open(GRAVES_CS, encoding="utf-8").read()
    assert '"_ValeGraveRead"' in cs, "ValeGraves should set the read flags"


# ---- The guide's board in the Vale House (controller Ruling 9) ----

def board_tile():
    t = re.search(r"BoardTile\s*=\s*new\s*(?:Point)?\(\s*(\d+)\s*,\s*(\d+)\s*\)", open(GUIDE_CS, encoding="utf-8").read())
    assert t, "PaintersGuide.BoardTile not found"
    return int(t.group(1)), int(t.group(2))


def test_guide_board():
    # the TakeGuide action is on the board's wall tiles by the guest-book case (11, 18), the C#
    # tile among them, and there's floor right in front of it, reached from the front door
    m = Map(HOUSE_TMX)
    tiles = sorted(m.actions("blinxxy.MuseArt_TakeGuide"))
    assert tiles, "no TakeGuide action in the Vale House"
    bx, by = board_tile()
    assert (bx, by) in tiles, ((bx, by), tiles)
    assert all(abs(x - 11) <= 3 and abs(y - 18) <= 2 for x, y in tiles), f"not by the guest books: {tiles}"
    assert m.walkable(bx, by + 1), "nowhere to stand in front of the board"
    door = (15, 29)                          # just inside the front door (build_vale_house FRONT_DOOR)
    assert m.walkable(*door)
    assert (bx, by + 1) in m.reach(door), "the board's front isn't reachable from the door"


if __name__ == "__main__":
    for name, fn in [(n, f) for n, f in list(globals().items()) if n.startswith("test_") and callable(f)]:
        try:
            fn()
        except Exception:
            traceback.print_exc()
            print("FAIL", name)
            sys.exit(1)
        print("ok  ", name)
