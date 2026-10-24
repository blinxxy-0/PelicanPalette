"""Pulls a generated sprite or portrait toward vanilla Stardew's measured
look (exports of the base-game portraits, 2026-10-24):
  - about 30 colours per 64x64 portrait (we cap at --colors, default 32)
  - no pure-black outlines: Stardew outlines are dark, hue-shifted colours
    (Leah's are deep plum, (56, 0, 42)). Near-black pixels become a darker,
    slightly cooler shade of whatever colour they border.
  - hard alpha only.

Usage: vanillafy.py in.png out.png [--colors 32]
"""
import argparse
from PIL import Image


def quantize(im, colors):
    """Reduce to `colors` colours. Median-cut groups similar colours, but its
    averaged group colours come out duller than the art; each group is
    drawn instead in its most common real colour, so saturation survives."""
    alpha = im.getchannel("A").point(lambda a: 255 if a >= 128 else 0)
    src = im.convert("RGB")
    labels = src.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    lab, spx, apx = labels.load(), src.load(), alpha.load()
    counts = {}
    for y in range(im.height):
        for x in range(im.width):
            if apx[x, y]:
                key = (lab[x, y], spx[x, y])
                counts[key] = counts.get(key, 0) + 1
    best = {}
    for (index, colour), n in counts.items():
        if n > best.get(index, (None, 0))[1]:
            best[index] = (colour, n)
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    opx = out.load()
    for y in range(im.height):
        for x in range(im.width):
            if apx[x, y]:
                opx[x, y] = best[lab[x, y]][0] + (255,)
    return out


def darken_toward(c, amount=0.32):
    # Darken a neighbour colour and nudge it toward plum, as vanilla outlines do.
    r, g, b = c
    r, g, b = r * amount, g * amount * 0.8, b * amount + 18
    return (int(min(255, r + 20)), int(g), int(min(255, b)))


def recolour_black(im, threshold=34):
    px = im.load()
    w, h = im.size
    changes = {}
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if p[3] == 0 or max(p[:3]) > threshold:
                continue
            # The most common non-dark neighbour colour decides this outline's hue.
            near = []
            for dy in (-2, -1, 0, 1, 2):
                for dx in (-2, -1, 0, 1, 2):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        q = px[nx, ny]
                        if q[3] and max(q[:3]) > threshold + 20:
                            near.append(q[:3])
            if near:
                base = max(set(near), key=near.count)
                changes[(x, y)] = darken_toward(base) + (255,)
            else:
                changes[(x, y)] = (40, 14, 34, 255)
    for k, v in changes.items():
        px[k] = v
    return im


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("src"); ap.add_argument("out"); ap.add_argument("--colors", type=int, default=32)
    a = ap.parse_args()
    im = Image.open(a.src).convert("RGBA")
    im = recolour_black(im)
    im = quantize(im, a.colors)
    im.save(a.out)
    n = len(set(p for p in im.getdata() if p[3] == 255))
    print(f"{a.out}: {n} colours")
