# The Gilded Vale's own fish (design)

2026-11-08. Five fish of the Vale's own for its normal outdoor water: the river, the pools under its two cascades, the waterfall basin, and the lake (`Maps/blinxxy.MuseArt_GildedVale`, `[CP] Gilded Vale (dev)/assets/GildedVale.tmx`). The map doc (`docs/design/gilded_vale_map.md`, section 2) left them for later: "Fish of the Vale's own could come later." Claude recommended the set from the lore and the user approved it ("nothing, looks good to me", 2026-11-08).

## Intent

Each fish is something the Vale's residents already talk about, so catching it feels like meeting a line of dialogue. The Vale is a gentle place: no quests everywhere, small changes in the world (the concept sheet). So the fish are everyday catches with one rare one, not a hunt.

**The user's standing rule:** the right answer is the one that fits the lore best. Read the lore (the briefs `docs/*_brief.md`, `docs/vale_events.md`, the Vale's `i18n/default.json`) before any call left to the builder.

## Goals
- Five fish, each with its own water, seasons, weather and times (the table below).
- The Vale's water also gets the game's own river and lake fish. **Today it has no fish list at all**: the map doc's "the river and the lake use the game's own river and lake fish" was never set up (no `Fish` in the location's `Data/Locations` entry), so this work sets that up too.
- They work everywhere a vanilla fish does: the fishing collection, Fish Ponds, fish tanks, roe and aged roe, the fish smoker, cooking that asks for "any fish", and the painting record's Fish tab.

## Non-goals and anti-goals
- **Not the Gilded Grove's painted fish.** That's a separate secret map, designed elsewhere, and its painted fish is meant to be the special, come-back-for-it catch. Nothing here may be painted, mystical or legendary, or take that role.
- **No legendary.** Two reasons. It would compete with the Grove's fish. And the portfolio's Legends page (`ArtStudio/Painting/Portfolio.cs`, the five vanilla legendaries) can't get a sixth slot: pages are append-only, and a new slot would un-finish players' completed books.
- **No new paint colours or pigments**, nor anything that turns a fish into paint (the user rejected pigments: "will complicate the rest of the mod"). The names are just names.
- **No new items** besides the five fish. Pond produce is vanilla items only.
- **Community Center bundles untouched** (vanilla and remixed).
- **No name clash** with vanilla or SVE. SVE already has Goldenfish, Bull Trout, Torpedo Trout, Meteor Carp, Diamond Carp and Gemfish (`.devgame/devmods_sve/.../code/Other/Fish.json`), which is why there's no "golden" fish and no plain trout name.

## The five fish (decided)

| Fish | Water | Seasons | Weather | Time | Difficulty, motion | Price | Rarity |
|---|---|---|---|---|---|---|---|
| **Claybank Loach** | the river | spring, summer, fall | any | 6am to 7pm | 30, sinker | 40g | common |
| **Cascade Trout** | the two cascade pools | spring, fall | rain only | 6am to 7pm | 70, dart | 120g | uncommon |
| **Falls Sculpin** | the waterfall basin | fall, winter | any | 6pm to 2am | 55, sinker | 90g | uncommon |
| **Lakeblue Shiner** | the lake | spring, summer | sunny only | 9am to 5pm | 45, mixed | 75g | common |
| **Gilt Carp** | the lake | fall only | sunny only | 10am to 6pm | 80, mixed | 250g | rare (about Sandfish's odds) |

Calibrated against vanilla (Chub 35/50g, Rainbow Trout 45/65g, Woodskip 50/75g, Sandfish 65/75g, Tiger Trout 60/150g, Catfish 75/200g, Sturgeon 78/200g). Sizes, the exact chance numbers and the level/depth requirements are left to the builder, matched to those neighbours.

### What each one is, and the lore it comes from
Each needs a description (in the Vale's voice, short like the game's own) that carries this:
- **Claybank Loach:** a small red-brown bottom fish that lives in the clay banks of the river. It comes from Tobin's clay pit by the river ("Good clay comes up red and stubborn. Like me.").
- **Cascade Trout:** it leaps up the cascades when the river runs high, in the spring and fall rain. From Emmett: "I'm writing a song about fish. It's not going well. Nothing rhymes with trout."
- **Falls Sculpin:** a mottled grey fish that hides under the river stones in the basin below the falls, out at night in the cold months. From the Hall's 'Spiral', "carved from river stone found below the falls". Its name also hints at sculpting in 1.5. That stays a quiet joke, with no mechanic.
- **Lakeblue Shiner:** so blue the painters who saw it were told they'd made it up. From Lupini at the lake: "Too blue. Nobody believes it." (and Sable: "Did you see the light come off the lake in the morning?").
- **Gilt Carp:** a gold-scaled carp that comes up in the fall, when the Gilded Tree's leaves drift down over the water. It's the Vale's name in a fish, but natural: no painted or magic look.

## Where they bite: fish areas

Water on the map today (the `Water` tiles on Back, read from the committed TMX):
- **The river:** comes in at the top edge (x 80 to 84), turns west along y 6 to 12, then runs south down x 59 to 64 to the lake. The upper bridge is at y 40 to 41.
- **The cascades:** the narrower water at x 60 to 63, y 28 to 32 (upper) and y 48 to 52 (lower). The **pools** are the cascade tiles plus the wider water just below each: about y 28 to 36 and y 48 to 54.
- **The waterfall basin:** not built yet. Session vale-map-gaps (branch `claude/infallible-jang-a66ee9`, spec `docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md` on that branch) is building a tall fall in the north-east, its notch at about x 80 to 85, a small basin at its foot, and the river leaving west along the cliff foot. Until it lands, the basin area is the top of the river at about x 76 to 88, y 0 to 12. **Re-check the rectangle once that branch is merged.**
- **The lake:** x 48 to 84, y 55 to 71. The overlook bluff (same session) will take its east side, so re-check that edge too.

Use the 1.6 location data's `FishAreas` (one each: River, CascadePools, Basin, Lake, by `Position` rectangles, like the Forest's River and Lake) and give each `Fish` entry a `FishAreaId`. The cascade pools and the basin are inside the river's water, so their rectangles must take precedence over the river's (the builder checks how the game picks between overlapping areas, and cuts the river's rectangles round them if needed).

**Vanilla fish alongside:** River, CascadePools and Basin get the game's river fish (the Forest's river list), and the Lake gets the Mountain lake's list, by season as the game has them. The Vale's own fish go in with chances so a day's fishing is mostly vanilla with the Vale's fish mixed in, and the Gilt Carp is a real find. Crab pots: freshwater, like the Forest.

