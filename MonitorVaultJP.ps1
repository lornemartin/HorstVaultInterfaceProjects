# Path to Job Processor executable
$jobProcessorPath = "C:\Program Files\Autodesk\Vault Client 2025\Explorer\JobProcessor.exe"

# Path to JobProcessor.log
$logFilePath = "C:\Users\lorne\AppData\Roaming\Autodesk\Autodesk Vault Job Processor\JobProcessor.log"

# Function to restart Job Processor
function Restart-JobProcessor {
    Write-Host "$(Get-Date -Format u): Restarting Job Processor..."

    # Kill any existing JobProcessor processes
    Get-Process JobProcessor -ErrorAction SilentlyContinue | Stop-Process -Force

    # Start Job Processor
    Start-Process -FilePath $jobProcessorPath

    Write-Host "$(Get-Date -Format u): Job Processor restarted."
}

# Monitor log file for new lines
Write-Host "Monitoring JobProcessor.log for failures..."
Get-Content -Path $logFilePath -Tail 0 -Wait | ForEach-Object {
    $line = $_
    if ($line -match "Failure") {
        Write-Host "$(Get-Date -Format u): Failure detected in log: $line"
        Restart-JobProcessor
    }
}
