using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SoundAds.Internal;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoundAds.Tests
{
    /// <summary>
    /// End-to-end SDK flows against the offline mock network with short ads,
    /// exercising the public API exactly as a publisher would.
    /// </summary>
    public class SoundAdsPlayModeTests
    {
        const string Placement = "test_placement";
        const float AdLength = 0.6f;

        SoundAdsSettings _settings;
        float _simulatedVolume;
        readonly List<string> _log = new();

        sealed class NullTransport : IEventTransport
        {
            public readonly List<string> Sent = new();

            public Awaitable<bool> SendAsync(string json)
            {
                Sent.Add(json);
                var source = new AwaitableCompletionSource<bool>();
                source.SetResult(true);
                return source.Awaitable;
            }
        }

        NullTransport _transport;

        [SetUp]
        public void SetUp()
        {
            SoundAdsSdk.ResetForTests();

            _settings = ScriptableObject.CreateInstance<SoundAdsSettings>();
            _settings.standardMuteDelaySec = 0.3f;
            _settings.eventFlushIntervalSec = 1f;
            _simulatedVolume = 1f;
            _transport = new NullTransport();
            _log.Clear();

            SoundAdsSdk.Bootstrap(
                new MockAdNetwork(latencySec: 0.05f, durationOverrideSec: AdLength, seed: 42),
                _transport,
                new SimulatedDeviceAudio(() => _simulatedVolume),
                _settings);

            SoundAdsSdk.OnAdLoaded += p => _log.Add("loaded");
            SoundAdsSdk.OnAdFailed += (p, reason) => _log.Add("failed:" + reason);
            SoundAdsSdk.OnAdStarted += p => _log.Add("started");
            SoundAdsSdk.OnRewardGranted += p => _log.Add("reward");
            SoundAdsSdk.OnAdFinished += (p, result) => _log.Add("finished:" + result);
        }

        [TearDown]
        public void TearDown()
        {
            SoundAdsSdk.ResetForTests();
            Object.Destroy(_settings);
        }

        IEnumerator LoadAndWait(AdFormat format)
        {
            SoundAdsSdk.LoadAd(Placement, format, AdMoment.Win);
            float timeout = Time.realtimeSinceStartup + 3f;
            while (!SoundAdsSdk.IsAdReady(Placement) && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.IsTrue(SoundAdsSdk.IsAdReady(Placement), "ad did not load");
        }

        IEnumerator WaitWhileShowing()
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (SoundAdsSdk.IsShowingAd && Time.realtimeSinceStartup < timeout)
                yield return null;
        }

        [UnityTest]
        public IEnumerator RewardedAd_Completes_GrantsReward_BeforeFinished()
        {
            yield return LoadAndWait(AdFormat.Rewarded);

            SoundAdsSdk.ShowAd(Placement);
            Assert.IsTrue(SoundAdsSdk.IsShowingAd);
            Assert.IsTrue(SoundAdsSdk.Player.Overlay.IsVisible);

            yield return WaitWhileShowing();

            CollectionAssert.AreEqual(new[] { "loaded", "started", "reward", "finished:Completed" }, _log);
            Assert.IsFalse(SoundAdsSdk.IsAdReady(Placement), "an ad is single-use");
            Assert.IsFalse(SoundAdsSdk.Player.Overlay.IsVisible);
        }

        [UnityTest]
        public IEnumerator StandardAd_MuteIgnoredBeforeDelay_ThenMutes_WithoutReward()
        {
            yield return LoadAndWait(AdFormat.Standard);
            SoundAdsSdk.ShowAd(Placement);

            SoundAdsSdk.Player.Overlay.MuteButton.onClick.Invoke(); // too early
            Assert.IsTrue(SoundAdsSdk.IsShowingAd, "mute must be locked during the delay");

            yield return new WaitForSecondsRealtime(_settings.standardMuteDelaySec + 0.05f);
            SoundAdsSdk.Player.Overlay.MuteButton.onClick.Invoke();

            Assert.IsFalse(SoundAdsSdk.IsShowingAd);
            CollectionAssert.AreEqual(new[] { "loaded", "started", "finished:Muted" }, _log);
        }

        [UnityTest]
        public IEnumerator ShowAd_WhenNotLoaded_FinishesExactlyOnceWithFailed()
        {
            SoundAdsSdk.ShowAd(Placement);
            yield return null;

            CollectionAssert.AreEqual(new[] { "failed:" + AdFailureReason.NotReady, "finished:Failed" }, _log);
        }

        [UnityTest]
        public IEnumerator MutedDevice_BlocksShow_AndKeepsAdLoaded()
        {
            yield return LoadAndWait(AdFormat.Rewarded);
            _simulatedVolume = 0f;

            SoundAdsSdk.ShowAd(Placement);

            Assert.IsFalse(SoundAdsSdk.IsShowingAd);
            Assert.IsTrue(SoundAdsSdk.IsAdReady(Placement), "ad should still be usable once volume is up");
            CollectionAssert.Contains(_log, "failed:" + AdFailureReason.DeviceMuted);
            CollectionAssert.Contains(_log, "finished:Failed");
        }

        [UnityTest]
        public IEnumerator CompletedAd_UploadsImpressionQuartilesAndComplete()
        {
            yield return LoadAndWait(AdFormat.Standard);
            SoundAdsSdk.ShowAd(Placement);
            yield return WaitWhileShowing();
            yield return new WaitForSecondsRealtime(_settings.eventFlushIntervalSec + 0.2f);

            string all = string.Join("\n", _transport.Sent);
            foreach (var type in new[] { "impression", "start", "firstQuartile", "midpoint", "thirdQuartile", "complete" })
                StringAssert.Contains($"\"type\":\"{type}\"", all);
        }

        [UnityTest]
        public IEnumerator PublisherException_DoesNotBreakSdk()
        {
            SoundAdsSdk.OnAdLoaded += _ => throw new System.InvalidOperationException("bad publisher code");
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("bad publisher code"));

            yield return LoadAndWait(AdFormat.Rewarded);
            SoundAdsSdk.ShowAd(Placement);
            yield return WaitWhileShowing();

            CollectionAssert.Contains(_log, "finished:Completed");
        }
    }
}
