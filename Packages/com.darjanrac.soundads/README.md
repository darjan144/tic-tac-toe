# SoundAds SDK for Unity

In-app **audio advertising** for Unity games: non-intrusive audio ads that play over a small
banner while the player keeps playing.

- **Rewarded** ads: the player opts in, listens to the end and gets a reward.
- **Standard** ads: they show a countdown, and the player can mute them after 5 seconds.
- **Moments**: tag each request with the game moment (Win / Draw / Loss / Reward) so advertisers can target it.
- **Volume gate**: ads never play on a muted device, and rewarded ads ask the player to turn the volume up.
- **Audio focus** (Android): music apps such as Spotify pause during the ad.
- **Tracking**: impressions, quartiles, completion, mute and reward events go to the server in batches, with retries.
- **Offline fallback**: built-in test ads keep the integration working in the Editor, on WebGL and without a network connection.

Requires **Unity 6000.0+** and uGUI. It has no dependency on TextMeshPro, DOTween or any game code.

## Install

**Package Manager → + → Add package from disk…** and pick this folder's `package.json`, or add it to `Packages/manifest.json`:

```json
"com.darjanrac.soundads": "file:../path/to/com.darjanrac.soundads"
```

Next, open **Project Settings → SoundAds**. Enter your API key and server URL, then press **Test connection**.

## Quick start

```csharp
using SoundAds;

void Start()
{
    SoundAdsSdk.OnAdStarted     += _ => music.Pause();          // yield the audio channel
    SoundAdsSdk.OnAdFinished    += (_, result) => music.UnPause();
    SoundAdsSdk.OnRewardGranted += placement => coins += 50;

    SoundAdsSdk.SetConsent(cmp.HasPersonalisationConsent);       // GDPR / TCF
    SoundAdsSdk.Initialize(ok =>
        SoundAdsSdk.LoadAd("level_end", AdFormat.Rewarded, AdMoment.Win));
}

public void OnListenForCoinsClicked()
{
    if (SoundAdsSdk.IsAdReady("level_end"))
        SoundAdsSdk.ShowAd("level_end");
}
```

You can also import the **Basic Integration** sample from the Package Manager.

## API

| Member | Description |
|---|---|
| `Initialize(Action<bool>)` / `Initialize(string apiKey, Action<bool>)` | Starts the SDK and checks the server. Calls made before it finishes are queued. |
| `SetConsent(bool)` | Personalised ads on or off. Off: requests send `npa=1` and events carry no session id. |
| `LoadAd(placementId, format, moment)` | Fetches an ad and buffers it completely. Raises `OnAdLoaded` or `OnAdFailed`. |
| `IsAdReady(placementId)` | True if `ShowAd` would start playback straight away. |
| `ShowAd(placementId)` | Plays the ad. Always ends with **exactly one** `OnAdFinished`. |
| `OnAdStarted` | The ad's audio has started. Pause or duck your game audio here. |
| `OnRewardGranted` | The rewarded ad was heard to the end. Raised **before** `OnAdFinished`. |
| `OnAdFinished(placement, AdResult)` | `Completed`, `Muted` or `Failed`. It is always safe to resume your game flow here. |
| `OnAdFailed(placement, reason)` | Gives one of the `AdFailureReason` values: `NoFill`, `DeviceMuted`, `NotReady`, … |
| `IsShowingAd`, `IsInitialized`, `HasConsent`, `ActiveNetwork` | Read-only state. |

### Guarantees
- Every method runs on the main thread, and every event is raised on the main thread.
- If one of your event handlers throws, the exception is logged and the SDK carries on in a valid state.
- Each loaded ad plays only once. Call `LoadAd` again after `OnAdFinished`.
- Ad playback uses unscaled time and `ignoreListenerPause`, so `Time.timeScale = 0` or `AudioListener.pause` in your game won't stall an ad.
- Static state resets when you enter Play Mode, so the SDK works with domain reload turned off.

## Architecture

```
SoundAdsSdk (public static facade)
 ├─ IAdNetwork ── HttpAdNetwork ──┐
 │                MockAdNetwork ──┼─ FallbackAdNetwork (decorator: transport errors → mock)
 ├─ EventTracker ── IEventTransport (Http / Log), batching + exponential backoff
 ├─ IDeviceAudio ── AndroidDeviceAudio (JNI → Kotlin) | SimulatedDeviceAudio
 └─ AdPlayer (hidden DontDestroyOnLoad host) ── AudioSource + AdOverlayView (runtime-built uGUI)
```

Internals are `internal` and only visible to the SDK's own editor and test assemblies. That keeps
the public API small, so it can be versioned.

## Server contract

- `GET {serverUrl}/v1/ads?placement=&format=standard|rewarded&moment=&npa=&apiKey=` returns `AdResponse` JSON, or `204` when there is no fill.
- `POST {serverUrl}/v1/events` takes `{ "events": [ { adId, placementId, type, timestampUtc, sessionId, moment } ] }`.
- `GET {serverUrl}/health` is used by `Initialize` and by **Test connection**.

## Tests

Open **Window → General → Test Runner**:
- The EditMode suite covers wire format, JSON parsing, event batching, retry and caps, and network fallback.
- The PlayMode suite covers full publisher flows: rewarded, muted standard, not ready, device muted, tracking, and publisher exceptions.
