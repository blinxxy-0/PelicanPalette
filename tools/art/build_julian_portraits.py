"""Builds Julian Vane's portrait sheet (Portraits/<id>: 128 px wide, 64x64
frames, two per row, in vanilla's order) from the PixelLab picks the user
made (2026-10-31): the face B5 (julian_stern_5), and expressions generated
with it as their identity reference (art/julian/expression_batch.json,
annoyed_batch.json).

As Sable's (build_sable_portraits.py): every expression takes over only the
pixels that changed around the face, so hair, glasses, coat and scarf stay
pixel-identical; then the sheet shares one palette of about 30 colours with
no pure-black outlines.

Usage: build_julian_portraits.py <PixelLab generations dir> <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import recolour_black
from build_sable_portraits import FACE, THRESHOLD, shared_palette, apply_palette

NEUTRAL = ("stardew-julian-portrait", "julian_stern", 5)
EXPRESSIONS = "stardew-julian-expressions"
# Portrait index (as dialogue uses it: $0..$7) -> (batch, job, candidate).
FRAMES = [
    (0, NEUTRAL),                                           # neutral: stern, appraising
    (1, (EXPRESSIONS, "happy", 1)),                         # $h a rare real smile
    (2, (EXPRESSIONS, "sad", 10)),                          # $s regret
    (3, (EXPRESSIONS, "smirk", 8)),                         # $u his own: the dry half-smirk
    (4, (EXPRESSIONS, "touched", 11)),                      # $l moved, caught off guard
    (5, ("stardew-julian-expressions-annoyed", "annoyed", 13)),   # $a annoyed: cold irritation (the first batch's foreheads were wrong)
    (6, (EXPRESSIONS, "surprised", 7)),                     # surprised
    (7, (EXPRESSIONS, "thoughtful", 6)),                    # studying a painting
]


def load(gen, pick):
    batch, job, n = pick
    return Image.open(os.path.join(gen, batch, f"{job}_{n}.png")).convert("RGBA")


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
    gen, out = sys.argv[1], sys.argv[2]
    neutral = load(gen, NEUTRAL)
    sheet = Image.new("RGBA", (128, 64 * ((len(FRAMES) + 1) // 2)), (0, 0, 0, 0))
    for index, pick in FRAMES:
        face = neutral if pick == NEUTRAL else splice(load(gen, pick), neutral)
        sheet.alpha_composite(face, ((index % 2) * 64, (index // 2) * 64))
    sheet = recolour_black(sheet)
    sheet.putalpha(sheet.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    sheet = apply_palette(sheet, shared_palette(sheet))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    per_frame = [len(set(p for p in sheet.crop(((i % 2) * 64, (i // 2) * 64, (i % 2) * 64 + 64, (i // 2) * 64 + 64)).getdata() if p[3])) for i, _ in FRAMES]
    print(f"wrote {out}: {len(FRAMES)} frames, colours per frame {per_frame}")


if __name__ == "__main__":
    main()
