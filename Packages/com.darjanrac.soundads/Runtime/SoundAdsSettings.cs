using UnityEngine;

namespace SoundAds
{
    /// <summary>
    /// Project-wide SDK configuration. Create it via <b>Tools > SoundAds > Create Settings</b> or
    /// <b>Project Settings > SoundAds</b>; it lives at <c>Assets/Resources/SoundAdsSettings.asset</c>
    /// so it can be loaded at runtime without a scene reference.
    /// </summary>
    public class SoundAdsSettings : ScriptableObject
    {
        public const string ResourcePath = "SoundAdsSettings";

        [Header("Account")]
        [Tooltip("Publisher API key issued by the SoundAds dashboard.")]
        public string apiKey = "test-key";

        [Tooltip("Base URL of the ad server, without a trailing slash.")]
        public string serverUrl = "http://localhost:5080";

        [Header("Behaviour")]
        [Tooltip("Serve built-in offline test ads only. No network traffic is made.")]
        public bool testMode;

        [Tooltip("If the server can't be reached, fall back to built-in offline test ads instead of failing.")]
        public bool fallbackToOfflineAds = true;

        [Tooltip("Seconds before a Standard ad's Mute button becomes available.")]
        [Min(0f)] public float standardMuteDelaySec = 5f;

        [Tooltip("Rewarded ads show a 'turn up your volume' hint below this device volume (0-1).")]
        [Range(0f, 1f)] public float lowVolumeThreshold = 0.3f;

        [Tooltip("Timeout for ad and audio requests, in seconds.")]
        [Min(1)] public int requestTimeoutSec = 8;

        [Tooltip("Seconds between event batch uploads.")]
        [Min(1f)] public float eventFlushIntervalSec = 5f;

        [Header("Debug")]
        [Tooltip("Log every SDK lifecycle step to the Console with a [SoundAds] prefix.")]
        public bool verboseLogging = true;

        [Tooltip("Editor / non-native platforms only: simulated device volume (0 = muted).")]
        [Range(0f, 1f)] public float editorSimulatedVolume = 1f;

        /// <summary>Loads the settings asset, or returns in-memory defaults if none exists.</summary>
        public static SoundAdsSettings LoadOrDefault()
        {
            var settings = Resources.Load<SoundAdsSettings>(ResourcePath);
            if (settings != null) return settings;

            settings = CreateInstance<SoundAdsSettings>();
            settings.name = "SoundAdsSettings (defaults)";
            return settings;
        }
    }
}
