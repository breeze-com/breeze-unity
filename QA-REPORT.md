# Breeze Unity Payment SDK — QA Security Review

**Date:** 2026-03-01  
**Reviewer:** TARS (automated QA)  
**Scope:** All SDK source (`sdks/`), native plugins (iOS/Android), example code (`examples/`)

---

## Security

### 1. Can payments be spoofed / faked?

🟡 **Medium** — The SDK itself does **not** verify payments. It delegates to `BreezePaymentVerifier` which polls the *game server*. The game server is the trust boundary. However, the **example code** (`ShowPaymentOptionsDialogUI.cs`) grants items based on the deep link URL path (`/purchase/success`) *before* server verification:

```csharp
// ShowPaymentOptionsDialogUI.cs line ~197
if (IsPaymentSuccessUrl(url))
{
    PlayPaymentSuccessEffect();  // Grants items based on URL alone!
}
```

The comment says "make sure verify the result on server side first" but the code doesn't actually wait for verification. A player could craft a deep link `breezedemo://breeze-payment/purchase/success` to trigger the success effect without paying.

**Recommendation:** Example should demonstrate using `BreezePaymentVerifier.WaitForPaymentAsync()` before granting items.

### 2. Is `directPaymentUrl` validation sufficient?

🟡 **Medium** — Both iOS and Android check `host.hasSuffix(".breeze.cash")` / `host.endsWith(".breeze.cash")`. Issues:

- **Subdomain spoofing:** An attacker could register `evil.breeze.cash` — though this requires DNS control of breeze.cash, so risk is low in practice.
- **Missing scheme check:** Neither platform enforces HTTPS. A URL like `http://pay.breeze.cash/...` would pass validation but send payment data over plaintext.
- **iOS validates, Android validates, C# SDK does NOT validate.** The validation only happens in native code. If a game developer uses the SDK on a platform without native validation (editor/Noop), there's no check at all.

**Recommendation:** Add HTTPS scheme enforcement. Consider also validating in the C# layer as defense-in-depth.

### 3. Can deep link return URLs be intercepted?

🟡 **Medium** — Custom URL schemes (`breezedemo://`) are inherently insecure on both iOS and Android. Any app can register the same scheme and intercept the redirect. On Android this is especially easy.

