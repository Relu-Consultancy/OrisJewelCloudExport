# Oris → JewelCloud Timepieces Export

A .NET 10 console application that downloads the Oris watch catalogue from
`https://product-api.oris.ch/api/watch/all` and writes it as a JewelCloud Simple Product CSV
(Timepieces sheet). The complete source record is preserved in `SRC_` columns on the right.

**Output:**
- [`output/oris-jewelcloud-timepieces.csv`](output/oris-jewelcloud-timepieces.csv): 1,765 rows,
  30 JewelCloud columns + 23 `SRC_` columns, UTF-8 with BOM. This is the deliverable.
- [`output/oris-jewelcloud-report.html`](output/oris-jewelcloud-report.html): a visual run report
  (column coverage, mapped values, prices, source completeness, data quality). Open it in any browser.

---

## What I Completed

- **API retrieval:** HTTP GET with timeout, retry with exponential back-off for transient failures
  (network errors, timeouts, HTTP 408/429/5xx), immediate failure on other HTTP errors, and validation
  of the JSON shape (must be an array; items that are not objects are skipped with a warning).
- **JewelCloud mapping:** all 30 Timepieces columns, in template order. 16 are populated from the
  source; the rest are deliberately blank where the source cannot support them (see below).
- **Normalization:** winding → WatchEnergy/WatchMovement, material → WatchCaseMaterial/MetalType/MetalColor,
  `isArchived` → IsActive, URL encoding of image paths. Every output value is from the template's
  accepted-value lists.
- **SRC_ columns:** generated from the raw JSON, so new API properties are picked up automatically.
- **CSV output:** RFC 4180 quoting, UTF-8 with BOM, written to a temporary file and then swapped in, so
  a failed run never leaves a partial CSV.
- **Run summary:** after each run, the console prints per-column coverage and the reasons values were
  left blank, including any source value that was present but not recognised.
- **HTML run report:** generated with the CSV on every run. It is one self-contained file (data
  embedded as JSON, no external scripts) that shows coverage per column, the distribution of every
  converted value, how prices were treated, SRC_ completeness and data-quality issues.
- **Tests:** 68 xUnit tests covering normalizers, mapping, SRC_ flattening, CSV escaping, API
  error handling and the report.
- **Documentation:** this README plus [MAPPING.md](MAPPING.md) (column-by-column mapping and coverage).

## How to Run the Application

**Prerequisite:** .NET 10 SDK (`dotnet --list-sdks` should list a `10.0.x` version).

```bash
cd OrisJewelCloudExport

# Run the export (writes the CSV and the HTML report to output/, relative to the current directory)
dotnet run --project src/OrisJewelCloudExport

# Run the tests
dotnet test
```

Or open `OrisJewelCloudExport.sln` in Visual Studio and run the `OrisJewelCloudExport` project.
Visual Studio runs it from `bin/Debug/net10.0`, so the CSV is written under that folder unless
`OutputPath` is absolute.

### Configuration

Settings live in [`src/OrisJewelCloudExport/appsettings.json`](src/OrisJewelCloudExport/appsettings.json)
and can be overridden on the command line:

| Setting | Default | Command-line override |
|---|---|---|
| `ApiUrl` | `https://product-api.oris.ch/api/watch/all` | `--url <url>` |
| `OutputPath` | `output/oris-jewelcloud-timepieces.csv` | `--output <path>` |
| `ReportPath` | `output/oris-jewelcloud-report.html` (empty = no report) | `--report <path>` |
| `IncludeInactivePrices` | `false` | `--include-inactive-prices true` |
| `PriceCurrency` | `USD` | `--Export:PriceCurrency <code>` |
| `HttpTimeoutSeconds` | `120` | `--Export:HttpTimeoutSeconds <n>` |
| `MaxAttempts` | `3` | `--Export:MaxAttempts <n>` |

### Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Unexpected application error (full exception logged) |
| 2 | Source error: API unreachable, HTTP error, invalid or empty JSON |
| 3 | Invalid configuration |
| 4 | Cancelled (Ctrl+C) |

## Project Structure

