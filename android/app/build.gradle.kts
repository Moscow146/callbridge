plugins {
	id("com.android.application")
}

android {
	namespace = "dev.callbridge"
	compileSdk = 36

	defaultConfig {
		applicationId = "dev.callbridge"
		minSdk = 31
		targetSdk = 35
		versionCode = 1
		versionName = "0.1.0"
	}

	buildTypes {
		release {
			isMinifyEnabled = false
			signingConfig = signingConfigs.getByName("debug")
		}
	}

	compileOptions {
		sourceCompatibility = JavaVersion.VERSION_17
		targetCompatibility = JavaVersion.VERSION_17
	}
}

dependencies {
	implementation("androidx.activity:activity:1.13.0")
	implementation("androidx.camera:camera-core:1.6.2")
	implementation("androidx.camera:camera-camera2:1.6.2")
	implementation("androidx.camera:camera-lifecycle:1.6.2")
	implementation("androidx.camera:camera-view:1.6.2")
	implementation("com.google.zxing:core:3.5.4")
}
