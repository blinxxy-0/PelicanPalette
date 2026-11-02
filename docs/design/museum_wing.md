# The museum's painting wing (plan, for approval)

The collection's last reward (restoration_mechanics.md section 5): once all
21 old paintings are donated, the museum opens a wing for them. Today the
donated paintings show only in Gunther's collection view; the wing is
where they actually hang.

## What it is

A new room, its own location, reached by a doorway in the museum. Built
like Margot's rooms: a decoratable shell with straight walls three tiles
tall and a wallpaper of its own (a deep museum red or green, to choose), and
furnished from the collection's own pieces:

- **The 21 paintings on the walls,** grouped by set (the Old Valley, the
  Wreck, the Miners, Calico, the Dwarves), each drawn from the restoration
  you donated, in its set's frame, with a small card under it. Click one
  for its card: title, set, who restored it, and the date.
- **The paint-tube Junimo** in the middle on its plinth, behind a velvet
  rope. **Gallery benches** facing the long wall, and **picture lights**
  over the best of them.
- **Margot** comes on some afternoons (her weekday schedule gains a stop
  there once it's open), and Gunther has a line or two for it.

## When it opens (decided 2026-11-02)

The morning after your **first** donation. Until then the doorway is there
but roped off, with a sign: "Gallery wing: opening soon". Every painting
has its own place on the walls from the start, like a gallery waiting for
its loans: an empty space with a small card ("The Wreck: still missing")
until you donate that one, then the painting itself in its set's frame.
The paint-tube Junimo arrives by letter at 21, as before; the wing has a
plinth waiting for it, which it fills once you've had the letter.

## Where the doorway goes (decided 2026-11-02: B)

`art/museum_wing/doorway_options.png` marks three:

- **A. The exhibit hall's back wall** (recommended): an archway between the
  last window and the palm. The wing sits "behind" the hall, which suits
  a museum. It changes a wall tile or two, and nothing anyone walks on.
- **B. The hall's east wall,** by the reading nook. Also works, but the
  doorway is tucked into a corner that's easy to miss.
- **C. The library's west wall,** by Gunther's counter. Close to Gunther,
  but it's beside his desk and the library's own window.

## Compatibility

Only the doorway's tiles change in the museum (an overlay, as for Margot's
storeroom steps); the wing is its own map. Checked with Stardew Valley
Expanded as for her steps.

## Layout (changed 2026-11-02: marble displays)

Every old painting's large frame is 3 tiles square, and a Stardew room shows
paintings only on walls that face you, so 21 won't fit on walls. The first
build stood freestanding gallery walls across the room; they read oddly
(Stardew has no freestanding walls to borrow from), so the user chose the
exhibition's display boards instead, made bigger:

- **27 tiles wide, 28 deep.** You come in from the museum at the bottom
  left (the hall's east-wall doorway leads into the wing's west side).
- **The back wall:** the Old Valley's five, framed in gold leaf.
- **Three rows of white marble displays** across the open floor, one
  painting each, three tiles wide (the painting about as big as in a wall
  frame): the Wreck's five, the Miners' five, and Calico's four with the
  Dwarves' two. A display waiting for its painting has a card on its board.
- **Benches** below the back wall and by the door, and the **paint-tube
  Junimo** by the door once it's come.
- **Runners** (2026-11-02): the museum's own purple rug, stretched along the
  back wall under the benches and in front of each row of displays.

The doorway in the museum: an opening in the hall's east wall, two tiles
tall, at the bottom of its east room (x 49, rows 15-16). There's no way through
until the wing's open (the warp is added the morning after the first
donation, when the rope comes down), and you arrive clear of the doorway on
both sides: a step inside the wing, and on the open floor below the
museum's display table. Roped off, it shows
the velvet rope end-on: two short brass posts, one behind the other, the
velvet sagging between them.

## Built (2026-11-02)

`tools/build_museum_wing.py` (the map, from the Shed's own tiles, papered
in the saloon red by default), `data/painting_wing.json` (the location, the
doorway's tiles and warps), `Social/PaintingWing.cs` (the rope, the framed
Old Valley, the displays with their paintings drawn on the boards, cards,
benches and Junimo; nothing there can be picked up; clicking a painting or
card reads it), `tools/art/gallery_furniture.py` (the display, widened from
its PixelLab pick, and the rope, made from the velvet rope's post),
`museart_wingtest` (eight checks). The doorway opens the morning after the
first donation.

Changed from the plan: no empty plinth before the Junimo comes (it simply
appears by the door once its letter has).

Once it's open, Margot spends Tuesday and Thursday afternoons (14:00 to
17:00) in the wing, in front of the Old Valley's wall (data/margot.json),
and the first time you talk to Gunther he tells you it's open.
