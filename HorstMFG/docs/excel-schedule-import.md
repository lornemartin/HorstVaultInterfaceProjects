# Excel Schedule & Batch Import — Implementation Plan

## Goal

Add two new import flows to HorstMFG, sharing one architecture:

- **Schedule import** — user uploads a schedule Excel (`OrderNumber`,
  `Qty`, `ProductNumber` per row). Creates a `Schedule` with
  `ScheduleOrder` records.
- **Batch import** — user uploads a batch Excel (`ProductNumber`, `Qty`
  per row). Creates a `Batch` with `BatchProduct` records.

In both cases, HorstMFG queries Vault server-side via a new
`HorstMFG.VaultGateway` service to retrieve BOMs for each product,
populates `PartLineItem` rows, and copies the corresponding PDFs from the
network share to local storage as part of the import.

Replaces the current manual workflow:
1. Run VaultItemProcessor → import batch from CSV → export tab-delimited BOM
2. Upload that TSV through the existing `BomImport.razor` page

After this work, step 1 is no longer needed — the user goes straight from
Excel to a populated schedule or batch.

The existing TSV-based import path (`BomImport.razor`) remains as a
fallback and stays untouched.

---

## Sample input

### Schedule Excel — sample available

`S:\Shop Schedule Drawings\YYYY\MM Month\YYYY-MM-DDDD\YYYY-MM-DDDD <Name>.xlsx`

| Cell      | Meaning                                               |
| --------- | ----------------------------------------------------- |
| A1        | Schedule name (e.g. `2026-04-2930`)                   |
| Col A     | Order Number (A-prefix; trailing whitespace, trim)    |
| Col D     | Qty                                                   |
| Col E     | Product Number — must match Vault Item Number exactly |

**Special row types observed in sample:**

- **"LA-" continuation rows** — product cell contains just `LA-` with no
  suffix. These represent extra/unspecified items belonging to the
  preceding real product row. The qty on these rows is the count of the
  extras.
- **All-LA orders** (e.g. order A639141 in sample) — every row for that
  order is `LA-`. Treat as an order with no product number, captured as
  an empty ScheduleOrder with notes.

Other columns (B = date, C = numeric, F = `D` flag) are ignored for V1.

### Batch Excel — format confirmed against `docs/Sample Batch.xlsx`

| Cell  | Meaning                                                |
| ----- | ------------------------------------------------------ |
| A1    | Batch name (e.g. `Snow Blade Batch 13 2026`)           |
| Col A | Qty                                                    |
| Col B | Product Number — must match Vault Item Number exactly  |

**Observations from the sample:**

- Sheet name is `batch` (parser reads sheet 1 regardless of name).
- Product numbers carry multiple prefixes — `BAT-`, `LA-`, `LP-` all
  appear. The prefix has no special meaning to the importer; it's just
  the Vault Item Number as-is.
