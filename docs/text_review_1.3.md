# 1.3 text (approved as written, 2026-11-02)

Everything new in 1.3 that you haven't read yet, in one place, grouped by
where you'll meet it. The heart events, Margot's dialogue, the Saloon scene
and her bouquet and pendant lines were approved as drafted, so they aren't
here. Say which lines to change; the rest stand.

`{{title}}`, `{{fee}}` and `{{set}}` are filled in by the game (a painting's
name, its fee in gold, and its set, such as "the Wreck"). Codes like `$h`
are Margot's or Gunther's portrait.

---

## 1. Margot's married life (57 lines)

They're in `docs/margot_marriage.md`, under **Married lines**: rainy days
and nights, mornings and evenings indoors, outdoors, good, neutral and bad
times, her room, her patio, leaving for and coming back from work,
children, and five days of the year. Her engaged line is there too:

> I keep writing 'Margot Pell' on things and stopping halfway. Like I've forgotten how it ends.

Edit that file directly if you like; `tools/build_margot_marriage.py` reads
the lines from it.

---

## 2. Margot's restoration service (before you have Mastery)

You hand her a damaged old painting at the museum:

| When | She says |
|---|---|
| She offers | Ooh, {{title}}. I can have it ready in three days, for {{fee}}g. Shall I? |
| Your answers | "Please." / "Not yet." |
| She takes it | Three days. Don't hover. *(next page)* Well. Hover a little. `$h` |
| You can't pay | That's {{fee}}g, I'm afraid. The museum's very particular about receipts. `$s` |
| She already has one | One at a time! I'm still on {{title}}. `$a` |
| It isn't ready | Not yet! Give me a little longer with {{title}}. `$7` |
| She hands it back | {{title}}, as good as I can make it. Careful with the corners. `$h` |
| Your bag is full | Make some room in your bag first. `$u` |
| You have Mastery | You don't need me for that any more, conservator. Your easel's waiting. `$h` |

---

## 3. Gunther's letters for the museum collection

**The Old Valley complete** (with the gallery bench), titled *The Old Valley, complete*:

> The Old Valley's paintings hang together now: the valley as it was before the town. Lewis found his grandfather's barn in one, and stood there a long time.
>
> We had benches made for the gallery. One was left over. It's yours.
>
> -Gunther

**The same day** (with the velvet rope), titled *A velvet rope*:

> A velvet rope, for your own paintings. Margot says people stand too close to good work. She says it from about two inches away.
>
> -Gunther

**And** (with the picture light), titled *A picture light*:

> And a picture light. Hang it over the painting you're proudest of. Paintings like to be looked at properly.
>
> -Gunther

**Six donated** (with the living painting), titled *A painting that moves*:

> Six of the old paintings, restored and on the wall. Margot says that's the most anyone's managed in a hundred years.
>
> This one came in a crate of hers from the city, uncatalogued. We think it's alive. It's certainly not still. Look at the water.
>
> It's happier with you than in our storeroom.
>
> -Gunther

**Twelve donated** (with Margot's loupe), titled *Margot's loupe*:

> Twelve restored paintings. Margot came in on her day off just to stand in front of them.
>
> She asked me to send you this. It's her loupe, the one she's had since her first week at the city museum. She says you'll see things she'd miss now.
>
> -Gunther

**All 21** (with the paint-tube Junimo), titled *The collection, complete*:

> All twenty-one. Every old painting that came back to the valley is restored and on our walls, and most of them by your hand.
>
> Sable heard, and made you a Junimo of your own out of their empty tubes, like the one in the gallery. The note says: 'Every collection needs a guardian.'
>
> Thank you, farmer. Truly.
>
> -Gunther

---

## 4. The painting wing

| Where | Text |
|---|---|
| The rope across the doorway | A velvet rope, and a little sign: "The Painting Wing. Opening soon." |
| A missing painting's card | {{set}}: a painting still missing. Restore it, and Gunther will hang it here. |
| A painting's card | {{title}}. {{set}}, artist unknown. Restored, and given to the museum. |
| Gunther, once it's open | Have you been through the east doorway? The painting wing's open. *(next page)* Your restoration's hanging there, and there's a place kept for every one still out in the valley. Margot cried. Don't tell her I told you. |
| The place's name | The Painting Wing |

---

## 5. Item names and descriptions

| Item | Name | Description |
|---|---|---|
| Old Valley reward | Velvet Rope, Gallery Bench, Picture Light | (furniture, no description) |
| Six donated | Living Painting | (furniture) |
| Twelve donated | Margot's Loupe | Margot's jeweller's loupe, worn on a cord. Old paintings turn up more often while you wear it. |
| All 21 | Paint-Tube Junimo | (furniture) |
| Margot's 10 hearts | Margot, by You / North Window, by Margot and You | (furniture) |
| The wing | Gallery Card, Marble Display | (the museum's; can't be picked up) |
