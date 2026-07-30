# HorstMFG Server Deployment Guide

## Software Prerequisites

- **.NET 8 ASP.NET Core Hosting Bundle** — installs the runtime and IIS ASP.NET Core Module v2
- **PostgreSQL** — install on the server or point to an existing instance
- **IIS** with the WebSockets feature enabled (required for Blazor Server SignalR)
- **Git** — required on the server to pull updates from the repository and run the deploy script (see Deployment Workflow)

---

## IIS Configuration

- App pool .NET CLR Version: **No Managed Code** (ASP.NET Core runs out-of-process)
- App pool identity: **domain service account** with access to network shares (see Permissions below)
- Enable **WebSockets** in IIS features — without this, Blazor Server falls back to long-polling

---

## Expected Users & Clients

| Location | Role | Count | Client type |
| -------- | ---- | ----- | ----------- |
| Plant 1 | Nesting station | 1 | Bridge app + browser |
| Plant 2 | Nesting station | 1 | Bridge app + browser |
| Plant 1 | Data entry | 1 | Bridge app + browser |
| Plant 2 | Data entry | 1 | Bridge app + browser |
| Plant 1, 2, 3 | Read-only / reports | Several | Browser |

**Nesting station and data entry PCs** run two things: the HorstMFG Bridge client app and a browser for the UI. The Bridge app is a background service that acts as a local agent on the client PC — it exposes the local file system (nesting project files, export files) to the web app, and relays commands and data between the web app's database and the local environment. It maintains a persistent connection to `/bridgehub` on the server. These are the only machines that need the bridge client installed — four total (one of each role per plant).

**Read-only users** access the app through a browser and are limited to generating and printing reports from the database. No write access is needed for these accounts.

This is a small concurrent user count. The server does not need to be sized for high traffic — the main resource consumers are PDF report generation (CPU + disk I/O) and EF Core queries (DB connections). A modest Windows Server VM or physical machine with an SSD is sufficient.

---

## SignalR & Client Connectivity

The app uses SignalR in two separate roles:

**1. Blazor Server UI circuit (`/_blazor`)**
Every browser tab running the app holds a persistent WebSocket connection to the server. This is how Blazor Server pushes UI updates to the browser in real time. If WebSockets are unavailable, SignalR falls back to long-polling — functional but significantly more chatty.

**2. Bridge hub (`/bridgehub`)**
Each nesting station (Plant1 and Plant 2) runs a small bridge client app that connects to this endpoint and holds a persistent WebSocket connection. The bridge authenticates via an API key (the `Bridge:ApiKey` in `appsettings.Production.json`) and uses this connection to receive nesting commands and report results back to the server. Unlike browser sessions, bridge connections are machine-to-machine and are intended to stay connected indefinitely.

### Network Requirements for Bridge Clients

- Nesting station PCs must be able to reach the server on **port 443** (outbound TCP). If a firewall exists between the shop floor network segment and the server, that traffic must be explicitly allowed.
- Any network device that aggressively terminates idle TCP connections (some firewalls and managed switches do this after 5–30 minutes of low traffic) will silently drop bridge connections. SignalR sends keepalive pings, but if the device kills the socket before the ping fires, the bridge will reconnect automatically — you may see log entries like "Bridge station X disconnected / reconnected" more often than expected. If this is a problem, increase the idle timeout on the relevant network device or adjust the SignalR server timeout settings.
- WebSockets must not be terminated by an intermediate proxy. Standard HTTPS pass-through is fine; a proxy that terminates and re-encrypts TLS needs to be configured to allow WebSocket upgrades (`Upgrade: websocket` header must pass through).

---

## Folder & Permissions

The app pool identity (domain service account) needs:

