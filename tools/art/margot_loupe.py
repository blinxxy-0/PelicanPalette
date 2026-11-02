"""Margot's loupe (1.3, the museum collection's reward for twelve donated):
its inventory icon, 16x16, to [CP] Pelican Palette/assets/collection/loupe.png.
A brass jeweller's loupe with a pale blue lens (PixelLab, picked 2026-11-01,
art/collection; drawn in the style of the game's ring icons, and unwrapped:
the generator returned it split across the left and right edges). Outline
recoloured (vanillafy.selout), never black.

Usage: margot_loupe.py art/collection/picks <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout


def main():
    picks, out = sys.argv[1], sys.argv[2]
    selout(Image.open(os.path.join(picks, "loupe_17.png")).convert("RGBA")).save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
