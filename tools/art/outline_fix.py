"""Gives a fitted sprite one clean, unbroken outline (Studio II, 2026-11-09).
Shrinking a generated picture to its footprint (object_fit.py) breaks its outline: edge cells
lose the vote to the colour inside, so the silhouette comes out ragged and the dark line has gaps.
This tidies the silhouette (drops stray specks, files off one-pixel spurs, fills one-pixel
notches and pinholes), then redraws every edge pixel, the ones touching the background on a
side, in the Vale's dark warm brown, so the outline runs all the way round like the game's own
sprites. Near-black pixels inside turn the same brown (lead lines, not black ink).

Usage: outline_fix.py <in.png> <out.png>"""
import sys
import numpy as np
from PIL import Image

OUTLINE = (54, 24, 20)
N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))


def shifted(m, dy, dx, fill=False):
    out = np.full_like(m, fill)
    h, w = m.shape
    out[max(dy, 0):h + min(dy, 0), max(dx, 0):w + min(dx, 0)] = m[max(-dy, 0):h + min(-dy, 0), max(-dx, 0):w + min(-dx, 0)]
    return out


def components(m):
    h, w = m.shape
    lab = np.zeros((h, w), int)
    n = 0
    for y in range(h):
        for x in range(w):
            if m[y, x] and not lab[y, x]:
                n += 1
                st = [(y, x)]
                lab[y, x] = n
                while st:
                    cy, cx = st.pop()
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h and 0 <= nx < w and m[ny, nx] and not lab[ny, nx]:
                                lab[ny, nx] = n
                                st.append((ny, nx))
    return lab, n


def main(src, dst):
    a = np.asarray(Image.open(src).convert("RGBA")).copy()
    m = a[..., 3] >= 128
    # stray specks: any separate bit under 4 pixels goes
    lab, n = components(m)
    for i in range(1, n + 1):
        if (lab == i).sum() < 4:
            m[lab == i] = False
    for _ in range(2):
        nb = sum(shifted(m, dy, dx).astype(int) for dy, dx in N4)
        m &= ~(nb <= 1)                      # spurs: a pixel held on by one side
        notch = ~m & (nb >= 3)               # notches and pinholes: a gap walled on three sides
        if notch.any():
            # fill from the neighbours (the most common opaque neighbour colour)
            for y, x in zip(*np.nonzero(notch)):
                cols = [tuple(a[y + dy, x + dx, :3]) for dy, dx in N4
                        if 0 <= y + dy < m.shape[0] and 0 <= x + dx < m.shape[1] and m[y + dy, x + dx]]
                a[y, x, :3] = max(set(cols), key=cols.count)
            m |= notch
    edge = m & ~np.logical_and.reduce([shifted(m, dy, dx) for dy, dx in N4])
    rgb = a[..., :3].astype(int)
    dark = m & (rgb.sum(2) < 90)
    a[..., :3][edge | dark] = OUTLINE
    a[..., 3] = np.where(m, 255, 0)
    a[~m, :3] = 0
    Image.fromarray(a).save(dst)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
