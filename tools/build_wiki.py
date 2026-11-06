"""Builds the Pelican Palette wiki (docs/wiki/), in the style of the Stardew
Valley wiki: an infobox on each item or character page, tables with item
icons, and a sidebar. The tables come from docs/media/wikidata.json (written
by `museart_wikidata` in a debug game), so prices, recipes and levels match
the shipping mod. The prose is here.

The pages are GitHub wiki pages: push docs/wiki/ to the repository's
<repo>.wiki.git and they appear under the repository's Wiki tab.

Usage: build_wiki.py   (from the repository root)
"""
import json
import os
import re
import shutil
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MEDIA = os.path.join(ROOT, "docs", "media")
WIKI = os.path.join(ROOT, "docs", "wiki")
DATA = json.load(open(os.path.join(MEDIA, "wikidata.json"), encoding="utf-8"))
CHANGELOG = open(os.path.join(ROOT, "CHANGELOG.md"), encoding="utf-8").read()

PAGES = {}


def page(name, text):
    PAGES[name] = text.strip() + "\n"


def slug(name):
    return name.replace(" ", "-")


def link(name, label=None):
    return f"[{label or name}]({slug(name)})"


def icon_file(qualified_id):
    s = "".join(c if c.isalnum() or c in "_." else "-" for c in qualified_id).strip("-")
    return s + ".png"


# Width only: GitHub caps an image's width to its table column but keeps a
# set height, which squashed icons in narrow columns into slivers.
def icon(qualified_id, size=32):
    return f'<img src="images/icons/{icon_file(qualified_id)}" width="{size}">'


def swatch(hex_colour):
    return f'<img src="images/swatches/{hex_colour[1:]}.png" width="24">'


def item_by_name(name):
    for group in ("objects", "bigCraftables"):
        for item in DATA[group]:
            if item["name"] == name:
                return item
    return None


def gold(n):
    return f"{n:,}g"


def level_text(condition):
    m = re.search(r"PAINTING_LEVEL Current (\d+)", condition or "")
    return f"Painting level {m.group(1)}" if m else "-"


def ingredients(recipe):
    return "<br>".join(f'{icon(i["id"], 24)} {i["name"]} ({i["count"]})' for i in recipe["ingredients"])


def infobox(title, image, rows, width=96):
    cells = "".join(f"<tr><td><b>{k}</b></td><td>{v}</td></tr>" for k, v in rows)
    return (f'<table align="right"><tr><th colspan="2">{title}</th></tr>'
            f'<tr><td colspan="2" align="center">{image}</td></tr>{cells}</table>\n')


def recipe_for(name):
    return next((r for r in DATA["recipes"] if r["item"] == name), None)


def stock_for(name):
    return next((s for s in DATA["shop"] if s["item"] == name), None)


def item_infobox(name, extra=()):
    item = item_by_name(name)
    r = recipe_for(name)
    s = stock_for(name)
    rows = [("Sell price", gold(item["sellPrice"]))] if item else []
    if r:
        rows.append(("Recipe", "Known from the start" if r["known"] else f'Painting level {r["level"]}'))
        rows.append(("Ingredients", ingredients(r)))
    if s:
        rows.append(("Art shop", f'{gold(s["price"])}' + ("" if not s["condition"] or "KNOWS" in s["condition"] else f' ({level_text(s["condition"])})')))
    rows.extend(extra)
    return infobox(name, icon(item["id"], 64) if item else "", rows)


# ---------------------------------------------------------------- pages

def home():
    page("Home", f"""
![Pelican Palette](images/page/title_card.png)

**Pelican Palette** is a mod for Stardew Valley that adds painting:
- a minigame as hands-on as fishing,
- a Painting skill,
- an art supply shop in Cindersap Forest,
- Sable, the former museum restorer who runs it,
- Margot Pell, the museum's conservator, and the valley's old paintings.

This wiki covers every item, recipe, price and mechanic. It's written for version {DATA["version"]}.

## Start here
- {link("Getting Started")}: your first easel, canvas and painting
- {link("Painting")}: how the minigame works
- {link("Sable")}: the art shop's owner, and a marriage candidate

## Painting
| | |
|---|---|
| {link("Painting")} | The easel, mixing, light, sparks and tools |
| {link("Canvases and Techniques")} | Canvas sizes, glazing, highlights and linen |
| {link("Quality and Masterpieces")} | How a painting's quality is decided |
| {link("Pigments and Paint")} | Every colour, how to learn it, and how to get paint |
| {link("Brushes")} | The five brushes and what they change |
| {link("Portraits and Plein Air")} | Painting friends, the view, creatures and animals |
| {link("Painting Skill")} | Levels, unlocks and professions |

## Paintings
| | |
|---|---|
| {link("Selling and Prices")} | Where to sell, and what a painting is worth |
| {link("Commissions")} | Villagers' requests on the art shop's board |
| {link("Consignment")} | Selling through Sable's gallery |
| {link("Painting Record")} | Every subject you've painted, and its milestones |
| {link("Varnishing")} | Curing paintings in a Varnishing Cabinet |
| {link("Gifting Paintings")} | Who likes what |
| {link("Portfolio and Back Room")} | The portfolio book and the plan chest |
| {link("Frames")} | Frame styles, what they do hung, and the Framer's Bench |
| {link("Old Paintings and Restoration")} | Damaged paintings, Painting Mastery, and restoring them |
| {link("The Museum")} | The collection, its rewards, and the Painting Wing |
| {link("Special Orders")} | The town board's painting orders, and Qi's |

## The shop and the rest
| | |
|---|---|
| {link("Art Shop")} | Location, hours and stock |
| {link("Crafting")} | Every recipe |
| {link("Sable")} | Schedule, gifts, heart events and marriage |
| {link("Margot Pell")} | The conservator: schedule, gifts, heart events and marriage |
| {link("Julian Vane")} | The critic who visits each season |
| {link("Festivals")} | The Fair's contest, the Night Market stall, and painting at festivals |
| {link("Multiplayer")} | Playing in co-op |
| {link("Configuration")} | The settings |
| {link("Compatibility")} | Other mods |
| {link("FAQ")} | Common questions |
| {link("Translating")} | Adding a language |
| {link("Changelog")} | Version history |
""")


def getting_started():
    easel = recipe_for("Easel")
    canvas = recipe_for("Small Canvas")
    page("Getting Started", f"""
On your second morning, a letter arrives from the new art shop in Cindersap Forest. It holds two **Small Canvases**.

You already know three recipes:

| | Item | Ingredients |
|---|---|---|
| {icon("(BC)blinxxy.MuseArt_Easel")} | **Easel** | {ingredients(easel)} |
| {icon("(O)blinxxy.MuseArt_Canvas_Small")} | **Small Canvas** | {ingredients(canvas)} |
| {icon("(O)blinxxy.MuseArt_Palette")} | **Wooden Palette** | {ingredients(recipe_for("Wooden Palette"))} |

## Your first painting
1. Craft an **Easel** and place it anywhere, indoors or out.
2. Make sure you have a **Small Canvas** and a **Palette** in your bag. Sable gives you a Palette on your first visit to the {link("Art Shop")}. (On Simplified mode, paint is free and you don't need one.)
3. Right-click the easel. Pick the canvas size, then something from your bag to paint.
4. Press **Begin** and copy the reference on the left of the screen. See {link("Painting")} for how mixing and the light work.
5. Finish, and the painting goes into your bag.

## What next
- **Visit the {link("Art Shop")}**, west of the path down from Marnie's ranch. Sable sells canvases, paint and brushes, and buys paintings for more than Pierre does.
- **Check the commission board** in the shop. Villagers ask for paintings of things they love; see {link("Commissions")}.
- **Level up** your {link("Painting Skill")}. It brings bigger canvases, new tools and higher quality.
- **Learn more colours** ({link("Pigments and Paint")}). The more colours you know, the closer your paintings get to the real thing, and the more they're worth.
""")


