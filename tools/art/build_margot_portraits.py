"""Builds Margot Pell's portrait sheet (Portraits/<id>: 128 px wide, 64x64
frames, two per row, in vanilla's order) from the PixelLab picks the user
made (2026-11-01): the face A7 (art/margot/picks/neutral.png, from
portrait_batch.json), and expressions generated with it as their identity
reference (expression_batch.json), copied to art/margot/picks.

As Julian's (build_julian_portraits.py): every expression takes over only
the pixels that changed around the face, then the sheet shares one palette
of about 30 colours with no pure-black outlines. Margot's face area starts
below her loupe's band: taken from y 16, as Sable's and Julian's are, it let
a second, ghostly loupe into some expressions. The loupe's brass and the
pencil's yellow are few pixels, so the shared palette lost them (the ring
went olive); their three colours are kept as they are.

Usage: build_margot_portraits.py <art/margot/picks dir> <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import recolour_black
from build_sable_portraits import THRESHOLD, shared_palette, apply_palette

FACE = (12, 21, 52, 54)
BRASS = [(255, 201, 73), (226, 159, 45), (167, 121, 47)]
# Portrait index (as dialogue uses it: $0..$7) -> pick.
FRAMES = [
    (0, "neutral"),
    (1, "happy"),        # $h a big closed-eye grin
    (2, "sad"),          # $s soft, apologetic
    (3, "explaining"),   # $u her own: mid-sentence, eager
    (4, "blush"),        # $l flustered
    (5, "annoyed"),      # $a cross, not mean
    (6, "surprised"),
    (7, "thoughtful"),   # studying a painting
]


def splice(im, base):
    out = base.copy()
    src, dst = im.load(), out.load()
    x0, y0, x1, y1 = FACE
    for y in range(y0, y1):
        for x in range(x0, x1):
            a, b = src[x, y], dst[x, y]
            if max(abs(a[i] - b[i]) for i in range(4)) > THRESHOLD:
                dst[x, y] = a
    return out


def main():
    picks, out = sys.argv[1], sys.argv[2]
    load = lambda name: Image.open(os.path.join(picks, name + ".png")).convert("RGBA")
    neutral = load("neutral")
    sheet = Image.new("RGBA", (128, 64 * ((len(FRAMES) + 1) // 2)), (0, 0, 0, 0))
    for index, name in FRAMES:
        face = neutral if name == "neutral" else splice(load(name), neutral)
        sheet.alpha_composite(face, ((index % 2) * 64, (index // 2) * 64))
    sheet = recolour_black(sheet)
    sheet.putalpha(sheet.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    sheet = apply_palette(sheet, shared_palette(sheet) + BRASS)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    per_frame = [len(set(p for p in sheet.crop(((i % 2) * 64, (i // 2) * 64, (i % 2) * 64 + 64, (i // 2) * 64 + 64)).getdata() if p[3])) for i, _ in FRAMES]
    print(f"wrote {out}: {len(FRAMES)} frames, colours per frame {per_frame}")


if __name__ == "__main__":
    main()