- One product appears twice with different quantities
  (`BAT-SB4203W1016` qty 10 in row 14, qty 40 in row 27 — see
  Open Question #1 below for handling).
- No LA- continuation rows.
- No order numbers (batches are not order-driven).

Likelihood of a Vault item being missing is lower for batches than
schedules but must still be handled.

---

## Decisions already locked

| Decision                       | Choice                                                                |
| ------------------------------ | --------------------------------------------------------------------- |
| Multi-product schedule orders  | Suffix order numbers (`a`, `b`, `c`…) per the existing manual pattern |
| Schedule LA- continuation rows | Append to `Notes` field of the preceding `ScheduleOrder`              |
| Duplicate product in batch Excel | Merge into one `BatchProduct` (sum qty), surface a warning in the upload preview, record the merge source in `BatchProduct.Notes` |
| Batch import                   | Same architecture as schedule import; separate parser + ingest path because target entities differ |
| Vault connectivity             | New server-side `HorstMFG.VaultGateway` Windows Service that references `VaultAccess.dll` |
| Communication channel          | HTTP between HorstMFG.Web and VaultGateway, both on the same server   |
| Long-running import handling   | Async job pattern — VaultGateway returns a job id immediately and pushes per-item results back to HorstMFG.Web via callback as they complete |
| Item refresh before BOM read   | Always run the existing `UpdateItem` promote-components routine on the top-level item and every BOM child before extracting BOM data |
| Vault item not found           | Create empty `ScheduleOrder` / `BatchProduct` with the product number stored; allow re-import later |
| PDF copy scope                 | In scope for V1. Each callback that successfully ingests a BOM also copies its PDFs from the share to the local folder before completing. |
| Bridge involvement             | None for this feature. Bridge keeps its existing per-user file operations untouched. |

---

## Architecture overview

```
HorstMFG Server (Windows Server + IIS)
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  HorstMFG.Web (.NET 8, in IIS)  ◄──── Blazor SignalR ────►   │  Browser
│      │   ▲                                                   │
│      │   │ HTTP callback (per-item result)                   │
│      │   │                                                   │
│      ▼   │                                                   │
│  HorstMFG.VaultGateway (.NET Framework 4.8 Windows Service)  │
│      │                                                       │
│      │ references VaultAccess.dll                            │
│      │                                                       │
│      └────────────────► Vault SDK (Autodesk.Connectivity.*)  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
                          │
                          ▼
                    Vault Server
```

Three processes on one Windows server, sharing `VaultAccess.dll` as a
referenced library (no source duplication):

| Process                                | Runtime                            | Role                                            |
| -------------------------------------- | ---------------------------------- | ----------------------------------------------- |
| HorstMFG.Web                           | .NET 8 (in IIS)                    | Web UI, DB access, orchestration, PDF copy      |
| HorstMFG.VaultGateway                  | .NET Framework 4.8 Windows Service | Vault SDK access for BOM lookups                |
| Vault Job Processor (existing, future) | .NET Framework 4.8                 | PDF generation jobs (forward-looking — see below) |

---

## Dev / debugger setup

Both processes can run side-by-side on a Windows dev machine without
IIS — useful for end-to-end testing before deployment.

| Process              | How to run in dev                                        |
| -------------------- | -------------------------------------------------------- |
| HorstMFG.Web         | Visual Studio → F5 (Kestrel, e.g. `https://localhost:7xxx`) |
| HorstMFG.VaultGateway | Console app via `--console` switch (same pattern as Bridge), launched from a second VS instance or `dotnet run`. Listens on `http://127.0.0.1:5050`. |

**Configuration**

`appsettings.Development.json` in HorstMFG.Web:

```jsonc
{
  "VaultGateway": {
    "BaseUrl": "http://127.0.0.1:5050",
    "CallbackApiKey": "<dev-key>"
  }
}
```

The callback URL the Gateway uses to reach HorstMFG.Web is passed in
each request body (`callbackUrl` field), so it dynamically picks up
whatever Kestrel port VS is using — no hardcoding needed.

**Prerequisites**

- Vault client installed on the dev machine (the Gateway is .NET
  Framework 4.8 and uses the COM-backed Vault SDK — same prerequisite
  VaultItemProcessor has today).
- Network reachability from dev machine to `hwvsvt04`.

**HTTPS dev cert gotcha**

If HorstMFG.Web is bound to HTTPS, the Gateway's `HttpClient` must
trust the VS dev cert when posting callbacks. Usually fine after
`dotnet dev-certs https --trust`. If you hit cert validation errors:

- Easiest: bind HorstMFG.Web to HTTP in dev (Gateway is localhost-only,
  so HTTP is acceptable in dev) by setting the callback URL to
  `http://localhost:5xxx/...`.
- Or: in the Gateway's dev build, wire a permissive
  `ServerCertificateCustomValidationCallback` on the callback
  `HttpClient`, gated by `IHostEnvironment.IsDevelopment()`.

---

## Decisions deferred

- **Concurrent license capacity.** Vault Pro uses concurrent network
  licenses; the VaultGateway adds one slot from the pool while it holds
  an open connection. Confirm with Autodesk's license-usage report that
  pool capacity is sufficient before deploying. If marginal, the gateway
  can connect on-demand (per-job) and disconnect when idle.

- **Vault account for the Gateway.** The plan originally proposed reusing
  the existing `JobProcessor` account, but Phase 4 testing surfaced that
  `JobProcessor` lacks permission to run the promote-components routine
  that `UpdateItem` depends on (Vault SDK error 303/155 from `EditItems`
  / `UndoEditItems`). Options:
  - Grant `JobProcessor` the additional permissions it needs (Edit Item +
    Promote Components),
  - Or use a different prod account that already has those rights.
  Dev currently uses `lorne/lorne` (same as Bridge dev). Decision needed
  before deploying to prod.

---

## Data model

### Existing entities used as-is

`Schedule`, `ScheduleOrder`, `Batch`, `BatchProduct`, `PartLineItem` —
unchanged in shape except for the additions below.

### Schema additions

#### `ScheduleOrder` — three new columns

```csharp
public string? ProductNumber { get; set; }      // LA-xxx as parsed from Excel
public bool VaultBomImported { get; set; }      // true when BOM successfully retrieved
public string? Notes { get; set; }              // LA- continuation row info
```

Rationale:

- `ProductNumber` becomes a first-class field instead of being derived
  from `PartLineItem` rows where `Category="product"`. Required because
  for orders Vault doesn't have, we still need to remember what we tried
  to import (so re-import works).
- `VaultBomImported` flags whether re-import is needed.
- `Notes` captures the LA- continuation rows verbatim — one line per
  continuation row, in the form `LA- {qty}` (just a literal copy of what
  the row contained). Multiple continuations stack as separate lines.

#### `BatchProduct` — two new columns

```csharp
public bool VaultBomImported { get; set; }      // true when BOM successfully retrieved
public string? Notes { get; set; }              // import-time annotations (e.g. duplicate-row merge source)
```

`BatchProduct.ProductName` already exists and serves the same role as
`ScheduleOrder.ProductNumber` (both are matched against Vault Item
Number). `Notes` records when an import merged duplicate rows
(e.g. `"Merged from Excel rows 14, 27 (qty 10 + 40)"`).

#### `BomService.GetFlatScheduleRows` change

Today, `BomService.cs:472-481` derives `ProductNumber` from
`PartLineItem` where `Category="product"`. After this change, it reads
from `ScheduleOrder.ProductNumber` directly. The migration backfills
existing rows from the existing derivation logic so behavior is preserved
for old data.

#### Migration

```
20260427000000_ExcelScheduleAndBatchImport.cs
```

- Adds `ProductNumber`, `VaultBomImported`, `Notes` to `schedule_orders`
- Adds `VaultBomImported`, `Notes` to `batch_products`
- Backfills:
  ```sql
  UPDATE schedule_orders so
  SET product_number = (
      SELECT pli.part_number
      FROM part_line_items pli
      WHERE pli.schedule_order_id = so.id AND pli.category = 'product'
      LIMIT 1
  );
  UPDATE schedule_orders SET vault_bom_imported = true WHERE product_number IS NOT NULL;
  UPDATE batch_products SET vault_bom_imported = true;  -- existing batches assumed complete
  ```

---

## End-to-end flow

Identical for schedule and batch imports — only the target entities and
parser differ.

```
┌─────────────┐  1. upload xlsx  ┌────────────┐  2. INSERTs  ┌──────────┐
│   Browser   │ ───────────────► │ HorstMFG   │ ───────────► │ Postgres │
│ (Blazor UI) │                  │  .Web      │              └──────────┘
└─────────────┘                  │            │
       ▲                         │ 3. POST /api/import-bom-batch (HTTP)
       │ 8. live UI updates      ▼
       │ via Blazor SignalR    ┌─────────────────────┐  4. UpdateItem +
       │                       │ HorstMFG.VaultGateway│ ──── BOM extract ────► Vault
       │                       │   (.NET 4.8 service) │ ◄────                  Server
       │                       └─────────┬────────────┘
       │ 7. INSERT PartLineItems          │ 5. POST /api/internal/vault-bom-result
       │    + copy PDFs (per item)        │    (one POST per item, as completed)
       │    set VaultBomImported=true     ▼
       │                       ┌─────────────────────┐
       └───────────────────────│ HorstMFG.Web         │
                               │   callback handler   │
                               └──────────────────────┘
                                          │
                                          ▼ 6. PDF copy from share to local
                                    \\share\PDF Drawing Files\
                                          → C:\HorstMFG\PDFs\<Schedules|Batches>\<name>\
```

### Step-by-step

1. **Excel parse** (in HorstMFG.Web) — parser depends on import type
   (schedule vs batch). Reject and report rows that don't fit the
   expected shape.
2. **Header + child entity creation** — create the `Schedule`/`Batch`
   header, then one `ScheduleOrder`/`BatchProduct` per real product row.
   For schedules: suffix order numbers when an A-number repeats; attach
   LA- continuations to the preceding ScheduleOrder's `Notes`. Save with
   `VaultBomImported = false`. Commit transaction.
3. **Dispatch to VaultGateway** — HorstMFG.Web POSTs the list of
   `{trackingId, productNumber}` to `http://localhost:<port>/api/import-bom-batch`
   along with the callback URL. `trackingId` is opaque to the Gateway —
   it's a `ScheduleOrderId` for schedule imports, a `BatchProductId` for
   batch imports. HorstMFG.Web tracks which kind based on the import job
   it's running. Gateway responds immediately with a `jobId`.
4. **VaultGateway processes** — for each item:
   - Look up Vault Item by item number
   - If not found: send a "not found" result for this item, continue
   - If found: call the consolidated `UpdateItem` (promote components)
     routine on the top-level item and every child in the BOM
   - Walk the resulting `PkgItemsAndBOM`, project to `VaultBomItem` DTO
5. **Per-item callback** — VaultGateway POSTs the result for each item
   back to HorstMFG.Web's callback endpoint as soon as it finishes (so
   partial progress is visible mid-import).
