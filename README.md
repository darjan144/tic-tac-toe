# tic-tac-toe

Local two-player Tic-Tac-Toe in **Unity 6 (6000.3.11f1)**, URP, uGUI and DOTween. It supports portrait and landscape.

The game is also the **publisher app** for **SoundAds**, an in-app audio ad SDK in the style of Audiomob, which lives in this repo. You don't need an account or an SDK from anyone else.

| Part | Where | What it is |
|---|---|---|
| Game | `Assets/` | The tic-tac-toe game. It integrates the SDK through `Assets/Scripts/Ads/`. |
| **SoundAds Unity SDK** | `Packages/com.darjanrac.soundads/` | Embedded UPM package: the public API, ad playback, the banner and event tracking. |
| **Ad server** | `backend/SoundAds.Server/` | ASP.NET Core (.NET 10) API that serves ads and collects events, plus a dashboard. |
| **Android native bridge** | `native/android/` | Kotlin library for device volume, mute detection and audio focus. It is built as an `.aar` for Unity. |

---

## How the pieces fit

```
 ┌──────────── Unity game (Assets/) ────────────┐
 │  GameAds.cs ── AdPlacements.cs               │
 │      │  Initialize / LoadAd / ShowAd / events │
 │      ▼                                        │
 │  SoundAds SDK (Packages/com.darjanrac.soundads)
 │   ├─ HttpAdNetwork ───── HTTP ──────────────────► backend/SoundAds.Server
 │   │    └ falls back to MockAdNetwork offline  │      GET  /v1/ads   (ad + audio URL)
 │   ├─ EventTracker ────── HTTP ──────────────────►      POST /v1/events (impressions…)
 │   ├─ AdPlayer + AdOverlayView (banner)        │      GET  /dashboard
 │   └─ AndroidDeviceAudio ── JNI ──┐            │
 └──────────────────────────────────┼────────────┘
                                    ▼
             soundads-bridge.aar (native/android, Kotlin)
             volume · mute · audio focus (pauses Spotify etc.)
```

- **Only the Unity project is needed to play.** If the server can't be reached, the SDK serves built-in offline test ads with generated jingle audio, so the game always works, including in the Editor and on WebGL.
- **The server** provides the real spoken ads, plus tracking and the dashboard.
- **The Kotlin AAR** is used only in Android builds. Without it, Android falls back to simulated device audio: no mute check, and other apps' music isn't paused.

## Ads in the game

| Placement | Format | Where it plays |
|---|---|---|
| `standard_between_games` | Standard: mute unlocks after 5 s | On every 2nd **Retry**. It loads while the win-line animation plays. |
| `rewarded_extra_undo` | Rewarded | Each game has 1 free undo. After that, the button reads **+1 UNDO (AD)**. |
| `rewarded_theme_unlock` | Rewarded | Tapping the locked last theme ("LISTEN TO UNLOCK") unlocks it permanently. |

- **Moments:** every ad request is tagged with the game moment: win, draw or reward.
- **Audio:** game music fades out and sound effects are muted while an ad plays.
- **Consent:** the **Settings → Personalised ads** toggle sets GDPR consent. With it off, ads are still served, but they are non-personalised (`npa=1`) and carry no session id.

---

## Running it

### 1. Editor (quickest)

1. Open the project in Unity 6000.3.11f1.
2. Optional: start the server (step 2) for real ads. If you skip it, offline ads are used.
3. Open `Assets/Scenes/MainMenu.unity` and press Play.
4. Watch the Console for `[SoundAds]` (SDK) and `[Ads]` (game) log lines.

### 2. Ad server

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend/SoundAds.Server
dotnet run --urls http://0.0.0.0:5080
```

- Dashboard: http://localhost:5080/dashboard
- Tests: `dotnet test backend`
- Endpoint details: [backend/README.md](backend/README.md)

`0.0.0.0` makes the server reachable from other devices on your network, which an Android phone needs. If you only test in the Editor, `http://localhost:5080` is enough.

### 3. SDK settings

