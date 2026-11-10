using System;
using System.Collections.Generic;
using System.Threading;
using SoundAds.Internal;
using UnityEngine;

namespace SoundAds
{
    /// <summary>
    /// Public entry point of the SoundAds SDK. All methods must be called from the main thread;
    /// all events are raised on the main thread.
    /// </summary>
    /// <example>
    /// <code>
    /// SoundAdsSdk.Initialize(ok => SoundAdsSdk.LoadAd("level_end", AdFormat.Rewarded, AdMoment.Win));
    /// SoundAdsSdk.OnRewardGranted += placement => GiveCoins(50);
    /// if (SoundAdsSdk.IsAdReady("level_end")) SoundAdsSdk.ShowAd("level_end");
    /// </code>
    /// </example>
    public static class SoundAdsSdk
    {
        public const string Version = "0.1.0";

        enum PlacementStatus { Idle, Loading, Ready, Showing }

        sealed class Placement
        {
            public PlacementStatus Status;
            public AdFormat Format;
            public AdMoment Moment;
            public LoadedAd Ad;
        }

        static readonly Dictionary<string, Placement> Placements = new();
        static readonly List<Action> PendingUntilInitialized = new();

        static SoundAdsSettings _settings;
        static IAdNetwork _network;
        static IDeviceAudio _device;
        static EventTracker _tracker;
        static AdPlayer _player;
        static CancellationTokenSource _lifetime;
        static readonly string SessionId = Guid.NewGuid().ToString("N");

        // ---------------------------------------------------------------- state

        /// <summary>True once <see cref="Initialize(Action{bool})"/> has completed successfully.</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>True while initialisation is in flight. Calls made meanwhile are queued.</summary>
        public static bool IsInitializing { get; private set; }

        /// <summary>Whether the user consented to personalised ads (GDPR/TCF). Defaults to false.</summary>
        public static bool HasConsent { get; private set; }

        /// <summary>True while an ad is audible. Publishers typically pause their music while this is set.</summary>
        public static bool IsShowingAd => _player != null && _player.IsPlaying;

        /// <summary>Name of the active ad source ("Http+Mock", "Mock"...), for diagnostics.</summary>
        public static string ActiveNetwork => _network?.Name ?? "None";

        // ---------------------------------------------------------------- events

        /// <summary>An ad for the placement finished downloading and can be shown instantly.</summary>
        public static event Action<string> OnAdLoaded;

        /// <summary>A load or show failed. Second argument is an <see cref="AdFailureReason"/> value.</summary>
        public static event Action<string, string> OnAdFailed;

        /// <summary>Ad audio started. Pause or duck game audio here.</summary>
        public static event Action<string> OnAdStarted;

        /// <summary>
        /// Raised exactly once per <see cref="ShowAd"/> call, including failures, so it is always safe
        /// to resume the game flow from here.
        /// </summary>
        public static event Action<string, AdResult> OnAdFinished;

        /// <summary>A Rewarded ad was listened to completely. Raised before <see cref="OnAdFinished"/>.</summary>
        public static event Action<string> OnRewardGranted;

        // ---------------------------------------------------------------- lifecycle

        /// <summary>Initialise with the API key from <see cref="SoundAdsSettings"/>.</summary>
        public static void Initialize(Action<bool> onComplete = null) =>
            Initialize(null, onComplete);

        /// <summary>
        /// Initialise the SDK. Safe to call more than once. Probes the ad server; if it is unreachable and
        /// <see cref="SoundAdsSettings.fallbackToOfflineAds"/> is on, offline test ads are served instead.
        /// </summary>
        public static void Initialize(string apiKey, Action<bool> onComplete = null)
        {
            if (IsInitialized) { onComplete?.Invoke(true); return; }
            if (IsInitializing)
            {
                PendingUntilInitialized.Add(() => onComplete?.Invoke(IsInitialized));
                return;
            }

            _settings = SoundAdsSettings.LoadOrDefault();
            SoundAdsLog.Verbose = _settings.verboseLogging;

            string key = string.IsNullOrWhiteSpace(apiKey) ? _settings.apiKey : apiKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                SoundAdsLog.Error("Initialize failed: no API key. Set one in Project Settings > SoundAds.");
                onComplete?.Invoke(false);
                return;
            }

            IsInitializing = true;
            _ = InitializeAsync(key, onComplete);
        }

