"""Packs the art shop's furnishings into one tilesheet, from the PixelLab
picks (Create Image Pro, art/shop_interior/batch.json, 2026-10-24).

Each pick is cropped to its pixels and set on the tile grid: centred
across whole tiles, its base on the bottom edge of the last tile row, so
it stands on the floor like Pierre's shelves. Black outlines are softened
to vanilla's dark browns. The sheet is ArtShopFurniture.png, and
furniture.json lists where each piece is (in tiles) for
build_shop_interior.py.

Usage: build_shop_furniture.py <PixelLab generations dir> <sheet dir> <furniture.json>
"""
import json, math, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import recolour_black

SHEET_TILES = 16            # sheet width in tiles

# name: (batch folder/job, candidate number). Sizes come from the pixels.
BATCH = "stardew-artshop-interior/"
PICKS = {
    # At vanilla scale (the user found the first, bigger set out of
    # proportion with the counter and fireplace): floor shelves 3x2 tiles
    # like Pierre's, racks 2x3 like Leah's bookcase.
    # Paint shelves drawn in the style of small_craft 1 (the angle the user
    # liked; the first paint shelves leaned).
    "low_paints": ("front_paints", 11),
    "low_tubes": ("front_paints", 13),
    "low_craft": ("small_craft", 1),
    "low_craft_b": ("small_craft", 7),
    "rack_cubbies": ("small_racks", 1),
    "rack_canvases": ("small_racks", 2),
    "rack_paper": ("small_racks", 4),
    # On the floor.
    "crate_tubes": ("props_bins", 3),
    "barrel_brushes": ("props_bins", 21),
    "bin_paper": ("props_bins", 36),
    "basket_yarn": ("props_bins", 44),
    "stack_canvases": ("props_bins", 49),
    "crate_frames": ("props_bins", 61),
    # Sable's paintings on the walls, every one different.
    # Wall art fits the wallpaper band above the wainscot (24 pixels); the
    # first paintings cut into the panelling.
    "painting_river": ("band_paintings", 1),
    "painting_lighthouse": ("small_paintings", 9),
    "painting_sunset_sea": ("small_paintings", 17),
    "painting_snow_cabin": ("small_paintings", 25),
    "painting_night": ("small_paintings", 33),
    "painting_flowers": ("small_paintings", 49),
    "painting_fruit_jug": ("band_paintings", 4),
    "painting_cabin": ("band_paintings", 7),
    "painting_tree": ("paintings", 10),
    "painting_blocks": ("band_paintings", 40),
    "painting_mosaic": ("band_paintings", 50),
    "painting_stripes": ("band_paintings", 54),
    # The gallery evening (the 14-heart event's dressing).
    "gallery_panel_landscape": ("small_gallery", 1),
    "gallery_panel_portraits": ("small_gallery", 4),
    "gallery_bust": ("small_gallery", 11),
    "gallery_wine": ("small_gallery", 17),
}


def drop_specks(im):
    """Clears loose clusters of pixels (the generator sometimes leaves a few
    dots under a piece) that are much smaller than the piece itself."""
    px = im.load()
    seen, parts = set(), []
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3] and (x, y) not in seen:
                part, todo = [], [(x, y)]
                seen.add((x, y))
                while todo:
                    cx, cy = todo.pop()
                    part.append((cx, cy))
                    for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                        if 0 <= nx < im.width and 0 <= ny < im.height and px[nx, ny][3] and (nx, ny) not in seen:
                            seen.add((nx, ny))
                            todo.append((nx, ny))
                parts.append(part)
    biggest = max(len(p) for p in parts)
    for part in parts:
        if len(part) < biggest * 0.05:
            for x, y in part:
                px[x, y] = (0, 0, 0, 0)
    return im


def load(gen_dir, job, n):
    if "/" not in job:
        job = BATCH + job
    im = Image.open(os.path.join(gen_dir, job, f"{job.split('/')[-1]}_{n}.png")).convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    im = drop_specks(im)
    return recolour_black(im.crop(im.getbbox()))


