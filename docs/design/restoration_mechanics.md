# Painting Mastery and restoration: mechanics (1.3, step 5)

Written 2026-11-01. The plan is `docs/design/1.3_plan.md` section 4; this is
how it works, in enough detail to build. **Approved by the user 2026-11-01**,
with the three answers below.

## 1. Claiming Painting Mastery

**Not in the Mastery Cave (decided 2026-11-01).** The cave opens only once
all five vanilla skills reach 10, and its five mastery levels are all spoken
for. Painting Mastery is earned **at the museum, with Margot**: restoration
is museum work, and she's the one who'd know.

- Once Painting is level 10, Painting XP keeps counting, towards a
  Painting Mastery bar (**15,000 XP**, about a season of steady painting).
  It shows in the Painting skill's tooltip.
- With the bar full, Margot offers you **a test restoration**: an old
  painting of hers, restored at her table in the museum. Restore it to at
  least gold and she certifies you a conservator: Painting Mastery, with a
  short scene and the vanilla mastery sparkle and sound.
- **The reward:** restoring paintings yourself (section 4), and a mastery
  item like the vanilla ones: the **Master's Easel**, an easel that gives a
  little more light per painting.
- It needs Margot, so it's built with or after her (step 6).

## 2. Damaged paintings

**The item.** "Damaged Painting", one item whose modData says which old
painting it is. Its icon is the painting, flaked and stained. Its
description names the set it's from ("An oilcloth case from a wreck").

**Sets.** Each source has its own set; together they're the museum's
collection (section 5). 21 paintings:

| Set | Where it turns up | Paintings | Canvas |
|---|---|---|---|
| The Old Valley | Artifact spots, 1.5% a dig | 5: the town square, a farm, the old mill, Lewis young, a harvest | Medium |
| The Wreck | Fishing treasure chests, 4% a chest | 5: a ship's portrait, seascapes, the captain | Medium |
| The Miners | Mine crates and barrels, 0.6% (more deeper) | 5: the mining town, a miner's family, a Sunday picnic, the mine entrance, the crystal cavern | Medium |
| Calico | Skull Cavern crates, 1%; treasure rooms 10% | 4: the old city, a market, a desert queen, a caravan | Large |
| Dwarvish | Volcano chests, 8% | 2: a forge, a dwarf king | Large |

The Travelling Cart sometimes sells one from any set (1 day in 6, 3,000g),
for anyone unlucky. **The first one is guaranteed:** the first fishing
treasure chest or artifact spot after you reach Painting level 3.

**The Old Valley's art is chosen (2026-11-01):** `ArtStudio/assets/old_paintings/valley_*.png`, from PixelLab (the town square, a farm, the old mill, Lewis young, the harvest), 28 by 28 in the mod's pigments.

**The Wreck's art is chosen (2026-11-01):** `wreck_*.png` (a ship's portrait, the storm, the harbour at dawn, the captain, the lighthouse at dusk).

**The Miners' art is chosen (2026-11-01):** `miners_*.png` (the mining town, a miner's family, a Sunday picnic in place of the lamp room, which read too dark at 28 cells, the mine entrance, the crystal cavern).

**Calico's and the Dwarvish art are chosen (2026-11-01):** `calico_*.png` (the city, the market, the queen, the caravan) and `dwarvish_*.png` (the forge, the king), 36 by 36. All 21 old paintings are done.

**The art.** 21 small pixel paintings at canvas size (28 or 36 cells),
in the mod's own pigments, in an old-master look per set. Made with
PixelLab and reduced to the palette, shown to you a set at a time. Each
painting has a fixed **damage**: flaked patches, a tear, a water stain,
covering about a third of it.

## 3. Restoring with Margot (before Mastery)

