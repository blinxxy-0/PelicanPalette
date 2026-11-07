"""Winter versions of the Vale's painted features (the user, 2026-11-07: recolouring
them by the game's colour tables made winter "extraordinarily messy", and flowers in
bloom under snow made no sense). Each feature's spring painting goes to PixelLab's
colour edit (edit-image-pixen, as the snow roofs were made) with a winter
description; build_vale.py then cuts the result against a render of the bare map in
winter, the same way spring is cut against the bare map in spring. The edit takes at
most 256 px a side, so bigger features go in strips and are stitched back.

Usage: python tools/art/vale_feature_winter.py [names...]
  writes art/gilded_vale/features/winter/<name>_<n>.png, the feature's box only.
"""
import io, os, sys, time, json, base64, urllib.error
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
from vale_stairs import b64, call, images_in
import build_vale as bv

DESC = ("The same Stardew Valley garden scene in deep winter, like the game's town in winter: the ground, grass and dirt paths "
        "under an even layer of white snow; snow resting on the tops of hedges, walls, benches, tables and statues; the "
        "flowers gone, beds bare under snow; hedges dark green with snow on top; no icicles. Wood, stone, glass, pots and tools "
        "keep their colours. Keep every shape, outline and position exactly the same: only the colours change.")


def box_of(name):
    art, box, left, top, *rest = bv.FEATURES[name]
    if isinstance(box, str):
        _, before, rx, ry = box.split(":")
        x1, y1 = rest[:2]
        box = ((left - int(rx)) * 16, (top - int(ry)) * 16, (x1 - int(rx) + 1) * 16, (y1 - int(ry) + 1) * 16)
    return art, box


def submit(key, im, jobs):
    for _ in range(30):
        try:
            r = call("POST", "/edit-image-pixen", {"image": {"base64": b64(im).split(",", 1)[1]}, "description": DESC[:500],
                                                  "seed": int(os.environ.get("VALE_SEED", "3")), "no_background": False})
            jobs[key] = r["background_job_id"]
            return
        except urllib.error.HTTPError as e:
            if e.code != 429:
                print("failed", key, e.code, e.read()[:300], flush=True)
                return
            time.sleep(20)


def main():
    names = sys.argv[1:] or list(bv.FEATURES)
    out = os.path.join(bv.ART, "features", "winter")
    os.makedirs(out, exist_ok=True)
    jobs, parts_of, results = {}, {}, {}
    for name in names:
        art, box = box_of(name)
        whole = Image.open(os.path.join(bv.ART, art + ".png")).convert("RGB").crop(box)
        nx, ny = -(-whole.width // 256), -(-whole.height // 256)
        sw, sh = -(-whole.width // nx // 16) * 16, -(-whole.height // ny // 16) * 16
        parts = []
        for j in range(ny):
            for i in range(nx):
                at = (i * sw, j * sh)
                parts.append(at)
                submit(f"{name}#{len(parts) - 1}", whole.crop((at[0], at[1], min(whole.width, at[0] + sw), min(whole.height, at[1] + sh))), jobs)
        parts_of[name] = (whole.size, parts)
    while jobs:
        time.sleep(12)
        for key, jid in list(jobs.items()):
            try:
                r = call("GET", "/background-jobs/" + jid)
            except Exception as e:
                print("poll", e, flush=True)
                continue
            if r["status"] == "completed":
                results[key] = [Image.open(io.BytesIO(base64.b64decode(b.split(",", 1)[-1]))).convert("RGBA")
                                for b in images_in(r.get("last_response"), [])]
                print("done", key, len(results[key]), flush=True)
                del jobs[key]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", key, json.dumps(r)[:300], flush=True)
                del jobs[key]
    for name, (size, parts) in parts_of.items():
        keys = [f"{name}#{k}" for k in range(len(parts))]
        if not all(k in results and results[k] for k in keys):
            print("incomplete", name)
            continue
        for n in range(min(len(results[k]) for k in keys)):
            im = Image.new("RGBA", size)
            for k, at in zip(keys, parts):
                im.paste(results[k][n], at)
            im.save(os.path.join(out, f"{name}_{n}.png"))
        print("saved", name)


if __name__ == "__main__":
    main()