**Recommendation:** Document that deep link URLs should NOT be trusted for payment verification (they aren't — the webhook flow handles this). Consider recommending Universal Links / App Links for production.

### 4. Is sensitive data logged?

🔴 **Critical** — Yes, extensively:

- `BreezeNativeAndroid.cs:62` — Logs the full request JSON: `Debug.Log($"brz_show_payment_options_dialog: request = {requestJson}")` — this includes `directPaymentUrl` and `data`
- `BreezeNativeIos.cs:30` — Same log on iOS
- `BreezeNativeNoop.cs:35` — Same in editor
- `Breeze.cs:79` — Logs dismiss reason and data
- `BreezeBridgeMessenger.cs:8` — Logs raw JSON payload from Android
- `BreezePaymentVerifier.cs` — Logs order IDs (acceptable)
- `YourGameClient.cs:55` — Logs the full create order request JSON including `billingEmail` and `clientReferenceId`
- `YourGameClient.cs:73` — Logs the full order response

In production, these logs could leak to crash reporters, device logs (`adb logcat`), or analytics.

**Recommendation:** Remove or guard all `Debug.Log` calls behind a debug/verbose flag. At minimum, never log full request JSON in release builds.

### 5. Is BreezePaymentVerifier polling secure?

🟡 **Medium** —
- ✅ Auth token sent as Bearer header
- ❌ **No HTTPS enforcement** — `GameServerBaseUrl` can be `http://` and the SDK will happily poll over plaintext, leaking the auth token
- ❌ **No certificate pinning**
- ✅ Per-request timeout of 10s
- ✅ Overall timeout + max attempts

**Recommendation:** Validate that `GameServerBaseUrl` starts with `https://`.

### 6. Can the native dialog be manipulated?

🟢 **Low** — The dialog is rendered programmatically (no XML/XIB injection surface). On Android, it uses `Theme_Translucent_NoTitleBar` with a full-screen overlay, reducing overlay attack risk. On iOS, it's a modal `UIViewController`. The main risk is **tapjacking** on Android — the dialog doesn't set `filterTouchesWhenObscured`.

**Recommendation:** Consider setting `filterTouchesWhenObscured = true` on clickable views.

### 7. Thread safety — race conditions?

🟡 **Medium** —
- `BreezeNativeAndroid.pendingDismissCallback` is a static field accessed from both the UI thread (via `UnitySendMessage`) and potentially the Unity main thread. No synchronization.
- `Breeze._instance` is not thread-safe — `Initialize`/`Uninitialize` could race.
- `BreezeAndroidCallbackReceiver.Instance` uses a non-synchronized singleton pattern.
- `BreezeSafariView.activeInstance` is a static var — if two Safari views are somehow opened, they'd overwrite each other.

**Recommendation:** Add `volatile` or locks to shared static state, especially `pendingDismissCallback`.

### 8. Memory leaks — native cleanup?

🟢 **Low** —
- iOS: `BreezeSafariView` retains itself via `activeInstance` and clears on dismiss. Correct pattern, but if dismiss is never called, the instance leaks forever.
- Android: `ExecutorService` created per image load in `BreezePaymentOptionsDialog.loadImageAsync()` is never shut down. Each dialog creates a new executor.
- Android: `BreezeNativeAndroid.androidPlugin` and `androidPluginInstance` are never disposed.
- C# `BreezeNativeNoop` device ID stored in PlayerPrefs is fine.

**Recommendation:** Cache and reuse the executor on Android. Ensure `BreezeSafariView.activeInstance` is nil-ed on timeout.

---

## Payment Flow Correctness

### 9. Webhook arrives before client starts polling

✅ **Not an issue** — The polling design handles this correctly. The game server stores the webhook result; when the client starts polling, it gets the terminal state immediately.

### 10. Webhook never arrives (timeout)

✅ **Handled** — `BreezePaymentVerifier` has configurable `TimeoutSeconds` (default 120s) and `MaxAttempts` (default 60). Returns `Pending` status with error message.

### 11. Game backgrounded during payment

🟡 **Medium** — 
- `Time.realtimeSinceStartup` continues ticking while backgrounded, so the timeout may expire while the user is in the browser paying.
- On Android, Chrome Custom Tabs run in a separate process, so the payment page continues. But if the verifier is running, it may timeout.
- On iOS, `SFSafariViewController` stays alive since it's in-process.

**Recommendation:** Pause timeout while app is backgrounded, or restart polling when app returns to foreground.

### 12. Game killed during payment

🟡 **Medium** — No persistence. If the game is killed:
- The payment may complete on the Breeze side
- The webhook fires and game server knows
- But the client never polls, so items aren't granted in that session
- On next launch, the game would need to check for pending orders — **this is not implemented in the SDK or example**

**Recommendation:** Document that games should check for pending fulfilled orders on startup.

### 13. Items granted without payment?

🔴 **Critical (Example only)** — Yes, in the example code. `PlayPaymentSuccessEffect()` is called based on URL path alone without server verification (see finding #1). The SDK itself doesn't grant items — that's the game's responsibility.

### 14. Double-grant from single payment?

🟢 **Low** — The SDK doesn't track granted payments. The `OnPaymentOptionsDialogDismissed` event fires once per dialog. The `BreezePaymentVerifier` returns a result once. Idempotency is the game server's responsibility.

**Recommendation:** Document that game servers must implement idempotent fulfillment.

---

## Platform-Specific

### 15. iOS SFSafariViewController

🟢 **Low** — Delegation is correct. `BreezeSafariView` is an `NSObject` subclass acting as `SFSafariViewControllerDelegate`. The `didInvokeDismiss` flag prevents double callbacks. `activeInstance` retains the delegate. `weak var safariViewController` is correct.

One minor issue: the root VC lookup `windowScene.windows.first(where: { $0.isKeyWindow })` may fail if called during a transition.

### 16. iOS Storefront country code

🔴 **Critical** — The code checks `storefront.countryCode.lowercased() != "usa"` but `Storefront.countryCode` returns the **three-letter ISO code** (e.g., "USA"). The comparison is correct for "usa" after lowercasing, BUT it means the dialog **only shows for US users**. If the storefront check fails (nil), it dismisses with `appStoreTapped` — meaning non-US users are silently redirected to the App Store without seeing the dialog. This is likely intentional for compliance but should be documented.

ℹ️ **Info** — If Breeze expands to other regions, this hard-coded check needs updating.

### 17. Android Chrome Custom Tabs reflection

🟡 **Medium** — Using reflection (`Class.forName`, `newInstance()`, `getMethod()`) for Custom Tabs is fragile:
- `newInstance()` is deprecated in Java 9+
- If the Custom Tabs API changes method signatures, this silently breaks
- The fallback to default browser is good

**Recommendation:** Consider using a direct dependency on `androidx.browser` or documenting the requirement.

### 18. Android activity recreated

🟡 **Medium** — If the activity is recreated (rotation, memory pressure), `UnityPlayer.currentActivity` will point to the new activity, but:
- `currentDialog` reference is lost (it's an instance field on the singleton)
- The `BreezeAndroidCallbackReceiver` GameObject survives if `DontDestroyOnLoad`

Not a major issue since Unity typically handles config changes, but edge cases exist.

### 19. Android back button

🟢 **Low** — The dialog is `setCancelable(true)` with `setOnCancelListener` that triggers `CloseTapped` dismiss. Back button works correctly. Chrome Custom Tabs handle back natively.

---

## API & Integration

### 20. JSON serialization edge cases

🟡 **Medium** —
- C# uses `Newtonsoft.Json`, iOS uses `JSONDecoder`, Android uses `org.json.JSONObject` — three different parsers for the same data.
- The `BrzPaymentDialogDismissReason` enum uses `StringEnumConverter` in C# but integer values in the Android bridge (`reason.getValue()` returns string, but `DialogDismissedPayload.Reason` is `int`). The Android bridge sends the string value via `reason.getValue()` ("CloseTapped") but the C# receiver casts an int. **This is a bug.**

🔴 **Critical** — In `BreezeUnityBridge.java`, `sendDialogDismissed` puts `reason.getValue()` (a String like "CloseTapped") into JSON as `"reason"`. But `BreezeAndroidCallbackReceiver.DialogDismissedPayload.Reason` is `int`. Newtonsoft.Json will fail to deserialize a string into an int, falling into the catch block which defaults to `CloseTapped`. This means **all Android dismiss reasons are reported as `CloseTapped`**.

Wait — actually looking more carefully: `BreezeBridgeMessenger.cs` also receives this message and uses `BrzPaymentDialogDismissReason` (an enum with `StringEnumConverter`). There are TWO receivers: `BreezeBridgeMessenger` and `BreezeAndroidCallbackReceiver`, both on GameObjects named "BreezePay". `UnitySendMessage` sends to ALL GameObjects with that name.

The `BreezeAndroidCallbackReceiver` receiver has `Reason` as `int` — this will fail parsing the string. The `BreezeBridgeMessenger` receiver uses the enum type with StringEnumConverter — this should work.

**But**: `BreezeAndroidCallbackReceiver` is the one that calls `BreezeNativeAndroid.HandleDialogDismissed`. If it fails parsing, it falls back to `CloseTapped` with null data. The `BreezeBridgeMessenger` calls `Breeze.NotifyOnPaymentOptionsDialogDismissed` directly. So there are **two competing callback paths** with one broken.

**Recommendation:** Remove one of the duplicate callback receivers. Fix the type mismatch.

### 21. URL construction — orderId injection

🟢 **Low** — `BuildStatusUrl` uses `Uri.EscapeDataString(orderId)` which properly encodes the order ID. Path traversal via orderId is prevented.

### 22. UnityWebRequest error handling

🟢 **Low** — The verifier checks `request.result != UnityWebRequest.Result.Success` which covers connection errors, protocol errors, and data processing errors. The 10s timeout handles hung connections. Exception catch handles unexpected errors.

Missing: no check for HTTP status codes (e.g., 401, 500 would still be `Result.Success` with error content).

**Recommendation:** Check `request.responseCode` for non-2xx status.

### 23. Timeout — slow but not down server

🟢 **Low** — Per-request timeout is 10s, overall timeout is 120s. A slow server that responds in 9s would consume attempts slowly. With 2s poll interval + 9s response = 11s per attempt, ~10 attempts in 120s. Adequate.

---

## Code Quality

### 24. Naming conventions

ℹ️ **Info** —
- `BrzShowPaymentOptionsDialogRequest.ProductDisplayInfo` is named `ProductDisplayInfo` in C# but serializes as `"product"` — the property name doesn't match the JSON key. Not a bug but inconsistent.
- `BreezeConfiguration.AppScheme` — the example sets it to `"breezedemo://"` including the scheme separator, but it's never used in the SDK code. Unclear purpose.
- `BreezeBridgeMessenger.OnAndroidDialogDismissed` parameter named `jsonPaylod` (typo: should be `jsonPayload`)

### 25. Null reference risks

🟡 **Medium** —
- `Breeze.Instance` can be null if `Initialize` hasn't been called. No null check in example before `Breeze.Instance.GetDeviceUniqueId()`.
- `ShowPaymentOptionsDialogUI.OnPaymentPageResult` calls `Breeze.Instance.DismissPaymentPageView()` without null check.
- `result.Data.Url` in example — if `Data` is null, NPE.
- `BreezeNativeAndroid.androidPluginInstance` can be null if initialization failed, and `EnsureAndroidPluginInitialized` throws, but callers don't catch.

### 26. Exception handling

🟢 **Low** — Generally adequate. The verifier has try/catch around polling. Native bridges have try/catch. Some empty catches in `BreezeHelper.GetCurrentTwoLetterIsoRegionName` swallow all exceptions silently.

### 27. Dead code

ℹ️ **Info** —
- `BreezeConfiguration.Environment` is set but never read by the SDK
- `BreezeConfiguration.AppScheme` is validated but never used
- `BreezeConstants.SDK_VERSION` is defined but never sent in any request
- `BreezeBase64Helper` — the entire class appears unused
- `BreezeDateTimeExtensions` — no callers found in the SDK
- `BreezeNativeNoop.DismissPaymentPageView()` throws `NotImplementedException` — this would crash in the editor if called

### 28. Missing documentation

ℹ️ **Info** —
- `BreezePaymentVerifier` has excellent XML docs ✅
- `Breeze.cs` main class has no XML docs
- `BreezeHelper` has no docs
- No README or integration guide in the SDK folder
- The example doesn't demonstrate the recommended verification flow

---

## Summary

| Severity | Count | Key Issues |
|----------|-------|------------|
| 🔴 Critical | 3 | Sensitive data logging, example grants items without verification, Android callback type mismatch + duplicate receivers |
| 🟡 Medium | 9 | No HTTPS enforcement, deep link interception, thread safety, backgrounding timeout, missing HTTP status check |
| 🟢 Low | 7 | Overlay attacks, memory leaks, idempotency docs, reflection fragility |
| ℹ️ Info | 4 | Dead code, naming, missing docs |

### Top Recommendations

1. **Remove/guard Debug.Log calls** that dump request JSON in production
2. **Fix Android callback architecture** — remove duplicate receiver, fix type mismatch
3. **Enforce HTTPS** for `directPaymentUrl` and `GameServerBaseUrl`
4. **Update example** to use `BreezePaymentVerifier` before granting items
5. **Add thread safety** to shared static state
6. **Document** deep link security limitations and recovery-on-restart pattern