6. **PDF copy** (HorstMFG.Web) — for each child PartLineItem with
   `RequiresPdf` and an existing PDF on the share, copy from share to the
   local folder for this Schedule/Batch.
7. **HorstMFG.Web ingests** — the callback handler validates the auth
   token, hands the payload to `VaultBomIngestService` which creates
   `PartLineItem` rows for the matching parent and sets
   `VaultBomImported = true`. Failures are recorded.
8. **UI live update** — Blazor Server pushes the update to the import
   page via the existing `/_blazor` circuit. The user sees rows fill in
   as they complete.

---

## Excel parsing

### Library

`ClosedXML` (NuGet — pure managed, no Excel install needed, MIT-licensed).
EPPlus would also work but its license is restrictive for commercial use
after v4.

### Parser contracts

```csharp
// HorstMFG.Web/Services/ExcelScheduleParser.cs
public record ParsedScheduleRow(int RowIndex, string OrderNumber, int Qty, string ProductNumber);
public record ParsedSchedule(string Name, List<ParsedScheduleRow> Rows, List<string> Errors);

public class ExcelScheduleParser
{
    public ParsedSchedule Parse(Stream xlsx);
}

// HorstMFG.Web/Services/ExcelBatchParser.cs
public record ParsedBatchRow(int RowIndex, string ProductNumber, int Qty);
public record ParsedBatch(string Name, List<ParsedBatchRow> Rows, List<string> Errors);

public class ExcelBatchParser
{
    public ParsedBatch Parse(Stream xlsx);
}
```

