"""The mod page's art: wooden sign headings, the framed banner, and the
title card (the banner with the mod's name across it).

The heading text is the game's own bold pixel font, rendered in game by
`museart_signtext` (at the font's 3x scale) into docs/media/signtext/. Each
sign is a board of planks between two posts, drawn here pixel by pixel in
the keg and Varnishing Cabinet's wood colours, then scaled 3x to match the
text. The banner is three in-game screenshots side by side in a log frame
at the game's 4x scale, under the title sign.

Usage: page_art.py <docs/media dir>   (writes <dir>/page/)
"""
import os
import sys
from PIL import Image

WOOD = {
    "o": (61, 34, 20), "D": (99, 52, 28), "R": (138, 72, 38), "M": (176, 98, 45),
    "L": (214, 135, 62), "H": (240, 176, 92),
}
SHADOW = (61, 34, 20, 255)
SIGN_SCALE, FRAME_SCALE = 3, 4


def px(im, x, y, key):
    if 0 <= x < im.width and 0 <= y < im.height:
        im.putpixel((x, y), WOOD[key] + (255,))


def board(width, planks=3, plank_h=7):
    """A board of `planks` horizontal planks, `width` px wide (1x), with a
    post at each end; returns the image and the board's inner box."""
    post_w, cap = 5, 3
    height = planks * plank_h + 2 + 2 * cap
    im = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    left, right = post_w - 1, width - post_w
    for p in range(planks):
        top = cap + 1 + p * plank_h
        for x in range(left, right + 1):
            for y in range(top, top + plank_h):
                edge = y == top + plank_h - 1
                key = "o" if edge else "L" if y == top else "R"
                # wood grain: short darker streaks
                if not edge and y != top and (x * 7 + y * 13 + p * 5) % 23 == 0:
                    key = "D"
                px(im, x, y, key)
    for x in range(left, right + 1):
        px(im, x, cap, "o")
    # Posts: rounded log ends standing a little proud of the board.
    for x0 in (0, width - post_w):
        for x in range(x0, x0 + post_w):
            for y in range(0, height):
                rim = x in (x0, x0 + post_w - 1) or y in (0, height - 1)
                key = "o" if rim else "H" if x == x0 + 1 else "M" if x == x0 + 2 else "D"
                px(im, x, y, key)
        for y in (2, height - 3):            # bolts
            px(im, x0 + 2, y, "o")
    return im, (left + 2, cap + 2, right - 2, height - cap - 2)


def sign(text_png, out_png):
    text = Image.open(text_png).convert("RGBA")
    text = text.crop(text.getbbox())
    # Planks with room around the words, inside a frame of logs.
    w = (text.width + 2 * 48) // SIGN_SCALE
    planks, _ = board(w, planks=3, plank_h=9)
    planks = planks.crop((4, 3, planks.width - 4, planks.height - 3))
    frame, border = log_frame(planks.width, planks.height, border=5)
    b = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    b.alpha_composite(planks, (border, border))
    b.alpha_composite(frame)
    big = b.resize((b.width * SIGN_SCALE, b.height * SIGN_SCALE), Image.NEAREST)
    x = (big.width - text.width) // 2
    y = (big.height - text.height) // 2
    shadow = Image.new("RGBA", text.size, SHADOW)
    shadow.putalpha(text.getchannel("A"))
    big.alpha_composite(shadow, (x + SIGN_SCALE, y + SIGN_SCALE))
    big.alpha_composite(text, (x, y))
    big.save(out_png)
    return big


