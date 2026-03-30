# HorstMFG Nesting Bridge — Implementation Plan

## What it is

A .NET Framework 4.8 Worker Service that runs on each Radan workstation as a Windows
Service in production, or as a console app for debugging (`--console` launch argument).
It acts as the intermediary between HorstMFG and the nesting software (currently Radan),
since the web app has no direct access to the local file system or Radan's COM interface.

---

## Solution structure

```
HorstMFG.Bridge/
├── Program.cs                         — Host setup, Windows Service / console switch
├── Worker.cs                          — Manages SignalR connection lifecycle + reconnect
├── BridgeHub.cs                       — SignalR hub client method handlers
├── FileWatcher.cs                     — Monitors active RPD file, triggers auto-sync
├── Handlers/
│   ├── SendToNestingHandler.cs
│   ├── RetrieveFromNestingHandler.cs
│   ├── RetrieveFromVaultHandler.cs
│   ├── SyncHandler.cs
│   ├── FinalizeHandler.cs
│   └── UpdateThumbnailHandler.cs
├── Nesting/
│   ├── INestingProjectService.cs      — Generic interface (software-agnostic)
│   └── Radan/
│       ├── RadanProjectService.cs     — Implements INestingProjectService
│       └── RpdService.cs             — Low-level RPD XML read/write
├── Vault/
│   ├── IVaultService.cs              — Generic interface
│   └── VaultService.cs              — Wraps VaultAccess
├── appsettings.json
└── appsettings.Development.json
```

**Project references:** `RadanProject`, `RadanInterface2`, `VaultAccess`

---

## Configuration (appsettings.json)

```json
{
  "Bridge": {
    "StationId": 1,
    "NestingSoftware": "Radan",
    "HorstMfgUrl": "https://horstmfg.local",
    "ApiKey": "...",
    "RadanProjectsRootPath": "C:\\Radan Projects",
    "SymNetworkSharePath": "\\\\server\\sym-files"
  },
  "Vault": {
    "Server": "vault-server",
    "Username": "svc-radan",
    "Password": "..."
  }
}
```

`NestingSoftware` controls which `INestingProjectService` implementation is registered at
startup — switching nesting software in the future means a new implementation class and
one config change, nothing else.

---

## SignalR connection lifecycle

- On startup: connect to `{HorstMfgUrl}/hubs/bridge`, authenticate with `ApiKey` header,
  call `Register(stationId)` to identify this bridge
- HorstMFG records connection and marks station online
- On disconnect: automatic reconnect with exponential backoff (built into SignalR client)
- On reconnect: re-send `Register(stationId)` so HorstMFG reassociates the connection
- HorstMFG marks station offline when connection drops (no explicit heartbeat needed)

---

## File watcher (auto-sync)

- `FileSystemWatcher` monitors the active project `.rpd` file for changes
- Radan triggers multiple change events per save — debounce with 2-second quiet period
  before acting
- Retry logic handles brief file locks during Radan's save operation
- On stable change detected: run full Sync and push result to HorstMFG via SignalR
- Watcher path is updated when Finalize creates a new project
- Makes automatic Sync the primary mechanism; manual Sync button becomes a fallback

---

## HorstMFG additions

### SignalR hub — `BridgeHub.cs`

**Bridge → HorstMFG (hub methods HorstMFG exposes):**

- `Register(stationId)` — bridge calls on connect
- `Progress(stationId, commandId, message, pctComplete)` — incremental progress
- `CommandResult(stationId, commandId, success, payload)` — final result
- `AutoSync(stationId, payload)` — bridge pushes file-watcher-triggered sync

**HorstMFG → bridge (hub methods bridge exposes):**

- `ExecuteCommand(commandId, commandType, payload)`

### New API key middleware

Validates `ApiKey` header on hub connection.

### NestingStation fields updated by bridge

- `BridgeLastSeen`, `BridgeVersion` — on Register
- `ProjectName`, `ProjectPath` — on Finalize result

---

## Commands

### `SendToNesting`

**Payload:** `[{ ItemId, ItemType (Order/Batch), FileName, QtyRequired, Material,
Thickness, OrderNumber }]`

- Acts on user selection only — never all parts
- Before queuing: HorstMFG checks which selected parts are missing a `.sym` file on the
  share and shows a warning: _"X part(s) have no symbol file and will need Vault retrieval
  after sending. Continue?"_
- If confirmed: all selected parts are sent regardless
- Bridge flow per part:
  1. Copy `.sym` from share → `{ProjectFolder}\Symbols\{OrderNumber}\{ProjectName}\{FileName}\{FileName}.sym`
     (skipped if file missing — part still added to RPD, flagged for retrieval)
  2. Read RPD `<NextID>`, assign as `RadanIdNumber`, add `<Part>` entry, increment `<NextID>`
  3. Save RPD
- Returns `[{ ItemId, RadanIdNumber, MissingSymFile }]`
- HorstMFG sets `IsInRadanProject = true`, stores `RadanIdNumber`; flags missing-sym parts visually

### `RetrieveFromVault`

**Payload:** `[{ ItemId, FileName }]`

