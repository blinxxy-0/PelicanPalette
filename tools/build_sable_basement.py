"""Builds Sable's room under the shop (13x13), down through the trapdoor
in the shop floor.

The room starts from Sebastian's basement room (SebastianRoom: the stone
wall, the wainscot, the floor and the stairs up in the left-hand corner),
with his things and his door taken out. The lock is on the trapdoor
upstairs instead (ArtShop.cs). It's furnished as a painter's bedroom:
Emily's bed in the right-hand corner with a nightstand and lamp, her
dresser, Sable's own paintings on the stone, the drop cloth and an easel
from their studio (SableStudio.png), and canvases and frames from the shop
(ArtShopFurniture.png). Every other tile references the game's own
tilesheets, so no vanilla art is copied.

Sable sleeps in the bed (BED: the tile under the blanket, so the blanket,
on Front, covers them).

Reads maps written by the museart_export_map debug command.
Usage: build_sable_basement.py <exports dir> <content pack assets dir> <x> <y>
  (x/y: the shop tile the stairs up bring you to, beside the trapdoor)
"""
import json, os, sys
from xml.sax.saxutils import quoteattr
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_interior import read
from build_spouse_areas import to_sheet, STUDIO

LAYERS = ["Back", "Back2", "Buildings", "Front", "Front2", "AlwaysFront"]
W = H = 13
ARRIVAL = (1, 2)                  # at the foot of the stairs, coming down
BED = (10, 5)
LIGHTS = [(9, 4), (4, 8), (7, 4)]
LIGHT_TEXTURE = 4
FURNITURE_COLS = 16


class Room:
    def __init__(self, grid, furniture, empty):
        self.grid, self.furniture, self.empty = grid, furniture, empty

    def put(self, layer, x, y, cell):
        self.grid[layer][y][x] = cell

    def clear(self, layers, x0, y0, x1, y1):
        for layer in layers:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    self.grid[layer][y][x] = None

    def copy(self, src, layers, sx, sy, dx, dy, w, h):
        """Tiles from another map (in its own tileset order)."""
        sets, data = src
        for layer in layers:
            if layer not in data:
                continue
            lw, d = data[layer]
            for j in range(h):
                for i in range(w):
                    gid = d[(sy + j) * lw + sx + i]
                    if gid:
                        self.put(layer, dx + i, dy + j, to_sheet(gid, sets))

    def studio(self, piece, x, y, solid=1, layer=None):
        p = STUDIO["pieces"][piece]
        for j in range(p["rows"]):
            for i in range(p["cols"]):
                target = layer or ("Buildings" if j >= p["rows"] - solid else "Front")
                self.put(target, x + i, y + j, ("studio", (p["y"] + j) * STUDIO["tiles_wide"] + p["x"] + i))

    def shop(self, name, x, y, solid=1):
        """A piece from the shop's furniture sheet, as build_shop_interior
        places them (wall art takes solid=0)."""
        p = self.furniture[name]
        for j in range(p["h"]):
            for i in range(p["w"]):
                index = (p["y"] + j) * FURNITURE_COLS + p["x"] + i
                if index in self.empty:
                    continue
                if j >= p["h"] - solid:
                    self.put("Buildings", x + i, y + j, ("furniture", index))
                else:
                    self.put("Front2" if self.grid["Front"][y + j][x + i] else "Front", x + i, y + j, ("furniture", index))


def shell(seb):
    sets, layers, _ = seb
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    for l in LAYERS:
        if l not in layers:
            continue
        lw, data = layers[l]
        for y in range(H):
            for x in range(W):
                gid = data[y * lw + x]
                grid[l][y][x] = to_sheet(gid, sets) if gid else None
    room = Room(grid, None, None)
    plain_rail = grid["Buildings"][3][9]          # (read before the row is cleared)
    # His posters and things on the wall, and everything on the floor.
    room.clear(["Front", "Front2"], 3, 1, 10, 2)
    room.clear(["Front2"], 11, 1, 11, 2)          # his papers on the side wall
    room.clear(["Front", "Front2", "Back2"], 1, 3, 11, 10)
    room.clear(["Buildings"], 1, 4, 11, 10)
    room.clear(["Buildings"], 3, 3, 10, 3)
    plain_back = grid["Back"][3][3]
    for x in range(3, 11):
        room.put("Buildings", x, 3, plain_rail)
        room.put("Back", x, 3, plain_back)
    # The floor where his rugs and dark boards were: plain planks.
    for y in range(5, 12):
        for x in range(1, 12):
            room.put("Back", x, y, grid["Back"][5 + (y + 1) % 2][4 + x % 2])
    # His door at the foot of the stairs goes: the stairs run on down.
    room.put("Back", 1, 3, grid["Back"][2][1])
    for l in ("Buildings", "Front"):
        room.put(l, 1, 3, None)
    room.put("Front", 1, 2, None)
    # The bottom edge without his things on it.
    for x in (4, 5):
        room.put("Front", x, 11, grid["Front"][11][6])
        room.put("Front2", x, 11, None)
    room.put("Front", 10, 11, None)
    room.put("Front", 11, 11, None)
    room.put("Buildings", 10, 11, None)
    room.put("Buildings", 11, 11, None)
    return grid


