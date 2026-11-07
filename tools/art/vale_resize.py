"""Rescales a picked building with PixelLab's pixel-art resize (POST /v2/resize), so
every door in the Vale is the game's door size (the user, 2026-11-06: "make sure all
of their doors are proportional"; the game's single doors are about 20 x 33 px with
their frames, its double doors about 33 px tall). A plain resample smears pixel art at
factors like 0.8; the endpoint redraws it on the new grid.

Usage: python tools/art/vale_resize.py <art.png> <factor> <description> <out dir> <name> [seeds...]
"""
import io, os, sys, time, json, base64, urllib.error
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vale_stairs import b64, call, images_in


def main():
    src, factor, description, out, name = sys.argv[1], float(sys.argv[2]), sys.argv[3], sys.argv[4], sys.argv[5]
    seeds = [int(s) for s in sys.argv[6:]] or [1, 2]
    strength = float(os.environ.get("VALE_INIT_STRENGTH", "0"))   # >0: start from a plain shrink of the art, to keep its design
    os.makedirs(out, exist_ok=True)
    im = Image.open(src).convert("RGBA")
    im = im.crop(im.getbbox())
    tw, th = round(im.width * factor), round(im.height * factor)
    for seed in seeds:
        body = {"description": description, "reference_image": {"base64": b64(im).split(",", 1)[1]},
                "reference_image_size": {"width": im.width, "height": im.height},
                "target_size": {"width": tw, "height": th}, "no_background": True, "seed": seed,
                "color_image": {"base64": b64(im).split(",", 1)[1]}}
        if strength:
            # the endpoint can drop features (it lost the atelier's skylight); starting
            # from the art plainly resampled to the new size keeps them in place
            body["init_image"] = {"base64": b64(im.resize((tw, th), Image.LANCZOS)).split(",", 1)[1]}
            body["init_image_strength"] = strength
        for _ in range(40):
            try:
                r = call("POST", "/resize", body)
                break
            except urllib.error.HTTPError as e:
                if e.code != 429:
                    print("failed", name, seed, e.code, e.read()[:400], flush=True)
                    r = None
                    break
                time.sleep(20)
        if r and "background_job_id" in r:
            jid = r["background_job_id"]
            while True:
                time.sleep(10)
                r = call("GET", "/background-jobs/" + jid)
                if r["status"] not in ("processing", "queued", "pending"):
                    break
            r = r.get("last_response") if r["status"] == "completed" else None
        found = images_in(r, []) if r else []
        for n, b in enumerate(found):
            res = Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA")
            res.save(os.path.join(out, f"{name}{'_i%d' % strength if strength else ''}_s{seed}_{n}.png"))
            print("done", name, seed, res.size, flush=True)
        if not found:
            print("no image", name, seed, json.dumps(r)[:300] if r else "", flush=True)


if __name__ == "__main__":
    main()
