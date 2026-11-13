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
| **Hard** | The full juggle: you set the wheel's speed yourself (fast throws quicker but thins the walls faster), patience runs out in under half Normal's time and comes back a little slower, clay worked on past it gives way (so a throw needs a rest or two), the clay dries as you work, and overfiring cracks pots. Worth the most. |

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
  - **Built (2026-11-11, dev only):** `blinxxy.MuseArt_PottersClay` (placeholder icon, `tools/art/potters_clay.py`), 80g a lump at Tobin's counter, sells for 25g. The pit is the clay bank's grey pit (Vale tiles x 53-57, y 23-25 and x 54-56, y 26): hoe it from the sand round it. A dig costs 4 energy (a field's hoe swing costs 2) and gives 1 lump (60%) or 2 (40%), plus a Stone (12%), a Chipped Amphora (5%) or an Arrowhead, Ancient Doll, Rusty Spoon or Bone Flute (2%). 6 digs a day for everyone together, 9 after rain or in it; frozen in winter. Right-click it for Tobin's note and the digs left. The teaching wheel (the pottery's second wheel, 17-18, 19) asks for a shape: cup or bowl 1 lump, vase or jug 2, taken before the wheel opens. The 2-heart gate is a marked stub (`ValeClay.WheelOpenTo`). Code `ArtStudio/Vale/ValeClay.cs`, data `[CP] Gilded Vale (dev)/data/vale_clay.json`, checks `museart_claytest`.
  - **A third source, the Slaking Tub (the user, 2026-11-11: "a machine that converts normal clay to potters clay"):** a big craftable machine (`Data/Machines`, so it works like a Keg): 3 Clay in, 1 Potter's Clay out, 8 hours later (filled in the morning, ready by evening; at night, by morning). Its recipe is 1,000g at Tobin's counter and takes 30 Wood, 1 Copper Bar and 10 Clay. Placeholder sprite, `tools/art/slaking_tub.py`.
- **Your own kiln:** a craftable kiln at a Pottery level, so his firing days are a convenience, not a lock.

## 8b. The Pottery skill (a first pass)

A SpaceCore skill from 1 to 10, like Painting. XP comes from throwing, glazing and firing; higher levels unlock larger pieces, more glazes, the kiln and the potter's wheel as furniture for your own house.

| Level 5 | Level 10 (pick one of two) |
|---|---|
| **Thrower**: pots sell for 25% more | **Production Potter**: patience lasts 25% longer, and a small lump makes two cups. **Master of Sets**: matching sets are worth double, with a wider tolerance |
| **Glazer**: banding's steadiness band is wider | **Kiln Keeper**: stoking's band is wider, and kiln gifts come twice as often. **Alchemist**: rare glazes from common minerals (Copper Red from Copper Ore) |

## 9. Decisions for you

Decided (2026-11-07): its own skill; banding; a stoking minigame. Decided (2026-11-12): **Pottery ships in 1.5 and Sculpting moves to 1.6**; **the wheel only** (vessels: no hand-built figures or tiles; figures stay Sculpting's); the professions as first drafted (built); Hard's patience shortened, then patience nearly permanent on every difficulty (section 10). Still open:
1. **Controls:** hands follow the mouse's height and you click a move (recommended), or you drag the profile directly.
2. **Professions:** the first pass in section 8b.
3. **Hand-building:** whether Pottery also makes clay figures and tiles by hand, besides the wheel's vessels, or leaves figures to Sculpting's carving in wood and stone.
4. **Difficulty in throwing:** decided 2026-11-11, Hard's patience is shortened so a throw needs a rest or two (section 10). Whether Normal's should follow is open, once you've played Hard.
5. **The jug's outline:** pulling up without shaping already gets 4 stars. A fuller belly would make it ask for shaping.
6. **Three skills in one release:** 1.5 now carries Sculpting and Pottery. It may want splitting (Pottery in 1.6, say) once both are prototyped.

## 10. How to build it, if you like it

As painting was: a **prototype first**. A plain dev-only screen (`museart_pottery`) with centring, the three throwing moves, the patience meter and a reference profile, played and tuned before any art or items. Then banding and stoking the same way. Then a simulation of the judge (`ArtStudio.Sim`, as painting's stars were calibrated), then the skill, art, items, kiln and Tobin.

**Prototype (2026-11-07):** `museart_pottery`, Debug only (`ArtStudio/Pottery/`). It has centring, throwing, four shapes and a judge. The user loved it, and was stuck at 3.5 stars until walls were judged as a potter would (a thick foot and firm rim allowed, changing smoothly), shown live, and explained on the results.

**Banding prototype (2026-11-07):** "Glaze it" on the results, or `museart_pottery vase normal band` (the bot throws, you glaze). `ArtStudio/Pottery/Banding.cs`.
- **On the wheel:** the whole pot, no cut-away, bisque until dipped. Six glazes (section 5's), keys 1 to 6. "Dip" coats it all and covers any bands. "Sponge off bands" starts the bands again.
- **The brush:** hold it on the pot and one turn of the wheel (1.1 s) paints a ring a row wide; drag to widen. The glaze sits on a grid wrapped round the pot (three slices to a row, 32 round), so a wavy band really is wavy and its waves come round as it turns.
- **The pressure:** the brush hand drifts, from nothing to its full reach over a couple of seconds held: Simplified 0.35 rows, Normal 1.2, Hard 2. The brush is drawn where it really is, so you correct by watching it.
- **The design:** a base coat and two bands per shape, given as fractions of height so they fit the pot you threw. The bands are coloured marks hugging the pot's right side (the chalk line's way), and listed on the left.
- **Judge:** each band a column at a time round the pot, its painted edges against the design's (a slice of slop free, gaps count), times neatness (glaze where the design has none). Glaze = 0.15 base + 0.85 bands. The pot's stars = 0.75 throw + 0.25 glaze. The bot, averaged over 20 per shape: a perfect hand 1.0; a hand that never corrects scores 0.97 to 1.0 on Simplified, 0.66 to 0.85 on Normal and 0.39 to 0.53 on Hard.
- **Not yet:** a banding wheel of its own (it turns on the kick wheel for now; the art can come later), and drying between throwing and glazing.

**Firing prototype (2026-11-07):** "Fire it" on a glazed pot's results, or `museart_pottery vase normal fire` (the bots throw and glaze it, and you fire it). `ArtStudio/Pottery/Firing.cs`.
- **The scene:** inside the kiln, drawn wholly by code, so no new art. It has brick walls, a vignette as if through the spy hole, the pot on a kiln shelf, and a burner port every 11 pixels with its own flickering flame. Everything glows as it heats: dull red brick, then orange, with the glaze's colours lost in the glow. When it's done the kiln is shown cooled, with the glaze's own colours. A badly fired glaze is greyed, and a cracked pot shows its crack.
- **The schedule:** 30 seconds. The heat climbs to half the peak by 9 s, soaks (holds) until 14 s, climbs to the glaze's peak by 24 s and holds. Each glaze has its own peak, from Clear (70) to Copper red (88). The soak stops one steady rhythm of fuel from following it.
- **The fuels** (keys 1 to 3): Wood gives 8 over 3 s, Hardwood 15 over 9 s and Coal 13 over 1.5 s, against a slow leak. Draughts push the heat about: none on Simplified, more on Hard.
- **The chart** (the left panel) is like a pyrometer's. The band runs through the whole firing, and the heat so far is plotted on it in colour (in the band, too cool, too hot). A hollow marker shows where the fire will have taken the heat in 3 s, so you feed for the future, not the present.
- **Band** (half-width): Simplified 10, Normal 6, Hard 4. On Hard, far over the band for 1.5 s cracks the pot.
- **Judge:** the share of time in the band, after a 2 s grace, with partial credit just outside it. The verdicts are Perfect (0.9 or more), Well fired (0.65 or more), or Underfired / Overfired. A kiln gift (a crackle, a drip, a flash) has a 50% chance on a perfect firing and 10% on a good one, and adds half a star. The pot's stars: the glaze's part is scaled by 0.7 + 0.3 × firing. A crack is 0 stars.
- **Calibration, 10 per level:** a stoker who looks at the forecast every 2 s and adds hardwood when it's heading low scores 1.0 / 0.91 / 0.63 (Simplified / Normal / Hard). One who feeds whenever it's low, four times a second, overfeeds: 0.79 / 0.41 / 0 (cracked). Coal on a beat scores 0, and never feeding scores under 0.1.
- **Not yet:** loading several pots, fuel from your inventory, the overnight cooling, and Tobin's firing days.

**Throwing calibration (2026-11-09):** `dotnet run -- pottery` in `ArtStudio.Sim` (`PotterySim.cs`). The throwing rules, the centring wobble and the bot are game-free now (`ClayPot.cs`, `PotBot.cs`), so the simulation runs the menu's own code. Five hands throw every shape on every difficulty, 40 times each, centring included:

| Hand | Simplified | Normal | Hard | 5 stars (Normal) |
|---|---|---|---|---|
| Perfect (the old greedy bot) | 4.75 | 4.75 | 4.75 | 50% |
| Careful | 4.55 | 4.53 | 4.60 | 25% |
| Average | 4.24 | 4.23 | 4.19 | 8% |
| Rushed | 3.69 | 3.72 | 3.71 | 0% |
| Restless (never lets it rest) | 3.69 | 3.72 | 3.71 | 0% |

(Hard here is before the change below.)

- **Five stars can be reached on every shape, but the vase and the jug need a plan.** The greedy bot gets 4.5 on both: pressing a neck in at full height leaves it thick (clay is conserved), and the judge allows a middle wall 3.4 at most. The jug gets 5 if you pull up through the neck after shaping it. The vase gets 5 only if you hold about four rows back, shape it, then pull the neck up to full height. The results now say so ("The neck's heavy: ...").
- **The difficulty doesn't change the throw's stars, only the time.** A throw that follows the outline is done long before the clay tires: you can pull nonstop for 33 s on Normal (29 s on Hard) before it falls, and by then the pot is 22 rows tall, past every outline (11 to 16). So nobody collapses a pot by accident, and Hard only means waiting longer for the clay to firm up (41 s for a perfect throw, against 17 s). Glazing and firing do scale with difficulty (above). **Hard's patience, shortened (2026-11-11, the user's choice):**
- **The change:** patience drains 3 times as fast as the base rate (it was 1.35; Normal is 1), and comes back at 0.03 a second (it was 0.01; Normal is 0.035). A rest from a low bar to nearly full takes about 18 s.
- **It has teeth:** clay worked on for 2 s after its patience is gone gives way ("You kept working it after its patience ran out..."). Before, a tired pot only slumped a little, and the walls rule only brings down pots over 22 rows, so shorter patience alone changed nothing.
- **The bots on Hard now:** perfect 4.75 stars with 1.8 rests a throw; careful 4.60, 1.3 rests; average 4.18, 0.9; rushed (rests only when nearly out) 3.61, 0.6; restless (never rests) 3.53, and 4% of its pots fall. Nonstop pulling gives way after 8 s (it was 29 s).
- **A person is slower than the bot,** whose whole throw is about 8 s of handling. So a player will need more rests than these, and one who ignores the bar will lose pots more often than 4%. That needs checking in play.
- **The spread between hands** (4.75 down to 3.7) is as forgiving as painting's judge: a painting 35% wrong gets 4.2.
- **Not really throwing:** a lump never touched gets 1 to 1.5 stars, and an outline pulled 6 rows short 0.5 to 1. But **pulling up without shaping gets 4.5 on the cup and 4 on the jug**, since both are close to the cylinder that pulling makes. That's fine for the cup, the first piece, but generous for the jug.

**After the user's playtest (2026-11-11)**, which found it too forgiving, the glaze free, the patience coming back too fast, the clay sliding off the wheel and the stages feeling like separate minigames:
- **One piece of work at a wheel:** throw, then glaze, then fire. After the throw the results offer "Glaze it" or "Throw again", which takes fresh clay. After glazing they offer only "Fire it", and after firing "Done". There's no "Next shape", "Glaze again" or "Fire again" at a wheel; the dev prototype (`museart_pottery`) keeps its free buttons.
- **Glazes cost minerals,** one for each glaze a pot wears, the first time it touches the pot (section 5's table: Quartz, Jade, Iron Ore, Aquamarine, Coal, Fire Quartz; an Alchemist can use Copper Ore for Copper red). The swatches show how many you have, and a stroke with none says so.
- **The kiln burns your fuel** (2026-11-12): at a wheel each feed takes one Wood, Hardwood or Coal from your bag; the buttons show how many you have, and one you're out of is greyed and says so. The dev prototype's fire stays free.
- **Patience is nearly a one-way budget:** resting gives back only 15% of what's been spent (8% on Hard), and clay worked past its patience gives way on Normal too (3 s; Hard 2 s). The bar shows the part that's gone for good. Hard drains 1.6x (it was 3x with full rests).
- **Centring follows the wheel:** the lump is carried round, so it sways about the axis once a turn, as far as it's off true (at most 3.5 units: the lump is nearly as wide as the wheel head). You ride the sway to press it true; off it, it works loose. The hand's band is 2.1 units on Normal, 1.5 on Hard, 3 on Simplified.
- **A stricter judge:** a silhouette now scores from 76% overlap to full marks at 97% (it was 55% to 95%), walls fall off faster, smoothness is stricter, and centring is worth 25%. Glazing's hand drifts 1.6 rows on Normal (it was 1.2) and 2.4 on Hard; the kiln's band is 5 on Normal (it was 6; Hard stays 4).
- **The simulation after (throws, centring included):** Normal: perfect 4.75, careful 4.11, average 3.13 (34% under 3 stars), rushed 1.90 (85% under 3). Hard: 4.75 / 3.90 / 2.46 / 1.88. Glazing, a hand that never corrects: 0.42 to 0.69 on Normal (it was 0.66 to 0.85), 0.27 to 0.44 on Hard. Firing, the careful stoker: 0.85 on Normal (0.91), 0.60 on Hard. The bots handle a pot in about 10 to 15 s; a person is slower, so patience will run out sooner in play than here.

## 11. Visuals (a plan for review)

The prototype is plain rectangles. The aim is a screen that sits beside the easel's as one family: the same menu frame, framed panels, gold stars and word grades. Mockup of the pot: `art/pottery/visual_mock_v1.png` (left, now; right, proposed).

### The screen
- **Laid out like the easel's:**
  - **Top bar:** the title and difficulty, with the patience bar styled like the easel's Light bar.
  - **Left:** a framed **Reference** panel, showing the shape to throw as a finished pot, drawn by the same renderer.
  - **Centre:** a framed **wheel scene**.
  - **Right:** a framed **Tools** panel with the three moves as icon buttons, and a small gauge for how wet the clay is.
  - **Bottom:** the instruction line and the Finish button, as the easel has them.
- **Centring:** the same scene. The lump wobbles with a little blur, your hands follow the mouse, and the Tools panel's gauge becomes the off-true meter, like the easel's mix bar.
- **Results:** laid out like the painting results. The pot as thrown, in leather-hard colour; the stars; word grades (Shape, Walls, Centring: Excellent, Good…); and the tips spoken as Tobin ("Heavy at the neck. Pull through it first.").

### The pot: pixel art, drawn by code
The pot changes shape every frame, so it can't be a sprite. It's drawn pixel by pixel at the game's own scale (1 art pixel = 4 screen pixels):
- **Shading:** each pixel is shaded as a cylinder lit from the upper left, from a six-colour terracotta ramp.
- **Spinning:** throwing rings scroll round to show it turning.
- **Wet sheen:** a stripe of sheen runs down the lit side.
- **Outlines and rim:** outlines are a darker shade of the colour beside them (the house rule), and the rim has a lighter lip.
- **The clay tires:** it gets paler and shinier as patience runs down, droops visibly when it slumps, and falls in with a splat when it collapses.
- **The cut-away:** it moves to the **right** half, so the lit side keeps its sheen. The thin and heavy tints stay.
- **The reference:** a dotted chalk line.

### Art to make (PixelLab, the house pipeline, a vanilla workshop as the style reference)
1. **The kick wheel**, front on: a wooden frame and splash pan, and a grey wheel head with a 2 to 4 frame spin shine.
2. **The backdrop:** Tobin's workshop wall, with shelves of pots and the kiln's glow at one side. There's a second backdrop for your own wheel at home later.
3. **Hands:** a pair for each move (pull: fingers rising, inside and out; press in: palms squeezing; let out: one hand inside, pushing) and for centring (palms cupped). They're tinted to your farmer's skin tone, with two frames each so they move slightly.
4. **Tool icons:** three 16x16 icons for the moves, in the style of the easel's tools.
5. **By code:** water drops and slip spray while you work.
6. **Sound:** a wheel hum, a wet squelch while working, and a splat for a collapse, from the game's own sound cues where they fit.

### Order, each shown to you before the next
1. The pot renderer, in code: the biggest change, and no art needed.
2. The easel-style layout and results.
3. The wheel and the backdrop (PixelLab candidates to choose from).
4. The hands and icons.
5. Sound and the collapse animation.

### Open
- **The cut-away:** always shown, or only while you hold the right mouse button. Potters check a wall by feel or by cutting a test pot; showing it only on demand is cleaner, but harder.
- **The backdrop:** always Tobin's workshop, or wherever the wheel is (his workshop, or your house once you own one).