```
OrisJewelCloudExport.sln
src/OrisJewelCloudExport/
  Program.cs                     Entry point: settings, error handling, exit codes
  ExportJob.cs                   Pipeline: fetch → map → SRC_ → write CSV → summary → HTML report
  Configuration/ExportSettings   appsettings.json + command-line binding and validation
  Source/OrisApiClient           HTTP download, retry, JSON shape validation
  Source/OrisWatch               Typed, null-tolerant view of the fields used for mapping
  Source/SourceTable             Raw JSON → SRC_ columns (generic flattening)
  Mapping/TimepieceColumns       The 30 template columns, in order
  Mapping/TimepieceColumnCatalog How each column is filled, or why it stays blank (used by report + tests)
  Mapping/TimepieceMapper        Oris watch → JewelCloud row
  Mapping/OrisValueNormalizer    Value conversions to JewelCloud accepted values
  Mapping/MappingDiagnostics     Counts of blank/unrecognised values for the run summary
  Output/CsvFileWriter           RFC 4180 CSV, UTF-8 with BOM
  Reporting/ExportReportBuilder  Summarises the run for the report
  Reporting/HtmlReportWriter     Injects the summary as JSON into report-template.html (embedded resource)
tests/OrisJewelCloudExport.Tests/ xUnit tests
output/                           Generated CSV and HTML report
MAPPING.md                        Full field mapping and coverage
```

**Design choice:** mapping and source preservation are two separate paths over the same raw JSON.
`OrisWatch` is a small typed view, used only for the fields being mapped. `SourceTable` never uses it
and works directly from the JSON, so new or renamed API properties can never be dropped from the
`SRC_` columns by a model that doesn't know about them.

## Field Mapping Decisions

The full table is in [MAPPING.md](MAPPING.md). These mappings needed interpretation:

| JewelCloud | Decision | Type |
|---|---|---|
| **Template sheet** | **Timepieces.** It is the only sheet with watch attributes. | Judgment |
| ProductType | `0` for every row: the brief says to treat all Oris products as Simple Products. | Fixed |
| Categories | `Watches` for every row. The endpoint only returns watches, and `Watches` is in the template's WATCH CATEGORIES list. | Defensible inference |
| IsActive | `isArchived: true` → `1` (inactive), `false` → `0`. Not `2` (deleted): archived watches are still real products. | Normalization |
| StyleNumber / RetailerStockNumber | `reference`, kept with its spaces. The template prefers letters and digits "if possible", but the spaced form is how Oris and retailers identify the watch. All 1,765 are unique. | Direct |
| ProductName / Description | Source `name` / `description`, trimmed. No "Designer – …" name composed and no brand prefix added; those would be new data. | Direct |
| WatchEnergy | "Automatic winding" (and variants such as "…, with red rotor") → `Automatic`; "Hand winding" → `Manual Mechanical`. This is the brief's own example. | Normalization |
| WatchMovement | Automatic and hand-wound movements are both `Mechanical`. | Normalization |
| WatchCaseMaterial | Taken only from the first phrase of `material` ("Multi-piece **stainless steel** case, …"). Later phrases describe the top ring or plating. Bronze, steel/bronze and carbon fibre are not in the accepted list → blank, not "Other". | Normalization |
| MetalType | Same case phrase. A steel case with a solid 18K gold top ring → `Steel: Stainless with Gold`; gold PVD/plating does not count as gold. | Normalization |
| MetalColor | Only for 18K rose/yellow gold cases, where the source names the colour. Blank for steel, titanium and others. | Normalization |
| ImagePath / ImagePath2 | Full URL with spaces percent-encoded (1,071 URLs contain spaces). Raw URL kept in SRC_. | Normalization |
| RetailPrice | `price.amount` only when the currency is USD and `price.active = true` (353 rows). The other 1,256 prices are marked inactive by Oris (all on archived watches), so they may not be current. Configurable with `IncludeInactivePrices`. | Judgment |

## Missing or Ambiguous Data

These columns are **intentionally blank**:

| JewelCloud column | Why blank | What would be needed |
|---|---|---|
| WatchDialColor | `color` ("Black") doesn't say whether it is the dial, strap or case colour. Some values aren't in the list either ("Grey" vs "Gray", "Multicoloured", "Skeleton"). | Confirmation that `color` = dial colour |
| WatchSize | `size` is a case diameter ("42.00 mm"; some "39.00 x 40.00 mm"). JewelCloud uses Mini … Extra Extra Large with no mm ranges defined. | JewelCloud size ranges |
| WatchBandMaterial, WatchBandType | No strap/bracelet field. The reference suffix seems to encode it but has no documented key, so decoding it would be a guess. | Strap attributes or a reference-code key from Oris |
| WatchType | Could only be guessed from collection names ("Divers", "ProPilot"). | Explicit watch type |
| WatchNumberType | Its accepted values mix dial numerals ("Applied Indexes") with movement types ("Mechanical Self Winding"), so the column's meaning is unclear. | JewelCloud clarification |
| PriceType | Pricing policy (Enforce MSRP, allow markups, call for price, …) is a retailer decision, not product data. | Retailer/brand input |
| WatchCaseShape, WatchCrystalType, WatchDisplayType | No source field. | Technical specification fields |
| Gender, DeliveryTime | No source field. | Retailer/brand input |
| MetalColor (non-gold) | Steel or titanium colour/finish is not stated. | Case finish attribute |
| ExtendedField1/2 | No agreed use. | — |

