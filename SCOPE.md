# HelloFresh Recipe Card App: Scope

Based on card **#59 Weeknight Chicken Fried Rice** (`QR3208-79`, week `2026-W46`, 2P/3P/4P). The findings below come from running the real PDF through **PdfPig**, the .NET library the app would use, not just from reading it by eye.

---

## 1. Anatomy of a card

**Page 1 (hero page)**, A4 landscape (842 × 596 pt)

| Element | Example | How to detect it |
|---|---|---|
| Title | Weeknight Chicken Fried Rice | Agrandir-Medium, 18pt, top left |
| Subtitle | with Sambal Mayo, Mushrooms and Pak Choi | Agrandir-Medium, 12pt, directly below |
| Tags | Calorie Smart · **Very Hot** · High Protein | Agrandir-Regular 11pt; teal = normal tag, **red = spice level** |
| Time | 20 - 25 Minutes | Regex `(\d+)\s*-\s*(\d+)\s*Minutes` |
| 5-a-day | 1 of your 5 a day | Regex |
| Recipe number | 59 | 35pt, top right box |
| Ingredient thumbnails | 11 named images | 7pt captions in right column; 14 images on the page (hero, logo, thumbnails, QR) |
| QR code | "Customised recipe" | Image; can be decoded (ZXing.Net) to store the online recipe URL |

**Page 2 (method page)**

| Region | Content |
|---|---|
| Left column (x < ~200) | Cooking Tools, Ingredients table (2P/3P/4P), Pantry table, Nutrition, Allergens, safety text |
| 3 × 2 grid | 6 steps, each with a green number, green title, photo and `a)`/`b)`/`c)` sub-steps |
| Footer | Week code `2026-W46` |

**File name** carries metadata too: `59 QR3208-79 Weeknight Chicken Fried Rice 59_2P_3P_4P_web.pdf` gives the number, product code, title and serving variants.

---

## 2. Parsing findings (what PdfPig actually returns)

The good news: **the PDF has a real text layer** with positions, font sizes and colours, so no OCR is needed. The catches:

1. **Body text uses Type3 fonts.** Font names come back as just `Type3`, so **bold can't be detected**. The ingredient highlighting in the steps ("add the **diced chicken** and **sliced mushrooms**") has to be rebuilt by fuzzy-matching the step text against the ingredient list ("diced chicken" → *Diced British Chicken Breast*, "soy" → *Soy Sauce*, "sambal" → *Sambal Paste*).
2. **Colour *does* survive, and it carries meaning:**
   - Red `(0.88, 0.24, 0.18)` marks **hot/spicy ingredients** ("Indonesian style spice mix", "sambal") and `IMPORTANT:` safety notes
   - Green `(0.18, 0.76, 0.42)` marks `TIP:`
   - Grey `(0.5, 0.5, 0.5)` marks units in the ingredient table (`(g)`, `(ml)`, `(unit(s))`)
   - Green headings `(0, 0.66, 0.27)` mark section titles and step numbers
3. **The "ft" ligature is lost.** "after" comes out as `a\0er` (a NUL character where the glyph should be). This needs a ligature fix-up pass. Check more cards to see whether `fi`/`fl` behave the same way.
4. **Allergen codes sit inline in the ingredient name cell.** `Soy Sauce (ml) 13) 11) 15) 10 20 20`. Telling them apart from quantities needs **X positions**: the column headers `2P 3P 4P` sit at x ≈ 120 / 147 / 173, and allergen markers fall to the left of x ≈ 110. Plain text extraction would merge them into "13 11 15 10 20 20".
5. **Multi-line ingredient names.** `Indonesian Style Spice Mix (sachet(s))` wraps over two lines, with its quantities vertically centred between them. Rows need to be grouped by Y with a tolerance and merged when a row has no quantities.
6. **Reading order in the step grid.** Naive extraction interleaves the columns. Use the six green step numbers as anchors, assign each word to the nearest anchor above and to its left, then split on `a)`, `b)` and so on.
7. **Quantities are mixed types:** decimals (`0.5`, `22.5`), units (`g`, `ml`, `unit(s)`, `sachet(s)`, `tbsp`), and non-linear packaging (Pak Choi `1 / 2 / 2`, spice sachet `1 / 1 / 2`).
8. **Durations in the step text:** `10-12 mins`, `7-8 mins`, `2 mins`, `1-2 mins`. A regex like `(\d+)(?:\s*-\s*(\d+))?\s*mins?` covers this card; seconds and hours should be supported too.
9. **Implicit pantry items.** The Pantry table lists honey and mayo, but the steps also use **oil, salt, pepper and water** without listing them. Detect these from the step text for the "check you have" list.

**Main risk:** the parser depends on the layout. HelloFresh changes card templates over time, and there may be 1P/2P cards or Thermomix variants. Mitigations:
- Collect **10 to 20 varied cards** (different weeks and types) as golden-file tests before building too much.
- The parser returns a **confidence/validation report** (for example: "6 steps found", "every ingredient has 3 quantities", "every step references at least one ingredient").
- An **edit screen** so a bad parse can be fixed by hand rather than blocking the user.
- *Optional:* fall back to an LLM (Claude API) to extract the structure from the raw text when validation fails.

---

## 3. Data model (sketch)

