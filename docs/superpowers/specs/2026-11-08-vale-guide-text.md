# The painters' guide, the Grove and the graveyard: the text (DRAFT, for approval)

Every player-facing line for the design in `2026-11-08-vale-painters-guide-grove-graveyard-design.md`, under its future i18n key. Nothing here is in the i18n files yet: once you've approved it (with your edits), this file is the source for every string.

How to read it:
- `#$b#` starts a new box, as in the residents' dialogue. Portrait codes (`$0`, `$h`, `$s`, `$u`, `$l`) are only on lines a resident says.
- `^` is a line break inside one box, as on the Old Studios sign.
- Quotes inside a line are single quotes, like the rest of the Vale's strings.

---

## The spots (the guide's eight lines)

The first touch with the guide shows the name and the line; later touches show only the line. The lines are the old painters' own, so they speak as "we" and give advice.

**`vale.guide.spot.rooftops.name`**
> The Rooftops

**`vale.guide.spot.rooftops.line`**
> From here the high street is nothing but roofs, every red there is. Come after rain, when the tiles shine and the cobbles turn the colour of a pigeon.

**`vale.guide.spot.northlight.name`**
> The North Light

**`vale.guide.spot.northlight.line`**
> Face north. The light here stays the same all day and never throws a hard shadow, which is why painters fight over it. The sunflowers turn their backs on you. Paint them anyway.

**`vale.guide.spot.wildflowers.name`**
> The Wildflower Field

**`vale.guide.spot.wildflowers.line`**
> Don't try to paint every flower. You'll be here till winter. Paint the wind going through them, a poppy here, a cornflower there, and let the green do the rest.

**`vale.guide.spot.lakewindow.name`**
> The Lake Window

**`vale.guide.spot.lakewindow.line`**
> Come at dawn, before the wind gets up. For a few minutes the lake is a window: sky above, sky below, and you in the middle. By noon it's only water.

**`vale.guide.spot.parterre.name`**
> The Lost Parterre

**`vale.guide.spot.parterre.line`**
> The gardener says nothing here is lost, only resting. From this stool you can see the old pattern under the hedges, like a drawing showing through the paint.

**`vale.guide.spot.falls.name`**
> The Falls

**`vale.guide.spot.falls.line`**
> Listen first, paint second. Leave the canvas bare where the water's white. You can't paint white water. You can only leave room for it.

**`vale.guide.spot.tree.name`**
> Under the Gilded Tree

**`vale.guide.spot.tree.line`**
> Everyone paints the tree from across the lawn. Sit under it instead, and look up. In the fall it's gold leaf all the way to the sky. The Vale was named for this.

**`vale.guide.spot.stage.name`**
> The Old Stage

**`vale.guide.spot.stage.line`**
> Sit off to the side. From the seats you see the stage; from here you see the stage and the seats, which is the whole of a theatre. On quiet days, paint the ivy.

---

## The guide

**`vale.guide.plain_stool`** (verbatim from the spec)
> The stool's seat is worn smooth. Someone sat here for years, looking at something.

**`vale.guide.board`** (the board by the guest books, shown when you touch it)
> A small frame by the guest books. Inside it, a thin book spotted with old paint: The Old Painters' Guide to the Viewpoints.

**`vale.guide.take`** (Bea, when you take it)
> The painters' guide? They left that here, oh, long before my time. My aunt kept it out for whoever asked.$0#$b#Nobody's asked in years. Take it. If you find all eight, come and tell me. I've only ever found the tea room.$u

**`vale.guide.again`** (a replacement, if yours is lost)
> Tucked behind the frame's backing is another copy, just as spotted with paint. The painters must have known people lose things.

**`vale.guide.page.title`**
> The Old Painters' Guide to the Viewpoints

**`vale.guide.page.unfound`** (an unfound spot on the page; plain dots, since the letter font may not have the ellipsis character)
> ...

**`vale.guide.last_page`** (when the eighth spot is found)
> The guide's last page has always been blank. Now colour runs into it like water into paper: old trees, a still pool, grass gone gold in a light that doesn't move.#$b#It's nowhere you've seen in the Vale. Under it, in faded ink: 'Our favourite place. We painted it together, so it would keep.' The page is warm, like a window left open.

**`vale.guide.step_in`** (verbatim)
> Step into the last page?

**`vale.guide.not_here`** (verbatim)
> The page only seems to come alive in the Vale.

---

## The quest

**`quest.painters_guide.title`**
> The Old Painters' Spots

**`quest.painters_guide.description`**
> An old guide from the Vale House names eight places where the Vale's painters liked to sit. Find each one, and see what they saw.

**`quest.painters_guide.objective`**
> {{found}} of 8 spots found

---

## The graveyard

