"""Lupini's painting spots carry their names (the painters' guide design, section 4,
docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md).

Each spot is a stool and a paint rock (build_vale PICKS spot_*_stool / spot_*_rock),
both 2 x 2: a top on Front2 over a solid foot on Buildings. Only the stool is touched:
both its foot tiles get `blinxxy.MuseArt_PaintSpot <spotId>` (ArtStudio/Vale/
PaintersGuide.cs, Spots), and the rock is scenery, with no action. The clearing's
spot (the old Painter's clearing, x 3-12, y 10-22) goes: the clearing becomes the
graveyard (section 6). The lake spot stays where the overlook put it.

The pieces are known by their pixels (the spring sheet's tiles under the foot are the
bottom row of the stool's or the rock's art), so a spot is found wherever it stands.
Everything this changes is inside vale_patch.SPOT_BOXES.

Run as: python tools/vale_spots.py <GildedVale.tmx> [exports]   (build_vale.VALE_PIECES)
Re-runnable: a second run changes nothing.
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_vale as bv
from vale_patch import SPOT_BOXES, load, save
from vale_overlook import ROCK as LAKE_ROCK, SPOT_ID as LAKE_ID, STOOL as LAKE_STOOL

PAINT_SPOT = "blinxxy.MuseArt_PaintSpot"
ART = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), bv.ART, "props", "vale_props_v2")
PIECE_ART = {"stool": "painters_stool_1_3", "rock": "paint_rock_clean"}
# id (PaintersGuide.Spots): the left foot tile of its stool, and of its rock
SPOTS = {
    "rooftops": ((43, 4), (46, 4)),          # the high street, by its hedge
    "northlight": ((79, 16), (79, 19)),      # Lupini's garden, by the atelier
    "wildflowers": ((28, 65), (31, 66)),     # the meadow
    "lakewindow": (LAKE_STOOL, LAKE_ROCK),   # the lake, from the overlook (tools/vale_overlook.py)
}
CLEARING = (3, 10, 12, 22)                   # the old Painter's clearing: its spot goes
BOXES = {name: box for name, box in SPOT_BOXES.items()}
SPOT_BOX = {sid: SPOT_BOXES["spot_" + sid] for sid in SPOTS}


def action(sid):
    return f"{PAINT_SPOT} {sid}"


class Pieces:
    """Knows a stool's or a rock's foot by the pixels of the tiles laid there."""

    def __init__(self, assets):
        self.feet = {}
        for kind, name in PIECE_ART.items():
            im = bv.tile_piece(Image.open(os.path.join(ART, name + ".png")).convert("RGBA"), 0)
            self.feet[kind] = tuple(im.crop((i * 16, 16, i * 16 + 16, 32)).tobytes() for i in (0, 1))
        self.sheet = Image.open(os.path.join(assets, bv.SHEET)).convert("RGBA")
        self.slots = (self.sheet.width // 16) * (self.sheet.height // 16)

    def px(self, gid):
        i = gid - bv.BUILD_FIRST
        if not 0 <= i < self.slots:
            return None
        x, y = i % bv.SHEET_COLS * 16, i // bv.SHEET_COLS * 16
        return self.sheet.crop((x, y, x + 16, y + 16)).tobytes()

    def at(self, m, x, y):
        """'stool' or 'rock' if a piece's left foot tile is at (x, y), else None."""
        if x + 1 >= len(m.g["Buildings"][0]):
            return None
        pair = (self.px(m.g["Buildings"][y][x]), self.px(m.g["Buildings"][y][x + 1]))
        return next((k for k, feet in self.feet.items() if pair == feet), None)

    def find(self, m, box):
        """{(x, y): kind} for every piece whose left foot tile is inside box."""
        x0, y0, x1, y1 = box
        return {(x, y): k for y in range(y0, y1 + 1) for x in range(x0, x1 + 1) if (k := self.at(m, x, y))}


def _inside(box, x, y):
    x0, y0, x1, y1 = box
    assert x0 <= x <= x1 and y0 <= y <= y1, f"({x}, {y}) is outside its box {box}"


def _set_action(m, x, y, value):
    """The Buildings Action of a tile set to value (None: removed); its other properties kept."""
    p = dict(m.props.get(("Buildings", x, y), {}))
    if value is None:
        p.pop("Action", None)
    else:
        p["Action"] = value
    if p:
        m.props[("Buildings", x, y)] = p
    else:
        m.props.pop(("Buildings", x, y), None)


def patch_map(m, pieces):
    for sid, (stool, rock) in SPOTS.items():
        box = SPOT_BOX[sid]
        for (x, y), kind in ((stool, "stool"), (rock, "rock")):
            assert pieces.at(m, x, y) == kind, f"{sid}: no {kind} at ({x}, {y})"
            for dx in (0, 1):
                _inside(box, x + dx, y)
                _inside(box, x + dx, y - 1)
                _set_action(m, x + dx, y, action(sid) if kind == "stool" else None)

    # the clearing's stool and rock: their feet, their tops on Front2, their actions
    box = SPOT_BOXES["spot_clearing"]
    for (x, y), kind in pieces.find(m, CLEARING).items():
        for dx in (0, 1):
            _inside(box, x + dx, y)
            _inside(box, x + dx, y - 1)
            m.g["Buildings"][y][x + dx] = 0
            m.g["Front2"][y - 1][x + dx] = 0
            _set_action(m, x + dx, y, None)
    for (layer, x, y), p in list(m.props.items()):
        if CLEARING[0] <= x <= CLEARING[2] and CLEARING[1] <= y <= CLEARING[3] and p.get("Action", "").split()[:1] == [PAINT_SPOT]:
            _inside(box, x, y)
            _set_action(m, x, y, None)


def patch_file(tmx_path, exports=None):
    m = load(tmx_path)
    patch_map(m, Pieces(os.path.dirname(os.path.abspath(tmx_path))))
    save(m, tmx_path)
    print("painting spots: wrote", tmx_path)


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
