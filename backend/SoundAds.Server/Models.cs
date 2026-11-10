namespace SoundAds.Server;

/// <summary>An ad creative in the inventory file (<c>ads.json</c>).</summary>
public sealed record AdCreative
{
    public required string AdId { get; init; }
    public required string Format { get; init; }          // "standard" | "rewarded"
    public required string AudioFile { get; init; }       // file under wwwroot/audio
    public required string Advertiser { get; init; }
    public required string Title { get; init; }
    public string ClickUrl { get; init; } = "";
    public string[] TargetMoments { get; init; } = [];    // empty = any moment
    public float DurationSec { get; set; }                // filled from the WAV header at startup
}

/// <summary>Response of <c>GET /v1/ads</c>. Mirrors <c>SoundAds.AdResponse</c> in the Unity SDK.</summary>
public sealed record AdResponse(
    string AdId,
    string PlacementId,
    string Format,
    string AudioUrl,
    float DurationSec,
    string Advertiser,
    string Title,
    string ClickUrl,
    string[] TargetMoments);

/// <summary>One tracking beacon sent by the SDK.</summary>
public sealed record AdEvent(
    string AdId,
    string PlacementId,
    string Type,
    string TimestampUtc,
    string SessionId,
    string Moment);

public sealed record AdEventBatch(AdEvent[] Events);

public sealed record AdReportRow(
    string Key,
    int Impressions,
    int Completes,
    int Mutes,
    int Rewards,
    double CompletionRate);

public sealed record AdReport(
    int TotalEvents,
    int UniqueSessions,
    AdReportRow[] ByAd,
    AdReportRow[] ByMoment,
    AdReportRow[] ByPlacement);

public sealed class SoundAdsOptions
{
    public string[] ApiKeys { get; set; } = ["test-key"];
    public string InventoryFile { get; set; } = "ads.json";
}
