"""Builds Margot Pell's full sprite sheet (special frames and Flower Dance
rows) and her winter, beach and wedding outfits, sprites and portraits, as
Sable's (build_sable_outfits.py, build_sable_sprites.py):

  Characters/MargotPell(_Winter, _Beach, _Wedding)   64x384 sheets
  Portraits/MargotPell(_Winter, _Beach, _Wedding)    128x256 sheets

From her everyday sheets (build_margot_sprites.py, build_margot_portraits.py)
and PixelLab Pixen edits of them (2026-11-01, art/margot/outfits/
outfit_batch.json and eyes_batch.json; picks in art/margot/outfits/picks):

  Winter   a dark moss-green wool coat with brass buttons, a cream knit scarf
  Beach    a sleeveless cream linen top, sage-green shorts, bare arms and legs
  Wedding  an ivory dress with lace sleeves, small white flowers in her bun.
           Worn only for the ceremony (changeSprite/changePortrait).
  Dance    (the Flower Dance rows only) a sage and cream dress, a pink
           flower in her hair

The outfit is taken pixel by pixel where the edit really changed a colour
(build_sable_outfits.outfit_mask), so her face and hair stay as drawn.

Special frames, at Sable's indices: the kiss (28, facing right), working at
an easel from behind (32-34), the wedding (36 front, 37 facing the farmer on
her left, 38 the kiss), asleep (39), and the Flower Dance (40-43 front,
44-47 from behind). The closed eyes and blush of the kiss and sleep are
Pixen edits of single frames; only the changed face pixels are taken (the
edits also added props: a magnifier, brushes), and the same patch goes on
every outfit, whose heads are the everyday one.

The Pixen edits (the dance, winter and wedding ones) turned two of the
four back-view frames (8 and 11) to face the front or the side. Every
outfit's back row is rebuilt from the everyday back heads on the dress of
the two good back frames: 10 (standing) for 8, and 9 (mid-step) mirrored
for 11, the other foot forward.

Usage: build_margot_outfits.py <art/margot/outfits/picks dir> <assets/margot dir>
"""
import colorsys, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black
from build_sable_outfits import outfit_mask, merge
from build_sable_sprites import frame_at, put_frame

COLOURS = 26
PICKS = {"winter": ("winter_walk_1", "winter_portrait_2"), "beach": ("beach_walk_2", "beach_portrait_1"),
         "wedding": ("wedding_walk_1", "wedding_portrait_1")}
DANCE = "dance_walk_1"
# The eye edits: (pick, frame of the everyday sheet it was made from). The
# edits are that frame on a 32x32 canvas, 8 pixels in.
EYES = {"kiss_right": ("kiss_right_1", 4), "kiss_left": ("kiss_left_1", 12), "sleep": ("sleep_3", 0)}
FACE_ROWS = range(6, 17)
# In the portraits, from this row down every pixel the edit touched counts
# as clothing (the collar and shoulders).
COLLAR_LINE = 50
# Her back view's colours, for the arm raised to the easel.
SLEEVE, SLEEVE_EDGE = (237, 223, 173), (107, 65, 45)
SKIN, SKIN_SHADE = (248, 184, 152), (222, 146, 122)
BRUSH_HANDLE, BRUSH_TIP = (122, 89, 71), (60, 120, 104)


def face_patch(everyday, pick, frame_index):
    """The pixels of her face the eye edit changed, as {(x, y): colour},
    leaving out the props the edit added (bright, saturated or new pixels)."""
    base = frame_at(everyday, frame_index)
    edit = pick.crop((8, 0, 24, 32))
    pb, pe = base.load(), edit.load()
    patch = {}
    for y in FACE_ROWS:
        for x in range(16):
            a, b = pb[x, y], pe[x, y]
            if not a[3] or not b[3] or a[:3] == b[:3]:
                continue
            h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in b[:3]))
            if 0.1 < h < 0.75 and s > 0.35:      # yellow, green, blue: props
                continue
            if sum(abs(i - j) for i, j in zip(a[:3], b[:3])) < 40:
                continue
            patch[(x, y)] = b[:3] + (255,)
    return patch


def patched(frame, patch):
    out = frame.copy()
    px = out.load()
    for xy, c in patch.items():
        if px[xy][3]:
            px[xy] = c
    return out


def working_frame(back, raise_by):
    """From behind, her right arm raised to the easel with a brush."""
    out = back.copy()
    px = out.load()
    for y in range(19, 26):              # the hanging arm goes
        for x in (13, 14):
            px[x, y] = (0, 0, 0, 0)
        if px[12, y][3]:
            px[12, y] = SLEEVE_EDGE + (255,)
    shoulder = 18
    for y in range(shoulder - raise_by, shoulder + 1):
        px[13, y] = SLEEVE + (255,)
        px[14, y] = SLEEVE_EDGE + (255,)
    hand = shoulder - raise_by - 1
    px[13, hand] = SKIN + (255,)
    px[14, hand] = SKIN_SHADE + (255,)
    px[14, hand - 1] = BRUSH_HANDLE + (255,)
    px[15, hand - 2] = BRUSH_TIP + (255,)
    return out


