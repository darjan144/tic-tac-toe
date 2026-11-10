using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>Single logging funnel so every SDK message is prefixed and verbosity is controllable.</summary>
    internal static class SoundAdsLog
    {
        const string Prefix = "[SoundAds] ";

        internal static bool Verbose { get; set; } = true;

        internal static void Info(string message)
        {
            if (Verbose) Debug.Log(Prefix + message);
        }

        internal static void Warn(string message) => Debug.LogWarning(Prefix + message);

        internal static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
