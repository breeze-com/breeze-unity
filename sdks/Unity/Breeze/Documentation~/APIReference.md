# Breeze Payment SDK — API Reference

**Package:** `com.breeze.payment-unity`  
**Namespace:** `BreezeSdk.Runtime`  
**Platforms:** iOS 15.0+, Android API 21+  

---

## Table of Contents

1. [Breeze](#breeze)
2. [BreezeConfiguration](#breezeconfiguration)
3. [BreezeEnvironment](#breezeenvironment)
4. [BreezeRuntimeSettings](#breezeruntimesettings)
5. [BrzShowPaymentOptionsDialogRequest](#brzshowpaymentoptionsdialogRequest)
6. [BrzPaymentOptionsTheme](#brzpaymentoptionstheme)
7. [BrzProductDisplayInfo](#brzproductdisplayinfo)
8. [BrzPaymentDialogDismissReason](#brzpaymentdialogdismissreason)
9. [BrzShowPaymentWebviewRequest](#brzshowpaymentwebviewrequest)
10. [BrzPaymentWebviewDismissReason](#brzpaymentwebviewdismissreason)
11. [Delegates](#delegates)
12. [Internal Result Codes](#internal-result-codes)

---

## Breeze

The main entry point for the Breeze Payment SDK. A singleton — initialize once and access via `Breeze.Instance`.

### Static Methods

#### `Initialize()`

Initializes the SDK singleton using settings from the Breeze Setup editor window. The `AppScheme` is loaded automatically from the `BreezeRuntimeSettings` asset. Must be called before accessing `Instance` or invoking any payment methods. Logs a warning and returns immediately if already initialized.

**Throws:** `ArgumentException` — when the `BreezeRuntimeSettings` asset is missing or `AppScheme` is empty.

---

#### `Initialize(BreezeConfiguration configuration)`

Initializes the SDK singleton with the provided configuration. If `AppScheme` is not set on the configuration, it is loaded automatically from the `BreezeRuntimeSettings` asset. Must be called before accessing `Instance` or invoking any payment methods. Logs a warning and returns immediately if already initialized.

| Parameter | Type | Description |
|---|---|---|
| `configuration` | `BreezeConfiguration` | SDK configuration. If `AppScheme` is empty, it is read from editor settings. |

**Throws:** `ArgumentException` — when `configuration` is `null` or `AppScheme` cannot be resolved.

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

### Instance Properties

#### `AppScheme`

```csharp
public string AppScheme { get; }
```

Returns the custom URL scheme used by this SDK instance (e.g. `"yourgame"`).

---

#### `SuccessReturnUrl`

```csharp
public string SuccessReturnUrl { get; }
```

The deep-link URL that the Breeze payment page redirects to on a successful purchase (e.g. `yourgame://breeze-payment/purchase/success`). Pass this as `successReturnUrl` when creating a payment page on your server.

---

#### `FailureReturnUrl`

```csharp
public string FailureReturnUrl { get; }
```

The deep-link URL that the Breeze payment page redirects to on a failed or cancelled purchase (e.g. `yourgame://breeze-payment/purchase/failure`). Pass this as `failReturnUrl` when creating a payment page on your server.

---

### Instance Methods

#### `GetDeviceUniqueId()`

```csharp
public string GetDeviceUniqueId()
```

Returns a stable device-unique identifier. On iOS this is the vendor identifier (IDFV); on Android it is the Android ID.

**Returns:** A non-empty string.

---

#### `IsPaymentSuccessUrl(string url)`

```csharp
public bool IsPaymentSuccessUrl(string url)
```

Checks whether the given deep-link URL represents a successful payment redirect. The URL alone does not guarantee the payment succeeded — always verify the result on your server.

| Parameter | Type | Description |
|---|---|---|
| `url` | `string` | The deep-link URL received via `Application.deepLinkActivated`. |

**Returns:** `true` if the URL matches the Breeze payment success pattern; otherwise `false`.

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
| `AppScheme` | `string` | — | Custom URL scheme registered for your app (without `://`), e.g. `"yourgame"`. If empty, loaded automatically from `BreezeRuntimeSettings` (configured via **Tools → Breeze → Setup**). |
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

## BreezeRuntimeSettings

ScriptableObject that stores Breeze SDK configuration so it can be loaded at runtime via `Resources.Load`. Managed automatically by the Breeze Setup editor window (**Tools → Breeze → Setup**) — you should not need to edit this asset by hand.

```csharp
public class BreezeRuntimeSettings : ScriptableObject
```

### Properties

| Property | Type | Description |
|---|---|---|
| `AppScheme` | `string` | The custom URL scheme configured via the Breeze Setup window (e.g. `"yourgame"`). |
| `Environment` | `BreezeEnvironment` | The Breeze backend environment. Defaults to `Production`. |

### Static Methods

#### `Load()`

```csharp
public static BreezeRuntimeSettings Load()
```

Loads the settings asset from `Resources`. Returns `null` when the asset has not been created yet (i.e. Breeze Setup was never saved).

### Asset Paths

| Constant | Value | Description |
|---|---|---|
| `AssetDir` | `Assets/Breeze/Resources` | Directory where the asset is stored. |
| `AssetPath` | `Assets/Breeze/Resources/BreezeRuntimeSettings.asset` | Full path used by the editor. |

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