def painting():
    page("Painting", f"""
![The painting screen](images/screens/paint_glaze.png)

Right-click a placed **Easel** to paint. A session has three screens: **Setup**, **Painting** and **Results**.

## Setup
- **Canvas:** choose small, medium or large. Bigger canvases unlock with the {link("Painting Skill")}; see {link("Canvases and Techniques")}.
- **Subject:** choose what to paint:
  - one to three items from your bag (a still life),
  - a friend who will sit for a portrait,
  - the view, at an easel outdoors ({link("Portraits and Plein Air")}),
  - a monster you've slain, or one of your animals ({link("Portraits and Plein Air")}),
  - a damaged old painting to restore, once you're a conservator ({link("Old Paintings and Restoration")}),
  - or a **Free canvas**, with no reference and no score.
- **Preview:** shows the reference exactly as you'll copy it, with its **difficulty** (0-100) and how long the light lasts.
- **Brush:** cycles through the {link("Brushes")} in your bag.
- **Frame a painting:** puts one of your paintings in a frame, to hang as furniture. See {link("Frames")}.
- **More to paint than fits:** the items and the row of friends, views and studies turn pages, with the arrows at the end of each row's label (or the mouse wheel).
- **Frames in this room:** at an easel you've placed, icons above the bottom buttons show what the room's framed paintings give the painting. Hover one to see how much. See {link("Frames")}.
- **Warnings:** under Begin, the screen tells you if you're out of canvases, if it's too late in the day (a painting can't run past 2am), or if your Palette is low on a colour the subject needs.

## Painting
The reference is pinned on the left; your canvas is in the middle; the palette and tools are on the right.

**Mixing.** Click a colour on the palette and a mixing bar opens, like fishing.
- The true colour drifts up and down the bar. Hold the mouse button to lift your mix, and let go to let it fall.
- Keep it on the true colour until the meter fills.
- A clean mix paints **pure** colour. A shaky one comes out **a bit off**, and a bad one **muddy** (visibly grimy, and worth less).
- Each colour moves in its own way: earth colours drift smoothly, minerals dart, and rare pigments are the hardest. See {link("Pigments and Paint")}.

**Painting.** Left-click or drag to paint with what's on the brush. A load covers a number of cells, set by your brush and level. Right-click scrapes paint off.

**The light.** A bar at the top runs down while you work. How long it lasts depends on how much there is to paint: every colour area's edge, every fill, every glaze and highlight. So a busy portrait gets far more light than a parsnip, and every painting asks about the same pace (a little quicker on Hard). Your Painting level, the Master's Easel and Light frames add more.
- When it's gone, the painting is finished as it stands.
- Finish early and the light that's left becomes bonus experience.
- The light pauses while you mix, except on Hard.

**Inspiration sparks.** Every so often a spark appears on a cell of the canvas: a gold ring round it, the cell glowing in the colour it wants. The same cell is ringed on the reference, its colour left in view.
- Paint that cell its right colour, mixed pure, before the spark fades.
- Each spark you catch adds light.
- Catch three and the painting is **Inspired**, which raises its quality.
- Very rarely, a spark teaches you a rare pigment.

**Paint.** Every cell you paint uses one cell of paint from your Palette (except on Simplified). See {link("Pigments and Paint")}.

**Finding your place.** Point at a cell of the canvas and the same cell is framed on the reference, and the other way round, so you always know which cell you're on. Under the reference, the colour that cell wants is named with its palette key (for example "Snow (6)"), so look-alike colours like Cream and Snow can't be mixed up.

**Zooming.** From a medium canvas, the **mouse wheel** over the canvas (or the reference) zooms in, up to 3x, towards the cell under the mouse. The reference zooms with it and always shows the same part of the picture. While zoomed, the **arrow keys** move round, scroll bars in the canvas's frame show where you are, and the wheel back out shows the whole picture again.

**The mode.** On the setup screen, click the mode button beside **Begin** ("Normal") to switch between Simplified, Relaxed, Normal and Hard; hover it to see what each changes. **Relaxed** is Normal with no time limit; **Simplified** also makes paint free and the picture plain. The choice is saved, and is the same setting as in the config.

### Tools
| Tool | Key | Unlocks | What it does |
|---|---|---|---|
| Brush | B | Start | Paints one cell |
| Broad brush | G | Level 2 | Paints a 2x2 block |
| Fill | F | Start | Fills an area of one colour |
| Ruler | L | Level 3 | Drag out a straight line |
| Knife | K | Start | Scrapes paint off (right-click does too) |
| Dropper | D | Level 6 | Click a cell to mix its colour |
| Mirror | M | Level 4 | Repeats strokes across the middle |
| Glaze | T | Medium canvases | A dark wash over paint ({link("Canvases and Techniques")}) |
| Highlight | H | Large canvases | A dab of thick, light paint ({link("Canvases and Techniques")}) |
| Undo | Z | Start | Undoes the last stroke (paint isn't refunded) |
| Zoom | Mouse wheel | Medium canvases | Zooms the canvas and reference in, up to 3x; arrow keys move round |

Number keys 1-0 pick your first ten colours.

## Results
![Results](images/screens/results_masterpiece.png)

The results screen shows:
- **Stars**, from how well you copied the reference.
- **Shape, Color, Neatness and (on medium and large canvases) Technique**, each rated Poor to Excellent.
- **Mixing**: how pure your paint was. **Sparks** caught, and the **light** left.
- The painting's **quality** and price, and the Painting XP you earned.
- A **critique**. From 2 hearts with Sable, the critique is theirs.

See {link("Quality and Masterpieces")} for how the quality is worked out.

## Painting takes time
Painting uses part of your day. When you step away from the easel, the clock moves on:

| Canvas | Still life | Portrait or view |
|---|---|---|
""" + "\n".join(f'| {c["kind"]} | {c["minutes"] // 60} hour{"s" if c["minutes"] > 60 else ""} | {c["minutes"] // 60 + 1} hours |' for c in DATA["canvases"]) + """

In multiplayer, where the clock is shared, painting doesn't move it.
""")


def canvases():
    rows = []
    for c in DATA["canvases"]:
        name = f'{c["kind"]} Canvas'
        item = item_by_name(name)
        r = recipe_for(name)
        s = stock_for(name)
        tech = {"None": "Plain paint", "Glaze": "Glazing", "Glaze, Highlight": "Glazing and highlights"}[c["techniques"]]
        rows.append(f'| {icon(item["id"])} | **{name}** | {c["size"]}x{c["size"]} | {"Start" if not c["level"] else "Level " + str(c["level"])} | {tech} | {ingredients(r)} | {gold(s["price"])} |')
    for kind in ("Medium", "Large"):
        name = f"Linen Canvas ({kind})"
        item = item_by_name(name)
        r = recipe_for(name)
        s = stock_for(name)
        size = next(c["size"] for c in DATA["canvases"] if c["kind"] == kind)
        rows.append(f'| {icon(item["id"])} | **{name}** | {size}x{size} | Level 8 | As {kind.lower()}, and Masterpieces | {ingredients(r)} | {gold(s["price"])} |')
    page("Canvases and Techniques", """
## Canvases
| | Canvas | Cells | Unlocks | Techniques | Recipe | Art shop |
|---|---|---|---|---|---|---|
""" + "\n".join(rows) + f"""

What goes on each size:
- **Small:** one item.
- **Medium:** up to two items, or a portrait (head and shoulders).
- **Large:** up to three items, or a whole portrait.

Every size can take a view, painted outdoors (see {link("Portraits and Plein Air")}).

## Techniques
The painting grows with the canvas, one new idea at a time.

![Glazing](images/screens/paint_glaze.png)

### Glazing (medium and large canvases)
Some cells of the reference are **glazed**: a thin, dark wash over a colour, like the shaded side of a pumpkin. To copy one:
1. Paint the cell its colour.
2. Go over it with the **Glaze** tool (T).

Glazing a bare cell does nothing, and painting over a glaze covers it. With the Glaze tool in hand, a dot marks every cell that wants a glaze. Glazes cost no paint.

![Highlights](images/screens/paint_highlight.png)

### Highlights (large canvases)
A few of the brightest spots (the shine on a gem, the light in an eye) take a **highlight**: a dab of thick, light paint, put on with the **Highlight** tool (H).
- **Highlights go on last.** Painting a cell next to a highlight smears it away, and the easel warns you when that happens.
- As with glazes, dots show where highlights go, and they cost no paint.

### How techniques are scored
On a canvas with glazes or highlights, the results add a **Technique** rating: how many of them you put in the right places, and how few in the wrong ones (a cell out is forgiven). A painting with no techniques at all loses about a star. See {link("Quality and Masterpieces")}.

### Simplified mode
In **Simplified** mode (one of the four on the easel's setup screen, or in the {link("Configuration")}), references are plain paint only: no glazes or highlights to copy. It's the easiest mode: no light meter, and free paint that never dries. Paintings are worth half as much. If you'd like the free paint but the whole picture, turn off **Simplified mode removes glazes and highlights** in the {link("Configuration")}. From a medium canvas you still have the Glaze tool, to glaze wherever you like (it isn't scored). The exception is a painting for a glazed commission: its reference has glazes (no highlights), scored as usual.

A small canvas has no glazes or highlights in any mode; a small painting is still shaded, with your pigments.

## Linen
From Painting level 8, linen canvases (medium and large) paint like any other. They're the only canvas a **Masterpiece** can be painted on; see {link("Quality and Masterpieces")}. On the setup screen, the **Linen** button next to the canvas sizes switches to linen.
""")


def quality():
    q = DATA["quality"]
    caps = []
    last = None
    for l in DATA["levels"]:
        if l["maxQuality"] != last:
            caps.append(f'| {l["level"]}+ | {l["maxQuality"]} |')
            last = l["maxQuality"]
    page("Quality and Masterpieces", f"""
## Quality
A painting's quality comes from how well you copied the reference, plus your skill:

> **Score** = stars + 0.03 x Painting level + {q["inspiredBonus"]} if Inspired + 0.25 if every cell was mixed pure

| Score | Quality |
|---|---|
| under {q["silver"]} | Normal |
| {q["silver"]} | Silver |
| {q["gold"]} | Gold |
| {q["iridium"]} | Iridium |

Your level caps the quality, however good the painting:

| Painting level | Highest quality |
|---|---|
""" + "\n".join(caps) + f"""

The **Visionary** profession doubles what Inspired adds. A painting is Inspired when you catch {q["sparksForInspired"]} inspiration sparks.

### Stars
Stars (half steps, out of five) measure how close your copy is:
- **Shape:** does your painted silhouette match the reference's?
- **Color:** the right colours in the right places, with a cell's slip forgiven. Off and muddy paint counts for less.
- **Neatness:** stray paint well away from the subject.
- **Technique** (medium and large canvases): the glazes and highlights.

Where on the canvas you paint doesn't matter: every position is tried, and the best one counts.

## Masterpieces
![A Masterpiece](images/screens/results_masterpiece.png)

A **Masterpiece** is above iridium. It needs all of these:
- a **linen canvas** (from Painting level 8),
- **iridium** quality,
- **five stars**,
- **Inspired**,
- on a canvas with techniques, at least 90% of its glazes and highlights right.

A Masterpiece:
- is marked with a gold star in your bag, and "A masterpiece." in its description,
- sells for {DATA["market"]["masterpiece"]}x an iridium painting,
- earns half as much friendship again as a gift, and Sable has something to say about one,
- still counts as iridium quality to the game, so other mods see an iridium painting.
""")


def palette_table():
    rows = []
    for name, tubes, level in (("Wooden Palette", 3, None), ("Enamel Palette", 6, 5), ("Iridium Palette", 10, 9)):
        item = item_by_name(name)
        r = recipe_for(name)
        s = stock_for(name)
        rows.append(f'| {icon(item["id"])} | **{name}** | {tubes} tubes ({tubes * 100} cells) | {ingredients(r)} | '
                    f'{"Known from the start" if r["known"] else "Painting level " + str(r["level"])} | {gold(s["price"])} |')
    return ("| | Palette | Each colour holds | Recipe | Unlocks | Art shop |\n|---|---|---|---|---|---|\n" + "\n".join(rows))


