plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.darjanrac.soundads"
    compileSdk = 34
    // Unity's bundled Android SDK ships only build-tools 36.0.0; pin it so AGP doesn't try
    // to download 34.0.0 into the (read-only) Unity install folder.
    buildToolsVersion = "36.0.0"

    defaultConfig {
        minSdk = 23
        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions {
        jvmTarget = "17"
        // Unity's Gradle build does not ship kotlin-stdlib. Without these flags the compiler
        // inserts kotlin.jvm.internal.Intrinsics null checks, which throw NoClassDefFoundError
        // on device and silently push the SDK onto simulated device audio.
        freeCompilerArgs += listOf(
            "-Xno-param-assertions",
            "-Xno-call-assertions",
            "-Xno-receiver-assertions",
        )
    }
}

// No dependency on Unity's classes.jar: the bridge only needs a Context, which C# passes in.

// ./gradlew copyAarToUnity  ->  builds release AAR and drops it into the Unity package.
tasks.register<Copy>("copyAarToUnity") {
    dependsOn("assembleRelease")
    from(layout.buildDirectory.file("outputs/aar/soundads-bridge-release.aar"))
    into(rootProject.file("../../Packages/com.darjanrac.soundads/Plugins/Android"))
    rename { "soundads-bridge.aar" }
}
