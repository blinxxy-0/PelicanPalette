"""Seasonal sheets for Margot's cottage and Sable's shop (1.3, from
playtesting: neither had a winter look).

The game swaps an outdoor map's tilesheet named spring_<name> for
<season>_<name> as the seasons turn (GameLocation.updateSeasonalTileSheets),
so each building's sheet is written four times: spring, summer and fall as
drawn, and winter with the building under snow. The map patches
(MargotCottagePatch.tmx, ArtShopExteriorPatch.tmx) name the spring sheet.

Winter art (art/winter, PixelLab Pixen edits of the buildings, 2026-11-05):
  cottage_winter.png  the cottage (the sheet's 80x96 top-left), snow on the
                      roof and sills, the ivy bare, the window box snowed
                      over (so its lavender and marigold tiles, 21-22 and
                      29-30, are the snowy box too)
  shop_winter.png     the shop's whole 112x112 sheet, snow on both roofs,
                      its planters gone for the winter

Run after build_margot_cottage.py and margot_window_box.py.

Usage: seasonal_buildings.py <repo root>
"""
import os, sys
from PIL import Image

SHEET_COLS = 8


def tile(index, cols=SHEET_COLS):
    return (index % cols * 16, index // cols * 16, index % cols * 16 + 16, index // cols * 16 + 16)


def write_seasons(assets, name, winter):
    spring = Image.open(os.path.join(assets, "spring_" + name)).convert("RGBA")
    for season in ("summer", "fall"):
        spring.save(os.path.join(assets, f"{season}_{name}"))
    winter.save(os.path.join(assets, "winter_" + name))
    print("wrote the seasons of", name)


def main():
    root = sys.argv[1]
    assets = os.path.join(root, "[CP] Pelican Palette", "assets")
    art = os.path.join(root, "art", "winter")
    for name in ("MargotCottage.png", "ArtShopExterior.png"):
        plain, spring = os.path.join(assets, name), os.path.join(assets, "spring_" + name)
        if os.path.exists(plain):
            os.replace(plain, spring)

    cottage = Image.open(os.path.join(assets, "spring_MargotCottage.png")).convert("RGBA")
    winter = cottage.copy()
    house = Image.open(os.path.join(art, "cottage_winter.png")).convert("RGBA")
    winter.paste(Image.new("RGBA", house.size, (0, 0, 0, 0)), (0, 0))
    winter.alpha_composite(house, (0, 0))
    for src, dst in ((33, 21), (34, 22), (33, 29), (34, 30)):
        winter.paste(winter.crop(tile(src)), tile(dst)[:2])
    write_seasons(assets, "MargotCottage.png", winter)

    shop = Image.open(os.path.join(art, "shop_winter.png")).convert("RGBA")
    write_seasons(assets, "ArtShopExterior.png", shop)


if __name__ == "__main__":
    main()
