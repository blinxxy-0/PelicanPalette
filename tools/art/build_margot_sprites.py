"""Builds Margot Pell's walking sprite sheet (Characters/<id>: 64x128,
16x32 frames; rows walk down, right, up, left, 4 frames each, as vanilla)
from the PixelLab candidate the user picked (walk_emily_0, 2026-11-01,
drawn with Emily's sheet as the style reference; art/margot/picks/walk.png).

Vanilla clean-up as Julian's (build_julian_sprites.py): hard alpha, no
pure-black outlines, one shared palette. PixelLab drew her hair nearly
black-maroon, so beside vanilla NPCs she read as a dark blot: the hair's
inside is moved onto the portrait's auburn ramp, its outer edge kept dark.

Usage: build_margot_sprites.py <art/margot/picks dir> <out.png>
"""
import colorsys, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black

COLOURS = 24
# The portrait's auburn, darkest to lightest (portraits.png).
AUBURN = [(67, 3, 33), (82, 8, 38), (96, 11, 41), (116, 21, 42), (135, 33, 44), (152, 44, 43), (177, 60, 59)]


def is_hair(p):
    h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in p[:3]))
    return (h > 0.83 or h < 0.03) and s > 0.6 and v < 0.45


def auburn(im):
    """Moves the hair's inside (not its edge) onto the auburn ramp, by value."""
    px = im.load()
    w, h = im.size
    changes = {}
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if not p[3] or not is_hair(p):
                continue
            edge = any(not (0 <= x + dx < w and 0 <= y + dy < h) or px[x + dx, y + dy][3] == 0
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                continue
            v = max(p[:3]) / 255
            # Value 0.14..0.45 spread over the ramp.
            i = min(len(AUBURN) - 1, max(0, round((v - 0.14) / 0.31 * (len(AUBURN) - 1))))
            changes[(x, y)] = AUBURN[i] + (255,)
    for k, c in changes.items():
        px[k] = c
    return im


def main():
    picks, out = sys.argv[1], sys.argv[2]
    im = Image.open(os.path.join(picks, "walk.png")).convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    im = quantize(recolour_black(im), COLOURS)
    im = auburn(im)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out)
    print(f"wrote {out}: {len(set(p for p in im.get_flattened_data() if p[3]))} colours")


if __name__ == "__main__":
    main()
