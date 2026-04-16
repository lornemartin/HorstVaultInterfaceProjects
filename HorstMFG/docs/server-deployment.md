# HorstMFG Server Deployment Guide

## Software Prerequisites

- **.NET 8 ASP.NET Core Hosting Bundle** — installs the runtime and IIS ASP.NET Core Module v2
- **PostgreSQL** — install on the server or point to an existing instance
- **IIS** with the WebSockets feature enabled (required for Blazor Server SignalR)

---

## IIS Configuration

- App pool .NET CLR Version: **No Managed Code** (ASP.NET Core runs out-of-process)
- App pool identity: **domain service account** with access to network shares (see Permissions below)
- Enable **WebSockets** in IIS features — without this, Blazor Server falls back to long-polling

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

**Share-level permissions** (set on the sharing server — hwvsse01 / HWVMWK02):

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
```
