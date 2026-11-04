"""The Gilded Vale's shore corners, painted by PixelLab into the map (2026-11-04).

The game's own rounded bank corners are one-off drawings, never repeated (each of
the Mountain pond's corner pieces is used once in all the vanilla maps), so the
corner autotiling in build_vale.py can only make square notches or steps there
(the user: corners "flat before the jagged edges"). So each corner is inpainted:
a spring render of the map (render_tmx.py) with the corner masked goes to
PixelLab's Pro inpaint (inpaint-v3), which draws a rounded rocky corner joining
the straight banks either side, in the game's style.

Then each painted corner is recoloured for summer, fall and winter by the game's
own palette: every pixel of spring_outdoorsTileSheet is compared with the same
pixel of the summer, fall and winter sheets, and each spring colour maps to the
colour it most often becomes (a colour not on the sheet takes its nearest spring
colour's change). build_vale.py lays them on a seasonal sheet
(spring_GildedValeShore.png and its siblings; the game swaps any sheet named
spring_*).

Usage:
  vale_shore.py paint <spring render.png> <out dir> [region names...]
  vale_shore.py seasons <exports dir> <spring sheet dir> <out dir>   (writes the season palette maps)
"""
import base64, collections, io, json, os, sys, time, urllib.error, urllib.request
from PIL import Image

API = "https://api.pixellab.ai/v2"

# Each corner: the masked box in map tiles (x0, y0, x1, y1, inclusive), and which way
# the bank turns, for the prompt. Merged from the corners build_vale.py's terrain has
# (water with land on two sides); those under the bridges and falls are left out.
REGIONS = {
    "lake_nw": ((49, 55, 57, 64), "the lake's top-left corner: the shore comes along the top and turns down the left side"),
    "lake_ne": ((73, 55, 83, 64), "the lake's top-right corner: the shore comes along the top and turns down the right side"),
    "lake_sw": ((49, 69, 53, 72), "the lake's bottom-left corner: the left shore turns into the flat bottom shore"),
    "lake_se": ((80, 69, 84, 72), "the lake's bottom-right corner: the right shore turns into the flat bottom shore"),
    "river_bend": ((58, 6, 65, 13), "where the river, flowing along from the right, turns the corner and flows down"),
    "river_inner": ((76, 4, 85, 13), "the inner bend where the river comes down from the top and turns to flow left"),
    "river_mouth": ((57, 54, 67, 57), "where the river opens out into the lake below"),
}
CONTEXT = 3          # tiles of map round each mask, so the painting meets what's there

DESCRIPTION = ("Stardew Valley pixel art map, high top-down view: {where}. Paint the bank as one "
               "smoothly rounded curve of the same jagged brown rocky bank edge and sand as the "
               "straight banks either side, joining them seamlessly; no square notches, no steps, "
               "no hard straight corners. Same water, sand, rock and grass colours and texture as "
               "the surrounding map. No new objects, plants or props.")


def b64(im):
    buf = io.BytesIO()
    im.save(buf, "PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


def call(method, path, body=None):
    req = urllib.request.Request(API + path, method=method, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Authorization": "Bearer " + os.environ["PIXELLAB_SECRET"],
                                          "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read())


def images_in(v, out):
    if isinstance(v, dict):
        if isinstance(v.get("base64"), str):
            out.append(v["base64"])
        else:
            for x in v.values():
                images_in(x, out)
    elif isinstance(v, list):
        for x in v:
            images_in(x, out)
    return out


def crop_box(region, map_w=96, map_h=76):
    x0, y0, x1, y1 = region
    cx0, cy0 = max(0, x0 - CONTEXT), max(0, y0 - CONTEXT)
    cx1, cy1 = min(map_w - 1, x1 + CONTEXT), min(map_h - 1, y1 + CONTEXT)
    return cx0, cy0, cx1, cy1


def paint(render, out, names, seeds=(1, 2)):
    os.makedirs(out, exist_ok=True)
    full = Image.open(render).convert("RGB")
    jobs = {}
    for name in names or REGIONS:
        region, where = REGIONS[name]
        cx0, cy0, cx1, cy1 = crop_box(region)
        im = full.crop((cx0 * 16, cy0 * 16, (cx1 + 1) * 16, (cy1 + 1) * 16))
        mask = Image.new("RGB", im.size, (0, 0, 0))
        x0, y0, x1, y1 = region
        mask.paste((255, 255, 255), ((x0 - cx0) * 16, (y0 - cy0) * 16, (x1 - cx0 + 1) * 16, (y1 - cy0 + 1) * 16))
        im.save(os.path.join(out, f"{name}_before.png"))
        size = {"width": im.width, "height": im.height}
        for seed in seeds:
            body = {"description": DESCRIPTION.format(where=where), "seed": seed, "crop_to_mask": True,
                    "no_background": False, "inpainting_image": {"image": {"base64": b64(im)}, "size": size},
                    "mask_image": {"image": {"base64": b64(mask)}, "size": size}}
            for _ in range(60):
                try:
                    r = call("POST", "/inpaint-v3", body)
                    break
                except urllib.error.HTTPError as e:
                    if e.code != 429:
                        print("failed", name, seed, e.code, e.read()[:300], flush=True)
                        r = None
                        break
                    time.sleep(20)
            if r:
                jobs[(name, seed)] = r["background_job_id"]
                print("queued", name, seed, flush=True)
    while jobs:
        time.sleep(15)
        for (name, seed), jid in list(jobs.items()):
            r = call("GET", "/background-jobs/" + jid)
            if r["status"] == "completed":
                for n, b in enumerate(images_in(r.get("last_response"), [])):
                    Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA").save(
                        os.path.join(out, f"{name}_s{seed}_{n}.png"))
                print("done", name, seed, flush=True)
                del jobs[(name, seed)]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", name, seed, json.dumps(r)[:300], flush=True)
                del jobs[(name, seed)]


def season_map(spring_sheet, season_sheet):
    """spring colour -> the colour it most often becomes on the season's sheet."""
    a, b = Image.open(spring_sheet).convert("RGBA"), Image.open(season_sheet).convert("RGBA")
    pa, pb = a.load(), b.load()
    votes = collections.defaultdict(collections.Counter)
    for y in range(min(a.height, b.height)):
        for x in range(min(a.width, b.width)):
            if pa[x, y][3] == 255 and pb[x, y][3] == 255:
                votes[pa[x, y][:3]][pb[x, y][:3]] += 1
    return {c: v.most_common(1)[0][0] for c, v in votes.items()}


def recolour(im, cmap):
    """The image with each colour moved the way its nearest spring colour moves."""
    keys = list(cmap)
    cache = {}
    out = im.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            c = (r, g, b)
            if c not in cache:
                if c in cmap:
                    cache[c] = cmap[c]
                else:
                    k = min(keys, key=lambda q: (q[0] - r) ** 2 + (q[1] - g) ** 2 + (q[2] - b) ** 2)
                    t = cmap[k]
                    cache[c] = tuple(max(0, min(255, t[i] + c[i] - k[i])) for i in range(3))
            px[x, y] = cache[c] + (a,)
    return out


if __name__ == "__main__":
    if sys.argv[1] == "paint":
        paint(sys.argv[2], sys.argv[3], sys.argv[4:])
