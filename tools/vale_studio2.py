"""Studio II, the Corrie twins' (docs/design/vale_newcomers.md, section 5): builds the map
and its heart stages.

  python tools/vale_studio2.py <exports dir>

  1. builds the base map, "[CP] Gilded Vale (dev)/assets/ValeStudio2.tmx" and its sheet, with
     tools/build_vale_buildings.py ("studio2");
  2. builds each heart stage in tools/build_vale_buildings.py STUDIO2_STAGES on its own, and
     diffs it against the base, tile by tile and property by property;
  3. writes data/vale_studio2.json (edit that through this, not by hand): one EditMap per stage
     with its When on the flag its heart event sets, so the stages combine in any order; the door from
     the Vale (86, 38) from the renovation (Spring 1, Year 3; data/vale_studios.json); and the
     right-click texts.

<exports dir> holds the game's maps the builder reads (Saloon.tmx for the beds), as for
build_vale_buildings.py build.
"""
import json, os, sys, tempfile
import xml.etree.ElementTree as ET

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale_buildings as bvb

PACK = "[CP] Gilded Vale (dev)"
ASSETS = os.path.join(PACK, "assets")
DOOR = (86, 38)                 # Studio II's front door on GildedVale.tmx
RENOVATED = 225                 # ValeDate of Spring 1, Year 3 (content.json's dynamic token)
HEARTS = ", ".join(str(n) for n in range(0, 15))


def read(path):
    """A built map: {layer: rows of gids}, [(name, firstgid)], {(layer, x, y): {prop: value}}."""
    root = ET.parse(path).getroot()
    tilesets = sorted(((t.get("name"), int(t.get("firstgid"))) for t in root.iter("tileset")), key=lambda t: t[1])
    layers = {}
    for layer in root.iter("layer"):
        text = layer.find("data").text.strip()
        layers[layer.get("name")] = [[int(v) for v in row.strip().rstrip(",").split(",")] for row in text.splitlines()]
    props = {}
    for group in root.iter("objectgroup"):
        for obj in group.iter("object"):
            x, y = int(obj.get("x")) // 16, int(obj.get("y")) // 16
            props[(group.get("name"), x, y)] = {p.get("name"): p.get("value") for p in obj.iter("property")}
    return layers, tilesets, props


def tile_of(gid, tilesets):
    name, first = [t for t in tilesets if t[1] <= gid][-1]
    return name, gid - first


def diff(base_path, stage_path):
    base, tilesets, bprops = read(base_path)
    stage, stilesets, sprops = read(stage_path)
    assert tilesets == stilesets, "a stage must build with the base's tilesheets"
    tiles = []
    for layer, rows in stage.items():
        for y, row in enumerate(rows):
            for x, gid in enumerate(row):
                if gid == base[layer][y][x]:
                    continue
                entry = {"Position": {"X": x, "Y": y}, "Layer": layer}
                if gid == 0:
                    entry["Remove"] = True
                else:
                    sheet, index = tile_of(gid, tilesets)
                    entry["SetTilesheet"], entry["SetIndex"] = sheet, index
                tiles.append(entry)
    for key in set(bprops) | set(sprops):
        if bprops.get(key) != sprops.get(key):
            layer, x, y = key
            new = sprops.get(key, {})
            gone = {k: "" for k in bprops.get(key, {}) if k not in new}   # (CP: an empty value removes it)
            match = next((t for t in tiles if t["Layer"] == layer and t["Position"] == {"X": x, "Y": y}), None)
            if not match:
                match = {"Position": {"X": x, "Y": y}, "Layer": layer}
                tiles.append(match)
            match["SetProperties"] = {**gone, **new}
    return tiles


def main():
    exports = sys.argv[1]
    base_spec = bvb.BUILDINGS["studio2"]
    bvb.build("studio2", exports, ASSETS)
    base_path = os.path.join(ASSETS, base_spec["tmx"])
    changes = []
    tmp = tempfile.mkdtemp()
    for stage, (when, _) in bvb.STUDIO2_STAGES.items():
        name = "studio2_" + stage
        bvb.BUILDINGS[name] = bvb.studio2_stage(base_spec, [stage])
        bvb.build(name, exports, tmp)
        tiles = diff(base_path, os.path.join(tmp, base_spec["tmx"]))
        changes.append({
            "LogName": f"Studio II: {stage}",
            "Action": "EditMap",
            "Target": "Maps/" + base_spec["location"],
            "When": when,
            "Update": "OnLocationChange",
            "MapTiles": tiles})
        print(f"  {stage}: {len(tiles)} tiles")
    # the door, once Robin's done (before then it's boarded: data/vale_studios.json)
    room, d, _ = bvb.fronts(base_spec)[0]
    inside = (d, base_spec["rooms"][room]["f"] - 1)
    changes.insert(0, {
        "LogName": "Studio II: the door",
        "Action": "EditMap",
        "Target": "Maps/blinxxy.MuseArt_GildedVale",
        "When": {f"Query: {{{{ValeDate}}}} >= {RENOVATED}": "true"},
        "MapTiles": [{"Position": {"X": DOOR[0], "Y": DOOR[1]}, "Layer": "Buildings",
                      "SetProperties": {"Action": f"Warp {inside[0]} {inside[1]} {base_spec['location']}"}}]})
    # the right-click texts (i18n vale.studio2.<key>)
    keys = sorted({v for st in [base_spec] + [bvb.studio2_stage(base_spec, [s]) for s in bvb.STUDIO2_STAGES]
                   for v in st["describe"].values()})
    changes.insert(0, {
        "Action": "EditData",
        "Target": "Strings/StringsFromMaps",
        "Entries": {f"blinxxy.MuseArt_Vale.studio2.{k}": f"{{{{i18n:vale.studio2.{k}}}}}" for k in keys}})
    header = ("// Studio II, the Corrie twins': generated by tools/vale_studio2.py (edit that, not this).\n"
              "// The map is assets/ValeStudio2.tmx (content.json loads it); here: its right-click texts,\n"
              "// the door from the Vale once Robin's renovated it (Spring 1, Year 3), and the heart\n"
              "// stages, each over the base, after the heart event that changes the room (its mail flag):\n"
              "// Opal's 6, their own glass lamps for the city ones; Jet's 8, a gap in the line; Jet's 10,\n"
              "// Mabel's bench in use and her window in the studio's.\n")
    out = os.path.join(PACK, "data", "vale_studio2.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2) + "\n")
    print("wrote", out, "texts:", ", ".join(keys))


if __name__ == "__main__":
    main()
