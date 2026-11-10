using System;
using UnityEngine;

namespace SoundAds.Internal
{
    /// <summary>
    /// Platform audio facts the SDK needs before playing an ad. The Android implementation talks to
    /// the Kotlin bridge (<c>com.darjanrac.soundads.DeviceAudioBridge</c>); other platforms are simulated.
    /// </summary>
    internal interface IDeviceAudio
    {
        string Name { get; }

        /// <summary>Media stream volume, 0-1.</summary>
        float GetVolume01();

        /// <summary>True when the user would not hear an ad (volume 0 or silent mode).</summary>
        bool IsMuted();

        /// <summary>Ask other apps (music players, podcasts) to pause for the duration of the ad.</summary>
        void RequestAudioFocus();

        void AbandonAudioFocus();
    }

    internal static class DeviceAudioFactory
    {
        public static IDeviceAudio Create(SoundAdsSettings settings)
        {
            if (Application.platform == RuntimePlatform.Android &&
                AndroidDeviceAudio.TryCreate(out var android))
                return android;

            return new SimulatedDeviceAudio(() => settings.editorSimulatedVolume);
        }
    }

    /// <summary>Editor / WebGL / iOS stand-in. Volume comes from <see cref="SoundAdsSettings.editorSimulatedVolume"/>.</summary>
    internal sealed class SimulatedDeviceAudio : IDeviceAudio
    {
        readonly Func<float> _volume;

        public SimulatedDeviceAudio(Func<float> volume) => _volume = volume;

        public string Name => "Simulated";
        public float GetVolume01() => Mathf.Clamp01(_volume());
        public bool IsMuted() => GetVolume01() <= 0f;
        public void RequestAudioFocus() => SoundAdsLog.Info("Audio focus requested (simulated)");
        public void AbandonAudioFocus() => SoundAdsLog.Info("Audio focus released (simulated)");
    }

    /// <summary>JNI bridge to the Kotlin <c>DeviceAudioBridge</c> object shipped in <c>Plugins/Android/soundads-bridge.aar</c>.</summary>
    internal sealed class AndroidDeviceAudio : IDeviceAudio
    {
        const string BridgeClass = "com.darjanrac.soundads.DeviceAudioBridge";

        readonly AndroidJavaClass _bridge;
        readonly AndroidJavaObject _activity;

        AndroidDeviceAudio(AndroidJavaClass bridge, AndroidJavaObject activity)
        {
            _bridge = bridge;
            _activity = activity;
        }

        public string Name => "Android";

        public static bool TryCreate(out IDeviceAudio deviceAudio)
        {
            deviceAudio = null;
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var bridge = new AndroidJavaClass(BridgeClass);
                deviceAudio = new AndroidDeviceAudio(bridge, activity);
                SoundAdsLog.Info($"Native Android bridge connected (volume={deviceAudio.GetVolume01():0.00})");
                return true;
            }
            catch (Exception e)
            {
                SoundAdsLog.Warn($"Android bridge unavailable, using simulated device audio: {e.Message}");
                return false;
            }
        }

        public float GetVolume01() => Call(() => _bridge.CallStatic<float>("getVolume01", _activity), 1f);
        public bool IsMuted() => Call(() => _bridge.CallStatic<bool>("isMuted", _activity), false);
        public void RequestAudioFocus() => Call(() => _bridge.CallStatic<bool>("requestAudioFocus", _activity), false);
        public void AbandonAudioFocus() => Call(() => { _bridge.CallStatic("abandonAudioFocus", _activity); return true; }, false);

        // A native failure must never crash the host game: log and return a safe default.
        static T Call<T>(Func<T> call, T fallback)
        {
            try { return call(); }
            catch (Exception e)
            {
                SoundAdsLog.Warn($"Native call failed: {e.Message}");
                return fallback;
            }
        }
    }
}
