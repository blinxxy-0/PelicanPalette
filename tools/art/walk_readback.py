"""Reads a walk sheet Higgsfield drew at 12x (768x1536: 4x4 frames of 16x32) back to the
game's 64x128, then cleans it: one colour per 12px cell (the commonest in its middle, so
nothing is blurred), the background made transparent (filled from each frame's edges), lone
specks dropped, the colours reduced to a small palette, a dark outline where the silhouette
meets the air. Its grid is forced rather than found: these edits don't keep a clean one.

Bea's walk sheet: bea_edit_b.png (Higgsfield's edit of Leah's sheet), then
tools/art/bea_walk_fix.py for her eyes and the back of her apron.

Usage: walk_readback.py <generated 768x1536.png> <out 64x128.png> [colours=24]"""
import sys, collections
import numpy as np
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
k = int(sys.argv[3]) if len(sys.argv) > 3 else 24
P = 12
g = np.asarray(Image.open(src).convert("RGB")).astype(int)
H, W = g.shape[0] // P, g.shape[1] // P
a = np.zeros((H, W, 3), int)
for j in range(H):
    for i in range(W):
        cell = g[j*P + P//4:(j+1)*P - P//4, i*P + P//4:(i+1)*P - P//4].reshape(-1, 3)
        a[j, i] = collections.Counter(map(tuple, (cell // 6) * 6)).most_common(1)[0][0]
bg = np.median(np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]]), axis=0)
near = np.abs(a - bg).sum(2) < 24          # tight: her skin is close to the tan
mask = np.ones((H, W), bool)
for fy in range(0, H, 32):
    for fx in range(0, W, 16):
        seen = set()
        todo = [(x, y) for x in range(fx, fx + 16) for y in (fy, fy + 31)] + \
               [(x, y) for y in range(fy, fy + 32) for x in (fx, fx + 15)]
        while todo:
            x, y = todo.pop()
            if (x, y) in seen or not (fx <= x < fx + 16 and fy <= y < fy + 32) or not near[y, x]:
                continue
            seen.add((x, y)); mask[y, x] = False
            todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
for y in range(H):
    for x in range(W):
        if mask[y, x] and sum(mask[yy, xx] for yy, xx in ((y-1, x), (y+1, x), (y, x-1), (y, x+1))
                              if 0 <= yy < H and 0 <= xx < W) <= 1:
            mask[y, x] = False
q = Image.fromarray(a.astype(np.uint8)).quantize(colors=k, method=Image.Quantize.MEDIANCUT)
pal = np.array(q.getpalette()[:k * 3]).reshape(k, 3)
qa = pal[np.asarray(q)]
rgba = np.zeros((H, W, 4), np.uint8)
rgba[mask, :3] = qa[mask]; rgba[mask, 3] = 255
for y in range(H):
    for x in range(W):
        if mask[y, x] and qa[y, x].sum() > 200 and any(
                not (0 <= yy < H and 0 <= xx < W) or not mask[yy, xx]
                for yy, xx in ((y-1, x), (y+1, x), (y, x-1), (y, x+1))):
            rgba[y, x, :3] = (68, 30, 28)
Image.fromarray(rgba).save(out)
