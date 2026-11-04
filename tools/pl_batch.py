"""Runs a PixelLab batch spec (art/<npc>/*_batch.json): every job through
generate-image-v2, candidates saved to <out>/<batch>/<job>_<n>.png."""
import base64, io, json, os, sys, time, urllib.error, urllib.request
from PIL import Image

API = "https://api.pixellab.ai/v2"
KEY = os.environ["PIXELLAB_SECRET"]


def call(method, path, body=None):
    req = urllib.request.Request(API + path, method=method, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Authorization": "Bearer " + KEY, "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def post(path, body):
    """POST, waiting while PixelLab has too many of our jobs running."""
    for _ in range(80):
        try:
            return call("POST", path, body)
        except urllib.error.HTTPError as e:
            if e.code != 429: raise
            time.sleep(20)
    raise RuntimeError("still busy")


def ref(path, usage):
    im = Image.open(path)
    buf = io.BytesIO(); im.save(buf, "PNG")
    return {"image": {"type": "base64", "base64": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()},
            "size": {"width": im.width, "height": im.height}, "usage_description": usage[:500]}


def images_of(resp, out):
    found = []
    def walk(v):
        if isinstance(v, dict):
            if "base64" in v and isinstance(v["base64"], str): found.append(v["base64"])
            else: [walk(x) for x in v.values()]
        elif isinstance(v, list): [walk(x) for x in v]
    walk(resp)
    return found


def main():
    spec, out = json.load(open(sys.argv[1])), sys.argv[2]
    only = set(sys.argv[3:])
    folder = os.path.join(out, spec["batch"]); os.makedirs(folder, exist_ok=True)
    pending = {}
    for name, job in spec["jobs"].items():
        if only and name not in only: continue
        j = {**spec["common"], **job}
        if j.get("endpoint") == "edit-image-pixen":
            r = post("/edit-image-pixen", {"image": ref(j["image"], "x")["image"], "description": j["description"][:500],
                                                    "seed": j.get("seed", 1), "no_background": True})
            pending[name] = r["background_job_id"]; print("queued", name, flush=True); time.sleep(2)
            continue
        body = {"description": j["description"], "image_size": j["image_size"], "no_background": j.get("no_background", True)}
        if "style_image" in j: body["style_image"] = ref(j["style_image"], j.get("style_usage", ""))
        if "reference_image" in j: body["reference_images"] = [ref(j["reference_image"], j.get("reference_usage", ""))]
        if "reference_images" in j: body["reference_images"] = [ref(p, u) for p, u in j["reference_images"]]
        if "seed" in j: body["seed"] = j["seed"]
        r = post("/generate-image-v2", body)
        pending[name] = r["background_job_id"]; print("queued", name, flush=True)
        time.sleep(2)
    while pending:
        time.sleep(15)
        for name, jid in list(pending.items()):
            r = call("GET", "/background-jobs/" + jid)
            if r["status"] == "completed":
                imgs = images_of(r.get("last_response"), folder)
                for n, b in enumerate(imgs):
                    data = base64.b64decode(b.split(",", 1)[-1])
                    open(os.path.join(folder, f"{name}_{n}.png"), "wb").write(data)
                print("done", name, len(imgs), flush=True); del pending[name]
            elif r["status"] not in ("processing", "queued", "pending"):
                print("failed", name, json.dumps(r)[:300], flush=True); del pending[name]


if __name__ == "__main__":
    main()
