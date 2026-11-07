"""Paints a feature into the Gilded Vale's map with PixelLab inpaint, in the game's
own style: a strip of a real vanilla map (default: Pelican Town's park) is put above
the Vale's render in the picture it paints into, so the model imitates the game's
pieces it can see (the user, 2026-11-06: painted features must "feel more like
vanilla"; this is how the Lost Parterre was made).

Usage: python tools/art/vale_paint_feature.py <vale render.png> <region x0 y0> <mask x0 y0 x1 y1>
          <prompt file> <out dir> <name> [--context strip.png] [seeds...]
  The render is the Vale map from tile (x0, y0), at most 32 x 20 tiles; the mask is
  in map tiles, inclusive. Each result is saved whole and as <name>_s<seed>_<n>.png,
  the Vale part only (crop the mask box from it to lay it: build_vale.py FEATURES).
"""
import io, os, sys, time, json, base64, urllib.error
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vale_stairs import b64, call, images_in


def main():
    args = sys.argv[1:]
    context = None
    if "--context" in args:
        i = args.index("--context"); context = args[i + 1]; del args[i:i + 2]
    render, rx, ry, mx0, my0, mx1, my1, prompt, out, name, *seeds = args
    rx, ry, mx0, my0, mx1, my1 = map(int, (rx, ry, mx0, my0, mx1, my1))
    seeds = [int(s) for s in seeds] or [1, 2, 3]
    vale = Image.open(render).convert("RGB")
    strip = Image.open(context).convert("RGB") if context else None
    top = strip.height if strip else 0
    canvas = Image.new("RGB", (512, top + vale.height))
    if strip:
        canvas.paste(strip.crop((0, 0, 512, strip.height)), (0, 0))
    canvas.paste(vale, (0, top))
    mask = Image.new("RGB", canvas.size)
    ImageDraw.Draw(mask).rectangle(((mx0 - rx) * 16, top + (my0 - ry) * 16, (mx1 - rx + 1) * 16 - 1, top + (my1 - ry + 1) * 16 - 1), fill=(255, 255, 255))
    assert canvas.width <= 512 and canvas.height <= 512, canvas.size
    os.makedirs(out, exist_ok=True)
    desc = open(prompt, encoding="utf-8").read().strip()
    size = {"width": canvas.width, "height": canvas.height}
    jobs = {}
    for seed in seeds:
        body = {"description": desc, "seed": seed, "crop_to_mask": True, "no_background": False,
                "inpainting_image": {"image": {"base64": b64(canvas)}, "size": size},
                "mask_image": {"image": {"base64": b64(mask)}, "size": size}}
        for _ in range(30):
            try:
                jobs[seed] = call("POST", "/inpaint-v3", body)["background_job_id"]
                break
            except urllib.error.HTTPError as e:
                if e.code != 429:
                    print("failed", seed, e.code, e.read()[:300], flush=True)
                    break
                time.sleep(20)
    while jobs:
        time.sleep(12)
        for seed, jid in list(jobs.items()):
            try:
                r = call("GET", "/background-jobs/" + jid)
            except Exception as e:
                print("poll", e, flush=True)
                continue
            if r["status"] == "completed":
                for n, b in enumerate(images_in(r.get("last_response"), [])):
                    im = Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA")
                    im.save(os.path.join(out, f"{name}_s{seed}_{n}_whole.png"))
                    im.crop((0, top, im.width, im.height)).save(os.path.join(out, f"{name}_s{seed}_{n}.png"))
                print("done", name, seed, flush=True)
                del jobs[seed]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", name, seed, json.dumps(r)[:200], flush=True)
                del jobs[seed]


if __name__ == "__main__":
    main()
