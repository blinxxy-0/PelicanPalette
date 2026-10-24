"""Builds Sable's portrait sheet (Portraits/Sable: 128 px wide, 64x64
frames, two per row, in vanilla's order) from the PixelLab batch the user
picked a face from (2026-10-24, "b_10" refined with Elliott-style framing).

Vanilla expressions change only the face: hair, glasses and clothes stay
pixel-identical between frames. So every frame starts from the neutral and
takes over only the pixels that changed around the face (compared by
colour, so a pink blush on brown skin counts). Then the whole sheet shares
one palette of about 30 colours (vanilla portraits measure 26-36 per
frame) with no pure-black outlines. Colours an expression adds that the
shared palette lacks (the blush) get a few reserved slots, so they aren't
merged into the skin.

Usage: build_sable_portraits.py <batch dir> <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black

# Portrait index (as dialogue uses it: $0..$7) -> candidate number in the batch.
FRAMES = [
    (0, 1),    # neutral: calm, faint smile
    (1, 5),    # $h happy: eyes-closed grin
    (2, 7),    # $s sad
    (3, 9),    # $u Sable's own: a dry, wry smirk
    (4, 4),    # $l love: blushing
    (5, 10),   # $a annoyed
    (6, 6),    # surprised
    (7, 3),    # playful wink
]
FACE = (12, 16, 52, 54)     # the area expressions may change (x0, y0, x1, y1)
THRESHOLD = 30              # largest per-channel difference that still counts as unchanged
COLOURS = 30
RESERVED = 4                # extra slots for colours only an expression uses
FAR = 36                    # a colour this far from every palette colour gets a reserved slot


def frame(batch, n, base):
    im = Image.open(os.path.join(batch, f"neutral_elliott_{n}.png")).convert("RGBA")
    if base is None:
        return im
    out = base.copy()
    src, dst = im.load(), out.load()
    x0, y0, x1, y1 = FACE
    for y in range(y0, y1):
        for x in range(x0, x1):
            a, b = src[x, y], dst[x, y]
            if max(abs(a[i] - b[i]) for i in range(4)) > THRESHOLD:
                dst[x, y] = a
    return out


def distance(a, b):
    return sum((a[i] - b[i]) ** 2 for i in range(3)) ** 0.5


def shared_palette(sheet):
    # The common look: quantize, then read back the palette it chose.
    q = quantize(sheet, COLOURS)
    palette = sorted(set(p[:3] for p in q.getdata() if p[3]))
    # Colours far from all of those (e.g. a blush) get reserved slots.
    far = {}
    for p in sheet.getdata():
        if p[3] and min(distance(p, c) for c in palette) > FAR:
            key = tuple(v // 12 * 12 for v in p[:3])
            far[key] = far.get(key, 0) + 1
    for colour, _ in sorted(far.items(), key=lambda kv: -kv[1])[:RESERVED]:
        palette.append(colour)
    return palette


def apply_palette(sheet, palette):
    out = sheet.copy()
    px = out.load()
    cache = {}
    for y in range(out.height):
        for x in range(out.width):
            p = px[x, y]
            if not p[3]:
                continue
            if p[:3] not in cache:
                cache[p[:3]] = min(palette, key=lambda c: distance(p, c))
            px[x, y] = cache[p[:3]] + (255,)
    return out


def main():
    batch, out = sys.argv[1], sys.argv[2]
    neutral = frame(batch, FRAMES[0][1], None)
    sheet = Image.new("RGBA", (128, 64 * ((len(FRAMES) + 1) // 2)), (0, 0, 0, 0))
    for index, n in FRAMES:
        sheet.alpha_composite(frame(batch, n, neutral), ((index % 2) * 64, (index // 2) * 64))
    sheet = recolour_black(sheet)
    sheet.putalpha(sheet.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    sheet = apply_palette(sheet, shared_palette(sheet))
    sheet.save(out)
    per_frame = []
    for index, _ in FRAMES:
        f = sheet.crop(((index % 2) * 64, (index // 2) * 64, (index % 2) * 64 + 64, (index // 2) * 64 + 64))
        per_frame.append(len(set(p for p in f.getdata() if p[3])))
    print(f"wrote {out}: {len(FRAMES)} frames, colours per frame {per_frame}")


if __name__ == "__main__":
    main()
