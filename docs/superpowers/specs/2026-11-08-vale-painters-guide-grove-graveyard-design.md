# The painters' guide, the Gilded Grove and the graveyard (Gilded Vale, 1.4): design

Status: DRAFT for the user's approval (2026-11-08). Nothing is built yet.
Session: the design discussion that started from the user's question about the "path to nothing" west of the Vale House.

## 1. Why

The user saw a winter screenshot of the Vale: a dirt trail west of the Vale House (tiles about x 5–8, y 10–20) that leads nowhere. That's the Painter's clearing. The map doc (`docs/design/gilded_vale_map.md`) planned "a hidden easel overlooking the Vale" there, but it was never built. What's there is Lupini's Monday painting spot (a stool and a paint rock) and a ring of trees.

The user also found Lupini's painting spots "weird". Touching the stool **or** the rock opens the easel. The user expected touch-and-find objects, not hidden easels.

This design makes the spots the steps of a quest. Finishing the quest opens a secret, mystical place you reach only by warp. The clearing becomes a graveyard of the Vale's past residents. Four threads (the guide, the spots, the Grove, the graveyard) tell one story: the people who made the Vale, who they were, and what they left behind.

## 2. Goals, non-goals, anti-goals

**Goals**
- The painting spots become places you **find**, with a moment of magic the first time (a paint splash across the screen, like the Wizard's cutscene effects).
- A quest to find every spot, started from the old painters' guide (the map doc's discovery, "the old painters' guide to the viewpoints").
- The reward: a Wizard-style warp into **the Gilded Grove**, a small, secret, Secret-Woods-like map that you can reach **only** by that warp.
- The Grove has a reason to come back (its own fish), a view (the hidden easel), a one-time secret (a frame you earn with a masterpiece), and its heart, **the painted echoes** of the Vale's past artists. The user loves this part.
- The clearing becomes a quiet **graveyard**. The old trail finally goes somewhere.

**Non-goals**
- **No new paint colours or pigments.** The user rejected this: "will complicate the rest of the mod".
- No restoring paintings in the Grove. That's Margot's job (the 1.3 restoration system).
- The Vale's ordinary fish are not part of this. They're session `gilded-vale-fish-a-set-f`'s work (branch `claude/vale-fish`).
- No heart events here. Their scripts are in `docs/vale_events.md`, awaiting approval.

**Anti-goals** (this design fails if any of these happen, even if it technically works)
- **The amphitheatre gets cluttered.** The user plans a skill there, probably in 1.5. Its spot must not intrude (section 4).
- **The graveyard turns grim or spooky.** It's a quiet, loved place. The echoes are brushwork, not ghosts.
- **The anonymous painter is named.** Lupini's brief keeps the painter of the unfinished painting anonymous. No stone, no echo, no hint of a name.
- **The Corrie twins are buried.** They're alive (session `old-studios-twins`): about sixty, stained-glass restorers who come home in year 3 and buy back Studio II.

## 3. The guide and the quest