| Path                                          | Access     | Why                                         |
| --------------------------------------------- | ---------- | ------------------------------------------- |
| `C:\HorstMFG\PDFs\`                           | Read/Write | Local PDF copy on import                    |
| `\\hwvsse01\Manufacturing\PDF Drawing Files\` | Read       | PDF share for inline viewing and BOM import |
| App folder (e.g. `C:\inetpub\horstmfg\`)      | Read       | Run the app                                 |
| `logs\` subfolder of app folder               | Read/Write | Serilog file sink                           |

> **Note:** IIS `ApplicationPoolIdentity` (the default) cannot authenticate to network shares.
> You must use a domain service account as the app pool identity.

---

## Domain Service Account — Detailed Requirements

### Why a Domain Service Account Is Needed

IIS app pools run under one of several built-in identities by default. The standard default,
`ApplicationPoolIdentity`, is a local virtual account scoped to the server. It has no domain
credentials, so when the app tries to open a UNC path like `\\hwvsse01\...`, Windows has no
credentials to present to the remote server and access is denied.

A **domain service account** is a regular Active Directory user account used by a service rather
than a person. Because it has domain credentials, it can authenticate to other servers on the
domain (hwvsse01, HWVMWK02) using Kerberos or NTLM, the same way a logged-in user would.

### Creating the Account (AD task — done by sysadmin)

1. In **Active Directory Users and Computers**, create a new user in an appropriate OU (e.g. `Service Accounts`):
   
   - Username: something like `svc-horstmfg` or `svc_horstmfg`
   - Set a strong password
   - Check **"Password never expires"** — service accounts should not have expiring passwords, as expiry causes silent failures at 3am
   - Uncheck **"User must change password at next logon"**
   - The account does **not** need to be a member of any privileged groups (Domain Admins, etc.) — it should be a plain domain user with only the specific permissions granted below

2. Grant the account the **"Log on as a service"** right on the web server:
   
   - On the web server, open **Local Security Policy** → Security Settings → Local Policies → User Rights Assignment
   - Add `svc-horstmfg` to **"Log on as a service"**
   - Alternatively this is granted automatically when you set the app pool identity in IIS

### Granting Share Permissions

Network share permissions have two independent layers that both must allow access:

**Share-level permissions** (set on the sharing server — hwvsse01):

- Right-click the shared folder → Properties → Sharing → Advanced Sharing → Permissions
- Add `DOMAIN\svc-horstmfg`
- For `PDF Drawing Files`: grant **Read**

**NTFS permissions** (also set on the sharing server — hwvsse01):

- Right-click the folder → Properties → Security → Edit
- Add `DOMAIN\svc-horstmfg`
- For `PDF Drawing Files`: grant **Read & Execute**, **List folder contents**, **Read**

Both layers must allow access — share permissions and NTFS permissions are ANDed together.
If either denies, access fails. A common mistake is setting share permissions correctly but
leaving NTFS permissions at default (which may not include the service account).

### Configuring IIS to Use the Account

1. Open **IIS Manager** → Application Pools → select the HorstMFG app pool
2. Click **Advanced Settings** → **Identity** → click the `...` button
3. Select **Custom account** → click **Set**
4. Enter `DOMAIN\svc-horstmfg` and the password
5. Click OK — IIS will validate the credentials immediately

### Local Permissions on the Web Server

The service account also needs permissions on the web server itself:

- **App folder** (e.g. `C:\inetpub\horstmfg\`): Read & Execute — grant via NTFS security
- **`C:\HorstMFG\PDFs\`**: Read, Write, Modify — grant via NTFS security. If this folder does
  not exist yet, create it first, then set permissions
- **`logs\` folder** inside the app folder: Write, Modify — the app creates this automatically
  but the service account must be able to write to it

A convenient way to grant local folder permissions from PowerShell (run as admin on the server):

```powershell
$account = "DOMAIN\svc-horstmfg"
icacls "C:\inetpub\horstmfg"   /grant "${account}:(OI)(CI)RX" /T
icacls "C:\HorstMFG\PDFs"      /grant "${account}:(OI)(CI)M"  /T
icacls "C:\inetpub\horstmfg\logs" /grant "${account}:(OI)(CI)M" /T
```

### Verifying Access Before Go-Live

The simplest way to verify the service account can reach the shares is to use `runas` to
open a command prompt as the service account and test the paths:

```cmd
runas /user:DOMAIN\svc-horstmfg cmd.exe
```

Then in the new window:

```cmd
dir "\\hwvsse01\Manufacturing\PDF Drawing Files\"
```

If the directory lists successfully, the account has the access it needs. If you get
"Access is denied", check both the share-level and NTFS permissions on hwvsse01.

---

## Environment Variable

Set in IIS (site → Configuration Editor, or app pool environment variables):

```
ASPNETCORE_ENVIRONMENT = Production
```

This causes ASP.NET Core to merge `appsettings.Production.json` over `appsettings.json` at startup.

---

## Production Configuration

Create `appsettings.Production.json` **on the server** in the app root folder.
This file is not deployed from the developer's PC — it stays on the server only.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=horstmfg;Username=horstmfg_user;Password=<prod-password>"
  },
  "Bridge": {
    "ApiKey": "<prod-api-key>"
  },
  "FileSystemPaths": {
    "PdfSharePath": "\\\\hwvsse01\\Manufacturing\\PDF Drawing Files\\",
    "LocalPdfPath": "C:\\HorstMFG\\PDFs\\"
  }
}
```

> The Syncfusion license key in `appsettings.json` can remain as-is (not a security credential).
> Do not commit the production DB password to git.

---

## PostgreSQL Setup

- Create database `horstmfg` and a user with a strong password
- Grant the user DDL rights (CREATE TABLE etc.) — the app runs EF Core migrations automatically on first startup via `DbInitializer.InitializeAsync()`
- Allow connections from localhost (or wherever the app server runs)

---

