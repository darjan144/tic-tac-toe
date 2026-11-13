# SoundAds ad server

A small ASP.NET Core (.NET 10) minimal API. It serves audio ads to the Unity SDK and collects its tracking events.

```bash
cd backend/SoundAds.Server
dotnet run --no-launch-profile --urls http://0.0.0.0:5080
```

Then open http://localhost:5080/dashboard to see the live report. Binding to `0.0.0.0` lets a phone reach the server, over USB with `adb reverse tcp:5080 tcp:5080` or over the same Wi-Fi using the PC's IP. For the full device setup, see the [root README](../README.md#full-android-build-game-sdk-bridge-and-server-on-a-phone).

| Endpoint | Purpose |
|---|---|
| `GET /health` | Liveness check. The SDK calls it during `Initialize` to choose between the server and the offline ads. |
| `GET /v1/ads?placement=&format=standard\|rewarded&moment=win\|draw\|loss\|reward&apiKey=` | Picks a random creative that matches the format and moment. Returns `204` for no fill and `401` for a bad key. The key can also be sent in the `X-Api-Key` header. |
| `POST /v1/events` (header `X-Api-Key`) | Takes a batch of `{ events: [ { adId, placementId, type, timestampUtc, sessionId, moment } ] }` and returns `202 { accepted }`. |
| `GET /v1/report` | Impressions, completes, mutes, rewards and completion rate, grouped by ad, by moment and by placement. |
| `GET /v1/inventory` | The loaded creatives. |
| `GET /dashboard` | HTML dashboard built on `/v1/report`. |

```bash
curl "http://localhost:5080/v1/ads?placement=p1&format=rewarded&moment=reward&apiKey=test-key"
```

- The inventory lives in `ads.json`. Audio files go in `wwwroot/audio/`, and each duration is read from the WAV header.
- The sample ads advertise made-up brands. They were generated offline with Windows text-to-speech.
- Events are kept in memory only, so they reset on restart.
- CORS is open so that WebGL builds can call the server.
- API keys are set in `appsettings.json` under `SoundAds:ApiKeys`.

Run the tests with:

```bash
dotnet test backend
```
