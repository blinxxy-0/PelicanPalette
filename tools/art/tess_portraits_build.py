"""Tess's portrait sheet, made the way Bea's, Fern's and Emmett's were
(art/bea/higgsfield/README.md, art/vale_residents_portraits/README.md):

  base      Higgsfield gpt_image_2_6 candidate tess_7 (the user's pick, 2026-11-09), read back
            onto its 64 px grid (tools/art/pixel_readback.py): tess_7_64.png
  faces     face-only edits of base_x16.png, read back the same way; only the face box is
            taken from each, so her hair, jumper and apron are pixel-identical in every slot
  sheet     2 across, slots 0 neutral, 1 happy, 2 sad, 3 unique (her dry sideways half-smile),
            4 love, 5 angry, as her dialogue will tag them ($h $s $u $l $a); the background
            cut away (flood fill from the edges)

Usage: tess_portraits_build.py <dir with tess_7_64.png and expr/<name>_64.png> <out sheet.png>"""
import os, sys
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
FACE = (15, 22, 48, 52)            # x0, y0, x1, y1 (exclusive): brows to chin, cheek to cheek
SLOTS = ["neutral", "happy", "sad", "unique", "love", "angry"]


def cut(im):
    """The flat background, from the edges in, made transparent."""
    im = im.convert("RGBA")
    px = im.load()
    bg = px[0, 0]
    close = lambda c: sum(abs(c[i] - bg[i]) for i in range(3)) < 40
    seen, todo = set(), [(x, y) for x in range(64) for y in (0, 63)] + [(x, y) for y in range(64) for x in (0, 63)]
    while todo:
        x, y = todo.pop()
        if (x, y) in seen or not (0 <= x < 64 and 0 <= y < 64) or not close(px[x, y]):
            continue
        seen.add((x, y))
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    for x, y in seen:
        px[x, y] = (0, 0, 0, 0)
    return im


base = Image.open(os.path.join(src, "tess_7_64.png")).convert("RGBA")
sheet = Image.new("RGBA", (128, 192))
for i, name in enumerate(SLOTS):
    face = base.copy()
    if name != "neutral":
        edit = Image.open(os.path.join(src, "expr", name + "_64.png")).convert("RGBA")
        face.paste(edit.crop(FACE), FACE[:2])
    sheet.paste(cut(face), ((i % 2) * 64, (i // 2) * 64))
sheet.save(out)
print("wrote", out)