Two parsers (not one with a mode flag) because the row shapes,
validation rules, and grouping logic differ enough that branching inside
one method would obscure the rules.

### Schedule parser rules

1. Trim whitespace from `OrderNumber` and `ProductNumber`.
2. Skip rows where order number AND product number are both blank.
3. Add row to `Errors` if order number is blank but product number is set
   (or vice versa).
4. Treat `LA-` (with nothing after the dash) as a "continuation" marker.

Grouping logic in `ExcelScheduleImportService`:

```
foreach row in parsed.Rows:
    if row.IsContinuation:
        attach to most recent non-continuation row in same OrderNumber
        if no such row exists yet, attach to a placeholder for that order
    else:
        record it; if OrderNumber repeats within file, suffix a/b/c...
```

Edge case: 27th product on a single order would exhaust single letters.
For V1, throw a validation error with a clear message — sample data tops
out at 2 products per order.

### Batch parser rules

1. Read batch name from `A1`.
2. Read rows 2..N: column A = qty, column B = product number.
3. Trim whitespace from `ProductNumber`.
4. Skip rows where both columns are blank.
5. Add to `Errors` if qty present but product number missing.
6. No continuation markers.

### Batch grouping logic in `ExcelBatchImportService`

Walk parsed rows in order:

```
foreach row in parsed.Rows:
    if a BatchProduct already exists in this import for row.ProductNumber:
        add row.Qty to its qty
        append "merged from row N (+qty)" to its Notes
        record a warning with both row indices
    else:
        create a new BatchProduct
```

