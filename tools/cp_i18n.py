"""Player-facing text in the content pack lives in [CP] Pelican Palette/i18n/default.json,
and the data files point at it with {{i18n:key}} tokens. The generator scripts
use this to lift their text out as they build.

Each generator owns a set of key prefixes: saving drops that generator's old
keys and writes its new ones, leaving every other key as it was.
"""
import json, os, re


class Strings:
    def __init__(self, pack, prefixes):
        self.path = os.path.join(pack, "i18n", "default.json")
        self.data = {}
        if os.path.exists(self.path):
            with open(self.path, encoding="utf-8") as f:
                self.data = json.load(f)
        self.data = {k: v for k, v in self.data.items() if not k.startswith(tuple(prefixes))}

    def add(self, key, text):
        if key in self.data and self.data[key] != text:
            raise ValueError(f"{key} is already used for different text")
        self.data[key] = text
        return "{{i18n:" + key + "}}"

    def script(self, prefix, script):
        """Lifts the text out of an event script: every quoted line, and a
        quickQuestion's question and answers. Keys are <prefix>.01, .02... in
        script order."""
        n = 0

        def key():
            nonlocal n
            n += 1
            return f"{prefix}.{n:02}"

        def lift(m):
            if m.group(1) is not None:
                return '"' + (self.add(key(), m.group(1)) if m.group(1).strip() else m.group(1)) + '"'
            parts = m.group(2).split("#")
            return "quickQuestion " + "#".join(self.add(key(), p) if p.strip() else p for p in parts) + "(break)"

        return re.sub(r'"([^"]*)"|quickQuestion (.*?)\(break\)', lift, script)

    def save(self):
        os.makedirs(os.path.dirname(self.path), exist_ok=True)
        with open(self.path, "w", encoding="utf-8") as f:
            f.write(json.dumps(self.data, indent=2, ensure_ascii=False) + "\n")