- **Where the guide is:** in the Vale House tea room, on a board or frame the builder places near Bea's guest-book case (about (11, 18) in `ValeHouse.tmx`). The 1.4 plan says the guide is "found in the inn". (The noticeboard outside the Exhibition Hall, at (41, 42) on the outdoor map, is a different board.)
- **Taking it:** touch the board. You take the guide, a book-like item that can't be gifted, sold or dropped, and the quest starts: "The old painters' spots", with a counter, *0 of 8 found*. My call: a short line of Bea's when you take it (she says the painters left it, and nobody has used it in years).
- **Progress is per player** (multiplayer). Each farmhand finds their own spots and earns their own warp.
- **Finding a spot** (section 4) adds it to the guide and moves the counter.
- **The eighth spot:** a bigger splash, every spot's colours at once. The guide's last page fills in with a view that isn't anywhere on the map: the Grove. The quest completes.
- **What the guide is** (my call, kept small): an item and some text boxes, not an illustrated book. Using it before the quest is done shows a letter-style text page (like the game's mail viewer) listing the eight spots by name, found ones ticked and unfound ones as "…". There's no picture page. The Grove "page" is described in words when the quest completes.
- **The warp:** from then on, using the guide **anywhere in the Gilded Vale** asks "Step into the last page?" (yes or no). Yes takes you to the Grove. You get the Wizard's kind of magic: a flash, sparkles, a magic sound, and the screen washing into paint. Outside the Vale, using the guide shows a line ("The page only seems to come alive in the Vale.") and does nothing. My call: this keeps the Grove part of the Vale and stops it from being a warp totem.
- **The way back:** a painted frame at the Grove's edge. Touching it returns you to the graveyard's gate (section 6), so you come back among the people you just met as echoes.

## 4. The spots

**What a spot is:** (changed 2026-11-11: the user didn't like the stool and kit, so each spot is now only its paint rock, and the rock is touched, as the stool was below.) Each spot keeps its stool and paint rock. **Only the stool is touchable.** The rock is scenery. Neither one opens an easel any more. The `blinxxy.MuseArt_PaintSpot` action (`ArtStudio/Vale/GildedVale.cs`) stops calling `ArtHooks.OpenEasel`, and the rock's tile loses the action. You can still paint any view the usual way, with an easel you set down yourself (plein air, `docs/wiki/Portraits-and-Plein-Air.md`).

**Before you have the guide** (my call): touching a stool gives a plain line ("The stool's seat is worn smooth. Someone sat here for years, looking at something.") with no splash, and it doesn't count. The guide is what wakes the spots, so every player starts at 0 of 8.

**The first touch with the guide:** a paint splash across the screen in that view's colours, drawn by our own code as an overlay. It's not a cutscene and doesn't load anything. It plays where you stand, about 1.5 seconds, holding the player still for that time, with a vanilla magic sound (the builder picks the cue). Then a box shows the spot's name and a line from the guide. **Later touches** show just the line. No splash.

**The eight spots** (the map doc's "views to paint", section 7). Names and palettes are my calls, and the guide's text is drafted later (section 8):

| # | Spot (the guide's name) | Where | Palette | State |
|---|---|---|---|---|
| 1 | The Rooftops | the high street: stool (43, 4), rock (46, 4) | warm roof reds, cobble grey | exists |
| 2 | The North Light | Lupini's garden: stool (79, 16), rock (79, 19) | sunflower yellow, sky blue | exists |
| 3 | The Wildflower Field | the meadow: stool (28, 65), rock (31, 66) | meadow greens, poppy and cornflower | exists |
| 4 | The Lake Window | the lake: on main, stool (88, 58), rock (88, 61), Lupini's Friday tile (88, 59). The approved overlook design moves it up onto the new bluff; it goes wherever that design puts it | lake blues, dawn gold | exists; moves with the overlook build |
| 5 | The Lost Parterre | the sculpture garden (Fern's), just inside its gate, about x 5–28, y 39–47; clear of the statue hidden in the hedge in its south-west corner | hedge greens, old stone, rose | new |
| 6 | The Falls | by the new waterfall basin | white water, mossy greens | new: needs the waterfall |
| 7 | Under the Gilded Tree | on the tree's lawn (the tree's left x 35, bottom y 68) | the tree's gold | new |
| 8 | The Old Stage | **outside** the amphitheatre bowl, looking at it (below) | weathered stone, ivy | new |

- **Spot 8 must not intrude** (the user: "make sure the spot near the amphitheatre doesn't feel too intrusive. I eventually have plans for a skill there"). Put it outside the bowl: not on the seats, the stage or the stage's approach, and at least 2 tiles clear of the amphitheatre's footprint (x 29–45, y 49–59). It should be a small stool that looks *at* the amphitheatre from the side. The builder picks the tile and shows the user a render first.
- **Spots 4 and 6** depend on session `vale-map-gaps` (`local_7418f31a-16c2-4bbf-bc43-be2059577f55`, branch `claude/infallible-jang-a66ee9`). It's building the waterfall and basin (passed review) and the overlook bluff (not built yet). Its approved design (`docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md` on that branch) already moves the lake spot onto the bluff, so **the lake spot is the overlook's spot**. There's no separate "Overlook" spot crowding the edge that design keeps clear for Lupini's 10-heart easels. Wait for that branch to merge before placing spots 4 and 6.
- **Spot 5, the sculpture garden,** is one of the map doc's views to paint (section 7). It replaces a second spot on the bluff.
- **The clearing's spot** (stool at about (8, 15), rock at about (9, 18)) is **removed**, because the clearing becomes the graveyard.
- **Lupini's schedule** (`[CP] Gilded Vale (dev)/data/lupini_schedule.json`): he still paints at the spots. Monday changes from the clearing to **the graveyard**. He sits on its bench and sketches in the morning. His other days keep their spots. If a spot moves, his standing tile moves with it. Each tile must be walkable and reachable (the file's own rule).

## 5. The Gilded Grove

**What it is:** a painting the Vale's first painters made of their favourite place. You step inside it. It's a small map of its own, about 30 by 25 tiles, location `blinxxy.MuseArt_GildedGrove`. It's always in golden light, the moment the Vale was named for, and it looks the same in every season, because painted light doesn't change.

**The look:** old trees, a still pool, soft grass and drifting motes of paint (Junimo-dust style, a looping ambient effect). Built from the game's own outdoor tiles, like the rest of the Vale, held in their **fall** look all year, with a warm golden tint. How to hold the season and tint it is left to the builder: for example, non-seasonal copies of the fall sheets, or the location's own lighting. Woods close every edge. There's no exit except the frame.

**In it:**
1. **The painted fish** (the reason to come back). It lives only in the Grove's pool, drawn with visible brushstrokes. It's simply a rare fish: a good price, and a trophy that says you found the Grove. It's not a paint ingredient. Its data and icon should follow the style of the five Vale fish on branch `claude/vale-fish`, but it isn't one of them.
2. **The hidden easel** (the view). It stands at the Grove's edge, looking down over the whole Vale from above. This is the map doc's "hidden easel overlooking the Vale", at last. **How the view works** (my call, so no new painting code is needed): the Grove ends in a drop, and below it the Vale is drawn into the map as a painted panorama, a small picture of the valley in gold light (one piece of art, which could start from a scaled-down render of `GildedVale.tmx`). Plein air paints "the scene around the easel", so the panorama is in the picture without any change to the painting code. The hidden easel is a fixed easel that opens the easel menu itself (`ArtHooks.OpenEasel`), the one place in the Vale where a found object still does. The Grove has no seasons, so it's one Views entry in the Painting Record (`docs/wiki/Painting-Record.md`), not four. The gold light makes a masterpiece a little more likely; the builder sizes the bonus to match the existing ones.
3. **The Founders' easel** (the one-time secret, like the Secret Woods' Old Master Cannoli). An old empty easel. Offer it one masterpiece of your own (`docs/wiki/Quality-and-Masterpieces.md`). **The easel takes it** (my call, as Old Master Cannoli takes its berry; giving up a masterpiece is what makes the frame mean something). Afterwards, for that player, the easel shows a generic finished canvas, not their own painting, so the map needs only a per-player flag. In return you get the **Founders' Frame**, a frame style you can't get anywhere else, added to the frames table (`docs/wiki/Frames.md`). My call on the name: not "gilded", because the **Gold-Leaf Frame** (Epic) already exists. My call on rarity: **Epic**. The builder picks a trait no Epic frame already has. It happens once per player. After that, the easel has a line of thanks.
4. **The painted echoes** (the heart). The four buried artists (section 6), **Agnes Morrow, Hollis Pike, Mabel Corrie and Bram Fairlie**, each sketched in loose, unfinished linework at their easel or bench and caught mid-stroke. Agnes paints, Hollis cuts a woodblock, Mabel holds a piece of glass to the light, Bram carves. Touch one for a memory in their own voice. Each memory answers something hinted at in their epitaph. They're brushwork, not ghosts: no transparency or floating, no spooky sound. A gentle idle motion (a brush moving) is welcome.
5. **The way back:** the painted frame (section 3).

## 6. The graveyard

**Where:** the old Painter's clearing, west of the Vale House, at about x 3–12, y 10–22 on `Maps/blinxxy.MuseArt_GildedVale`. The ring of trees stays (`tools/vale_decor.py`, around line 181); the bare maple in winter suits it. The old dirt trail (`tools/build_vale.py`, around line 598) is joined up to the road's turning circle, so it finally leads somewhere.

**The place:** a low stone wall with a gate, about seven headstones in two rows, and a bench. Everything sits well inside the wall: nothing on the wall and nothing straddling its edges (the user's rule). Only a tall thing's bottom row is solid; its top goes on Front.

**Who's buried here** (from the lore and the Old Studios sign):
- **Bea's great-aunt**, who ran the Vale House for fifty years and left it to Bea eight years ago (`docs/bea_brief.md`).
- **Fern's teacher, the old gardener**, who died at nearly ninety (`docs/fern_brief.md`; Fern's Thu8 line).
- **The old potter**, whose empty pottery and kiln brought Tobin to the Vale (`docs/tobin_brief.md`).
- **Four artists from the Old Studios sign** (the roll is in session `old-studios-twins`'s notes, `old_studios_names.md`, and built into i18n `vale.studios.sign`): **Agnes Morrow** (oils), **Hollis Pike** (woodcuts), **Mabel Corrie** (stained glass, the Corrie twins' mother, which gives the twins a reason to come home) and **Bram Fairlie** (carving, 31 summers).
- **Not here:** Teodor Lisle, Wren Hadley and Ida Venn, who left when the Vale emptied; the anonymous painter; the Corrie twins (alive).
- **Names** for the great-aunt, the gardener and the potter (my calls, easy to change): **Hester Thistle**, **Amos Reed** and **Ada Brannock**.

**Your kiln idea, kept gentle:** the Vale's custom was cremation. The old potter built the outdoor kiln to fire the burial urns, each glazed in the colour that person loved. A small glazed urn sits at the foot of each grave. Tobin fires them now, when he's asked. He gets one quiet line about it (my call: unlocked at 4 hearts).

**Three kinds of marker, and the timeline** (my calls, to fit the lore's dates):
- **Bram's carved stones** for the artists who died while he was carving: Agnes, Hollis and Mabel.
- **Bram's own stone** is the one **plain, uncarved stone**: he died about twenty years ago, after the studios had emptied (Margot: boarded "thirty years" ago), and nobody was left to carve one. Only his name is scratched on it. Lupini knew him, and can have a line about him.
- **Glazed clay markers** for the people who died after Bram: **Ada**, who made her own marker and fired her own urn years early (a potter's joke: she glazed "not yet" on the lid), and **Hester** and **Amos**, both about eight years ago, whose markers and urns Tobin made. So there's never a carved stone nobody could have carved, and the newest graves are Tobin's work.

This needs one more piece of art: a clay marker in place of a headstone for three of the seven graves.

**Reading a stone:** touch it for the name, what they made or did, how many summers they lived in the Vale (summers, like the sign; no dates), and one line of epitaph. The four artists' epitaphs hint at what their echoes tell you in the Grove.

**Seasons:** stones, urns and wall in four versions where snow or leaves would show (winter snow on the stones, like the Vale's other pieces).

## 7. Constraints

- **Merge order:** the Old Studios roll and the Corrie twins' story are built into `vale.studios.sign` only on branch `claude/jovial-blackwell-676837` (session `old-studios-twins`), and the overlook and waterfall only on `claude/infallible-jang-a66ee9`. The graveyard's names and spots 4 and 6 assume both branches merge first.
- Debug-only, like the rest of the Vale: the dev content pack `[CP] Gilded Vale (dev)` and `ArtStudio/Vale/` (`#if DEBUG`). Never released.
- Read `docs/terrain_painting.md` before painting any terrain.
- `tools/build_vale.py` doesn't reproduce the committed `GildedVale.tmx`. Patch the TMX in place. Session `vale-map-gaps` has a shared map patcher on its branch; use it once that branch is merged rather than writing another.
- Other sessions edit this TMX too (`vale-map-fixes`, `vale-map-gaps`), so keep edits contained to the graveyard, the spots, and anything else this design names.
- `tools/vale_routes.py`: add the graveyard gate, the bench and each new spot to `PLACES`, and keep it passing.
- Check every change in all four seasons (`museart_menu mapshot` and offline renders with `tools/render_tmx.py`).
- Never launch a game over the user's running game. To test, use a separate `--mods-path` copy under `.devgame/` and check the log's "Mods go here" line.
- Show art candidates before spending PixelLab credits, with a rough count of generations first.
- Commit only this work's own files. Never release.

## 8. Left to the builder

- All the code: the quest, the guide item, the splash overlay, the warp, the frame, the offering, and the echoes' interaction.
- Exact tiles for the new spots, the Grove's layout, and the graveyard's layout, within the rules above. Show renders before committing.
- **All player-facing text is drafted by the builder and shown to the user as one batch before it's committed:** the guide's eight lines and its last page, the quest text, Bea's line, Tobin's line, the epitaphs, the four echoes' memories, and the Founders' easel's lines. Write it in the voice of `docs/vale_events.md` and the residents' briefs.
- Art: headstones (three carved, one plain), clay markers (three), urns, the Vale panorama below the Grove's edge, the wall and gate, the echoes (four), the Founders' easel, the hidden easel, the return frame, the guide's icon, the painted fish's icon, and the splash. The builder chooses PixelLab or hand-drawn, within the credit rule above.
- The masterpiece bonus at the hidden easel, and the Founders' Frame's trait.
- In-game checks in `ArtStudio/Vale/ValeTests.cs`, in the style of the existing ones.

## 9. Order

1. Spots become touch-and-find (the four existing spots), the guide and the quest. This can be built and tested now.
2. The graveyard (map, stones, urns, text), plus Lupini's Monday move and Tobin's line.
3. The Gilded Grove (map, fish, hidden easel, Founders' easel, echoes, the warp and the way back).
4. The new spots: 5 (the parterre), 7 (the tree) and 8 (the amphitheatre) any time; 4 (its move) and 6 (the falls) once `vale-map-gaps` has merged.
5. Update `docs/design/gilded_vale_map.md`: the clearing is now the graveyard, the hidden easel lives in the Grove, and the discoveries table and the views list change to match.