def on_grid(im, hang=False):
    """Sets a piece on whole tiles, centred across them. Furniture stands on
    the bottom edge; wall art (hang) hangs one pixel below the top edge, so
    a painting placed on the wall's first row sits inside the wallpaper band
    (24 pixels above the wainscot rail) without cutting into the panelling."""
    w, h = math.ceil(im.width / 16), math.ceil((im.height + (1 if hang else 0)) / 16)
    tile = Image.new("RGBA", (w * 16, h * 16), (0, 0, 0, 0))
    tile.alpha_composite(im, ((w * 16 - im.width) // 2, 1 if hang else h * 16 - im.height))
    return tile


# Bins of supplies stacked down the left-hand wall, like Pierre's seed bins:
# paint tubes, brushes, coloured pencils, pastels, yarn, rolled paper, clay.
BINS = [1, 17, 25, 33, 42, 49, 54]
BIN_PITCH = 13          # each bin sits this far below the last, overlapping its back


def column(bins):
    """The bins stacked down the wall, each drawn over the back of the one
    above it (it's nearer the camera)."""
    width = max(b.width for b in bins)
    height = BIN_PITCH * (len(bins) - 1) + bins[-1].height
    col = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    for i, b in enumerate(bins):
        col.alpha_composite(b, ((width - b.width) // 2, i * BIN_PITCH))
    return on_grid(col)


def pack(pieces):
    """Shelf packing, tallest first, rows of SHEET_TILES tiles."""
    order = sorted(pieces, key=lambda n: (-pieces[n].height, -pieces[n].width))
    x = y = row_h = 0
    places = {}
    for name in order:
        w, h = pieces[name].width // 16, pieces[name].height // 16
        if x + w > SHEET_TILES:
            x, y, row_h = 0, y + row_h, 0
        places[name] = {"x": x, "y": y, "w": w, "h": h}
        x += w
        row_h = max(row_h, h)
    sheet = Image.new("RGBA", (SHEET_TILES * 16, (y + row_h) * 16), (0, 0, 0, 0))
    for name, p in places.items():
        sheet.alpha_composite(pieces[name], (p["x"] * 16, p["y"] * 16))
    return sheet, places


# Easels in Sable's corner: our own easel sprite (the Easel item's) with a
# different painting on each canvas, so no two match (the old corner used
# Leah's easels, which show the same two canvases twice).
EASEL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "ArtStudio", "assets", "bigcraftables.png")
CANVAS_BOX = (3, 5, 13, 16)         # the easel canvas's picture area (10x11)
EASELS = {"easel_lighthouse": "painting_lighthouse", "easel_sunset": "painting_sunset_sea", "easel_tree": "painting_tree"}


def easel_with(painting):
    """The easel, its canvas showing `painting` (its picture, frame cropped
    off, scaled down to the canvas)."""
    easel = Image.open(EASEL).convert("RGBA").crop((0, 0, 16, 32))
    inner = painting.crop((4, 4, painting.width - 4, painting.height - 4))
    w, h = CANVAS_BOX[2] - CANVAS_BOX[0], CANVAS_BOX[3] - CANVAS_BOX[1]
    thumb = inner.resize((w, h), Image.BOX).convert("RGB").quantize(8).convert("RGBA")
    easel.paste(thumb, CANVAS_BOX[:2])
    return easel


def main():
    gen_dir, out, index = sys.argv[1], sys.argv[2], sys.argv[3]
    raw = {name: load(gen_dir, job, n) for name, (job, n) in PICKS.items()}
    pieces = {name: on_grid(im, hang=name.startswith("painting_")) for name, im in raw.items()}
    pieces["bins_column"] = column([load(gen_dir, "wall_bins", n) for n in BINS])
    for name, painting in EASELS.items():
        pieces[name] = easel_with(raw[painting])
    sheet, places = pack(pieces)
    sheet.save(os.path.join(out, "ArtShopFurniture.png"))
    json.dump(places, open(index, "w"), indent=1)
    print(f"packed {len(places)} pieces into {sheet.width // 16}x{sheet.height // 16} tiles")


if __name__ == "__main__":
    main()
