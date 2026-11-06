"""Builds the Nexus page's upload kit from release/nexus/description.bbcode
and the README's Images tab list:

  release/nexus/nexus_description_images/ (and .zip): every image the
      description names, numbered in the order it appears, to upload first
      and link in place of each {{name.png}}.
  release/nexus/nexus_gallery_images/ (and .zip): the Images tab, in the
      README's suggested order.
  release/nexus/preview.html: the description as the page will lay it out,
      with the images inline, to read before pasting.

Usage: nexus_kit.py   (from the repository root)
"""
import base64, html, os, re, shutil, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NEXUS = os.path.join(ROOT, "release", "nexus")
MEDIA = os.path.join(ROOT, "docs", "media")


def find(name):
    for sub in ("page", "screens"):
        path = os.path.join(MEDIA, sub, name)
        if os.path.exists(path):
            return path
    raise FileNotFoundError(name)


def fill(folder, names):
    out = os.path.join(NEXUS, folder)
    shutil.rmtree(out, ignore_errors=True)
    os.makedirs(out)
    for n, name in enumerate(names, 1):
        shutil.copy(find(name), os.path.join(out, f"{n:02d}_{name}"))
    with zipfile.ZipFile(out + ".zip", "w", zipfile.ZIP_DEFLATED) as z:
        for f in sorted(os.listdir(out)):
            z.write(os.path.join(out, f), f)
    print(f"{folder}: {len(names)} images")


def preview(bbcode):
    def img(m):
        data = base64.b64encode(open(find(m.group(1)), "rb").read()).decode()
        return f'<img src="data:image/png;base64,{data}" style="max-width:100%">'
    t = html.escape(bbcode, quote=False)
    t = re.sub(r"\[img\]\{\{([^}]+)\}\}\[/img\]", img, t)
    t = re.sub(r"\[url=([^\]]+)\](.*?)\[/url\]", r'<a href="\1">\2</a>', t)
    t = re.sub(r"\[size=(\d)\]", lambda m: f'<span style="font-size:{ {"2": "12px", "3": "15px", "4": "19px"}[m.group(1)] }">', t)
    t = t.replace("[/size]", "</span>")
    for tag, h in (("b", "b"), ("i", "i")):
        t = t.replace(f"[{tag}]", f"<{h}>").replace(f"[/{tag}]", f"</{h}>")
    t = t.replace("[center]", '<div style="text-align:center">').replace("[/center]", "</div>")
    t = t.replace("[quote]", '<blockquote>').replace("[/quote]", "</blockquote>")
    t = t.replace("\n", "<br>\n")
    return f"""<!doctype html><html><head><meta charset="utf-8"><title>Nexus page preview</title>
<style>body{{background:#2a2a2a;color:#d8d8d8;font-family:Roboto,Arial,sans-serif;max-width:900px;margin:24px auto;padding:0 16px;line-height:1.5}}
blockquote{{background:#333;border-left:4px solid #c87d3a;margin:12px auto;padding:10px 16px;text-align:left;max-width:760px}}a{{color:#e09a52}}</style>
</head><body>{t}</body></html>"""


def main():
    bbcode = open(os.path.join(NEXUS, "description.bbcode"), encoding="utf-8").read()
    names = []
    for name in re.findall(r"\{\{([^}]+)\}\}", bbcode):
        if name not in names:
            names.append(name)
    fill("nexus_description_images", names)
    readme = open(os.path.join(NEXUS, "README.md"), encoding="utf-8").read()
    gallery = re.findall(r"^\d+\. `([^`]+\.png)`", readme, re.M)
    fill("nexus_gallery_images", gallery)
    with open(os.path.join(NEXUS, "preview.html"), "w", encoding="utf-8") as f:
        f.write(preview(bbcode))
    print("wrote preview.html")


if __name__ == "__main__":
    main()
