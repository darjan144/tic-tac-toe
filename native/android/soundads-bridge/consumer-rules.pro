# Unity calls DeviceAudioBridge reflectively over JNI; keep it if the host app minifies.
-keep class com.darjanrac.soundads.DeviceAudioBridge { *; }