The upload preview surfaces the warning list before the user clicks
**Import** so they can spot data-entry mistakes (e.g. an unintended
duplicate). The merge proceeds regardless once Import is clicked.

---

## VaultGateway service

### Project

```
HorstMFG.VaultGateway/
├── HorstMFG.VaultGateway.csproj    — .NET Framework 4.8 Worker Service
├── Program.cs                      — Host setup, Windows Service / console switch
├── Worker.cs                       — Hosts Kestrel on a localhost-bound port
├── Controllers/
│   └── ImportController.cs         — POST /api/import-bom-batch, GET /api/jobs/{id}
├── Services/
│   ├── VaultBomService.cs          — Wraps VaultAccess BOM extraction
│   └── ImportJobRunner.cs          — Manages async jobs, dispatches callbacks
├── Dtos/
│   ├── ImportBomBatchRequest.cs
│   └── VaultBomResult.cs
└── appsettings.json
```

**Project references:** `VaultAccess` (existing .NET Framework 4.8
library shared with Bridge and ItemExport).

The Gateway is intentionally agnostic to schedule vs batch — it only
knows about `{trackingId, productNumber}` pairs.

### Configuration — appsettings.json

```jsonc
{
  "Gateway": {
    "ListenUrl": "http://127.0.0.1:5050",     // localhost only
    "CallbackApiKey": "..."                   // shared with HorstMFG.Web
  },
  "Vault": {
    "Server": "hwvsvt04",
    "VaultName": "Vault",
    "Username": "JobProcessor",
    "Password": "..."                          // configured in appsettings.Production.json, not in repo
  }
}
```

Bound to `127.0.0.1` so the gateway is only reachable from the same
machine. Authentication is a shared API key in the request header.

### HTTP API

#### `POST /api/import-bom-batch`

Submit a batch of items to fetch.

Request body:

```jsonc
{
  "callbackUrl": "https://horstmfg.local/api/internal/vault-bom-result",
  "items": [
    { "trackingId": 1234, "productNumber": "LA-SBSWC48RR800" },
    { "trackingId": 1235, "productNumber": "LA-HDN7560VOL50Q" }
  ]
}
```

Response (202 Accepted, processed asynchronously):

```jsonc
{ "jobId": "f1a2b3c4-..." }
```

#### `GET /api/jobs/{jobId}`

Status / progress endpoint (mainly for diagnostics; routine flow uses
the callback).

#### Per-item callback (Gateway → HorstMFG.Web)

`POST <callbackUrl>` with header `X-Gateway-Key: <CallbackApiKey>`.

```jsonc
{
  "jobId": "f1a2b3c4-...",
  "trackingId": 1234,
  "found": true,
  "topLevelItem": {
    "number": "LA-SBSWC48RR800", "title": "...", "category": "product",
    "material": null, "thickness": null, "operations": null,
    "structCode": null, "revision": "A"
  },
  "children": [
    {
      "number": "B12345", "title": "Bracket Left",
      "category": "purchased|laser|formed|...", "qty": 4,
      "material": "MS", "thickness": "SH-062",
      "operations": "Laser, Form", "structCode": "...", "revision": "A"
    }
  ]
}
```

Failure: `{ jobId, trackingId, found: false, errorMessage: "..." }`.

The shape of `children` mirrors the columns produced by
VaultItemProcessor's existing `ExportToPackage(FileFormat.TDL_LEVEL, …)`
mapping (`VaultAccess.cs:1977`) so HorstMFG.Web's ingest reuses the
field translation already living in `BomService.ParseExportFile`.

### `VaultAccess` additions

#### 1. Consolidate the `UpdateItem` promote routine

Today the routine exists in two places — `ItemExport/ItemExportCommandExtension.cs:672`
and a copy in `VaultAccess` (the file in ItemExport carries a comment
saying so). Move the canonical implementation to a single
`VaultAccess.ItemPromoter` class. Update ItemExport to call into it
instead of holding its own copy.

Small but valuable cleanup — no behavior change, just removes the
"please remember to update both places" footgun.

#### 2. New `GetItemBom` method

`GetItemBom` is a new wrapper, not an SDK function. It sits on top of
SDK calls VaultAccess already uses
(`PackageService.GetLatestPackageDataByItemIds`,
`ItemService.GetItemsByItemNumbers`,
`ItemService.GetPrimaryComponentsByItemIds`).

