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

def test_return_tile_walkable():
    # GildedGrove.ReturnTile (until Task 6's ValeGraves.GraveyardGateTile) stands on open ground in the Vale
    x, y = cs_point("ReturnTile")
    vale = Map(VALE_TMX)
    assert vale.walkable(x, y), (x, y)


if __name__ == "__main__":
    for name, fn in [(n, f) for n, f in list(globals().items()) if n.startswith("test_") and callable(f)]:
        try:
            fn()
        except Exception:
            traceback.print_exc()
            print("FAIL", name)
            sys.exit(1)
        print("ok  ", name)
