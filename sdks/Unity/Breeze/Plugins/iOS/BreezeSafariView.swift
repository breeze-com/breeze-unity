import Foundation
import SafariServices
import UIKit

/// Wrapper that presents SFSafariViewController and invokes onDismiss when it is closed.
class BreezeSafariView: NSObject, SFSafariViewControllerDelegate {
    private let url: URL
    private let onDismiss: () -> Void

    /// Retain self until the Safari VC is dismissed so the delegate stays alive.
    public static var activeInstance: BreezeSafariView?

    /// The presented Safari view controller; set when shown, cleared when dismissed.
    private weak var safariViewController: SFSafariViewController?

    /// Ensures onDismiss is only called once (whether user or programmatic dismiss).
    private var didInvokeDismiss = false

    init(url: URL, onDismiss: @escaping () -> Void) {
        self.url = url
        self.onDismiss = onDismiss
        super.init()
    }

    func show() {
        guard let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
            let rootViewController = windowScene.windows.first(where: { $0.isKeyWindow })?
                .rootViewController
                ?? windowScene.windows.first?.rootViewController
        else {
            onDismiss()
            return
        }

        let vc = SFSafariViewController(url: url)
        vc.delegate = self
        safariViewController = vc

        BreezeSafariView.activeInstance = self
        rootViewController.present(vc, animated: true, completion: nil)
    }

    func dismiss() {
        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }
            guard let vc = self.safariViewController else { return }
            vc.dismiss(animated: true) {
                self.invokeDismissCallbackIfNeeded()
            }
        }
    }

    private func invokeDismissCallbackIfNeeded() {
        BreezeSafariView.activeInstance = nil
        guard !didInvokeDismiss else { return }
        didInvokeDismiss = true
        onDismiss()
    }

    // MARK: - SFSafariViewControllerDelegate

    func safariViewControllerDidFinish(_ controller: SFSafariViewController) {
        invokeDismissCallbackIfNeeded()
    }
}
