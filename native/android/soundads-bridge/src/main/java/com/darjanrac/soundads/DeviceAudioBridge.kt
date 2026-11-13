package com.darjanrac.soundads

import android.content.Context
import android.media.AudioAttributes
import android.media.AudioFocusRequest
import android.media.AudioManager
import android.os.Build
import android.util.Log

/**
 * Native half of the SoundAds Unity SDK.
 *
 * Unity calls these as static methods over JNI:
 * ```csharp
 * var bridge = new AndroidJavaClass("com.darjanrac.soundads.DeviceAudioBridge");
 * float volume = bridge.CallStatic<float>("getVolume01", activity);
 * ```
 * Every method is defensive: a failure here must never crash the host game.
 */
object DeviceAudioBridge {
    private const val TAG = "SoundAds"

    /** Kept so the same request can be abandoned on API 26+. */
    private var focusRequest: AudioFocusRequest? = null

    /** Legacy (API < 26) focus listener; must be the same instance for request and abandon. */
    private val legacyListener = AudioManager.OnAudioFocusChangeListener { change ->
        Log.d(TAG, "Audio focus changed: $change")
    }

    // Typed overload (API 23+) instead of `getSystemService(...) as AudioManager`: the cast makes
    // kotlinc emit Intrinsics.checkNotNull, and Unity's build has no kotlin-stdlib to back it.
    private fun audioManager(context: Context): AudioManager =
        context.applicationContext.getSystemService(AudioManager::class.java)

    /**
     * Current media (STREAM_MUSIC) volume, normalised to 0..1.
     * Ads play on the media stream, so that is the volume the user will actually hear.
     */
    @JvmStatic
    fun getVolume01(context: Context): Float = try {
        val am = audioManager(context)
        val max = am.getStreamMaxVolume(AudioManager.STREAM_MUSIC)
        if (max <= 0) 0f else am.getStreamVolume(AudioManager.STREAM_MUSIC).toFloat() / max
    } catch (e: Exception) {
        Log.w(TAG, "getVolume01 failed", e)
        1f
    }

    /**
     * True when an audio ad would be inaudible: media volume is zero, or the stream is muted
     * (API 23+). The ringer's silent mode is respected too, as users expect "silent" to mean silent.
     */
    @JvmStatic
    fun isMuted(context: Context): Boolean = try {
        val am = audioManager(context)
        val streamMuted = am.isStreamMute(AudioManager.STREAM_MUSIC)
        val silentMode = am.ringerMode == AudioManager.RINGER_MODE_SILENT
        streamMuted || silentMode || am.getStreamVolume(AudioManager.STREAM_MUSIC) == 0
    } catch (e: Exception) {
        Log.w(TAG, "isMuted failed", e)
        false
    }

    /**
     * Requests transient audio focus so music/podcast apps (Spotify, YouTube Music...) pause
     * for the length of the ad and resume automatically afterwards.
     *
     * @return true if focus was granted.
     */
    @JvmStatic
    fun requestAudioFocus(context: Context): Boolean = try {
        val am = audioManager(context)
        val result = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val request = AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN_TRANSIENT)
                .setAudioAttributes(
                    AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_MEDIA)
                        .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
                        .build()
                )
                .setOnAudioFocusChangeListener(legacyListener)
                .build()
            focusRequest = request
            am.requestAudioFocus(request)
        } else {
            @Suppress("DEPRECATION")
            am.requestAudioFocus(legacyListener, AudioManager.STREAM_MUSIC, AudioManager.AUDIOFOCUS_GAIN_TRANSIENT)
        }
        val granted = result == AudioManager.AUDIOFOCUS_REQUEST_GRANTED
        Log.d(TAG, "Audio focus requested, granted=$granted")
        granted
    } catch (e: Exception) {
        Log.w(TAG, "requestAudioFocus failed", e)
        false
    }

    /** Releases focus taken by [requestAudioFocus]; other apps resume playback. */
    @JvmStatic
    fun abandonAudioFocus(context: Context) {
        try {
            val am = audioManager(context)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                focusRequest?.let { am.abandonAudioFocusRequest(it) }
                focusRequest = null
            } else {
                @Suppress("DEPRECATION")
                am.abandonAudioFocus(legacyListener)
            }
            Log.d(TAG, "Audio focus abandoned")
        } catch (e: Exception) {
            Log.w(TAG, "abandonAudioFocus failed", e)
        }
    }
}
