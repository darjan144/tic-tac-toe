plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.darjanrac.soundads"
    compileSdk = 34

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
