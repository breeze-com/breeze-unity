# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
