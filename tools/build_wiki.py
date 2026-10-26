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


def icon(qualified_id, size=32):
    return f'<img src="images/icons/{icon_file(qualified_id)}" width="{size}" height="{size}">'


def swatch(hex_colour):
    return f'<img src="images/swatches/{hex_colour[1:]}.png" width="24" height="24">'


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
- Sable, the former museum restorer who runs it.

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
| {link("Portraits and Plein Air")} | Painting friends and painting the view |
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

## The shop and the rest
| | |
|---|---|
| {link("Art Shop")} | Location, hours and stock |
| {link("Crafting")} | Every recipe |
| {link("Sable")} | Schedule, gifts, heart events and marriage |
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
2. Make sure you have a **Small Canvas** and a **Palette** in your bag. Sable gives you a Palette on your first visit to the {link("Art Shop")}.
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
  - or a **Free canvas**, with no reference and no score.
- **Preview:** shows the reference exactly as you'll copy it, with its **difficulty** (0-100) and how long the light lasts.
- **Brush:** cycles through the {link("Brushes")} in your bag.
- **Frame a painting:** puts one of your paintings in a Picture Frame, to hang as furniture.
- **Warnings:** under Begin, the screen tells you if you're out of canvases, if it's too late in the day (a painting can't run past 2am), or if your Palette is low on a colour the subject needs.

## Painting
The reference is pinned on the left; your canvas is in the middle; the palette and tools are on the right.

**Mixing.** Click a colour on the palette and a mixing bar opens, like fishing.
- The true colour drifts up and down the bar. Hold the mouse button to lift your mix, and let go to let it fall.
- Keep it on the true colour until the meter fills.
- A clean mix paints **pure** colour. A shaky one comes out **a bit off**, and a bad one **muddy** (visibly grimy, and worth less).
- Each colour moves in its own way: earth colours drift smoothly, minerals dart, and rare pigments are the hardest. See {link("Pigments and Paint")}.

**Painting.** Left-click or drag to paint with what's on the brush. A load covers a number of cells, set by your brush and level. Right-click scrapes paint off.

**The light.** A bar at the top runs down while you work.
- When it's gone, the painting is finished as it stands.
- Finish early and the light that's left becomes bonus experience.
- The light pauses while you mix, except on Hard.

**Inspiration sparks.** Every so often a spark appears on a cell of the canvas.
- Paint that cell its right colour, mixed pure, before the spark fades.
- Each spark you catch adds light.
- Catch three and the painting is **Inspired**, which raises its quality.
- Very rarely, a spark teaches you a rare pigment.

**Paint.** Every cell you paint uses one cell of paint from your Palette (except on Relaxed). See {link("Pigments and Paint")}.

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
        tech = {"None": "Flat colour", "Glaze": "Glazing", "Glaze, Highlight": "Glazing and highlights"}[c["techniques"]]
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

**Relaxed** difficulty paints flat colour on every canvas.

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
- **Relaxed** difficulty paints for free and needs no Palette.

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
        rows.append(f'| {img} | **{b["name"]}** | {b["load"]} | {", ".join(effects) or "-"} | {b["uses"] or "Never wears out"} | '
                    f'{ingredients(r) + "<br>(Level " + str(r["level"]) + ")" if r else "-"} | {gold(s["price"]) + " (" + level_text(s["condition"]) + ")" if s else "-"} | {desc} |')
    page("Brushes", f"""
Brushes are the painter's tackle. Pick one on the setup screen; the hog bristle brush is always there.

| | Brush | Load | Effects | Uses | Recipe | Art shop | |
|---|---|---|---|---|---|---|---|
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

The **novelty** price stops one easy subject being farmed all week. The weekly count resets every Monday, so no subject is ever used up for good. The results screen tells you when a painting is worth more for being new, or less for being familiar. See {link("Painting Record")}.

## What it earns, roughly
Net gold per in-game hour of painting (sale price minus canvas and paint, divided by the hours a painting takes), from the mod's own economy check:

| Painting | Per hour |
|---|---|
| A first parsnip (level 1, small, sold to Pierre) | about 140g |
| A still life (level 3, small, silver, at the art shop) | about 200g |
| A pair (level 5, medium, gold, Gallerist) | about 370g |
| A portrait (level 6, medium, gold) | about 530g |
| A trio (level 9, large, iridium) | about 820g |
| A view or portrait (level 9-10, large, iridium) | about 1,200g |
| A new friend's Masterpiece, varnished and consigned | about 3,300g |
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

## Novelty
A subject's first painting ever sells for more. Painting the same subject again and again in one week sells for less, and the count starts over every Monday; see {link("Selling and Prices")}.

## Milestones
| Different subjects | Reward |
|---|---|
{rows}
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
- **Glazed:** once medium canvases are open to you, about one in five asks for a painting with glazing. They pay 25% more. See {link("Canvases and Techniques")}.

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


def sable():
    s = DATA["sable"]
    def gifts(key):
        return "<br>".join(f'{icon(g["id"], 24)} {g["name"]}' for g in s[key]) or "-"
    box = infobox("Sable", '<img src="images/sable_portrait.png" width="128">', [
        ("Pronouns", "they/them"),
        ("Birthday", s["birthday"]),
        ("Lives in", "The art shop, Cindersap Forest"),
        ("Marriage", "Yes, by anyone"),
        ("Best gifts", gifts("loves")),
    ])
    page("Sable", box + f"""
**Sable** runs the art supply shop in Cindersap Forest.
- **Before the valley:** a restorer at the Zuzu City Museum of Art, fixing other people's masterpieces.
- **Why they came:** the museum sent them a damaged landscape of Cindersap Forest, signed only "E.". They couldn't stop thinking about the place in it, so they quit and moved here.
- **Character:** dry, exact and quietly funny. Honest about your work, never cruel.
- **The critique:** from 2 hearts, it's Sable who writes the critique on your results screen.

> "Rain's the best light there is. Flat and honest. Nothing to hide behind."

## Schedule
| Day | Where |
|---|---|
| Most days | Painting by the river in the morning (spring and summer), at the counter from 10am, painting in the shop's corner in the evening |
| Wednesday | Shop closed: the museum in the morning, Evelyn and George's in the afternoon |
| Friday | Painting outside the shop in the morning, the counter until 5pm, then the Saloon |
| Sunday | The beach in the morning, the counter from 1pm |
| Rain and winter | Indoors, painting until the shop opens |

Sable goes to every festival, and dresses for the weather: a coat and scarf outdoors in winter, a linen shirt on Ginger Island.

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
| 14 | The art shop, a sunny evening, once married | An exhibition |

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

**Shared, like any other object:**
- easels, Pigment Mills and Varnishing Cabinets, which anyone can use
- placed and framed paintings
- the art shop and Sable

**Time:** painting doesn't move the clock in multiplayer, because time is shared. The host's morning marks varnish as cured, for everyone.
""")


def configuration():
    page("Configuration", """
Change these with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), or in `Mods/PelicanPalette/PelicanPalette/config.json` after the game has run once.

| Setting | Default | What it does |
|---|---|---|
| `Difficulty` | `Normal` | **Relaxed:** no light meter, flat colour on every canvas, no paint needed, and each colour only needs mixing once.<br>**Normal:** mixed colours dry after a while.<br>**Hard:** the paint on the brush runs out, the light never pauses, and sparks fade fast; paintings pay 20% more. |
| `LightTimeMultiplier` | `1.0` | Gives you more (or less) light per painting, on top of the difficulty (0.5x to 3x). |
| `ArtLoversLikeAnyPainting` | `true` | Whether art lovers like even a normal-quality painting. |
""")


def compatibility():
    page("Compatibility", """
- **Stardew Valley** 1.6.15, with **SMAPI** 4.0 or later.
- **Required:** [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.9+ and [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348).
- **Optional:** [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).
- **Stardew Valley Expanded:** compatible. The shop, Sable's schedule, events and festival spots all fit SVE's maps, and the shop is drawn on SVE's world map too.
- **[Automate](https://www.nexusmods.com/stardewvalley/mods/1063):** Pigment Mills and Varnishing Cabinets work with it. A cabinet next to a chest takes an unvarnished painting and a tin of Varnish from it, and puts the painting back with the gallery finish after 28 days.
- **[NPC Map Locations](https://www.nexusmods.com/stardewvalley/mods/239):** Sable shows on the map, their marker cropped to their face.
- **[Remapping](https://www.nexusmods.com/stardewvalley/mods/29049) and map recolours:** the shop draws itself on the world map, in the same spot on vanilla and SVE maps, and its shadow takes on the colour of whatever ground is under it.
- **[Polyamory Sweet](https://www.nexusmods.com/stardewvalley/mods/20599):** Sable can be one of several spouses. The shop's trust and the 14-heart exhibition go by Sable's own relationship, not which spouse is "first".
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
- a Palette in your bag,
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
Any friend at 4 hearts or more, from medium canvases.

**How do I paint the view?**
Place an easel outdoors and use it, or use the one in the art shop's yard ([Portraits and Plein Air](Portraits-and-Plein-Air)).

**Can I change the difficulty?**
Yes ([Configuration](Configuration)).
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

**The valley**
- {link("Art Shop")}
- {link("Sable")}
- {link("Crafting")}

**Playing**
- {link("Multiplayer")}
- {link("Configuration")}
- {link("Compatibility")}
- {link("Translating")}
- {link("Changelog")}
""")
    page("_Footer", f"Pelican Palette {DATA['version']} by blinxxy. Item tables are generated from the mod's own data.")


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


def main():
    for build in (home, getting_started, painting, canvases, quality, pigments, brushes, skill, selling, record,
                  commissions, consignment, varnishing, portraits, gifting, art_shop, crafting, sable, multiplayer,
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
