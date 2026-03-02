# Breeze Unity SDK — Roadmap

## Current State (v1.0.0)

### iOS
- Native payment options dialog with product info, pricing, and discount display
- Payment page rendered via Safari View Controller
- Deep linking for payment completion callbacks
- Theme support: Auto / Light / Dark

### Android
- Native payment options dialog with product info, pricing, and discount display
- Payment page rendered via Chrome Custom Tabs (fallback to default browser)
- Deep linking for payment completion callbacks
- Theme support: Auto / Light / Dark

### General
- Unity Package Manager (UPM) distribution via git URL
- Event-driven API (`OnPaymentOptionsDialogDismissed`) with dismiss reason and custom data passthrough
- Demo project with full IAP integration example
- Unit tests for URL helpers and native models

---

## Planned

### Developer Experience — Unity Editor Support
Improve the inner development loop by enabling debugging and testing directly in the Unity Editor without building to a device.

- Simulate the payment options dialog UI in the Editor (replace current no-op logging with a visual mock)
- Generate mock payment callbacks so developers can test their event handling logic end-to-end
- Add editor-visible debug logging and diagnostics for SDK initialization and payment flow

### Configuration — Settings UI in Editor
Provide a visual interface for configuring the SDK, removing the need to set options purely in code.

- Custom Editor window or Inspector panel for Breeze SDK settings
- Visual configuration of App Scheme, Environment, and theme preferences
- Build-time validation of settings to catch misconfigurations early

### Webview Payment Page — iOS & Android
Render the Breeze payment page inside an in-app webview instead of switching to Safari View Controller or Chrome Custom Tabs, keeping the player fully within the game.

- Embedded webview component for iOS (WKWebView) and Android (Android WebView)
- Intercept payment completion events from the webview to trigger callbacks
- Maintain the existing deep linking path as a fallback option

### In-Game Shop
Provide an embeddable shop UI that lets players browse and purchase products without leaving the game.

- Reusable shop component that renders within the game's UI layer
- Product catalog display with pricing and Breeze discount information
- Integration with the existing payment dialog and payment page flows