def pigments():
    rows = []
    for p in DATA["pigments"]:
        sold = next((st for st in DATA["shop"] if st["item"] == p["name"] + " Pigment"), None)
        where = f'Buy the pigment from Sable{" (" + level_text(sold["condition"]) + ")" if sold and "LEVEL" in (sold["condition"] or "") else ""}' if sold else None
        how = ("Known from the start" if p["starter"]
               else "Rare: " + ", ".join(x for x in (where and where.replace("Buy the pigment from Sable", "Sable"), "inspiration sparks", "milling", "a tube") if x) if p["rare"]
               else (where or "Mill it") + ", or pick up a tube")
        mill = "<br>".join(f'{icon(m["id"], 24)} {m["input"]} x{m["count"]} → {m["tubes"]} tubes' for m in p["mill"]) or "-"
        rows.append(f'| {swatch(p["hex"])} | {icon(p["tube"], 24)} **{p["name"]}** | {p["difficulty"]} | {p["motion"]} | {how} | {gold(p["tubePrice"])} | {mill} |')
    pigment_stock = [s for s in DATA["shop"] if s["item"].endswith(" Pigment")]
    stock_rows = "\n".join(f'| {icon(s["id"], 24)} {s["item"]} | {gold(s["price"])} | {level_text(s["condition"])} |' for s in pigment_stock)
    page("Pigments and Paint", f"""
Painting has two resources: the **colours you know** and the **paint you have**.

## Colours are knowledge
You know a colour the way you know a recipe, and you never lose one. You start with the eight starter colours. There are four ways to learn more:
- **Buy its pigment** from Sable. The jar teaches you the colour the moment it reaches your bag.
- **Pick up a tube** of a colour you don't know: the first one teaches it.
- **Mill it** in a Pigment Mill (below).
- **Inspiration sparks** very rarely teach a rare colour.

The more colours you know, the closer your references get to the real thing. That makes them harder, and worth more.

## Paint is what you spend
- **The Palette.** It holds paint for each colour, in tubes' worth (a tube is 100 cells). A new Palette comes with the starter colours.
- **Spending.** Every cell you paint uses one cell of paint. The setup screen warns you if you're short of a colour.
- **Tubes go straight in.** When a tube reaches your bag, bought, milled or picked up, it goes into your Palette at once, if you know the colour and there's room for the whole tube. Anything that doesn't fit stays in your bag.
- **Simplified** paints for free and needs no Palette.

## Palettes
{palette_table()}

- **Which Palette you paint from:** the easel uses the biggest Palette in your bag, and tubes go into that one too.
- **Bigger wells:** once your {link("Painting Record")} reaches 15 subjects, every Palette holds half as much again.

- **Not a gift:** like a tool, a Palette can't be given to a villager by accident.

### A Palette on the floor
![A Palette's wells](images/screens/palette_menu.png)

Set a Palette down and it becomes storage for paint, like a chest that only takes tubes:
- **Right-click** it to open it. Each colour shows how many tubes' worth it holds.
  - Click a colour to take a whole tube out.
  - Click a tube in your bag to put it in. Shift-click puts in the whole stack, as much as fits.
- **Hit it with any tool** to pick it up again, paint and all.
- **To paint, it has to be in your bag**, so keep a spare Palette at home as a paint store, and carry your best one.

## Every colour
Difficulty and motion describe the colour's mixing bar: how hard it is, and how the true colour moves.

| | Colour | Difficulty | Motion | How to learn it | Tube | Pigment Mill |
|---|---|---|---|---|---|---|
""" + "\n".join(rows) + f"""

## Pigments at the art shop
Each pigment is sold once, until you know the colour.

| Pigment | Price | Needs |
|---|---|---|
{stock_rows}

## The Pigment Mill
{item_infobox("Pigment Mill")}
The Pigment Mill grinds tubes of paint overnight (10 hours) from clay, coal, stone, flowers, berries, minerals and gems: see the table above. Milling a colour you don't know gives you its tube, and the tube teaches it. The recipe unlocks at Painting level 1, or you can buy the Mill from Sable.
""")


def brushes():
    rows = []
    for b in DATA["brushes"]:
        r = recipe_for(b["name"])
        s = stock_for(b["name"])
        img = icon(b["item"]) if b["item"] else ""
        effects = []
        if b["band"]: effects.append(f'wider mix band (+{b["band"]:g})')
        if b["steadiness"]: effects.append(f'steadier colours ({int(b["steadiness"] * 100)}%)')
        if b["sparkTime"]: effects.append(f'sparks last +{b["sparkTime"]:g}s')
        desc = "The brush you always have." if b["id"] == "hog" else b["description"]
        # The icon sits with the name: a column of its own was squeezed thin.
        rows.append(f'| {img + "<br>" if img else ""}**{b["name"]}** | {b["load"]} | {", ".join(effects) or "-"} | {b["uses"] or "Never wears out"} | '
                    f'{ingredients(r) + "<br>(Level " + str(r["level"]) + ")" if r else "-"} | {gold(s["price"]) + " (" + level_text(s["condition"]) + ")" if s else "-"} | {desc} |')
    page("Brushes", f"""
Brushes are the painter's tackle. Pick one on the setup screen; the hog bristle brush is always there.

| Brush | Load | Effects | Uses | Recipe | Art shop | |
|---|---|---|---|---|---|---|
""" + "\n".join(rows) + """

- **Load** is how many cells a full brush covers before you mix again, before level bonuses.
- **Uses** is how many paintings a brush lasts. A brush wears by one each time you finish a painting with it, like fishing tackle. A green bar on the item shows what's left, like the watering can's water.
""")


def skill():
    rows = "\n".join(f'| {l["level"]} | {l["xp"]:,} | {"<br>".join(l["unlocks"])} |' for l in DATA["levels"])
    prof = {p["name"]: p["description"] for p in DATA["professions"]}
    page("Painting Skill", f"""
**Painting** is a new skill, with its own bar on the skills page (through SpaceCore).

## Experience
- **Finishing a painting:** more for more stars, harder references and bigger canvases. Portraits and views give 30% more.
- **Selling a painting:** a little, at a shop or through the shipping bin.
- **Completing a commission.**

## Levels
| Level | Total XP | Unlocks |
|---|---|---|
{rows}

## Professions
At level 5 you choose one of two professions, and at level 10 one of the two that follow from it.

| Level 5 | Level 10 |
|---|---|
| **Plein Air**: {prof["Plein Air"]} | **Steady Hand**: {prof["Steady Hand"]}<br>**Visionary**: {prof["Visionary"]} |
| **Gallerist**: {prof["Gallerist"]} | **Patron's Favorite**: {prof["Patron's Favorite"]}<br>**Prolific**: {prof["Prolific"]} |

## Past level 10: Painting Mastery
Your Painting XP keeps counting after level 10, towards **Painting Mastery** (15,000 XP). Margot sends you her old **Study Board** to hang up, and it fills with pinned studies as you go. When it's full, she tests you at the museum; pass, and you're a conservator, with the **Master's Easel** and the right to restore old paintings yourself. See {link("Old Paintings and Restoration")}.
""")


def selling():
    m = DATA["market"]
    nov = m["novelty"]
    page("Selling and Prices", f"""
## Where to sell
| Where | Pays | Notes |
|---|---|---|
| Pierre's | the painting's price | |
| The shipping bin | the painting's price | |
| The {link("Art Shop")} | {m["artShop"]}x | Sable pays more for paintings |
| {link("Consignment")} at Sable's gallery | {m["consign"]}x | Sells in 1 to 3 days |
| A {link("Commissions", "commission")} | the reward on the board | Plus friendship and XP |

## What a painting is worth
A painting's price is set when you finish it. These all raise it:
- **Canvas size:** bigger canvases are worth much more.
- **Difficulty:** busier references, with more colours.
- **The subject:** a still life of something valuable is worth more. Portraits are valued like a loved gift. Views are valued by place: the farm and town least, the Summit, the Desert and Ginger Island most, and more again in rain, in winter or after 7pm.
- **Your Painting level.**
- **Quality:** silver 1.25x, gold 1.5x and iridium 2x, as for any item.

And these multiply it:

| | Value |
|---|---|
| Portraits and views | {m["portrait"]}x |
| The **Gallerist** profession | {m["gallerist"]}x |
| A Masterpiece | {m["masterpiece"]}x |
| Renown (the 30-subject milestone) | {m["renown"]}x |
| A subject you've never painted | {nov[0]["value"]}x |
| The 1st or 2nd painting of it this week | {nov[1]["value"]}x |
| The 3rd or 4th this week | {nov[3]["value"]}x |
| The 5th or later this week | {nov[5]["value"]}x |
| Varnish | up to {m["finishes"][-1]["Value"]}x ({link("Varnishing")}) |
| The mode you painted it on | Simplified 0.5x, Relaxed 0.85x, Normal 1x, Hard 3x |
| A Value frame in the room | up to 1.12x ({link("Frames")}) |

The **novelty** price stops one easy subject being farmed all week. The weekly count resets every Monday, so no subject is ever used up for good. The results screen tells you when a painting is worth more for being new, or less for being familiar. See {link("Painting Record")}.

## What it earns, roughly
Net gold per in-game hour of painting (sale price minus canvas and paint, divided by the hours a painting takes), from the mod's own economy check, by mode:

| Painting | Simplified | Relaxed | Normal | Hard |
|---|---|---|---|---|
| A first parsnip (level 1, small, sold to Pierre) | about 60g | about 110g | about 140g | about 490g |
| A still life (level 3, small, silver, at the art shop) | about 90g | about 160g | about 200g | about 670g |
| A pair (level 5, medium, gold, Gallerist) | about 170g | about 300g | about 370g | about 1,200g |
| A portrait (level 6, medium, gold) | about 260g | about 440g | about 530g | about 1,700g |
| A trio (level 9, large, iridium) | about 390g | about 680g | about 820g | about 2,600g |
| A view or portrait (level 9-10, large, iridium) | about 610g | about 1,000g | about 1,200g | about 3,900g |
| A new friend's Masterpiece, varnished and consigned | about 1,500g | about 2,800g | about 3,300g | about 10,500g |

That last painting sells for about 7,200g on Simplified, 12,200g on Relaxed, 14,400g on Normal and 43,200g on Hard (more with a Value frame in the room).
""")