In Unity, choose **Tools → SoundAds → Create or Select Settings**. That creates `Assets/Resources/SoundAdsSettings.asset`, which is also editable under **Project Settings → SoundAds**.

| Field | Editor | Android device |
|---|---|---|
| API Key | `test-key` | `test-key` |
| Server Url | `http://localhost:5080` | See step 5 |
| Test Mode | Off; on = offline ads only | Off |
| Fallback To Offline Ads | On | On |

Use **Test connection** to confirm the Editor can reach the server. Without the asset, the SDK uses the defaults shown above.

---

## Android build checklist

The project's build target is currently **WebGL**. To ship the ads on Android:

### 1. Install the Android module
In **Unity Hub**, go to **Installs → 6000.3.11f1 → Add modules** and add **Android Build Support**, including the OpenJDK and Android SDK & NDK tools.

### 2. Switch the platform
Go to **File → Build Profiles → Android → Switch Platform**.

### 3. Build the Kotlin bridge
Build the AAR into the package once. You need JDK 17 and the Android SDK, either from Android Studio or from the Unity module above.

```bash
cd native/android
gradle wrapper --gradle-version 8.7   # first time only (or open the folder in Android Studio)
./gradlew copyAarToUnity
```

- This writes `Packages/com.darjanrac.soundads/Plugins/Android/soundads-bridge.aar`.
- In Unity, select the `.aar` and make sure only the **Android** platform is ticked.
- If you use the Unity module's JDK and SDK, point the build at them by setting:
  - `JAVA_HOME` to `…/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK`
  - `ANDROID_HOME` to `…/AndroidPlayer/SDK`
- Details: [native/android/README.md](native/android/README.md).

### 4. Allow HTTP to the development server
The dev server is plain `http://`, and Android blocks cleartext traffic by default. Open **Project Settings → Player → Android → Other Settings** and set **Allow downloads over HTTP** to **Always allowed**, or to *Allowed in development builds* together with a Development Build.

For production, use an `https://` server and leave this setting at *Not allowed*.

### 5. Let the phone reach the server
On a phone, `localhost` means the phone itself, not your PC. Pick one of these:

- **USB with `adb reverse` (recommended):** keep Server Url at `http://localhost:5080` and run:
  ```bash
  adb reverse tcp:5080 tcp:5080
  ```
- **Wi-Fi:** set Server Url to `http://<your-PC-LAN-IP>:5080`. Run the server with `--urls http://0.0.0.0:5080`, and allow port 5080 through Windows Firewall.

### 6. Build and run
Use **Build And Run**. You can check the result in `adb logcat` by filtering for `SoundAds`:

| What you do | What you should see |
|---|---|
| Launch the app | `Native Android bridge connected (volume=…)`. If you see `Android bridge unavailable` instead, the AAR is missing. |
| Play music in Spotify, then trigger an ad | Spotify pauses during the ad and resumes when it ends (audio focus). |
| Turn the media volume to 0 and trigger an ad | The ad is refused with `DeviceMuted`, and the game continues. |
| Play some ads | Impressions and completions appear on the server's dashboard. |

If the server can't be reached, the device still plays offline ads, and the log reads `Ad server … unreachable; serving offline test ads`.

---

## Tests

| Suite | How to run | Covers |
|---|---|---|
| SDK EditMode (17) | **Window → General → Test Runner → EditMode** | Wire format, JSON, event batching and retry, network fallback |
| SDK PlayMode (9) | Test Runner → PlayMode | Rewarded, standard and mute flows, the muted-device gate, tracking. 3 tests run against the live server and are skipped if it isn't running. |
| Server (10) | `dotnet test backend` | Fill, no fill, auth, events, report, CORS |

The SDK tests are registered through `"testables"` in `Packages/manifest.json`.

## Docs

- [SDK README](Packages/com.darjanrac.soundads/README.md): public API, guarantees, architecture
- [Server README](backend/README.md)
- [Android bridge README](native/android/README.md)
- `Project Spec/`: the original game assignment
