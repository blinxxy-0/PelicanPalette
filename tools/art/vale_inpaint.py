"""Paints a new feature into the Gilded Vale's map with PixelLab's Pro inpaint
(inpaint-v3), the way the stairs were made (tools/art/vale_stairs.py): a render of
the map around the spot, a mask over the feature's tiles, so it's drawn to meet
the real cliff, grass and paths in the game's style (docs/terrain_painting.md).

Usage: python tools/art/vale_inpaint.py <render.png> <crop x0> <crop y0> <mask x0> <mask y0> <mask x1> <mask y1>
                                         <description file> <out dir> <name> [seeds...]
  crop x0, y0: the render's top-left, in map tiles; the mask box is in map tiles, inclusive.
"""
import os, sys, time, json, io, base64, urllib.error
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vale_stairs import b64, call, images_in


def main():
    render = sys.argv[1]
    cx0, cy0, mx0, my0, mx1, my1 = (int(v) for v in sys.argv[2:8])
    description = open(sys.argv[8], encoding="utf-8").read().strip()
    out, name = sys.argv[9], sys.argv[10]
    seeds = [int(s) for s in sys.argv[11:]] or [1, 2]
    os.makedirs(out, exist_ok=True)
    im = Image.open(render).convert("RGB")
    mask = Image.new("RGB", im.size, (0, 0, 0))
    box = ((mx0 - cx0) * 16, (my0 - cy0) * 16, (mx1 - cx0 + 1) * 16, (my1 - cy0 + 1) * 16)
    mask.paste((255, 255, 255), box)
    size = {"width": im.width, "height": im.height}
    jobs = {}
    for seed in seeds:
        body = {"description": description, "seed": seed, "crop_to_mask": True, "no_background": False,
                "inpainting_image": {"image": {"base64": b64(im)}, "size": size},
                "mask_image": {"image": {"base64": b64(mask)}, "size": size}}
        for _ in range(60):
            try:
                r = call("POST", "/inpaint-v3", body)
                break
            except urllib.error.HTTPError as e:
                if e.code != 429:
                    print("failed", seed, e.code, e.read()[:300], flush=True)
                    r = None
                    break
                time.sleep(20)
        if r:
            jobs[seed] = r["background_job_id"]
            print("queued", name, seed, flush=True)
    while jobs:
        time.sleep(15)
        for seed, jid in list(jobs.items()):
            r = call("GET", "/background-jobs/" + jid)
            if r["status"] == "completed":
                for n, b in enumerate(images_in(r.get("last_response"), [])):
                    whole = Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA")
                    whole.save(os.path.join(out, f"{name}_s{seed}_{n}_whole.png"))
                    whole.crop(box).save(os.path.join(out, f"{name}_s{seed}_{n}.png"))
                print("done", name, seed, flush=True)
                del jobs[seed]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", name, seed, json.dumps(r)[:300], flush=True)
                del jobs[seed]


if __name__ == "__main__":
    main()
