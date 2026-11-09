"""Keeps the Gilded Vale's buildings clear of trees, bushes and spawned debris.

The user, 2026-11-08, of the Old Studio at the east end: "this house is borderline
inaccessible. I had to cut down a bunch of trees to even see the front of this house
and there are a bunch literally in the house that I couldn't break." Two causes:
- build_vale.py's woods-round-the-edge rule (x > 90) and the decor pass planted Paths
  tree markers against the studio's east wall and in front of it, checking only the
  tile itself, never how near a building it was. An oak's canopy is 3 tiles wide and
  6 tall over its trunk, so a trunk beside a wall or a few rows below a front hides it.
- A save keeps every tree it ever planted. Trees from an older map, before the Old
  Studios stood there, still grow inside its walls (ArtStudio/Vale/ValeGrounds.cs
  sweeps them out).

So every building (the PICKS whose art is a building) gets zones round it:
- its footprint, and a strip 2 tiles wide down each side: no trees;
- 5 rows in front, a tile wider each side: no trees, so the front shows from the path;
- its footprint and the 3 rows in front (the door's approach): no bushes or long
  grass either, and NoSpawn on Back, so forage, weeds and stones never land there.
  Those tiles also carry blinxxy.MuseArt_KeepClear, which the game-side sweep reads.
- the sides and the front rows get NoSpawn "Tree", so no sapling grows there.

build_vale.py runs clear() after the decor and the woods. Run this file on its own to
apply the same rule to the committed map in place (its rebuild doesn't reproduce the
committed map exactly, 2026-11-08):
    python tools/vale_yards.py "[CP] Gilded Vale (dev)/assets/GildedVale.tmx"
"""
import os, re, sys

TREES = (9, 10, 11, 34)          # oak, maple, pine, fruit tree (SpawnTree)
BUSHES = {24: 3, 25: 2, 26: 1}   # large, medium, small: their width in tiles
GRASS = 22
SIDE, FRONT, APPROACH = 2, 5, 3
KEEP_CLEAR = "blinxxy.MuseArt_KeepClear"


def building_boxes():
    """(left, top, right, bottom) of every building piece, as place_buildings lays it."""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import build_vale as bv
    from PIL import Image
    boxes = {}
    for name, (art, left, bottom, solid, *rest) in bv.PICKS.items():
        if not art.startswith("buildings/"):
            continue
        im = bv.tile_piece(Image.open(os.path.join(bv.ART, art + ".png")).convert("RGBA"), rest[0] if rest else bv.LIFT)
        cols, rows = im.width // 16, im.height // 16
        boxes[name] = (left, bottom - rows + 1, left + cols - 1, bottom)
    return boxes


def zones(boxes, W, H):
    """tile -> set of zone names ("foot", "side", "front", "approach")."""
    z = {}

    def mark(x0, y0, x1, y1, kind):
        for y in range(max(0, y0), min(H - 1, y1) + 1):
            for x in range(max(0, x0), min(W - 1, x1) + 1):
                z.setdefault((x, y), set()).add(kind)

    for left, top, right, bottom in boxes.values():
        mark(left, top, right, bottom, "foot")
        mark(left - SIDE, top, left - 1, bottom, "side")
        mark(right + 1, top, right + SIDE, bottom, "side")
        mark(left - 1, bottom + 1, right + 1, bottom + FRONT, "front")
        mark(left, bottom + 1, right, bottom + APPROACH, "approach")
    return z