def dressed_walk(everyday_walk, edited):
    return quantize(recolour_black(merge(everyday_walk, edited, outfit_mask(everyday_walk, edited))), COLOURS)


HEAD_END = 16


def back_row(everyday_walk, dance):
    """An outfit's back-view row: everyday back heads, the dress from the
    edit's good back frames (see the module notes)."""
    out = dance.copy()
    bodies = {8: frame_at(dance, 10), 9: frame_at(dance, 9), 10: frame_at(dance, 10),
              11: frame_at(dance, 9).transpose(Image.FLIP_LEFT_RIGHT)}
    for i, body in bodies.items():
        frame = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
        frame.alpha_composite(body.crop((0, HEAD_END, 16, 32)), (0, HEAD_END))
        frame.alpha_composite(frame_at(everyday_walk, i).crop((0, 0, 16, HEAD_END)), (0, 0))
        put_frame(out, i, frame)
    return out


def full_sheet(walk, patches, dance, wedding_from=None):
    """The 64x384 sheet: the walk, the special frames, the dance rows.
    wedding_from: the sheet whose front and left frames are the wedding's
    (the dance outfit's on the everyday sheet, as Sable's festival jacket)."""
    sheet = Image.new("RGBA", (64, 384), (0, 0, 0, 0))
    sheet.alpha_composite(walk, (0, 0))
    put_frame(sheet, 28, patched(frame_at(walk, 4), patches["kiss_right"]))
    back = frame_at(walk, 8)
    put_frame(sheet, 32, working_frame(back, 3))
    put_frame(sheet, 33, working_frame(back, 1))
    put_frame(sheet, 34, back)
    wed = wedding_from or walk
    put_frame(sheet, 36, frame_at(wed, 0))
    put_frame(sheet, 37, frame_at(wed, 12))
    put_frame(sheet, 38, patched(frame_at(wed, 12), patches["kiss_left"]))
    put_frame(sheet, 39, patched(frame_at(walk, 0), patches["sleep"]))
    sheet.alpha_composite(dance.crop((0, 0, 64, 32)), (0, 10 * 32))       # 40-43
    sheet.alpha_composite(dance.crop((0, 64, 64, 96)), (0, 11 * 32))      # 44-47
    return sheet


def dressed_portraits(everyday, edited_neutral):
    """The outfit onto every expression; each keeps its own face (the
    expressions differ from the neutral only around the face)."""
    neutral = everyday.crop((0, 0, 64, 64))
    mask = outfit_mask(neutral, edited_neutral, COLLAR_LINE)
    out = everyday.copy()
    unchanged = []
    pn = neutral.load()
    for f in range(everyday.width // 64 * (everyday.height // 64)):
        x0, y0 = (f % 2) * 64, (f // 2) * 64
        frame = everyday.crop((x0, y0, x0 + 64, y0 + 64))
        pf = frame.load()
        own = {(x, y) for y in range(64) for x in range(64) if pf[x, y] != pn[x, y]}
        dressed = merge(frame, edited_neutral, mask - own)
        pd = dressed.load()
        unchanged += [(x0 + x, y0 + y) for y in range(64) for x in range(64) if pd[x, y] == pf[x, y]]
        out.paste(dressed, (x0, y0))
    # Cut the new clothes' colours down, but keep what the outfit didn't
    # touch (face, eyes, hair, the loupe's brass) exactly as it was.
    reduced = quantize(recolour_black(out), 36)
    pr, po = reduced.load(), everyday.load()
    for x, y in unchanged:
        pr[x, y] = po[x, y]
    return reduced


def main():
    picks, assets = sys.argv[1], sys.argv[2]
    load = lambda name: Image.open(os.path.join(picks, name + ".png")).convert("RGBA")
    walk = Image.open(os.path.join(assets, "sprites.png")).convert("RGBA").crop((0, 0, 64, 128))
    portraits = Image.open(os.path.join(assets, "portraits.png")).convert("RGBA")
    patches = {k: face_patch(walk, load(p), i) for k, (p, i) in EYES.items()}
    dance = back_row(walk, dressed_walk(walk, load(DANCE)))
    full_sheet(walk, patches, dance, wedding_from=dance).save(os.path.join(assets, "sprites.png"))
    for outfit, (walk_pick, portrait_pick) in PICKS.items():
        dressed = back_row(walk, dressed_walk(walk, load(walk_pick)))
        # The wedding and the dance rows stay festival clothes, except on
        # the wedding sheet itself, where the ceremony frames are the dress.
        sheet = full_sheet(dressed, patches, dance, wedding_from=None if outfit == "wedding" else dance)
        sheet.save(os.path.join(assets, f"sprites_{outfit}.png"))
        dressed_portraits(portraits, load(portrait_pick)).save(os.path.join(assets, f"portraits_{outfit}.png"))
        print("built", outfit)
    print("patches:", {k: len(v) for k, v in patches.items()})


if __name__ == "__main__":
    main()
