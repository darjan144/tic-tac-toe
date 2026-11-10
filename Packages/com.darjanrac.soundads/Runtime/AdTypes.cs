using System;

namespace SoundAds
{
    /// <summary>Ad format requested for a placement.</summary>
    public enum AdFormat
    {
        /// <summary>Plays with an on-screen countdown; the user may mute it after a short delay (default 5 s).</summary>
        Standard,

        /// <summary>The user opts in and must listen to the end; <see cref="SoundAdsSdk.OnRewardGranted"/> fires on completion.</summary>
        Rewarded
    }

    /// <summary>
    /// The game moment an ad is shown in. Advertisers can target positive or negative moments,
    /// so tagging requests accurately improves fill and eCPM.
    /// </summary>
    public enum AdMoment
    {
        None,
        Win,
        Draw,
        Loss,
        Reward
    }

    /// <summary>How an ad playback ended. Reported exactly once per <see cref="SoundAdsSdk.ShowAd"/> call.</summary>
    public enum AdResult
    {
        /// <summary>The ad played to the end.</summary>
        Completed,

        /// <summary>The user muted a Standard ad before it finished.</summary>
        Muted,

        /// <summary>The ad could not be shown (not loaded, device muted, already showing, ...).</summary>
        Failed
    }

    /// <summary>Well-known failure reasons passed to <see cref="SoundAdsSdk.OnAdFailed"/>.</summary>
    public static class AdFailureReason
    {
        public const string NotInitialized = "NotInitialized";
        public const string NoFill = "NoFill";
        public const string NetworkError = "NetworkError";
        public const string NotReady = "NotReady";
        public const string AlreadyShowing = "AlreadyShowing";
        public const string AlreadyLoading = "AlreadyLoading";
        public const string DeviceMuted = "DeviceMuted";
        public const string InvalidPlacement = "InvalidPlacement";
    }

    /// <summary>Ad metadata as returned by the ad server. Field names match the server's JSON contract.</summary>
    [Serializable]
    public class AdResponse
    {
        public string adId;
        public string placementId;
        public string format;
        public string audioUrl;
        public float durationSec;
        public string advertiser;
        public string title;
        public string clickUrl;
        public string[] targetMoments;
    }
}
