# tic-tac-toe × SoundAds

A two-player Tic-Tac-Toe game for Unity 6, plus **SoundAds**, a full in-app **audio advertising SDK** built from scratch alongside it. The repo holds the whole ad stack end to end:

| Part | Where | Tech |
|---|---|---|
| **Game** (publisher app) | [`Assets/`](Assets/) | Unity 6000.3.11f1, URP, uGUI, DOTween |
| **SoundAds Unity SDK** | [`Packages/com.darjanrac.soundads/`](Packages/com.darjanrac.soundads/) | Embedded UPM package, C# |
| **Android native bridge** | [`native/android/`](native/android/) | Kotlin, built as an `.aar` |
| **Ad server and dashboard** | [`backend/`](backend/) | ASP.NET Core minimal API, .NET 10 |

It has been tested end to end on a physical Android phone: the game serves spoken ads from the server, the Kotlin bridge pauses Spotify and refuses ads on a muted device, and impressions and completions show up live on the dashboard.

---

## Architecture

```
 ┌──────────────── Unity game (Assets/) ─────────────────┐
 │  GameAds.cs ── AdPlacements.cs                         │
 │      │  Initialize / LoadAd / ShowAd / events          │
 │      ▼                                                 │
 │  SoundAds SDK  (Packages/com.darjanrac.soundads)       │
 │   ├─ HttpAdNetwork ─────── HTTP ──────────────────────────►  backend/SoundAds.Server
 │   │    └─ FallbackAdNetwork → MockAdNetwork (offline)  │       GET  /health
 │   ├─ EventTracker (batched, retried) ─ HTTP ──────────────►    GET  /v1/ads      → ad JSON + audio URL
 │   ├─ AdPlayer + AdOverlayView (runtime uGUI banner)    │       POST /v1/events   → impressions, quartiles…
 │   └─ AndroidDeviceAudio ─── JNI ──┐                    │       GET  /dashboard   → live report
 └───────────────────────────────────┼────────────────────┘
                                     ▼
              soundads-bridge.aar  (native/android, Kotlin)
              media volume · mute detection · audio focus
```

**Things worth a look:**

- **Small public API.** `SoundAdsSdk` is one static facade with five methods and five events. Everything else is `internal` and visible only to the SDK's own editor and test assemblies, so the public API can be versioned.
- **Exactly one completion callback.** Every `ShowAd` ends with exactly one `OnAdFinished` (`Completed`, `Muted` or `Failed`), so publishers always know when to resume their game.
- **Never breaks the game.** If the server is unreachable, a `FallbackAdNetwork` decorator switches to built-in offline ads. If the native bridge is missing or a JNI call throws, the SDK falls back to simulated device audio. Exceptions in publisher event handlers are logged and contained.
- **Audio done right.** Ads play on unscaled time with `ignoreListenerPause`, game music fades out and SFX mute during an ad, and on Android the SDK takes transient audio focus, so music apps pause and then resume.
- **Respects the player.** Ads never play to a muted device. Rewarded ads prompt the player to turn up the volume. Standard ads become mutable after 5 s. GDPR consent switches requests to non-personalised (`npa=1`) with no session id.
- **Tested at every layer.** 17 EditMode and 9 PlayMode SDK tests, plus 10 server integration tests.

---

## The game

Local two-player Tic-Tac-Toe in portrait or landscape: themes, stats saved between sessions, settings, a win-line strike animation, a result popup with the game's duration, and undo.

The codebase also serves as the demonstration project for a bachelor's thesis on design patterns in mobile games. You can find a **Singleton** (`SingletonMonoBehaviour`), an explicit **State Machine** (`GameStateMachine`), **Command** with undo (`ICommand`, `PlaceMarkCommand`, `CommandManager`), an **Object Pool** (`MarkerPool`), and the **Observer** pattern through C# events. The SDK itself adds **Facade**, **Strategy** (`IAdNetwork`, `IDeviceAudio`) and **Decorator** (`FallbackAdNetwork`).

### Ads in the game

| Placement | Format | Trigger |
|---|---|---|
| `standard_between_games` | Standard (mutable after 5 s) | Every 2nd **Retry**. It preloads while the win-line animation plays. |
| `rewarded_extra_undo` | Rewarded | After the free undo is used, the button reads **+1 UNDO (AD)**. |
| `rewarded_theme_unlock` | Rewarded | Tapping the locked theme ("LISTEN TO UNLOCK") unlocks it permanently. |

Every request is tagged with the game moment (win, draw or reward) so advertisers can target it. **Settings → Personalised ads** controls consent.