def record():
    perks = {"tubes": "Three tubes of Aquamarine paint (and the colour)", "palette": "Every Palette's wells hold half as much again",
             "renown": f'Every painting you make is worth {DATA["market"]["renown"]}x', "iridium": "A letter from Sable, with an Iridium Brush"}
    rows = "\n".join(f'| {c["subjects"]} | {perks[c["perk"]]} |' for c in DATA["catalogue"])
    page("Painting Record", f"""
![The painting record](images/screens/record_page.png)

Every subject you paint goes in your **painting record**: how many times you've painted it, your best quality, and when you first painted it. Open it at the art shop's counter (**Your painting record**).

**What counts as a subject:**
- each item, or each set of items in a still life (in any order),
- each friend's portrait,
- each place's view in each season.

Free canvases don't count.

## Tabs
The record opens on an **overview** of everything you've painted, with the milestones below. The tabs along the top sort it like the game's Collections tab:

| Tab | Holds |
|---|---|
| Crops | Everything grown from seeds or fruit trees |
| Forage | Wild produce: greens, berries, mushrooms |
| Fish | Everything in the fishing collection |
| Minerals | Gems and minerals |
| Cooking | Every dish |
| Portraits | Everyone who can sit for a portrait |
| Views | Every outdoor place you can set up an easel |
| Creatures | Every monster you've slain, and the Junimos |
| Everything else | Artisan goods, animal products and the rest (only what you've painted) |

- **Still to paint:** what you haven't painted yet shows faded. Hover over it to see how to paint it. Something you've never had shows as "???".
- **Still lifes of several items** count for each item in them.
- **Views** count for their place in any season. Small squares on the tile show the seasons you've painted it in.
- **Your own painting:** a subject you keep a painting of, in the portfolio or the plan chest, shows that painting on its tile ({link("Portfolio and Back Room")}).

## Tab rewards
Every tab except "everything else" rewards you for painting a share of it:

| Painted | Reward |
|---|---|
| 25% | Three tubes of the tab's colour (and the colour) |
| 50% | Two Picture Frames |
| 100% | Every painting from that tab sells for 1.1x |

The colours: Leaf (crops), Moss (forage), Cerulean (fish), Iridium Violet (minerals), Crimson (cooking), Peach (portraits) and Slate (views). Rewards come after the painting that reaches them, so a record from before the tabs catches up with the next painting you make.

## Novelty
A subject's first painting ever sells for more. Painting the same subject again and again in one week sells for less, and the count starts over every Monday; see {link("Selling and Prices")}.

## Milestones
| Different subjects | Reward |
|---|---|
{rows}
""")


def back_room():
    page("Portfolio and Back Room", f"""
The art shop has a **back room** now: the door in the shop's back wall, under the "Artists only!" sign. It's where you keep your work. Everything in it is shared by the whole farm.

## The portfolio book
The book on the lectern is a little like the Community Center, but for paintings. Each page asks for particular paintings. Put one in a slot and it stays there, showing in its slot. A better painting of the same thing can take its place later, and the old one comes back to you.

Finish a page and Sable writes the next day, with a reward.

| Page | Asks for | Reward |
|---|---|---|
| The Four Seasons | Pelican Town in spring, summer, fall and winter, and your farm | A Varnishing Cabinet |
| Harvest | A still life of a crop from each of spring, summer and fall, a tree fruit, and three crops together | A Pigment Mill |
| Fine Work | A gold painting, an iridium one, a glazed one, a varnished one, and a Masterpiece | Two medium linen canvases |
| Far Places | Views of the Calico Desert, the Skull Cavern, Ginger Island's beach and its north, and the Secret Woods | Two large linen canvases |
| Legends | The Crimsonfish, Angler, Legend, Glacierfish and Mutant Carp, painted from the fish in your bag | An Iridium Palette |
| Faces of the Valley | Portraits (4 hearts per sitter): the Saloon regulars (Gus, Emily or Shane), a family (Robin, Demetrius, Maru or Sebastian), an old couple (Evelyn or George), Sable, and a visitor (Julian) | Three Moonlit Frames |

A group's slot takes a portrait of any one of its members.

## The finished book
- **Every page finished:** Sable writes with the **Gold-Leaf Frame**, an epic {link("Frames", "frame")}.
- **Then the Summit:** paint the view from the Summit, and Sable climbs up to join you and look through the finished book. The scene plays by itself. The Summit view goes into the book as its last page.

You can set an easel down just inside the Skull Cavern for its view.

Paintings in the book (and the plan chest) show as **your own painting** in the {link("Painting Record")}.

## The plan chest
The wide chest of drawers holds any paintings you want to keep: 70 of them, like a big chest, shared by the farm.

## Sable's room
The trapdoor down to Sable's room is in the back room's corner now, not on the shop floor. It opens at 2 hearts, or with the Key to the Town, as before. See {link("Sable")}.
""")


def julian():
    box = infobox("Julian Vane", '<img src="images/julian_portrait.png" width="128">', [
        ("Pronouns", "he/him"),
        ("Visits", "The 22nd to the 24th of every season"),
        ("Lives in", "Zuzu City"),
        ("Marriage", "No"),
    ])
    page("Julian Vane", box + f"""
**Julian Vane** is an art critic from Zuzu City, at *The Zuzu Review*. Ten years ago he reviewed Sable's first show in one sentence everyone still quotes. Now he's visiting the valley.

<br clear="right">

## Visits
Julian is in the valley on the **22nd, 23rd and 24th** of every season. He gets off the morning bus just before 10am and leaves on the evening one, once he's walked back to the bus stop after 6pm. His friendship doesn't fade while he's away.

| Day | 10am | 1pm | 4pm | 6pm |
|---|---|---|---|---|
| 22nd | The art shop's back room, looking through your portfolio | The museum | The Saloon | The bus stop, then gone |
| 23rd | The beach | The museum | The Saloon | The bus stop, then gone |
| 24th | The art shop's back room | Cindersap Forest, by the river where Sable paints (from 1:30pm) | The Saloon | The bus stop, then gone |

The times are when he sets off, so give him a little while to walk there. He's at the Stardew Valley Fair too, on the 16th of fall (see {link("Festivals")}).

## Gifts
- **Loves:** wine, truffle oil, prismatic shards, and fine paintings
- **Likes:** cheese and preserves
- **Hates:** Joja Cola, slime and seaweed

## Heart events
Five heart events, at 2, 4, 6, 8 and 10 hearts, tell his story with Sable. Along the way there's an Old Catalogue to deliver, and at the end a framed review to hang.

<details><summary>Spoilers: when and where each event happens</summary>

| Hearts | Where and when |
|---|---|
| 2 | The shop's back room, a visit morning, once your portfolio holds three paintings |
| 4 | The museum, a visit afternoon |
| 6 | The bus stop, the evening of the 24th, after Sable's 6-heart event |
| 8 | The Saloon, a visit evening |
| 10 | The art shop, a visit evening, after Sable's 8-heart event and once Sable has the catalogue |

</details>

## Julian buys a painting
Once a season, on one of his visit days, talk to Julian again after his first line and he'll offer to buy a painting of yours at **twice the art shop's price**. Pick it from your bag, and he tells you what he thinks of it: the better the painting, the kinder the review. A good home for a Masterpiece.

## Winter
In winter Julian wears a long camel overcoat and a charcoal scarf outdoors.

## Julian and Margot
Once you've met them both, Julian meets {link("Margot Pell")} at the Saloon on one of his visit days, and they argue about whether restoring a painting is art. You settle it. After that they bicker on his visits, a little fonder each time.

## The Fair
Julian judges the painting contest at the Stardew Valley Fair. See {link("Festivals")}.
""")


def festivals():
    page("Festivals", f"""
## The Stardew Valley Fair: the painting contest
Julian judges a painting contest at the Fair, at a table on the grass beside the grange displays.

- **Entering:** talk to him before Lewis judges the grange, and pick one painting from your bag. It stands on his easel for the day. One entry each.
- **Judging:** it's judged with the grange, out of 100:
  - quality: 30 normal, 45 silver, 60 gold, 75 iridium
  - stars: 3 for each
  - how hard the subject was: up to 5
  - a varnish: satin 3, gloss 5, gallery 8
  - a Masterpiece: 10
- **The rivals:** Leah, Elliott (or Evelyn, later in Sable's story) and Emily enter too. Their stack of canvases leans on the tree beside him.
- **The result:** talk to Julian after the judging for your placing, star tokens, a ribbon to hang, and your painting back.

| Place | Star tokens | Ribbon |
|---|---|---|
| 1st | 1,000 | Blue |
| 2nd | 500 | Red |
| 3rd | 200 | Yellow |
| 4th | 50 | - |

## The Night Market: Sable's stall
All three nights from 5pm, Sable keeps a little skiff of their own, moored below the bridge to the submarine. Talk to them from the bridge.

- **The Moonlit Frame:** one a night, for 1,500g. Frame a painting in it at an easel, and it hangs in midnight blue and silver. It's a rare frame: hung, it gives the easels in its room more light after 7pm (see {link("Frames")}).
- **Unusual paintings:** Sable buys them for 50% more (1.5× the usual price), up to three a night:
  - views of the Far Places (the desert, the Skull Cavern, Ginger Island, the Secret Woods)
  - the legendary fish
  - monsters
  - views painted at night (from 7pm) or in the rain
- **Lupini**, the market's own painter, is three boats along. Ask Sable about them.

A view painted at night or in the rain says so in its description. Views painted before 1.2 don't remember.

## Spirit's Eve: the haunted maze
An easel stands in the heart of the haunted maze, by the fountain. Paint the maze by lantern light: a view you can only paint that night, and painting at a festival takes no time.

## The Dance of the Moonlight Jellies
An easel waits on the sand during the dance. Paint the jellies drifting out to sea: another view of one night only.

## The Feast of the Winter Star
If Sable draws you as their secret friend, their gift is a painting: an iridium still life of whatever you ship most.

## Sable and Margot at the festivals
Sable goes to every festival; see {link("Sable")}. So does Margot, once she's in the valley; see {link("Margot Pell")}. She dances at the Flower Dance if you ask her at 4 hearts or more.
""")


def commissions():
    c = DATA["commissions"]
    page("Commissions", f"""
![The commission board](images/screens/board.png)

The **commission board** in the {link("Art Shop")} posts up to {c["MaxPosted"]} requests at a time. Each one is from a villager, who wants a painting of something they love or like, at a minimum quality, for a reward.

## Taking a commission
1. Click **Accept** on a request. You can hold {c["MaxAccepted"]} at a time, and you have {c["DaysToDeliver"]} days to deliver.
2. Paint it. On the setup screen, a gold ribbon marks the subject someone is waiting on.
3. Hand the painting to the villager, as you would a gift. It doesn't use up a gift for the week.

They pay the reward, and you gain {c["FriendshipReward"]} friendship points and Painting XP. If the painting isn't good enough, they tell you what quality they wanted.

## Portrait commissions
From medium canvases (Painting level 4), friends at 4 hearts or more sometimes ask for a **portrait of themselves**. Only friends who would sit for you ask. See {link("Portraits and Plein Air")}.

## Asking for more
Later on, some requests want more than the subject:
- **Varnished:** from Painting level 7, about one in four asks for a varnished painting (any finish, satin or better). They pay 30% more, and you have 14 days to deliver, time for the varnish to cure. See {link("Varnishing")}.
- **Glazed:** once medium canvases are open to you, about one in five asks for a glazed painting: one painted to a reference with glazes, with at least half of them (and any highlights) in the right places. A single stray glaze doesn't count. They pay 25% more, and on Simplified the reference for that subject still shows its glazes. See {link("Canvases and Techniques")}.

The board shows what's wanted beside the quality, and if you hand over a painting without it, the villager tells you what's missing.

The **Patron's Favorite** profession makes commissions pay more.
""")


