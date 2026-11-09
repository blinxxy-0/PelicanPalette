"""The Corrie twins' stained-glass furniture (1.4 dev pack): the counter's windows, lamps, mosaics
and suncatcher, and the heart-event keepsakes (Jet's blue offcut, Opal's spring window, the
window Jet drew for your farmhouse).

Art: Higgsfield (gpt_image_2_6), picked by the user 2026-11-09, in
art/gilded_vale/interiors/studio2/higgsfield/v2/ (README there). This packs them into
"[CP] Gilded Vale (dev)/assets/corrie_furniture.png" with the second frame the game wants
beside each one: the game draws a window's next sprite at night and in rain (so the glass goes
dark), and a lamp's next sprite while it's lit (so the shade glows). Then it writes
data/corrie_furniture.json (the texture and Data/Furniture).

Display names are i18n keys corrie.furniture.<key> (the text approved with the user).
The counter's stock is in data/vale_shops.json.

Usage: python tools/corrie_furniture.py        (from the repository root)"""
import json
import numpy as np
from PIL import Image

PACK = "[CP] Gilded Vale (dev)"
ART = "art/gilded_vale/interiors/studio2/higgsfield/"
SHEET_W = 256
TEXTURE = "Mods/blinxxy.MuseArt/CorrieFurniture"
OUTLINE = (54, 24, 20)

# key: (art, type, sheet tiles, bounding tiles, price, off limits for random sale, (x, y) on the sheet)
ITEMS = {
    # the counter's stock
    "rose_window":    ("v2/rose_window_3_fit",   "window",   (2, 2), (2, 2), 2500, False, (0, 0)),
    "vale_window":    ("v2/vale_window_4_fit",   "window",   (2, 2), (2, 2), 3000, False, (64, 0)),
    "arch_window":    ("v2/arch_window_3_fit",   "window",   (1, 2), (1, 2), 1500, False, (0, 32)),
    "table_lamp":     ("v2/table_lamp_1_fit32",  "lamp",     (1, 2), (1, 1), 1200, False, (32, 32)),
    "floor_lamp":     ("glass_lamp_3_fit",       "lamp",     (1, 3), (1, 1), 1500, False, (0, 64)),
    "mosaic_sun":     ("v2/mosaic_sun_4_fit",    "painting", (2, 2), (2, 2), 2000, False, (80, 32)),
    "mosaic_waves":   ("v2/mosaic_wave_2_fit",   "painting", (2, 2), (2, 2), 2000, False, (112, 32)),
    "suncatcher":     ("v2/catch2_4_fix",        "painting", (1, 2), (1, 2), 800,  False, (64, 32)),
    # the heart events' keepsakes
    "blue_offcut":    ("v2/blue_offcut_3_fit",   "decor",    (1, 1), (1, 1), 100,  True,  (144, 48)),
    "spring_window":  ("v2/spring_window_2_fit", "window",   (2, 2), (2, 2), 3000, True,  (128, 0)),
    "farm_window":    ("v2/farmC_1_fix",         "window",   (2, 2), (2, 2), 3000, True,  (192, 0)),
}
ID = "blinxxy.MuseArt_Corrie"


def name_of(key):
    return ID + "".join(w.capitalize() for w in key.split("_"))


def inside(alpha, depth):
    """Pixels at least `depth` steps from the background (the glass inside a window's frame)."""
    m = alpha >= 128
    for _ in range(depth):
        n = m.copy()
        n[1:] &= m[:-1]; n[:-1] &= m[1:]; n[:, 1:] &= m[:, :-1]; n[:, :-1] &= m[:, 1:]
        m = n
    return m


def night(im):
    """The window at night or in rain: the glass goes dark blue, the frame stays."""
    a = np.asarray(im).astype(float)
    glass = inside(a[..., 3], 3 if im.width >= 32 else 2)
    dark = a[..., :3].sum(2) < 150
    tint = a[..., :3] * 0.32 + np.array([24, 32, 62])
    a[..., :3][glass & ~dark] = tint[glass & ~dark]
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def lit(im, shade_rows):
    """The lamp lit: its shade, the top rows of the lamp, glows warm."""
    a = np.asarray(im).astype(float)
    ys = np.nonzero((a[..., 3] >= 128).any(1))[0]
    top = ys[0]
    rows = np.zeros(a.shape[:2], bool)
    rows[top:top + shade_rows] = True
    shade = rows & (a[..., 3] >= 128) & (a[..., :3].sum(2) >= 150)
    warm = a[..., :3] * 1.25 + np.array([46, 34, 6])        # (brighter, same colours: blending to cream washed it out)
    a[..., :3][shade] = warm[shade]
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def main():
    sheet = Image.new("RGBA", (SHEET_W, 112), (0, 0, 0, 0))
    used = np.zeros((112 // 16, SHEET_W // 16), bool)
    entries = {}
    for key, (art, typ, (tw, th), (bw, bh), price, off, (x, y)) in ITEMS.items():
        im = Image.open(ART + art + ".png").convert("RGBA")
        assert im.size == (tw * 16, th * 16), (key, im.size)
        frames = [im]
        if typ == "window":
            frames.append(night(im))
        elif typ == "lamp":
            frames.append(lit(im, 12 if th == 2 else 9))
        for i, f in enumerate(frames):
            cx, cy = x // 16 + i * tw, y // 16
            assert not used[cy:cy + th, cx:cx + tw].any(), f"{key} overlaps on the sheet"
            used[cy:cy + th, cx:cx + tw] = True
            sheet.alpha_composite(f, (x + i * tw * 16, y))
        index = (y // 16) * (SHEET_W // 16) + x // 16
        n = name_of(key)
        entries[n] = (f"{n}/{typ}/{tw} {th}/{bw} {bh}/1/{price}/-1/{{{{i18n:corrie.furniture.{key}}}}}"
                      f"/{index}/{TEXTURE.replace('/', chr(92))}/{'true' if off else 'false'}")
    sheet.save(f"{PACK}/assets/corrie_furniture.png")
    head = ("// The Corrie twins' stained-glass furniture: generated by tools/corrie_furniture.py (edit that,\n"
            "// rerun it). The counter's stock (data/vale_shops.json) and the heart events' keepsakes\n"
            "// (data/corrie_events.json). The sheet holds each window's night frame and each lamp's lit\n"
            "// frame beside it, where the game looks for them.\n"
            "// Furniture fields: name/type/sheet size/bounding box/rotations/price/placement/display name/sprite index/texture/off limits for sale\n")
    changes = [{"Action": "Load", "Target": TEXTURE, "FromFile": "assets/corrie_furniture.png"},
               {"Action": "EditData", "Target": "Data/Furniture", "Entries": entries}]
    with open(f"{PACK}/data/corrie_furniture.json", "w", encoding="utf-8", newline="\n") as f:
        f.write(head + json.dumps({"Changes": changes}, indent=2) + "\n")
    for n in entries:
        print(n)


if __name__ == "__main__":
    main()
