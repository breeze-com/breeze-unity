import Foundation
import UIKit

public class BreezeNative {
    public static let instance = BreezeNative()

    private init() {
    }

    private func getDeviceId() -> String {
        return UIDevice.current.identifierForVendor?.uuidString ?? ""
    }

    private func copyDataToBuffer(
        _ data: Data?, _ buffer: UnsafeMutablePointer<UInt8>?, _ bufferLength: UInt32
    ) -> UInt32 {
        let requiredLength = UInt32(data?.count ?? 0)
        if buffer == nil || bufferLength == 0 || requiredLength == 0 {
            return requiredLength
        }
        let bytesToCopy = Int(min(bufferLength, requiredLength))
        if bytesToCopy > 0 {
            data?.copyBytes(to: buffer!, count: bytesToCopy)
        }
        return requiredLength
    }

    public func brzGetDeviceId(
        deviceIdBuffer: UnsafeMutablePointer<UInt8>?, deviceIdLength: UInt32
    ) -> UInt32 {
        let deviceId: String = BreezeNative.instance.getDeviceId()
        let deviceIdData: Data? = deviceId.data(using: .utf8)
        return self.copyDataToBuffer(deviceIdData, deviceIdBuffer, deviceIdLength)
    }

    public func brzShowPaymentOptionsDialog(
        jsonRequest: UnsafePointer<CChar>?,
        onDismiss: BrzPaymentDialogDismissCallback? = nil
    )
        -> BrzShowPaymentOptionsResultCode
    {
        guard let jsonString = jsonRequest else {
            print("Error: jsonString is nil")
            return BrzShowPaymentOptionsResultCode.nullInput
        }

        let json = String(cString: jsonString)

        // Parse JSON
        guard let jsonData = json.data(using: .utf8) else {
            print("Error: Could not convert JSON string to data")
            return BrzShowPaymentOptionsResultCode.invalidUtf8
        }

        let decoder = JSONDecoder()
        do {
            let request = try decoder.decode(
                BrzShowPaymentOptionsDialogRequest.self, from: jsonData)
            showPaymentOptionsDialog(request: request, onDismiss: onDismiss)
            return BrzShowPaymentOptionsResultCode.success
        } catch {
            print("Error decoding JSON: \(error)")
            return BrzShowPaymentOptionsResultCode.jsonDecodingFailed
        }
    }

    public func brzDismissPaymentPageView() {
        BreezeSafariView.activeInstance?.dismiss();
    }
}
