"""Generation controls for the art shop's furnishings (PixelLab Create Image
Pro): flat view guides for each kind of piece, and style crops of Pierre's
shop (stocked shelves, bins, crates) at native resolution. These steer the
generator; they aren't shipped.

The user wanted the shop to feel like a local craft store, stocked the way
Pierre's is (2026-10-24), instead of a counter and a few paintings. Pieces
are drawn in Stardew's interior camera: a front face, with the top seen as
a band above it.

Usage: shop_interior_guides.py <exports dir> <out dir>
"""
import os, subprocess, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
TOP, FRONT, SHELF, GOODS, FEET = (214, 170, 116), (160, 104, 60), (96, 56, 34), (200, 120, 150), (70, 40, 26)


def shelf_unit(path):
    """64x64: a freestanding shop shelf 4 tiles wide, 3 tall: its top, with
    a few things standing on it, and three stocked shelves on the front."""
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 14, 63, 22], fill=TOP)
    for x in (4, 18, 34, 48):
        d.rectangle([x, 6, x + 8, 16], fill=GOODS)
    d.rectangle([0, 22, 63, 60], fill=FRONT)
    for y in (24, 36, 48):
        d.rectangle([3, y, 60, y + 9], fill=GOODS)
        d.rectangle([3, y + 10, 60, y + 11], fill=SHELF)
    d.rectangle([2, 60, 6, 63], fill=FEET)
    d.rectangle([57, 60, 61, 63], fill=FEET)
    im.save(path)


def low_shelf(path):
    """64x64 with the shelf in the bottom 44: a low, wide display shelf for
    the middle of the floor (4 tiles wide, under 3 tall), so the room stays
    open over it like Pierre's. The first shelves came back 4 tiles tall."""
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x in (3, 15, 27, 39, 51):
        d.rectangle([x, 20, x + 9, 27], fill=GOODS)
    d.rectangle([0, 26, 63, 32], fill=TOP)
    d.rectangle([0, 32, 63, 60], fill=FRONT)
    for y in (34, 47):
        d.rectangle([3, y, 60, y + 10], fill=GOODS)
        d.rectangle([3, y + 11, 60, y + 12], fill=SHELF)
    d.rectangle([2, 60, 6, 63], fill=FEET)
    d.rectangle([57, 60, 61, 63], fill=FEET)
    im.save(path)


# --- Vanilla scale (2026-10-24) -------------------------------------------
# The first pieces came out a third too big beside the game's own counter
# and fireplace (the user's note). These guides draw them at the sizes
# vanilla furniture has: Pierre's shelves are under 3 tiles tall, Leah's
# bookcase 2x3, paintings 1-2 tiles.

def low_shelf_small(path):
    """48x48 with the shelf in the bottom 30: 3 tiles wide, 2 tall."""
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x in (2, 12, 22, 32, 40):
        d.rectangle([x, 18, x + 6, 22], fill=GOODS)
    d.rectangle([0, 22, 47, 26], fill=TOP)
    d.rectangle([0, 26, 47, 45], fill=FRONT)
    for y in (28, 37):
        d.rectangle([2, y, 45, y + 6], fill=GOODS)
        d.rectangle([2, y + 7, 45, y + 8], fill=SHELF)
    d.rectangle([1, 45, 4, 47], fill=FEET)
    d.rectangle([43, 45, 46, 47], fill=FEET)
    im.save(path)


def tall_rack_small(path):
    """48x48 with a 30x46 rack: 2 tiles wide, 3 tall, against the wall."""
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x0 = 9
    d.rectangle([x0, 2, x0 + 29, 5], fill=TOP)
    d.rectangle([x0, 5, x0 + 29, 47], fill=FRONT)
    for y in (8, 21, 34):
        d.rectangle([x0 + 2, y, x0 + 27, y + 9], fill=GOODS)
        d.rectangle([x0 + 2, y + 10, x0 + 27, y + 11], fill=SHELF)
    im.save(path)


