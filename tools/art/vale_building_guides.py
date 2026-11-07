"""Shape guides for the Gilded Vale's buildings (PixelLab reference images).

Flat colour planes that fix the camera, silhouette and roof shape, like
art/shop_exterior/roof_gable_guide.png. Measured against the game's own
houses (Sam's, Emily and Haley's, the Saloon; 2026-11-03):

  - Most roofs are FRONT GABLES: the ridge runs back away from the viewer,
    so the roof shows as two steep, thick planes (lit on the left, shaded on
    the right) behind a tall triangular gable end, and the ridge recedes far
    up the picture.
  - The roof is about 60% of the building's height, and the buildings are
    taller than they are wide (Sam's house is about 8 tiles wide, 11 tall).
  - Wide buildings are two gables side by side (Emily and Haley's) or a
    gable with lower side wings, never one flat band.

The first guides (a flat band with a lid, then thin rims round a big wall
triangle) were wrong, per the user (2026-11-03).

One guide per building at its size in pixels (16 per tile), into
art/gilded_vale/buildings/<name>_guide.png.

Usage: python tools/art/vale_building_guides.py
"""
import os
from PIL import Image, ImageDraw

OUT = "art/gilded_vale/buildings"
WALL, BEAM, WIN, DOOR, CHIM, SHADOW = (232, 214, 176), (120, 82, 54), (150, 196, 214), (110, 70, 44), (140, 84, 64), (60, 44, 40)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


def gable(d, cx, half, wall_top, base, roof_col, wall=None):
    """A front gable centred on cx, half wide, its walls from wall_top down to base.
    Like Sam's house and the Saloon: a low, wide gable triangle at the front, and
    the ridge running far back up the picture, so the two roof planes (lit left,
    shaded right) make most of the building's height.
    Returns (peak y, ridge-end y, the right-hand plane as a function (u, v) -> point)."""
    wall = wall or WALL
    peak = wall_top - int(half * 0.55)
    depth = int(half * 1.3)
    x0, x1 = cx - half, cx + half
    lit, dark = shade(roof_col, 1.12), shade(roof_col, 0.84)
    d.polygon([(x0 - 3, wall_top + 4), (cx, peak), (cx, peak - depth), (x0 - 3, wall_top + 4 - depth)], fill=lit)
    d.polygon([(cx, peak), (x1 + 3, wall_top + 4), (x1 + 3, wall_top + 4 - depth), (cx, peak - depth)], fill=dark)
    d.line([(cx, peak), (cx, peak - depth)], fill=shade(roof_col, 1.4), width=2)
    for k in range(1, 6):  # rows of shingles, parallel to the eaves
        t = k / 6
        d.line([(x0 - 3, wall_top + 4 - depth * t), (cx, peak - depth * t)], fill=shade(lit, 0.92))
        d.line([(cx, peak - depth * t), (x1 + 3, wall_top + 4 - depth * t)], fill=shade(dark, 0.9))
    d.rectangle([x0 + 2, wall_top + 4, x1 - 2, base], fill=wall)
    d.polygon([(x0 + 2, wall_top + 5), (cx, peak + 4), (x1 - 2, wall_top + 5)], fill=wall)
    d.line([(x0 - 3, wall_top + 4), (cx, peak), (x1 + 3, wall_top + 4)], fill=shade(roof_col, 0.6), width=4)
    d.rectangle([x0 + 2, wall_top + 5, x1 - 2, wall_top + 7], fill=SHADOW)

    def right_plane(u, v):
        """A point on the shaded plane: u from the ridge (0) to the eave (1), v from the front (0) to the back (1)."""
        return (cx + (x1 + 3 - cx) * u, peak + (wall_top + 4 - peak) * u - depth * v)
    return peak, peak - depth, right_plane