Each stone shows a line about the marker, then what's on it: the name, what they made or did, their summers in the Vale (like the Old Studios sign), and the epitaph. Bram carved the three artists' headstones (his mark, a B). Tobin made Hester's and Amos's clay markers (his mark, a T). Ada made her own.

**`vale.grave.hester`**
> A clay marker glazed heather-purple, a small T pressed into the back.^^HESTER THISTLE^The Vale House. 50 summers.^'There was always tea, and always room.'

**`vale.grave.amos`**
> A clay marker glazed hedge-green, a small T pressed into the back. Something nobody planted flowers at its foot.^^AMOS REED^The gardens. 72 summers.^'A gardener's job is to listen.'

**`vale.grave.ada`**
> A clay marker she glazed herself, years before it was needed.^^ADA BRANNOCK^Pots, and the kiln. 46 summers.^On her urn's lid, in her glaze: 'not yet'. Nobody has had the heart to change it.

**`vale.grave.agnes`**
> A headstone, cut deep and clean. A small B in one corner.^^AGNES MORROW^Oils. 18 summers.^'Not one sky the same.'

**`vale.grave.hollis`**
> A headstone, cut deep and clean. A small B in one corner.^^HOLLIS PIKE^Woodcuts. 22 summers.^'He knew what to leave out.'

**`vale.grave.mabel`**
> A headstone, cut deep and clean. A small B in one corner.^^MABEL CORRIE^Stained glass. 24 summers.^'She let the light through.'

**`vale.grave.bram`** (nothing on it but his name; Lupini scratched it, see his line below)
> A plain stone, never carved. Scratched into it, not very neatly:^^BRAM FAIRLIE

---

## The painted echoes

Memories in the artists' own voices, from their own time. They don't know you're there, or how long ago it was. Each one answers its stone.

**`vale.echo.agnes`** (answers 'Not one sky the same.')
> Same window. Same hills. Every morning since I came, and they still ask me why I keep painting the sky.#$b#Because it's never the same one. Look: yesterday's was all bruise and gold. Today's is milk. Not one sky the same. I could paint a thousand more.

**`vale.echo.hollis`** (answers 'He knew what to leave out.')
> A woodcut's mostly what you take away. Every bit you cut prints white. Cut one line too many and the block's spoiled, and there's no painting over it.#$b#Took me years to learn what to leave out. Folk call that patience. It isn't. I'm just frightened of the knife.

**`vale.echo.mabel`** (answers 'She let the light through.')
> Glass on the bench is nothing much. Grey, a bit dusty. Hold it up to the window, though... There. That's the whole trick. You let the light through.#$b#Opal's at my bench again, loud as a kettle, and Jet's under it sorting the blues. They'll go off to the city one day, the pair of them. Good. Then they can come home and tell me all about it.

**`vale.echo.bram`** (answers the plain stone)
> Every stone I carve, they want a line on it. Something to make the visitors stop. I pick each word with them, and cut it deep, so it lasts.#$b#Not mine. When it's my turn, leave it plain. Scratch the name on and let the stone do the talking. Stone's good at that. Better than me.

---

## The Grove

**`vale.grove.frame`** (the way back; written as a yes/no question, like the step in)
> A painted frame at the Grove's edge. Through it, faint as a sketch, a low stone wall and a gate. Step back into the Vale?

**`vale.grove.founders.ask`** (holding a masterpiece)
> An old, empty easel, worn smooth by hands long gone. It seems to be waiting for your very best work. Leave your masterpiece on it? You won't get it back.

**`vale.grove.founders.taken`** (it takes the painting; the Founders' Frame follows)
> The easel takes your painting gently, the way a friend takes your coat. Its colours sink into the Grove's gold. In your hands is an old frame, warm, as if it had just been carved.

**`vale.grove.founders.thanks`** (every touch afterwards)
> The old easel holds a finished canvas now, glowing in the Grove's light. It isn't waiting any more. Somehow, that feels like thanks.

**`vale.grove.founders.not_masterpiece`** (a painting that isn't a masterpiece, or empty hands)
> The easel waits. It wants a masterpiece, and nothing less.

**`vale.grove.founders.framed`** (a framed masterpiece)
> The easel won't take a painting in a frame. It wants the bare canvas. A Framer's Bench can take it out.

---

## The residents

**`tobin.dialogue.urns`** (4 hearts)
> The urns in the clearing. Ada built a kiln outdoors just for those. Every one glazed in the colour that person loved.$0#$b#Never met her. I fire it now, when somebody asks. Bea's aunt was heather. Amos was hedge green. Took me three firings. He'd have said four.$u

**`lupini.dialogue.bram`**
> Bram carved the stones for his friends in the clearing, every one. When he went, nobody was left who could carve his. I scratched his name on with a palette knife. He'd have hated my lettering.$s
