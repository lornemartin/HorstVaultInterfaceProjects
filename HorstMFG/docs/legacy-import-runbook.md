# Legacy RadanMaster Import Runbook

Backs up a HorstMFG environment, clears it, and imports the production history of the legacy
RadanMaster SQL Server databases (one per plant) with `HorstMFG.DataTransfer`.

Written for the test run on the dev workstation (both RadanMaster databases restored into the
LocalDB instance `(localdb)\RadanLegacy`, PostgreSQL on `localhost`). See
[Go-live: what changes](#go-live-what-changes) for running it against the server.

## Before you start

- **`clear` empties everything except users, plants, stations and configuration.** That includes
  Vault BOM imports (batches, schedules, part line items) and their PDFs, not just nesting data.
  The backup in step 2 is the only way back.
- Each plant's stations should have a Radan projects folder set in the environment. A station
  without one makes the transfer print a warning (not an error): its bridge would fall back to its
  own setting, which is probably the real projects folder. Set it with
  `EnvTool set-radan-root <env> <station id|name> "<folder>"`.
- Legacy RadanMaster databases are SQL Server 2022 backups; restore them into a SQL Server 2025
  instance (LocalDB `RadanLegacy`). Use SSMS 21+ to browse them (`(localdb)\RadanLegacy`, Windows
  Authentication, Trust server certificate).

Run everything in an **elevated PowerShell**.

## 0. Settings (edit these each time)

```powershell
$Repo   = 'C:\Users\lorne\source\repos\HorstVaultInterfaceProjects\HorstMFG'
$Env    = 'horstmfg'
$Plant1 = 'Server=(localdb)\RadanLegacy;Database=RadanMaster_Plant1_20261116;Trusted_Connection=true;TrustServerCertificate=true'
$Plant2 = 'Server=(localdb)\RadanLegacy;Database=RadanMaster_Plant2_20261105;Trusted_Connection=true;TrustServerCertificate=true'
sqllocaldb start RadanLegacy
```

## 1. Stop the web app

Stop it in Visual Studio (on the server: `Stop-WebAppPool -Name HorstMFG`). Make sure no bridges
are mid-job.

## 2. Back up the environment (database and PDFs)

```powershell
Set-Location "$Repo\tools\HorstMFG.EnvTool"
dotnet run -- list
dotnet run -- backup $Env
```

**Write down the folder it prints** (`Backup written to C:\HorstMFG\Backups\horstmfg_...`). You
need it to undo.

## 3. Clear the environment

```powershell
dotnet run -- clear $Env --no-backup --confirm $Env
```

`--no-backup` because step 2 already took the backup.

## 4. Preview both plants (writes nothing)

```powershell
Set-Location "$Repo\tools\HorstMFG.DataTransfer"
dotnet run -- preview --plant "Plant 1" --database $Env --sql-conn $Plant1
dotnet run -- preview --plant "Plant 2" --database $Env --sql-conn $Plant2
```

Check the counts, and that nothing shows as `Error:`. Plant names must match HorstMFG exactly.

## 5. Import Plant 1, then Plant 2

```powershell
dotnet run -- run --plant "Plant 1" --database $Env --sql-conn $Plant1 --confirm $Env
dotnet run -- run --plant "Plant 2" --database $Env --sql-conn $Plant2 --confirm $Env
```

Each run is a single transaction, so a failure leaves nothing half-written. Parts are shared by all
plants (matched by file name), so Plant 2 reuses the parts Plant 1 already brought in.

## 6. Check the result

```powershell
dotnet run -- status --database $Env --sql-conn $Plant2
```

The destination totals should match the two previews. (The "Source" block shows only Plant 2's
numbers, which is expected.)

## 7. Start the web app and look around

Check Nesting for both plants, legacy projects (status *Legacy*), and nests and parts.

## Undo (put the pre-import data back)

```powershell
Set-Location "$Repo\tools\HorstMFG.EnvTool"
dotnet run -- restore "C:\HorstMFG\Backups\<folder from step 2>" horstmfg_restored
dotnet run -- swap horstmfg_restored --disconnect --confirm horstmfg_restored
dotnet run -- list
dotnet run -- drop horstmfg_prev_<timestamp> --no-backup --confirm horstmfg_prev_<timestamp>
```

The restored copy keeps its PDFs in `C:\HorstMFG\PDFs\horstmfg_restored`. It records that path
itself, so everything works even though the folder name doesn't match. Get the exact
`horstmfg_prev_...` name for the drop from the `list` output.

## Go-live: what changes

1. **Get fresh backups** from both plants. Restore them into `RadanLegacy` as new databases (e.g.
   `RadanMaster_Plant1_<date>`), and update `$Plant1` and `$Plant2` in step 0.
2. **Steps 1–3 run on the server.** Set `"AppPool": "HorstMFG"` in the server's EnvTool
   `appsettings.json` so it stops and starts the app pool itself.
3. **Steps 4–6 have to reach both databases at once.** LocalDB only accepts connections from its
   owner's login on its own machine, so pick one of these:
   - **Do everything on the server (preferred).** Install SQL Server 2025 Express LocalDB there,
     restore the two `.bak` files into it, and run all the steps on the server. No network setup.
   - **Run the transfer on the workstation and point it at the server's PostgreSQL.** Add a
     `DefaultConnection` under `ConnectionStrings` in `tools\HorstMFG.DataTransfer\appsettings.json`
     with the server's host and password. It overrides the web project's `localhost` one. The
     server's PostgreSQL must accept remote connections (`pg_hba.conf` and the firewall on port
     5432). Don't commit that password.
4. **Set every station's Radan folder** before importing if its bridge will be used:
   `dotnet run -- set-radan-root horstmfg <station id|name> "<folder>"` (from the EnvTool folder).
5. **Use `--since yyyy-MM-dd`** in steps 4–5 if you want only recent history. Open orders always
   come across, whatever their date.