def consignment():
    page("Consignment", f"""
![Consigning paintings](images/screens/consign_menu.png)

Leave up to **three paintings** with Sable at the art shop's counter (**Leave paintings with Sable to sell**).
- They go on the easels in Sable's corner of the shop, where you can see them.
- Each sells within one to three days, for **{DATA["market"]["consign"]}x** its price: more than any shop pays.
  - Iridium paintings sell by the next morning.
  - Gold ones sell within two days.
  - Others take two or three.
- You can take a painting back any time before it sells.
- When one sells, the money arrives that morning, with a message and a little Painting XP.

Sable takes paintings in person: the counter has to be staffed (or you have to be married to Sable). In {link("Multiplayer")}, each player has their own three easels.
""")


def varnishing():
    f = DATA["market"]["finishes"]
    rows = "\n".join(f'| {x["Days"]} days | {x["Key"].capitalize()} | {x["Value"]}x |' for x in f)
    page("Varnishing", f"""
{item_infobox("Varnishing Cabinet")}
{item_infobox("Varnish")}
![A Varnishing Cabinet](images/screens/farmhouse_varnish.png)

Varnish a painting and it's worth more: the painter's answer to a cask. From Painting level 7, you can craft both the **Varnishing Cabinet** and **Varnish**, or buy them from Sable.

## Using the cabinet
1. Place the cabinet, indoors or out.
2. With a tin of **Varnish** in your bag, use the cabinet while holding a painting. The painting goes in and the tin is used up. You can see it through the glass.
3. Wait. The varnish cures over the weeks:

| In the cabinet | Finish | Value |
|---|---|---|
{rows}

4. **Right-click** the cabinet to see how it's coming along. Like a cask, it gives the painting up by itself only once it's fully cured. It shows the "ready" bubble then.
5. **To take it out sooner**, hit the cabinet with any tool. You get the finish it has reached. Before 7 days there's no finish, and the varnish is wasted.

A varnished painting:
- has a faint gloss on its picture,
- shows its finish in its description,
- keeps the finish when framed.

Each painting can be varnished once.
""")


def portraits():
    page("Portraits and Plein Air", f"""
## Portraits
![Painting a portrait](images/screens/setup_portrait.png)

Once you have medium canvases (Painting level 4), friends at **4 hearts or more** will sit for a portrait. On the setup screen, their faces appear under the items in your bag: pick one.
- **Canvas:** a medium canvas paints head and shoulders; a large one, the whole portrait.
- **The reference** is their own portrait, reduced to the canvas in the colours you know.
- **Time:** a portrait takes an hour longer than a still life.
- **Value:** it sells for {DATA["market"]["portrait"]}x, and gives 30% more Painting XP.
- **As a gift:** the sitter always loves being given theirs.
- **Commissions:** friends at 4+ hearts also post portrait requests on the {link("Commissions", "commission board")}.
- **Krobus and the Wizard** sit sooner, without the usual 4 hearts: Krobus once you've met him in the sewers, the Wizard once you've brought him the magic ink.

## Creatures
Paint a study of any monster you've slain, from memory. On the setup screen, the creatures slot shows them in place of your bag. Junimos join once the Community Center (or Joja's) is done.
- **The reference** is the creature's own sprite, in the colours you know.
- **Size:** a bigger creature needs a bigger canvas.
- **The record** has a Creatures tab ({link("Painting Record")}).

## Animals
Paint your pet, your horse, and any of the farm animals you own: chickens, ducks, rabbits, cows, goats, sheep, pigs, ostriches and dinosaurs. They have their own slot on the setup screen, next to the creatures, at any easel.
- **The reference** is the animal's own sprite, in the colours you know.
- **Size:** picking one moves you up to the smallest canvas it fits that you've unlocked. Goats, sheep and ostriches are shrunk to fit a medium canvas; cows, pigs and horses need a large one.
- **The record** has an Animals tab ({link("Painting Record")}).

## Plein air: painting the view
![Painting the view](images/screens/setup_view.png)

An easel placed **outdoors** offers **the view** as a subject. The scene around the easel, mostly what's in front of it, becomes the reference. The game draws the place as it is: trees, buildings, the weather, the season.
- **Canvas:** the view fills the whole canvas. A small canvas paints a closer view (about 10 tiles across, two cells to a tile), a medium one 13 tiles and a large one 16, so a house still reads as a house on a small canvas.
- **Time and value:** like a portrait, it takes an hour longer and is worth {DATA["market"]["portrait"]}x.
- **Place:** it's worth more somewhere far from the farm. The farm and town are everyday; the Woods and the Railroad less so; the Summit, the Desert and Ginger Island are rare.
- **Conditions:** rain, winter and evening (after 7pm) each raise it.
- **Seasons:** the same place in another season is a new subject in your {link("Painting Record")}, so a year of views pays well.
""")


def gifting():
    lovers = ", ".join(DATA["artLovers"])
    page("Gifting Paintings", f"""
Paintings make good gifts, and nobody dislikes one.

| Painting | Most villagers | Art lovers |
|---|---|---|
| Normal quality | Neutral | Like |
| Silver or gold | Like | Like |
| Iridium | Love | Love |

- **Art lovers:** {lovers}.
- **The subject matters:** a painting of something the villager loves or likes goes up a step (Neutral to Like, Like to Love).
- **Portraits:** a villager always loves a portrait of themselves.
- **Masterpieces** earn half as much friendship again.
- **Patron's Favorite:** the profession earns 25% more friendship from painting gifts.
- **Quality:** the game's usual quality bonus to friendship applies on top.

The art lovers list can be changed by other mods (`Mods/blinxxy.MuseArt/ArtLovers`) and in the {link("Configuration")}.
""")


def art_shop():
    rows = []
    for s in DATA["shop"]:
        cond = s["condition"] or ""
        if "KNOWS_PIGMENT" in cond and cond.startswith("!"):
            when = "Until you know the colour" + (f' ({level_text(cond)})' if "LEVEL" in cond else "")
        elif "KNOWS_PIGMENT" in cond:
            when = "Once you know the colour"
        else:
            when = level_text(cond) if cond else "Always"
        rows.append(f'| {icon(s["id"], 24)} | {s["item"]} | {gold(s["price"])} | {when} |')
    page("Art Shop", f"""
![The art shop](images/screens/shop_exterior.png)

The **art supply shop** is in Cindersap Forest, west of the path down from Marnie's ranch. Sable runs it.

| | |
|---|---|
| **Hours** | 10am to 6pm |
| **Fridays** | until 5pm |
| **Sundays** | from 1pm |
| **Closed** | Wednesdays |

Married to Sable, you're never turned away: when they're out, the till runs on trust.

The shop is on the world map (M), west of the path down from Marnie's, and so are you while you're inside.

**The yard easel** by the barrels is yours to paint at, like an easel you set down yourself: still lifes, or the view of the yard. On Friday mornings Sable paints there, and it's theirs.

![Inside the shop](images/screens/shop_interior.png)

## The counter
Talk to the counter to:
- **Browse supplies (and sell paintings).** Sable pays {DATA["market"]["artShop"]}x for paintings.
- **The commission board:** see {link("Commissions")}.
- **Leave paintings with Sable to sell:** see {link("Consignment")}.
- **Your painting record:** see {link("Painting Record")}.

## Stock
| | Item | Price | Available |
|---|---|---|---|
""" + "\n".join(rows) + """
""")


def crafting():
    rows = []
    for r in DATA["recipes"]:
        rows.append(f'| {icon(r["id"])} | **{r["item"]}**{" x" + str(r["makes"]) if r["makes"] > 1 else ""} | {ingredients(r)} | {"Known from the start" if r["known"] else "Painting level " + str(r["level"]) if r["level"] else "-"} |')
    page("Crafting", """
Recipes unlock with the Painting skill: you learn each one the morning after you reach its level.

| | Item | Ingredients | Unlocks |
|---|---|---|---|
""" + "\n".join(rows) + """
""")


# Sable's other outfits, as the content pack names their portrait sheets.
SABLE_OUTFITS = {"winter": "Winter", "beach": "Ginger Island", "wedding": "Wedding"}


def outfit_row():
    """Small portraits of Sable's other outfits, each opening full size when
    clicked (GitHub wikis allow no scripts, so links rather than a swap)."""
    cells = "".join(
        f'<td align="center"><a href="images/sable_portrait_{key}.png"><img src="images/sable_portrait_{key}.png" width="64" alt="{label}"></a><br><sub>{label}</sub></td>'
        for key, label in SABLE_OUTFITS.items())
    return f'<br><sub>Outfits (click to enlarge):</sub><table><tr>{cells}</tr></table>'