### Integrating the SDK

```csharp
using SoundAds;

SoundAdsSdk.OnAdStarted     += _ => music.Pause();
SoundAdsSdk.OnAdFinished    += (_, result) => music.UnPause();
SoundAdsSdk.OnRewardGranted += placement => GrantExtraUndo();

SoundAdsSdk.SetConsent(userConsented);
SoundAdsSdk.Initialize(ok =>
    SoundAdsSdk.LoadAd("rewarded_extra_undo", AdFormat.Rewarded, AdMoment.Reward));

// later
if (SoundAdsSdk.IsAdReady("rewarded_extra_undo"))
    SoundAdsSdk.ShowAd("rewarded_extra_undo");
```

The full API, guarantees and wire format are in the [SDK README](Packages/com.darjanrac.soundads/README.md).

---

## Quick start: Editor

1. Open the project in **Unity 6000.3.11f1**.
2. Optional: start the server (see [Run the ad server](#1-run-the-ad-server)). Without it you get offline jingle ads.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
4. Filter the Console by `[SoundAds]` (SDK) and `[Ads]` (game).

---

## Full Android build: game, SDK, bridge and server on a phone

### Prerequisites

| What | Notes |
|---|---|
| Unity 6000.3.11f1 with **Android Build Support** | Add it in Unity Hub with the **OpenJDK** and **Android SDK & NDK** options. It provides JDK 17, the Android SDK and `adb`, so you don't need Android Studio. |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | For the ad server. |
| An Android phone | With Developer options and USB debugging turned on, if you use a cable. |

The commands below are PowerShell. They use Unity's bundled toolchain, so set this variable in **every** new terminal:

```powershell
$AP = "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer"
```

### 1. Run the ad server

Leave this terminal open:

```powershell
cd backend\SoundAds.Server
dotnet run --no-launch-profile --urls http://0.0.0.0:5080
```

Check it at http://localhost:5080/health and http://localhost:5080/dashboard. Binding to `0.0.0.0` makes the server reachable from the phone. Events are kept in memory, so restarting the server clears the dashboard.

### 2. Build the Kotlin bridge

```powershell
cd native\android
$env:JAVA_HOME = "$AP\OpenJDK"; $env:ANDROID_HOME = "$AP\SDK"
.\gradlew.bat copyAarToUnity
```

This writes `Packages/com.darjanrac.soundads/Plugins/Android/soundads-bridge.aar`. You only need to rebuild it when the Kotlin code changes. If the build complains about the SDK path, see [Troubleshooting](#troubleshooting).

### 3. Configure Unity

1. Go to **File → Build Profiles → Android → Switch Platform**.
2. In the Project window, select **Packages → SoundAds SDK → Plugins → Android → soundads-bridge**. Make sure only **Android** is ticked; that's Unity's default for `.aar` files.
3. Choose **Tools → SoundAds → Create or Select Settings**, which creates `Assets/Resources/SoundAdsSettings.asset`:

   | Field | Value |
   |---|---|
   | API Key | `test-key` |
   | Server Url | `http://localhost:5080` over USB, or `http://<PC-IP>:5080` over Wi-Fi (see step 4) |
   | Test Mode | Off. On means offline ads only. |
   | Fallback To Offline Ads | On |

4. In **Project Settings → Player → Android → Other Settings**, set:
   - **Package Name**: your own, for example `com.yourname.tictactoe`
   - **Allow downloads over HTTP**: **Always allowed**. The dev server is plain `http://`. Use HTTPS and *Not allowed* in production.
   - **Internet Access**: **Require**
5. In the Android build profile, tick **Development Build** and leave **Build App Bundle** off so you get an APK.
6. Use **File → Save Project**. Player Settings only reach the disk when you save.

### 4. Connect the phone to the server

On the phone, `localhost` means the phone itself, so choose one of these two options.

**A. USB cable (recommended)**

Keep Server Url at `http://localhost:5080` and forward the port:

```powershell
& "$AP\SDK\platform-tools\adb.exe" devices
& "$AP\SDK\platform-tools\adb.exe" reverse tcp:5080 tcp:5080
```

Run the `reverse` command again whenever you unplug the phone.

**B. Same Wi-Fi, no cable**

1. Run `ipconfig` and find the **IPv4 Address** of the *Wi-Fi* adapter. Ignore `vEthernet`, which belongs to Hyper-V/WSL.
2. Set Server Url to `http://<that-IP>:5080`, then save the project.
3. Open the port in an **admin** PowerShell:
   ```powershell
   New-NetFirewallRule -DisplayName "SoundAds dev 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
   ```
   Your Wi-Fi network has to be set to *Private* in Windows.
4. Check from the **phone's browser**: `http://<that-IP>:5080/health` should return `{"status":"ok"…}`.

> **Office, coworking or hotel Wi-Fi** usually has *client isolation*: devices reach the internet but not each other, so the health check never loads. Turn on your **phone's hotspot**, connect the PC to it, and use the new IP instead. The IP can change each time you reconnect.

### 5. Build and install

- **With a cable:** go to **Build Profiles → Android**, pick your phone under *Run Device*, and click **Build And Run**.
- **Without a cable:** click **Build**, copy the `.apk` to the phone (Drive, Quick Share, email…), open it, and allow *Install unknown apps*.

> **Start the server and the connection first.** The SDK checks `/health` once, in `Initialize`. If the server is unreachable at launch, that whole session uses offline ads. After fixing the connection, force-stop the app and open it again.

### 6. Verify

With a cable you can stream the logs:

```powershell
& "$AP\SDK\platform-tools\adb.exe" logcat -s Unity SoundAds
```

| What you do | What you should see |
|---|---|
| Launch the app | `[SoundAds] Initializing SDK … server=…`, then `Native Android bridge connected (volume=…)`, then `[Ads] SDK initialised: True` |
| Finish a game and tap **Retry** twice | A **spoken** ad plays, and the server console logs `Fill ad-… -> placement=standard_between_games` |
| Use the free undo, then tap **+1 UNDO (AD)** | A rewarded ad plays and you get an extra undo |
| Play Spotify, then trigger an ad | Spotify pauses for the ad and resumes afterwards (audio focus) |
| Set media volume to 0, then trigger an ad | The ad is refused with `DeviceMuted` and the game carries on |
| Refresh `/dashboard` | Impressions, completions, mutes and rewards per ad, moment and placement |

Without a cable, use the server console and the dashboard. Spoken ads mean the server is serving them; short generated jingles mean the app fell back to offline ads.

---

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `Ad server … unreachable; serving offline test ads` | The server or the connection wasn't up at launch. Check `/health` from the phone's browser, then force-stop and relaunch the app. |
| `Android bridge unavailable, using simulated device audio` | The `.aar` is missing, or Android isn't ticked on it. Rebuild it (step 2). |
| `Insecure connection not allowed` | **Allow downloads over HTTP** isn't set, or Player Settings weren't saved. |
| `Http401` on ad requests | The API key in `SoundAdsSettings` doesn't match `SoundAds:ApiKeys` in `appsettings.json`. |
| Gradle: `JAVA_HOME is set to an invalid directory: \OpenJDK` | `$AP` isn't set in that terminal. |
| Gradle: `The filename, directory name, or volume label syntax is incorrect` | `native/android/local.properties` has single backslashes in `sdk.dir`, which is an escape character in `.properties` files. Use forward slashes: `sdk.dir=C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK` |
| Health check works on the PC but not from the phone over Wi-Fi | The firewall rule doesn't cover the active network profile, or the Wi-Fi uses client isolation. Use the hotspot or a cable. |

---

## Tests

| Suite | How to run | Covers |
|---|---|---|
| SDK EditMode (17) | **Window → General → Test Runner → EditMode** | Wire format, JSON parsing, event batching and retry, network fallback |
| SDK PlayMode (9) | Test Runner → **PlayMode** | Rewarded, standard and mute flows, the muted-device gate, tracking, publisher exceptions. 3 tests hit the live server and are skipped if it isn't running. |
| Server (10) | `dotnet test backend` | Fill, no fill, auth, events, report, CORS |

The SDK tests are registered through `"testables"` in `Packages/manifest.json`.

## Repository layout

```
Assets/                         game: scenes, scripts (Core, Game, UI, Ads), art, audio
Packages/com.darjanrac.soundads SoundAds SDK: Runtime, Editor (settings provider), Tests, Samples
native/android/                 Kotlin bridge (Gradle project), copyAarToUnity task
backend/SoundAds.Server/        ad server, ads.json inventory, wwwroot/audio creatives, dashboard
backend/SoundAds.Server.Tests/  server integration tests
docs/                           WebGL build of the game (GitHub Pages)
Project Spec/                   original game assignment
```

## More docs

- [SDK README](Packages/com.darjanrac.soundads/README.md): public API, guarantees, architecture, server contract
- [Server README](backend/README.md): endpoints, inventory, configuration
- [Android bridge README](native/android/README.md): Kotlin build and JNI surface
