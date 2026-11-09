"""Tess's own pieces for her counter in Studio III (1.4): carved figures and stools. Higgsfield
gpt_image_2_6, one piece each in the style of the game's own small furniture (its Squirrel
Figurine, Futan Rabbit, Model Ship and a stool as the reference), the user's OK for about 15
credits; each cut from its background and read down to the game's size, a pixel taking its
cell's commonest colour. The small Gilded Tree is Bram's finished carving (studio3/carving/
state_5), read down the same way.

  sheet  art/tess/counter/carvings.png -> [CP] Gilded Vale (dev)/assets/tess/carvings.png, 8 across:
         0 rabbit, 1 duck, 2 fox, 3 owl (16x16; row 0)
         4 ash stool, 5 walnut stool (16x32, the stool in the lower half, as the game's stools)
         6 heron, 7 small Gilded Tree (16x32)

Usage: python tools/art/tess_counter_art.py
"""
import os
from collections import Counter
from PIL import Image

D = "art/tess/counter/"


def cut(im, tol=48):
    px = im.load()
    w, h = im.size
    bg = px[4, 4]
    close = lambda c: sum(abs(c[i] - bg[i]) for i in range(3)) < tol
    seen, todo = set(), [(x, y) for x in range(0, w, 2) for y in (0, h - 1)] + [(x, y) for y in range(0, h, 2) for x in (0, w - 1)]
    while todo:
        x, y = todo.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not close(px[x, y]):
            continue
        seen.add((x, y))
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    for x, y in seen:
        px[x, y] = (0, 0, 0, 0)
    return im


def shrink(im, W, H):
    im = im.crop(im.getbbox())
    s = min(W / im.width, H / im.height)
    nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
    ip = im.load()
    out = Image.new("RGBA", (W, H))
    op = out.load()
    ox, oy = (W - nw) // 2, H - nh
    for ty in range(nh):
        for tx in range(nw):
            x0, y0 = int(tx / s), int(ty / s)
            x1, y1 = max(x0 + 1, int((tx + 1) / s)), max(y0 + 1, int((ty + 1) / s))
            cells = [ip[x, y] for y in range(y0, min(y1, im.height)) for x in range(x0, min(x1, im.width))]
            opaque = [c for c in cells if c[3] > 128]
            if len(opaque) * 2 < len(cells):
                continue
            q = Counter((c[0] // 14, c[1] // 14, c[2] // 14) for c in opaque).most_common(1)[0][0]
            pick = [c for c in opaque if (c[0] // 14, c[1] // 14, c[2] // 14) == q]
            op[ox + tx, oy + ty] = tuple(sum(c[i] for c in pick) // len(pick) for i in range(3)) + (255,)
    return out


def piece(name, W=16, H=16):
    return shrink(cut(Image.open(D + name + ".png").convert("RGBA")), W, H)


def owl():
    im = piece("owl")
    px = im.load()
    # its eyes read as mud at this size: a dark pupil in a pale ring each
    for cx in (5, 10):
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            px[cx + dx, 4 + dy] = (226, 160, 98, 255)
        px[cx, 4] = (52, 24, 10, 255)
    px[7, 5] = px[8, 5] = (196, 120, 60, 255)          # the beak
    return im


def main():
    sheet = Image.new("RGBA", (128, 32))
    for i, name in enumerate(("rabbit", "duck", "fox")):
        sheet.alpha_composite(piece(name), (i * 16, 0))
    sheet.alpha_composite(owl(), (48, 0))
    for i, name in enumerate(("ash_stool", "walnut_stool")):
        sheet.alpha_composite(piece(name), (64 + i * 16, 16))
    sheet.alpha_composite(piece("heron", 16, 32), (96, 0))
    tree = shrink(cut(Image.open("art/gilded_vale/interiors/studio3/carving/state_5.png").convert("RGBA")), 16, 32)
    sheet.alpha_composite(tree, (112, 0))
    sheet.save(D + "carvings.png")
    print("wrote", D + "carvings.png")


if __name__ == "__main__":
    main()
