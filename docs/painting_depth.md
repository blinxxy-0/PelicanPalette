# Deeper painting: options

Planning notes, 2026-10-25.

**Status:** you chose **C**. Built:
- Painting takes time (section 3).
- Pigments are knowledge (1A).
- Paint as a resource (1C): the Palette with 300-cell wells, 100-cell tubes priced 8/12/20/60g, and the Pigment Mill (`ArtItems.MillRecipes`).
- Relaxed difficulty paints for free.

Not built yet: section 2's price check against real playtests, and everything in section 4. Each section gives the options and a recommendation, then lists the decisions for you.

## Where it stands

- **Pigments:** a pigment jar is an inventory item, but the first time you open the easel with it, the colour is unlocked forever (saved on your farmer) and the jar is gone. The jar is just a delivery wrapper for a permanent unlock. That's why it feels wrong.
- **What a painting costs to make:**
  - a canvas: 25g / 80g / 200g at the shop, or crafted (the small one from 5 fiber and 2 wood)
  - brush wear: a brush lasts 20 paintings
  - no paint costs anything
  - no in-game time passes: the easel is a menu, and menus stop the clock
- **What it sells for:** canvas base × difficulty × subject price × level, then the game's quality multiplier. For a mid-difficulty subject:

  | Canvas | Normal | Gold | Iridium (level 10) |
  |---|---|---|---|
  | Small, 20×20 | ~150g | ~225g | ~350g |
  | Medium, 28×28 | ~350g | ~530g | ~800g |
  | Large, 36×36 | ~790g | ~1,190g | ~1,700g |

  The art shop pays 20% more.

So every painting is nearly pure profit, limited only by canvases, and it takes no game time. Any added complexity has to fix that first. Otherwise it's just friction on a money printer.

## 1. Pigments: what you own, and what you spend

**A. Knowledge only (tidy up what we have).** Buying a pigment teaches you the colour, the way a recipe or a wallet item works. The shop sells it once ("Learn: Rose"), and it never appears in your bag. Painting stays free to run.
- Pros: simple, honest, and no economy changes.
- Cons: adds no depth.

**B. Consumable paint.** Every colour is a finite supply, and painting uses it up.
- Pros: real resource play.
- Cons: you'd have to restock before every painting, and buying every colour every time gets tedious.

**C. Know the colour, spend the paint (recommended).** The two ideas combined:
- **Knowing a colour** is a permanent unlock, as in A. It's what lets you mix it on the bar.
- **Paint is what you spend.** You carry a **Palette**, a single tool-like item with wells for the colours you've loaded. Each well holds a number of brush loads.
- **One mix uses one load.** A load already covers a set number of cells (brush + level, about 6–16), so a small painting takes roughly 25–60 loads in all, a large one 3–4 times that. A muddy mix still costs a load, which gives "mix cleanly" a new reason.
- **Refilling** uses **paint tubes**, a stackable consumable per colour, filled into the Palette at the easel.
- **Tubes** come from two places:
  - Sable sells the common colours cheap, and the rare ones dear.
  - A **Pigment Mill**, a craftable artisan machine like a Preserves Jar, grinds farm, forage and mine goods into tubes overnight:
    - clay → Sienna, ochre-coloured forage → Ochre
    - flowers → Rose, Marigold, Plum
    - Amethyst → Iridium Violet, Aquamarine → Aquamarine, Ruby → Ruby Red
  
  This ties painting into the rest of the farm, the way kegs tie into crops.

Variants of C to choose between:
- **C1: shared paint.** One generic "paint" pool instead of per-colour wells. Simpler, but colour choice stops mattering.
- **C2: a bigger Palette** as a later upgrade or Painting-skill perk (more wells, deeper wells).
- **C3: easy mode.** Relaxed difficulty ignores paint entirely. This keeps your rule that the default modes don't stack pressures. Resource prep happens outside the easel either way, so Normal stays fair.

## 2. Keeping painting profitable

A rule to price everything by: **at shop prices, the paint for a painting should cost at most about 15% of what it sells for at normal quality.** Milled paint should cost you materials and time, not gold.

| Canvas | Normal sale | Paint budget (15%) | Loads used | Price per load |
|---|---|---|---|---|
| Small | ~150g | ~22g | ~40 | ~0.5g |
| Medium | ~350g | ~52g | ~90 | ~0.6g |
| Large | ~790g | ~120g | ~150 | ~0.8g |

So a **tube of a common colour (about 20 loads) sells for about 10–15g**, and a rare one (Ruby, Violet, Aquamarine) for about 60–100g. Rare colours are used sparingly, only where the subject needs them, so the per-painting cost holds.

What that means for the shop:
- **Pigment unlocks** become one-time teaching costs. They can stay roughly at today's prices (120–1,500g) because you pay them once.
- **The pressure goes into the paint.** A painting that comes out poorly still covers its paint. A good one profits well.

## 3. Painting takes time (recommended, whatever you choose above)

The easel should cost in-game time, like fishing or chopping wood:
- **Duration:** about 1 hour for a small canvas, 2 for medium, 3 for large. The clock jumps forward when you finish, with a short fade.
- **Daylight:** painting in the evening carries on into the night.
- **Why:** this caps how many paintings a day can produce, so the prices above can stay generous. It's the single biggest balance lever, and it costs the minigame nothing.

## 4. Other ways to add depth

Ordered by how much depth each adds against how much work it is:

1. **Sell where it pays.** Hang finished paintings in Sable's gallery on consignment. They sell over the following days for more than Pierre pays (a slower, better route). Paintings of a subject that's rarely painted pay a novelty bonus, and the 10th parsnip still life pays less. That stops one easy subject being farmed forever.
2. **Plein air: paint the real scene.** An easel placed outdoors offers "this view" as a subject: the tiles in front of it, downscaled to the canvas the way item references are. Season, weather and time of day change the scene. A rare view is worth more, such as the beach in a storm or the mountain lake at dusk. It gives landscapes their own life and gives you a reason to carry the easel around.
3. **Finishing.** Varnish is a cheap craft that adds a little value and stops a painting fading. Hung paintings could fade a little over seasons if left unvarnished. That's optional and probably fussy.
4. **Better canvases.** A linen canvas (crafted from cloth) gives a small accuracy edge, or raises the quality cap one step, at a higher cost.
5. **A painting collection.** A collection page of subjects you've painted, like the fish collection, with milestone rewards: a Palette upgrade, a rare pigment, a letter from Sable.
6. **New techniques in the minigame:** glazing over a colour, palette-knife texture, layering. These deepen the moment-to-moment play, but they add load to the minigame. Under the one-thing-at-a-time rule, they'd be Hard-only or skill unlocks.

## Suggested order

1. Painting takes time (section 3), plus pigments as knowledge (1A). Small, and it fixes the economy and the odd jars straight away.
2. Paint as a resource (1C): the Palette, tubes, the Pigment Mill, and repricing the shop by section 2.
3. Consignment and the novelty bonus (4.1).
4. Plein air (4.2), the big one.
5. The collection page (4.5), and the rest if wanted.

## Decisions for you

- Pigment model: A, B or **C** (and, if C, per-colour wells or a single shared pool).
- Should painting take in-game time? About how long per canvas size?
- Should Relaxed difficulty skip paint costs?
- Which items from section 4 are in, and in what order?
