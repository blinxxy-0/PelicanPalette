"""Review sheet for the Gilded Vale's building candidates: each building's
PixelLab candidates in a row beside three vanilla houses (Sam's, Emily and
Haley's, the Saloon, cropped from the Town at the game's scale), on grass, at 2x,
numbered, so they're judged against the game's own buildings.

Usage: python tools/art/vale_candidates.py <batch folder> <out folder>
"""
import glob, os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = "art/gilded_vale/buildings"
VANILLA = ["vanilla_sam.png", "vanilla_emily_haley.png", "vanilla_saloon.png"]
BUILDINGS = ["vale_house", "exhibition_hall", "atelier", "pottery", "fern_cottage"]
GRASS = (104, 148, 72, 255)


def main():
    batch, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)
    van = [Image.open(os.path.join(HERE, v)).convert("RGBA") for v in VANILLA]
    for b in BUILDINGS:
        files = sorted(glob.glob(os.path.join(batch, b + "_s*_*.png")))
        if not files:
            continue
        cands = [Image.open(f).convert("RGBA") for f in files]
        row = van + cands
        w = sum(i.width for i in row) + 12 * (len(row) + 1)
        h = max(i.height for i in row) + 30
        sheet = Image.new("RGBA", (w, h), GRASS)
        d = ImageDraw.Draw(sheet)
        x = 12
        for k, im in enumerate(row):
            sheet.alpha_composite(im, (x, h - 18 - im.height))
            label = "vanilla" if k < len(van) else os.path.basename(files[k - len(van)])[len(b) + 1:-4]
            d.text((x, h - 15), label, fill=(255, 255, 255, 255), font=font)
            x += im.width + 12
        sheet = sheet.resize((w * 2, h * 2), Image.NEAREST)
        path = os.path.join(out, b + "_candidates.png")
        sheet.save(path)
        print(path, len(cands))


if __name__ == "__main__":
    main()
