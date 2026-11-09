"""Fits a Higgsfield furniture picture to its footprint in game pixels (Studio II, 2026-11-09).
The model draws its pixel art finer than asked (about twice the game's density), so the grid
read-back alone leaves the piece too big. This lifts the piece off its background (flat magenta,
or whatever plain colour fills the corners: flooded in from the edges), crops it, shrinks it to
fit W x H (keeping its shape, standing on the bottom edge, centred across), and snaps every pixel
to a palette taken from the piece itself, so nothing blurs.

A picture with its own transparency (Higgsfield's background "transparent") uses that instead of
the flood (anything under 200 alpha is background: the model's soft edge). "stretch" fills W x H
exactly instead of keeping the shape (for a window, which the model never draws quite 2:1).

Usage: object_fit.py <generated.png> <out.png> <W> <H> [stretch]"""
import sys
from collections import deque
import numpy as np
from PIL import Image

src, out, W, H = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
stretch = "stretch" in sys.argv[5:]
src_im = Image.open(src)
has_alpha = src_im.mode in ("RGBA", "LA") and src_im.getchannel("A").getextrema()[0] < 200
im = src_im.convert("RGB")
small = im.resize((im.width // 4, im.height // 4), Image.BOX)     # (flooding at a quarter size is plenty)
a = np.asarray(small).astype(int)
h, w = a.shape[:2]
corner = np.median(np.array([a[0, 0], a[0, -1], a[-1, 0], a[-1, -1]]), axis=0)
near = np.abs(a - corner).sum(2) < 60
mag = (a[..., 0] > 170) & (a[..., 2] > 150) & (a[..., 1] < 110)
bg = np.zeros((h, w), bool)
q = deque((y, x) for y in range(h) for x in range(w) if (y in (0, h - 1) or x in (0, w - 1)) and (near[y, x] or mag[y, x]))
for y, x in q:
    bg[y, x] = True
while q:
    y, x = q.popleft()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        ny, nx = y + dy, x + dx
        if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and (near[ny, nx] or mag[ny, nx]):
            bg[ny, nx] = True
            q.append((ny, nx))
bg |= mag                                                          # magenta anywhere is background
mask = Image.fromarray(np.where(bg, 0, 255).astype(np.uint8)).resize(im.size, Image.NEAREST)
if has_alpha:
    mask = src_im.getchannel("A").point(lambda v: 255 if v >= 200 else 0)
rgba = im.convert("RGBA")
rgba.putalpha(mask)
rgba = rgba.crop(rgba.getbbox())
scale = min(W / rgba.width, H / rgba.height)
tw, th = max(1, round(rgba.width * scale)), max(1, round(rgba.height * scale))
if stretch:
    tw, th = W, H
# Shrink by majority: each new pixel takes the commonest colour of the cells it covers (after a
# first pass onto the picture's own pixel grid, so those cells are clean colours), a dark
# outline colour winning a tie, so saturated glass and thin lines survive. Averaging
# muddied them (the offcuts' blue went teal).
import os, subprocess, tempfile
from collections import Counter
here = os.path.dirname(os.path.abspath(__file__))
tmp = tempfile.mkdtemp()
rgba.convert("RGB").save(os.path.join(tmp, "c.png"))
subprocess.run([sys.executable, os.path.join(here, "pixel_readback.py"), os.path.join(tmp, "c.png"), os.path.join(tmp, "g.png")],
               check=True, stdout=subprocess.DEVNULL)
grid = Image.open(os.path.join(tmp, "g.png")).convert("RGB")
gmask = rgba.getchannel("A").resize(grid.size, Image.BOX)
g = np.asarray(grid).astype(int)
ga = np.asarray(gmask)
gh, gw = g.shape[:2]
o = np.zeros((th, tw, 4), np.uint8)
for ty in range(th):
    for tx in range(tw):
        y0, y1 = int(ty * gh / th), max(int(ty * gh / th) + 1, int((ty + 1) * gh / th))
        x0, x1 = int(tx * gw / tw), max(int(tx * gw / tw) + 1, int((tx + 1) * gw / tw))
        cells = [(tuple(g[y, x]), ga[y, x]) for y in range(y0, y1) for x in range(x0, x1)]
        solid = [c for c, al in cells if al > 140]
        if len(solid) * 2 < len(cells):
            continue
        counts = Counter(solid)
        best = counts.most_common(1)[0]
        darks = [(n, c) for c, n in counts.items() if sum(c) < 180 and n >= best[1]]   # (a tie only: more thickened outlines)
        c = max(darks)[1] if darks else best[0]
        o[ty, tx, :3] = c
        o[ty, tx, 3] = 255
canvas = Image.new("RGBA", (W, H))
canvas.alpha_composite(Image.fromarray(o, "RGBA"), ((W - tw) // 2, H - th))
canvas.save(out)
print(f"{out}: {tw}x{th} in {W}x{H}")
