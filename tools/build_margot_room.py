"""Builds Margot's room at the Saloon and her cottage's inside (1.3), and
the furniture and wallpaper sheets they use. She reaches it down a short flight of steps from Gus's storeroom
upstairs at the Saloon, like the steps down to Sebastian's room at Robin's
(the steps themselves are tile edits in data/margot.json).

  MargotRoom.tmx       her room, 11x10: the game's own Shed, narrowed (its
                       middle columns and rows taken out), so it's a
                       decoratable room like the farmhouse: straight walls
                       three tiles tall, marked WallID/FloorID, wallpapered
                       and floored by the game itself (data/margot.json makes
                       it a DecoratableLocation). Her wallpaper is the sage
                       damask (Wallpapers.png, a real wallpaper: see
                       data/margot.json). Furnished with pieces made for her
                       (PixelLab, picked 2026-11-01; art/margot/room): a
                       restoration table with a magnifying lamp, a plan
                       chest, a museum crate with a wrapped painting, her
                       travelling trunk, and on the walls a map of the
                       valley, pinned sketches and a museum poster. The bed
                       is the Saloon's spare bed.
  MargotRoom.png       those pieces, outlines recoloured (vanillafy.selout:
                       a darker shade of what they border, never black).
  Wallpapers.png       the mod's wallpapers, 256 wide as the game requires;
                       the damask is pattern 0.
Reads maps written by the museart_export_map debug command (vanilla's
Saloon, for the bed).
Usage: build_margot_room.py <exports dir> <content pack assets dir> art/margot/room/picks
  (the picks dir holds the chosen generations, by batch, as PixelLab named them)
"""
import os, sys
from xml.sax.saxutils import quoteattr
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from build_shop_interior import read
from build_spouse_areas import to_sheet
from vanillafy import selout

LAYERS = ["Back", "Back2", "Buildings", "Front", "Front2", "AlwaysFront"]
WALL_ROW = 1                          # WallID marks each wall column's top tile
LIGHT_TEXTURE = 4
ROOM_SHEET = "MargotRoom.png"
# Every piece, by name: (generation batch, job, candidate).
SOURCES = {
    "table":    ("stardew-margot-room", "restoration_table", 6),
    "chest":    ("stardew-margot-room", "plan_chest", 15),
    "crates":   ("stardew-margot-room-front", "crates_front", 9),
    "trunk":    ("stardew-margot-room-trunk", "trunk_front", 10),
    "map":      ("stardew-margot-walls", "valley_map", 9),
    "sketches": ("stardew-margot-walls", "sketches", 6),
    "poster":   ("stardew-margot-walls", "poster", 15),
    "rack":     ("stardew-margot-yard", "moulding_rack_1"),
    "bench":    ("stardew-margot-yard", "workbench_3"),
    "mitre":    ("stardew-margot-yard", "mitre_0"),
}
WALLPAPER = ("stardew-margot-walls", "wall_damask", 4)
SHEET_COLS = 8                        # MargotRoom.png is 128 wide
# The Saloon's spare bed (its tiles in the Saloon, x 22-23, y 2-5).
BED_FROM = (22, 2)
# The rooms, each the game's Shed cut to size (its door is at column 6):
# which of its columns and rows they keep, their door out, and where each
# piece stands (x, y of its top-left tile, and how many bottom rows are
# solid; 0: on the wall, drawn over it).
ROOMS = {
    # At the Saloon, down the storeroom steps.
    "MargotRoom": dict(
        cols=[0, 1, 2, 3, 5, 6, 7, 8, 9, 11, 12], rows=[0, 1, 2, 3, 4, 5, 6, 7, 12, 13],
        exit=(5, 9), warp="Saloon 36 8", lights=[(2, 4), (8, 4), (5, 7)], bed=(8, 2),
        pieces={"table": ((1, 2), 1), "chest": ((4, 3), 1), "crates": ((1, 6), 2), "trunk": ((8, 6), 1),
                "map": ((3, 1), 0), "sketches": ((5, 1), 0), "poster": ((7, 1), 0)}),
    # Her cottage in town, out to its door by the cherry tree: her corner
    # (bed, restoration table, plan chest, trunk, the walls' map, sketches
    # and poster) and her frame workshop along the left (the moulding rack,
    # the frame workbench and the mitre-box sawhorse), the way to the door
    # kept clear.
    "MargotCottage": dict(
        cols=list(range(13)), rows=[0, 1, 2, 3, 4, 5, 6, 7, 12, 13],
        exit=(6, 9), warp="Town 21 97", lights=[(2, 4), (9, 4), (6, 7)], bed=(9, 2),
        pieces={"table": ((1, 2), 1), "chest": ((3, 3), 1), "crates": ((6, 3), 1), "trunk": ((9, 6), 1),
                "map": ((3, 1), 0), "sketches": ((6, 1), 0), "poster": ((8, 1), 0),
                "rack": ((1, 5), 1), "bench": ((3, 6), 1), "mitre": ((7, 6), 1)}),
}


def shell(shed, room):
    sets, layers = shed[0], shed[1]
    w, h = len(room["cols"]), len(room["rows"])
    grid = {l: [[None] * w for _ in range(h)] for l in LAYERS}
    for l in LAYERS:
        if l not in layers:
            continue
        lw, data = layers[l]
        for y, sy in enumerate(room["rows"]):
            for x, sx in enumerate(room["cols"]):
                gid = data[sy * lw + sx]
                grid[l][y][x] = to_sheet(gid, sets) if gid else None
    return grid


