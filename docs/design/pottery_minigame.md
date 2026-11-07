# Pottery: the wheel (plan, 2026-11-07)

A plan for pottery and its minigame. The user's decisions so far (2026-11-07):
- **Pottery is its own skill**, not a Sculpting material. This reverses the earlier "no pottery skill" rule.
- **It comes with Sculpting in 1.5.**
- **Glazing is banding:** you paint rings of glaze onto the spinning pot.
- **Firing has a stoking minigame.**

Section 9 lists what's still open.

## 1. Where it fits

- **A third skill:** Pottery, from 1 to 10 with six professions (section 8b), beside Painting and Sculpting, all in 1.5. Tobin teaches it and fires your pieces, and his arc is told through it (`docs/tobin_brief.md`).
- **Different from sculpting:** sculpting's minigame is about **taking away**: you carve a block from the front and the side, and you can't put stone back. Clay on a wheel is the opposite. You **move** clay: nothing is cut off, and every push changes the shape somewhere else. **The wheel makes vessels**: cups, bowls, vases, jugs and planters.
- **Clay moves to Pottery:** Sculpting keeps wood, stone, marble and ice (`1.4_plan.md` section 4's materials table, updated).

## 2. The idea in one line

**You throw a pot to match a reference silhouette, with a fixed lump of clay that you can only move, never add to.**

A pot on a wheel is round, so the whole shape is one **profile**: the wall's outline from foot to rim. The reference is a profile; you draw yours with your hands on the spinning clay. And clay is conserved: to make a tall vase you have to pull the walls up, which makes them thinner. Too thin and they collapse; too wet and they slump. That's the one tension the whole game is built on, the way paint and light are painting's.

## 3. The stages, one at a time

The mod's rule for minigames applies: by default each stage asks one thing of you, and the stacked pressures are for Hard (`feedback: minigame load`).

1. **Centring.** The lump wobbles on the wheel. You hold it steady: a bar like fishing's, where you keep a wobble marker inside a band as it drifts. Real potters call centring the hardest part, and here it decides how true the pot is. An off-centre pot has walls that thicken and thin as it turns, which costs a little at the end. It's short: about 10 seconds.
2. **Opening and pulling.** This is the heart of it. The pot is shown from the front, spinning, with the reference profile as a faint outline behind it. Your hands are at a height on the wall (the mouse's height, or the stick). Three moves:
   - **Pull up:** draws clay up from below. The pot gets taller and the walls thinner.
   - **Press in:** narrows the pot at that height (a neck, a waist).
   - **Let out:** swells it from inside (a belly).

   Every move uses up the clay's **patience**: it gets wetter and weaker. A meter shows how much handling it has left. When it's low, the walls start to sag. You can stop and let it firm up for a moment (Normal), but the wheel waits for you: there's no clock.
3. **Trimming and the rim.** A short finishing pass: trim the foot and compress the rim, one rhythm stage of three or four timed presses, like painting's finishing touches. It sets the **finish**: rough, clean or fine.
4. **Glazing (at the kiln, later).** The pot is left to dry. When you take it to the kiln you choose a glaze and how it's applied (section 5).
5. **Firing: stoking the kiln.** At the kiln you load your glazed pots, then **stoke**: the kiln's heat rises and falls on a gauge, and you feed it fuel to keep it inside the glaze's band as the firing climbs to its peak. It's like fishing's bar, but slower and steadier, about 30 seconds.
   - **Fuel:** Wood, Hardwood (burns long and steady) or Coal (hot and fast). That's why Tobin loves Hardwood, and it gives fuel a use.
   - **Outcome:** the time inside the band sets the glaze's result: underfired is dull, well fired is true, and a perfect firing has a better chance of a **kiln gift** (a glaze that ran beautifully, a crackle, a flash of colour). Overfiring can crack a pot, but only in Hard; elsewhere it just dulls the glaze.
   - **Then overnight:** the pots cool in the kiln and are ready in the morning.
   - **At Tobin's:** on his firing days (Tuesday and Friday) he stokes your pots in with his own, safely but without the perfect-firing bonus, or you can stoke them yourself.

## 4. Difficulty, as painting's

| Mode | What it asks |
|---|---|
| **Simplified** | No collapse: thin walls only lose points. Patience never runs out. Wide bands for centring, banding and stoking. Worth half. |
| **Relaxed** | Normal with no time pressure anywhere. |
| **Normal** | One pressure per stage: the wobble in centring, patience in throwing, timing in trimming, steady hands in banding, the heat in stoking. The clay firms up while you pause. |
| **Hard** | The full juggle: you set the wheel's speed yourself (fast throws quicker but thins the walls faster), patience only comes back slowly, the clay dries as you work, and overfiring cracks pots. Worth the most. |

## 5. Glaze

Glazes come from **minerals**, which is already what Tobin likes as gifts ("minerals and crystals, for glazes"). A first set:

| Glaze | From | Look |
|---|---|---|
| Clear | Quartz | The clay's own colour, shining |
| Celadon | Jade | Sea green |
| Tenmoku | Iron Ore | Deep brown-black |
| Cobalt | Aquamarine | Blue |
| Ash | Coal, or Fern's dried flowers | Speckled, earthy |
| Copper red | Fire Quartz | Red, rare |

**Banding (decided):** the pot spins on the banding wheel. You pick a glaze, touch the brush to the pot at a height and hold it; it paints a ring as wide as you hold it. The reference shows the bands to match: colours, heights and widths. It plays like painting, but in rings. Your hand drifts slightly as the wheel turns, and keeping it steady is this stage's one pressure, so a wobbly hand gives a wavy band. A base coat (dipping the whole pot) comes first, as one click.

Later there's a bridge to painting, like **painted sculpture** (`1.4_plan.md` section 5B): decorating a fired pot at the easel, its surface unwrapped as a canvas.

## 6. Judging

Scored like painting (`PaintingJudge`), from 0 to 5 stars:
- **Shape:** how well your profile matches the reference's, measured on the whole silhouette. A few pixels out counts less than a wrong shape.
- **Walls:** how even and how fine. Even and thin is best; thick and lumpy loses some; off-centre loses a little.
- **Finish:** from the trimming stage.
- **Glaze:** how closely the bands match the reference (banding only).

Quality and the Sculpting skill's level then work as painting's do.

## 7. What you make, and why it's worth making

- **Sizes, like canvases:** a cup or small bowl (small), a vase or jug (medium), and a large urn or planter (large). They take more clay, and the large one needs the most handling.
- **Furniture:** every piece is furniture, in its glaze and star quality. Bowls on tables, vases with flowers in them, planters by the door.
- **Useful too (Tobin's whole point):**
  - A fired **planter** works as a Garden Pot.
  - A **matching set** (four cups thrown to one profile within a tolerance) is a real potter's skill. Bea's commission for the inn asks for exactly that.
- **Sold and gifted:** at Tobin's (on consignment, like Sable's), or sold directly. Pieces are gifted under the same "nobody dislikes art" rule as paintings, with Tobin, Fern and Bea fond of good pottery.
- **Commissions:** pottery orders on the commission board and from the Vale's residents.

## 8. Tobin, and how you learn it

- **2 hearts (his brief):** he shows you the wheel. "You're bad at it; so was he." This event teaches the minigame, and the first throw is guided.
- **4 hearts:** your first piece comes out of his kiln.
- **Clay: buy it or dig it** (the user, 2026-11-07, "similar to Graveyard Keeper"). Throwing clay is its own item, **Potter's Clay**, and it has two sources only:
  - **Buy it from Tobin**, year-round, a lump at a time.
  - **Dig it yourself at his clay pit** on the riverbank below the pottery (the clay bank on the Vale's map). Use a hoe or shovel on the pit's face, at a cost in energy. Each dig gives a lump or two, with a chance of a pebble, an old shard or a buried artifact. The pit has so many digs a day and refills overnight, more after rain. It's frozen in winter, when Tobin's shop is the only source. He doesn't mind you digging: "Take what you need. Fill the holes."
  - **The vanilla Clay** you turn up tilling isn't throwing clay: it's grit and roots. Tobin can explain, and that doesn't change it.
  - **How much:** a small piece takes 1 lump, a medium 2 and a large 4.
- **Your own kiln:** a craftable kiln at a Pottery level, so his firing days are a convenience, not a lock.

## 8b. The Pottery skill (a first pass)

A SpaceCore skill from 1 to 10, like Painting. XP comes from throwing, glazing and firing; higher levels unlock larger pieces, more glazes, the kiln and the potter's wheel as furniture for your own house.

| Level 5 | Level 10 (pick one of two) |
|---|---|
| **Thrower**: pots sell for 25% more | **Production Potter**: patience lasts 25% longer, and a small lump makes two cups. **Master of Sets**: matching sets are worth double, with a wider tolerance |
| **Glazer**: banding's steadiness band is wider | **Kiln Keeper**: stoking's band is wider, and kiln gifts come twice as often. **Alchemist**: rare glazes from common minerals (Copper Red from Copper Ore) |

## 9. Decisions for you

Decided (2026-11-07): its own skill; with Sculpting in 1.5; banding; a stoking minigame. Still open:
1. **Controls:** hands follow the mouse's height and you click a move (recommended), or you drag the profile directly.
2. **Professions:** the first pass in section 8b.
3. **Hand-building:** whether Pottery also makes clay figures and tiles by hand, besides the wheel's vessels, or leaves figures to Sculpting's carving in wood and stone.
4. **Three skills in one release:** 1.5 now carries Sculpting and Pottery. It may want splitting (Pottery in 1.6, say) once both are prototyped.

## 10. How to build it, if you like it

As painting was: a **prototype first**. A plain dev-only screen (`museart_pottery`) with centring, the three throwing moves, the patience meter and a reference profile, played and tuned before any art or items. Then banding and stoking the same way. Then a simulation of the judge (`ArtStudio.Sim`, as painting's stars were calibrated), then the skill, art, items, kiln and Tobin.
