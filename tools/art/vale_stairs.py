"""New stairs for the Gilded Vale's cliffs, painted by PixelLab into the map itself
(the user, 2026-11-04: the game's own Town stairs, cut to fit our cliff, "are
getting worse"; use PixelLab).

A crop of the map with the cliff unbroken (build_vale.py with VALE_NO_STAIRS=1,
rendered by render_tmx.py) goes to PixelLab's Pro inpaint (inpaint-v3) with a
mask over the flight's 4 x 9 tiles, so the stairs are drawn to meet this cliff,
this path and this grass, in the game's style. Each candidate is saved whole and
as the masked piece alone (art/gilded_vale/stairs/).

Usage: python tools/art/vale_stairs.py <bare crop.png> <mask x> <mask y> <out dir> [seeds...]
  mask x, y: the flight's top-left tile within the crop
"""
import base64, io, json, os, sys, time, urllib.error, urllib.request
from PIL import Image

API = "https://api.pixellab.ai/v2"
KEY = os.environ["PIXELLAB_SECRET"]
W_TILES, H_TILES = 4, 9

DESCRIPTION = (
    "A flight of stone stairs cut straight down through the rocky cliff, in Stardew Valley pixel art, "
    "high top-down front view like the game's town stairs: worn pale stone steps in even rows from the "
    "dirt path at the top to the dirt path at the bottom, with a low stone side wall on each side that "
    "joins the cliff's rocks, the cliff's grassy edge curling neatly round the top of the walls. Same "
    "palette, outline and shading as the cliff and grass around it. No railings, no plants on the steps.")


def b64(im):
    buf = io.BytesIO()
    im.save(buf, "PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


def call(method, path, body=None):
    req = urllib.request.Request(API + path, method=method, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Authorization": "Bearer " + KEY, "Content-Type": "application/json"})
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


def main():
    crop, mx, my, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    seeds = [int(s) for s in sys.argv[5:]] or [1, 2, 3]
    os.makedirs(out, exist_ok=True)
    im = Image.open(crop).convert("RGB")
    mask = Image.new("RGB", im.size, (0, 0, 0))
    box = (mx * 16, my * 16, (mx + W_TILES) * 16, (my + H_TILES) * 16)
    mask.paste((255, 255, 255), box)
    size = {"width": im.width, "height": im.height}
    jobs = {}
    for seed in seeds:
        body = {"description": DESCRIPTION, "seed": seed, "crop_to_mask": True, "no_background": False,
                "inpainting_image": {"image": {"base64": b64(im)}, "size": size},
                "mask_image": {"image": {"base64": b64(mask)}, "size": size}}
        for _ in range(60):
            try:
                r = call("POST", "/inpaint-v3", body)
                break
            except urllib.error.HTTPError as e:
                if e.code != 429:
                    print("failed", seed, e.code, e.read()[:300])
                    r = None
                    break
                time.sleep(20)
        if r:
            jobs[seed] = r.get("background_job_id")
            print("queued", seed, flush=True)
    while jobs:
        time.sleep(15)
        for seed, jid in list(jobs.items()):
            r = call("GET", "/background-jobs/" + jid)
            if r["status"] == "completed":
                for n, b in enumerate(images_in(r.get("last_response"), [])):
                    whole = Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA")
                    whole.save(os.path.join(out, f"stairs_s{seed}_{n}_whole.png"))
                    whole.crop(box).save(os.path.join(out, f"stairs_s{seed}_{n}.png"))
                print("done", seed, flush=True)
                del jobs[seed]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", seed, json.dumps(r)[:300], flush=True)
                del jobs[seed]


if __name__ == "__main__":
    main()