def sable():
    s = DATA["sable"]
    def gifts(key):
        return "<br>".join(f'{icon(g["id"], 24)} {g["name"]}' for g in s[key]) or "-"
    box = infobox("Sable", '<img src="images/sable_portrait.png" width="128">' + outfit_row(), [
        ("Pronouns", "they/them"),
        ("Birthday", s["birthday"]),
        ("Lives in", "A room under the art shop, Cindersap Forest"),
        ("Marriage", "Yes, by anyone"),
    ])
    page("Sable", box + f"""
**Sable** runs the art supply shop in Cindersap Forest.
- **Before the valley:** a restorer at the Zuzu City Museum of Art, fixing other people's masterpieces.
- **Why they came:** the museum sent them a damaged landscape of Cindersap Forest, signed only "E.". They couldn't stop thinking about the place in it, so they quit and moved here.
- **Character:** dry, exact and quietly funny. Honest about your work, never cruel.
- **The critique:** from 2 hearts, it's Sable who writes the critique on your results screen.

> "Rain's the best light there is. Flat and honest. Nothing to hide behind."

<br clear="right">

## Schedule
| Day | Where |
|---|---|
| Most days | Painting by the river in the morning (spring and summer), at the counter from 10am, painting in the shop's corner in the evening |
| Wednesday | Shop closed: the museum in the morning, Evelyn and George's in the afternoon |
| Friday | Painting outside the shop in the morning, the counter until 5pm, then the Saloon |
| Sunday | The beach in the morning, the counter from 1pm |
| Rain and winter | Indoors, painting until the shop opens |
| Winter 15 to 17 | The counter until 4:30, then their stall at the Night Market ({link("Festivals")}) |

At 10pm Sable goes down the trapdoor in the shop's back room to their room underneath, and sleeps there. You can follow from 2 hearts, or with the Key to the Town; once they've let you in, it stays open.

Sable goes to every festival, and keeps a stall at the Night Market. They dress for the weather: a coat and scarf outdoors in winter, a linen shirt on Ginger Island.

## Gifts
| Love | Like | Dislike | Hate |
|---|---|---|---|
| {gifts("loves")} | {gifts("likes")} | {gifts("dislikes")} | {gifts("hates")} |

Like most villagers, Sable loves iridium paintings, and as an art lover they like any painting at all. See {link("Gifting Paintings")}.

## Heart events
<details><summary>Spoilers: when and where each event happens</summary>

| Hearts | Where and when | |
|---|---|---|
| 2 | The art shop, 6pm to 8pm (not Wednesdays) | Sable shows you the painting that brought them here |
| 4 | Cindersap Forest's riverbank, a sunny morning (Mon, Tue, Thu, Sat) | Painting side by side |
| 6 | The art shop, 6pm to 8pm | A letter from the museum, and a choice |
| 8 | Evelyn and George's house, a Wednesday afternoon | Who "E." was |
| 10 | The art shop, 6pm to 8pm, while dating | A portrait |
| 14 | The art shop, a sunny evening, once married | An exhibition, with your portfolio on show |

</details>

## Romance and marriage
- **Who:** Sable can be dated and married by any farmer, with a bouquet and a Mermaid's Pendant as usual.
- **The wedding:** it uses gender-neutral vows, and Sable wears a cream suit with a teal waistcoat, a flower on the lapel and a paint-splashed pocket square.
- **Married life:** Sable moves in with their own spouse room (a studio corner) and patio (an easel), and still keeps the shop. They're at the counter from 9am to 5pm, the Saloon on Fridays, and the beach on Sunday mornings.
  - Every so often Sable paints the farm from their patio easel, and the next morning gives you the painting.
  - Some mornings they have something to say about a painting of yours you've hung in the house.

## The yard easel on Fridays
Before marriage, Sable paints at the easel in the shop's yard from 8 to 10 on Friday mornings. Talk to them there to hear what's on it; talk again and you can **stay and watch** for half an hour, for a little Painting XP (once a day).
""")


def multiplayer():
    page("Multiplayer", f"""
Pelican Palette works in co-op. Every player needs the mod installed.

**Each player has their own:**
- Painting skill, professions and known colours
- {link("Painting Record")} and milestones
- {link("Commissions")}
- {link("Consignment", "consigned paintings")} (Sable's easels show your own)
- entry in the Fair's painting contest, and the paintings Sable buys at the Night Market ({link("Festivals")})

**Shared, like any other object:**
- easels, Pigment Mills and Varnishing Cabinets, which anyone can use
- placed and framed paintings
- the art shop and Sable
- the portfolio book and the plan chest in the back room ({link("Portfolio and Back Room")}): one for the farm

**Time:** painting doesn't move the clock in multiplayer, because time is shared. The host's morning marks varnish as cured, for everyone.
""")


def configuration():
    page("Configuration", """
Change these with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), or in `Mods/PelicanPalette/PelicanPalette/config.json` after the game has run once.

| Setting | Default | What it does |
|---|---|---|
| `Difficulty` | `Normal` | **Simplified:** the easiest: no light meter, free paint that never dries, and references in plain paint only (no glazes or highlights to copy); paintings are worth half as much.<br>**Relaxed:** Normal with no clock: no light meter, and sparks wait for you. Paint comes from your Palette and dries, and the picture is the same as on Normal; paintings are worth 85% as much.<br>**Normal:** mixed colours dry after a while.<br>**Hard:** the paint on the brush runs out, the light never pauses, and sparks fade fast; paintings are worth three times as much, and you earn 20% more XP.<br>Also on the easel's setup screen, beside Begin. |
| `LightTimeMultiplier` | `1.0` | Gives you more (or less) light per painting, on top of the difficulty (0.5x to 3x). |
| `SimplifiedPlainPictures` | `true` | Whether Simplified mode takes glazes and highlights out of the picture. Turn it off for Simplified's free paint and wide mixing band with the whole picture to copy. |
| `ArtLoversLikeAnyPainting` | `true` | Whether art lovers like even a normal-quality painting. |
""")


def compatibility():
    page("Compatibility", """
- **Stardew Valley** 1.6.15, with **SMAPI** 4.0 or later.
- **Required:** [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.9+ and [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348).
- **Optional:** [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).
- **Stardew Valley Expanded:** compatible. The shop, Sable's schedule, events and festival spots all fit SVE's maps, and the shop is drawn on SVE's world map too. Julian's events, the Fair contest and Sable's Night Market stall fit SVE's maps as well.
- **[CJB Cheats Menu](https://www.nexusmods.com/stardewvalley/mods/4):** its Skills tab has a Painting button beside the vanilla skills (one level at a time, with the level-up screen and the profession choice at 5 and 10), and the Professions list has the six Painting professions to tick on or off.
- **[Automate](https://www.nexusmods.com/stardewvalley/mods/1063):** Pigment Mills, Varnishing Cabinets and Framer's Benches work with it. A cabinet next to a chest takes an unvarnished painting and a tin of Varnish from it, and puts the painting back with the gallery finish after 28 days. A bench takes a framed painting and gives back the painting, then its frame.
- **[NPC Map Locations](https://www.nexusmods.com/stardewvalley/mods/239):** Sable shows on the map, their marker cropped to their face.
- **[Remapping](https://www.nexusmods.com/stardewvalley/mods/29049) and map recolours:** the shop draws itself on the world map, in the same spot on vanilla and SVE maps, and its shadow takes on the colour of whatever ground is under it.
- **[Polyamory Sweet](https://www.nexusmods.com/stardewvalley/mods/20599):** Sable can be one of several spouses. The shop's trust and the 14-heart exhibition go by Sable's own relationship, not which spouse is "first".
- **[Alec Revisited](https://www.nexusmods.com/stardewvalley/mods/10697)** (and its SVE patch): compatible. Alec's house sits just north of the shop; the shop is added to the forest after other mods, so it's never painted over.
- **Other mods:**
  - Anything that changes the stretch of Cindersap Forest west of the path from Marnie's may overlap the shop.
  - Paintings are ordinary objects (stored with the mod's data), so they're safe in chests, Automate and other inventory mods.
  - **[Custom Spouse Rooms](https://www.nexusmods.com/stardewvalley/mods/10029) and [Custom Spouse Patio Redux](https://www.nexusmods.com/stardewvalley/mods/10889):** Sable's studio corner and patio easel work with them.
  - **[Lookup Anything](https://www.nexusmods.com/stardewvalley/mods/541):** a painting's gift tastes show correctly, by its quality and subject.
  - **[CJB Item Spawner](https://www.nexusmods.com/stardewvalley/mods/93):** a spawned "Painting" has no picture: it shows as a bare canvas, sells for its base price, and can't be framed.
- **Saves:** safe to add to an existing save. To remove the mod, sell or trash your paintings, easels and art supplies first.
- **Multiplayer:** supported; see [Multiplayer](Multiplayer).
""")


def faq():
    page("FAQ", """
**Do I need a new save?**
No. The art shop's letter arrives the morning after you install.

**I can't start a painting.**
Read the line under **Begin**. It tells you what's missing:
- a canvas of that size,
- a Palette in your bag (not on Simplified, where paint is free),
- or the time: a painting can't run past 2am.

**A colour on the palette is faded.**
You don't know it yet. Buy its pigment, pick up a tube, or mill it ([Pigments and Paint](Pigments-and-Paint)).

**My paint keeps running out.**
Buy tubes of colours you use a lot, or grind your own in a Pigment Mill. Tubes refill the Palette when you sit down at the easel.

**My paintings never get past gold.**
Iridium needs Painting level 6, a high score, and usually Inspired or flawless mixing ([Quality and Masterpieces](Quality-and-Masterpieces)).

**What do the dots on the canvas mean?**
With the Glaze or Highlight tool in hand, dots show where the reference wants a glaze or a highlight ([Canvases and Techniques](Canvases-and-Techniques)).

**Who can I paint?**
Any friend at 4 hearts or more, from medium canvases. Also any monster you've slain, your pet, your horse and your farm animals ([Portraits and Plein Air](Portraits-and-Plein-Air)).

**My picture looks plain.**
- **A small canvas** has no glazes or highlights; they come with medium and large canvases.
- **The reference only uses colours you know**, in every mode. Learn more colours and pictures get truer ([Pigments and Paint](Pigments-and-Paint)).
- **Simplified mode** leaves glazes and highlights out, unless you turn that off in the [Configuration](Configuration).

**How do I meet Margot?**
Find a damaged old painting (your first is guaranteed from Painting level 3, in the next artifact spot or fishing treasure chest) and take it to Gunther at the museum. He writes to the city that day; his letter comes the next morning, and she arrives a week after it ([Margot Pell](Margot-Pell)).

**How do I restore old paintings myself?**
Reach Painting level 10, then keep painting: your XP fills the Painting Mastery bar, and Margot's **Study Board** on your wall fills with it. When it's full, talk to Margot at the museum and pass her test at gold or better. Until then, she restores them for a fee ([Old Paintings and Restoration](Old-Paintings-and-Restoration)).

**I lost my Study Board.**
Margot posts you another the next morning, if you don't have one anywhere (at most once a week).

**How do I paint the view?**
Place an easel outdoors and use it, or use the one in the art shop's yard ([Portraits and Plein Air](Portraits-and-Plein-Air)).

**Can I change the difficulty?**
Yes, on the easel itself: click the mode button beside **Begin** ("Normal") to go through Simplified, Relaxed, Normal and Hard, or set it in the [Configuration](Configuration). Harder modes pay more: Simplified half, Relaxed 85%, Hard three times ([Selling and Prices](Selling-and-Prices)).
""")


