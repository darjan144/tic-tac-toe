using System;
using SoundAds;
using UnityEngine;

/// <summary>
/// The game's single integration point with the SoundAds SDK (publisher side).
/// Initialises the SDK, pauses game music while ads play, and offers one-shot helpers
/// so gameplay code never has to manage SDK event subscriptions itself.
/// </summary>
public static class GameAds
{
    // Frequency cap: a between-games ad is shown on every Nth Retry.
    const int RetriesPerBetweenGamesAd = 2;

    static int _retriesSinceAd;

    // Runs automatically after the first scene loads, so no scene/prefab wiring is needed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        _retriesSinceAd = 0;

        SoundAdsSdk.OnAdStarted += HandleAdStarted;
        SoundAdsSdk.OnAdFinished += HandleAdFinished;
        SoundAdsSdk.OnAdFailed += (placement, reason) => Debug.Log($"[Ads] '{placement}' failed: {reason}");

        SoundAdsSdk.SetConsent(SaveManager.Instance != null && SaveManager.Instance.AdsConsent);
        SoundAdsSdk.Initialize(ok =>
        {
            Debug.Log($"[Ads] SDK initialised: {ok} (network: {SoundAdsSdk.ActiveNetwork})");
            if (ok) Preload(AdPlacements.ExtraUndo, AdFormat.Rewarded, AdMoment.Reward);
        });
    }

    static void HandleAdStarted(string placementId)
    {
        Debug.Log($"[Ads] '{placementId}' started: pausing game music");
        AudioManager.Instance?.PauseBGMForAd();
    }

    static void HandleAdFinished(string placementId, AdResult result)
    {
        Debug.Log($"[Ads] '{placementId}' finished: {result}");
        AudioManager.Instance?.ResumeBGMAfterAd();
    }

    /// <summary>Request an ad in the background so it can be shown instantly later.</summary>
    public static void Preload(string placementId, AdFormat format, AdMoment moment)
    {
        if (!SoundAdsSdk.IsAdReady(placementId))
            SoundAdsSdk.LoadAd(placementId, format, moment);
    }

    /// <summary>
    /// Call on every Retry. Returns true when the frequency cap says a between-games ad is due
    /// and one is loaded.
    /// </summary>
    public static bool ShouldShowBetweenGamesAd()
    {
        _retriesSinceAd++;
        if (_retriesSinceAd < RetriesPerBetweenGamesAd) return false;
        if (!SoundAdsSdk.IsAdReady(AdPlacements.BetweenGames)) return false;

        _retriesSinceAd = 0;
        return true;
    }

    /// <summary>
    /// Show an ad and get exactly one completion callback. If the ad isn't loaded yet it is
    /// requested first. <paramref name="onReward"/> fires only for completed Rewarded ads.
    /// </summary>
    public static void Show(string placementId, AdFormat format, AdMoment moment,
                            Action<AdResult> onFinished, Action onReward = null)
    {
        if (SoundAdsSdk.IsAdReady(placementId))
        {
            ShowLoaded(placementId, onFinished, onReward);
            return;
        }

        Action<string> loaded = null;
        Action<string, string> failed = null;

        void Unsubscribe()
        {
            SoundAdsSdk.OnAdLoaded -= loaded;
            SoundAdsSdk.OnAdFailed -= failed;
        }

        loaded = p =>
        {
            if (p != placementId) return;
            Unsubscribe();
            ShowLoaded(placementId, onFinished, onReward);
        };
        failed = (p, _) =>
        {
            if (p != placementId) return;
            Unsubscribe();
            onFinished?.Invoke(AdResult.Failed);
        };

        SoundAdsSdk.OnAdLoaded += loaded;
        SoundAdsSdk.OnAdFailed += failed;
        SoundAdsSdk.LoadAd(placementId, format, moment);
    }

    static void ShowLoaded(string placementId, Action<AdResult> onFinished, Action onReward)
    {
        Action<string> rewarded = null;
        Action<string, AdResult> finished = null;

        rewarded = p =>
        {
            if (p == placementId) onReward?.Invoke();
        };
        finished = (p, result) =>
        {
            if (p != placementId) return;
            SoundAdsSdk.OnRewardGranted -= rewarded;
            SoundAdsSdk.OnAdFinished -= finished;
            onFinished?.Invoke(result);
        };

        SoundAdsSdk.OnRewardGranted += rewarded;
        SoundAdsSdk.OnAdFinished += finished;
        SoundAdsSdk.ShowAd(placementId);
    }

    /// <summary>Maps a finished game to the ad moment advertisers target.</summary>
    public static AdMoment MomentFor(Player winner) =>
        winner == Player.None ? AdMoment.Draw : AdMoment.Win;
}
