"""Builds Sable's walking sprite sheet (Characters/Sable: 64x128, 16x32
frames; rows walk down, right, up, left, 4 frames each, as vanilla) from
PixelLab candidate walk_leah_1 (2026-10-24). It matched vanilla Leah's
sheet frame for frame: the body spans rows 6-32 or 7-32 with the same
1-pixel walking bob.

Fixes on top of the render:
  - The back view showed the apron as a cream panel over the whole back.
    Aprons tie behind, so the cream there becomes the turtleneck's rust,
    with the apron's straps and waist tie added back in cream.
  - Vanilla clean-up: no pure-black outlines, and one shared palette
    (vanilla NPC frames measure 10-30 colours).

Then the Flower Dance frames (40-47, sheet rows 10-11; rows 4-9 stay empty
for the special animations to come). A they/them partner dances in the
line facing away from the camera, which uses frames 44-47 like Elliott's;
40-43 are the front-facing set in case another line is used. They're the
walk frames in a festival outfit: a cream linen jacket over the rust
collar, no apron, and a pink flower tucked in the hair.

Usage: build_sable_sprites.py <walk_leah_1.png> <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black

UP_ROW = 2
COLOURS = 24              # sheet palette; vanilla Leah's frames measure 15-21 each


def is_cream(p):
    r, g, b, a = p
    return a and r > 170 and g > 150 and b > 110 and r - b < 110


def rust_for(p, ramp):
    # Same brightness step of the turtleneck's colour ramp.
    lum = 0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]
    return min(ramp, key=lambda c: abs((0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]) * 1.9 - lum))


def turtleneck_ramp(sheet):
    # Rust colours from the front-facing frames' shoulders.
    colours = {}
    for x in range(16):
        for y in range(14, 20):
            p = sheet.getpixel((x, y))
            if p[3] and p[0] > 110 and p[0] > p[1] + 40 and p[1] > p[2] - 10:
                colours[p[:3]] = colours.get(p[:3], 0) + 1
    return sorted(colours, key=lambda c: -colours[c])[:4] or [(150, 60, 40)]


def fix_back_view(sheet):
    ramp = turtleneck_ramp(sheet)
    px = sheet.load()
    for frame in range(4):
        x0, y0 = frame * 16, UP_ROW * 32
        cream = [(x, y) for y in range(y0, y0 + 32) for x in range(x0, x0 + 16) if is_cream(px[x, y])]
        if not cream:
            continue
        light = max((px[x, y] for x, y in cream), key=lambda p: sum(p[:3]))
        top = min(y for _, y in cream)
        bottom = max(y for _, y in cream)
        xs = sorted(x for x, _ in cream)
        left, right = xs[0], xs[-1]
        for x, y in cream:
            px[x, y] = rust_for(px[x, y], ramp) + (255,)
        # Two straps down the back and a small bow at the waist, at a fixed
        # height on the body (from the top of the head) so they don't jump
        # between frames; the head-bob carries them with the body.
        head = sheet.crop((x0, y0, x0 + 16, y0 + 32)).getbbox()[1] + y0
        waist = head + 16
        for y in range(head + 11, waist):
            for x in (left + 2, right - 2):
                if px[x, y][3]:
                    px[x, y] = light
        middle = (left + right) // 2
        for x, y in ((middle, waist), (middle - 1, waist - 1), (middle + 1, waist - 1), (middle - 1, waist + 1), (middle + 1, waist + 1)):
            if px[x, y][3]:
                px[x, y] = light
    return sheet


# The dance outfit, by colour: the turtleneck's rust and the apron's cream
# become a cream jacket, light to dark (the outline colours stay).
JACKET = {
    (242, 222, 170): (246, 238, 214),     # apron cream -> jacket light
    (249, 236, 219): (250, 244, 228),
    (173, 68, 38): (226, 210, 174),       # rust -> jacket mid
    (160, 87, 56): (226, 210, 174),       # (the back's rust, quantised onto the skin colour)
    (121, 37, 24): (190, 166, 128),       # rust shade -> jacket shade
    (96, 21, 17): (146, 116, 88),         # rust dark -> jacket dark
}
FLOWER = [((4, 2), (236, 118, 160)), ((5, 2), (252, 196, 214)), ((4, 3), (196, 78, 118)), ((5, 1), (236, 118, 160))]


def dance_frame(frame, back):
    """A walk frame in the festival outfit. From behind, the jacket is one
    flat tone (no apron straps showing through) with its darker folds."""
    out = frame.copy()
    px = out.load()
    top = frame.getbbox()[1]
    skin = (160, 87, 56)
    for y in range(top + 12, top + 21):              # the torso below the collar
        sleeves = y < top + 19                       # the hands hang below the sleeves
        for x in range(2 if sleeves else 4, 14 if sleeves else 12):
            p = px[x, y]
            if not p[3] or p[:3] not in JACKET or (p[:3] == skin and not back):
                continue                             # (from the front, that colour is skin)
            colour = JACKET[p[:3]]
            if back and colour[0] > 240:             # straps and bow: same cloth as the rest
                colour = JACKET[(173, 68, 38)]
            px[x, y] = colour + (255,)
    for (dx, dy), colour in FLOWER:
        if px[dx, top + dy][3]:
            px[dx, top + dy] = colour + (255,)
    return out


def add_dance_rows(sheet):
    tall = Image.new("RGBA", (64, 12 * 32), (0, 0, 0, 0))
    tall.alpha_composite(sheet, (0, 0))
    for i in range(4):
        front = sheet.crop((i * 16, 0, i * 16 + 16, 32))
        back = sheet.crop((i * 16, UP_ROW * 32, i * 16 + 16, UP_ROW * 32 + 32))
        tall.alpha_composite(dance_frame(front, back=False), (i * 16, 10 * 32))     # 40-43
        tall.alpha_composite(dance_frame(back, back=True), (i * 16, 11 * 32))      # 44-47
    return tall


def main():
    src, out = sys.argv[1], sys.argv[2]
    sheet = Image.open(src).convert("RGBA")
    assert sheet.size == (64, 128)
    sheet.putalpha(sheet.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    sheet = fix_back_view(sheet)
    sheet = quantize(recolour_black(sheet), COLOURS)
    sheet = add_dance_rows(sheet)
    sheet.save(out)
    counts = []
    for r in (0, 1, 2, 3, 10, 11):
        for c in range(4):
            f = sheet.crop((c * 16, r * 32, c * 16 + 16, r * 32 + 32))
            counts.append(len(set(p for p in f.get_flattened_data() if p[3])))
    print(f"wrote {out}: colours per frame {min(counts)}-{max(counts)}")


if __name__ == "__main__":
    main()
