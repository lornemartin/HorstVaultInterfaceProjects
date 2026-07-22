# run this on the server
Import-Module WebAdministration

$repoRoot    = "C:\Users\lorne\source\repos\HorstVaultInterfaceProjects"
$projectPath = "$repoRoot\HorstMFG\src\HorstMFG.Web\HorstMFG.Web.csproj"
$publishPath = "C:\inetpub\horstmfg"
$appPool     = "HorstMFG"

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

Write-Host "Deploy complete."