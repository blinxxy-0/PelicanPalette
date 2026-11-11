"""Lupini's painting spots carry their names (the painters' guide design, section 4,
docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md).

Each spot is a paint rock (build_vale PICKS spot_*_rock), 2 x 2: a top on Front2 over a
solid foot on Buildings. Both its foot tiles get `blinxxy.MuseArt_PaintSpot <spotId>`
(ArtStudio/Vale/PaintersGuide.cs, Spots). The spots had a painter's stool beside the rock,
which carried the action; the user didn't like the stool and kit (2026-11-11), so this takes
each stool out (its foot and top, and its action) and gives the rock its job. The stool tiles
in SPOTS are where each stood, kept so a re-run can check they're gone. The clearing's
spot (the old Painter's clearing, x 3-12, y 10-22) goes: the clearing becomes the
graveyard (section 6). The lake spot stays where the overlook put it.

The four new spots (section 4) are laid here, from the same slots as the rooftops' stool and
rock: the Lost Parterre's, just inside its east gate; the Falls', on the river's north bank
west of the cliff; the Gilded Tree's, on its lawn east of the trunk; and the Old Stage's, under
the cliff west of the amphitheatre (the user's yes, 2026-11-09).

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
# The new spots, laid by this script (patch_map): their stools and rocks copy the template spot's
# tiles, so they take no new slots on the sheets.
NEW_SPOTS = {
    # in the pocket just inside the east gate, south of the path, looking west down the garden
    # (its rock in the north-east corner, clear of the east statue's head)
    "parterre": ((25, 44), (25, 39)),
    # on the river's north bank west of the falls' cliff, looking east at the fall's lip, the rock
    # a little west. Not on the basin's south bank: that's beside the north light's stool (79, 16),
    # and "can't be two right next to each other"; and the stool's top hung over the water. Just
    # west of the pine at (75, 4), whose canopy (x 74-76) hides the cliff's end
    "falls": ((72, 4), (68, 4)),
    # on the tree's lawn, east of it between the canopy and the lakeside sand (south of the tree
    # is under the woods' canopies, and west of it is the wildflowers' spot); the rock above it
    "tree": ((43, 66), (43, 63)),
    # under the cliff west of the amphitheatre, looking at it from the side: 3 tiles clear of its
    # footprint and off its seats, its stage and its approach (the user: "make sure the spot near
    # the amphitheatre doesn't feel too intrusive. I eventually have plans for a skill there";
    # this tile shown first, and approved, 2026-11-09). Its foot is on the cliff's shading row, so
    # it stands off the plaza's curb (row 54)
    "stage": ((25, 53), (22, 53)),
}
SPOTS.update(NEW_SPOTS)
TEMPLATE = "rooftops"
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


def _cells(m, foot):
    """A piece's tiles as laid: [(dx, dy, layer, gid)] from its left foot tile."""
    x, y = foot
    return [(dx, 0, "Buildings", m.g["Buildings"][y][x + dx]) for dx in (0, 1)] +            [(dx, -1, "Front2", m.g["Front2"][y - 1][x + dx]) for dx in (0, 1)]


def _lay(m, pieces, sid, kind, foot, cells):
    """Lays a stool or a rock with its left foot at foot, unless it already stands there. The
    ground must be clear: nothing on the foot's Buildings or the top's Front layers, no Paths
    marker (a tree, a bush, long grass), and no water under its foot or its top (nothing straddles
    the water's edge)."""
    x, y = foot
    if pieces.at(m, x, y) == kind:
        return
    for dx in (0, 1):
        for dy in (0, -1):
            tx, ty = x + dx, y + dy
            _inside(SPOT_BOX[sid], tx, ty)
            assert not m.g["Paths"][ty][tx], f"{sid}: a Paths marker under its {kind} at ({tx}, {ty})"
            assert m.g["Back"][ty][tx] not in bv.WATER_BACKS, f"{sid}: its {kind} over the water at ({tx}, {ty})"
            for layer in ("Front", "Front2", "AlwaysFront"):
                assert not m.g[layer][ty][tx], f"{sid}: something on {layer} where its {kind} goes, ({tx}, {ty})"
        assert not m.g["Buildings"][y][x + dx], f"{sid}: something on Buildings where its {kind}'s foot goes, ({x + dx}, {y})"
    for dx, dy, layer, gid in cells:
        m.g[layer][y + dy][x + dx] = gid


def patch_map(m, pieces):
    rock_cells = _cells(m, SPOTS[TEMPLATE][1])
    assert pieces.at(m, *SPOTS[TEMPLATE][1]) == "rock", f"{TEMPLATE}: no rock at {SPOTS[TEMPLATE][1]}"
    for sid, (_, rock) in NEW_SPOTS.items():
        _lay(m, pieces, sid, "rock", rock, rock_cells)

    for sid, (stool, rock) in SPOTS.items():
        box = SPOT_BOX[sid]
        x, y = stool
        if pieces.at(m, x, y) == "stool":
            # the stool goes: its foot, its top, and its action
            for dx in (0, 1):
                _inside(box, x + dx, y)
                _inside(box, x + dx, y - 1)
                m.g["Buildings"][y][x + dx] = 0
                m.g["Front2"][y - 1][x + dx] = 0
        for dx in (0, 1):
            _set_action(m, x + dx, y, None)
        x, y = rock
        assert pieces.at(m, x, y) == "rock", f"{sid}: no rock at ({x}, {y})"
        for dx in (0, 1):
            _inside(box, x + dx, y)
            _inside(box, x + dx, y - 1)
            _set_action(m, x + dx, y, action(sid))

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
