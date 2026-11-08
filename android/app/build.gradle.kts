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
		versionCode = 6
		versionName = "0.5.0"
	}

	signingConfigs {
		System.getenv("CALLBRIDGE_KEYSTORE")?.let { keystore ->
			create("permanent") {
				storeFile = file(keystore)
				storeType = "PKCS12"
				storePassword = System.getenv("CALLBRIDGE_KEYSTORE_PASSWORD")
				keyAlias = "callbridge"
				keyPassword = System.getenv("CALLBRIDGE_KEYSTORE_PASSWORD")
			}
		}
	}

	buildTypes {
		release {
			isMinifyEnabled = false
			signingConfig = signingConfigs.findByName("permanent") ?: signingConfigs.getByName("debug")
		}
	}

	compileOptions {
		sourceCompatibility = JavaVersion.VERSION_17
		targetCompatibility = JavaVersion.VERSION_17
	}
}

dependencies {
	implementation("androidx.activity:activity:1.13.0")
	implementation("androidx.dynamicanimation:dynamicanimation:1.1.0")
	implementation("androidx.camera:camera-core:1.6.2")
	implementation("androidx.camera:camera-camera2:1.6.2")
	implementation("androidx.camera:camera-lifecycle:1.6.2")
	implementation("androidx.camera:camera-view:1.6.2")
	implementation("com.google.zxing:core:3.5.4")
	testImplementation("junit:junit:4.13.2")
}
