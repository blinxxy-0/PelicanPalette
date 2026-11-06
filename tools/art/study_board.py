"""Margot's study board (1.3, ArtStudio/Social/StudyBoard.cs): six 2x2
stages of a cork board, bare to full, then full with her certificate.

Sources in art/study_board/ (PixelLab, 2026-11-05): full_c42.png is the
Create Image Pro pick (styled on the shop's commission board); bare.png an
inpaint of its cork; stage1-4 put its pinned studies back a few at a time
(compose_notes.py); stage5 adds the certificate cut from a Pixen edit
(cert_edit.png). Each stage gets vanilla outlines (vanillafy.selout).

Installs them in row 4 of ArtStudio/assets/furniture.png (sprite 320, the
stages two tiles apart), growing the sheet to 96 pixels tall.

Usage: study_board.py <repo root>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

ROW_Y = 64


def main():
    root = sys.argv[1]
    src = os.path.join(root, "art", "study_board")
    sheet_path = os.path.join(root, "ArtStudio", "assets", "furniture.png")
    sheet = Image.open(sheet_path).convert("RGBA")
    if sheet.height < ROW_Y + 32:
        taller = Image.new("RGBA", (sheet.width, ROW_Y + 32), (0, 0, 0, 0))
        taller.paste(sheet, (0, 0))
        sheet = taller
    for stage in range(6):
        im = Image.open(os.path.join(src, f"stage{stage}.png")).convert("RGBA")
        assert im.size == (32, 32), im.size
        box = (stage * 32, ROW_Y, stage * 32 + 32, ROW_Y + 32)
        sheet.paste(Image.new("RGBA", (32, 32), (0, 0, 0, 0)), box[:2])
        sheet.paste(selout(im), box[:2])
    sheet.save(sheet_path)
    print("installed 6 stages in", sheet_path, sheet.size)


if __name__ == "__main__":
    main()
