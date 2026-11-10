# SoundAds Android bridge (Kotlin)

The native half of the SoundAds Unity SDK. It is one Kotlin `object`, `com.darjanrac.soundads.DeviceAudioBridge`, and does two jobs:

- **Volume gate**: it reports whether the device is muted or silent, and the current media volume. Ads aren't played to a muted device.
- **Audio focus**: during an ad it takes `AUDIOFOCUS_GAIN_TRANSIENT`, so Spotify, podcast apps and similar pause. They resume automatically when the ad ends.

## Build

You need JDK 17 and the Android SDK (compileSdk 34). The easiest way to get both is Android Studio: open this folder (`native/android`) and let it sync. Alternatively, install Unity's **Android Build Support** module, which bundles both under `Editor/Data/PlaybackEngines/AndroidPlayer`, and set `JAVA_HOME` and `ANDROID_HOME` to point at them.

The Gradle wrapper isn't committed yet. Generate it once with `gradle wrapper --gradle-version 8.7`, or let Android Studio create it. Then build and copy the AAR into the Unity package:

```bash
./gradlew copyAarToUnity
```

The AAR lands in `Packages/com.darjanrac.soundads/Plugins/Android/soundads-bridge.aar`. In Unity, select it and make sure only the **Android** platform is ticked.

## C# side

`Runtime/Internal/DeviceAudio.cs` → `AndroidDeviceAudio`:

```csharp
var bridge = new AndroidJavaClass("com.darjanrac.soundads.DeviceAudioBridge");
float volume = bridge.CallStatic<float>("getVolume01", activity);
bool muted   = bridge.CallStatic<bool>("isMuted", activity);
bool granted = bridge.CallStatic<bool>("requestAudioFocus", activity);
bridge.CallStatic("abandonAudioFocus", activity);
```

The C# side falls back to simulated device audio in three cases: the AAR is missing, a JNI call throws, or the platform isn't Android (Editor, WebGL, iOS). Native problems never crash the game.