```csharp
public class VaultBomQueryService
{
    public VaultBomResult? GetItemBom(string itemNumber, bool refreshFromSource);
}

public record VaultBomResult(VaultBomItem TopLevel, List<VaultBomItem> Children);
public record VaultBomItem(
    string Number, string Title, string Category, int Qty,
    string? Material, string? Thickness, string? Operations,
    string? StructCode, string? Revision);
```

Implementation:

1. `ItemService.GetItemsByItemNumbers(new[] { itemNumber })` → top-level item.
2. If null/empty → return null (caller treats as "not found").
3. If `refreshFromSource = true`:
   - `ItemPromoter.UpdateItem(topLevel)` (consolidated routine).
   - For each child item id: `ItemPromoter.UpdateItem(child)`.
4. `PackageService.GetLatestPackageDataByItemIds([topLevel.Id], BOMTyp.Latest)`.
5. Walk `PkgItemsAndBOM`, project to `VaultBomResult`, reusing the
   `MapPair[]` field list already used by `ExportToPackage` so the field
   set matches the existing TSV schema HorstMFG already ingests.

Import path always passes `refreshFromSource = true` — same behavior
as today's ItemExport BOM export. Cheaper read-only callers (preview,
lookup) can pass `false` later if/when added.

---

## PDF copy handling

Both import flows must copy referenced PDFs from the share to local
storage as part of the import. Existing code in
`BomService.cs:217, 249, 328, _localPdfPath / _pdfSharePath` already
implements the copy logic for the TSV path; refactor it into a shared
helper.

### New helper — `BomPdfCopyService`

```csharp
public class BomPdfCopyService
{
    Task CopyForScheduleAsync(string scheduleName, IEnumerable<PartLineItem> parts, CancellationToken ct);
    Task CopyForBatchAsync(string batchName, IEnumerable<PartLineItem> parts, CancellationToken ct);
}
```

For each part:
- Skip if `RequiresPdf == false`.
- Source path: `{_pdfSharePath}\{PartNumber}.pdf` (existing convention).
- If source missing: leave `HasPdf = false`, log, continue.
- If source present: copy to
  `{_localPdfPath}\Schedules\{name}\{PartNumber}.pdf` (or `Batches\{name}\…`).
- On success: set `HasPdf = true` on the PartLineItem.
- Parallelism: copy up to 4 files concurrently (matches existing pattern
  in `BomService.ParseExportFile`).

`BomService.ImportBatchAsync` and `ImportScheduleAsync` are updated to
call this helper instead of copying inline. No behavior change for the
existing TSV path; new code path uses the same helper.

### When the copy runs in the new flow

In `VaultBomIngestService.IngestSingleAsync` — after the PartLineItems
for one ScheduleOrder/BatchProduct are saved, immediately call
`BomPdfCopyService` for that order's parts before the method returns.
That way the UI's "Imported" status is only set once both BOM data
and PDFs are in place; if PDF copying fails for any individual part,
the order is still marked imported but those specific parts retain
`HasPdf = false` and can be flagged in the UI.

PDF copy failures don't fail the whole ingest — partial success is
visible in the per-row status.

---

## HorstMFG.Web changes

### Files touched / created

```
HorstMFG.Core/Entities/Engineering/ScheduleOrder.cs            (modify — add 3 fields)
HorstMFG.Core/Entities/Engineering/BatchProduct.cs             (modify — add VaultBomImported)
HorstMFG.Infrastructure/Data/ApplicationDbContext.cs           (modify — column config)
HorstMFG.Infrastructure/Data/Migrations/<new>.cs               (new)
HorstMFG.Web/Services/ExcelScheduleParser.cs                   (new)
HorstMFG.Web/Services/ExcelBatchParser.cs                      (new)
HorstMFG.Web/Services/ExcelScheduleImportService.cs            (new)
HorstMFG.Web/Services/ExcelBatchImportService.cs               (new)
HorstMFG.Web/Services/VaultGatewayClient.cs                    (new — typed HttpClient wrapper)
HorstMFG.Web/Services/VaultBomIngestService.cs                 (new)
HorstMFG.Web/Services/BomPdfCopyService.cs                     (new — extracted from BomService)
HorstMFG.Web/Controllers/InternalCallbackController.cs         (new — POST /api/internal/vault-bom-result)
HorstMFG.Web/Components/Pages/Engineering/ExcelScheduleImport.razor (new)
HorstMFG.Web/Components/Pages/Engineering/ExcelBatchImport.razor    (new)
HorstMFG.Web/Components/Layout/NavMenu.razor                    (modify — links)
HorstMFG.Web/Services/BomService.cs                             (modify — read ProductNumber from column; delegate PDF copy to helper)
HorstMFG.Core/DTOs/FlatSchedulePartRow.cs                       (modify — drop derivation)
HorstMFG.Web/appsettings.json                                   (modify — Gateway URL + callback key)
```

