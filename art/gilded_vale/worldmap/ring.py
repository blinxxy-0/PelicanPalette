"""Draws A's gold ring around the old tree in B (the user, 2026-11-08: B, with A's ring)."""
import math, sys
from PIL import Image
src, dst = sys.argv[1], sys.argv[2]
b = Image.open(src).convert('RGB'); out = b.copy()
bright = [(0xfd,0xd8,0x14),(0xfc,0xe2,0x26),(0xfd,0xf3,0x54),(0xf9,0xe8,0x1d),(0xfe,0xfb,0x79),(0xfb,0xda,0x27)]
deep = [(0xdf,0xb9,0x14),(0xdd,0xba,0x20),(0xe7,0xc8,0x24),(0xd6,0xc7,0x28)]
CX, CY = 126, 160
def ellipse(rx, ry):
    pts = []
    for k in range(1440):
        t = 2 * math.pi * k / 1440
        p = (round(CX + rx * math.cos(t)), round(CY + ry * math.sin(t)))
        if p not in pts: pts.append(p)
    return pts
def keep(p):
    r, g, bl = b.getpixel(p)
    return r > g + 10 or (r + g + bl) < 0x120   # the trunk, roots and shadows stay
for i, p in enumerate(ellipse(13.5, 5)):
    if i % 9 != 8 and not keep(p): out.putpixel(p, deep[i % len(deep)])
for i, p in enumerate(ellipse(12.5, 4)):
    if i % 7 != 6 and not keep(p): out.putpixel(p, bright[(i * 5) % len(bright)])
# a soft glow on the grass inside
for y in range(CY - 4, CY + 5):
    for x in range(CX - 12, CX + 13):
        if ((x - CX) / 11) ** 2 + ((y - CY) / 3.2) ** 2 <= 1 and not keep((x, y)) and out.getpixel((x, y)) == b.getpixel((x, y)):
            r, g, bl = b.getpixel((x, y)); out.putpixel((x, y), (min(255, r + 30), min(255, g + 16), bl))
out.save(dst)
