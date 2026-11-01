"""Adds the back room's two pieces (1.2) to the shop's furniture sheet: the
plan chest and the portfolio book's stand, PixelLab picks from
art/back_room/batch.json (Create Image Pro, 2026-10-31). Each is cut out,
given vanilla outlines and set on the tile grid as build_shop_furniture.py
does, then put in a free spot on the existing sheet (or new rows below it),
so nothing already there moves. furniture.json gets their places.

Usage: back_room.py <PixelLab generations dir> <ArtShopFurniture.png> <furniture.json>
"""
import json, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_furniture import drop_specks, on_grid, SHEET_TILES
from vanillafy import recolour_black, selout

BATCH = "stardew-artshop-backroom/"
# The user left the choice to me: the darker plan chest (it stands out from
# the back room's light floor) and the stand with landscapes on its open pages.
PICKS = {
    "plan_chest": ("plan_chest", 23),
    "lectern": ("lectern", 47),
}
# Drawn pixel by pixel (tools/art/back_room_door.py), already tile-sized: the
# sign and the window for the back room's door in the shop.
LOCAL = {
    "door_sign": "art/back_room/door_sign.png",
    "door_window": "art/back_room/door_window.png",
}


def load(gen_dir, job, n):
    """A candidate as build_shop_furniture.load cuts one (this batch keeps
    its candidates in one folder, not one per job)."""
    im = Image.open(os.path.join(gen_dir, BATCH, f"{job}_{n}.png")).convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    im = drop_specks(im)
    return recolour_black(im.crop(im.getbbox()))


def free_spot(taken, w, h, rows):
    for y in range(rows - h + 1):
        for x in range(SHEET_TILES - w + 1):
            if not any((x + i, y + j) in taken for i in range(w) for j in range(h)):
                return x, y
    return None


def main():
    gen_dir, sheet_path, index = sys.argv[1], sys.argv[2], sys.argv[3]
    sheet = Image.open(sheet_path).convert("RGBA")
    places = json.load(open(index))
    taken = {(p["x"] + i, p["y"] + j) for p in places.values() for i in range(p["w"]) for j in range(p["h"])}
    here = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
    pieces = [(name, on_grid(selout(load(gen_dir, job, n)))) for name, (job, n) in PICKS.items()]
    pieces += [(name, Image.open(os.path.join(here, path)).convert("RGBA")) for name, path in LOCAL.items()]
    for name, piece in pieces:
        w, h = piece.width // 16, piece.height // 16
        rows = sheet.height // 16
        spot = places.get(name) and (places[name]["x"], places[name]["y"]) or free_spot(taken, w, h, rows)
        if spot is None:
            grown = Image.new("RGBA", (sheet.width, (rows + h) * 16), (0, 0, 0, 0))
            grown.alpha_composite(sheet)
            sheet, spot = grown, (0, rows)
        x, y = spot
        sheet.paste(Image.new("RGBA", (w * 16, h * 16), (0, 0, 0, 0)), (x * 16, y * 16))
        sheet.alpha_composite(piece, (x * 16, y * 16))
        places[name] = {"x": x, "y": y, "w": w, "h": h}
        taken |= {(x + i, y + j) for i in range(w) for j in range(h)}
        print(f"{name}: {w}x{h} tiles at {x},{y}")
    sheet.save(sheet_path)
    json.dump(places, open(index, "w"), indent=1)


if __name__ == "__main__":
    main()
