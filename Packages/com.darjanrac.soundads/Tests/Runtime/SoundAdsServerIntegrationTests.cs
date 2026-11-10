using System.Collections;
using System.Threading;
using NUnit.Framework;
using SoundAds.Internal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace SoundAds.Tests
{
    /// <summary>
    /// Talks to a real SoundAds server (backend/SoundAds.Server, <c>dotnet run</c>).
    /// Ignored automatically when the server isn't running, so CI without a backend stays green.
    /// </summary>
    public class SoundAdsServerIntegrationTests
    {
        const string ServerUrl = "http://localhost:5080";
        const string ApiKey = "test-key";

        static IEnumerator Await<T>(Awaitable<T> awaitable, System.Action<T> result)
        {
            var awaiter = awaitable.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;
            result(awaiter.GetResult());
        }

        [UnitySetUp]
        public IEnumerator RequireServer()
        {
            using var ping = UnityWebRequest.Get(ServerUrl + "/health");
            ping.timeout = 2;
            yield return ping.SendWebRequest();
            if (ping.result != UnityWebRequest.Result.Success)
                Assert.Ignore("SoundAds server not running on " + ServerUrl);
        }

        [UnityTest]
        public IEnumerator HttpNetwork_DownloadsRewardedAdWithAudio()
        {
            var network = new HttpAdNetwork(ServerUrl, ApiKey, 8);
            AdLoadResult result = default;

            yield return Await(network.RequestAdAsync(
                new AdRequest("it_rewarded", AdFormat.Rewarded, AdMoment.Reward, false), CancellationToken.None),
                r => result = r);

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual("rewarded", result.Ad.Metadata.format);
            Assert.IsNotNull(result.Ad.Clip, "audio creative should be downloaded before OnAdLoaded");
            Assert.Greater(result.Ad.Clip.length, 5f);
        }

        [UnityTest]
        public IEnumerator HttpNetwork_BadApiKey_IsAuthErrorNotTransportError()
        {
            var network = new HttpAdNetwork(ServerUrl, "wrong-key", 8);
            AdLoadResult result = default;

            yield return Await(network.RequestAdAsync(
                new AdRequest("it_bad", AdFormat.Standard, AdMoment.Win, false), CancellationToken.None),
                r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Http401", result.Error);
            Assert.IsFalse(result.IsTransportError, "auth errors must not trigger the offline fallback");
        }

        [UnityTest]
        public IEnumerator EventTracker_UploadsToServer_AndShowsInReport()
        {
            string adId = "it-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var tracker = new EventTracker(new HttpEventTransport(ServerUrl, ApiKey, 8), 1f);
            var ad = new AdResponse { adId = adId, placementId = "it_events" };

            tracker.Track("impression", ad, AdMoment.Win);
            tracker.Track("complete", ad, AdMoment.Win);

            var flush = tracker.FlushAsync(0f).GetAwaiter();
            while (!flush.IsCompleted) yield return null;
            Assert.AreEqual(0, tracker.PendingCount, "upload should have succeeded");

            using var report = UnityWebRequest.Get(ServerUrl + "/v1/report");
            yield return report.SendWebRequest();
            StringAssert.Contains(adId, report.downloadHandler.text);
        }
    }
}