## SSL Certificate

The app has `app.UseHttpsRedirection()` and `app.UseHsts()` in `Program.cs`, so HTTPS is
expected in production. HTTP requests on port 80 will automatically redirect to HTTPS.

### Choosing a Certificate

You have three options, in order of preference for an internal shop floor app:

**Option 1: Certificate from your domain's Active Directory Certificate Services (ADCS)**
This is the best option if your domain already has an internal CA (many corporate domains do).

- The cert is trusted automatically by all domain-joined machines — no manual installation needed on shop floor PCs
- Ask your sysadmin whether an ADCS root CA exists in the domain
- If yes, request a certificate for the server's hostname (e.g. `horstmfg.horst.local` or whatever DNS name you'll use)
- The sysadmin can issue this from the CA server or via the Certificates MMC snap-in

**Option 2: Self-signed certificate**
Straightforward to create but requires manual trust installation on every client machine.

- Create via IIS Manager → Server Certificates → Create Self-Signed Certificate, or via PowerShell:
  
  ```powershell
  New-SelfSignedCertificate -DnsName "horstmfg.horst.local" -CertStoreLocation "cert:\LocalMachine\My"
  ```

- Export the certificate's public key (no private key) as a `.cer` file

- Deploy it to the **Trusted Root Certification Authorities** store on each client machine,
  either manually or via Group Policy (Computer Configuration → Windows Settings → Security Settings → Public Key Policies → Trusted Root Certification Authorities)

- Without this step, browsers will show a security warning on every machine

