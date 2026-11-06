"""Fixes Margot's side walks (1.3, from playtesting): in some generated
frames her bun swung round to the front of her head, so it seemed to jump
as she walked. Walking right, a frame whose hair sits toward the front
(right of centre) is replaced by the mirrored left-walk frame of the same
pose; the left walk is then the right walk mirrored, so the two always
match. Safe to run again.

Usage: margot_side_walk.py <assets/margot dir>
"""
import glob, os, sys
from PIL import Image, ImageOps

W, H = 16, 32
RIGHT, LEFT = 1, 3


def hair_x(frame):
    px = frame.load()
    pts = [x for y in range(7) for x in range(W) if px[x, y][3] > 128]
    return sum(pts) / len(pts)


def fix(path):
    sheet = Image.open(path).convert("RGBA")
    frame = lambda r, c: sheet.crop((c * W, r * H, c * W + W, r * H + H))
    right = []
    for c in range(4):
        f = frame(RIGHT, c)
        if hair_x(f) > W / 2 - 0.5:                   # bun in front: use the left walk's, mirrored
            f = ImageOps.mirror(frame(LEFT, c))
        right.append(f)
    for c in range(4):
        for row, f in ((RIGHT, right[c]), (LEFT, ImageOps.mirror(right[c]))):
            sheet.paste((0, 0, 0, 0), (c * W, row * H, c * W + W, row * H + H))
            sheet.alpha_composite(f, (c * W, row * H))
    sheet.save(path)
    print(os.path.basename(path), [round(hair_x(f), 1) for f in right])


if __name__ == "__main__":
    for p in sorted(glob.glob(os.path.join(glob.escape(sys.argv[1]), "sprites*.png"))):
        fix(p)
