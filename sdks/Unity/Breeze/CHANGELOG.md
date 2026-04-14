# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-04-14

- **Added** Breeze Setup editor window (`Tools/Breeze/Setup`) with automatic deep link configuration for iOS and Android
- **Added** `BreezeEditorSettings` to persist app scheme and configuration in `ProjectSettings/BreezeSettings.json`
- **Added** `BreezeRuntimeSettings` ScriptableObject to auto-load app scheme from editor setup at runtime
- **Added** Dynamic bundle ID for iOS URL schemes in Xcode post-process (replaces hardcoded scheme)
- **Added** `com.unity.purchasing` as a package dependency
- **Added** UPM sample registration — demo is now importable via Package Manager (`Samples~/BreezeDemo`)
- **Changed** Migrated demo scene to Universal Render Pipeline (URP)
- **Changed** Renamed `Documents/` to `Documentation~/` to exclude docs from package installation
- **Changed** Minimum Unity version set to `6000.3`
- **Fixed** Unused variable warning caused by `BREEZE_DEBUG` compile flag in `BreezeNativeAndroid.cs`
- **Fixed** `IapDemoPostProcess` StoreKit path and added `UNITY_IOS`/`UNITY_EDITOR` platform guards
- **Removed** `csc.rsp` from Runtime
- **Removed** Unused demo assets (manually placed `AndroidManifest.xml`, UI Toolkit settings)


## [1.1.0] - 2026-03-03

- **Added** `Breeze.Instance.ShowPaymentWebview` show payment page in webview instead of browser tabs
- **Added** `BREEZE_DEBUG` compile flag — SDK logs stripped by default
- **Added** `BrzPaymentOptionsTheme` support (auto/light/dark)
- **Added** `AppScheme` validation on initialization
- **Fixed** Android dismiss reasons all reported as `CloseTapped` (type mismatch in bridge)
- **Fixed** Duplicate Android callbacks (two competing receivers)
- **Fixed** iOS double callback on direct payment tap
- **Fixed** `Breeze.Initialize()` silently re-initialized when called twice
- **Fixed** `DismissPaymentPageView()` crash in Editor (was `NotImplementedException`)
- **Fixed** Android `getDeviceUniqueId()` returned hardcoded string
- **Tests** 8 test files covering all SDK components

## [1.0.0] - 2026-03-01

### This is the first release of *Breeze Payment SDK*.

- Initial release
