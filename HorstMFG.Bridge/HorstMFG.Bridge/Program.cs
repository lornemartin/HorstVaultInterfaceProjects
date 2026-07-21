using HorstMFG.Bridge;
using HorstMFG.Bridge.Handlers;
using HorstMFG.Bridge.Nesting;
using HorstMFG.Bridge.Nesting.Radan;
using HorstMFG.Bridge.Vault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

// Windows Services don't reliably run with the executable's folder as the working
// directory, so relative paths here can silently resolve elsewhere (e.g. System32).
var logPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "Horst Manufacturing", "Bridge", "logs", "bridge-.log");

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
            services.Configure<BridgeConfig>(ctx.Configuration.GetSection("Bridge"));
            services.Configure<VaultConfig>(ctx.Configuration.GetSection("Vault"));

            // Nesting software implementation (swap here to support future CAM software)
            var software = ctx.Configuration["Bridge:NestingSoftware"] ?? "Radan";
            if (software.Equals("Radan", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<INestingProjectService, RadanProjectService>();
                services.AddHostedService<RadanConnectionService>();
            }
            else
                throw new NotSupportedException($"Unsupported nesting software: {software}");

            services.AddSingleton<IVaultService, VaultService>();

            // Handlers
            services.AddSingleton<SendToNestingHandler>();
            services.AddSingleton<RetrieveFromNestingHandler>();
            services.AddSingleton<RetrieveFromVaultHandler>();
            services.AddSingleton<SyncHandler>();
            services.AddSingleton<FinalizeHandler>();
            services.AddSingleton<UpdateThumbnailHandler>();
            services.AddSingleton<FileWatcher>();
            services.AddSingleton<BomExportWatcher>();

            services.AddHostedService<Worker>();
        });

    if (!isConsole)
        builder.UseWindowsService();

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bridge terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