Source fields with no JewelCloud column (`calibre`, `powerReserve`, `limitedEdition`, `hasStock`,
`productLink`, `family`, …) are preserved in `SRC_` only.

## Source Data Strategy

The `SRC_` columns are built by [`SourceTable`](src/OrisJewelCloudExport/Source/SourceTable.cs)
directly from each raw `JsonObject`, independent of the typed model:

- **Naming:** `SRC_` + the JSON property name, unchanged (`SRC_imageUrl`, `SRC_limitedEdition`).
- **Nested objects:** flattened recursively with `_`: `price.amount` → `SRC_price_amount`.
- **Arrays:** kept as compact JSON text in one column (`SRC_tags` = `["a","b"]`). None exist today.
- **Values:** strings verbatim (not trimmed, so `"44.70 mm\r\n"` survives, quoted). Numbers and
  booleans as their JSON text (`7`, `true`).
- **Nulls:** JSON `null` and a missing property both become an empty cell. When `price` is `null`,
  its `SRC_price_*` cells are empty; no extra `SRC_price` column is added.
- **Column set and order:** the union of all properties across all records, in first-seen order.
  If Oris adds `"waterResistance": "10 bar"`, a `SRC_waterResistance` column appears automatically.
  A unit test covers exactly this case.
- **Name collisions** (e.g. a top-level `price_amount` next to `price.amount`) are detected and logged.

## Validation Performed

- 68 unit tests pass (`dotnet test`).
- The HTML report was rendered in headless Chrome in light and dark themes. It has no horizontal
  overflow at a 375px phone width, and source text cannot escape its embedded JSON (unit-tested
  with a `</script>` payload).
- A mutation check: when the CSV quoting rule was switched to CsvHelper's default with `NewLine` set,
  the round-trip test failed on a lone `\n`. That is why the explicit quoting rule is there.
- The generated CSV was re-read with Python's `csv` module (independent of this code):
  - BOM present; 1,765 rows × 53 columns; no ragged rows.
  - The JewelCloud header matches the template's Timepieces header exactly.
  - All 40,595 `SRC_` cells match the API JSON exactly.
  - Every mapped value is in the template's accepted-value lists.
  - RetailPrice is set only where `price.active = true`.

## Known Limitations

- **Text-pattern rules:** winding and material mapping rely on Oris's English phrasing ("Automatic
  winding", "Multi-piece … case"). New phrasing results in blank cells, not guesses, and is listed in
  the run summary, but it needs a rule update to be mapped.
- **Price:** USD only. Inactive prices are excluded by default; whether JewelCloud should receive
  them, and whether archived watches (1,560 of 1,765) should be exported at all, are business decisions.
- **SRC_ nulls:** `null`, empty string and a missing property are indistinguishable in the SRC_
  columns.
- **SRC_ column positions:** the SRC_ column set follows the data, so a new API property adds a
  column. Downstream consumers should read by header name, not position.
- **Collections:** JewelCloud requires collections to exist before import. The 28 Oris collection
  names are not checked against JewelCloud.
- **Memory:** the whole response (about 1.5 MB) is loaded into memory. That's fine at this size, but
  a much larger catalogue would need streaming.
- **No live-API test:** the API client is tested with stubbed HTTP responses; there is no automated
  test against the live API.
- **Report fonts:** the HTML report loads IBM Plex from Google Fonts when online and falls back to
  system fonts offline. Everything else in it is inline.

## Next Steps

1. Confirm the open questions with JewelCloud/Oris: meaning of `color`, WatchSize ranges, PriceType
   policy, inactive prices, and whether archived products belong in the feed.
2. Get Oris's reference-code key (or a detail endpoint) to fill band material/type, crystal, case
   shape and display type.
3. Move the value lookup tables from code into a configuration file, and load the accepted values
   from the template so the run fails or warns on any value JewelCloud would reject.
4. Save each run's report data as JSON as well, so coverage can be compared between runs to spot
   regressions when the Oris feed changes.
5. Use streaming JSON parsing and a resilience library (e.g. Polly) if the catalogue or call volume grows.

## Total Time on Project

_To be filled in:_ ___ minutes.
