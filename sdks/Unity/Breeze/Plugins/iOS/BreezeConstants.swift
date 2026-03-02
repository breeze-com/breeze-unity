import Foundation

enum BreezeConstants {
    static let sdkVersion = "0.1.0"
    static let sdkPlatform = "ios"

    /// Domains allowed for webview navigation and JS bridge injection.
    static let allowedHosts = [".breeze.cash", ".breeze.com"]

    /// Host and paths for custom-scheme payment redirects.
    enum PaymentRedirect {
        static let host = "breeze-payment"
        static let successPath = "/purchase/success"
        static let failurePath = "/purchase/failure"
    }

    /// WKScriptMessageHandler names (must match JS bridge property names).
    enum MessageHandler {
        static let onPaymentSuccess = "onPaymentSuccess"
        static let onPaymentFailure = "onPaymentFailure"
        static let dismiss = "dismiss"
        static let getDeviceInfo = "getDeviceInfo"

        static let all = [onPaymentSuccess, onPaymentFailure, dismiss, getDeviceInfo]
    }
}
