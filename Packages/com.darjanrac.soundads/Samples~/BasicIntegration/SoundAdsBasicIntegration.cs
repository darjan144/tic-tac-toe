using SoundAds;
using UnityEngine;

/// <summary>
/// Minimal SoundAds integration. Add to any GameObject in a scene with an EventSystem,
/// press Play, then use the on-screen buttons.
/// </summary>
public class SoundAdsBasicIntegration : MonoBehaviour
{
    const string RewardedPlacement = "sample_rewarded";
    const string StandardPlacement = "sample_standard";

    [Tooltip("Coins granted per completed rewarded ad.")]
    [SerializeField] int _rewardCoins = 50;

    int _coins;
    string _status = "Initializing...";

    void Start()
    {
        // 1. Subscribe before initialising so no callback is missed.
        SoundAdsSdk.OnAdLoaded += placement => _status = $"Loaded: {placement}";
        SoundAdsSdk.OnAdFailed += (placement, reason) => _status = $"Failed: {placement} ({reason})";
        SoundAdsSdk.OnAdStarted += _ => Debug.Log("Ad started: pause your game music here");
        SoundAdsSdk.OnRewardGranted += _ => _coins += _rewardCoins;      // grant the reward
        SoundAdsSdk.OnAdFinished += (placement, result) => _status = $"Finished: {placement} -> {result}";

        // 2. Consent from your CMP (GDPR/TCF). false = non-personalised ads.
        SoundAdsSdk.SetConsent(false);

        // 3. Initialise (API key from Project Settings > SoundAds), then preload.
        SoundAdsSdk.Initialize(ok =>
        {
            _status = ok ? $"Ready ({SoundAdsSdk.ActiveNetwork})" : "Init failed";
            SoundAdsSdk.LoadAd(RewardedPlacement, AdFormat.Rewarded, AdMoment.Reward);
            SoundAdsSdk.LoadAd(StandardPlacement, AdFormat.Standard, AdMoment.Win);
        });
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 220, 420, 400));
        GUILayout.Label($"Coins: {_coins}");
        GUILayout.Label(_status);

        if (GUILayout.Button(SoundAdsSdk.IsAdReady(RewardedPlacement) ? "Listen for coins" : "Load rewarded"))
        {
            if (SoundAdsSdk.IsAdReady(RewardedPlacement)) SoundAdsSdk.ShowAd(RewardedPlacement);
            else SoundAdsSdk.LoadAd(RewardedPlacement, AdFormat.Rewarded, AdMoment.Reward);
        }

        if (GUILayout.Button(SoundAdsSdk.IsAdReady(StandardPlacement) ? "Play standard ad" : "Load standard"))
        {
            if (SoundAdsSdk.IsAdReady(StandardPlacement)) SoundAdsSdk.ShowAd(StandardPlacement);
            else SoundAdsSdk.LoadAd(StandardPlacement, AdFormat.Standard, AdMoment.Win);
        }

        GUILayout.EndArea();
    }
}
