# The Vale fills up: the Old Studios sell, and three newcomers move in

1.4, the Gilded Vale. Designed with the user, 2026-11-08 (approved by the user 2026-11-08). Nothing here is built
except what section 1 says already exists.

The characters have their own briefs: `docs/corrie_twins_brief.md` (Opal and Jet Corrie) and
`docs/tess_brief.md` (Tess Brand). This document is everything around them: why they come, the two
studios' sale and renovation, the calendar, their interiors, and what it changes in the 1.4 plan.

**How to read it:** lines marked *(the user)* were the user's decisions in this conversation. Lines marked
*(Claude's call)* were filled in by Claude and are open to change. Everything else follows from those or
from the existing briefs and map.

---

## 1. Why, and what already exists

**The idea (the user):** the Vale is a place artists left. Its past is all over the map: the boarded Old
Studios, the weathered sign naming who worked there, the graveyard. 1.4 should let the Vale come back to
life in front of the player, not just be visited. Two of the empty studios sell, Robin renovates them, and
the Vale's own children and grandchildren come home.

**Already built (commit `3f08e95`, and `59b74ea` for the Corries):**
- **The weathered sign** by the Old Studios (map 78-80, 43-45) lists each studio's artists, oldest first,
  with how many summers they stayed (i18n `vale.studios.sign`). Studio II's list ends with *Mabel Corrie,
  stained glass, 24 summers* and *The Corrie twins, stained glass, 8 summers*; Studio III's with *Bram
  Fairlie, carving, 31 summers* and *Ida Venn, watercolours, 12 summers*.
- **Carved numerals** I, II, III over the three studio doors, in flaking old gold. Right-clicking a plaque
  names its last tenant (i18n `vale.studios.plaque_1/_2/_3`; Studio II's names the Corrie twins).
- **The house rules** board inside Studio I (`vale.studio.rules`).
- **The tooling:** `tools/vale_studio_signs.py` patches `GildedVale.tmx` in place: it swaps the plaque tiles
  at (73,35) (73,36) (86,35) (91,35) (92,35) for copies with the numerals, and records the originals in
  `art/gilded_vale/signs/plaques.json`. Its tileset is `z_blinxxy.MuseArt_GildedValeSigns` (firstgid 31200,
  seasonal sheets `spring_/summer_/fall_/winter_GildedValeSigns.png`).

The studios on the map (`docs/design/gilded_vale_map.md`, the Old Studios row): **Studio I** is the big one
at x 68 (door x 73), Lupini's 6-heart studio, and stays his. **Studio II** is at x 82 (door x 86).
**Studio III** is at x 88 (doors x 91-92). Studios II and III have no interiors yet.

## 2. Goals, non-goals, anti-goals

**Goals**
- The player watches two boarded studios come back: a notice, a sale, Robin at work, a fresh gold numeral,
  someone moving in. Small visible changes in the world, the concept sheet's way, not a quest.
- Three newcomers with their own lives, tied to the Vale's past through names the player has already read
  on the sign.
- One new romance (Tess) that stands next to Fern and Emmett without repeating them.

**Non-goals**
- No quests or bundles to make the sale happen. It runs on the calendar *(the user)*.
- No changes to the farm.
- Studio I doesn't change hands. It stays Lupini's 6-heart studio, with the anonymous painter's easel.

**Anti-goals (what would make this fail even if it works)**
- **Gloom.** The Vale's past is visible but not gloomy (the concept sheet). The twins come home because
  they *want* to make things again; Tess comes looking, not grieving on every line.
- **The twins as one character.** They're two people who argue (Opal loud, Jet quiet). If a line could be
  said by either, it's wrong.
- **Breaking the anonymous painter.** Nobody, including the Corries who were there, ever names the
  painter of Studio I's unfinished landscape (`docs/lupini_brief.md`).
- **Blocking the player's own building.** Robin's job in the Vale must never stop the player ordering or
  upgrading farm buildings (section 4).

## 3. The calendar *(the user)*

On a fixed calendar like Kent's, so every player sees it whatever they've done with Lupini.

| When | Studio II (the Corries) | Studio III (Tess) |
|---|---|---|
| Year 1 to Summer, Year 2 | Boarded; the old gold II | Boarded; the old gold III |
| **Fall 1, Year 2** | A **For Sale** notice covers the II | |
| **Winter 1, Year 2** | **Sold** across the notice *(Claude's call: the 1st)* | |
| **Winter 15 to 28, Year 2** | **Robin renovates** *(Claude's call: the second half of winter)* | |
| **Spring 1, Year 3** | Renovated; the II in fresh gold; **the twins move in** | |
| **Summer 1, Year 3** | | A **For Sale** notice covers the III |
| **Fall 1, Year 3** | | **Sold** *(Claude's call: the 1st)* |
| **Fall 15 to 28, Year 3** | | **Robin renovates** *(Claude's call)* |
| **Winter 1, Year 3** | | Renovated, **the III left bare** (the gold is gone, the boards off); **Tess moves in**. In her first week she carves a new III herself (section 5) |

Notes:
- **The Hall's windows** are the twins' first big job. If Lupini's 8-heart event (the Hall reopening)
  hasn't happened by Spring 1, Year 3, that work waits for it; the twins talk about the Hall as a job they
  want (section 6 of their brief). **If the Hall never reopens** *(Claude's call)*: their Hall days are
  studio days, and Opal's 10-heart window goes into the Vale House tea room's window instead (Bea's), so
  Opal's arc can be finished without Lupini.
- **Kent's precedent:** Kent arrives Spring 1, Year 2 with no event, just there. The newcomers do the
  same; a short arrival scene is optional *(Claude's call: a short one each, see the briefs)*.
- **Players past these dates when 1.4 installs:** the calendar is a condition, not a trigger, so a save in
  Year 4 sees both studios renovated and everyone moved in at once. Fine, and the same as Kent.

## 4. The sale and renovation

### The notices on the plaques *(the user)*

The For Sale notice and the Sold notice cover the carved numeral on each plaque: same cells, swapped
tiles. They're conditional `EditMap`s in the content pack, layered over the cells
`tools/vale_studio_signs.py` already owns (Studio II: (86,35); Studio III: (91,35) (92,35)).
- **For Sale:** a paper notice pinned over the numeral, a little weathered (rain-streaked in fall; a lip of
  snow on its top edge in winter).
- **Sold:** the same notice with a red Sold strip pasted across it.
- **Right-click text** changes with the state: *For sale. Enquire at the Vale House* *(Claude's call: Bea
  handles it, she's the Vale's noticeboard)*; *Sold*; then, once renovated, the new tenants' names.

### Robin's job *(the user)*

Robin takes a contracting job in the Vale and renovates each studio in the two weeks before move-in.
- **What the player sees:** Robin in the Vale on weekdays in that fortnight, scaffolding and a timber pile
  in the studio's yard (inside the yard, nothing straddling a wall or the lane), the boards coming off.
- **Pelican Town:** Robin's mail or a line on the noticeboard says she's away on a job in the Vale, and her
  shop's opening follows her schedule *(Claude's call)*.
- **The rule:** this must not block the player's own orders. Vanilla Robin building on the farm overrides
  her schedule; if a farm build is in progress, she works on the farm and the Vale job simply has no
  Robin that day (the scaffolding stays). The builder checks how the game decides this.
- **Robin and Tess** *(Claude's call)*: Robin is the first person in the Vale to see Tess use a chisel, and
  later sells her timber and teaches her some joinery (Tess's brief, schedule).

### The renovated exteriors *(the user: "would obviously also upgrade the exterior")*

After Robin, each studio looks lived in: boards off, roof mended, walls cleaned, windows glazed.
- **New art for both studios,** spring and winter at least, with summer and fall copies of spring (the
  Vale's seasonal-sheet convention: `spring_*.png` swaps to its siblings).
- **Studio II's** windows are the Corries' own stained glass *(Claude's call: the one obvious sign of who
  lives there)*.
- **Studio III's** door gets Tess's hand-carved III instead of gold (section 5).
- **How the art gets made:** the 1.4 plan's way for smaller buildings: first from the game's own house
  pieces, PixelLab only if that looks copied. **Ask the user before spending any PixelLab credits, with an
  estimate.**
- **Map rules** (`docs/terrain_painting.md`): props well inside the yards; only a tall thing's bottom row
  is solid and its top goes on Front; rerun `tools/vale_studio_signs.py` after any rebuild that resets the
  plaque cells. `tools/build_vale.py` doesn't reproduce the committed map: patch in place.

### The numerals afterwards *(the user)*

- **Studio II:** the II returns in **fresh gold**, repainted during the renovation.
- **Studio III:** Tess **carves** a new III into the wood herself in her first week *(the user)*: plain,
  ungilded, a carver's numeral. The plaque changes **by date, on Winter 7, Year 3** *(Claude's call)*, so
  every player sees it whatever their hearts with her; until then it's bare. A save installed later than
  that simply has the carved III.

## 5. The interiors

Both are new interior maps behind the existing doors, the size of Leah's cottage inside *(Claude's call,
from the studios' footprints)*. Both change with hearts, through content-pack conditions on the residents'
friendship. Floor canvases and leaning pieces lean on a wall or furniture.

### The counters' stock *(the user)*

Both counters sell **a mix: new items, and a selection of the game's own placeable furniture** that fits
(for Tess, the game's existing wooden pieces and carvings; for the twins, its glass and lamp pieces). The
new items fill the gaps, because the game has some of these but not nearly enough:
- **The Corries:** stained-glass windows, lamps, mosaic tiles.
- **Tess:** carved wooden figures and animals, stools, frames. **No signs** *(the user: they'd be complex)*.
- **Art:** drawn by hand in the Vale's style, like the Old Studios' signs, so no PixelLab credits
  *(Claude's call)*; PixelLab only with the user's OK and an estimate.
- Tess's stock grows with her hearts (section 5); the twins' starts full.

### Studio II, the Corries *(the user: a workshop with a counter, city money showing, split down the middle)*

- **The split:** a line down the middle of the floor. **Opal's half** is a riot of colour, sketches and
  half-finished designs. **Jet's half** is spotless: tools in rows, offcuts sorted by colour.
- **The money:** expensive city lamps and a sleek modern kiln, far too nice for the Vale.
- **The work:** a light table under the window with a half-leaded panel; racks of coloured sheet glass
  throwing colour on the floor; a counter where they **sell stained-glass furniture** (windows, lamps,
  mosaic tiles) like Tobin sells ceramics.
- **Mabel's bench** sits on the line between the halves, kept and never used *(Claude's call: where)*.
- **Two beds,** one at each end.
- **As hearts grow** *(Claude's call: which hearts)*: the city polish wears off (their own glass lamps
  replace the expensive ones), the line gets a gap where they've started sharing the middle, and at the
  end Mabel's bench is in use, with her finished window in the studio's own window.

### Studio III, Tess *(the user: broke, living off what Bram left, learning, growing homey)*

- **At first:** she makes do with what Bram left: his dusty furniture, his workbench, his stock of timber.
  A bedroll on the floor.
- **Bram's unfinished carving** stands in a corner under a dust sheet *(the user)*. She works on it a little
  after each heart event, and it's finished at her last one.
- **Bram's photograph** is hidden in a drawer until the reveal; after it, it's on the shelf *(the user)*.
- **As her hearts grow** she learns woodworking (furniture, not only carving) *(the user)*: Bram's pieces
  get mended or replaced with her own, and the room gets homier. Her **counter** starts with a few carved
  animals and gains stools and frames *(Claude's call)*. No carved signs *(the user: too complex)*.
- **If you marry her,** the studio stays her workshop *(Claude's call)*; her spouse room is in her brief.

## 6. What it changes in the 1.4 plan *(the user: all three are in 1.4)*

- **Eight residents,** not five: Lupini, Bea, Fern, Tobin, Emmett, Opal and Jet Corrie, Tess Brand.
- **Three romances:** Fern, Emmett and **Tess**, so two of the three are women. The plan's "one man, one
  woman" rule is superseded *(the user)*. That makes five marriage candidates in the mod: Sable, Margot,
  Fern, Emmett, Tess.
- **Hearts:** Opal and Jet are friends to ten hearts; Tess has events to fourteen.
- **Interiors:** two more (Studios II and III), with heart-stage variants.
- **Art lovers:** the Corries and Tess join `ArtItems.DefaultArtLovers` *(Claude's call: they're artists)*.
- **Size:** this roughly adds a third romance and two friendship arcs to the biggest update yet.
- **The name:** the user expects "Pelican Palette" may need a broader name as it grows; to settle later,
  not part of this work.

## 7. Lore to respect

- **The roll on the sign is the source of truth** for the Vale's past artists. The graveyard
  (vale-empty-corner's design) takes its buried artists from it. Shared notes: `old_studios_names.md`.
- **The graveyard is settled by its own approved design** (branch `claude/vale-guide-grove`, commit
  83d979e, `docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md`). Its facts
  bind this design:
  - **Mabel Corrie is buried there,** under a stone Bram carved. Her painted echo in the Gilded Grove holds
    a piece of glass to the light.
  - **The Vale's custom was cremation:** a glazed urn sits at the foot of every grave.
  - **Bram Fairlie is buried there** *(the user)*, under the one **plain, uncarved stone** with only his
    name scratched on it: he died about twenty years ago, after the studios had emptied, with nobody left
    to carve one.
- **Bram's timeline** *(Claude's proposal, the user agreed 2026-11-08)*: the sign's "31 summers"
  is his time *in Studio III*, not his life. When he got too old for the studio he handed it to Ida Venn
  (12 summers) and kept carving at home: Agnes's, Hollis's and Mabel's stones were among the last things
  he made. He died about twenty years ago, around when the twins left for the city, and when Tess was a
  baby. So the twins and Lupini remember him well.
- **Tess carves Bram's stone** *(the user, 2026-11-08)*: at her 8-heart event she asks to, and afterwards
  his plain stone is carved, in her own style, for good. The graveyard design's "one plain stone" stays
  true until that event; the stone's art needs a second, carved version.
- **The twins are alive** and must not appear in the graveyard.
- **Lupini came about thirty years ago** when "twenty of us" filled the studios. The Corries were there
  (they left about twenty years ago), so he knew them. The studios were boarded "years ago".
- **Leah teaches sculpting in 1.5,** starting with wood. Tess is a woodcarver and furniture-maker, not a
  sculptor or a teacher; in 1.5 the two of them can meet over wood, but Leah stays the teacher
  *(Claude's call)*.
- **Studio I's anonymous painter** stays anonymous (section 2).

## 8. Left to the builder

- Exact tiles, art and props for the notices, scaffolding and renovated exteriors, within section 4's rules.
- The interiors' layouts, and exactly which heart levels change which furniture.
- How Robin's Vale schedule coexists with farm builds (section 4's rule is the requirement).
- Event scripts, dialogue, schedules and gift tastes, from the briefs' outlines; each script comes back to
  the user for approval, as for the other residents.
- Birthday checks against Stardew Valley Expanded (the briefs propose dates).

## 9. Open (the user's calls)

Nothing open. Answered 2026-11-08: the counters' stock (section 5, a mix of new and the game's own
furniture, no signs), Bram's timeline (section 7, agreed), and Tess carving Bram's stone (section 7).
