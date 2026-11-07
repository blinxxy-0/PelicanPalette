"""Gives the Exhibition Hall's sculptures plinths of their own (the user, 2026-11-07:
"more color variety"; each colour has its reason in the plaque text). Recolours
only a plinth's grey stone, below the sculpture, by its brightness onto a new
ramp; the sculpture stays as PixelLab drew it.

Usage: vale_plinths.py   (writes art/gilded_vale/interiors/hall/plinth_*.png)
"""
import colorsys, os, random
from PIL import Image

HALL = "art/gilded_vale/interiors/hall"
# out name: (source, first plinth row, ramp dark -> light, moss specks)
PLINTHS = {
    "plinth_slate":     ("hall_v1/plinth_1_0", 14, [(34, 38, 50), (72, 84, 104), (118, 134, 156), (170, 184, 200)], False),
    "plinth_sandstone": ("hall_v1/plinth_1_5", 12, [(70, 52, 32), (146, 114, 70), (196, 166, 112), (228, 208, 160)], True),
    "plinth_brick":     ("hall_v1/plinth_1_7", 13, [(44, 18, 18), (92, 34, 30), (128, 52, 42), (164, 84, 66)], False),
}


def ramp(stops, t):
    t = max(0.0, min(1.0, t)) * (len(stops) - 1)
    i = min(int(t), len(stops) - 2)
    f = t - i
    return tuple(round(stops[i][k] + (stops[i + 1][k] - stops[i][k]) * f) for k in range(3))


def main():
    rnd = random.Random(7)
    for out, (src, y0, stops, moss) in PLINTHS.items():
        im = Image.open(os.path.join(HALL, src + ".png")).convert("RGBA")
        px = im.load()
        greys = [(x, y) for y in range(y0, im.height) for x in range(im.width)
                 if px[x, y][3] and colorsys.rgb_to_hsv(*[c / 255 for c in px[x, y][:3]])[1] < 0.2]
        lums = [sum(px[x, y][:3]) / 765 for x, y in greys]
        lo, hi = min(lums), max(lums)
        for (x, y), l in zip(greys, lums):
            px[x, y] = (*ramp(stops, (l - lo) / (hi - lo or 1)), px[x, y][3])
        if moss:                     # moss on its top edge and down its corners
            for x, y in greys:
                if (y < y0 + 3 or x < 3 or x > im.width - 4) and rnd.random() < 0.35:
                    px[x, y] = rnd.choice([(74, 110, 52, 255), (98, 138, 60, 255), (60, 88, 44, 255)])
        im.save(os.path.join(HALL, out + ".png"))
        print("wrote", out)


if __name__ == "__main__":
    main()
