"""Builds Margot's room (1.3) and the furniture and wallpaper sheets it
uses. She reaches it down a short flight of steps from Gus's storeroom
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
W, H = 11, 10
# The Shed's columns and rows kept (its door is at column 6).
COLS = [0, 1, 2, 3, 5, 6, 7, 8, 9, 11, 12]
ROWS = [0, 1, 2, 3, 4, 5, 6, 7, 12, 13]
EXIT = (5, 9)                         # the room's doorway, up the steps
ARRIVAL = (5, 8)                      # just inside, coming down
STOREROOM = (36, 8)                   # back up in the storeroom, between the barrels
WALL_ROW = 1                          # WallID marks each wall column's top tile
FLOOR_ROWS = range(3, 9)
LIGHTS = [(2, 4), (8, 4), (5, 7)]
LIGHT_TEXTURE = 4
ROOM_SHEET = "MargotRoom.png"
# The pieces: (generation batch, job, candidate), where they stand in the
# room (x, y of their top-left tile), and how many bottom rows are solid
# (0: on the wall, drawn over it).
PIECES = {
    "table":    (("stardew-margot-room", "restoration_table", 6), (1, 2), 1),
    "chest":    (("stardew-margot-room", "plan_chest", 15), (4, 3), 1),
    "crates":   (("stardew-margot-room-front", "crates_front", 9), (1, 6), 2),
    "trunk":    (("stardew-margot-room-trunk", "trunk_front", 10), (8, 6), 1),
    "map":      (("stardew-margot-walls", "valley_map", 9), (3, 1), 0),
    "sketches": (("stardew-margot-walls", "sketches", 6), (5, 1), 0),
    "poster":   (("stardew-margot-walls", "poster", 15), (7, 1), 0),
}
WALLPAPER = ("stardew-margot-walls", "wall_damask", 4)
SHEET_COLS = 8                        # MargotRoom.png is 128 wide
# The Saloon's spare bed (its tiles in the Saloon, x 22-23, y 2-5), against
# the back wall in the right-hand corner.
BED_FROM, BED_AT = (22, 2), (8, 2)


def shell(shed):
    sets, layers = shed[0], shed[1]
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    for l in LAYERS:
        if l not in layers:
            continue
        lw, data = layers[l]
        for y, sy in enumerate(ROWS):
            for x, sx in enumerate(COLS):
                gid = data[sy * lw + sx]
                grid[l][y][x] = to_sheet(gid, sets) if gid else None
    return grid


def pack(gen):
    """The pieces on one sheet, each on the 16-pixel grid, outlines
    recoloured; returns the sheet and each piece's (first index, cols, rows)."""
    slots, images, x, y, row_h = {}, {}, 0, 0, 0
    for name, ((batch, job, n), _, _) in PIECES.items():
        im = selout(Image.open(os.path.join(gen, batch, f"{job}_{n}.png")).convert("RGBA"))
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


def furnish(grid, slots, saloon):
    for name, (_, (px, py), solid) in PIECES.items():
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
                    grid[layer][BED_AT[1] + j][BED_AT[0] + i] = to_sheet(gid, sets)


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
    grid = shell(shed)
    sheet, slots = pack(gen)
    sheet.save(os.path.join(assets, ROOM_SHEET))
    furnish(grid, slots, saloon)

    # The wallpaper sheet: the damask as pattern 0.
    batch, job, n = WALLPAPER
    papers = Image.new("RGBA", (256, 48), (0, 0, 0, 0))
    papers.alpha_composite(Image.open(os.path.join(gen, batch, f"{job}_{n}.png")).convert("RGBA"), (0, 0))
    papers.save(os.path.join(assets, "Wallpapers.png"))

    tilesets = [("indoor", 1, "townInterior", 512, 1088),
                ("walls_and_floors", 2177, "walls_and_floors", 256, 688),
                ("z_blinxxy.MuseArt_MargotRoom", 2865, ROOM_SHEET, sheet.width, sheet.height)]
    tile_props = [(x, WALL_ROW, "WallID", "Wall") for x in range(1, W - 1)]
    tile_props += [(x, y, "FloorID", "Floor") for y in FLOOR_ROWS for x in range(1, W - 1)]
    props = {
        "WallIDs": "Wall blinxxy.MuseArt_Wallpapers:0",
        "FloorIDs": "Floor 0",
        "Warp": f"{EXIT[0]} {EXIT[1]} Saloon {STOREROOM[0]} {STOREROOM[1]}",
        "Light": " ".join(f"{x} {y} {LIGHT_TEXTURE}" for x, y in LIGHTS),
    }
    out = os.path.join(assets, "MargotRoom.tmx")
    write(out, grid, W, H, props, tilesets, tile_props)
    print("wrote", out, ROOM_SHEET, "Wallpapers.png")


if __name__ == "__main__":
    main()
