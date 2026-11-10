using System.Threading;
using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>A fully downloaded ad, ready for instant playback.</summary>
    internal sealed class LoadedAd
    {
        public AdResponse Metadata;
        public AudioClip Clip;
    }

    /// <summary>Result of an ad request. Exactly one of <see cref="Ad"/> / <see cref="Error"/> is set.</summary>
    internal readonly struct AdLoadResult
    {
        public readonly LoadedAd Ad;
        public readonly string Error;

        /// <summary>True for transport failures (server unreachable, timeout) where a fallback network may help.</summary>
        public readonly bool IsTransportError;

        AdLoadResult(LoadedAd ad, string error, bool isTransportError)
        {
            Ad = ad;
            Error = error;
            IsTransportError = isTransportError;
        }

        public bool Success => Ad != null;

        public static AdLoadResult Ok(LoadedAd ad) => new(ad, null, false);
        public static AdLoadResult Fail(string error, bool transport = false) => new(null, error, transport);
    }

    /// <summary>
    /// Source of ads. Implementations: <see cref="HttpAdNetwork"/> (real server),
    /// <see cref="MockAdNetwork"/> (offline), <see cref="FallbackAdNetwork"/> (decorator chaining the two).
    /// </summary>
    internal interface IAdNetwork
    {
        string Name { get; }
        Awaitable<AdLoadResult> RequestAdAsync(AdRequest request, CancellationToken cancellationToken);
    }
}
