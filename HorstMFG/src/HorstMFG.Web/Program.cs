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
    builder.Services.AddScoped<BomPdfCopyService>();
    builder.Services.AddScoped<VaultBomIngestService>();
    builder.Services.AddScoped<ExcelScheduleParser>();
    builder.Services.AddScoped<ExcelScheduleImportService>();
    builder.Services.AddScoped<ExcelBatchParser>();
    builder.Services.AddScoped<PoBatchParser>();
    builder.Services.AddScoped<ExcelBatchImportService>();
    builder.Services.AddHttpClient<VaultGatewayClient>();
    builder.Services.AddSingleton<BomImportJobTracker>();
    builder.Services.AddScoped<ReportService>();
    builder.Services.AddScoped<LocalStorageService>();

    // Bridge SignalR
    builder.Services.AddSingleton<BridgeNotificationService>();
    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 16 * 1024 * 1024; // 16 MB — accommodates large thumbnail batches from Bridge
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
    app.MapGet("/api/reports/schedule-traveller/{name}", async (string name, ReportService reports, HttpResponse response) =>
    {
        var n     = Uri.UnescapeDataString(name);
        var bytes = await reports.GenerateScheduleShopTravellerAsync(n);
        if (bytes is null) return Results.NotFound();
        response.Headers.ContentDisposition = $"inline; filename=\"ShopTraveller-{n}.pdf\"";
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

    // Minimal API: serve local PDF copy (stored under Batches/{name}/ or Schedules/{name}/)
    var localPdfPath = builder.Configuration["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
    app.MapGet("/api/pdf-local/{number}", (string number) =>
    {
        if (number.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Results.BadRequest("Invalid file name");

        var path = Directory.EnumerateFiles(localPdfPath, number + ".pdf", SearchOption.AllDirectories)
                            .FirstOrDefault();
        if (path is null)
            return Results.NotFound();

        return Results.File(path, "application/pdf");
    }).RequireAuthorization();

    // Minimal API: PDF first-page thumbnail from network share — no cache, always fresh
    var ghostscriptPath = builder.Configuration["FileSystemPaths:GhostscriptPath"]
                       ?? @"C:\Program Files\gs\gs9.21\bin\gswin64c.exe";

    app.MapGet("/api/pdf-thumbnail-share/{number}", async (string number) =>
    {
        if (number.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Results.BadRequest("Invalid file name");

        var pdfPath = Path.Combine(pdfSharePath, number + ".pdf");
        if (!File.Exists(pdfPath))
            return Results.NotFound();

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jpg");
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = ghostscriptPath,
                Arguments              = $"-dNOPAUSE -dBATCH -dSAFER -sDEVICE=jpeg -dFirstPage=1 -dLastPage=1 -r72 -dJPEGQ=80 \"-sOutputFile={tempPath}\" \"{pdfPath}\"",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardError  = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null) return Results.Problem("Could not start Ghostscript");
            await proc.WaitForExitAsync();

            if (!File.Exists(tempPath)) return Results.NotFound();
            var bytes = await File.ReadAllBytesAsync(tempPath);
            return Results.Bytes(bytes, "image/jpeg");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }).RequireAuthorization();

    // Minimal API: PDF first-page thumbnail from local copy (cached alongside PDF)
    app.MapGet("/api/pdf-thumbnail/{number}", async (string number) =>
    {
        if (number.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Results.BadRequest("Invalid file name");

        // PDFs are stored in subdirectories (Batches/{name}/ or Schedules/{name}/)
        var pdfPath = Directory.EnumerateFiles(localPdfPath, number + ".pdf", SearchOption.AllDirectories)
                               .FirstOrDefault();
        if (pdfPath is null)
            return Results.NotFound();

        // Thumbnail lives alongside the PDF so it's cleaned up automatically when the batch/schedule is deleted
        var thumbPath = Path.ChangeExtension(pdfPath, ".jpg");

        // Serve cached thumbnail only if it's newer than the source PDF
        if (File.Exists(thumbPath) && File.GetLastWriteTimeUtc(thumbPath) >= File.GetLastWriteTimeUtc(pdfPath))
            return Results.File(thumbPath, "image/jpeg");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = ghostscriptPath,
            Arguments              = $"-dNOPAUSE -dBATCH -dSAFER -sDEVICE=jpeg -dFirstPage=1 -dLastPage=1 -r72 -dJPEGQ=80 \"-sOutputFile={thumbPath}\" \"{pdfPath}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardError  = true,
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is null) return Results.Problem("Could not start Ghostscript");
        await proc.WaitForExitAsync();

        return File.Exists(thumbPath)
            ? Results.File(thumbPath, "image/jpeg")
            : Results.NotFound();
    }).RequireAuthorization();

    // Minimal API: thumbnails
    app.MapGet("/api/thumbnails/part/{partId:int}", async (int partId, IDbContextFactory<ApplicationDbContext> db, HttpContext http) =>
    {
        http.Response.Headers.CacheControl = "no-store";
        await using var ctx = await db.CreateDbContextAsync();
        var bytes = await ctx.Parts.Where(p => p.Id == partId).Select(p => p.Thumbnail).FirstOrDefaultAsync();
        if (bytes is null) return Results.NotFound();
        var mime = bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D ? "image/bmp"
                 : bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8 ? "image/jpeg"
                 : "image/png";
        return Results.File(bytes, mime);
    }).RequireAuthorization();

    app.MapGet("/api/thumbnails/nest/{nestId:int}", async (int nestId, IDbContextFactory<ApplicationDbContext> db, HttpContext http) =>
    {
        http.Response.Headers.CacheControl = "no-store";
        await using var ctx = await db.CreateDbContextAsync();
        var bytes = await ctx.Nests.Where(n => n.Id == nestId).Select(n => n.Thumbnail).FirstOrDefaultAsync();
        if (bytes is null) return Results.NotFound();
        var mime = bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D ? "image/bmp"
                 : bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8 ? "image/jpeg"
                 : "image/png";
        return Results.File(bytes, mime);
    }).RequireAuthorization();

    // Internal API: VaultGateway → HorstMFG callback for BOM ingest
    var gatewayCallbackKey = builder.Configuration["Gateway:CallbackApiKey"] ?? "";

    app.MapPost("/api/internal/vault-bom-result", async (
        HttpContext http,
        HorstMFG.Core.DTOs.VaultBomCallbackPayload payload,
        VaultBomIngestService ingest) =>
    {
        if (string.IsNullOrEmpty(gatewayCallbackKey) ||
            http.Request.Headers["X-Gateway-Key"].ToString() != gatewayCallbackKey)
        {
            return Results.Unauthorized();
        }

        await ingest.IngestSingleAsync(payload, http.RequestAborted);
        return Results.Ok();
    }).DisableAntiforgery();

    // Dev-only helper: register a fake job in the in-memory tracker so a callback
    // can be tested end-to-end without a full Excel import flow in place.
    if (app.Environment.IsDevelopment())
    {
        app.MapPost("/api/internal/test-register-job", (
            HttpContext http,
            TestRegisterJobRequest req,
            BomImportJobTracker tracker) =>
        {
            if (string.IsNullOrEmpty(gatewayCallbackKey) ||
                http.Request.Headers["X-Gateway-Key"].ToString() != gatewayCallbackKey)
            {
                return Results.Unauthorized();
            }

            tracker.Register(req.JobId, req.ImportType);
            return Results.Ok();
        }).DisableAntiforgery();
    }

    // Reject bad API keys before the WebSocket upgrade so the bridge's
    // StartAsync throws an HTTP 401 rather than connecting then aborting.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs/bridge"))
        {
            var expectedKey = builder.Configuration["Bridge:ApiKey"] ?? "";
            var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault()
                      ?? context.Request.Query["access_token"].FirstOrDefault()
                      ?? "";

            if (!string.IsNullOrEmpty(expectedKey) &&
                !string.Equals(apiKey, expectedKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid Bridge API key");
                return;
            }
        }
        await next(context);
    });

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

internal record TestRegisterJobRequest(string JobId, HorstMFG.Core.Enums.BomType ImportType);
