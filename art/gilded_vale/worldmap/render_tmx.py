"""Render a Stardew TMX map from its tile layers (first animation frame), for a season.
Usage: render_tmx.py <map.tmx> <season> <raw dir> <out.png>"""
import os, struct, sys
import xml.etree.ElementTree as ET
from PIL import Image

tmx, season, rawdir, out = sys.argv[1:5]
assets = os.path.dirname(tmx)
root = ET.parse(tmx).getroot()
W, H = int(root.get('width')), int(root.get('height'))


def raw(name):
    p = os.path.join(rawdir, name + '.raw')
    b = open(p, 'rb').read()
    w, h = struct.unpack('<ii', b[:8])
    return Image.frombytes('RGBA', (w, h), b[8:8 + w * h * 4])


sheets = []
for ts in root.findall('tileset'):
    src = ts.find('image').get('source')
    base = os.path.basename(src)
    for s in ('spring', 'summer', 'fall', 'winter'):
        if base.startswith(s + '_'):
            base = season + base[len(s):]
    if base.endswith('.png'):
        p = os.path.join(assets, base)
        if not os.path.exists(p):
            p = os.path.join(assets, os.path.basename(src))
        img = Image.open(p).convert('RGBA')
    else:
        try:
            img = raw(base)
        except FileNotFoundError:
            img = raw(os.path.basename(src))
    sheets.append((int(ts.get('firstgid')), int(ts.get('columns')), img))
sheets.sort()

canvas = Image.new('RGBA', (W * 16, H * 16), (0, 0, 0, 255))
skip = {'Paths'}
for layer in root.findall('layer'):
    if layer.get('name') in skip or layer.get('visible') == '0':
        continue
    data = [int(x) for x in layer.find('data').text.replace('\n', '').split(',') if x.strip()]
    for i, g in enumerate(data):
        gid = g & 0x1FFFFFFF
        if not gid:
            continue
        sheet = None
        for first, cols, img in sheets:
            if first <= gid:
                sheet = (first, cols, img)
        first, cols, img = sheet
        t = gid - first
        tile = img.crop(((t % cols) * 16, (t // cols) * 16, (t % cols) * 16 + 16, (t // cols) * 16 + 16))
        if g & 0x80000000:
            tile = tile.transpose(Image.FLIP_LEFT_RIGHT)
        if g & 0x40000000:
            tile = tile.transpose(Image.FLIP_TOP_BOTTOM)
        x, y = (i % W) * 16, (i // W) * 16
        canvas.alpha_composite(tile, (x, y))
canvas.save(out)
print(out, canvas.size)
