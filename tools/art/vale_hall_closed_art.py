"""The closed Exhibition Hall's pieces (until Lupini's 8-heart event), made without new
PixelLab generations (the user, 2026-11-07: keep the allowance for the characters).
From the hall batches' spare dust-sheet candidates, stretched to the footprint of
the display they cover, and a few drawn here:

  sheet_panel, sheet_panel_b   a tall dust sheet over a gallery panel (2 x 3): a candidate made taller
  sheet_desk                   the reception desk under a sheet (3 x 2): a candidate made wider
  sheet_bench, sheet_bench_b   the two gallery benches under sheets (3 x 1): low candidates made wider
  hook_<id>                    where one of the game's paintings hung: a paler square of wallpaper
                               the painting's exact size, its nail (drawn like the atelier's empty_hook)
  cobweb_l                     the Old Studio's cobweb, for a wall's left corner
  dust                         a drift of dust on the floor

Usage: vale_hall_closed_art.py <furniture.png (the game's TileSheets/furniture)>
       (writes art/gilded_vale/interiors/hall/closed/*.png)
"""
import os, random, sys
from PIL import Image, ImageDraw

INT = "art/gilded_vale/interiors"
OUT = os.path.join(INT, "hall", "closed")
# the game's paintings that hang in the open hall's gallery: id -> tiles (build_vale_buildings.GAME_PAINTINGS)
HUNG = {1554: (3, 2), 1541: (2, 2), 1612: (2, 2), 2421: (2, 2), 1567: (1, 2)}


def load(path):
    im = Image.open(os.path.join(INT, path + ".png")).convert("RGBA")
    return im.crop(im.getbbox())


def stretch(im, w=None, h=None):
    """im made w wide / h tall by repeating the columns / rows of its middle (a drape's
    folds run down it, so its body repeats without a seam)."""
    if w and w > im.width:
        m = im.width // 2
        out = Image.new("RGBA", (w, im.height))
        out.alpha_composite(im.crop((0, 0, m, im.height)), (0, 0))
        band = im.crop((m - 3, 0, m + 3, im.height))
        for x in range(m, w - (im.width - m), band.width):
            out.alpha_composite(band, (x, 0))
        out.alpha_composite(im.crop((m, 0, im.width, im.height)), (w - (im.width - m), 0))
        im = out
    if h and h > im.height:
        m = im.height * 3 // 5                      # below the shape's top, above the hem
        out = Image.new("RGBA", (im.width, h))
        out.alpha_composite(im.crop((0, 0, im.width, m)), (0, 0))
        band = im.crop((0, m - 4, im.width, m))
        for y in range(m, h - (im.height - m), band.height):
            out.alpha_composite(band, (0, y))
        out.alpha_composite(im.crop((0, m, im.width, im.height)), (0, h - (im.height - m)))
        im = out
    return im


def sheets():
    stretch(load("hall/hall_v1/sheeted_1_62"), h=44).save(os.path.join(OUT, "sheet_panel.png"))
    stretch(load("hall/hall_v1/sheeted_1_31"), h=44).save(os.path.join(OUT, "sheet_panel_b.png"))
    stretch(load("hall/hall_v1/sheeted_1_29"), w=44).save(os.path.join(OUT, "sheet_desk.png"))
    stretch(load("hall/hall_v1/sheeted_1_9"), w=46).save(os.path.join(OUT, "sheet_bench.png"))
    stretch(load("hall/hall_v1/sheeted_1_53"), w=46).save(os.path.join(OUT, "sheet_bench_b.png"))


def hooks(furniture):
    """A paler square where each painting hung, its exact size and place in its tiles."""
    sheet = Image.open(furniture).convert("RGBA")
    for n, (w, h) in HUNG.items():
        x, y = (n % 32) * 16, (n // 32) * 16
        x0, y0, x1, y1 = sheet.crop((x, y, x + w * 16, y + h * 16)).getbbox()
        im = Image.new("RGBA", (w * 16, h * 16))
        d = ImageDraw.Draw(im)
        d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=(255, 250, 235, 46), outline=(90, 60, 40, 40))
        cx = (x0 + x1) // 2 - 1
        d.point([(cx, y0 + 2), (cx + 1, y0 + 2)], fill=(176, 140, 60, 255))     # the nail
        d.point([(cx, y0 + 3)], fill=(100, 70, 30, 255))
        im.save(os.path.join(OUT, f"hook_{n}.png"))


def cobweb_l():
    Image.open(os.path.join(INT, "studio", "cobweb.png")).transpose(Image.FLIP_LEFT_RIGHT).save(
        os.path.join(OUT, "cobweb_l.png"))


def dust():
    rnd = random.Random(8)
    im = Image.new("RGBA", (16, 16))
    px = im.load()
    for _ in range(70):                     # a soft grey drift, thickest in the middle
        x, y = int(rnd.gauss(8, 3)), int(rnd.gauss(9, 2))
        if 1 <= x < 15 and 3 <= y < 15:
            a = px[x, y][3]
            px[x, y] = (128, 118, 104, min(170, a + 60))
    for _ in range(4):                      # and a few fluffs
        x, y = rnd.randrange(2, 14), rnd.randrange(4, 14)
        px[x, y] = (206, 200, 190, 220)
    im.save(os.path.join(OUT, "dust.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    sheets()
    hooks(sys.argv[1])
    cobweb_l()
    dust()
    print("wrote", OUT)
