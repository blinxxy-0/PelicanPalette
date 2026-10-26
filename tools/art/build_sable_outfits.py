"""Builds Sable's winter, beach and wedding outfits (Characters/Sable_Winter,
Portraits/Sable_Winter, Characters/Sable_Beach, Portraits/Sable_Beach,
Characters/Sable_Wedding, Portraits/Sable_Wedding) from
their everyday sheets and PixelLab Pixen edits of them (2026-10-25,
art/sable/outfits/outfit_batch.json).

The game dresses 30 villagers for winter (worn outdoors in winter, so at
the Ice Festival and the Feast of the Winter Star) and 21 for Ginger
Island. Like theirs, Sable's outfits change only the clothes:

  Winter  a plum wool coat and a chunky mustard knit scarf
  Beach   a pale sky-blue linen shirt open at the collar, sand shorts,
          bare arms and legs
  Wedding (1.1) a cream suit over a teal waistcoat and white collar, a pink
          flower on the lapel and a paint-splashed pocket square, goggles
          off. Worn only for the ceremony: the wedding script switches to it
          with changeSprite/changePortrait.

Pixen redrew each whole image, nudging every colour a little. So the
outfit is taken pixel by pixel: where the edit changed a colour a lot (or
a pixel appeared or vanished), that's the new clothing and the edit wins;
where it barely moved it (the face, hair, goggles, skin), the original
pixel stays, in Sable's exact palette.

  Sprites    the walk rows from the edit; the special frames (kiss,
             painting, sleep) rebuilt from them as the everyday sheet's are;
             the wedding and Flower Dance frames stay in the festival jacket.
  Portraits  the neutral's clothing goes onto every expression; each
             expression keeps its own face.

Usage: build_sable_outfits.py <generations dir> <assets/sable dir>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black
import build_sable_sprites as sprites

PICKS = {"winter": ("winter_walk_1", "winter_portrait_1"), "beach": ("beach_walk_1", "beach_portrait_1"),
         "wedding": ("wedding_walk_1", "wedding_portrait_1")}
# The wedding walk edit turned Sable's teal hair black, so its frames keep
# the everyday head (frame rows above HEAD_END) and take only the suit; the
# goggles on that head are painted over with the hair beside them.
HEAD_END = 17
GOGGLES = {(0x5D, 0x3F, 0x39), (0xBF, 0x95, 0x74), (0x7A, 0x59, 0x47)}
GOGGLE_ROWS = range(6, 10)
HAIR = (0x20, 0x23, 0x2A)
CHANGED = 70            # colour distance above which the edit counts as new clothing
# In the portraits, everything below the collar line (under the chin) is
# clothing outright: the beach shirt's bare chest is close enough in colour
# to the old turtleneck that the distance test alone kept the turtleneck.
COLLAR_LINE = 45
# The everyday portrait's turtleneck and apron colours, and Sable's neck skin.
OLD_CLOTHES = {(0xB1, 0x39, 0x20), (0xA7, 0x37, 0x22), (0x7C, 0x18, 0x1F), (0xF2, 0xDE, 0xAA), (0xDF, 0xD6, 0xB5), (0xAD, 0x98, 0x7D)}
JAW_LINE = 40           # expressions keep their own pixels above this row
COLLAR_SLIVER_TOP = 36  # the old collar can show beside the jaw from this row down
NECK = (0xA0, 0x57, 0x38)
COLOURS = 26            # per sheet, as the everyday one


def distance(a, b):
    return sum((x - y) ** 2 for x, y in zip(a[:3], b[:3])) ** 0.5


def outfit_mask(original, edited, clothing_from=None):
    """Pixels the edit really changed (new clothing); every pixel from row
    `clothing_from` down counts as clothing."""
    po, pe = original.load(), edited.load()
    return {(x, y) for y in range(original.height) for x in range(original.width)
            if (clothing_from is not None and y >= clothing_from and (po[x, y][3] >= 128 or pe[x, y][3] >= 128))
            or (po[x, y][3] >= 128) != (pe[x, y][3] >= 128)
            or (pe[x, y][3] >= 128 and distance(po[x, y], pe[x, y]) > CHANGED)}


def merge(original, edited, mask):
    out = original.copy()
    po, pe = out.load(), edited.load()
    for x, y in mask:
        po[x, y] = pe[x, y] if pe[x, y][3] >= 128 else (0, 0, 0, 0)
    return out


def build_sprites(everyday, edited_walk):
    walk = everyday.crop((0, 0, 64, 128))
    dressed = merge(walk, edited_walk, outfit_mask(walk, edited_walk))
    dressed = quantize(recolour_black(dressed), COLOURS)
    sheet = everyday.copy()
    sheet.paste(Image.new("RGBA", (64, 128), (0, 0, 0, 0)), (0, 0))
    sheet.alpha_composite(dressed, (0, 0))
    sheet = sprites.add_special_frames(sheet)
    # The wedding (36-38) and the Flower Dance (rows 10-11) keep the jacket.
    for y0, h in ((9 * 32, 32), (10 * 32, 64)):
        sheet.paste(everyday.crop((0, y0, 64, y0 + h)), (0, y0))
    sheet.paste(everyday.crop((48, 9 * 32, 64, 10 * 32)), (48, 9 * 32))   # 39, asleep: indoors anyway
    return sheet


def without_goggles(walk):
    """The walk frames with the goggles on top of the head painted out with
    the hair next to them."""
    out = walk.copy()
    px = out.load()
    for fy in range(0, walk.height, 32):
        for fx in range(0, walk.width, 16):
            for y in GOGGLE_ROWS:
                row = [px[fx + x, fy + y] for x in range(16)]
                for x in range(16):
                    if row[x][3] and row[x][:3] in GOGGLES:
                        near = [row[x + d] for d in (-1, 1, -2, 2, -3, 3) if 0 <= x + d < 16
                                and row[x + d][3] and row[x + d][:3] not in GOGGLES]
                        px[fx + x, fy + y] = near[0] if near else HAIR + (255,)
    return out


def build_wedding_sprites(everyday, edited_walk):
    walk = without_goggles(everyday.crop((0, 0, 64, 128)))
    # Below the head it's all suit (the threshold test kept bits of the old
    # apron and turtleneck where the edit's colours were close).
    pw, pe = walk.load(), edited_walk.load()
    mask = {(x, y) for y in range(walk.height) for x in range(walk.width)
            if y % 32 >= HEAD_END and (pw[x, y][3] >= 128 or pe[x, y][3] >= 128)}
    dressed = quantize(recolour_black(merge(walk, edited_walk, mask)), COLOURS)
    sheet = everyday.copy()
    sheet.paste(Image.new("RGBA", (64, 128), (0, 0, 0, 0)), (0, 0))
    sheet.alpha_composite(dressed, (0, 0))
    sheet = sprites.add_special_frames(sheet)
    # The ceremony frames in the suit itself: 36 facing front, 37 turned to
    # the farmer on their left, 38 the kiss.
    front, left = sprites.frame_at(sheet, 0), sprites.frame_at(sheet, 12)
    sprites.put_frame(sheet, 36, front)
    sprites.put_frame(sheet, 37, left)
    sprites.put_frame(sheet, 38, sprites.close_eye(left, lean=-1))
    return sheet


def without_strands(portrait):
    """Clears one-pixel-wide strands hanging in empty space: on a tilted
    head the old turtleneck's outline is left beside the suit's collar."""
    out = portrait.copy()
    px = out.load()
    for _ in range(3):
        strays = [(x, y) for y in range(out.height) for x in range(1, out.width - 1)
                  if px[x, y][3] and not px[x - 1, y][3] and not px[x + 1, y][3]]
        for x, y in strays:
            px[x, y] = (0, 0, 0, 0)
    return out


def build_portraits(everyday, edited_neutral, strands=False):
    neutral = everyday.crop((0, 0, 64, 64))
    mask = outfit_mask(neutral, edited_neutral, COLLAR_LINE)
    out = everyday.copy()
    unchanged = []      # (x, y) on the sheet that the outfit left as they were
    for f in range(everyday.width // 64 * (everyday.height // 64)):
        x0, y0 = (f % 2) * 64, (f // 2) * 64
        frame = everyday.crop((x0, y0, x0 + 64, y0 + 64))
        # Where this expression differs from the neutral (its face), it wins.
        pf, pn = frame.load(), neutral.load()
        # (Only above the jaw: a few clothing pixels differ between the
        # everyday expressions, and a tilted head shows a sliver of the old
        # collar beside the jaw; from there down the outfit wins.)
        own = {(x, y) for y in range(JAW_LINE) for x in range(64) if pf[x, y] != pn[x, y]}
        dressed = merge(frame, edited_neutral, mask - own)
        # A tilted head shows a sliver of the old turtleneck or apron beside
        # the jaw that the neutral doesn't: that's neck now.
        pd = dressed.load()
        # (Only beside the jaw: higher up, the same creams are the eye whites
        # and the blush, which had been painted over as skin.)
        for x, y in own:
            if y >= COLLAR_SLIVER_TOP and pd[x, y][:3] in OLD_CLOTHES:
                pd[x, y] = NECK + (255,)
        dressed = without_strands(dressed) if strands else dressed
        pd, pf = dressed.load(), frame.load()
        unchanged += [(x0 + x, y0 + y) for y in range(64) for x in range(64) if pd[x, y] == pf[x, y]]
        out.paste(dressed, (x0, y0))
    # The palette is cut down for the new clothes, but what the outfit didn't
    # touch (the face, eyes, hair) keeps its exact colours: cutting the whole
    # sheet's palette had swapped the irises for the nearest leftover colour.
    reduced = quantize(recolour_black(out), 34)
    pr, po = reduced.load(), everyday.load()
    for x, y in unchanged:
        pr[x, y] = po[x, y]
    return reduced


def main():
    gen_dir, sable_dir = sys.argv[1], sys.argv[2]
    everyday_sprites = Image.open(os.path.join(sable_dir, "sprites.png")).convert("RGBA")
    everyday_portraits = Image.open(os.path.join(sable_dir, "portraits.png")).convert("RGBA")
    for outfit, (walk_name, portrait_name) in PICKS.items():
        walk = Image.open(os.path.join(gen_dir, walk_name + ".png")).convert("RGBA")
        portrait = Image.open(os.path.join(gen_dir, portrait_name + ".png")).convert("RGBA")
        build = build_wedding_sprites if outfit == "wedding" else build_sprites
        build(everyday_sprites, walk).save(os.path.join(sable_dir, f"sprites_{outfit}.png"))
        build_portraits(everyday_portraits, portrait, strands=outfit == "wedding").save(os.path.join(sable_dir, f"portraits_{outfit}.png"))
        print("built", outfit)


if __name__ == "__main__":
    main()
