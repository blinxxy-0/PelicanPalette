"""Builds Sable's stall at the Night Market (1.2): their skiff, moored right
below the bridge to the submarine, where you talk to them from the bridge.

The skiff is the user's pick of the PixelLab boats (art/night_market/,
skiff_gallery_2), its bow tip trimmed to fit the six tiles between the
bridge's pilings and the dock to the east, and its outline firmed up: each
edge pixel takes a darker shade of its own colour (never black), as the
game's boats have. It becomes a small tilesheet of its own, and a patch puts
its tiles on the market map's Buildings layer, with the stall's action on
every one (so you can talk to Sable from the bridge or the dock). The two
floating lanterns and the mooring post that were under it go.

Sable isn't in the tiles: the mod draws them aboard from 5pm, when the stall
opens (ArtStudio/Social/NightMarketStall.cs), standing at SABLE_AT with the
hull's front rail (from RAIL_TOP down) drawn again over their feet.

Usage: build_night_market_stall.py <skiff png> <Beach-NightMarket.tmx export> <content pack assets dir>
  writes NightMarketStall.png and NightMarketStallPatch.tmx
"""
import os, sys
import xml.etree.ElementTree as ET
from PIL import Image
from xml.sax.saxutils import quoteattr

PATCH_X, PATCH_Y, W, H = 4, 37, 6, 5          # market tiles 4-9, 37-41 (the bridge is row 36)
TRIM_LEFT = 3                                 # the bow's tip, to fit 96 pixels
BOAT_TOP = 0                                  # the boat's top within the sheet: right against the bridge
SABLE_AT = (40, BOAT_TOP + 15)                # Sable's sprite's top left within the sheet
RAIL_TOP = BOAT_TOP + 41                      # the hull's front rail, drawn over Sable's feet
ACTION = "blinxxy.MuseArt_SableStall"
REMOVED = {854, 855, 856, 926, 927, 928, 998, 999, 1000,     # the floating lanterns
           946, 1018, 1090}                                   # and a mooring post
STALL_FIRST = 2000


def firm_outline(im):
    """Darkens each edge pixel (one with an empty neighbour) to a deeper
    shade of its own colour; pixels already dark are left alone."""
    px = im.load()
    w, h = im.size
    edges = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0:
                continue
            if any(not (0 <= x + dx < w and 0 <= y + dy < h) or px[x + dx, y + dy][3] == 0
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                edges.append((x, y))
    for x, y in edges:
        r, g, b, a = px[x, y]
        if 0.3 * r + 0.59 * g + 0.11 * b < 48:
            continue
        px[x, y] = (int(r * 0.45) + 6, int(g * 0.45) + 6, int(b * 0.5) + 10, 255)
    return im


def market_buildings(tmx):
    root = ET.parse(tmx).getroot()
    width = int(root.get("width"))
    for layer in root.findall("layer"):
        if layer.get("name") == "Buildings":
            values = [int(v) for v in layer.find("data").text.replace("\n", "").split(",") if v.strip()]
            return lambda x, y: values[y * width + x]
    raise SystemExit("no Buildings layer")


def main():
    skiff_png, market_tmx, assets = sys.argv[1:4]
    boat = Image.open(skiff_png).convert("RGBA")
    boat = boat.crop(boat.getbbox())
    boat = boat.crop((TRIM_LEFT, 0, boat.width, boat.height))
    assert boat.width <= W * 16, boat.width
    sheet = Image.new("RGBA", (W * 16, H * 16), (0, 0, 0, 0))
    sheet.alpha_composite(firm_outline(boat), ((W * 16 - boat.width) // 2, BOAT_TOP))
    sheet.save(os.path.join(assets, "NightMarketStall.png"))

    existing = market_buildings(market_tmx)
    buildings = [[0] * W for _ in range(H)]
    actions = []
    for ty in range(H):
        for tx in range(W):
            cell = sheet.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16))
            # (The top row, below the bridge, always gets a tile, even an
            # empty one, so the action's there wherever you face the boat.)
            if cell.getbbox() or ty == 0 and existing(PATCH_X + tx, PATCH_Y + ty) == 0:
                buildings[ty][tx] = STALL_FIRST + ty * W + tx
                actions.append((tx, ty))
            else:
                gid = existing(PATCH_X + tx, PATCH_Y + ty)
                buildings[ty][tx] = 0 if gid in REMOVED else gid
                if ty == 0 and gid:
                    actions.append((tx, ty))
    write(os.path.join(assets, "NightMarketStallPatch.tmx"), buildings, actions)
    print("wrote the stall: sheet", sheet.size, ";", len(actions), "boat tiles, at market", PATCH_X, PATCH_Y)


def write(path, buildings, actions):
    data = ",\n".join(",".join(str(v) for v in row) for row in buildings)
    objects = "\n".join(
        f'    <object id="{i + 1}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">\n'
        f'      <properties><property name="Action" value={quoteattr(ACTION)} /></properties>\n    </object>'
        for i, (x, y) in enumerate(actions))
    xml = f'''<?xml version="1.0" encoding="utf-8"?>
<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="3" nextobjectid="{len(actions) + 1}">
  <tileset firstgid="1" name="night market" tilewidth="16" tileheight="16" tilecount="1368" columns="72">
    <image source="night_market_tilesheet_objects" width="1152" height="304" />
  </tileset>
  <tileset firstgid="{STALL_FIRST}" name="z_blinxxy.MuseArt_NightMarketStall" tilewidth="16" tileheight="16" tilecount="{W * H}" columns="{W}">
    <image source="NightMarketStall.png" width="{W * 16}" height="{H * 16}" />
  </tileset>
  <layer id="1" name="Buildings" width="{W}" height="{H}">
    <data encoding="csv">
{data}
    </data>
  </layer>
  <objectgroup id="2" name="Buildings">
{objects}
  </objectgroup>
</map>
'''
    open(path, "w", encoding="utf-8").write(xml)


if __name__ == "__main__":
    main()
