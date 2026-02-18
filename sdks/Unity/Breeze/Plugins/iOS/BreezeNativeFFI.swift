import Foundation
import UIKit

@_cdecl("brz_get_device_id")
public func brz_get_device_id(
    _ deviceIdBuffer: UnsafeMutablePointer<UInt8>?, _ deviceIdLength: UInt32
) -> UInt32 {
    return BreezeNative.instance.brzGetDeviceId(
        deviceIdBuffer: deviceIdBuffer, deviceIdLength: deviceIdLength)
}

public func freeCString(_ ptr: UnsafeMutablePointer<Int8>?) {
    free(ptr)
}

// the pointer returned by this function must be freed by calling brz_free_string
private func toCString(_ str: String?) -> UnsafeMutablePointer<Int8>? {
    guard let str = str else { return nil }
    return str.withCString { strdup($0) }
}

// Data models matching the C# JSON structure
struct BrzProductDisplayInfo: Codable {
    let displayName: String
    let originalPrice: String
    let breezePrice: String
    let decoration: String?
    let productIconUrl: String?
}

/// Theme for the payment options dialog. Codable with JSON values "light" or "dark".
enum BrzPaymentOptionsTheme: String, Codable {
    case auto
    case light
    case dark
}

struct BrzShowPaymentOptionsDialogRequest: Codable {
    let title: String
    let product: BrzProductDisplayInfo
    let directPaymentUrl: String?
    let data: String?
    let theme: BrzPaymentOptionsTheme?
}

public enum BrzPaymentDialogDismissReason: Int32 {
    case closeTapped = 0
    case directPaymentTapped = 1
    case appStoreTapped = 2
}

public enum BrzShowPaymentOptionsResultCode: Int32 {
    case success = 0
    case nullInput = 1
    case invalidUtf8 = 2
    case jsonDecodingFailed = 3
}

public typealias BrzShowPaymentOptionsResultCodeC = Int32

// C callback type for payment dialog dismissal (uses Int32 for C interop)
public typealias BrzPaymentDialogDismissCallbackC =
    @convention(c) (Int32, UnsafePointer<CChar>?) -> Void

// Swift callback type using the enum
public typealias BrzPaymentDialogDismissCallback =
    (BrzPaymentDialogDismissReason, String?) -> Void

@_cdecl("brz_show_payment_options_dialog")
public func brz_show_payment_options_dialog(
    // JSON string of BrzShowPaymentOptionsDialogRequest
    _ jsonRequest: UnsafePointer<CChar>?,
    _ onDismiss: BrzPaymentDialogDismissCallbackC?
) -> BrzShowPaymentOptionsResultCodeC {
    // Convert C callback (Int32) to Swift callback (enum)
    let swiftCallback: BrzPaymentDialogDismissCallback? = onDismiss.map { cCallback in
        return { reason, data in
            let dataPtr = toCString(data)
            defer {
                freeCString(dataPtr)
            }
            cCallback(reason.rawValue, dataPtr)
        }
    }

    return BreezeNative.instance.brzShowPaymentOptionsDialog(
        jsonRequest: jsonRequest,
        onDismiss: swiftCallback
    ).rawValue
}

@_cdecl("brz_dismiss_payment_page_view")
public func brz_dismiss_payment_page_view() {
    BreezeNative.instance.brzDismissPaymentPageView()
}