def translating():
    page("Translating", """
All of the mod's text is in two files:
- `PelicanPalette/i18n/default.json`: the easel, items, skill and shop.
- `[CP] Pelican Palette/i18n/default.json`: Sable's dialogue, events, mail and the shop's lines.

To translate:
1. Copy each file to `<language>.json`, for example `fr.json`.
2. Translate the values.

Keep these as they are:
- the keys,
- anything in `{{double braces}}`,
- in dialogue, the codes: `$h`, `#$b#`, `@` and similar.
""")


def changelog():
    page("Changelog", CHANGELOG.split("\n", 1)[1])


def sidebar():
    page("_Sidebar", f"""
**[Pelican Palette](Home)**

**Getting started**
- {link("Getting Started")}
- {link("FAQ")}

**Painting**
- {link("Painting")}
- {link("Canvases and Techniques")}
- {link("Quality and Masterpieces")}
- {link("Pigments and Paint")}
- {link("Brushes")}
- {link("Portraits and Plein Air")}
- {link("Painting Skill")}

**Paintings**
- {link("Selling and Prices")}
- {link("Commissions")}
- {link("Consignment")}
- {link("Painting Record")}
- {link("Varnishing")}
- {link("Gifting Paintings")}
- {link("Portfolio and Back Room")}
- {link("Frames")}
- {link("Old Paintings and Restoration")}
- {link("The Museum")}
- {link("Special Orders")}

**The valley**
- {link("Art Shop")}
- {link("Sable")}
- {link("Margot Pell")}
- {link("Julian Vane")}
- {link("Festivals")}
- {link("Crafting")}

**Playing**
- {link("Multiplayer")}
- {link("Configuration")}
- {link("Compatibility")}
- {link("Translating")}
- {link("Changelog")}
""")
    page("_Footer", f"Pelican Palette {DATA['version']} by blinxxy. Item tables are generated from the mod's own data.")



# ---------------------------------------------------------------- 1.3

def margot():
    m = DATA["margot"]

    def gifts(key):
        return "<br>".join(f'{icon(g["id"], 24)} {g["name"]}' for g in m[key]) or "-"
    box = infobox("Margot Pell", '<img src="images/margot_portrait.png" width="128">', [
        ("Pronouns", "she/her"),
        ("Birthday", m["birthday"]),
        ("Lives in", "A room off the Saloon, then a cottage by the river"),
        ("Works at", "The museum"),
        ("Marriage", "Yes, by anyone"),
    ])
    page("Margot Pell", box + f"""
**Margot Pell** is a conservator from the Zuzu City Museum of Art. When old paintings start turning up in the valley, the museum sends her to look after them.
- **Arrives:** a week after Gunther's letter. Take your first damaged old painting to Gunther at the museum; he writes to the city, and his letter comes the next morning. See {link("Old Paintings and Restoration")}.
- **Character:** warm, quick and talkative, especially about her work. She explains things nobody asked about, apologises, and keeps going.
- **Sable:** they trained together at the museum, before Sable left. She has a few things to say about that.

> "You don't make it new. You make it able to keep being old."

<br clear="right">

## Schedule
| Day | Where |
|---|---|
| Most days | The museum, at her table in the library, from 9am to 5pm; the Saloon in the evening |
| Tuesday and Thursday | Once the Painting Wing is open: the museum in the morning, the wing from 2pm ({link("The Museum")}) |
| Sunday | Sketching at the beach in the morning (she won't show you), then the Saloon |
| Rain | The museum until 8pm, then the Saloon |

She sleeps in her room down the steps from Gus's storeroom at the Saloon. After her 4-heart event, Robin builds her a **cottage** of her own between the main road and the river, south of Haley and Emily's house, and she goes home there instead. Her Saloon room becomes Gus's guest room.

## Festivals
Once she's in the valley she goes to all eight festivals, near Sable, with something to say at each. Ask her to the **Flower Dance** at 4 hearts or more and she'll dance with you, in the same white dress as the other dancers.

## Her cottage
Robin's letter comes three days after the 4-heart event, and from that morning the cottage stands on the town map, with a cobbled path down from the road to its door and a bench by the river.
- **The window box:** in the 4-heart event, Robin asks what to plant under the big window: **pink roses**, **lavender** or **marigolds**. That's what grows there (it's under snow in winter), and Margot mentions it the first time you talk to her at home.
- **Inside:** her restoration table under the north window, a plan chest, a little kitchen, and her frame workshop along the front wall.

She dresses for the weather: a coat and scarf outdoors in winter, linen on Ginger Island.

## Gifts
| Love | Like | Dislike | Hate |
|---|---|---|---|
| {gifts("loves")} | {gifts("likes")} | {gifts("dislikes")} | {gifts("hates")} |

Like most villagers, she loves an iridium painting; see {link("Gifting Paintings")}.

## What she does for you
- **Restores old paintings for a fee**, before you can do it yourself. Hold a damaged painting and talk to her at the museum (even before her line for the day: she'll always look at a painting first). She takes it, and three days later she posts it back to you, restored at silver quality: it comes attached to her letter. One at a time. Hold one out to her anywhere else and she'll tell you to bring it to the museum.
- **Painting Mastery:** the morning after you reach Painting level 10 (or after you first meet her, if that's later), she writes with her old **Study Board** to hang on your wall. It fills as your mastery bar does, and she has something to say about each quarter the next time you talk. When it's full, she sets you a test restoration at the museum. Pass it and you're a conservator. See {link("Old Paintings and Restoration")}.

| Set | Her fee |
|---|---|
""" + "\n".join(f'| {name} | {gold(fee)} |' for name, fee in dict((o["setName"], o["fee"]) for o in DATA["oldPaintings"]).items()) + f"""

## Heart events
<details><summary>Spoilers: when and where each event happens</summary>

| Hearts | Where and when |
|---|---|
| 2 | The museum, a weekday afternoon |
| 4 | Robin's shop, a weekday morning: the cottage is planned |
| 6 | Her cottage, an evening |
| 8 | The Forest river, a sunny morning |
| 10 | Her cottage, after hours, while dating |
| 14 | The museum, an evening, once married |

Julian and Margot also meet at the Saloon on one of his visit days, once you've met them both. See {link("Julian Vane")}.

</details>

## Romance and marriage
- **Who:** Margot can be dated and married by any farmer, with a bouquet and a Mermaid's Pendant as usual. She has her own wedding.
- **Married life:** she keeps her museum hours, Monday to Saturday. On Sundays she sketches at the river bend from her 8-heart event.
  - **Her spouse room:** a conservation corner, with her restoration table.
  - **Her patio:** an easel and a rack of her own small canvases. She's started painting.
""")


def old_paintings():
    sets = {}
    for o in DATA["oldPaintings"]:
        sets.setdefault(o["setName"], []).append(o)
    where = {
        "valley": "Artifact spots (1.5% a dig)",
        "wreck": "Fishing treasure chests (4% a chest)",
        "miners": "Crates and barrels in the mines (0.4% to 0.8%, more deeper down)",
        "calico": "Skull Cavern crates (1%), treasure rooms (10%)",
        "dwarvish": "Volcano Dungeon chests (8%)",
    }
    rows = []
    for name, items in sets.items():
        first = items[0]
        cells = first["size"]
        rows.append(f'| **{name}** | {where.get(first["set"], "-")} | {", ".join(i["title"] for i in items)} | {cells}x{cells} | {gold(first["value"])} |')
    page("Old Paintings and Restoration", f"""
![Restoring an old painting](images/screens/restore_mill.png)

Damaged old paintings turn up around the valley: twenty-one of them, in five sets. Each is flaked, torn or water-stained over about a third of it. Restored, they're worth far more than your own paintings, and the museum wants them.

## Finding them
| Set | Where | Paintings | Canvas | Restored value |
|---|---|---|---|---|
""" + "\n".join(rows) + f"""

- **Your first is guaranteed:** the first fishing treasure chest or artifact spot after you reach Painting level 3.
- **The Travelling Cart** sometimes sells one from any set (about one day in six), for 3,000g.
- **Margot's loupe**, from the museum's collection, makes every way of finding one half as likely again. See {link("The Museum")}.
- **Which one you get:** each find is a painting from its set that the museum hasn't got yet and that isn't already in your bag.

Take your first to **Gunther** at the museum: he writes to the city, and {link("Margot Pell")} arrives a week after his letter.

**Lost one?** Nothing's lost for good. If you lose your first before Gunther has seen it (trashed, left in a chest, dropped in the mines), the next artifact spot or fishing chest brings another. After that, a painting you've lost or sold comes back as readily as one you've never found, until the museum has it. A restoration of any quality can be donated.

## Getting one restored
Until you're a conservator, **Margot** restores them for a fee: hold the damaged painting and talk to her at the museum. Three days later it comes back in the post, attached to her letter, at silver quality. See {link("Margot Pell")} for her fees.

## Painting Mastery
Past Painting level 10, your Painting XP keeps counting towards **Painting Mastery**: a bar of 15,000 XP, shown in the Painting skill's tooltip.

The morning after you reach level 10, she writes to you with her old **Study Board** (if you haven't met her yet, it comes the morning after you first talk to her). Hang it on a wall: it fills with pinned studies a quarter at a time as the bar does, and is full when she's ready to test you. Right-click it (or hover it in your bag) for the exact numbers. If you lose it, Margot posts you another (at most once a week). Margot notices each quarter too, the next time you talk to her.

When the board is full, talk to Margot at the museum: she offers the test on your first word, before anything else she has to say. Say you're ready and she asks which mode you'd like to paint it in (Simplified, Relaxed, Normal or Hard; it's saved, like the easel's mode button). Then she sets you a **test restoration** at her table.
- **Gold or better** and she certifies you a conservator, with the game's mastery sparkle. You get the **Master's Easel**, an easel with 15% more light. A medium or large painting made at it at gold or better is a **living masterwork**: hung on a wall, it moves a little. A scene (a view painted outdoors, a restored old painting or an original) comes alive in its colours: glints drift across its water (slate, cerulean, aquamarine), firelight breathes in its reds and golds (crimson, ruby, marigold) and a breeze passes over its leaves (moss, leaf). Anything else (a still life, a portrait, an animal or a creature) catches a soft band of window light drifting across it now and then.
- **Below gold**, try again the next day.

The painting is **Zuzu at Dusk**, the Zuzu City Museum of Art's own, lent to her for testing conservators. It isn't one of the valley's 21: it goes back to the city either way, and isn't part of your collection.

## Restoring one yourself
Once you're a conservator, a damaged painting in your bag shows a **Restore** slot on any easel's setup screen.
- **The canvas** starts as the damaged painting, with the surviving paint already there. It's its own canvas, so none is used.
- **The reference** shows the painting as it was, whole. The canvas starts with what's left of it, and the missing cells are hatched, so bare canvas can't be mistaken for pale paint. Point at a gap to see its colour on the reference.
- **Scoring:** only the damaged cells count. The right colour counts fully, a near one half, and mixing purity counts as usual. Painting over surviving paint with a different colour costs you.
- **The light** is set from what's missing (the cells you have to paint), with a little more for matching each gap to the picture.
- **The result:** the painting, titled as it was ("The Old Mill"), at the quality you earned, restored by you.

## What a restored painting is worth
| Quality | Value |
|---|---|
| Normal | The set's value (above) |
| Silver | 1.25x |
| Gold | 1.5x |
| Iridium | 2x |

Sell it, keep it, frame it, or give it to the museum ({link("The Museum")}).
""")


