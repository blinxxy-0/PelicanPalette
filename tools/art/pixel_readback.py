"""Reads a generated 'pixel art' image (Higgsfield / GPT Image, ~1024 px) back onto its own
pixel grid: finds the grid's period and phase from where colours change, then takes one
colour per cell (the commonest in its middle), so nothing is averaged or blurred. Prints the
grid it found; a 1024 px image drawn on a true 64x64 grid reads back at period 16, phase 0.
(Averaging it down instead blurred Bea's first portraits: the user, 2026-11-08.)

Usage: pixel_readback.py <generated.png> <out.png>"""
import sys, collections
import numpy as np
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
a = np.asarray(Image.open(src).convert("RGB")).astype(int)
def find(sig):
    best = (0, 16.0, 0.0)
    for p10 in range(100, 260):
        p = p10 / 10
        for ph10 in range(0, int(p * 10), 2):
            ph = ph10 / 10
            idx = np.arange(ph, len(sig), p).astype(int) - 1
            idx = idx[(idx >= 0) & (idx < len(sig))]
            s = sig[idx].mean() / (sig.mean() + 1e-9)
            if s > best[0]:
                best = (s, p, ph)
    return best
dx = (np.abs(np.diff(a, axis=1)).sum(2) > 40).sum(0)
dy = (np.abs(np.diff(a, axis=0)).sum(2) > 40).sum(1)
sx, px_, phx = find(dx); sy, py_, phy = find(dy)
nx = int(round((a.shape[1] - phx) / px_)); ny = int(round((a.shape[0] - phy) / py_))
print(f"grid x period {px_} phase {phx} ({nx} cells, score {sx:.2f}); y period {py_} phase {phy} ({ny} cells, score {sy:.2f})")
img = Image.new("RGB", (nx, ny))
for j in range(ny):
    for i in range(nx):
        x0 = int(phx + i * px_ + px_ * 0.25); x1 = int(phx + (i + 1) * px_ - px_ * 0.25)
        y0 = int(phy + j * py_ + py_ * 0.25); y1 = int(phy + (j + 1) * py_ - py_ * 0.25)
        cell = a[y0:max(y0 + 1, y1), x0:max(x0 + 1, x1)].reshape(-1, 3)
        c = collections.Counter(map(tuple, (cell // 6) * 6)).most_common(1)[0][0]
        img.putpixel((i, j), tuple(int(v) for v in c))
img.save(out)
