using System.Threading;
using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>
    /// Decorator: asks the primary network first and only consults the fallback when the primary
    /// failed at the transport level (offline, timeout). A genuine "no fill" is respected.
    /// </summary>
    internal sealed class FallbackAdNetwork : IAdNetwork
    {
        readonly IAdNetwork _primary;
        readonly IAdNetwork _fallback;

        public FallbackAdNetwork(IAdNetwork primary, IAdNetwork fallback)
        {
            _primary = primary;
            _fallback = fallback;
        }

        public string Name => $"{_primary.Name}+{_fallback.Name}";

        public async Awaitable<AdLoadResult> RequestAdAsync(AdRequest request, CancellationToken cancellationToken)
        {
            var result = await _primary.RequestAdAsync(request, cancellationToken);
            if (result.Success || !result.IsTransportError)
                return result;

            SoundAdsLog.Warn($"{_primary.Name} unreachable for {request.PlacementId}; using {_fallback.Name} ads");
            return await _fallback.RequestAdAsync(request, cancellationToken);
        }
    }
}