def side_column(path):
    """32x96 with a 16-wide column on the left: cubbies on the left-hand
    wall, one tile wide and five tall, like Pierre's seed bins: each cubby
    shows its goods from the front."""
    im = Image.new("RGBA", (32, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 14, 15, 95], fill=FRONT)
    for y in range(16, 94, 16):
        d.rectangle([2, y, 13, y + 11], fill=GOODS)
        d.rectangle([1, y + 12, 14, y + 13], fill=SHELF)
    im.save(path)


def painting_small(path):
    """32x32 with a 28x20 framed painting: 2 tiles wide, under 1.5 tall."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([2, 6, 29, 25], fill=(150, 100, 50))
    d.rectangle([4, 8, 27, 23], fill=(120, 170, 200))
    d.rectangle([4, 17, 27, 23], fill=(90, 150, 80))
    im.save(path)


def gallery_small(path):
    """48x48 with a 40x36 piece: gallery furniture 3 tiles wide at most."""
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([4, 12, 43, 42], fill=(236, 236, 230))
    d.rectangle([12, 18, 35, 32], fill=(120, 170, 200))
    d.rectangle([6, 42, 9, 47], fill=FEET)
    d.rectangle([38, 42, 41, 47], fill=FEET)
    im.save(path)


def wall_bin(path):
    """32x32 with a 14x11 tray in the middle: one of the open bins stacked
    down the left-hand wall like Pierre's seed bins, seen from above, full
    of one kind of goods. (The tall cubby case there first was wrong: the
    user wants sideways bins.)"""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([9, 10, 22, 20], fill=FRONT)
    d.rectangle([10, 11, 21, 18], fill=GOODS)
    d.rectangle([9, 19, 22, 20], fill=SHELF)
    im.save(path)


def painting_band(path):
    """32x32 with a 26x20 framed painting: wall art fits the wallpaper band
    above the wainscot (24 pixels), so it doesn't cut into the panelling."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([3, 6, 28, 25], fill=(150, 100, 50))
    d.rectangle([5, 8, 26, 23], fill=(120, 170, 200))
    d.rectangle([5, 17, 26, 23], fill=(90, 150, 80))
    im.save(path)


def wall_shelf(path):
    """32x48 inside 64x64: a tall shelf or pegboard 2 tiles wide, 3 tall,
    standing against the wall."""
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x0 = 16
    d.rectangle([x0, 16, x0 + 31, 20], fill=TOP)
    d.rectangle([x0, 20, x0 + 31, 63], fill=FRONT)
    for y in (23, 36, 49):
        d.rectangle([x0 + 3, y, x0 + 28, y + 10], fill=GOODS)
        d.rectangle([x0 + 3, y + 11, x0 + 28, y + 12], fill=SHELF)
    im.save(path)


def small_prop(path):
    """32x32: a crate, bin or barrel 2 tiles wide, its top seen above its
    front, full of goods."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([2, 8, 29, 16], fill=GOODS)
    d.rectangle([2, 14, 29, 30], fill=FRONT)
    d.rectangle([2, 14, 29, 16], fill=TOP)
    im.save(path)


def painting(path):
    """32x32: a framed painting hung on a wall, 2 tiles wide, 1.5 tall."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([1, 6, 30, 27], fill=(150, 100, 50))
    d.rectangle([4, 9, 27, 24], fill=(120, 170, 200))
    d.rectangle([4, 18, 27, 24], fill=(90, 150, 80))
    im.save(path)


def crop(exports, path, region):
    render = os.path.join(os.path.dirname(HERE), "render_tmx.py")
    subprocess.run([sys.executable, render, os.path.join(exports, "SeedShop.tmx"), path,
                    "--region", *map(str, region), "--scale", "1"], check=True)


if __name__ == "__main__":
    exports, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    for make in (shelf_unit, low_shelf, wall_shelf, small_prop, painting,
                 low_shelf_small, tall_rack_small, side_column, painting_small, gallery_small,
                 wall_bin, painting_band):
        make(os.path.join(out, make.__name__ + "_guide.png"))
    crop(exports, os.path.join(out, "pierre_shelves.png"), (9, 15, 8, 9))     # the two stocked shelf units and the aisle ends
    crop(exports, os.path.join(out, "pierre_bins.png"), (1, 17, 7, 10))       # seed bins, produce crates, barrels, baskets
    print("guides written to", out)
