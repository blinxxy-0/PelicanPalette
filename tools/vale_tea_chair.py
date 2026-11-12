"""Bea's tea garden: the chair pressed into the east hedge goes (the user, 2026-11-11: "the tea
setting chair that is merged into the hedge").

The tea garden is painted (build_vale.py FEATURES "tea"). The upper table's right-hand chair stands
hard against the flowering east hedge, its back lost in it. The lower table has one chair, so the
upper one keeps its left-hand chair and this one is taken out: its pixels on the two tiles it's
painted on (its seat and back on the Buildings tile at (39, 12), its legs on the Back2 tile at
(39, 13)) become transparent, so the map's own grass under the painting shows there, as it already
does round it. The table, its cakes and the blue flowers below are untouched.

Only those two tiles, each used once on the map, in all four <season>_GildedValeBuildings.png
sheets. The map's tiles don't change.

Re-runnable: a pixel already cleared stays cleared, so a second run changes nothing. A rebuild that
paints the garden afresh lays the chair again, and this takes it out again.

Run as: python tools/vale_tea_chair.py <GildedVale.tmx> [exports]   (build_vale.VALE_PIECES)
"""
import os, re, sys
import numpy as np
from PIL import Image

SHEET = "z_blinxxy.MuseArt_GildedValeBuildings"
SEASONS = ("spring", "summer", "fall", "winter")
# (layer, tile x, tile y): the chair's pixels in that tile, as (x from, x to, y from, y to), inclusive
CHAIR = [
    ("Buildings", 39, 12, (10, 15, 7, 15)),     # the seat and back
    ("Back2", 39, 13, (10, 15, 0, 6)),          # the legs (the flower below them, rows 7 on, stays)
]


def _gid(text, layer, x, y):
    m = re.search(rf'name="{layer}"[^>]*>\s*<data encoding="csv">(.*?)</data>', text, re.S)
    return int(m.group(1).strip().split("\n")[y].strip().rstrip(",").split(",")[x])


def _uses(text, gid):
    return sum(row.strip().rstrip(",").split(",").count(str(gid))
               for m in re.finditer(r'<data encoding="csv">(.*?)</data>', text, re.S)
               for row in m.group(1).strip().split("\n"))


def patch_file(tmx_path, exports=None):
    assets = os.path.dirname(os.path.abspath(tmx_path))
    text = open(tmx_path, encoding="utf-8").read()
    first = int(re.search(rf'firstgid="(\d+)" name="{SHEET}"', text).group(1))
    cleared = 0
    for season in SEASONS:
        path = os.path.join(assets, f"{season}_GildedValeBuildings.png")
        sheet = np.array(Image.open(path).convert("RGBA"))
        before = sheet.copy()
        for layer, tx, ty, (x0, x1, y0, y1) in CHAIR:
            gid = _gid(text, layer, tx, ty)
            assert first <= gid < first + 10000, f"({tx}, {ty}) {layer}: not a painted tile ({gid})"
            assert _uses(text, gid) == 1, f"({tx}, {ty}) {layer}: its tile is used elsewhere too"
            i = gid - first
            r, c = (i // 32) * 16, (i % 32) * 16
            sheet[r + y0:r + y1 + 1, c + x0:c + x1 + 1] = 0
        if not np.array_equal(before, sheet):
            Image.fromarray(sheet).save(path)
            cleared += 1
    print(f"tea garden chair: {'cleared in ' + str(cleared) + ' season(s)' if cleared else 'already gone'}")


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