```
Recipe
  Number, ProductCode, WeekCode, Title, Subtitle
  Tags[], SpiceLevel, TimeMin, TimeMax, FiveADay
  HeroImage, QrUrl, SourcePdf (stored), ParseWarnings[]
  BaseServings[] (e.g. 2,3,4)
  Ingredients[] -> Ingredient
  Tools[]                       ("Large Frying Pan", "Sieve" ...)
  Steps[] -> Step
  Nutrition (per serving + per 100g, "based on 2P")
  Allergens[] (code + name, read from the card's own legend)

Ingredient
  Name, Unit, IsPantry, IsHot (red), StoreInFridge (**), NotIncluded (*)
  AllergenCodes[], Thumbnail
  QuantitiesByServings { 2: 150, 3: 225, 4: 300 }

Step
  Number, Title, Photo
  SubSteps[] -> SubStep

SubStep
  Letter, Text, Tips[], Warnings[] (IMPORTANT)
  Timers[] -> { MinSeconds, MaxSeconds, Label ("Cook the rice") }
  IngredientRefs[], HeatLevel (high/medium), IsParallel ("Meanwhile")
```

---

## 4. Scaling to N people

The card only gives 2, 3 and 4. Proposed rules:

| N | Strategy |
|---|---|
| 2, 3, 4 | Use the card's column exactly |
| 1 | Half of the 2P column |
| 5+ | **Add columns together** (5 = 2P + 3P, 6 = 2 × 3P, 8 = 2 × 4P). This keeps the real pack sizes and is what you'd actually have if you ordered two boxes |
| Other options | Linear per-person (from 4P), rounded to sensible steps: whole sachets and units, 0.5 for limes, 5 g / 5 ml for weights |

Also:
- **Rewrite the step text with scaled amounts.** "mix together the sambal and mayo (see pantry for amount)" becomes "mix together the **sambal (15 g)** and **mayo (2 tbsp)**". The card points elsewhere for amounts a lot, so this is the single biggest quality-of-life win.
- **Show amounts the card doesn't scale** (`¼ tsp salt`, "half-full kettle") as-is, with a "you may need more" hint at 4P and above.
- **Equipment warning** at high N: "480 g chicken plus mushrooms will crowd one large frying pan; consider cooking in two batches."
- Nutrition stays **per serving**.

---

## 5. Cook mode and timers

- **One step per screen**, large text, swipe or big Next/Back buttons, step photo, sub-steps as a checklist.
- **Timers come from the text.** A tappable chip on "Add the rice and cook for **[10-12 mins ⏱]**".
  - Range timers: the first alert at the minimum ("check the rice"), then a final one at the maximum.
  - **Several timers run at once.** Rice (step 1) is still running while chicken (step 2) starts. A persistent timer bar shows them all, wherever you are in the steps.
  - Sound, vibration and notification when done; keep the screen awake with the Wake Lock API.
- **Parallel timeline.** "Meanwhile" marks steps that overlap. Build a small Gantt view (rice 0 to 12 min; chicken 0 to 8; prep during the chicken; flavour at 8 to 10 ...) and a "start this now" prompt, which also gives a more honest total time than "20 - 25 Minutes".
- **Safety callouts** (`IMPORTANT:`) shown as red banners that must be acknowledged, such as the chicken handling note.
- **Prep checklist before you start** (mise en place): scaled ingredients, tools, and pantry items including the implicit ones (oil, salt, pepper).

---

## 6. Additional features (prioritised)

**High value, low effort**
- Bulk import: drag-drop many PDFs or watch a folder; de-duplicate on `ProductCode`.
- Recipe library with search and filters: time, tags, spice level, protein, calories, ingredient, allergens.
- Allergen and spice warnings against a household profile ("contains soya, gluten, wheat").
- Pantry/shopping checklist for "not included" items.
- Store and display the hero image and ingredient thumbnails (PdfPig can extract them).

**Medium**
- **Weekly meal plan**, grouped by week code, with a **combined shopping list** across selected recipes and units merged.
- Favourites, ratings, personal notes ("used half the sambal"), and cooked history.
- Printable or shareable scaled card.
- "Make it milder" hint that halves the red/hot ingredients.

**Later / nice to have**
- Voice control in cook mode ("next", "start timer") so you don't touch the screen with messy hands.
- Unit conversion (g ↔ cups, tbsp ↔ ml).
- Store the decoded QR link for customised versions.
- Scaled nutrition totals for a week's plan.

---

## 7. Suggested tech stack

| Layer | Choice |
|---|---|
| Parsing | `UglyToad.PdfPig`: positions, fonts, colours, images (confirmed working on this card) |
| QR decode | `ZXing.Net` (optional) |
| Core library | `HelloFresh.Core` class library: parser, scaler, timer extractor, timeline. No UI dependencies, so it's easy to test |
| Tests | xUnit plus golden-file tests: PDF in, expected JSON out |
| App | **ASP.NET Core Razor Pages + EF Core (SQLite), installable as a PWA** so it runs on a phone or tablet in the kitchen. Timers run client-side in JS (they need to keep going in the browser and play alarms) |
| Hosting | Your usual setup on Tyrion (HTTPS through the tunnel is needed for Wake Lock and notifications) |

*Alternative:* a .NET MAUI app if you'd rather have a native, offline-first app. The core library stays the same either way.

---

## 8. Phased plan

1. **Parser spike**: Core library and tests against this card. Gather more cards and harden the parser (ligatures, table columns, grid order, validation report).
2. **Import and library**: upload PDFs, store them in SQLite, list/detail pages, edit screen for fixing bad parses.
3. **Scaling and cook mode**: N-person scaling, inline amounts, step-by-step UI, multi-timers, wake lock.
4. **Timeline, meal plan and shopping list.**
5. **Polish**: allergen profiles, favourites/notes, voice, PWA install.

---

## 9. Open questions

1. **Platform**: web/PWA on Tyrion, or a native desktop/mobile app?
2. **More sample cards**: can you drop in another 10 or so, ideally from different months and types (veggie, oven-baked, 1P/2P cards)?
3. **Serving range**: is 1 to 6 enough, and which scaling strategy for 5+?
4. **Single user or household?** Affects whether accounts and allergen profiles are needed.