1. For each part: download `.ipt` from Vault via `IVaultService`
2. Unfold via nesting software interface, export `.sym` to `{SymNetworkSharePath}`
3. Report progress per part
4. Returns `[{ ItemId, Success }]`
5. HorstMFG clears the missing-sym flag; user can then re-run SendToNesting for those parts

### `RetrieveFromNesting`

**Payload:** `[{ ItemId, RadanIdNumber }]` — user selection

1. Run Sync first to capture latest nest data
2. Remove matching `<Part>` entries from RPD, save RPD
3. Returns sync payload + `[ItemId]` cleared list
4. HorstMFG applies sync, then sets `IsInRadanProject = false`, `RadanIdNumber = null`,
   removes associated `NestedParts`

### `Sync`

**No payload** — also triggered automatically by file watcher

1. Read all `<Part>` entries: `{ ID, Made }`
2. Read `<UsedInNests>` per part: `{ NestId, Made }`
3. Read nest `.drg` file paths from `NestFolder`
4. Returns:
   
   ```json
   {
   "Parts": [{ "RadanIdNumber": 72861, "QtyNested": 5 }],
   "Nests": [{
    "NestName": "13",
    "NestPath": "C:\\...\\nests\\13.drg",
    "Parts": [{ "RadanIdNumber": 72861, "Qty": 5 }]
   }]
   }
   ```
5. HorstMFG updates `QtyNested` per item, upserts `Nest` + `NestedPart` records

### `UpdateThumbnail`

**Payload:** `[{ PartId, FileName }]` — user-triggered, selection or individual part

1. For each part: locate `{FileName}.sym` on the network share
2. Extract thumbnail from `.sym` via `INestingProjectService.ExtractThumbnail()`
3. Report progress per part
4. Returns `[{ PartId, ThumbnailBytes }]`
5. HorstMFG updates `Part.Thumbnail`

**Future auto-population opportunities (deferred):**

- After `RetrieveFromVault` completes — `.sym` was just created, thumbnail can be extracted immediately
- When a batch or schedule is released to production — bridge could be asked to extract
  thumbnails for all newly released parts that have a `.sym` on the share

### `Finalize`

**No payload**

1. Full Sync
2. Identify parts with `Made = 0` → cleared list
3. Create new project:
   - New folder: `{RadanProjectsRoot}\{today:yyyy-MM-dd}` (append `(1)`, `(2)` if exists)
   - Copy RPD into new folder, rename to `{date}.rpd`
   - Clear `<Parts>` list, reset `<NextID>` to 1
   - Update `NestFolder` and `RemnantSaveFolder` paths in RPD
   - Create empty `nests\` and `Symbols\` subfolders
   - Save RPD
4. Update file watcher to monitor new RPD path
5. Returns sync payload + cleared list + `{ NewProjectName, NewProjectPath }`
6. HorstMFG applies sync, clears un-nested items, updates `NestingStation.ProjectName/Path`

---

## INestingProjectService interface

```csharp
public interface INestingProjectService
{
    NestingProjectData LoadProject(string path);
    void SaveProject(NestingProjectData project, string path);
    long GetNextId(NestingProjectData project);
    void AddPart(NestingProjectData project, string symPath, long nestingId,
                 int qty, string material, decimal thickness);
    void RemoveParts(NestingProjectData project, long[] nestingIds);
    SyncPayload ReadSyncData(NestingProjectData project);
    string CreateNewProject(string currentPath, DateTime date);
    byte[]? ExtractThumbnail(string symPath);
}
```

`NestingProjectData` and `SyncPayload` are generic DTOs — no Radan types leak out of
the `Radan/` subfolder.

---

## IVaultService interface

```csharp
public interface IVaultService
{
    Task<string> DownloadPartAsync(string fileName, string targetFolder);
}
```

---

## RpdService responsibilities (internal to Radan implementation)

- `LoadProject(path)` → deserialize RPD via `RadanProjectExtension.LoadData()`
- `SaveProject(project, path)` → serialize via `RadanProjectExtension.SaveData()`
- `GetNextId(project)` → read `Parts.NextID`
- `AddPart(...)` → build `RadanPart`, call `RadanProjectExtension.AddPart()`
- `RemoveParts(project, ids[])` → remove matching `<Part>` entries
- `ReadSyncData(project)` → map `Made` + `UsedInNests` to `SyncPayload`
- `CreateNewProject(currentPath, date)` → copy folder, clear parts/nests/symbols, update paths
- `ExtractThumbnail(symPath)` → open `.sym` via `RadanInterface2`, capture rendered image as `byte[]`

---

## HorstMFG Production page UI (future sprint)

- Per-station status indicator (green/grey dot based on SignalR connection state)
- Active project name display
- Station selector (if plant has multiple stations)
- **Send to Nesting** — selection only; warns on missing .sym files before proceeding
- **Retrieve from Vault** — for parts flagged as missing .sym
- **Retrieve from Nesting** — selection; removes parts from active project
- **Sync** — manual fallback (auto-sync via file watcher is primary)
- **Finalize** — confirmation prompt; UI disabled until bridge responds
- **Update Thumbnail** — per-part or batch, user-triggered
- Progress panel showing live command status

## To Generate API keys

* ` -join ((1..32) | ForEach-Object { '{0:X2}' -f (Get-Random -Max 256) })`