Margot arrives the week after your first find (the plan's section 6).
At the museum, she'll restore a damaged painting for a fee, taking three
days, as Clint breaks geodes:

| Set | Fee |
|---|---|
| The Old Valley | 600g |
| The Wreck, The Miners | 800g |
| Calico | 1,500g |
| Dwarvish | 2,500g |

Her restorations come back **silver** quality: careful, but cautious.

## 4. Restoring it yourself (with Mastery)

- At any easel, a damaged painting in your bag shows a **Restore** slot on
  the setup screen, beside the views and creatures.
- **Painting it:** the canvas starts as the damaged painting: the surviving
  cells already there, the damaged ones bare. The reference panel shows
  the painting **as it is**, damage and all: you work out what's missing
  from what's left, as Margot does ("matching what's missing to what's
  left"). Hovering a damaged cell outlines it on both, as now.
- **Scored on the damaged cells only:** the right pigment counts fully, a
  near one ("a bit off") half, mixing purity as usual. Painting over a
  surviving cell with a different colour costs you. The light, the
  canvas's size and the difficulty mode work as for any painting.
- **The result:** the restored painting, at the quality you earned (up to
  iridium), titled as the original ("The Old Mill"), its provenance
  "Painted by an unknown artist, c. 1890. Restored by <you>, <date>."

## 5. What restored paintings are for

- **Sell them:** worth a set's base (Old Valley 1,500g up to Dwarvish
  6,000g), times quality. Much more than your own paintings.
- **Keep them,** framed in any frame.
- **Donate them** to the museum: Gunther's counter gets "Donate a restored
  painting". They hang in the museum, in the collection:

| Donated | Reward |
|---|---|
| A whole set | That set's frame style: driftwood (Wreck), iron (Miners), sandstone (Calico), Dwarvish (Dwarvish); the Old Valley brings gallery furniture (a velvet rope, a bench, a picture light) |
| 6 paintings | A **living painting**: an old painting whose picture moves a little |
| 12 paintings | **Margot's loupe**, a trinket |
| All 21 | The **paint-tube Junimo statue** (furniture), and the museum's new **wing** for the collection |

## 6. Build order within step 5

1. The damaged painting item, its sets and the art (shown to you set by set).
2. Finding them (the sources and the guaranteed first).
3. Restoring at the easel, and the restored painting.
4. The mastery bar and the Master's Easel (Margot's test restoration comes with her, step 6).
5. Donating and the collection's rewards.
6. Margot's fee service waits for Margot herself (step 6), or comes first
   with a stand-in (see question 3).

## Decided (2026-11-01)

1. **Where mastery is claimed:** not the Mastery Cave after all (the user,
   reconsidering): at the museum, with Margot, through a test restoration
   (section 1).
2. **The art:** PixelLab-generated, reduced to the palette, shown and
   approved a set at a time.
3. **Before Margot exists:** Margot (step 6) is built before her fee
   service, so it's hers from day one. Step 5's parts 1 to 5 come first,
   then Margot, then the fee service.

## Built so far (2026-11-01)

Parts 1 to 3 (`Painting/OldPaintings.cs`, `Painting/OldPaintingFinds.cs`,
`Core/RestorationJudge.cs`, `data/old_paintings.json`, `museart_oldtest`):
the 21 paintings and their fixed damage (33 to 36% each), the damaged
painting items, finding them (the rates above, the Travelling Cart, the
guaranteed first from Painting level 3), and restoring at the easel. Two
things added while building: the damaged cells still bare are hatched on
the reference and the canvas, so missing paint can't be mistaken for pale
paint; and a restoration gets half again the usual light, since what's
missing has to be worked out as well as painted. Restoring is unlocked by
the Painting Mastery flag, which Margot's test restoration will set
(step 6); until then only the dev test sets it.

Parts 4 and 5 (`Social/MuseumCollection.cs`, `tools/art/masters_easel.py`,
`tools/art/set_frames.py`, `museart_collectiontest`): the mastery bar
(Painting XP past level 10, shown in the skill's tooltip; Margot's test
restoration reads it in step 6), the Master's Easel (15% more light), the
four set frames (driftwood, iron, sandstone, Dwarvish), and donating to
Gunther: the collection view, and each finished set's frames by letter.
The framing screen shows its style buttons in two rows once you have more
than four.

**Still to make:** the Old Valley's gallery furniture, the living painting
(6 donated), Margot's loupe (12) and the Junimo statue and museum wing
(21). Their rewards are wired: each letter is sent as soon as it exists in
the game's mail, so adding one reaches players who already qualify.

