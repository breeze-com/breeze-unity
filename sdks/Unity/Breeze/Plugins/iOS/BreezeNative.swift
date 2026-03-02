import Foundation
import UIKit

struct BreezeDeviceInfo: Codable {
    let deviceId: String
}

public class BreezeNative {
    public static let instance = BreezeNative()

    private init() {
    }

    private func getDeviceId() -> String {
        return UIDevice.current.identifierForVendor?.uuidString ?? ""
    }

    func getDeviceInfo() -> BreezeDeviceInfo {
        return BreezeDeviceInfo(deviceId: getDeviceId())
    }

    func getDeviceInfoJSON() -> String {
        let info = getDeviceInfo()
        guard let data = try? JSONEncoder().encode(info),
            let json = String(data: data, encoding: .utf8)
        else {
            return "{}"
        }
        return json
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

    public func brzShowPaymentWebview(
        jsonRequest: UnsafePointer<CChar>?,
        onDismiss: BrzPaymentWebviewDismissCallback? = nil
    ) -> BrzShowPaymentWebviewResultCode {
        guard let jsonString = jsonRequest else {
            print("Error: jsonString is nil")
            return .nullInput
        }

        let json = String(cString: jsonString)

        guard let jsonData = json.data(using: .utf8) else {
            print("Error: Could not convert JSON string to data")
            return .invalidUtf8
        }

        let decoder = JSONDecoder()
        do {
            let request = try decoder.decode(
                BrzShowPaymentWebviewRequest.self, from: jsonData)

            guard let urlString = request.directPaymentUrl,
                let url = URL(string: urlString),
                let host = url.host?.lowercased(),
                BreezeConstants.allowedHosts.contains(where: { host.hasSuffix($0) })
            else {
                print("Warning: Direct payment URL is invalid or not an allowed Breeze domain")
                return .invalidUrl
            }

            BreezeWebView.show(url: url, data: request.data, onDismiss: onDismiss)
            return .success
        } catch {
            print("Error decoding JSON: \(error)")
            return .jsonDecodingFailed
        }
    }

    public func brzDismissPaymentWebview() {
        BreezeWebView.activeInstance?.dismiss()
    }
}