        static async Awaitable InitializeAsync(string apiKey, Action<bool> onComplete)
        {
            _lifetime = new CancellationTokenSource();
            SoundAdsLog.Info($"Initializing SDK v{Version} (testMode={_settings.testMode}, server={_settings.serverUrl})");

            IEventTransport transport;
            if (_settings.testMode)
            {
                _network = new MockAdNetwork();
                transport = new LogEventTransport();
            }
            else
            {
                var http = new HttpAdNetwork(_settings.serverUrl, apiKey, _settings.requestTimeoutSec);
                var lifetime = _lifetime;
                bool reachable = false;
                try { reachable = await http.PingAsync(lifetime.Token); }
                catch (OperationCanceledException) { }

                // The SDK was reset (e.g. by tests or leaving Play Mode) while we were probing.
                if (lifetime.IsCancellationRequested) return;

                if (reachable)
                {
                    _network = _settings.fallbackToOfflineAds ? new FallbackAdNetwork(http, new MockAdNetwork()) : http;
                    transport = new HttpEventTransport(_settings.serverUrl, apiKey, _settings.requestTimeoutSec);
                }
                else if (_settings.fallbackToOfflineAds)
                {
                    SoundAdsLog.Warn($"Ad server {_settings.serverUrl} unreachable; serving offline test ads");
                    _network = new MockAdNetwork();
                    transport = new LogEventTransport();
                }
                else
                {
                    SoundAdsLog.Warn($"Ad server {_settings.serverUrl} unreachable; requests will fail until it is back");
                    _network = http;
                    transport = new HttpEventTransport(_settings.serverUrl, apiKey, _settings.requestTimeoutSec);
                }
            }

            Bootstrap(_network, transport, DeviceAudioFactory.Create(_settings), _settings);
            SoundAdsLog.Info($"Initialized: network={_network.Name}, deviceAudio={_device.Name}, consent={HasConsent}");

            onComplete?.Invoke(true);
            RunPending();
        }

        /// <summary>Wires the runtime together. Also used by tests to inject fakes.</summary>
        internal static void Bootstrap(IAdNetwork network, IEventTransport transport, IDeviceAudio device, SoundAdsSettings settings)
        {
            _settings = settings;
            _network = network;
            _device = device;
            _tracker = new EventTracker(transport, settings.eventFlushIntervalSec) { SessionId = HasConsent ? SessionId : string.Empty };
            _player = AdPlayer.Create(settings, _tracker, device);
            _lifetime ??= new CancellationTokenSource();

            IsInitializing = false;
            IsInitialized = true;
        }

        /// <summary>
        /// Record the user's consent for personalised ads. Without consent, requests are flagged
        /// non-personalised (<c>npa=1</c>) and events carry no session id.
        /// </summary>
        public static void SetConsent(bool hasConsent)
        {
            HasConsent = hasConsent;
            if (_tracker != null) _tracker.SessionId = hasConsent ? SessionId : string.Empty;
            SoundAdsLog.Info($"Consent set: personalised={hasConsent}");
        }

        // ---------------------------------------------------------------- ads

        /// <summary>
        /// Request and pre-buffer an ad for a placement. Raises <see cref="OnAdLoaded"/> or <see cref="OnAdFailed"/>.
        /// If an ad is already loaded for the placement, <see cref="OnAdLoaded"/> is raised again immediately.
        /// </summary>
        /// <param name="placementId">Your identifier for where in the app the ad shows, e.g. "level_complete".</param>
        /// <param name="format">Standard (mutable) or Rewarded.</param>
        /// <param name="moment">The game moment, used for advertiser targeting.</param>
        public static void LoadAd(string placementId, AdFormat format, AdMoment moment = AdMoment.None)
        {
            if (string.IsNullOrWhiteSpace(placementId))
            {
                RaiseFailed(placementId, AdFailureReason.InvalidPlacement);
                return;
            }

            if (IsInitializing)
            {
                PendingUntilInitialized.Add(() => LoadAd(placementId, format, moment));
                return;
            }

            if (!IsInitialized)
            {
                SoundAdsLog.Warn($"LoadAd('{placementId}') called before Initialize");
                RaiseFailed(placementId, AdFailureReason.NotInitialized);
                return;
            }

            var placement = GetPlacement(placementId);
            switch (placement.Status)
            {
                case PlacementStatus.Loading:
                    SoundAdsLog.Info($"LoadAd('{placementId}') ignored: already loading");
                    return;
                case PlacementStatus.Showing:
                    RaiseFailed(placementId, AdFailureReason.AlreadyShowing);
                    return;
                case PlacementStatus.Ready when placement.Format == format:
                    SoundAdsLog.Info($"LoadAd('{placementId}'): ad already loaded");
                    Raise(() => OnAdLoaded?.Invoke(placementId));
                    return;
            }

            placement.Status = PlacementStatus.Loading;
            placement.Format = format;
            placement.Moment = moment;
            placement.Ad = null;

            _ = LoadAsync(placementId, placement, new AdRequest(placementId, format, moment, HasConsent));
        }

