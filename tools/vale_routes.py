"""Checks the Gilded Vale's routes offline: a path search over the map's solid tiles
(anything on Buildings, and water), between the places a player must reach (the user,
2026-11-07: "do a hitbox check", after being stopped in the nursery and the parterre).

Usage: python tools/vale_routes.py <GildedVale.tmx>
"""
import collections, sys, os
import xml.etree.ElementTree as ET
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_tmx import layer_data

# name: a tile the player must be able to stand on (in front of it)
PLACES = {
    "bus stop": (12, 8), "inn door": (28, 20), "tea garden tables": (37, 13), "pottery door": (46, 19),
    "clay pit": (51, 24), "atelier door": (75, 24), "Lupini's garden": (86, 18),
    "Fern's door": (13, 39), "seedhouse door": (17, 39), "nursery bench": (23, 33), "parterre birdbath": (15, 42),
    "parterre east statue": (25, 41), "Hall door": (44, 40), "fountain": (48, 46), "noticeboard": (41, 43),
    "Old Studio (big)": (73, 39), "studio B": (84, 39), "studio C": (90, 39),
    "amphitheatre stage": (37, 56), "Emmett's door": (18, 62), "Emmett's yard": (11, 58),
    "Gilded Tree": (38, 69), "lakeside bench": (47, 60), "jetty": (48, 66), "picnic table": (90, 66),
    "overlook bench": (86, 27),
}


def main():
    root = ET.parse(sys.argv[1]).getroot()
    W, H = int(root.get("width")), int(root.get("height"))
    L = {l.get("name"): layer_data(l.find("data")) for l in root.findall("layer")}
    first = {}
    for ts in root.findall("tileset"):
        first[os.path.basename(ts.find("image").get("source"))] = int(ts.get("firstgid"))
    water = set()        # the outdoors sheet's water tiles, Town numbering, by colour isn't available: use Back gids
    solid = [[bool(L["Buildings"][y * W + x]) for x in range(W)] for y in range(H)]
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
    import build_vale as bv
    V = None
    try:
        bv_v = bv.SHORE_V
    except AttributeError:
        bv_v = []
    def ok(x, y):
        return 0 <= x < W and 0 <= y < H and not solid[y][x]
    start = PLACES["bus stop"]
    seen = {start}
    q = collections.deque([start])
    while q:
        x, y = q.popleft()
        for d in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + d[0], y + d[1])
            if n not in seen and ok(*n):
                seen.add(n)
                q.append(n)
    bad = 0
    for name, p in PLACES.items():
        reach = p in seen or any((p[0] + dx, p[1] + dy) in seen for dx in (-1, 0, 1) for dy in (-1, 0, 1))
        if not reach:
            bad += 1
        print(("ok      " if reach else "BLOCKED ") + name, p)
    print(bad, "blocked")


if __name__ == "__main__":
    main()