### Import service orchestration

```csharp
public class ExcelScheduleImportService
{
    Task<ImportResult> ImportAsync(Stream xlsx, CancellationToken ct);
}

public class ExcelBatchImportService
{
    Task<ImportResult> ImportAsync(Stream xlsx, CancellationToken ct);
}
```

Both follow the same pattern:

1. Parse Excel with the appropriate parser.
2. Open transaction.
3. Insert header (`Schedule`/`Batch`).
4. Insert child entities (`ScheduleOrder`/`BatchProduct`), applying
   suffix logic and notes for schedules.
5. Commit.
6. Call `VaultGatewayClient.SubmitBatchAsync(...)`, get back `jobId`.
7. Track `(jobId, importType)` so callbacks know whether trackingId
   refers to a `ScheduleOrderId` or a `BatchProductId`.
8. Return `(parentId, jobId, summary)` to the page.

### `VaultBomIngestService`

```csharp
public class VaultBomIngestService
{
    Task IngestSingleAsync(VaultBomCallbackPayload payload, CancellationToken ct);
}
```

Internally:

- Looks up the in-flight job for this `jobId` to find the import type.
- Routes to `IngestForScheduleOrder(...)` or `IngestForBatchProduct(...)`.
- For successful results: creates `PartLineItem` rows, then immediately
  invokes `BomPdfCopyService` for that order/product's parts, then sets
  `VaultBomImported = true`.
- For failures: leaves the parent empty, records `errorMessage`.

Reuses field-mapping helpers from `BomService` — extract those into
`BomFieldMappers` static class so all consumers share them (thickness
lookup, IsStock derivation, etc.).

### `InternalCallbackController`

```csharp
[ApiController]
[Route("api/internal")]
public class InternalCallbackController : ControllerBase
{
    [HttpPost("vault-bom-result")]
    public async Task<IActionResult> VaultBomResult(
        [FromHeader(Name = "X-Gateway-Key")] string apiKey,
        [FromBody] VaultBomCallbackPayload payload)
    {
        // Validate key against config, hand to VaultBomIngestService,
        // push update to UI via IHubContext<...> or Blazor circuit notification
    }
}
```

### UI — two pages

`ExcelScheduleImport.razor` and `ExcelBatchImport.razor` share the same
two-state pattern:

1. **Upload** — drag/drop Excel file, parse preview shows
   `Name + N orders/products`, validation errors listed inline. Buttons:
   `Cancel`, `Import`.
2. **Importing / Done** — table of orders/products, color-coded per-row
   status (`Pending`, `Imported`, `Vault item not found`, `Vault error`,
   `PDFs missing`). Updates live as callbacks arrive. Per-row `Retry`
   button on failures.

Common UI pieces extracted into a shared component if duplication looks
heavy after the first cut.

Entry points in nav: **Engineering → Schedule from Excel** and
**Engineering → Batch from Excel** (alongside the existing
**BOM Import** page that handles TSV).

---

## Re-importing later

Same `POST /api/import-bom-batch`; just send a payload limited to the
items that need re-running.

UI:

- On the schedule/batch detail page, add a **Re-import BOM** button next
  to each child where `VaultBomImported = false`.
- Bulk **Re-import all missing BOMs** button on the parent header.

Server:

- Reuses `VaultBomIngestService` exactly. New ingest replaces any
  existing `PartLineItem` rows for that parent (delete-then-insert) so
  the operation is idempotent. PDF copy re-runs as part of the ingest.

---

## Error handling