## How they fit with the rest of the game (decided)
- **Fishing collection and perfection:** included, like SVE's fish (`ExcludeFromFishingCollection` false). This also puts them in the painting record's Fish tab.
- **Painting record:** `RecordTabs` skips items whose id starts with `blinxxy.MuseArt_` (the mod's own things). So the fish's ids must not use that prefix. `{{ModId}}_` in the dev pack gives `blinxxy.MuseArt.GildedValeDev_…`, which is fine. *(Builder's call: ids that will survive the move from the dev pack to the released pack, so test saves don't lose them.)* Painting one as a still life fills its Fish-tab entry like any fish. Not added to `PaintingTags.LegendaryFish`.
- **Fish Ponds:** each gets `Data/FishPondData`: roe at every size, as usual, and a little vanilla extra that fits it:
  - Loach: Clay.
  - Sculpin: Stone, or a geode.
  - Trout and Shiner: roe only, or the game's ordinary extras.
  - Gilt Carp: a rare Gold Ore.

  Population quests use vanilla items. *(The exact lists are the builder's call, in that spirit.)*
- **Fish tanks:** each gets `Data/AquariumFish`, with its own aquarium sprite.
- **Context tags:** the usual fish tags (`fish_river` or `fish_lake`, season tags, motion, `fish_has_roe`) so the game's own systems treat them as river and lake fish.
- **SVE:** no clashes (names checked above). Checked in a dev game with SVE (`.devgame/devmods_sve`) as well as without.

## Art
- **Icons:** five 16×16 item sprites in the vanilla fish style, plus five aquarium sprites. **Show candidates to the user before choosing.** *(Claude's call on how they're made: drawn by hand with the free pixel tools, starting from the nearest vanilla fish (Loach from the Chub's or Bream's shape, Trout from the Rainbow Trout, Sculpin from the Stonefish's or Woodskip's, Shiner from the Anchovy's or Sardine's, Carp from the Carp) and recoloured and reshaped, so they match the game's palette and outlines. PixelLab only if those look copied, and then only after saying how many generations: about 3 candidates a fish, so about 15. The allowance is low and wanted for characters.)*
- Colours from the descriptions: the Loach clay red-brown, the Trout spotted silver with a rosy side, the Sculpin mottled slate grey, the Shiner a very bright, unlikely blue, the Carp gold.

## Constraints
- **Where:** the Debug-only content pack `[CP] Gilded Vale (dev)`: `Data/Objects`, `Data/Fish`, the location's `Fish`/`FishAreas` in its `Data/Locations` entry, `Data/FishPondData`, `Data/AquariumFish`, the icons under `assets/`, strings in `i18n/default.json`. No C# expected. If something needs code, it goes in `ArtStudio/Vale/` (Debug only).
- **Don't touch the TMX** for this: fish areas are data, not map edits. Other sessions are editing the map.
- **Testing:** never launch over the user's running game. Use a separate `--mods-path` copy under `.devgame/` and check the log's "Mods go here" line.
- **Commit only this work's files**, on branch `claude/vale-fish`. Never release.

## Done means
- All five catchable in their water, season, weather and time, checked in a dev game. Debug-set the season, weather and time, fish each area, and confirm the catch list. Also confirm the vanilla river and lake fish now bite in the Vale.
- Each one in the collection tab, and placed in a Fish Pond and a fish tank. A still life of one shows in the painting record's Fish tab.
- Checked once with SVE installed.
- The map doc's section 2 "Fishing" line updated to name the Vale's fish and point here.

## Left to the builder
- Sizes, chances, level and depth requirements, pond lists and quests, within the numbers above.
- Description wording, in the Vale's voice. Keep it short, like the game's own.
- How overlapping fish areas are resolved.
- Whether residents mention the fish is **not** in this build. A line for Emmett when you've caught a Cascade Trout ("still nothing rhymes") would be lovely, but dialogue is its own work.
