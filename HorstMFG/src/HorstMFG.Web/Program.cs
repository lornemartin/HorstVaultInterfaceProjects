using System.Security.Claims;
using HorstMFG.Core.Interfaces;
using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Components;
using HorstMFG.Web.Hubs;
using HorstMFG.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/horstmfg-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // HTTP context accessor (for auth in Blazor SSR)
    builder.Services.AddHttpContextAccessor();

    // Database
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<DbInitializer>();

    // Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/login";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });
    builder.Services.AddCascadingAuthenticationState();

    // Authorization policies
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Admin",            policy => policy.RequireClaim(ClaimTypes.Role, "Admin"))
        .AddPolicy("EngineeringOnly",  policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "Engineering"))
        .AddPolicy("Engineering",      policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "Engineering", "ShopFloor"))
        .AddPolicy("ShopFloor",        policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "ShopFloor"))
        .AddPolicy("Nesting",          policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "Nesting"));

    // Services
    builder.Services.AddScoped<IBomService, BomService>();
    builder.Services.AddScoped<ReportService>();
    builder.Services.AddScoped<ScheduleTreeFilterState>();
    builder.Services.AddScoped<ScheduleTreeAdaptor>();
    builder.Services.AddScoped<BatchTreeFilterState>();
    builder.Services.AddScoped<BatchTreeAdaptor>();

    // Bridge SignalR
    builder.Services.AddSingleton<BridgeNotificationService>();
    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 512 * 1024; // 512 KB — accommodates large BOM export files
    });

    // Syncfusion
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
        builder.Configuration["Syncfusion:LicenseKey"] ?? "");
    builder.Services.AddSyncfusionBlazor();

    // Blazor
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // Minimal API: reports
    app.MapGet("/api/reports/schedule-laser/{name}", async (string name, ReportService reports, HttpResponse response) =>
    {
        var bytes = await reports.GenerateScheduleLaserReportAsync(Uri.UnescapeDataString(name));
        if (bytes is null) return Results.NotFound();
        var fileName = $"Laser-Schedule-{Uri.UnescapeDataString(name)}.pdf";
        response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return Results.File(bytes, "application/pdf");
    }).RequireAuthorization();

    app.MapGet("/api/reports/batch-laser/{name}", async (string name, ReportService reports, HttpResponse response) =>
    {
        var bytes = await reports.GenerateBatchLaserReportAsync(Uri.UnescapeDataString(name));
        if (bytes is null) return Results.NotFound();
        var fileName = $"Laser-Batch-{Uri.UnescapeDataString(name)}.pdf";
        response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return Results.File(bytes, "application/pdf");
    }).RequireAuthorization();

    app.MapGet("/api/reports/schedule-op/{name}/{operation}", async (string name, string operation, ReportService reports, HttpResponse response) =>
    {
        var n  = Uri.UnescapeDataString(name);
        var op = Uri.UnescapeDataString(operation);
        var bytes = await reports.GenerateScheduleOperationReportAsync(n, op);
        if (bytes is null) return Results.NotFound();
        response.Headers.ContentDisposition = $"inline; filename=\"{op}-Schedule-{n}.pdf\"";
        return Results.File(bytes, "application/pdf");
    }).RequireAuthorization();

    app.MapGet("/api/reports/batch-op/{name}/{operation}", async (string name, string operation, ReportService reports, HttpResponse response) =>
    {
        var n  = Uri.UnescapeDataString(name);
        var op = Uri.UnescapeDataString(operation);
        var bytes = await reports.GenerateBatchOperationReportAsync(n, op);
        if (bytes is null) return Results.NotFound();
        response.Headers.ContentDisposition = $"inline; filename=\"{op}-Batch-{n}.pdf\"";
        return Results.File(bytes, "application/pdf");
    }).RequireAuthorization();

    // Minimal API: serve PDFs from the share
    var pdfSharePath = builder.Configuration["FileSystemPaths:PdfSharePath"] ?? @"S:\PDF Drawing Files\";
    app.MapGet("/api/pdf/{fileName}", (string fileName) =>
    {
        // Sanitize — only allow alphanumeric, dash, underscore, dot
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Results.BadRequest("Invalid file name");

        var path = Path.Combine(pdfSharePath, fileName + ".pdf");
        if (!File.Exists(path))
            return Results.NotFound();

        return Results.File(path, "application/pdf");
    }).RequireAuthorization();

    app.MapHub<BridgeHub>("/hubs/bridge");

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
