# Breeze Payment Unity SDK

[![Unity](https://img.shields.io/badge/Unity-6.3%2B-blue.svg)](https://unity3d.com/)
[![Version](https://img.shields.io/badge/version-1.0.0-green.svg)](https://github.com/breeze-com/breeze-unity)

The Breeze Payment Unity SDK enables seamless payment integration for Unity games on iOS and Android platforms. Show native payment option dialogs, handle payment flows, and provide a smooth checkout experience for your players.

## Features

- 🎮 **Native Integration** - Seamlessly integrated with Unity for iOS and Android
- 💳 **Payment Options Dialog** - Display native payment option dialogs with customizable product information
- 🛠️ **Easy Setup** - Simple installation via Unity Package Manager

## Requirements

- Unity 6.3 LTS or later
- iOS 15.0+ (for iOS builds)
- .NET Standard 2.1 or .NET Framework 4.8

## Installation

### Unity Package Manager (Recommended)

1. Open your Unity project
2. Open **Window** → **Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package from git URL**
5. Paste the following URL:
   ```
   https://github.com/breeze-com/breeze-unity.git?path=sdks/Unity/Breeze
   ```
6. Click **Add**

The SDK will be automatically installed along with its dependencies (Newtonsoft.Json).

## Quick Start

### 1. Initialize the SDK

Initialize Breeze in your game's startup code (e.g., in a MonoBehaviour's `Start()` method):

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        Breeze.Initialize(new BreezeConfiguration()
        {
            AppScheme = "mygame://",  // Your app's custom URL scheme
            Environment = BreezeEnvironment.Production,  // or BreezeEnvironment.Development
        });
    }
}
```

### 2. Show Payment Options Dialog

Display a payment options dialog when the user wants to make a purchase:

```csharp
void ShowPaymentDialog()
{
    var request = new BrzShowPaymentOptionsDialogRequest()
    {
        Title = "Select payment method",
        ProductDisplayInfo = new BrzProductDisplayInfo()
        {
            DisplayName = "Premium Pack",
            OriginalPrice = "USD $9.99",
            BreezePrice = "USD $7.99",
            Decoration = "Save 20%",
            // your product icon url
            ProductIconUrl = "",
        },
        DirectPaymentUrl = "https://link.breeze.cash/link/your-payment-link",
        Data = "product-id-123",  // Optional: pass custom data
    };

    // Subscribe to dismissal events
    Breeze.Instance.OnPaymentOptionsDialogDismissed += OnPaymentDialogDismissed;
    
    // Show the dialog
    Breeze.Instance.ShowPaymentOptionsDialog(request);
}

void OnPaymentDialogDismissed(BrzPaymentDialogDismissReason reason, string data)
{
    Debug.Log($"Payment dialog dismissed. Reason: {reason}, Data: {data}");
    
    switch (reason)
    {
        case BrzPaymentDialogDismissReason.CloseTapped:
            // User closed the dialog
            break;
        case BrzPaymentDialogDismissReason.DirectPaymentTapped:
            // User selected direct payment - handle redirect
            break;
        case BrzPaymentDialogDismissReason.AppStoreTapped:
            // User selected App Store payment
            break;
    }
    
    // Unsubscribe from events
    Breeze.Instance.OnPaymentOptionsDialogDismissed -= OnPaymentDialogDismissed;
}
```

## Create Payment URL and Handle Deeplink

### 1. Set up the deeplink scheme in your app

Breeze uses the following deeplink format:
- `<your-app-scheme>://breeze-payment/purchase/success`
- `<your-app-scheme>://breeze-payment/purchase/failure` 

**Remember to replace the `<your-app-scheme>` to your app's deeplink scheme.**

For Unity, follow this guide to setup the deeplink:
- https://docs.unity3d.com/6000.3/Documentation/Manual/deep-linking-ios.html
- After you've generated the `Unity-iPhone.xcodeproj`
    - Open the project with Xcode
    - Double check the `Info.plist`
    - Make sure the `URL Types > Item 0 > URL Schemes > Item 0` contains `<your-app-scheme>`, e.g. in our demo, it should be `breezedemo` (**without** the `://` suffix)

If you want to test your deeplink on iPhone simulator:
- Make sure your app is installed in the iPhone simulator
- Run the following command from macOS terminal:
    - `xcrun simctl openurl booted "<your-app-scheme>://breeze-payment/purchase/success"`


### 2. Create a payment page URL on your server with the correct redirect URL

To create a payment page url, follow this document:
- Create a Payment Page: https://docs.breeze.cash/docs/quick-start#3-create-a-payment-page
- Note, the `successReturnUrl` and `failReturnUrl` should be the deeplink urls:
    - `"successReturnUrl": "<your-app-scheme>://breeze-payment/purchase/success"`
    - `"failReturnUrl": "<your-app-scheme>://breeze-payment/purchase/failure"`

### 3. Handle the deeplinks and verify the purchase on your server

1. register the deeplink handler in Unity:
    - `Application.deepLinkActivated += this.OnPaymentPageResult;`
2. in the `OnPaymentPageResult` handler:
    - Dimiss the payment page: `Breeze.Instance.DismissPaymentPageView();`
    - Verify the payment result from your server

## Example Implementation

A complete example implementation can be found in the demo project:

[examples/UnityBreezeDemo/Assets/BreezeDemo/ShowPaymentOptionsDialogUI.cs](./examples/UnityBreezeDemo/Assets/BreezeDemo/ShowPaymentOptionsDialogUI.cs)

This example demonstrates:
- SDK initialization
- UI integration with Unity UI Toolkit
- Payment dialog display
- Event handling for dialog dismissal

## Troubleshooting

### SDK Not Initialized

If you see warnings about the SDK not being initialized, ensure you call `Breeze.Initialize()` before using any SDK methods.

### Payment Dialog Not Appearing

- Verify that `Breeze.Initialize()` was called successfully
- Ensure you're running on a physical device or simulator (not all features work in the Unity Editor)

## Support

For issues, questions, or feature requests:

- **Email:** support@breeze.cash
- **Website:** https://breeze.cash
- **GitHub Issues:** [Create an issue](https://github.com/breeze-com/breeze-unity/issues)

---

Made with ❤️ by [Breeze](https://breeze.cash)
