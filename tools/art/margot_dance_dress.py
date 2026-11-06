"""Margot's Flower Dance dress (1.3, from playtesting: she danced in her
work clothes, and turned her back for the last steps).

The Flower Dance's main event (Data/Festivals/spring24) shows the girls'
line in frames 40-43 and 46-47 of each dancer's sheet: 40 standing, 41-43
the twirl, 46-47 arms up. (44-45 are the boys' line; Margot always dances
in the girls'.) Every bachelorette wears the same white dress there, so
Margot's is the base game's own: the dress and arms from Leah's frames,
her skin put in Margot's tones, under Margot's own head from her dance
frames, which keeps her face as it was drawn (no hand-drawn faces).

The base game's dancers bob a pixel in frames 43 and 47; her head bobs
with the dress.

Usage: margot_dance_dress.py <Characters_Leah.png, exported with
       museart_export_tex> <the mod's assets/margot/sprites.png>
       (writes the six frames into the sheet in place)
"""
import sys
from PIL import Image

FRAMES = (40, 41, 42, 43, 46, 47)
CUT = 15                 # the first row of the dress (the neckline), when not bobbing
# Leah's skin ramp, and Margot's: base, shade, dark, outline. Base and the
# outline are Margot's own; the two between are darker shades of her base
# (outlines: a darker shade of the colour beside them, never black).
SKIN = {
    (255, 198, 147): (248, 184, 152),
    (252, 160, 120): (226, 152, 126),
    (232, 105, 78): (186, 112, 92),
    (130, 30, 41): (107, 65, 45),
}
# The arms-up frames raise the arms beside the head: rows 11-14, outside
# columns 4-11.
ARM_ROWS = range(11, CUT)


def frame_box(f):
    return (f % 4 * 16, f // 4 * 32, f % 4 * 16 + 16, f // 4 * 32 + 32)


def top(im):
    return min(y for y in range(32) if any(im.getpixel((x, y))[3] for x in range(16)))


def recolour(im):
    px = im.load()
    for x in range(16):
        for y in range(32):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in SKIN:
                px[x, y] = SKIN[(r, g, b)] + (a,)
    return im


def main():
    leah = Image.open(sys.argv[1]).convert("RGBA")
    sheet_path = sys.argv[2]
    sheet = Image.open(sheet_path).convert("RGBA")
    standing = top(leah.crop(frame_box(40)))
    out = {}
    for f in FRAMES:
        body = recolour(leah.crop(frame_box(f)))
        bob = top(body) - standing
        # Her own head: the twirl frames' heads for 41-42, the standing one otherwise.
        head = sheet.crop(frame_box(f if f in (41, 42, 43) else 40))
        frame = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
        frame.paste(body.crop((0, CUT + bob, 16, 32)), (0, CUT + bob))
        frame.alpha_composite(head.crop((0, 0, 16, CUT)), (0, bob))
        if f in (46, 47):
            for y in ARM_ROWS:
                for x in list(range(0, 4)) + list(range(12, 16)):
                    p = body.getpixel((x, y + bob))
                    if p[3] and p[:3] in SKIN.values():
                        frame.putpixel((x, y + bob), p)
        out[f] = frame
    for f, frame in out.items():
        box = frame_box(f)
        sheet.paste(Image.new("RGBA", (16, 32), (0, 0, 0, 0)), box[:2])
        sheet.alpha_composite(frame, box[:2])
    sheet.save(sheet_path)
    print("wrote Margot's dance frames", FRAMES, "into", sheet_path)


if __name__ == "__main__":
    main()
