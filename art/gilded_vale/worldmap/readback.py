"""Reads a 1200x720 generation back onto a 300x180 grid (4 screen px an art pixel)."""
import sys, collections
import numpy as np
from PIL import Image
def readback(path, W=300, H=180, k=4):
    a = np.asarray(Image.open(path).convert("RGB")).astype(int)
    if a.shape[1] != W*k or a.shape[0] != H*k:
        a = np.asarray(Image.open(path).convert("RGB").resize((W*k, H*k), Image.NEAREST)).astype(int)
    dx = (np.abs(np.diff(a, axis=1)).sum(2) > 40).sum(0); dy = (np.abs(np.diff(a, axis=0)).sum(2) > 40).sum(1)
    phx = max(range(k), key=lambda p: dx[p + k-1::k].sum()); phy = max(range(k), key=lambda p: dy[p + k-1::k].sum())
    out = Image.new("RGB", (W, H))
    for j in range(H):
        for i in range(W):
            x0 = min(W*k-k, phx + i*k); y0 = min(H*k-k, phy + j*k)
            cell = a[y0+1:y0+3, x0+1:x0+3].reshape(-1, 3)
            c = collections.Counter(map(tuple, cell)).most_common(1)[0][0]
            out.putpixel((i, j), tuple(int(v) for v in c))
    return out, (phx, phy)
if __name__ == "__main__":
    for p in sys.argv[1:]:
        im, ph = readback(p)
        im.save(p.replace(".png", "_300.png"))
        print(p, Image.open(p).size, "phase", ph, "colours", len(set(im.getdata())))
