# Designing a Stardew interior

Studied 2026-11-07, after the Vale House went wrong three times. I had built it from the Shed's shell, one box per room, joined by warps and patched-in doorways, and it didn't read as a Stardew house. These notes come from the vanilla houses, taken apart layer by layer: Sam's, Haley's, Leah's, Elliott's, Pierre's, Josh's and the Saloon. All tile numbers are townInterior's, counting from 1 as a map's gids do (Town numbering).

## 1. A house is one floor plan, not a set of boxes

- **One map, one outline.** Every vanilla house is a single map whose floor is one irregular shape. Rooms are areas of that shape. Sam's house is a kitchen, a living room, two bedrooms and a hall, all on one map.
- **Rooms join through gaps in the walls between them.** An opening is a gap 1 to 3 tiles wide where the floor runs straight through. The game never draws a staircase indoors, and never uses a door that warps to another map inside a house. Upstairs rooms are drawn on the same floor plan.
- **Shapes are irregular.** Rooms differ in size, are offset from each other, and corners step in and out. A hall can be a single tile wide. The outline never closes into a plain rectangle.

## 2. The outline and the walls

- **The outline** is the orange border line, on **Front**. It traces the floor plan's whole edge, inner partitions included, as one continuous line. The kit has straight runs (3, 166 and 161–168), inner and outer corners, and 45° chamfered corners (2, 4, 33, 36).
- **North-facing walls are the only walls with a face.** Where floor has wall above it, the wall shows its face:
  - the wallpaper rows on **Back**, normally 2 rows, which a DecoratableLocation papers through WallID;
  - the wainscot or skirting row on **Buildings**, solid (81 and 113, 75 and 107; 106–108 and 138–140 with diagonal ends);
  - the outline on Front above them.
- **The other walls are just the outline.** West-, east- and south-facing walls have no face, because the camera looks down from the south.
- **A back wall's ends turn at 45°** to meet the side walls (the diagonal pieces 39/41, 106/108, 138/140). That is what makes a room look like it has depth. A square-ended back wall reads as a flat box, as the Shed does.
- **Each room has its own floor** on Back: wood planks, a checked kitchen, carpet in a bedroom, straw on a porch. Floors carry wear: darker patches and stains. Rugs go on **Back2** (Haley's red rug: townInterior_2, a 9-slice).

## 3. Furniture

- **Against the back wall, furniture overlaps it.** A bed's headboard, a bookcase or a dresser stands with its feet on the first floor row. Its upper part is drawn on Front, over the wall face, and its base on Buildings. Nothing hangs in mid-air, and nothing sits a row out unless it is meant to stand free (a table, a counter).
- **Wall-mounted pieces sit in the wallpaper rows on Front:** pictures, windows, shelves, a clock, a lamp.
- **A fireplace is built into a back wall.** Its chimney on Front runs up through the outline to the top of the map (Leah's, Haley's).
- **Things come in clusters with a purpose:** the bed with a nightstand and a chest at its foot; the stove, counter and a shelf above; the easel with a stool and a paint pot; the fireplace with logs, a basket and a plant.
- **Small clutter fills the floor near the walls:** potted plants, crates, a stool, books, tools. Each object is 1 tile; there are lots of them.
- **The middle stays open.** Walkways run from the door to every room, at least 1 tile wide and usually 2.
- **Free-standing pieces** (tables, counters, sofas) sit out in the room, each with its chairs or space round it.

## 4. What this means for building

- **Design the plan first**, on a grid: rooms, floors, openings, the door in, and what goes where.
- **Generate the walls from the plan**, like the terrain painter. For every floor cell, its neighbours decide its outline piece, and any floor cell with wall above it gets the wall face. The pieces are learned from the vanilla houses, as the terrain was learned from the outdoor maps.
- **Then place furniture by these rules, and check in game** against a vanilla room side by side.

## 5. Built so far

- **tools/build_vale_house.py** builds the Vale House from rectangles, using Haley's kit (all the tile numbers are in its header).
- **Doorways between stacked rooms are real doors.** The foot tile (121) carries `Action Door`, and the game opens the door as you walk up, as it does Haley's. The dev route checker (`museart_route`) can't pass any door, Haley's included, so walk the doors by hand.
- **Doors** need the map property `Doors` (`x y 1 120` per door) as well as `Action Door` on the foot tile, or the game never opens them. The game's doors stay open until everyone leaves the building, and ours do the same.
- **A bed** you can sleep in needs `Bed T` on its Back tiles as well as `TouchAction Sleep`. Without `Bed`, the game darkens the room but never ends the day: `Game1.NewDay` fades out only a farmer in bed.
- **Floors under doorways** are the room's own planks. The vanilla threshold tiles (232, 264, 671) are a different wood.
- **Furniture is the game's own** where it has the piece: Haley's couch, coffee table, armchair, kitchen and dining table, Josh's hall table and clock, the potted plants (`VANILLA` in the builder, row by row and layer by layer, as their maps lay them). A couch goes with its back on the wainscot and its cushions to the room, never facing away from everything. Seating comes in groups round a table, on one rug. Tables in a room are either all on rugs or none.
- **Wall art varies:** the paintings made for the art shop's gallery (cut from `ArtShopFurniture.png` into the Vale's sheet), each hung once, plus the game's small framed pictures (176-178, 208, 210, 246, 247). Never the same picture repeated round a house.
- **Paintings hang in the wallpaper's band:** from the top of the wall's first row to the wainscot's rail, 9 px into the second row (25 px). Our own paintings are drawn centred in it (`hang()` in the builder). The game's 1x1 framed pictures sit against the ceiling's border there, so the inn uses small versions of our own paintings instead. Nothing hangs on a wall's 45-degree end columns (a room's first and last floor columns).
