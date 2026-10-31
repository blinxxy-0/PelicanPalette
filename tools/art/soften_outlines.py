"""Gives the shop's furniture sheet vanilla outlines (1.2): every dark,
colourless pixel in each piece becomes a darker shade of the colour it
borders (vanillafy.selout). Runs on the finished sheet, after
build_shop_furniture.py and gallery_panels.py have both added their pieces,
so it covers them all; the layout doesn't move.

Usage: soften_outlines.py <ArtShopFurniture.png> <furniture.json>
"""
import json, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout


def main():
    sheet_path, index = sys.argv[1], sys.argv[2]
    sheet = Image.open(sheet_path).convert("RGBA")
    for name, p in json.load(open(index)).items():
        box = (p["x"] * 16, p["y"] * 16, (p["x"] + p["w"]) * 16, (p["y"] + p["h"]) * 16)
        sheet.paste(selout(sheet.crop(box)), box[:2])
    sheet.save(sheet_path)
    print("softened", sheet_path)


if __name__ == "__main__":
    main()
