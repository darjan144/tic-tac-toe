using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>
    /// Offline ad source used in test mode and as a fallback when the server is unreachable.
    /// Ad audio is synthesised procedurally (a short jingle), so the SDK ships no audio assets
    /// and works in the Editor, WebGL and on devices without connectivity.
    /// </summary>
    internal sealed class MockAdNetwork : IAdNetwork
    {
        readonly float _latencySec;
        readonly float _fillRate;
        readonly float _durationOverrideSec;
        readonly System.Random _random;

        static readonly AdResponse[] Inventory =
        {
            new() { adId = "mock-nebula", advertiser = "Nebula Juice", title = "Taste the galaxy", format = "standard",
                    durationSec = 12f, targetMoments = new[] { "win", "reward" } },
            new() { adId = "mock-pixel", advertiser = "Pixel Mobile", title = "Unlimited data, unlimited play", format = "standard",
                    durationSec = 12f, targetMoments = Array.Empty<string>() },
            new() { adId = "mock-comfort", advertiser = "Cozy Socks Co.", title = "Better luck next time — in comfort", format = "standard",
                    durationSec = 10f, targetMoments = new[] { "draw", "loss" } },
            new() { adId = "mock-reward", advertiser = "Starlight Coffee", title = "Your reward is brewing", format = "rewarded",
                    durationSec = 8f, targetMoments = Array.Empty<string>() },
        };

        /// <param name="latencySec">Simulated network latency.</param>
        /// <param name="fillRate">Probability (0-1) that a request is filled.</param>
        /// <param name="durationOverrideSec">If &gt; 0, every ad uses this duration (handy for fast tests).</param>
        /// <param name="seed">Optional RNG seed for deterministic tests.</param>
        public MockAdNetwork(float latencySec = 0.3f, float fillRate = 1f, float durationOverrideSec = 0f, int? seed = null)
        {
            _latencySec = latencySec;
            _fillRate = Mathf.Clamp01(fillRate);
            _durationOverrideSec = durationOverrideSec;
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public string Name => "Mock";

        public async Awaitable<AdLoadResult> RequestAdAsync(AdRequest request, CancellationToken cancellationToken)
        {
            if (_latencySec > 0f)
                await Awaitable.WaitForSecondsAsync(_latencySec, cancellationToken);

            if (_random.NextDouble() >= _fillRate)
                return AdLoadResult.Fail(AdFailureReason.NoFill);

            var candidates = SelectCandidates(request);
            if (candidates.Count == 0)
                return AdLoadResult.Fail(AdFailureReason.NoFill);

            var template = candidates[_random.Next(candidates.Count)];
            float duration = _durationOverrideSec > 0f ? _durationOverrideSec : template.durationSec;

            var metadata = new AdResponse
            {
                adId = template.adId,
                placementId = request.PlacementId,
                format = template.format,
                audioUrl = "mock://" + template.adId,
                durationSec = duration,
                advertiser = template.advertiser,
                title = template.title,
                clickUrl = string.Empty,
                targetMoments = template.targetMoments
            };

            return AdLoadResult.Ok(new LoadedAd
            {
                Metadata = metadata,
                Clip = JingleSynth.Create(template.adId, duration, template.adId.GetHashCode())
            });
        }

        static List<AdResponse> SelectCandidates(AdRequest request)
        {
            string format = request.Format.ToWire();
            string moment = request.Moment.ToWire();
            var result = new List<AdResponse>();

            foreach (var ad in Inventory)
            {
                if (ad.format != format) continue;
                if (ad.targetMoments.Length == 0 || Array.IndexOf(ad.targetMoments, moment) >= 0)
                    result.Add(ad);
            }

            return result;
        }
    }

    /// <summary>Generates a pleasant looping arpeggio so offline ads are audible without bundled assets.</summary>
    internal static class JingleSynth
    {
        const int SampleRate = 22050;
        static readonly float[] Scale = { 261.63f, 329.63f, 392.00f, 523.25f, 392.00f, 329.63f }; // C major arpeggio

        public static AudioClip Create(string name, float durationSec, int seed)
        {
            int totalSamples = Mathf.Max(1, Mathf.CeilToInt(durationSec * SampleRate));
            var data = new float[totalSamples];

            const float noteSec = 0.25f;
            int samplesPerNote = Mathf.CeilToInt(noteSec * SampleRate);
            int offset = (seed & int.MaxValue) % Scale.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                int noteIndex = (i / samplesPerNote + offset) % Scale.Length;
                float t = (i % samplesPerNote) / (float)SampleRate;
                float envelope = Mathf.Exp(-6f * t);                    // plucky decay per note
                float freq = Scale[noteIndex];
                float sample = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f
                             + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.15f; // a little overtone
                data[i] = sample * envelope * 0.35f;
            }

            var clip = AudioClip.Create("SoundAdsMock_" + name, totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Converts SDK enums to the lowercase strings used on the wire.</summary>
    internal static class WireFormat
    {
        public static string ToWire(this AdFormat format) => format == AdFormat.Rewarded ? "rewarded" : "standard";

        public static string ToWire(this AdMoment moment) => moment switch
        {
            AdMoment.Win => "win",
            AdMoment.Draw => "draw",
            AdMoment.Loss => "loss",
            AdMoment.Reward => "reward",
            _ => "none"
        };
    }
}
