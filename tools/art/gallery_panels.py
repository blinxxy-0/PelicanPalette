"""More displays for the 14-heart exhibition (2026-10-25: the user asked
for at least two more now the room has space, and a really crazy one in
the middle).

Each is the landscape panel (a cream display board on legs) with its
picture taken off and another of Sable's paintings hung in its place,
centred where the old one was. They're added to the furniture sheet
(ArtShopFurniture.png) below what's there, and to furniture.json, so
build_shop_interior.py can place them; running this again replaces them.

Usage: gallery_panels.py <ArtShopFurniture.png> <furniture.json>
"""
import json
import os
import sys
from collections import Counter
from PIL import Image

PANELS = {
    "gallery_panel_cabin": "painting_snow_cabin",
    "gallery_panel_night": "painting_night",
}
BASE = "gallery_panel_landscape"

# Whole pieces from their own art: the centrepiece, a Junimo made of
# squeezed paint tubes on a marble plinth (PixelLab Create Image Pro, the
# user's pick of the concepts; candidate 12 of job fae9dd10).
IMPORTS = {"gallery_junimo": "gallery_junimo.png"}


def piece(sheet, at):
    x, y, w, h = at["x"], at["y"], at["w"], at["h"]
    return sheet.crop((x * 16, y * 16, (x + w) * 16, (y + h) * 16))


def blank_panel(panel):
    """The panel with its picture lifted off: everything between the board's
    label strip and its legs that isn't board-cream becomes board-cream."""
    px = panel.load()
    cream = Counter(c for c in panel.getdata() if c[3] and c[0] > 200 and c[1] > 190).most_common(1)[0][0]
    # The picture: the box around the non-cream pixels on the board.
    board = [(x, y) for y in range(panel.height) for x in range(panel.width)
             if px[x, y][3] and px[x, y][0] > 200 and px[x, y][1] > 190]
    bx0, by0 = min(x for x, _ in board), min(y for _, y in board)
    bx1, by1 = max(x for x, _ in board), max(y for _, y in board)
    picture = [(x, y) for y in range(by0 + 4, by1 - 1) for x in range(bx0 + 1, bx1)
               if px[x, y][3] and px[x, y] != cream]
    px0, py0 = min(x for x, _ in picture), min(y for _, y in picture)
    px1, py1 = max(x for x, _ in picture), max(y for _, y in picture)
    for y in range(py0, py1 + 1):
        for x in range(px0, px1 + 1):
            px[x, y] = cream
    return panel, ((px0 + px1) // 2, (py0 + py1) // 2)


def main():
    sheet_path, json_path = sys.argv[1], sys.argv[2]
    sheet = Image.open(sheet_path).convert("RGBA")
    pieces = json.load(open(json_path))
    for name in list(PANELS) + list(IMPORTS):
        pieces.pop(name, None)
    top = max(p["y"] + p["h"] for p in pieces.values())
    cols = sheet.width // 16
    out = Image.new("RGBA", (sheet.width, (top + 3) * 16), (0, 0, 0, 0))
    art_dir = os.path.dirname(os.path.abspath(json_path))
    out.alpha_composite(sheet.crop((0, 0, sheet.width, top * 16)))
    x = 0
    for name, painting in PANELS.items():
        panel, (cx, cy) = blank_panel(piece(sheet, pieces[BASE]))
        art = piece(sheet, pieces[painting])
        art = art.crop(art.getbbox())
        panel.alpha_composite(art, (cx - art.width // 2, cy - art.height // 2))
        w, h = pieces[BASE]["w"], pieces[BASE]["h"]
        assert x + w <= cols
        out.alpha_composite(panel, (x * 16, top * 16))
        pieces[name] = {"x": x, "y": top, "w": w, "h": h}
        x += w
    for name, file in IMPORTS.items():
        art = Image.open(os.path.join(art_dir, file)).convert("RGBA")
        w, h = art.width // 16, art.height // 16
        assert x + w <= cols
        out.alpha_composite(art, (x * 16, top * 16 + (3 - h) * 16))
        pieces[name] = {"x": x, "y": top + 3 - h, "w": w, "h": h}
        x += w
    out.save(sheet_path)
    json.dump(pieces, open(json_path, "w"), indent=1)
    print("added", ", ".join(list(PANELS) + list(IMPORTS)))


if __name__ == "__main__":
    main()
