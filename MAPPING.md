# Oris → JewelCloud Field Mapping & Coverage

- **Source:** `https://product-api.oris.ch/api/watch/all` (profiled 2026-10-23, **1,765 records**)
- **Destination:** JewelCloud Simple Product template → **Timepieces** sheet (30 columns)
- **Product type:** all records treated as Simple Products (`ProductType = 0`)
- **Implementation:** [`TimepieceMapper.cs`](src/OrisJewelCloudExport/Mapping/TimepieceMapper.cs), [`OrisValueNormalizer.cs`](src/OrisJewelCloudExport/Mapping/OrisValueNormalizer.cs)

Coverage figures are from the run on 2026-10-23 and are reprinted by the application at the end of every run.

## Status legend

| Status | Meaning |
|---|---|
| ✅ Direct | Source value used as-is (only trimmed/cleaned) |
| ✅ Normalized | Source value converted to a JewelCloud accepted value by a defensible rule |
| ✅ Fixed | Constant value justified by the assessment brief |
| ⚠️ Blank (ambiguous) | Source data exists but its meaning is too ambiguous to map safely |
| ❌ Blank (missing) | No source data available |

---

## 1. Field mapping (Timepieces sheet)

| # | JewelCloud column | Oris source | Rule / transformation | Coverage | Status |
|---|---|---|---|---|---|
| A | IsActive | `isArchived` | `false` → `0` (active), `true` → `1` (inactive). Never `2` (deleted): archived watches are real products. | 1765 / 1765 | ✅ Normalized |
| B | ProductType | — | Always `0` (Simple Product), per brief | 1765 / 1765 | ✅ Fixed |
| C | StyleNumber | `reference` | Trimmed; spaces/hyphens kept so it matches the official Oris reference (unique across all records) | 1765 / 1765 | ✅ Direct |
| D | RetailerStockNumber | `reference` | Same as StyleNumber (template note: "typically the same") | 1765 / 1765 | ✅ Direct |
| E | ProductName | `name` | Trimmed (955 records have a leading space). No brand prefix added. | 1765 / 1765 | ✅ Direct |
| F | ProductDescription | `description` | Trimmed, including trailing `\r\n` | 1765 / 1765 | ✅ Direct |
| G | MetalType | `material` (case phrase) | See [case material rules](#2-case-material-rules) | 1737 / 1765 | ✅ Normalized |
| H | MetalColor | `material` (case phrase) | Only gold cases, which state their colour: 18K rose gold → `Rose`, 18K yellow gold → `Yellow` | 11 / 1765 | ✅ Normalized (partial) |
| I | ImagePath | `imageUrl` | Absolute URL; spaces percent-encoded as `%20` (1,071 URLs contain spaces) | 1765 / 1765 | ✅ Direct |
| J | Collections | `collection` | Trimmed (28 distinct, no special characters) | 1765 / 1765 | ✅ Direct |
| K | Categories | — | Always `Watches` (from the WATCH CATEGORIES list) | 1765 / 1765 | ✅ Fixed |
| L | WatchBandMaterial | — | No source field | 0 | ❌ Blank (missing) |
| M | WatchBandType | — | No source field | 0 | ❌ Blank (missing) |
| N | WatchCaseMaterial | `material` (case phrase) | See [case material rules](#2-case-material-rules) | 1688 / 1765 | ✅ Normalized |
| O | WatchCaseShape | — | No source field | 0 | ❌ Blank (missing) |
| P | WatchCrystalType | — | No source field | 0 | ❌ Blank (missing) |
| Q | WatchDialColor | `color` | Not stated whether `color` is dial, strap or case colour | 0 | ⚠️ Blank (ambiguous) |
| R | WatchDisplayType | — | No source field | 0 | ❌ Blank (missing) |
| S | WatchMovement | `winding` | "Automatic winding…" / "Hand winding" → `Mechanical`; null → blank | 1748 / 1765 | ✅ Normalized |
| T | WatchNumberType | (`winding`) | Accepted list mixes dial-numeral and movement terms; column intent unclear | 0 | ⚠️ Blank (ambiguous) |
| U | WatchEnergy | `winding` | "Automatic winding…" → `Automatic`; "Hand winding" → `Manual Mechanical`; null → blank | 1748 / 1765 | ✅ Normalized |
| V | Gender | — | No source field | 0 | ❌ Blank (missing) |
| W | PriceType | — | Pricing policy (1–5) is a retailer decision, not in source | 0 | ⚠️ Blank (ambiguous) |
| X | DeliveryTime | — | No source field (JewelCloud defaults to 2 Weeks) | 0 | ❌ Blank (missing) |
| Y | WatchSize | `size` | "42.00 mm" cannot be bucketed into Mini…Extra Extra Large without defined ranges; 9 values are non-round ("39.00 x 40.00 mm") | 0 | ⚠️ Blank (ambiguous) |
| Z | WatchType | (`collection`) | Inferring "Divers"/"Pilot" from collection names is an assumption | 0 | ⚠️ Blank (ambiguous) |
| AA | ImagePath2 | `image2Url` | As ImagePath; 1 record has an empty value | 1764 / 1765 | ✅ Direct |
| AB | RetailPrice | `price.amount` | Only when `price.currency = USD` **and** `price.active = true`; must parse as a positive decimal. Set `IncludeInactivePrices = true` to also use inactive prices (→ 1609 / 1765). | 353 / 1765 | ✅ Direct |
| AC | ExtendedField1 | — | Not used | 0 | ❌ Blank (unused) |
| AD | ExtendedField2 | — | Not used | 0 | ❌ Blank (unused) |

---

## 2. Case material rules

The case metal is taken from the **first phrase** of `material` (up to the word "case", or the first comma/semicolon).
Later phrases describe the top ring, plating or lugs and are ignored, except for one MetalType rule:
a steel case with a **solid 18K gold** top ring is `Steel: Stainless with Gold`. Gold *PVD* or *plating* does not count as gold.

| Source (first phrase of `material`) | Records | WatchCaseMaterial | MetalType | MetalColor |
|---|---|---|---|---|
| stainless steel case | 1547 | Stainless Steel | Steel: Stainless | — |
| stainless steel case + "18K … gold … top ring" | 30 | Stainless Steel | Steel: Stainless with Gold | — |
| titanium case | 100 | Titanium | Titanium | — |
| 18K rose gold case | 9 | Rose Gold | 18K Rose Gold | Rose |
| 18K yellow gold case | 2 | Yellow Gold | 18K Gold | Yellow |
| bronze case | 40 | *blank* (no Bronze in list) | Bronze | — |
| stainless steel/bronze case | 28 | *blank* (mixed metal) | *blank* (no combined value) | — |
| carbon fibre | 9 | *blank* (not in list) | Carbon | — |

DLC/PVD-coated steel cases ("…stainless steel case, black DLC plated") are mapped as stainless steel: the coating does not change the case metal.

---

## 3. Coverage summary

| Category | Columns | Count |
|---|---|---|
| ✅ Populated | A, B, C, D, E, F, G, H (gold cases only), I, J, K, N, S, U, AA, AB | 16 |
| ⚠️ Blank — ambiguous source | Q, T, W, Y, Z | 5 |
| ❌ Blank — no source data | L, M, O, P, R, V, X | 7 |
| ❌ Blank — unused | AC, AD | 2 |
| **Total** | | **30** |

---

## 4. Oris fields with no JewelCloud destination

All of these are preserved in the `SRC_` columns.

| Oris field | Reason not mapped |
|---|---|
| `id`, `familyId` | Internal Oris identifiers |
| `family` | Duplicates `name` in most records (differs in 160) |
| `productLink` | No product-URL column in the template |
| `calibre` | No calibre column |
| `powerReserve` | No power-reserve column |
| `limitedEdition` | No limited-edition flag |
| `hasStock` | Stock is not a template field |
| `color` | Ambiguous (see Q) |
| `size` | No reliable bucket rule (see Y) |
| `price.priceType`, `price.currency`, `price.active` | Used as mapping conditions, not destination values |

---

## 5. Source data quality notes

| Issue | Records | Handling |
|---|---|---|
| Leading whitespace in `name` / `family` | 955 | Trimmed in mapped columns; raw value kept in SRC_ |
| Trailing `\r\n` in `size`, `material`, `description` | 84 | Trimmed in mapped columns; raw value kept in SRC_ (quoted in CSV) |
| Embedded double quotes (e.g. `BC4 "Der Meisterflieger"`) | 6 | Escaped per RFC 4180 (`""`) |
| Non-ASCII characters (ö, ü, “ ” ’) | 24 | UTF-8 with BOM output |
| `price: null` | 156 | RetailPrice blank; SRC_price_* cells blank |
| `price.active = false` | 1256 | RetailPrice blank by default (configurable) |
| `calibre`, `winding`, `powerReserve` null | 17 | WatchMovement / WatchEnergy blank |
| `image2Url` empty | 1 | ImagePath2 blank |
| Image URLs with spaces | 1071 | Percent-encoded in ImagePath / ImagePath2 |

---

## 6. Source information needed to fill the blank fields

| JewelCloud column(s) | What would be needed |
|---|---|
| WatchBandMaterial, WatchBandType | Explicit strap/bracelet attributes. The reference suffix (e.g. `…07 4 25 01`, `FC`, `EB`, `-Set`) appears to encode this, but no documented key is available, so it is not decoded. |
| WatchCaseShape, WatchCrystalType, WatchDisplayType | Structured technical specification fields from Oris |
| WatchDialColor | Confirmation from Oris that `color` refers to the dial |
| WatchSize | JewelCloud mm ranges for Mini / Small / Mid Size / Extra Large / Extra Extra Large |
| MetalColor (non-gold cases) | Explicit case colour/finish attribute |
| Gender, PriceType, DeliveryTime | Business input from the retailer or brand, not product data |
