# Path to Job Processor executable
$jobProcessorPath = "C:\Program Files\Autodesk\Vault Client 2025\Explorer\JobProcessor.exe"

# Path to JobProcessor.log
$logFilePath = "C:\Users\lorne\AppData\Roaming\Autodesk\Autodesk Vault Job Processor\JobProcessor.log"

# Watchdog log path in the same folder as the script
$watchdogLogPath = Join-Path -Path $PSScriptRoot -ChildPath "JobProcessorWatchdog.log"

# Load UI Automation assembly
Add-Type -AssemblyName UIAutomationClient

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

# Function to gracefully stop Job Processor using UI Automation
function Stop-JobProcessorGracefully {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        "Job Processor"
    )
    $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)

    if ($window) {
        # Find File menu
        $fileCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            "File"
        )
        $fileMenu = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $fileCondition)
        if ($fileMenu) {
            $invokePattern = $fileMenu.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $invokePattern.Invoke()
            Start-Sleep -Milliseconds 500

            # Find Exit menu item
            $exitCondition = [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::NameProperty,
                "Exit"
            )
            $exitMenu = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $exitCondition)
            if ($exitMenu) {
                $exitInvoke = $exitMenu.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                $exitInvoke.Invoke()
                Start-Sleep -Milliseconds 500
            }
        }

        # Wait for window to close
        do {
            Start-Sleep -Milliseconds 500
            $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)
        } while ($window)
    }
}

# Function to restart Job Processor
function Restart-JobProcessor {
    Write-Log "$(Get-Date -Format u): Restarting Job Processor..."

    # Gracefully stop JobProcessor if running
    Stop-JobProcessorGracefully

    # Fallback: kill any leftover delegate host processes just in case
    Get-Process Connectivity.JobProcessor.Delegate.Host -ErrorAction SilentlyContinue | Stop-Process -Force

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