def wing(d, x0, x1, top, wall_top, base, roof_col, wall=None):
    """A lower side wing: a side-gable roof seen from above as one big sloping plane
    (lighter toward its ridge), with walls under it."""
    d.polygon([(x0, top), (x1, top), (x1, wall_top + 4), (x0, wall_top + 4)], fill=roof_col)
    for y in range(top + 6, wall_top, 8):
        d.line([(x0, y), (x1, y)], fill=shade(roof_col, 0.9))
    d.rectangle([x0, top, x1, top + 4], fill=shade(roof_col, 1.3))
    d.rectangle([x0, wall_top + 1, x1, wall_top + 4], fill=shade(roof_col, 0.6))
    d.rectangle([x0 + 2, wall_top + 5, x1 - 2, base], fill=wall or WALL)
    d.rectangle([x0 + 2, wall_top + 5, x1 - 2, wall_top + 8], fill=SHADOW)


def window(d, x, y, w=14, h=14):
    d.rectangle([x, y, x + w, y + h], fill=WIN, outline=BEAM)
    d.line([(x + w // 2, y), (x + w // 2, y + h)], fill=BEAM)
    d.line([(x, y + h // 2), (x + w, y + h // 2)], fill=BEAM)


def door(d, x, base, w=14, h=24):
    d.rectangle([x, base - h, x + w, base], fill=DOOR)


def chimney(d, x, y, w=10, h=26, col=CHIM):
    d.rectangle([x, y, x + w, y + h], fill=col)
    d.rectangle([x - 1, y, x + w + 1, y + 3], fill=shade(col, 0.7))


def save(img, name):
    # the whole roof must be on the canvas: a ridge running off the top came back cut off
    top = img.getbbox()[1]
    assert top >= 2, f"{name}: the guide's roof touches the top of its canvas"
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + "_guide.png"))
    print(name, img.size)


def vale_house():  # 11 x 11 tiles: an inn, two plum front gables side by side, a porch between
    img = Image.new("RGBA", (176, 176)); d = ImageDraw.Draw(img)
    plum = (128, 66, 96)
    chimney(d, 120, 8)
    for cx in (44, 132):
        peak, _, _ = gable(d, cx, 42, 112, 175, plum)
        window(d, cx - 6, peak + 8, 12, 10)
        window(d, cx - 26, 124); window(d, cx + 12, 124)
        window(d, cx - 26, 150); window(d, cx + 12, 150)
    d.polygon([(74, 140), (102, 140), (106, 148), (70, 148)], fill=shade(plum, 0.9))
    d.rectangle([72, 148, 104, 175], fill=WALL)
    door(d, 80, 175, 16, 24)
    save(img, "vale_house")


def exhibition_hall():  # 14 x 11 tiles: a tall central gable over columns, lower side wings
    img = Image.new("RGBA", (224, 176)); d = ImageDraw.Draw(img)
    stone, slate = (226, 218, 196), (92, 104, 120)
    wing(d, 2, 74, 58, 112, 175, slate, stone)
    wing(d, 150, 222, 58, 112, 175, slate, stone)
    peak, _, _ = gable(d, 112, 46, 108, 175, slate, wall=stone)
    d.ellipse([105, peak + 7, 119, peak + 21], fill=WIN, outline=BEAM)
    for x in (80, 96, 122, 138):
        d.rectangle([x, 122, x + 6, 175], fill=(246, 240, 224))
    door(d, 102, 175, 20, 34)
    for x in (14, 44, 166, 196):
        window(d, x, 128, 14, 28)
    save(img, "exhibition_hall")


def atelier():  # 9 x 11 tiles: one big indigo front gable, a skylight in its shaded slope
    img = Image.new("RGBA", (144, 176)); d = ImageDraw.Draw(img)
    indigo = (62, 70, 128)
    chimney(d, 28, 30)
    peak, _, plane = gable(d, 72, 56, 116, 175, indigo, wall=(226, 196, 128))
    corners = [plane(0.2, 0.25), plane(0.75, 0.25), plane(0.75, 0.7), plane(0.2, 0.7)]
    d.polygon(corners, fill=WIN)
    for u in (0.34, 0.48, 0.62):
        d.line([plane(u, 0.25), plane(u, 0.7)], fill=BEAM)
    window(d, 66, peak + 9, 12, 10)
    window(d, 26, 132)
    d.rectangle([78, 128, 120, 158], fill=WIN, outline=BEAM)
    door(d, 46, 175, 14, 26)
    save(img, "atelier")


def pottery():  # 9 x 10 tiles: a terracotta front gable, an open workshop bay, a round brick kiln chimney
    img = Image.new("RGBA", (144, 160)); d = ImageDraw.Draw(img)
    terracotta = (176, 92, 62)
    d.rectangle([116, 12, 138, 159], fill=(150, 76, 58))
    d.ellipse([114, 6, 140, 18], fill=(120, 60, 46))
    peak, _, _ = gable(d, 56, 50, 102, 159, terracotta, wall=(170, 170, 164))
    window(d, 50, peak + 9, 12, 10)
    d.rectangle([62, 120, 100, 159], fill=SHADOW)
    for y in (128, 142):
        d.rectangle([64, y, 98, y + 2], fill=BEAM)
    window(d, 16, 120)
    door(d, 36, 159, 14, 26)
    save(img, "pottery")


def fern_cottage():  # 10 x 9 tiles: a thatched front-gable cottage and a glass seedhouse on its right
    img = Image.new("RGBA", (160, 144)); d = ImageDraw.Draw(img)
    peak, _, _ = gable(d, 48, 42, 92, 143, (196, 170, 96))
    window(d, 42, peak + 8, 12, 10)
    window(d, 14, 104); window(d, 66, 104)
    door(d, 41, 143, 14, 26)
    # The seedhouse: the farm's greenhouse in small (its copper frames, a steep glass
    # gable, the arched fanlight over the door), per the user (2026-11-03); the game's
    # greenhouse goes to PixelLab as a second reference.
    glass, lit, copper = (170, 214, 226), (204, 232, 238), (190, 104, 58)
    d.polygon([(100, 100), (128, 58), (156, 100)], fill=lit)
    d.polygon([(128, 58), (156, 100), (158, 90), (130, 48)], fill=glass)
    d.rectangle([100, 100, 156, 143], fill=glass)
    d.line([(100, 100), (128, 58), (156, 100)], fill=copper, width=2)
    d.line([(128, 58), (128, 100)], fill=copper, width=1)
    for x in (100, 114, 142, 156):
        d.line([(x, 100), (x, 143)], fill=copper, width=2)
    d.line([(100, 100), (156, 100)], fill=copper, width=2)
    d.line([(100, 122), (156, 122)], fill=copper, width=1)
    d.pieslice([118, 118, 138, 138], 180, 360, fill=lit, outline=copper)
    d.rectangle([120, 128, 136, 143], fill=DOOR)
    save(img, "fern_cottage")


def emmett_cottage():  # 8 x 8 tiles: a small wren-brown front gable, a stone chimney, a bench by the door
    img = Image.new("RGBA", (128, 144)); d = ImageDraw.Draw(img)        # 9 tiles tall, so the ridge fits
    brown = (128, 92, 62)
    chimney(d, 90, 30, 12, 26, (150, 146, 138))
    peak, _, _ = gable(d, 60, 46, 90, 143, brown, wall=(222, 206, 176))
    window(d, 54, peak + 8, 12, 10)
    window(d, 22, 104); window(d, 86, 104)
    door(d, 53, 143, 14, 26)
    d.rectangle([20, 132, 44, 137], fill=BEAM)
    save(img, "emmett_cottage")


def old_studio():  # 6 x 9 tiles: an empty artists' studio, a big north-light window boarded up
    # (9 tiles tall, not 8: the roof's ridge ran off the top of an 8-tile canvas and
    # every candidate came back with its tip cut off, the user, 2026-11-06)
    img = Image.new("RGBA", (96, 144)); d = ImageDraw.Draw(img)
    slate = (104, 110, 104)
    peak, ridge_end, plane = gable(d, 48, 42, 88, 143, slate, wall=(200, 190, 170))
    assert ridge_end >= 4, ridge_end
    d.polygon([plane(0.2, 0.25), plane(0.8, 0.25), plane(0.8, 0.7), plane(0.2, 0.7)], fill=(150, 120, 80))
    d.rectangle([22, 100, 74, 120], fill=(150, 120, 80), outline=BEAM)
    for y in (104, 112):
        d.line([(20, y), (76, y + 4)], fill=BEAM, width=2)
    door(d, 41, 143, 14, 22)
    save(img, "old_studio")


if __name__ == "__main__":
    for f in (vale_house, exhibition_hall, atelier, pottery, fern_cottage, emmett_cottage, old_studio):
        f()
