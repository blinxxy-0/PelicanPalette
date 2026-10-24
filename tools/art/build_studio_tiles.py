"""Packs Sable's own studio furniture into one tilesheet for the spouse room
and patio (SableStudio.png) plus a layout file the map builder reads
(SableStudio.json: each piece's tile position and size on the sheet).

The pieces were made with PixelLab (Create Image Pro, vanilla spouse-room
furniture as the style reference, 2026-10-24) and picked by hand; the
sources are art/sable_room/final_*.png. Each one is cropped to its pixels,
given hard alpha, and set into a whole number of tiles: standing furniture
sits on the bottom edge, wall and floor pieces are centred.

Usage: build_studio_tiles.py <art/sable_room dir> <content pack assets dir>
"""
import json, math, os, sys
from PIL import Image

# piece: (source, anchor) - "bottom" for furniture that stands on the floor.
PIECES = {
    "desk": ("final_desk.png", "bottom"),
    "cabinet": ("final_cabinet.png", "bottom"),
    "easel": ("final_easel.png", "bottom"),
    "rack": ("final_rack.png", "bottom"),
    "table": ("final_table.png", "bottom"),
    "rug": ("final_rug.png", "center"),
    "board": ("final_board.png", "center"),
}
SHEET_TILES = 8                      # sheet width in tiles


def block(path, anchor):
    im = Image.open(path).convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    im = im.crop(im.getbbox())
    cols, rows = math.ceil(im.width / 16), math.ceil(im.height / 16)
    out = Image.new("RGBA", (cols * 16, rows * 16), (0, 0, 0, 0))
    x = (out.width - im.width) // 2
    y = out.height - im.height if anchor == "bottom" else (out.height - im.height) // 2
    out.alpha_composite(im, (x, y))
    return out, cols, rows


def main():
    src, assets = sys.argv[1], sys.argv[2]
    blocks = {name: block(os.path.join(src, f), anchor) for name, (f, anchor) in PIECES.items()}
    # Shelf packing, left to right, a new shelf when the row is full.
    layout, x, y, shelf = {}, 0, 0, 0
    for name, (im, cols, rows) in blocks.items():
        if x + cols > SHEET_TILES:
            x, y, shelf = 0, y + shelf, 0
        layout[name] = {"x": x, "y": y, "cols": cols, "rows": rows}
        x += cols
        shelf = max(shelf, rows)
    height = y + shelf
    sheet = Image.new("RGBA", (SHEET_TILES * 16, height * 16), (0, 0, 0, 0))
    for name, (im, cols, rows) in blocks.items():
        sheet.alpha_composite(im, (layout[name]["x"] * 16, layout[name]["y"] * 16))
    sheet.save(os.path.join(assets, "SableStudio.png"))
    json.dump({"tiles_wide": SHEET_TILES, "tiles_high": height, "pieces": layout},
              open(os.path.join(src, "SableStudio.json"), "w"), indent=2)
    for name, p in layout.items():
        print(f"{name}: {p['cols']}x{p['rows']} at {p['x']},{p['y']}")


if __name__ == "__main__":
    main()