def museum():
    page("The Museum", f"""
![The museum's collection](images/screens/collection_view.png)

Gunther takes restored old paintings for the museum: talk to him at his counter and choose **Donate a restored painting**, then pick the painting from your bag. Framed ones count too: he keeps the painting and gives you back the empty frame. They join the museum's own collection, and **View the painting collection** at the counter shows what's there and what's missing. See {link("Old Paintings and Restoration")} for finding and restoring them.

## Rewards
| Donated | Reward |
|---|---|
| The Old Valley set | Gallery furniture, each in its own letter from Gunther: a velvet rope, a gallery bench (you can sit on it) and a marble plinth, a little pedestal you can set an item on, like a table. Sable also sells marble plinths from Painting level 6, for 2,000g |
| The Wreck set | Two Driftwood Frames ({link("Frames")}) |
| The Miners set | Two Iron Frames |
| The Calico set | Two Sandstone Frames |
| The Dwarvish set | Two Dwarvish Frames |
| 6 paintings | A **living painting**: a mill on a river at dusk, its water shimmering and its window's lamp flickering |
| 12 paintings | **Margot's loupe**, worn as a ring: old paintings turn up half as often again |
| All 21 | A **paint-tube Junimo** statue, from Sable |

Each reward comes in the post the next morning. When a donation earns one, Gunther tells you so, and to check your mailbox.

## The Painting Wing
![The Painting Wing](images/screens/wing_inside.png)

The day after your first donation, a new wing opens through the doorway in the exhibit hall's east wall (until then it's roped off). Inside:
- **The Old Valley** hangs framed along the back wall.
- **Every other painting** stands on its own white marble display, with its card.
- **A place is kept** for each painting still missing, so you can see what's left to find.

{link("Margot Pell")} spends Tuesday and Thursday afternoons in the wing.
""")


def frames():
    where = {
        "plain": "Crafted; the art shop",
        "pine": "The art shop", "oak": "The art shop", "birch": "The art shop", "white": "The art shop",
        "walnut": "The art shop, from Painting level 3", "red-lacquer": "The art shop, from Painting level 5", "rope": "The art shop, from Painting level 7",
        "moonlit": "Sable's Night Market stall ({})".format(link("Festivals")),
        "driftwood": "The museum: the Wreck set", "iron": "The museum: the Miners set", "sandstone": "The museum: the Calico set",
        "gold-leaf": "The finished portfolio ({})".format(link("Portfolio and Back Room")),
        "dwarvish": "The museum: the Dwarvish set",
        "prismatic": "Qi's gem shop (25 Qi Gems)",
        "qi": "Qi's special order ({})".format(link("Special Orders")),
    }
    what = {
        "Light": "+{:g}% light", "Night": "+{:g}% light after 7pm", "Sparks": "Inspiration {:g}% sooner",
        "Thrift": "A {:g}% chance a cell's paint isn't used", "Learning": "+{:g}% Painting XP", "Value": "Paintings made there worth {:g}% more",
    }
    rows = [f'| {icon(f["id"])} | **{f["item"]}** | {f["rarity"]} | {what[f["trait"]].format(round(f["percent"], 1))} | {where.get(f["style"], "-")} |'
            for f in sorted(DATA["frames"], key=lambda f: f["steps"])]
    st = DATA["frameSteps"]
    page("Frames", item_infobox("Framer's Bench") + f"""
Put a painting in a frame and it hangs on a wall as furniture: a small painting takes one tile (its picture fitted to the frame), a medium one 2x2 and a large one 3x3. On the easel's setup screen, **Frame a painting** shows every frame you have; pick one, then the painting. Hovering a frame shows its rarity and what it does.

## What a frame does
A framed painting hung in a room **inspires the easels in that room**: paintings made there get its frame's bonus. Build a studio.
- Each frame style has one **trait**. Its **rarity** sets how strong it is: a common frame gives one step of its trait, uncommon two, rare three, epic four, legendary six.
- **The three rarest** framed paintings in a room count, and their bonuses add up.
- **At an easel you've placed**, the setup screen shows an icon for each bonus the room gives. Hover one to see how much.

| Trait | Each step |
|---|---|
| Light | +{st["light"] * 100:g}% light |
| Night | +{st["night"] * 100:g}% light, from 7pm |
| Sparks | Inspiration sparks come {st["sparks"] * 100:g}% sooner |
| Thrift | A {st["thrift"] * 100:g}% chance a cell's paint isn't used |
| Learning | +{st["learning"] * 100:g}% Painting XP |
| Value | Paintings made there are worth {st["value"] * 100:g}% more |

## Frame styles
| | Frame | Rarity | One hung | From |
|---|---|---|---|---|
""" + "\n".join(rows) + f"""

## The Framer's Bench
Takes a painting out of its frame, so frames can be collected and swapped.
1. Place the bench and use it while holding a framed painting.
2. The next morning, take out the painting, just as it was, and its empty frame.

A recipe at Painting level 4, or from the art shop. It works with Automate.
""")


def special_orders():
    page("Special Orders", f"""
Painting orders appear on the special orders board outside Pierre's, alongside the usual ones, and one at Qi's.

| From | Asks for | Needs | Reward |
|---|---|---|---|
| Lewis | Three portraits of villagers, for the town hall | Painting level 3 | 3,000g, a heart with Lewis, and four Picture Frames |
| Gus | Two gold (or better) paintings of food or drink, for the Saloon's walls | Painting level 2 | 2,000g, a heart with Gus, and three tubes of Ruby Red |
| Leah | A view of the Secret Woods | Painting level 4, once you've been there | 2,500g, a heart with Leah, and a Rigger Brush |
| Elliott | A view of the beach at night | Painting level 4 | 2,500g, a heart with Elliott, and two Moonlit Frames |
| Mr. Qi | Ship a Masterpiece of a legendary fish | Painting level 8 | 30 Qi Gems, and Qi's Frame ({link("Frames")}) |

Deliver the paintings one at a time. A painting counts by what's in it: a portrait of a villager, food or drink, a view of a place, at night, and so on.
""")


# ---------------------------------------------------------------- images

def images():
    out = os.path.join(WIKI, "images")
    for sub in ("icons", "screens", "page", "swatches"):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    for f in os.listdir(os.path.join(MEDIA, "icons")):
        im = Image.open(os.path.join(MEDIA, "icons", f)).convert("RGBA")
        im.resize((im.width * 4, im.height * 4), Image.NEAREST).save(os.path.join(out, "icons", f))
    for sub in ("screens", "page"):
        for f in os.listdir(os.path.join(MEDIA, sub)):
            shutil.copy(os.path.join(MEDIA, sub, f), os.path.join(out, sub, f))
    for p in DATA["pigments"]:
        h = p["hex"][1:]
        Image.new("RGBA", (24, 24), tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)).save(os.path.join(out, "swatches", h + ".png"))
    # Sable's neutral portrait, at 2x.
    sheet = Image.open(os.path.join(ROOT, "[CP] Pelican Palette", "assets", "sable", "portraits.png")).convert("RGBA")
    sheet.crop((0, 0, 64, 64)).resize((128, 128), Image.NEAREST).save(os.path.join(out, "sable_portrait.png"))
    margot = Image.open(os.path.join(ROOT, "[CP] Pelican Palette", "assets", "margot", "portraits.png")).convert("RGBA")
    margot.crop((0, 0, 64, 64)).resize((128, 128), Image.NEAREST).save(os.path.join(out, "margot_portrait.png"))
    julian = Image.open(os.path.join(ROOT, "[CP] Pelican Palette", "assets", "julian", "portraits.png")).convert("RGBA")
    julian.crop((0, 0, 64, 64)).resize((128, 128), Image.NEAREST).save(os.path.join(out, "julian_portrait.png"))
    # And in each outfit, at 4x, for the infobox's outfit row (shown small,
    # opened full size on a click).
    for outfit in SABLE_OUTFITS:
        sheet = Image.open(os.path.join(ROOT, "[CP] Pelican Palette", "assets", "sable", f"portraits_{outfit}.png")).convert("RGBA")
        sheet.crop((0, 0, 64, 64)).resize((256, 256), Image.NEAREST).save(os.path.join(out, f"sable_portrait_{outfit}.png"))


def main():
    for build in (home, getting_started, painting, canvases, quality, pigments, brushes, skill, selling, record,
                  back_room, commissions, consignment, varnishing, portraits, gifting, art_shop, crafting, sable, julian, festivals, multiplayer,
                  margot, old_paintings, museum, frames, special_orders,
                  configuration, compatibility, faq, translating, changelog, sidebar):
        build()
    if os.path.isdir(WIKI):
        for f in os.listdir(WIKI):
            if f.endswith(".md"):
                os.remove(os.path.join(WIKI, f))
    os.makedirs(WIKI, exist_ok=True)
    for name, text in PAGES.items():
        with open(os.path.join(WIKI, slug(name) + ".md"), "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    images()
    print(f"{len(PAGES)} pages")


if __name__ == "__main__":
    main()