| Scenario                                       | Behavior                                                       |
| ---------------------------------------------- | -------------------------------------------------------------- |
| Excel parse error (bad columns, missing A1, etc.) | Stop before any DB write; show error list, no Schedule/Batch created. |
| VaultGateway service down / unreachable        | `ImportAsync` fails with a clear error; Schedule/Batch + children are still committed (so the user can retry without re-uploading). All rows show `Pending - Gateway offline`. |
| Vault server unreachable from Gateway          | Per-item failures bubble back via callback as `errorMessage`; bulk retry. |
| Vault item not found                           | Parent left empty; row flagged `Not in Vault`. Re-import visible. |
| PDF source file missing on share               | PartLineItem keeps `HasPdf = false`; row shows `PDFs missing` warning; ingest still succeeds. |
| PDF copy I/O failure                           | Same as above — logged, row flagged, ingest succeeds.          |
| Callback authentication fails                  | HorstMFG.Web returns 401; Gateway logs and continues with next item. |
| One bad item in a batch                        | Other items still processed; failed item flagged via its own callback. |
| Gateway crashes mid-job                        | Job state is in-memory only — restart leaves any unfinished items as `Pending`. User clicks Retry to reissue them. |

---

## What's deliberately out of scope for V1

- Editing/renaming the parsed schedule/batch name before commit.
- Bulk-import multiple Excel files at once.
- Persistent Gateway job state across restarts (in-memory only for V1).
- Batch Excel format finalization without a real sample (the working
  assumption is recorded above; revisit when a sample exists).

---

## Forward-looking: PDF on-demand and Job Processor co-location

(Not part of this feature. Included so the architecture decision is on
record for when PDF-on-demand work resumes.)

The `HorstMFG.VaultGateway` pattern — a .NET Framework process living
on the same server as HorstMFG.Web, sharing `VaultAccess.dll`, called
via HTTP — generalizes naturally to PDF generation:

- The existing Vault Job Processor (with `JobProcessorPDFPrint`
  extension) is already a .NET Framework Windows service.
- Co-locating it on the HorstMFG server lets HorstMFG.Web submit jobs
  to Vault's job queue (via VaultAccess) and the local JP picks them up
  with near-zero latency.
- The watchdog (`MonitorVaultJP.ps1`) co-locates with the thing it
  watches.

So this work doesn't just unblock Excel imports — it lays the
groundwork for replacing the separate JP machine with a server-local
install when the PDF-on-demand effort resumes.

---

## Open questions for confirmation

1. **Concurrent license pool capacity** — Gateway will use the existing
   `JobProcessor` account on `hwvsvt04`/`Vault`, which means it adds
   one slot of overlap with the existing Job Processor process whenever
   both are connected at the same time. Confirm with Autodesk's
   license-usage report that this is fine, or that the Gateway should
   connect on-demand only.

---

## Phased delivery

| Phase | Scope                                                                                | Verify by                                       |
| ----- | ------------------------------------------------------------------------------------ | ----------------------------------------------- |
| 1     | Schema + migration + ProductNumber backfill on ScheduleOrder; VaultBomImported on BatchProduct | Existing schedules/batches still render correctly |
| 2     | Consolidate `UpdateItem` into VaultAccess; add `GetItemBom`                          | Run from a small console harness against a known item; compare output to existing ItemExport TSV |
| 3     | VaultGateway service shell — HTTP listener, job runner, no Vault yet                 | POST a fake batch, verify callback fires with stub data |
| 4     | Wire VaultGateway → VaultAccess.GetItemBom                                           | Run gateway, POST a real batch, verify per-item callbacks land at HorstMFG.Web |
| 5     | Extract `BomPdfCopyService` from BomService; `VaultBomIngestService` ingests + copies PDFs | Manual: feed a fake callback at HorstMFG.Web, verify PartLineItems + PDFs land |
| 6     | Schedule import: `ExcelScheduleParser` + `ExcelScheduleImportService` + `ExcelScheduleImport.razor` | Upload sample schedule file end-to-end          |
| 7     | Batch import: `ExcelBatchParser` + `ExcelBatchImportService` + `ExcelBatchImport.razor` | Upload sample batch file end-to-end             |
| 8     | Re-import button + Gateway-offline handling + error states                           | Manual: stop gateway, import, restart, retry    |

Each phase ends in a working, mergeable state. Phase 7 can be parallelized
with Phase 8 once Phase 6 is in.
