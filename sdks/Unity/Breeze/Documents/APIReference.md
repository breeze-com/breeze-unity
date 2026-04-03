# Breeze Payment SDK — API Reference

**Package:** `com.breeze.payment-unity`  
**Namespace:** global (no namespace — all types are in the global namespace)  
**Platforms:** iOS 15.0+, Android API 21+  

---

## Table of Contents

1. [Breeze](#breeze)
2. [BreezeConfiguration](#breezeconfiguration)
3. [BreezeEnvironment](#breezeenvironment)
4. [BrzShowPaymentOptionsDialogRequest](#brzshowpaymentoptionsdialogRequest)
5. [BrzPaymentOptionsTheme](#brzpaymentoptionstheme)
6. [BrzProductDisplayInfo](#brzproductdisplayinfo)
7. [BrzPaymentDialogDismissReason](#brzpaymentdialogdismissreason)
8. [BrzShowPaymentWebviewRequest](#brzshowpaymentwebviewrequest)
9. [BrzPaymentWebviewDismissReason](#brzpaymentwebviewdismissreason)
10. [Delegates](#delegates)
11. [Internal Result Codes](#internal-result-codes)

---

## Breeze

The main entry point for the Breeze Payment SDK. A singleton — initialize once and access via `Breeze.Instance`.

### Static Methods

#### `Initialize(BreezeConfiguration configuration)`

Initializes the SDK singleton. Must be called before accessing `Instance` or invoking any payment methods. Logs a warning and returns immediately if already initialized.

| Parameter | Type | Description |
|---|---|---|
| `configuration` | `BreezeConfiguration` | SDK configuration. `AppScheme` is required. |

**Throws:** `ArgumentException` — when `configuration` is `null` or `AppScheme` is empty.

---

#### `Uninitialize()`

Destroys the SDK singleton. `Instance` returns `null` after this call. Call before `Initialize` if you need to re-initialize with a different configuration.

---

#### `NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason reason, string data)`

Native-to-managed callback. Called by the platform layer when the payment options dialog is dismissed. Raises `OnPaymentOptionsDialogDismissed` on the active instance. **Do not call this directly.**

---

#### `NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason reason, string data)`

Native-to-managed callback. Called by the platform layer when the payment web view is dismissed. Raises `OnPaymentWebviewDismissed` on the active instance. **Do not call this directly.**

---

### Static Properties

#### `Instance`

```csharp
public static Breeze Instance { get; }
```

Returns the current singleton instance, or `null` if `Initialize` has not been called.

---

### Instance Methods

#### `GetDeviceUniqueId()`

```csharp
public string GetDeviceUniqueId()
```

Returns a stable device-unique identifier. On iOS this is the vendor identifier (IDFV); on Android it is the Android ID.

**Returns:** A non-empty string.

---

#### `ShowPaymentOptionsDialog(BrzShowPaymentOptionsDialogRequest request)`

```csharp
public void ShowPaymentOptionsDialog(BrzShowPaymentOptionsDialogRequest request)
```

Presents the Breeze payment options bottom sheet over the current Unity view. When dismissed, `OnPaymentOptionsDialogDismissed` is raised on the main thread.

| Parameter | Type | Description |
|---|---|---|
| `request` | `BrzShowPaymentOptionsDialogRequest` | Product info, payment URL, and optional theme. |

---

#### `DismissPaymentPageView()`

```csharp
public void DismissPaymentPageView()
```

Programmatically dismisses any currently-visible payment page web view. No-op if the web view is not shown.

---

#### `ShowPaymentWebview(BrzShowPaymentWebviewRequest request)`

```csharp
public void ShowPaymentWebview(BrzShowPaymentWebviewRequest request)
```

Presents the Breeze payment web view, loading the provided payment URL. When dismissed, `OnPaymentWebviewDismissed` is raised on the main thread.

| Parameter | Type | Description |
|---|---|---|
| `request` | `BrzShowPaymentWebviewRequest` | Direct payment URL and optional data payload. |

---

### Events

#### `OnPaymentOptionsDialogDismissed`

```csharp
public event Action<BrzPaymentDialogDismissReason, string> OnPaymentOptionsDialogDismissed
```

Raised when the payment options dialog closes. Subscribe before calling `ShowPaymentOptionsDialog`.

| Argument | Type | Description |
|---|---|---|
| `reason` | `BrzPaymentDialogDismissReason` | How the dialog was dismissed. |
| `data` | `string` | Optional JSON payload forwarded from the request's `Data` field. |

---

#### `OnPaymentWebviewDismissed`

```csharp
public event Action<BrzPaymentWebviewDismissReason, string> OnPaymentWebviewDismissed
```

Raised when the payment web view closes. Subscribe before calling `ShowPaymentWebview`.

| Argument | Type | Description |
|---|---|---|
| `reason` | `BrzPaymentWebviewDismissReason` | How the web view was dismissed. |
| `data` | `string` | Optional JSON payload forwarded from the request's `Data` field. |

---

## BreezeConfiguration

Configuration object passed to `Breeze.Initialize`.

```csharp
public sealed class BreezeConfiguration
```

| Property | Type | Default | Description |
|---|---|---|---|
| `AppScheme` | `string` | — | **Required.** Custom URL scheme registered for your app (without `://`), e.g. `"yourgame"`. Must match your `Info.plist` / `AndroidManifest.xml` declaration. |
| `Environment` | `BreezeEnvironment` | `Production` | Backend environment the SDK connects to. Use `Development` during testing. |

---

## BreezeEnvironment

Selects the Breeze backend environment.

```csharp
public enum BreezeEnvironment
```

| Value | Integer | Description |
|---|---|---|
| `Production` | `0` | Live environment. Use for released builds. |
| `Development` | `1` | Sandbox environment. Use during development and QA. |

---

## BrzShowPaymentOptionsDialogRequest

Request object for `Breeze.ShowPaymentOptionsDialog`.

```csharp
public class BrzShowPaymentOptionsDialogRequest
```

| Property | Type | Required | Description |
|---|---|---|---|
| `Title` | `string` | No | Title shown at the top of the dialog. |
| `ProductDisplayInfo` | `BrzProductDisplayInfo` | No | Product name, pricing, and icon to display. |
| `DirectPaymentUrl` | `string` | Yes | Deep-link URL used when the user selects "Pay with Breeze". |
| `Data` | `string` | No | Opaque payload forwarded to `OnPaymentOptionsDialogDismissed`. |
| `Theme` | `BrzPaymentOptionsTheme?` | No | Dialog color theme. `null` follows system appearance. |

---

## BrzPaymentOptionsTheme

Controls the color scheme of the payment options dialog.

```csharp
public enum BrzPaymentOptionsTheme
```

| Value | JSON | Description |
|---|---|---|
| `Auto` | `"auto"` | Follows the system light/dark setting. |
| `Light` | `"light"` | Forces a light color scheme. |
| `Dark` | `"dark"` | Forces a dark color scheme. |

---

## BrzProductDisplayInfo

Product metadata rendered inside the payment options dialog.

```csharp
public class BrzProductDisplayInfo
```

| Property | Type | Description |
|---|---|---|
| `DisplayName` | `string` | Human-readable product name, e.g. `"1000 Gold Coins"`. |
| `OriginalPrice` | `string` | Regular price string shown as the crossed-out price, e.g. `"$9.99"`. |
| `BreezePrice` | `string` | Discounted Breeze price string, e.g. `"$8.99"`. |
| `Decoration` | `string` | Short promo label, e.g. `"10% off"`. |
| `ProductIconUrl` | `string` | URL of the product icon image loaded at runtime. |

---

## BrzPaymentDialogDismissReason

Reason why the payment options dialog was dismissed. Received as the first argument to `OnPaymentOptionsDialogDismissed`.

```csharp
public enum BrzPaymentDialogDismissReason : int
```

| Value | Integer | Description |
|---|---|---|
| `CloseTapped` | `0` | The user closed the dialog without selecting a payment option. |
| `DirectPaymentTapped` | `1` | The user selected the Breeze direct payment option. |
| `AppStoreTapped` | `2` | The user selected the Apple App Store IAP option. |
| `GoogleStoreTapped` | `3` | The user selected the Google Play Store IAP option. |

---

## BrzShowPaymentWebviewRequest

Request object for `Breeze.ShowPaymentWebview`.

```csharp
public class BrzShowPaymentWebviewRequest
```

| Property | Type | Required | Description |
|---|---|---|---|
| `DirectPaymentUrl` | `string` | Yes | The payment URL loaded by the web view. |
| `Data` | `string` | No | Opaque payload forwarded to `OnPaymentWebviewDismissed`. |

---

## BrzPaymentWebviewDismissReason

Reason why the payment web view was dismissed. Received as the first argument to `OnPaymentWebviewDismissed`.

```csharp
public enum BrzPaymentWebviewDismissReason : int
```

| Value | Integer | Description |
|---|---|---|
| `Dismissed` | `0` | The user dismissed the web view without completing a payment. |
| `PaymentSuccess` | `1` | The payment was completed successfully. |
| `PaymentFailure` | `2` | The payment failed or was declined. |
| `LoadError` | `3` | The web view failed to load the payment page (e.g., no network). |

---

## Delegates

### `BrzPaymentDialogDismissCallback`

```csharp
public delegate void BrzPaymentDialogDismissCallback(BrzPaymentDialogDismissReason reason, string data)
```

Native callback type used internally to bridge the platform layer to `NotifyOnPaymentOptionsDialogDismissed`. Not for direct use.

---

### `BrzPaymentWebviewDismissCallback`

```csharp
public delegate void BrzPaymentWebviewDismissCallback(BrzPaymentWebviewDismissReason reason, string data)
```

Native callback type used internally to bridge the platform layer to `NotifyOnPaymentWebviewDismissed`. Not for direct use.

---

## Internal Result Codes

These enums are returned by the native layer and are not intended for game code. Use the dismiss reason events for flow control.

### `BrzShowPaymentOptionsResultCode`

| Value | Integer | Description |
|---|---|---|
| `Success` | `0` | Dialog shown successfully. |
| `NullInput` | `1` | Null input provided. |
| `InvalidUtf8` | `2` | Request JSON contained invalid UTF-8. |
| `JsonDecodingFailed` | `3` | Request JSON could not be decoded. |

### `BrzShowPaymentWebviewResultCode`

| Value | Integer | Description |
|---|---|---|
| `Success` | `0` | Web view shown successfully. |
| `NullInput` | `1` | Null input provided. |
| `InvalidUtf8` | `2` | Request JSON contained invalid UTF-8. |
| `JsonDecodingFailed` | `3` | Request JSON could not be decoded. |
| `InvalidUrl` | `4` | The provided payment URL was malformed. |