def pack(gen):
    """The pieces on one sheet, each on the 16-pixel grid, outlines
    recoloured; returns the sheet and each piece's (first index, cols, rows)."""
    slots, images, x, y, row_h = {}, {}, 0, 0, 0
    for name, source in SOURCES.items():
        batch, job = source[0], "_".join(str(p) for p in source[1:])
        im = selout(Image.open(os.path.join(gen, batch, f"{job}.png")).convert("RGBA"))
        images[name] = im
        cols, rows = im.width // 16, im.height // 16
        if x + cols > SHEET_COLS:
            x, y, row_h = 0, y + row_h, 0
        slots[name] = (x, y, cols, rows)
        x += cols
        row_h = max(row_h, rows)
    sheet = Image.new("RGBA", (SHEET_COLS * 16, (y + row_h) * 16), (0, 0, 0, 0))
    for name, (sx, sy, _, _) in slots.items():
        sheet.alpha_composite(images[name], (sx * 16, sy * 16))
    return sheet, {n: (sy * SHEET_COLS + sx, c, r) for n, (sx, sy, c, r) in slots.items()}


def furnish(grid, slots, saloon, room):
    for name, ((px, py), solid) in room["pieces"].items():
        first, cols, rows = slots[name]
        for j in range(rows):
            for i in range(cols):
                layer = "Buildings" if j >= rows - solid else "Front"
                if layer == "Front" and grid["Front"][py + j][px + i]:
                    layer = "Front2"
                grid[layer][py + j][px + i] = (ROOM_SHEET, first + j * SHEET_COLS + i)
    # The bed: its headboard and blanket on Front, its frame on Buildings.
    sets, layers = saloon[0], saloon[1]
    for layer in ("Buildings", "Front"):
        lw, data = layers[layer]
        for j in range(4):
            for i in range(2):
                gid = data[(BED_FROM[1] + j) * lw + BED_FROM[0] + i]
                if gid:
                    grid[layer][room["bed"][1] + j][room["bed"][0] + i] = to_sheet(gid, sets)


def write(path, grid, w, h, props, tilesets, tile_props=()):
    firsts = {sheet: first for _, first, sheet, _, _ in tilesets}
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{w}" height="{h}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(LAYERS) + 2}" nextobjectid="{len(tile_props) + 1}">',
             '  <properties>']
    for k, v in props.items():
        lines.append(f'    <property name="{k}" value={quoteattr(v)} />')
    lines.append('  </properties>')
    for name, first, image, iw, ih in tilesets:
        lines.append(f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(iw // 16) * (ih // 16)}" columns="{iw // 16}">')
        lines.append(f'    <image source="{image}" width="{iw}" height="{ih}" />')
        lines.append('  </tileset>')
    for i, layer in enumerate(LAYERS):
        lines.append(f'  <layer id="{i + 1}" name="{layer}" width="{w}" height="{h}">')
        lines.append('    <data encoding="csv">')
        rows = [",".join(str(0 if c is None else firsts[c[0]] + c[1]) for c in grid[layer][y]) for y in range(h)]
        lines.append(",\n".join(rows))
        lines.append('    </data>')
        lines.append('  </layer>')
    if tile_props:
        lines.append(f'  <objectgroup id="{len(LAYERS) + 1}" name="Back">')
        for n, (x, y, key, value) in enumerate(tile_props):
            lines.append(f'    <object id="{n + 1}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            lines.append(f'      <properties><property name="{key}" type="string" value="{value}" /></properties>')
            lines.append('    </object>')
        lines.append('  </objectgroup>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


def main():
    exports, assets, gen = sys.argv[1], sys.argv[2], sys.argv[3]
    shed = read(os.path.join(exports, "Shed.tmx"))
    saloon = read(os.path.join(exports, "Saloon.tmx"))
    sheet, slots = pack(gen)
    sheet.save(os.path.join(assets, ROOM_SHEET))

    # The wallpaper sheet: the damask as pattern 0.
    batch, job, n = WALLPAPER
    papers = Image.new("RGBA", (256, 48), (0, 0, 0, 0))
    papers.alpha_composite(Image.open(os.path.join(gen, batch, f"{job}_{n}.png")).convert("RGBA"), (0, 0))
    papers.save(os.path.join(assets, "Wallpapers.png"))

    tilesets = [("indoor", 1, "townInterior", 512, 1088),
                ("walls_and_floors", 2177, "walls_and_floors", 256, 688),
                ("z_blinxxy.MuseArt_MargotRoom", 2865, ROOM_SHEET, sheet.width, sheet.height)]
    for name, room in ROOMS.items():
        grid = shell(shed, room)
        furnish(grid, slots, saloon, room)
        w, h = len(room["cols"]), len(room["rows"])
        tile_props = [(x, WALL_ROW, "WallID", "Wall") for x in range(1, w - 1)]
        tile_props += [(x, y, "FloorID", "Floor") for y in range(3, h - 1) for x in range(1, w - 1)]
        props = {
            "WallIDs": "Wall blinxxy.MuseArt_Wallpapers:0",
            "FloorIDs": "Floor 0",
            "Warp": f"{room['exit'][0]} {room['exit'][1]} {room['warp']}",
            "Light": " ".join(f"{x} {y} {LIGHT_TEXTURE}" for x, y in room["lights"]),
        }
        write(os.path.join(assets, name + ".tmx"), grid, w, h, props, tilesets, tile_props)
        print("wrote", name + ".tmx")
    print("wrote", ROOM_SHEET, "Wallpapers.png")


if __name__ == "__main__":
    main()
