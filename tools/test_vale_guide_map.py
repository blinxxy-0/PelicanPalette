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


class Map:
    """A TMX's layers, its tile properties (from its tilesets) and its TileData."""

    def __init__(self, path):
        root = ET.parse(path).getroot()
        self.w, self.h = int(root.get("width")), int(root.get("height"))
        self.props = {p.get("name"): p.get("value") for p in root.findall("properties/property")}
        self.layers = {l.get("name"): layer_data(l.find("data")) for l in root.findall("layer")}
        self.tile_props = {}               # gid -> {name: value}
        self.paths_first = None
        for ts in root.findall("tileset"):
            first = int(ts.get("firstgid"))
            if ts.get("name") == "Paths":
                self.paths_first = first
            for t in ts.findall("tile"):
                p = {q.get("name"): q.get("value") for q in t.findall("properties/property")}
                if p:
                    self.tile_props[first + int(t.get("id"))] = p
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


def test_grove_actions():
    # one frame home, one hidden easel, one Founders' easel, and an echo for each buried artist
    m = grove()
    assert len(m.actions("blinxxy.MuseArt_GroveFrame")) == 1
    assert len(m.actions("blinxxy.MuseArt_HiddenEasel")) == 1
    assert len(m.actions("blinxxy.MuseArt_FoundersEasel")) == 1
    echoes = sorted(a[0] for a in m.actions("blinxxy.MuseArt_Echo").values() if len(a) == 1)
    assert echoes == sorted(ECHOES), echoes


def test_grove_reachable():
    m = grove()
    start = arrival()
    assert m.walkable(*start), start
    seen = m.reach(start)
    targets = {}
    for name in ("GroveFrame", "HiddenEasel", "FoundersEasel", "Echo"):
        for p, args in m.actions("blinxxy.MuseArt_" + name).items():
            targets[f"{name} {' '.join(args)}".strip()] = p
    assert len(targets) == 7, targets
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
