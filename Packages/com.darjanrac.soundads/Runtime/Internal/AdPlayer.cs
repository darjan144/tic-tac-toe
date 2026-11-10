using System;
using SoundAds.UI;
using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>
    /// Hidden, persistent host object created by <see cref="SoundAdsSdk.Initialize(Action{bool})"/>.
    /// Owns the ad AudioSource and overlay, drives playback (quartile tracking, mute, app pause)
    /// and ticks the <see cref="EventTracker"/>. Lives under "[SoundAds]" with DontDestroyOnLoad.
    /// </summary>
    [AddComponentMenu("")] // not meant to be added by hand
    internal sealed class AdPlayer : MonoBehaviour
    {
        enum Quartile { None, Start, First, Mid, Third }

        AudioSource _source;
        AdOverlayView _overlay;
        EventTracker _tracker;
        IDeviceAudio _device;
        SoundAdsSettings _settings;

        LoadedAd _ad;
        AdFormat _format;
        AdMoment _moment;
        Action<AdResult> _onFinished;
        Quartile _quartile;
        float _elapsed;
        bool _appPaused;

        public bool IsPlaying => _ad != null;

        internal AdOverlayView Overlay => _overlay;

        /// <summary>Normalised playback progress of the current ad (0-1); used by tests.</summary>
        public float Progress => _ad == null ? 0f : Mathf.Clamp01(_elapsed / Duration);

        float Duration => Mathf.Max(0.01f, _ad.Clip != null ? _ad.Clip.length : _ad.Metadata.durationSec);

        public static AdPlayer Create(SoundAdsSettings settings, EventTracker tracker, IDeviceAudio device)
        {
            var go = new GameObject("[SoundAds]");
            DontDestroyOnLoad(go);

            var player = go.AddComponent<AdPlayer>();
            player._settings = settings;
            player._tracker = tracker;
            player._device = device;

            player._source = go.AddComponent<AudioSource>();
            player._source.playOnAwake = false;
            player._source.loop = false;
            player._source.spatialBlend = 0f;
            player._source.ignoreListenerPause = true; // keep playing even if the game pauses AudioListener

            player._overlay = AdOverlayView.Create(go.transform);
            player._overlay.MuteClicked += player.OnMuteClicked;
            return player;
        }

        public void Play(LoadedAd ad, AdFormat format, AdMoment moment, bool showLowVolumeHint, Action<AdResult> onFinished)
        {
            _ad = ad;
            _format = format;
            _moment = moment;
            _onFinished = onFinished;
            _quartile = Quartile.None;
            _elapsed = 0f;

            _device.RequestAudioFocus();

            _source.clip = ad.Clip;
            _source.time = 0f;
            _source.Play();

            _overlay.Show(ad.Metadata, format, showLowVolumeHint);
            _tracker.Track("impression", ad.Metadata, moment);
            SoundAdsLog.Info($"Playing '{ad.Metadata.title}' by {ad.Metadata.advertiser} ({Duration:0.0}s, {format})");
        }

        void Update()
        {
            _tracker?.Tick(Time.unscaledTime);

            if (_ad == null || _appPaused) return;

            // Time is advanced manually (unscaled) so Time.timeScale = 0 in the game can't stall an ad.
            _elapsed += Time.unscaledDeltaTime;
            float progress = Progress;
            TrackQuartiles(progress);

            float remaining = Mathf.Max(0f, Duration - _elapsed);
            float muteIn = _format == AdFormat.Standard ? Mathf.Max(0f, _settings.standardMuteDelaySec - _elapsed) : -1f;
            _overlay.UpdateProgress(progress, remaining, muteIn);

            bool clipEnded = _source.clip != null && !_source.isPlaying && _elapsed > 0.1f;
            if (_elapsed >= Duration || clipEnded)
                Finish(AdResult.Completed);
        }

        void TrackQuartiles(float progress)
        {
            if (_quartile < Quartile.Start) { _quartile = Quartile.Start; _tracker.Track("start", _ad.Metadata, _moment); }
            if (_quartile < Quartile.First && progress >= 0.25f) { _quartile = Quartile.First; _tracker.Track("firstQuartile", _ad.Metadata, _moment); }
            if (_quartile < Quartile.Mid && progress >= 0.5f) { _quartile = Quartile.Mid; _tracker.Track("midpoint", _ad.Metadata, _moment); }
            if (_quartile < Quartile.Third && progress >= 0.75f) { _quartile = Quartile.Third; _tracker.Track("thirdQuartile", _ad.Metadata, _moment); }
        }

        void OnMuteClicked()
        {
            if (_ad == null || _format != AdFormat.Standard) return;
            if (_elapsed < _settings.standardMuteDelaySec) return;
            Mute();
        }

        /// <summary>User mute. Only legal for Standard ads once the mute delay has passed.</summary>
        internal void Mute()
        {
            if (_ad == null) return;
            _tracker.Track("mute", _ad.Metadata, _moment);
            Finish(AdResult.Muted);
        }

        void Finish(AdResult result)
        {
            var ad = _ad;
            var callback = _onFinished;

            if (result == AdResult.Completed)
                _tracker.Track("complete", ad.Metadata, _moment);

            _source.Stop();
            _source.clip = null;
            _overlay.Hide();
            _device.AbandonAudioFocus();

            _ad = null;
            _onFinished = null;
            SoundAdsLog.Info($"Ad '{ad.Metadata.adId}' finished: {result}");
            callback?.Invoke(result);
        }

        void OnApplicationPause(bool paused)
        {
            _appPaused = paused;
            if (_ad == null) return;

            if (paused) _source.Pause();
            else _source.UnPause();
            SoundAdsLog.Info(paused ? "App paused: ad paused" : "App resumed: ad resumed");
        }

        void OnDestroy()
        {
            if (_overlay != null) _overlay.MuteClicked -= OnMuteClicked;
        }
    }
}