def furnish(room, haley):
    # Emily's bed in the right-hand corner, headboard to the wall, with a
    # nightstand and lamp beside it; her dresser further along. Their top
    # rows stand in front of the wall (Front); the wall's rail behind them
    # stays ours (hers, on Buildings, is the beach mural's).
    for sx, dx, w, h in ((1, 10, 2, 4), (3, 9, 1, 2), (6, 3, 2, 2)):
        room.copy(haley, ["Front"], sx, 3, dx, 3, w, h)
        room.copy(haley, ["Buildings"], sx, 4, dx, 4, w, h - 1)
    # Paintings on the stone: over the dresser, the middle of the wall, and
    # over the bed.
    room.shop("painting_night", 3, 1, solid=0)
    room.shop("painting_flowers", 6, 1, solid=0)
    room.shop("painting_lighthouse", 9, 1, solid=0)     # clear of the corner's slant
    # The easel out on a drop cloth, with the light from the lamp.
    room.studio("rug", 4, 6, layer="Back2")
    room.studio("easel", 5, 5)
    room.studio("table", 8, 7)                    # coffee and sweet peas
    # Canvases and frames along the left, and small canvases drying.
    room.shop("stack_canvases", 2, 6)
    room.shop("crate_frames", 2, 9)
    room.studio("rack", 9, 9)
    room.shop("basket_yarn", 5, 9)


def write(path, grid, props, tilesets, firsts):
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(LAYERS) + 1}" nextobjectid="1">',
             '  <properties>']
    for k, v in props.items():
        lines.append(f'    <property name="{k}" value={quoteattr(v)} />')
    lines.append('  </properties>')
    for name, first, image, iw, ih in tilesets:
        lines.append(f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(iw // 16) * (ih // 16)}" columns="{iw // 16}">')
        lines.append(f'    <image source="{image}" width="{iw}" height="{ih}" />')
        lines.append('  </tileset>')
    for i, layer in enumerate(LAYERS):
        lines.append(f'  <layer id="{i + 1}" name="{layer}" width="{W}" height="{H}">')
        lines.append('    <data encoding="csv">')
        rows = [",".join(str(0 if c is None else firsts[c[0]] + c[1]) for c in grid[layer][y]) for y in range(H)]
        lines.append(",\n".join(rows))
        lines.append('    </data>')
        lines.append('  </layer>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


def main():
    exports, assets, ux, uy = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    here = os.path.dirname(os.path.abspath(__file__))
    furniture = json.load(open(os.path.join(here, "..", "art", "shop_interior", "furniture.json")))
    sheet = Image.open(os.path.join(assets, "ArtShopFurniture.png")).convert("RGBA")
    rows = sheet.height // 16
    empty = {j * FURNITURE_COLS + i for j in range(rows) for i in range(FURNITURE_COLS)
             if sheet.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)).getbbox() is None}
    seb = read(os.path.join(exports, "SebastianRoom.tmx"))
    haley = read(os.path.join(exports, "HaleyHouse.tmx"))
    room = Room(shell(seb), furniture, empty)
    furnish(room, (haley[0], haley[1]))

    props = {
        # Up the stairs to beside the trapdoor.
        "Warp": f"1 0 blinxxy.MuseArt_ArtShop {ux} {uy}",
        "Light": " ".join(f"{x} {y} {LIGHT_TEXTURE}" for x, y in LIGHTS),
    }
    studio_w, studio_h = STUDIO["tiles_wide"] * 16, STUDIO["tiles_high"] * 16
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("p", 2177, "paths", 64, 256),
                ("v15_TownInterior2", 2241, "townInterior_2", 512, 656),
                ("z_blinxxy.MuseArt_SableStudio", 3553, "SableStudio.png", studio_w, studio_h),
                ("z_blinxxy.MuseArt_ArtShopFurniture", 3617, "ArtShopFurniture.png", FURNITURE_COLS * 16, rows * 16)]
    firsts = {"townInterior": 1, "paths": 2177, "townInterior_2": 2241, "studio": 3553, "furniture": 3617}
    out = os.path.join(assets, "SableBasement.tmx")
    write(out, room.grid, props, tilesets, firsts)
    print("wrote", out)


if __name__ == "__main__":
    main()
