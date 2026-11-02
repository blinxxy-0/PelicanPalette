"""The living painting (1.3, the museum collection's reward for six
donated): an old painting of a mill on a river at dusk (PixelLab, picked
2026-11-01, art/collection) whose picture moves a little. Four frames,
side by side, to [CP] Pelican Palette/assets/collection/living.png (128x32):
the river's water slides a pixel one way or the other along each row,
the banks staying put (a slow shimmer), and
the mill's lit window brightens and dims (a lamp flickering). The game
cycles them (ArtStudio/Painting/LivingPainting.cs). Outlines recoloured
(vanillafy.selout), never black.

Usage: living_painting.py art/collection/picks <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PICK = "living_painting_10"
INSIDE = (7, 22)                      # the picture's columns, inside the frame
WATER = range(19, 25)                 # the river's rows
WINDOW = [(17, 15)]                   # the mill's lit window
FLICKER = [1.0, 0.85, 1.1, 0.92]      # the window's brightness, frame by frame
FRAMES = 4


def is_water(c):
    r, g, b, a = c
    return a and b > r and b > 110


def shift_row(base, px, y, by):
    """Slides the water's own pixels along the row, each run of water on its
    own, so the banks stay where they are."""
    x0, x1 = INSIDE
    x = x0
    while x <= x1:
        if not is_water(base[x, y]):
            x += 1
            continue
        run = []
        while x <= x1 and is_water(base[x, y]):
            run.append(x)
            x += 1
        colours = [base[i, y] for i in run]
        for k, i in enumerate(run):
            px[i, y] = colours[(k - by) % len(run)]


def main():
    picks, out = sys.argv[1], sys.argv[2]
    base = selout(Image.open(os.path.join(picks, PICK + ".png")).convert("RGBA"))
    sheet = Image.new("RGBA", (32 * FRAMES, 32), (0, 0, 0, 0))
    for f in range(FRAMES):
        im = base.copy()
        px = im.load()
        for y in WATER:
            shift_row(base.load(), px, y, ((y + f) % 3) - 1)
        for x, y in WINDOW:
            for dx, dy in ((0, 0), (1, 0), (0, 1), (-1, 0), (0, -1)):
                r, g, b, a = px[x + dx, y + dy]
                if r > 150 and b < 120:          # only the warm light itself
                    k = FLICKER[f]
                    px[x + dx, y + dy] = (min(255, int(r * k)), min(255, int(g * k)), min(255, int(b * k)), a)
        sheet.alpha_composite(im, (32 * f, 0))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
