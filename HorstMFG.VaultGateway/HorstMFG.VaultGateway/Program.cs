using HorstMFG.VaultGateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

// Windows Services don't reliably run with the executable's folder as the working
// directory, so relative paths here can silently resolve elsewhere (e.g. System32).
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "gateway-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var isConsole = args.Length > 0 &&
                    args[0].Equals("--console", StringComparison.OrdinalIgnoreCase);

    var builder = Host.CreateDefaultBuilder(args)
        .UseContentRoot(AppContext.BaseDirectory)
        .UseSerilog((ctx, services, cfg) =>
        {
            var isDev = ctx.HostingEnvironment.IsDevelopment();
            cfg.MinimumLevel.Is(isDev ? LogEventLevel.Debug : LogEventLevel.Information)
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
               .MinimumLevel.Override("System",    LogEventLevel.Warning)
               .WriteTo.Console()
               .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
        })
        .ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            cfg.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json",
                            optional: true, reloadOnChange: true);
        })
        .ConfigureServices((ctx, services) =>
        {
            services.Configure<GatewayConfig>(ctx.Configuration.GetSection("Gateway"));
            services.Configure<VaultConfig>(ctx.Configuration.GetSection("Vault"));
            services.AddHttpClient();
            services.AddSingleton<VaultClient>();
            services.AddSingleton<ImportJobRunner>();
            services.AddHostedService<Worker>();
        });

    if (!isConsole)
        builder.UseWindowsService();

    var host = builder.Build();

    // Secrets are blank in appsettings.json; on the server they live in appsettings.Production.json,
    // which deploys never overwrite. Say so up front rather than failing later with a 401 or a login error.
    var config = host.Services.GetRequiredService<IConfiguration>();
    foreach (var key in new[] { "Gateway:CallbackApiKey", "Vault:Username", "Vault:Password" })
    {
        if (string.IsNullOrWhiteSpace(config[key]))
            Log.Error("{Key} is not set — on the server it belongs in appsettings.Production.json", key);
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "VaultGateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
