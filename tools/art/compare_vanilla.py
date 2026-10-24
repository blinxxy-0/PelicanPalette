"""Sable's portraits and walk frames beside vanilla villagers, for approval.

Top: each of Sable's 8 expressions, then the same-size neutral portraits of
four vanilla villagers, at 3x. Bottom: Sable's walk cycle (all 4 directions)
beside Leah's and Elliott's at 4x, on a grass-coloured background so the
outlines read as they do in the world. Colour counts per frame are printed
under each block.

Usage: compare_vanilla.py <exports dir> <portraits.png> <sprites.png> <out.png>
"""
import os, sys
from PIL import Image, ImageDraw

BG = (238, 214, 168)       # dialogue-box paper
GRASS = (104, 160, 56)


def colours(im):
    return len(set(p[:3] for p in im.get_flattened_data() if p[3]))


def main():
    exports, portraits, sprites, out = sys.argv[1:5]
    sable_p = Image.open(portraits).convert("RGBA")
    sable_s = Image.open(sprites).convert("RGBA")
    vanilla_p = [(n, Image.open(os.path.join(exports, f"Portraits_{n}.png")).convert("RGBA")) for n in ("Leah", "Elliott", "Emily", "Haley")]
    # Just the 4 walking rows (vanilla sheets go on with special animations).
    vanilla_s = [(n, Image.open(os.path.join(exports, f"Characters_{n}.png")).convert("RGBA").crop((0, 0, 64, 128))) for n in ("Leah", "Elliott", "Emily")]

    P, S = 3, 4
    width = 8 * (64 * P + 12) + 24
    height = 64 * P * 2 + 140 + 32 * S * 4 + 120
    sheet = Image.new("RGBA", (width, height), BG + (255,))
    draw = ImageDraw.Draw(sheet)

    def paste(im, x, y, scale, bg=None):
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        if bg:
            sheet.paste(Image.new("RGBA", im.size, bg + (255,)), (x, y))
        sheet.alpha_composite(im, (x, y))

    names = ["neutral", "happy", "sad", "wry", "blush", "annoyed", "surprised", "wink"]
    y = 12
    for i, name in enumerate(names):
        frame = sable_p.crop(((i % 2) * 64, (i // 2) * 64, (i % 2) * 64 + 64, (i // 2) * 64 + 64))
        x = 12 + i * (64 * P + 12)
        paste(frame, x, y, P)
        draw.text((x, y + 64 * P + 2), f"Sable {name} ({colours(frame)}c)", fill=(60, 40, 20))
    y += 64 * P + 24
    for i, (name, im) in enumerate(vanilla_p):
        frame = im.crop((0, 0, 64, 64))
        x = 12 + i * (64 * P + 12)
        paste(frame, x, y, P)
        draw.text((x, y + 64 * P + 2), f"{name} ({colours(frame)}c)", fill=(60, 40, 20))
    for i in range(2):  # two more Sable expressions next to the vanilla row for direct comparison
        frame = sable_p.crop((i * 64, 0, i * 64 + 64, 64))
        x = 12 + (4 + i) * (64 * P + 12)
        paste(frame, x, y, P)
        draw.text((x, y + 64 * P + 2), "Sable", fill=(60, 40, 20))

    y += 64 * P + 40
    blocks = [("Sable", sable_s)] + vanilla_s
    x = 12
    for name, im in blocks:
        paste(im, x, y, S, GRASS)
        counts = [colours(im.crop((c * 16, r * 32, c * 16 + 16, r * 32 + 32))) for r in range(4) for c in range(4)]
        draw.text((x, y + 128 * S + 2), f"{name} walk ({min(counts)}-{max(counts)}c per frame)", fill=(60, 40, 20))
        x += 64 * S + 24
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