        static async Awaitable LoadAsync(string placementId, Placement placement, AdRequest request)
        {
            SoundAdsLog.Info($"Requesting ad: {request} via {_network.Name}");

            AdLoadResult result;
            try
            {
                result = await _network.RequestAdAsync(request, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                return; // SDK shut down
            }
            catch (Exception e)
            {
                SoundAdsLog.Error($"Ad request for '{placementId}' threw: {e}");
                result = AdLoadResult.Fail(AdFailureReason.NetworkError, transport: true);
            }

            if (!result.Success)
            {
                placement.Status = PlacementStatus.Idle;
                SoundAdsLog.Info($"No ad for '{placementId}': {result.Error}");
                RaiseFailed(placementId, result.Error);
                return;
            }

            placement.Ad = result.Ad;
            placement.Status = PlacementStatus.Ready;
            SoundAdsLog.Info($"Ad loaded for '{placementId}': {result.Ad.Metadata.adId} ({result.Ad.Metadata.durationSec:0.0}s)");
            Raise(() => OnAdLoaded?.Invoke(placementId));
        }

        /// <summary>True if <see cref="ShowAd"/> would start playback immediately.</summary>
        public static bool IsAdReady(string placementId) =>
            placementId != null &&
            Placements.TryGetValue(placementId, out var p) &&
            p.Status == PlacementStatus.Ready;

        /// <summary>
        /// Play the loaded ad for a placement. Always ends with exactly one <see cref="OnAdFinished"/>.
        /// </summary>
        public static void ShowAd(string placementId)
        {
            if (!IsInitialized) { FailShow(placementId, AdFailureReason.NotInitialized); return; }
            if (IsShowingAd) { FailShow(placementId, AdFailureReason.AlreadyShowing); return; }
            if (!IsAdReady(placementId)) { FailShow(placementId, AdFailureReason.NotReady); return; }

            var placement = Placements[placementId];

            // Volume gate: an audio ad nobody can hear is worthless to the advertiser. The ad stays loaded.
            if (_device.IsMuted())
            {
                SoundAdsLog.Info($"ShowAd('{placementId}') blocked: device muted");
                FailShow(placementId, AdFailureReason.DeviceMuted);
                return;
            }

            bool lowVolume = placement.Format == AdFormat.Rewarded && _device.GetVolume01() < _settings.lowVolumeThreshold;

            placement.Status = PlacementStatus.Showing;
            var ad = placement.Ad;
            var format = placement.Format;
            var moment = placement.Moment;

            Raise(() => OnAdStarted?.Invoke(placementId));
            _player.Play(ad, format, moment, lowVolume, result =>
            {
                placement.Status = PlacementStatus.Idle;
                placement.Ad = null;

                if (result == AdResult.Completed && format == AdFormat.Rewarded)
                {
                    _tracker.Track("reward", ad.Metadata, moment);
                    SoundAdsLog.Info($"Reward granted for '{placementId}'");
                    Raise(() => OnRewardGranted?.Invoke(placementId));
                }

                Raise(() => OnAdFinished?.Invoke(placementId, result));
            });
        }

        // ---------------------------------------------------------------- helpers

        static Placement GetPlacement(string placementId)
        {
            if (!Placements.TryGetValue(placementId, out var placement))
            {
                placement = new Placement();
                Placements[placementId] = placement;
            }
            return placement;
        }

        static void FailShow(string placementId, string reason)
        {
            RaiseFailed(placementId, reason);
            Raise(() => OnAdFinished?.Invoke(placementId, AdResult.Failed));
        }

        static void RaiseFailed(string placementId, string reason) =>
            Raise(() => OnAdFailed?.Invoke(placementId, reason));

        // Publisher callbacks are isolated: an exception in game code must never break the SDK state machine.
        static void Raise(Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        static void RunPending()
        {
            var pending = PendingUntilInitialized.ToArray();
            PendingUntilInitialized.Clear();
            foreach (var call in pending) Raise(call);
        }

        /// <summary>
        /// Resets all static state. Runs automatically when entering Play Mode so the SDK behaves
        /// correctly with "Enter Play Mode Options" (domain reload disabled). Also used by tests.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForTests()
        {
            _lifetime?.Cancel();
            _lifetime = null;

            if (_player != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_player.gameObject);
                else UnityEngine.Object.DestroyImmediate(_player.gameObject);
            }
            _player = null;

            Placements.Clear();
            PendingUntilInitialized.Clear();
            _network = null;
            _device = null;
            _tracker = null;
            _settings = null;
            IsInitialized = false;
            IsInitializing = false;
            HasConsent = false;

            OnAdLoaded = null;
            OnAdFailed = null;
            OnAdStarted = null;
            OnAdFinished = null;
            OnRewardGranted = null;
        }

        /// <summary>Test hook: the live player, so tests can fast-forward or mute.</summary>
        internal static AdPlayer Player => _player;
    }
}