def clear(g, paths_first, tile_props, W, H, boxes=None, log=print):
    """Takes the markers out of the zones, and off any wall, and marks the zones NoSpawn.
    Returns what went."""
    z = zones(boxes or building_boxes(), W, H)
    removed = []
    for y in range(H):
        for x in range(W):
            v = g["Paths"][y][x]
            if not v:
                continue
            idx = v - paths_first
            cover = [(x + i, y) for i in range(BUSHES.get(idx, 1))]
            kinds = set().union(*(z.get(c, set()) for c in cover))
            # nothing grows out of a wall or a fence post (two of Lupini's garden markers
            # sat on his fence, laid before it)
            on_wall = any(cx < W and g["Buildings"][y][cx] for cx, _ in cover)
            if on_wall or (idx in TREES and kinds & {"foot", "side", "front"}) or \
                    ((idx in BUSHES or idx == GRASS) and kinds & {"foot", "approach"}):
                g["Paths"][y][x] = 0
                tile_props.pop(("Paths", x, y), None)
                removed.append((idx, x, y))
    for (x, y), kinds in z.items():
        if kinds & {"foot", "approach"}:
            tile_props.setdefault(("Back", x, y), {}).update({"NoSpawn": "All", KEEP_CLEAR: "T"})
        else:
            # beside and in front: no tree grows here (Tree.dayUpdate stops a sapling on
            # NoSpawn "Tree"), and the game-side sweep takes old ones out
            tile_props.setdefault(("Back", x, y), {}).update({"NoSpawn": "Tree"})
    names = {9: "oak", 10: "maple", 11: "pine", 34: "fruit tree", 24: "large bush", 25: "bush", 26: "small bush", 22: "long grass"}
    for idx, x, y in removed:
        log(f"yards: took the {names.get(idx, idx)} out at {x},{y}")
    return removed


# --- applying it to the committed map in place ------------------------------------------

def _patch(path):
    text = open(path, encoding="utf-8").read()
    W, H = int(re.search(r'<map [^>]* width="(\d+)"', text).group(1)), int(re.search(r'<map [^>]* height="(\d+)"', text).group(1))
    paths_first = int(re.search(r'<tileset firstgid="(\d+)" name="Paths"', text).group(1))
    layer_names = re.findall(r'<layer id="\d+" name="([^"]+)"', text)
    def layer(name):
        return re.search(r'(<layer id="\d+" name="' + name + r'"[^>]*>\s*<data encoding="csv">\n)(.*?)(\n\s*</data>)', text, re.S)
    m = layer("Paths")
    g = {name: [list(map(int, row.rstrip(",").split(","))) for row in layer(name).group(2).split("\n")] for name in ("Paths", "Buildings")}
    tile_props = {}
    for layer, body in re.findall(r'<objectgroup id="\d+" name="([^"]+)" visible="false">(.*?)</objectgroup>', text, re.S):
        for ox, oy, props in re.findall(r'<object id="\d+" name="TileData" x="(\d+)" y="(\d+)"[^>]*>\s*<properties>(.*?)</properties>', body, re.S):
            tile_props[(layer, int(ox) // 16, int(oy) // 16)] = dict(re.findall(r'<property name="([^"]+)" type="string" value="([^"]*)" />', props))
    clear(g, paths_first, tile_props, W, H)
    csv = ",\n".join(",".join(str(v) for v in row) for row in g["Paths"])
    text = text[:m.start(2)] + csv + text[m.end(2):]
    # the object groups, rewritten as build_vale.py write() writes them
    out, oid = [], 1
    for i, layer in enumerate(layer_names):
        props = sorted((x, y, p) for (l, x, y), p in tile_props.items() if l == layer)
        if not props:
            continue
        out.append(f'  <objectgroup id="{len(layer_names) + 1 + i}" name="{layer}" visible="false">')
        for x, y, p in props:
            out.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            out.append('      <properties>')
            out += [f'        <property name="{k}" type="string" value="{v}" />' for k, v in p.items()]
            out.append('      </properties>')
            out.append('    </object>')
            oid += 1
        out.append('  </objectgroup>')
    start = text.find("  <objectgroup")
    end = text.rfind("</map>")
    text = (text[:start] if start >= 0 else text[:end]) + "\n".join(out) + "\n" + text[end:]
    open(path, "w", encoding="utf-8", newline="\n").write(text)
    print("wrote", path)


if __name__ == "__main__":
    _patch(sys.argv[1])
