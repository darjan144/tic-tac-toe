# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-11-09

### Added
- `SoundAdsSdk` public API: `Initialize`, `SetConsent`, `LoadAd`, `IsAdReady`, `ShowAd` and the
  `OnAdLoaded` / `OnAdFailed` / `OnAdStarted` / `OnRewardGranted` / `OnAdFinished` events.
- Ad formats: Rewarded and Standard (mute unlocks after a configurable delay).
- Game-moment targeting (`AdMoment`).
- HTTP ad network with full pre-buffering of the audio creative, plus an offline mock network
  with procedurally generated audio, used in test mode and as an automatic fallback.
- Event tracking (impression, quartiles, complete, mute, reward) with batching, retry and backoff.
- Volume gate: ads are not played on a muted device; rewarded ads hint when volume is low.
- Android native bridge (Kotlin) for device volume and transient audio focus.
- Runtime-built overlay banner (no prefab / TextMeshPro dependency), safe-area aware.
- Project Settings page (`Project/SoundAds`) with connection test.
- EditMode and PlayMode tests, Basic Integration sample.
