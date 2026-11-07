using System.Diagnostics;

namespace HorstMFG.EnvTool;

/// <summary>
/// Stops and starts the web app's IIS app pool, the same way scripts/deploy.ps1 does
/// (WebAdministration's Stop-WebAppPool / Start-WebAppPool). Needs an elevated prompt.
/// With no app pool configured (a dev PC) it does nothing.
/// </summary>
public class WebApp
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);

    private readonly string? _appPool;

    public WebApp(string? appPool) => _appPool = appPool;

    public bool IsManaged => _appPool != null;

    /// <returns>False if the pool was already stopped, so the caller leaves it that way.</returns>
    public async Task<bool> StopAsync()
    {
        if (_appPool == null) return false;
        if (await StateAsync() == "Stopped")
        {
            Console.WriteLine($"App pool '{_appPool}' is already stopped; it will be left stopped.");
            return false;
        }

        Console.WriteLine($"Stopping app pool '{_appPool}'...");
        await RunAsync($"Stop-WebAppPool -Name '{_appPool}'");

        // Wait for the pool to fully stop so w3wp.exe has closed its database connections.
        var deadline = DateTime.Now + StopTimeout;
        while (await StateAsync() != "Stopped")
        {
            if (DateTime.Now > deadline) throw new UserError($"Timed out waiting for app pool '{_appPool}' to stop.");
            await Task.Delay(500);
        }
        return true;
    }

    public async Task StartAsync()
    {
        if (_appPool == null) return;
        Console.WriteLine($"Starting app pool '{_appPool}'...");
        await RunAsync($"Start-WebAppPool -Name '{_appPool}'");
    }

    private async Task<string> StateAsync() => (await RunAsync($"(Get-WebAppPoolState -Name '{_appPool}').Value")).Trim();

    private static async Task<string> RunAsync(string command)
    {
        var psi = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-Command",
                                    $"$ErrorActionPreference = 'Stop'; Import-Module WebAdministration; {command}" })
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new UserError($"IIS command failed (run from an elevated prompt): {command}\n{(await stderr).Trim()}");
        return await stdout;
    }
}