def log_frame(inner_w, inner_h, border=8):
    """A frame of logs around an inner_w x inner_h (1x) hole, with square
    caps at the corners like the Ridgeside-style mod banners."""
    w, h = inner_w + 2 * border, inner_h + 2 * border
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for x in range(w):
        for y in range(h):
            inside = border <= x < w - border and border <= y < h - border
            if inside:
                continue
            horizontal = y < border or y >= h - border
            t = (y if y < border else h - 1 - y) if horizontal else (x if x < border else w - 1 - x)
            key = "o" if t in (0, border - 1) else "H" if t == 1 else "M" if t == 2 else "R" if t == 3 else "D"
            if horizontal and t not in (0, border - 1) and (x * 5 + t) % 17 == 0:
                key = "D"
            if not horizontal and t not in (0, border - 1) and (y * 5 + t) % 17 == 0:
                key = "D"
            px(im, x, y, key)
    cap = border + 2
    for cx, cy in ((0, 0), (w - cap, 0), (0, h - cap), (w - cap, h - cap)):
        for x in range(cx, cx + cap):
            for y in range(cy, cy + cap):
                rim = x in (cx, cx + cap - 1) or y in (cy, cy + cap - 1)
                px(im, x, y, "o" if rim else "L" if (x - cx) in (1,) or (y - cy) in (1,) else "M")
        px(im, cx + cap // 2, cy + cap // 2, "o")
    return im, border


def banner(media, out_png, panels):
    """Three screenshots, each cropped to a 400x380 window, side by side."""
    pw, ph = 400, 380
    shots = []
    for name, cx, cy in panels:
        im = Image.open(os.path.join(media, "screens", name)).convert("RGBA")
        x0 = max(0, min(im.width - pw, cx - pw // 2))
        y0 = max(0, min(im.height - ph, cy - ph // 2))
        shots.append(im.crop((x0, y0, x0 + pw, y0 + ph)))
    gap = 8
    inner_w, inner_h = 3 * pw + 2 * gap, ph
    # The frame is drawn at 1x and scaled to the game's 4x.
    f, border = log_frame(inner_w // FRAME_SCALE, inner_h // FRAME_SCALE)
    frame = f.resize((f.width * FRAME_SCALE, f.height * FRAME_SCALE), Image.NEAREST)
    canvas = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    inset = border * FRAME_SCALE
    for i, shot in enumerate(shots):
        canvas.alpha_composite(shot, (inset + i * (pw + gap), inset))
    # Thin log dividers between the panels.
    for i in (1, 2):
        x = inset + i * (pw + gap) - gap
        for y in range(inset, inset + inner_h):
            for dx in range(gap):
                key = "o" if dx in (0, gap - 1) else "M" if dx < gap // 2 else "D"
                canvas.putpixel((x + dx, y), WOOD[key] + (255,))
    canvas.alpha_composite(frame)
    canvas.save(out_png)
    return canvas


def outlined(text, scale, fill_top, fill_bottom, outline=(46, 26, 16, 255), width=2):
    """The rendered heading scaled up (nearest), filled with a vertical
    gradient, ringed with a dark outline `width` font-pixels thick and a
    drop shadow, the way the game's own title lettering sits on art."""
    text = text.crop(text.getbbox())
    text = text.resize((text.width * scale, text.height * scale), Image.NEAREST)
    alpha = text.getchannel("A").point(lambda a: 255 if a > 128 else 0)
    w, h = text.size
    fill = Image.new("RGBA", (w, h))
    for y in range(h):
        t = y / max(1, h - 1)
        c = tuple(int(fill_top[i] + (fill_bottom[i] - fill_top[i]) * t) for i in range(3)) + (255,)
        for x in range(w):
            fill.putpixel((x, y), c)
    fill.putalpha(alpha)
    pad = width * scale * 2
    out = Image.new("RGBA", (w + 2 * pad, h + 2 * pad), (0, 0, 0, 0))
    ring = Image.new("RGBA", (w, h), outline)
    ring.putalpha(alpha)
    step = max(1, scale)
    r = width * scale
    for dx in range(-r, r + 1, step):
        for dy in range(-r, r + 1, step):
            out.alpha_composite(ring, (pad + dx, pad + dy))
    shadow = Image.new("RGBA", (w, h), (20, 12, 8, 200))
    shadow.putalpha(alpha)
    base = Image.new("RGBA", out.size, (0, 0, 0, 0))
    base.alpha_composite(shadow, (pad + r + step * 2, pad + r + step * 2))
    base.alpha_composite(out)
    base.alpha_composite(fill, (pad, pad))
    return base


def title_card(media, out_png, title_png, subtitle_png):
    """The banner, with the mod's name across the top of all three panels."""
    card = Image.open(os.path.join(media, "page", "banner.png")).convert("RGBA")
    title = outlined(Image.open(title_png).convert("RGBA"), 2, (255, 246, 214), (246, 190, 82), width=2)
    sub = outlined(Image.open(subtitle_png).convert("RGBA"), 1, (255, 250, 236), (238, 222, 188), width=2)
    # A soft dark band behind the words, so they read over grass, floor and canvas alike.
    band_h = title.height + sub.height + 18
    top = 44
    band = Image.new("RGBA", (card.width, band_h), (0, 0, 0, 0))
    for y in range(band_h):
        edge = min(y, band_h - 1 - y) / (band_h * 0.35)
        a = int(110 * min(1.0, edge))
        for x in range(card.width):
            band.putpixel((x, y), (30, 18, 12, a))
    card.alpha_composite(band, (0, top))
    card.alpha_composite(title, ((card.width - title.width) // 2, top))
    card.alpha_composite(sub, ((card.width - sub.width) // 2, top + title.height + 2))
    card.save(out_png)
    return card


def main():
    media = sys.argv[1]
    out = os.path.join(media, "page")
    os.makedirs(out, exist_ok=True)
    for f in sorted(os.listdir(os.path.join(media, "signtext"))):
        if f.startswith("sign_a_painting_mod"):        # the title card's subtitle, not a heading
            continue
        sign(os.path.join(media, "signtext", f), os.path.join(out, f.replace("sign_", "header_")))
    banner(media, os.path.join(out, "banner.png"), [
        ("shop_exterior.png", 610, 300),
        ("event_gallery_1.png", 600, 380),
        ("setup_portrait.png", 1030, 300),
    ])
    title_card(media, os.path.join(out, "title_card.png"),
               os.path.join(media, "signtext", "sign_pelican_palette.png"),
               os.path.join(media, "signtext", "sign_a_painting_mod_for_stardew_valley.png"))


if __name__ == "__main__":
    main()
