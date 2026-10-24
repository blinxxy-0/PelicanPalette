"""Generation controls for the art shop's exterior (PixelLab Create Image
Pro): a flat guide pinning Stardew's square-on 3/4 camera and the shop's
outline, and a style crop of the vanilla Forest around Leah's cottage.
These steer the generator; they aren't shipped.

Usage: shop_guides.py <exports dir> <out dir>
"""
import os, subprocess, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))


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


def style(exports, path):
    """The vanilla Forest around Leah's cottage, at native resolution."""
    render = os.path.join(os.path.dirname(HERE), "render_tmx.py")
    subprocess.run([sys.executable, render, os.path.join(exports, "Forest.tmx"), path,
                    "--region", "99", "24", "14", "11", "--scale", "1"], check=True)


if __name__ == "__main__":
    exports, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    guide(os.path.join(out, "shop_guide.png"))
    style(exports, os.path.join(out, "shop_style.png"))
    print("guides written to", out)
