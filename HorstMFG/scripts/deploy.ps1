Import-Module WebAdministration

$scriptPath  = "C:\Users\Lorne\Scripts\"
$repoRoot    = "C:\Users\lorne\source\repos\HorstVaultInterfaceProjects"
$projectPath = "$repoRoot\HorstMFG\src\HorstMFG.Web\HorstMFG.Web.csproj"
$publishPath = "C:\inetpub\horstmfg"
$appPool     = "HorstMFG"

$prodSettings = "$publishPath\appsettings.Production.json"
$backupRoot  = "$publishPath-config-backups"

# Production settings must live in a server-only file. Publish overwrites appsettings.json
# whenever the repo copy changes, which would silently point the app at the wrong database.
if (-not (Test-Path $prodSettings)) {
    throw "Missing $prodSettings. Move the server's production settings (connection string, API keys, paths) into it before deploying."
}

# Back up the server's config files so they can be restored if publish clobbers anything.
$backupPath = Join-Path $backupRoot (Get-Date -Format "yyyyMMdd-HHmmss")
New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
Copy-Item "$publishPath\appsettings*.json", "$publishPath\web.config" -Destination $backupPath -ErrorAction Stop
Write-Host "Backed up config files to $backupPath"

Set-Location $repoRoot
git pull

Write-Host "Stopping app pool '$appPool'..."
Stop-WebAppPool -Name $appPool

# Wait for the pool to fully stop so w3wp.exe releases file locks on the DLLs.
$timeout = (Get-Date).AddSeconds(30)
while ((Get-WebAppPoolState -Name $appPool).Value -ne "Stopped") {
	if ((Get-Date) -gt $timeout) {
        throw "Timed out waiting for app pool '$appPool' to stop."
    }
    Start-Sleep -Milliseconds 500
}

try {
    dotnet publish $projectPath -c Release -o $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
finally {
    Write-Host "Starting app pool '$appPool'..."
    Start-WebAppPool -Name $appPool
}

Set-Location $scriptPath
Write-Host "Deploy complete."