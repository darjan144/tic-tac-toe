using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundAds.Server;

namespace SoundAds.Server.Tests;

public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("rewarded", "reward")]
    [InlineData("standard", "win")]
    [InlineData("standard", "draw")]
    public async Task Ads_Fill_ReturnsMatchingFormatWithPlayableAudio(string format, string moment)
    {
        var ad = await _client.GetFromJsonAsync<AdResponse>(
            $"/v1/ads?placement=p1&format={format}&moment={moment}&apiKey=test-key");

        Assert.NotNull(ad);
        Assert.Equal(format, ad.Format);
        Assert.Equal("p1", ad.PlacementId);
        Assert.True(ad.DurationSec > 5f, "sample ads should be longer than the 5s mute delay");
        Assert.True(ad.TargetMoments.Length == 0 || ad.TargetMoments.Contains(moment));

        var audio = await _client.GetAsync(new Uri(ad.AudioUrl).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, audio.StatusCode);
    }

    [Fact]
    public async Task Ads_UnknownFormat_IsNoFill()
    {
        var response = await _client.GetAsync("/v1/ads?placement=p1&format=video&moment=win&apiKey=test-key");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Ads_BadApiKey_IsUnauthorized()
    {
        var response = await _client.GetAsync("/v1/ads?placement=p1&format=standard&apiKey=nope");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ads_AcceptsApiKeyHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/ads?placement=p1&format=rewarded&moment=reward");
        request.Headers.Add("X-Api-Key", "test-key");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Events_AreAccepted_AndReported()
    {
        // Same JSON shape the Unity SDK's JsonUtility produces.
        const string json = """
        {"events":[
          {"adId":"report-test","placementId":"p9","type":"impression","timestampUtc":"2026-11-09T10:00:00Z","sessionId":"s1","moment":"win"},
          {"adId":"report-test","placementId":"p9","type":"complete","timestampUtc":"2026-11-09T10:00:12Z","sessionId":"s1","moment":"win"}
        ]}
        """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/events")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", "test-key");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var report = await _client.GetFromJsonAsync<AdReport>("/v1/report");
        var row = Assert.Single(report!.ByAd, r => r.Key == "report-test");
        Assert.Equal(1, row.Impressions);
        Assert.Equal(1, row.Completes);
        Assert.Equal(1.0, row.CompletionRate);
    }

    [Fact]
    public async Task Events_WithoutApiKey_AreRejected()
    {
        var response = await _client.PostAsJsonAsync("/v1/events", new { events = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cors_AllowsAnyOrigin_ForWebGL()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://example.github.io");
        var response = await _client.SendAsync(request);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
