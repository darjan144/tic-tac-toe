using Microsoft.Extensions.Options;
using SoundAds.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SoundAdsOptions>(builder.Configuration.GetSection("SoundAds"));
builder.Services.AddSingleton<EventStore>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var options = sp.GetRequiredService<IOptions<SoundAdsOptions>>().Value;
    return AdInventory.Load(
        Path.Combine(env.ContentRootPath, options.InventoryFile),
        Path.Combine(env.WebRootPath, "audio"),
        sp.GetRequiredService<ILoggerFactory>().CreateLogger<AdInventory>());
});

// The Unity SDK calls us from WebGL builds hosted on other origins.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", version = "0.1.0" }));

// ---- Ad request: format + moment targeting, 204 = no fill ----
app.MapGet("/v1/ads", (HttpRequest request, string? placement, string? format, string? moment, string? apiKey,
                       AdInventory inventory, IOptions<SoundAdsOptions> options, ILogger<Program> log) =>
{
    string key = apiKey ?? request.Headers["X-Api-Key"].ToString();
    if (!options.Value.ApiKeys.Contains(key))
        return Results.Unauthorized();

    string fmt = string.IsNullOrWhiteSpace(format) ? "standard" : format.ToLowerInvariant();
    string mom = string.IsNullOrWhiteSpace(moment) ? "none" : moment.ToLowerInvariant();

    var ad = inventory.Select(fmt, mom, Random.Shared);
    if (ad is null)
    {
        log.LogInformation("No fill: placement={Placement} format={Format} moment={Moment}", placement, fmt, mom);
        return Results.NoContent();
    }

    string audioUrl = $"{request.Scheme}://{request.Host}/audio/{Uri.EscapeDataString(ad.AudioFile)}";
    log.LogInformation("Fill {AdId} -> placement={Placement} moment={Moment}", ad.AdId, placement, mom);

    return Results.Ok(new AdResponse(ad.AdId, placement ?? "", ad.Format, audioUrl, ad.DurationSec,
                                     ad.Advertiser, ad.Title, ad.ClickUrl, ad.TargetMoments));
});

// ---- Tracking beacons (batched by the SDK) ----
app.MapPost("/v1/events", (AdEventBatch batch, HttpRequest request, EventStore store, IOptions<SoundAdsOptions> options) =>
{
    if (!options.Value.ApiKeys.Contains(request.Headers["X-Api-Key"].ToString()))
        return Results.Unauthorized();

    int accepted = store.Add(batch.Events ?? []);
    return Results.Accepted(value: new { accepted });
});

app.MapGet("/v1/report", (EventStore store) => Results.Ok(store.BuildReport()));
app.MapGet("/v1/inventory", (AdInventory inventory) => Results.Ok(inventory.All));
app.MapGet("/dashboard", () => Results.Redirect("/dashboard.html"));

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the test project.</summary>
public partial class Program;
