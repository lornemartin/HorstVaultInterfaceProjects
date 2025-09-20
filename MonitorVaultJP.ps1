# Path to Job Processor executable
$jobProcessorPath = "C:\Program Files\Autodesk\Vault Client 2025\Explorer\JobProcessor.exe"

# Path to JobProcessor.log
$logFilePath = "C:\Users\lorne\AppData\Roaming\Autodesk\Autodesk Vault Job Processor\JobProcessor.log"

# Watchdog log path in the same folder as the script
$watchdogLogPath = Join-Path -Path $PSScriptRoot -ChildPath "JobProcessorWatchdog.log"

# Logging helper function
function Write-Log {
    param (
        [string]$Message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "$timestamp - $Message"

    # Write to console
    Write-Host $line

    # Append to log file
    Add-Content -Path $watchdogLogPath -Value $line
}

# Function to restart Job Processor
function Restart-JobProcessor {
    Write-Log "$(Get-Date -Format u): Restarting Job Processor..."

    # Kill any existing JobProcessor processes
    Get-Process JobProcessor -ErrorAction SilentlyContinue | Stop-Process -Force

    # Start Job Processor
    Start-Process -FilePath $jobProcessorPath

    Write-Log "$(Get-Date -Format u): Job Processor restarted."
}

# Monitor log file for new lines
Write-Log "Monitoring JobProcessor.log for failures..."
Get-Content -Path $logFilePath -Tail 0 -Wait | ForEach-Object {
    $line = $_
    if ($line -match "Failure") {
        Write-Log "$(Get-Date -Format u): Failure detected in log: $line"
        Restart-JobProcessor
    }
}
