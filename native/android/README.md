# SoundAds Android bridge (Kotlin)

The native half of the SoundAds Unity SDK. It is one Kotlin `object`, `com.darjanrac.soundads.DeviceAudioBridge`, and does two jobs:

- **Volume gate**: it reports whether the device is muted or silent, and the current media volume. Ads aren't played to a muted device.
- **Audio focus**: during an ad it takes `AUDIOFOCUS_GAIN_TRANSIENT`, so Spotify, podcast apps and similar pause. They resume automatically when the ad ends.

## Build

You need JDK 17 and the Android SDK (compileSdk 34). Unity's **Android Build Support** module bundles both, so you don't need Android Studio. In PowerShell:

```powershell
$AP = "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer"
$env:JAVA_HOME = "$AP\OpenJDK"; $env:ANDROID_HOME = "$AP\SDK"
.\gradlew.bat copyAarToUnity
```

The Gradle wrapper (8.14) is committed. The AAR lands in `Packages/com.darjanrac.soundads/Plugins/Android/soundads-bridge.aar`. Unity sets `.aar` files to Android only by default.

### Notes

- **No kotlin-stdlib at runtime.** Unity's Gradle build doesn't ship the Kotlin standard library, so the bridge must not call into it. `build.gradle.kts` turns off the compiler's null-check intrinsics (`-Xno-param-assertions`, `-Xno-call-assertions`, `-Xno-receiver-assertions`), and the code avoids constructs that emit `Intrinsics` calls. For example, it uses `getSystemService(AudioManager::class.java)` rather than an `as` cast. To check a build, run `javap -c` on the class in `classes.jar`: it should show no `kotlin/jvm/internal` calls.
- **Build tools.** Unity's SDK ships only build-tools 36.0.0, so the module pins `buildToolsVersion = "36.0.0"`. Otherwise the Android Gradle plugin tries to download 34.0.0 into the read-only Unity install.
- **`local.properties`.** If an IDE generates it, `sdk.dir` must use forward slashes (`C:/Program Files/...`). Single backslashes are escape characters in `.properties` files and give "The filename, directory name, or volume label syntax is incorrect".

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
