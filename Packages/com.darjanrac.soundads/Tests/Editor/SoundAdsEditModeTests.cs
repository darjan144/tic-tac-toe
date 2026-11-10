using System.Threading;
using NUnit.Framework;
using SoundAds.Internal;
using UnityEngine;

namespace SoundAds.Tests
{
    public class WireFormatTests
    {
        [TestCase(AdFormat.Standard, "standard")]
        [TestCase(AdFormat.Rewarded, "rewarded")]
        public void Format_MapsToWire(AdFormat format, string expected) =>
            Assert.AreEqual(expected, format.ToWire());

        [TestCase(AdMoment.None, "none")]
        [TestCase(AdMoment.Win, "win")]
        [TestCase(AdMoment.Draw, "draw")]
        [TestCase(AdMoment.Loss, "loss")]
        [TestCase(AdMoment.Reward, "reward")]
        public void Moment_MapsToWire(AdMoment moment, string expected) =>
            Assert.AreEqual(expected, moment.ToWire());

        [Test]
        public void AdResponse_ParsesServerJson()
        {
            const string json = "{\"adId\":\"a1\",\"placementId\":\"p\",\"format\":\"rewarded\"," +
                                "\"audioUrl\":\"http://x/a.wav\",\"durationSec\":12.5,\"advertiser\":\"Nebula\"," +
                                "\"title\":\"T\",\"clickUrl\":\"\",\"targetMoments\":[\"win\",\"reward\"]}";

            var ad = JsonUtility.FromJson<AdResponse>(json);

            Assert.AreEqual("a1", ad.adId);
            Assert.AreEqual("rewarded", ad.format);
            Assert.AreEqual(12.5f, ad.durationSec);
            CollectionAssert.AreEqual(new[] { "win", "reward" }, ad.targetMoments);
        }
    }

    public class EventTrackerTests
    {
        static readonly AdResponse Ad = new() { adId = "ad-1", placementId = "p1" };

        [Test]
        public void Flush_SendsQueuedEventsAsOneBatch()
        {
            var transport = new CapturingTransport();
            var tracker = new EventTracker(transport, 5f) { SessionId = "s1" };

            tracker.Track("impression", Ad, AdMoment.Win);
            tracker.Track("complete", Ad, AdMoment.Win);
            _ = tracker.FlushAsync(0f);

            Assert.AreEqual(0, tracker.PendingCount);
            Assert.AreEqual(1, transport.Sent.Count);
            StringAssert.Contains("\"type\":\"impression\"", transport.Sent[0]);
            StringAssert.Contains("\"type\":\"complete\"", transport.Sent[0]);
            StringAssert.Contains("\"moment\":\"win\"", transport.Sent[0]);
            StringAssert.Contains("\"sessionId\":\"s1\"", transport.Sent[0]);
        }

        [Test]
        public void FailedUpload_IsRequeued_AndBacksOff()
        {
            var transport = new CapturingTransport { Succeed = false };
            var tracker = new EventTracker(transport, 5f);

            tracker.Track("impression", Ad, AdMoment.None);
            _ = tracker.FlushAsync(0f);
            Assert.AreEqual(1, tracker.PendingCount, "event must survive a failed upload");

            transport.Succeed = true;
            tracker.Tick(1f); // still inside the backoff window (5s * 2^1 = 10s)
            Assert.AreEqual(0, transport.Sent.Count);

            tracker.Tick(11f);
            Assert.AreEqual(1, transport.Sent.Count);
            Assert.AreEqual(0, tracker.PendingCount);
        }

        [Test]
        public void Queue_IsCapped_DroppingOldest()
        {
            var tracker = new EventTracker(new CapturingTransport(), 5f);
            for (int i = 0; i < EventTracker.MaxQueueSize + 25; i++)
                tracker.Track("impression", Ad, AdMoment.None);

            Assert.AreEqual(EventTracker.MaxQueueSize, tracker.PendingCount);
        }

        [Test]
        public void WithoutConsent_SessionIdIsEmpty()
        {
            var transport = new CapturingTransport();
            var tracker = new EventTracker(transport, 5f) { SessionId = string.Empty };

            tracker.Track("impression", Ad, AdMoment.None);
            _ = tracker.FlushAsync(0f);

            StringAssert.Contains("\"sessionId\":\"\"", transport.Sent[0]);
        }
    }

    public class AdNetworkTests
    {
        static AdLoadResult Request(IAdNetwork network, AdFormat format, AdMoment moment)
        {
            var awaitable = network.RequestAdAsync(new AdRequest("p", format, moment, false), CancellationToken.None);
            return awaitable.GetAwaiter().GetResult(); // zero-latency networks complete synchronously
        }

        [Test]
        public void Mock_ReturnsRequestedFormat_WithPlayableClip()
        {
            var result = Request(new MockAdNetwork(latencySec: 0f, durationOverrideSec: 0.5f, seed: 1),
                                 AdFormat.Rewarded, AdMoment.Reward);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("rewarded", result.Ad.Metadata.format);
            Assert.AreEqual("p", result.Ad.Metadata.placementId);
            Assert.IsNotNull(result.Ad.Clip);
            Assert.AreEqual(0.5f, result.Ad.Clip.length, 0.01f);
        }

        [Test]
        public void Mock_ZeroFillRate_ReturnsNoFill()
        {
            var result = Request(new MockAdNetwork(latencySec: 0f, fillRate: 0f), AdFormat.Standard, AdMoment.Win);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AdFailureReason.NoFill, result.Error);
            Assert.IsFalse(result.IsTransportError);
        }

        [Test]
        public void Fallback_UsedOnlyForTransportErrors()
        {
            var offline = new StubNetwork(AdLoadResult.Fail(AdFailureReason.NetworkError, transport: true));
            var fallback = new StubNetwork(AdLoadResult.Ok(new LoadedAd { Metadata = new AdResponse { adId = "fb" } }));

            var result = Request(new FallbackAdNetwork(offline, fallback), AdFormat.Standard, AdMoment.None);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("fb", result.Ad.Metadata.adId);
            Assert.AreEqual(1, fallback.Calls);
        }

        [Test]
        public void Fallback_RespectsGenuineNoFill()
        {
            var noFill = new StubNetwork(AdLoadResult.Fail(AdFailureReason.NoFill));
            var fallback = new StubNetwork(AdLoadResult.Ok(new LoadedAd { Metadata = new AdResponse() }));

            var result = Request(new FallbackAdNetwork(noFill, fallback), AdFormat.Standard, AdMoment.None);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, fallback.Calls);
        }
    }

    public class SettingsTests
    {
        [Test]
        public void LoadOrDefault_NeverReturnsNull_AndHasSaneDefaults()
        {
            var settings = SoundAdsSettings.LoadOrDefault();

            Assert.IsNotNull(settings);
            Assert.IsFalse(string.IsNullOrEmpty(settings.serverUrl));
            Assert.GreaterOrEqual(settings.standardMuteDelaySec, 0f);
            Assert.That(settings.lowVolumeThreshold, Is.InRange(0f, 1f));
        }
    }
}
