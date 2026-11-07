# Pottery: the wheel (draft for review, 2026-11-07)

A first plan for a pottery minigame. Nothing here is decided yet; section 9 lists what is yours to decide.

## 1. Where it fits

- **Already decided:** there's no pottery skill. Clay is one of Sculpting's materials (1.5), and Tobin teaches clay and fires your pieces (`docs/tobin_brief.md`, `docs/design/1.4_plan.md` section 4).
- **What this adds:** sculpting's minigame is about **taking away**: you carve a block from the front and the side, and you can't put stone back. Clay on a wheel is the opposite. You **move** clay: nothing is cut off, and every push changes the shape somewhere else. That's different enough to need its own minigame. Sculpting's carving can still shape clay figures; **the wheel makes vessels**: cups, bowls, vases, jugs and planters.
- **Its place in the mod:** one more way to earn Sculpting XP, and the heart of Tobin's arc ("you learn clay from him").

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
5. **Firing.** Overnight in Tobin's kiln (his firing days are Tuesday and Friday), or your own kiln once you can build one. In the morning it's ready. Firing has a small chance of a **kiln gift**: a glaze that ran beautifully, a crackle, a flash of colour. It's a little luck, the way a Treasure Chest is in fishing.

## 4. Difficulty, as painting's

| Mode | What it asks |
|---|---|
| **Simplified** | No collapse: thin walls only lose points. Patience never runs out. A wide centring band. Worth half. |
| **Relaxed** | Normal with no time pressure anywhere. |
| **Normal** | One pressure per stage: the wobble in centring, patience in throwing, timing in trimming. The clay firms up while you pause. |
| **Hard** | The full juggle: you set the wheel's speed yourself (fast throws quicker but thins the walls faster), patience only comes back slowly, and the clay dries as you work, so you can't take forever. Worth the most. |

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

Two ways to apply it. Glazing has its own decision in section 9:
- **Simple:** choose one glaze, or two dipped halves.
- **Banding:** while the pot spins, you touch a glaze brush to it at a height and it paints a ring. You match the reference's bands of colour. This plays like painting, but in rings.

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
- **Clay** comes from digging (Clay is a vanilla item), from his pit, and from his shop.
- **Your own kiln:** a craftable kiln once you're good enough (a Sculpting level), so his firing days are a convenience, not a lock.

## 9. Decisions for you

1. **Skill:** clay stays a Sculpting material with no pottery skill (as decided), or pottery becomes its own skill.
2. **When:** with Sculpting in 1.5 (the current plan), or a smaller pottery-only piece earlier, in 1.4, as Tobin's thing.
3. **Glazing:** simple (choose a glaze) or banding (paint rings while it spins).
4. **Firing:** an overnight wait with a chance of a kiln gift (recommended), or a stoking minigame as well.
5. **Controls:** hands follow the mouse's height and you click a move (recommended), or you drag the profile directly.

## 10. How to build it, if you like it

As painting was: a **prototype first**. A plain dev-only screen (`museart_pottery`) with the three throwing moves, the patience meter and a reference profile, played and tuned before any art or items. Then a simulation of the judge (`ArtStudio.Sim`, as painting's stars were calibrated), then the art, items, kiln and Tobin.
