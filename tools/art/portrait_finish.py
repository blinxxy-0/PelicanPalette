"""Finishes a 64x64 portrait read back from a Higgsfield picture (tools/art/pixel_readback.py):
clears the plain tan background (flooded in from the edges, so tan inside the bust stays),
then moves the bust so its top has 6 px of air above it, as the other Vale residents' and
Leah's portraits do (the rows that fall off the bottom are coat).

Usage: portrait_finish.py <readback 64x64.png> <out.png> [air=6]"""
import sys
from collections import deque
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
air = int(sys.argv[3]) if len(sys.argv) > 3 else 6
a = np.asarray(Image.open(src).convert("RGB")).astype(int)
H, W = a.shape[:2]
bg = a[0, 0]
near = np.abs(a - bg).sum(2) < 36
clear = np.zeros((H, W), bool)
q = deque((y, x) for y in range(H) for x in range(W)
          if (y in (0, H - 1) or x in (0, W - 1)) and near[y, x])
for y, x in q:
    clear[y, x] = True
while q:
    y, x = q.popleft()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        ny, nx = y + dy, x + dx
        if 0 <= ny < H and 0 <= nx < W and near[ny, nx] and not clear[ny, nx]:
            clear[ny, nx] = True
            q.append((ny, nx))
o = np.zeros((H, W, 4), np.uint8)
o[..., :3] = a
o[..., 3] = np.where(clear, 0, 255)
top = np.nonzero(o[..., 3].any(1))[0].min()
shift = air - top
if shift > 0:
    o = np.concatenate([np.zeros((shift, W, 4), np.uint8), o[:H - shift]])
elif shift < 0:
    o = np.concatenate([o[-shift:], np.repeat(o[-1:], -shift, 0)])
Image.fromarray(o, "RGBA").save(out)
print("wrote", out, "moved", shift)