**Option 3: Public CA certificate (e.g. Let's Encrypt)**
Only applicable if the server is accessible from the public internet and has a public DNS name.
Not recommended for an internal manufacturing app.

### Binding the Certificate in IIS

1. Open **IIS Manager** → select the server node → **Server Certificates**
2. Import or create the certificate (if not already present)
3. Select the HorstMFG site → **Bindings** → **Add**
4. Type: **https**, Port: **443**, Hostname: the DNS name the app will be accessed by
5. Select the certificate from the dropdown → OK

Also ensure an **http** binding exists on port 80 (for the redirect to work):

- Type: **http**, Port: **80**, leave hostname blank or match the https hostname

### DNS

Shop floor machines need to reach the server by name, not just IP. Either:

- Add an **A record** in your internal DNS server pointing the hostname to the server's IP, or
- Add entries to `hosts` files on each client machine (not recommended — hard to maintain)

The hostname in DNS must match the hostname on the certificate, or browsers will show a
certificate mismatch warning even if the cert is trusted.

### HSTS Caveat

`app.UseHsts()` sends an `HSTS` header telling browsers to always use HTTPS for this hostname
for 30 days (the default). This is fine once everything is working, but be aware: if you later
need to access the site over HTTP for troubleshooting, browsers that have seen the HSTS header
will refuse the HTTP connection. Clear the HSTS cache in the browser if needed
(Chrome: `chrome://net-internals/#hsts`).

---

## Local PDF Storage Requirements

### Purpose

`C:\HorstMFG\PDFs\` is the server's local copy of PDF drawings, organized by import:

```
C:\HorstMFG\PDFs\
    Batches\
        {BatchName}\
            {PartNumber}.pdf ...
    Schedules\
        {ScheduleName}\
            {PartNumber}.pdf ...
```

PDFs are copied from the network share (`\\hwvsse01\Manufacturing\PDF Drawing Files\`) at import
time. The local copy is what the app serves to users for inline viewing and report generation.
The network share itself is not modified by the app.

This folder **replaces** the following folders currently on the Manufacturing (S:) drive:

- `S:\Shop Schedule Drawings\`
- `S:\Plant 2 Shop Schedule Drawings\`

Those folders will no longer need to be maintained once the app is in production.

### Measured Baseline (1 Year Simulated Data)

| Subfolder    | Files       | Size       |
| ------------ | ----------- | ---------- |
| `Batches\`   | 7,767       | ~1.7 GB    |
| `Schedules\` | 229,084     | ~51.8 GB   |
| **Total**    | **236,851** | **~52 GB** |

### Capacity Planning

| Window            | Estimated Size |
| ----------------- | -------------- |
| 1 year (measured) | ~52 GB         |
| 2 years           | ~104 GB        |
| 3 years (target)  | ~156 GB        |

**Recommended allocation: 250 GB minimum** — this covers the 3-year target with ~60% headroom
for growth above the simulated baseline.

If you expect import volume to increase significantly (more plants, higher order frequency),
size up accordingly. The app does not currently purge old data automatically, so the folder
will grow continuously until files are manually archived or removed.

### Disk Type

The PDFs folder is read heavily — every report generation and every inline PDF view reads
one or more files from it. An **SSD or SSD-backed storage volume** is recommended. A spinning
disk will work but report generation (which reads and merges multiple PDFs in sequence) will
be noticeably slower under load.

---

## Firewall

Open inbound on the server:

- Port 80 (HTTP — redirects to HTTPS)
- Port 443 (HTTPS)

---

## Database Migration

The app runs pending EF Core migrations automatically on startup. No manual SQL required.
On first deploy, ensure the DB user has DDL rights. After initial schema creation these can be reduced if desired.

---

## Deployment Workflow (Developer PC → Server)

1. Clone the repo on the server

2. Create a deploy script on the server:
   
   ```powershell
   git pull
   dotnet publish src/HorstMFG.Web/HorstMFG.Web.csproj -c Release -o C:\inetpub\horstmfg
   Restart-WebAppPool -Name "HorstMFG"
   ```

3. Push changes from your dev PC, then run the script on the server (via RDP or remote PowerShell)

---

## VaultGateway Service (Scheduled Task, not a Windows Service)

`HorstMFG.VaultGateway` (deployed to `C:\Services\VaultGateway` on `hwvsweb01`) is the local
process that calls the Autodesk Vault SDK on `HorstMFG.Web`'s behalf — it listens on
`http://127.0.0.1:5050` for BOM import jobs and logs into Vault via `VaultAccess.LoginHeadlessForItems`.

**It must be registered as a Scheduled Task, not a native Windows Service.** A native Windows
Service runs in Session 0 (no window station), and the Vault Connectivity SDK's login call hangs
indefinitely in that context even though it works instantly run interactively — the same reason
`HorstMFG.Bridge` runs as a logon scheduled task rather than a service (see
`HorstMFG.Bridge/Installer/installer.nsi`). A scheduled task with a password-based logon gives
the process a real interactive-equivalent token without requiring anyone to actually be logged in.

```powershell
$action   = New-ScheduledTaskAction -Execute "C:\Services\VaultGateway\HorstMFG.VaultGateway.exe" -WorkingDirectory "C:\Services\VaultGateway"
$trigger  = New-ScheduledTaskTrigger -AtStartup
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit 0 -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName "HorstMFGVaultGateway" -Action $action -Trigger $trigger -Settings $settings -User "HW.HORST.COM\svc-horstmfg" -Password '<svc-horstmfg password>' -RunLevel Highest

Start-ScheduledTask -TaskName "HorstMFGVaultGateway"
```

> **Use single quotes around `-Password`.** Double-quoted strings in PowerShell interpolate `$` as
> variable expansion — a password containing `$SomeText` will silently have that portion stripped
> in a double-quoted string, producing a corrupted password and a misleading "user name or
> password is incorrect" error even when the password is actually correct. Single quotes are
> fully literal. Do not commit the actual password to git — this file should only ever show a placeholder.

To redeploy after a code change: stop the task, copy the new build output over (but do **not**
overwrite `appsettings.json`, which holds the server's real `ListenUrl`/`CallbackApiKey`/`Vault`
values), then restart the task:

```powershell
Stop-ScheduledTask -TaskName "HorstMFGVaultGateway"
# copy new build output here, excluding appsettings.json
Start-ScheduledTask -TaskName "HorstMFGVaultGateway"
```

Check logs at `C:\Services\VaultGateway\logs\gateway-<yyyyMMdd>.log`. If the task starts but the
log stays empty and the Gateway never responds on port 5050, it's almost always stuck inside the
Vault login call — confirm with `curl.exe -v http://127.0.0.1:5050/api/jobs/x` (connection refused
means it's still stuck before the HTTP listener starts).

---

## Sysadmin Checklist

```
[ ] Install .NET 8 ASP.NET Core Hosting Bundle
[ ] Install/configure PostgreSQL, create horstmfg database and user
[ ] Install IIS with WebSockets feature enabled
[ ] Create app pool (No Managed Code, domain service account identity)
[ ] Create IIS site pointing to app folder
[ ] Set ASPNETCORE_ENVIRONMENT=Production in IIS environment variables
[ ] Create appsettings.Production.json with production DB password and Bridge API key
[ ] Create C:\HorstMFG\PDFs\ with read/write permissions for service account
[ ] Verify service account can read \\hwvsse01\Manufacturing\PDF Drawing Files\
[ ] Open ports 80 and 443 in Windows Firewall
[ ] Bind SSL certificate to IIS site
[ ] First deploy: app will auto-run DB migrations on startup
[ ] Deploy HorstMFG.VaultGateway to C:\Services\VaultGateway and register it as a
    Scheduled Task (AtStartup, password logon) — see "VaultGateway Service" above,
    NOT as a Windows Service
[ ] Ensure hwvsweb01 can resolve/reach the Vault server and PDF share over IPv4 —
    check for stray link-local AAAA DNS records if HTTPS calls between internal
    servers fail with TLS handshake resets
```
