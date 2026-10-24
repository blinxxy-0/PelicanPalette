"""Generation controls for the art shop's exterior (PixelLab Create Image
Pro): flat guides pinning the camera and the shop's outline, and style
crops of vanilla buildings. These steer the generator; they aren't shipped.

The first exterior (2026-10-23) came from `guide`, whose roof was a short
band: the user found it flat ("almost 2d"). Stardew's buildings are seen
from high above, so the roof is over half the building's height and is
built of planes that catch the light differently: a top slope seen from
above, front-facing gables, a ridge, a dark band of shadow under the
eaves. The `roof_*` guides draw those planes in separate tones.

Usage: shop_guides.py <exports dir> <out dir>
"""
import os, subprocess, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))

# Guide tones: the roof's planes from light (facing up) to dark (facing the
# camera), the eave shadow, and the walls.
TOP, SLOPE, GABLE_ROOF, EAVE = (96, 160, 168), (52, 110, 120), (36, 82, 92), (40, 30, 34)
WALL, WALL_SHADE, TIMBER = (226, 214, 180), (196, 180, 146), (120, 84, 56)
DOOR, GLASS, SIGN, CHIMNEY = (110, 72, 40), (150, 200, 220), (150, 100, 50), (140, 80, 64)


def guide(path):
    """112x112: a gabled cottage, front face square-on, roof seen as a band
    above it, no side faces. Door right of centre, display window left."""
    im = Image.new("RGBA", (112, 112), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    roof, wall, door, glass, sign, chimney = (52, 110, 120), (226, 214, 180), (110, 72, 40), (150, 200, 220), (150, 100, 50), (120, 80, 70)
    d.rectangle([76, 2, 88, 24], fill=chimney)
    d.polygon([(2, 54), (20, 10), (92, 10), (110, 54)], fill=roof)
    d.rectangle([8, 54, 104, 106], fill=wall)
    d.rectangle([14, 64, 50, 92], fill=glass)            # shop display window
    d.rectangle([62, 72, 80, 106], fill=door)
    d.rectangle([86, 66, 100, 84], fill=glass)            # small window
    d.rectangle([60, 58, 82, 68], fill=sign)              # hanging sign
    im.save(path)


def storefront(d, top):
    """The ground floor from `top` down to the base: walls, timber, a wide
    display window on the left, the door right of centre, the eave shadow."""
    d.rectangle([6, top, 105, 111], fill=WALL)
    d.rectangle([6, top, 105, top + 4], fill=EAVE)
    for x in (6, 56, 101):
        d.rectangle([x, top + 4, x + 4, 111], fill=TIMBER)
    d.rectangle([14, top + 12, 50, top + 34], fill=GLASS)
    d.rectangle([66, top + 14, 84, 111], fill=DOOR)
    d.rectangle([88, top + 14, 98, top + 26], fill=GLASS)


def roof_gable(path):
    """A long roof whose top slope is seen from above, with a front-facing
    gable over the display window: like Pierre's shop."""
    im = Image.new("RGBA", (112, 112), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([80, 2, 90, 26], fill=CHIMNEY)
    d.polygon([(0, 62), (10, 14), (102, 14), (112, 62)], fill=SLOPE)        # main slope
    d.polygon([(10, 14), (102, 14), (98, 24), (14, 24)], fill=TOP)           # ridge catching the light
    # The front gable: its two roof edges, then the gable wall with a round window.
    d.polygon([(2, 66), (32, 8), (62, 66), (54, 66), (32, 22), (10, 66)], fill=GABLE_ROOF)
    d.polygon([(10, 66), (32, 22), (54, 66)], fill=WALL_SHADE)
    d.ellipse([26, 40, 38, 52], fill=GLASS)
    storefront(d, 64)
    im.save(path)


def roof_cross(path):
    """Two sections like Leah's cottage: a wide roof on the left seen from
    above, and a taller front gable on the right over the door."""
    im = Image.new("RGBA", (112, 112), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([14, 4, 24, 26], fill=CHIMNEY)
    d.polygon([(0, 64), (8, 16), (70, 16), (70, 64)], fill=SLOPE)
    d.polygon([(8, 16), (70, 16), (70, 26), (10, 26)], fill=TOP)
    d.polygon([(52, 68), (82, 4), (112, 68), (104, 68), (82, 20), (60, 68)], fill=GABLE_ROOF)
    d.polygon([(60, 68), (82, 20), (104, 68)], fill=WALL_SHADE)
    d.rectangle([76, 36, 88, 50], fill=GLASS)
    storefront(d, 66)
    im.save(path)


def roof_dormer(path):
    """A tall, steep roof across the whole front, seen from above, with a
    dormer window set into it and a chimney at the ridge."""
    im = Image.new("RGBA", (112, 112), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([84, 0, 94, 22], fill=CHIMNEY)
    d.polygon([(0, 64), (12, 8), (100, 8), (112, 64)], fill=SLOPE)
    d.polygon([(12, 8), (100, 8), (97, 18), (15, 18)], fill=TOP)
    # The dormer: a little gabled roof over a window, sticking out of the slope.
    d.polygon([(30, 34), (46, 18), (62, 34)], fill=GABLE_ROOF)
    d.rectangle([34, 34, 58, 50], fill=WALL_SHADE)
    d.rectangle([38, 37, 54, 48], fill=GLASS)
    d.rectangle([34, 50, 58, 54], fill=EAVE)
    storefront(d, 64)
    im.save(path)


def style(exports, path):
    """The vanilla Forest around Leah's cottage, at native resolution."""
    render = os.path.join(os.path.dirname(HERE), "render_tmx.py")
    subprocess.run([sys.executable, render, os.path.join(exports, "Forest.tmx"), path,
                    "--region", "99", "24", "14", "11", "--scale", "1"], check=True)


def style_town(exports, path):
    """Pierre's shop and Harvey's clinic, at native resolution: deep roofs
    with gables, ridges and shading."""
    render = os.path.join(os.path.dirname(HERE), "render_tmx.py")
    subprocess.run([sys.executable, render, os.path.join(exports, "Town.tmx"), path,
                    "--region", "33", "46", "16", "11", "--scale", "1"], check=True)


if __name__ == "__main__":
    exports, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    guide(os.path.join(out, "shop_guide.png"))
    for make in (roof_gable, roof_cross, roof_dormer):
        make(os.path.join(out, make.__name__ + "_guide.png"))
    style(exports, os.path.join(out, "shop_style.png"))
    style_town(exports, os.path.join(out, "town_style.png"))
    print("guides written to", out)
