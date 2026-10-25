# Deeper painting: options

Planning notes, 2026-10-25.

**Status:** you chose **C**. Built:
- Painting takes time (section 3).
- Pigments are knowledge (1A).
- Paint as a resource (1C): the Palette with 300-cell wells, 100-cell tubes priced 8/12/20/60g, and the Pigment Mill (`ArtItems.MillRecipes`).
- Relaxed difficulty paints for free.

Also built (2026-10-25), at your request: **portraits of villagers** (`Painting/Sitters.cs`).
- From 4 hearts, on medium or large canvases.
- They take an hour longer, sell for 1.3x (valued like a loved gift, `PaintingRules.PortraitSubjectPrice`) and give 1.3x XP. Tuned down from 1.75x by the economy check below.
- The sitter loves receiving theirs.
- Portrait commissions come only from friends at 4+ hearts.

Also built (2026-10-25), from section 4:
- **Novelty pricing and the painting record** (4.1, 4.5; `Painting/Catalogue.cs`, `Menus/CollectionMenu.cs`):
  - A subject's first painting sells for 1.25x. The 2nd and 3rd sell at the normal price, the 4th to 6th for 0.85x, and after that 0.7x. A still life of the same items in another order counts as the same subject.
  - The record page is at Sable's counter. Milestones for different subjects painted:
    - 5: three aquamarine tubes
    - 15: 450-cell wells
    - 30: paintings worth 10% more
    - 50: a letter from Sable with an iridium brush
- **Consignment** (4.1; `Shop/Consignment.cs`, `Shop/ConsignMenu.cs`):
  - Up to 3 paintings, shown on the shop's easels.
  - Each sells in 1 to 3 days for 1.35x the base price (Sable's counter pays 1.2x). Iridium sells by the next day, gold in 1 to 2 days.
  - You can take one back any time before it sells.
- **Plein air** (4.2; `Painting/Views.cs`):
  - An easel placed outdoors offers the view: 16x16 tiles of the game's own map render, mostly in front of the easel, reduced to the canvas in your colours.
  - It's timed and valued like a portrait. The subject price depends on the place (farm 200 up to the Summit and Desert at 800), raised for rain, winter and evening.
  - The same place in another season is a new subject.

Also built (2026-10-25), from 4.3, 4.4 and 4.6 as you chose them:
- **Varnish** (`Painting/Varnish.cs`): a Varnishing Cabinet at level 7 works like a cask. Satin after 7 days (x1.1), gloss after 14 (x1.2), a gallery finish after 28 (x1.3). Hit it with a tool to take a painting out early.
- **Techniques by canvas** (`Core/Technique.cs`): glazing on medium canvases, highlights on large ones. The reference marks the cells: a glaze where a darker wash over an owned colour matches the picture clearly better than any plain colour, and a highlight on the brightest spots (a few per picture). The judge's Technique score, an F-score of marks in place, takes up to 15% off the overall, which costs about a star.
- **Linen and Masterpieces**: linen canvases at level 8. A Masterpiece needs linen, iridium, five stars, Inspired and Technique of at least 0.9. It's worth x1.5, still counts as iridium to the game, gives x1.5 friendship as a gift, and Sable has a line for one.

**The economy check** (`dotnet run -- economy` in ArtStudio.Sim, `Economy.cs`; all multipliers now live in `Core/Market.cs`). Net gold per in-game hour of painting:

| Painting | per hour |
|---|---|
| First parsnip (level 1, to Pierre) | 136 |
| Early still life (level 3, silver, at Sable's) | 196 |
| Mid still life (level 5, medium, gold, Gallerist) | 367 |
| Mid portrait (level 6, medium, gold) | 527 |
| Late still life (level 9, large, iridium) | 820 |
| Late view or portrait (large, iridium) | ~1,225 |
| ...a new sitter's Masterpiece, varnished, consigned | 3,301 |

For scale: early fishing is about 150g an hour, mid-game fishing or the Mines about 500, and late Skull Cavern runs about 2,000. Before tuning, a late portrait made 2,125g an hour, and the top case made 7,467g an hour (31,000g for one painting). Changes:
- Portraits' bonus went from 1.75x to 1.3x, and their subject value from 1000g to 400g.
- Consignment went from 1.5x to 1.35x.
- Varnish went from 1.2/1.35/1.5 to 1.1/1.2/1.3.

Not built yet: checking these prices against a real playthrough. Each section gives the options and a recommendation, then lists the decisions for you.

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
