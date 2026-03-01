import Foundation
import UIKit
import WebKit

// MARK: - BreezeWebView

class BreezeWebView: NSObject, WKScriptMessageHandler {

    /// Retain self until the webview is dismissed so the message handler stays alive.
    static var activeInstance: BreezeWebView?

    private let url: URL
    private let data: String?
    private let onDismiss: BrzPaymentWebviewDismissCallback?
    private var didInvokeDismiss = false
    private weak var viewController: BreezeWebViewController?

    init(url: URL, data: String?, onDismiss: BrzPaymentWebviewDismissCallback?) {
        self.url = url
        self.data = data
        self.onDismiss = onDismiss
        super.init()
    }

    // MARK: - Public API

    static func show(url: URL, data: String?, onDismiss: BrzPaymentWebviewDismissCallback?) {
        activeInstance?.dismiss()

        let instance = BreezeWebView(url: url, data: data, onDismiss: onDismiss)
        activeInstance = instance

        DispatchQueue.main.async {
            instance.present()
        }
    }

    func dismiss() {
        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }
            if let vc = self.viewController {
                vc.dismiss(animated: false) {
                    self.invokeDismissCallbackIfNeeded(reason: .dismissed, data: self.data)
                }
            } else {
                self.invokeDismissCallbackIfNeeded(reason: .dismissed, data: self.data)
            }
        }
    }

    // MARK: - Presentation

    private func present() {
        guard let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
            let rootViewController = windowScene.windows.first(where: { $0.isKeyWindow })?
                .rootViewController
                ?? windowScene.windows.first?.rootViewController
        else {
            print("Error: Could not find root view controller")
            invokeDismissCallbackIfNeeded(reason: .dismissed, data: data)
            return
        }

        let configuration = createWebViewConfiguration()
        let vc = BreezeWebViewController(
            url: url,
            configuration: configuration,
            onClose: { [weak self] in
                self?.invokeDismissCallbackIfNeeded(reason: .dismissed, data: self?.data)
            },
            onLoadError: { [weak self] error in
                print("WebView load error: \(error.localizedDescription)")
                self?.invokeDismissCallbackIfNeeded(reason: .loadError, data: self?.data)
            },
            onPaymentRedirect: { [weak self] reason in
                self?.handleDismissWithReason(reason, jsData: nil)
            }
        )
        self.viewController = vc
        rootViewController.present(vc, animated: false, completion: nil)
    }

    // MARK: - WebView Configuration

    private func createWebViewConfiguration() -> WKWebViewConfiguration {
        let configuration = WKWebViewConfiguration()
        let contentController = WKUserContentController()

        for name in BreezeConstants.MessageHandler.all {
            contentController.add(self, name: name)
        }

        let userScript = WKUserScript(
            source: createBridgeJavaScript(),
            injectionTime: .atDocumentStart,
            forMainFrameOnly: true
        )
        contentController.addUserScript(userScript)

        configuration.userContentController = contentController
        return configuration
    }

    static func isAllowedHost(_ hostname: String) -> Bool {
        let lower = hostname.lowercased()
        return BreezeConstants.allowedHosts.contains { lower.hasSuffix($0) }
    }

    private func createBridgeJavaScript() -> String {
        let allowedSuffixesJSON = (try? JSONSerialization.data(
            withJSONObject: BreezeConstants.allowedHosts, options: []
        )).flatMap { String(data: $0, encoding: .utf8) } ?? "[]"
        let deviceInfoJSON = BreezeNative.instance.getDeviceInfoJSON()

        return """
            (function() {
                var allowedSuffixes = \(allowedSuffixesJSON);
                var hostname = window.location.hostname.toLowerCase();
                var allowed = allowedSuffixes.some(function(suffix) {
                    return hostname.endsWith(suffix);
                });
                if (!allowed) return;

                window._breeze = {
                    version: "\(BreezeConstants.sdkVersion)",
                    platform: "\(BreezeConstants.sdkPlatform)",

                    getDeviceInfo: function() {
                        return \(deviceInfoJSON);
                    },

                    onPaymentSuccess: function(data) {
                        window.webkit.messageHandlers.\(BreezeConstants.MessageHandler.onPaymentSuccess).postMessage(
                            data !== undefined ? String(data) : ""
                        );
                    },
                    onPaymentFailure: function(data) {
                        window.webkit.messageHandlers.\(BreezeConstants.MessageHandler.onPaymentFailure).postMessage(
                            data !== undefined ? String(data) : ""
                        );
                    },
                    dismiss: function() {
                        window.webkit.messageHandlers.\(BreezeConstants.MessageHandler.dismiss).postMessage("");
                    }
                };
            })();
            """
    }

    // MARK: - WKScriptMessageHandler

    func userContentController(
        _ userContentController: WKUserContentController,
        didReceive message: WKScriptMessage
    ) {
        switch message.name {
        case BreezeConstants.MessageHandler.onPaymentSuccess:
            let body = message.body as? String
            handleDismissWithReason(.paymentSuccess, jsData: body)
        case BreezeConstants.MessageHandler.onPaymentFailure:
            let body = message.body as? String
            handleDismissWithReason(.paymentFailure, jsData: body)
        case BreezeConstants.MessageHandler.dismiss:
            handleDismissWithReason(.dismissed, jsData: nil)
        case BreezeConstants.MessageHandler.getDeviceInfo:
            // getDeviceInfo is handled synchronously in JS; no native round-trip needed.
            break
        default:
            print("Warning: Unknown message handler: \(message.name)")
        }
    }

    // MARK: - Dismiss Handling

    private func handleDismissWithReason(
        _ reason: BrzPaymentWebviewDismissReason, jsData: String?
    ) {
        let callbackData = jsData ?? self.data
        if let vc = self.viewController {
            vc.dismiss(animated: false) {
                self.invokeDismissCallbackIfNeeded(reason: reason, data: callbackData)
            }
        } else {
            invokeDismissCallbackIfNeeded(reason: reason, data: callbackData)
        }
    }

    private func invokeDismissCallbackIfNeeded(
        reason: BrzPaymentWebviewDismissReason, data: String?
    ) {
        BreezeWebView.activeInstance = nil
        guard !didInvokeDismiss else { return }
        didInvokeDismiss = true
        cleanup()
        onDismiss?(reason, data)
    }

    // MARK: - Cleanup

    private func cleanup() {
        viewController?.cleanup()
    }
}

// MARK: - BreezeWebViewController

class BreezeWebViewController: UIViewController, WKNavigationDelegate {

    private let url: URL
    private let webViewConfiguration: WKWebViewConfiguration
    private let onClose: () -> Void
    private let onLoadError: (Error) -> Void
    private let onPaymentRedirect: (BrzPaymentWebviewDismissReason) -> Void
    private var webView: WKWebView?

    // Dialog layout
    private let cornerRadius: CGFloat = 16
    private let handleBarHeight: CGFloat = 48
    private let minHeightRatio: CGFloat = 0.35
    private let maxHeightRatio: CGFloat = 0.95
    private let defaultHeightRatio: CGFloat = 0.90

    private var containerView: UIView!
    private var containerHeightConstraint: NSLayoutConstraint!
    private var dragStartHeight: CGFloat = 0

    init(
        url: URL,
        configuration: WKWebViewConfiguration,
        onClose: @escaping () -> Void,
        onLoadError: @escaping (Error) -> Void,
        onPaymentRedirect: @escaping (BrzPaymentWebviewDismissReason) -> Void
    ) {
        self.url = url
        self.webViewConfiguration = configuration
        self.onClose = onClose
        self.onLoadError = onLoadError
        self.onPaymentRedirect = onPaymentRedirect
        super.init(nibName: nil, bundle: nil)
        modalPresentationStyle = .overCurrentContext
        modalTransitionStyle = .crossDissolve
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .clear
        setupDimBackground()
        setupContainer()
        setupHandleBar()
        setupWebView()
        loadURL()
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        animatePresentation()
    }

    // MARK: - Dim Background

    private func setupDimBackground() {
        let dimView = UIView()
        dimView.backgroundColor = UIColor.black.withAlphaComponent(0.4)
        dimView.translatesAutoresizingMaskIntoConstraints = false
        dimView.tag = 999
        view.addSubview(dimView)

        NSLayoutConstraint.activate([
            dimView.topAnchor.constraint(equalTo: view.topAnchor),
            dimView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            dimView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            dimView.bottomAnchor.constraint(equalTo: view.bottomAnchor),
        ])

        let tapGesture = UITapGestureRecognizer(target: self, action: #selector(dimBackgroundTapped))
        dimView.addGestureRecognizer(tapGesture)
    }

    @objc private func dimBackgroundTapped() {
        animateDismissal {
            self.dismiss(animated: false) {
                self.onClose()
            }
        }
    }

    // MARK: - Container

    private func setupContainer() {
        containerView = UIView()
        containerView.backgroundColor = .systemBackground
        containerView.layer.cornerRadius = cornerRadius
        containerView.layer.maskedCorners = [.layerMinXMinYCorner, .layerMaxXMinYCorner]
        containerView.clipsToBounds = true
        containerView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(containerView)

        let defaultHeight = view.bounds.height * defaultHeightRatio
        containerHeightConstraint = containerView.heightAnchor.constraint(equalToConstant: defaultHeight)

        NSLayoutConstraint.activate([
            containerView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            containerView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            containerView.bottomAnchor.constraint(equalTo: view.bottomAnchor),
            containerHeightConstraint,
        ])

        // Start off-screen for animation
        containerView.transform = CGAffineTransform(translationX: 0, y: view.bounds.height)
    }

    // MARK: - Handle Bar (drag area + close button)

    private func setupHandleBar() {
        let handleBar = UIView()
        handleBar.backgroundColor = .secondarySystemBackground
        handleBar.translatesAutoresizingMaskIntoConstraints = false
        containerView.addSubview(handleBar)

        NSLayoutConstraint.activate([
            handleBar.topAnchor.constraint(equalTo: containerView.topAnchor),
            handleBar.leadingAnchor.constraint(equalTo: containerView.leadingAnchor),
            handleBar.trailingAnchor.constraint(equalTo: containerView.trailingAnchor),
            handleBar.heightAnchor.constraint(equalToConstant: handleBarHeight),
        ])

        // Drag indicator pill
        let pill = UIView()
        pill.backgroundColor = UIColor.tertiaryLabel
        pill.layer.cornerRadius = 2.5
        pill.translatesAutoresizingMaskIntoConstraints = false
        handleBar.addSubview(pill)

        NSLayoutConstraint.activate([
            pill.centerXAnchor.constraint(equalTo: handleBar.centerXAnchor),
            pill.topAnchor.constraint(equalTo: handleBar.topAnchor, constant: 8),
            pill.widthAnchor.constraint(equalToConstant: 36),
            pill.heightAnchor.constraint(equalToConstant: 5),
        ])

        // Close button
        let closeButton = UIButton(type: .system)
        closeButton.setImage(UIImage(systemName: "xmark.circle.fill"), for: .normal)
        closeButton.tintColor = .secondaryLabel
        closeButton.addTarget(self, action: #selector(closeTapped), for: .touchUpInside)
        closeButton.translatesAutoresizingMaskIntoConstraints = false
        handleBar.addSubview(closeButton)

        NSLayoutConstraint.activate([
            closeButton.centerYAnchor.constraint(equalTo: handleBar.centerYAnchor),
            closeButton.trailingAnchor.constraint(equalTo: handleBar.trailingAnchor, constant: -12),
            closeButton.widthAnchor.constraint(equalToConstant: 32),
            closeButton.heightAnchor.constraint(equalToConstant: 32),
        ])

        // Separator line at bottom of handle bar
        let separator = UIView()
        separator.backgroundColor = .separator
        separator.translatesAutoresizingMaskIntoConstraints = false
        handleBar.addSubview(separator)

        NSLayoutConstraint.activate([
            separator.leadingAnchor.constraint(equalTo: handleBar.leadingAnchor),
            separator.trailingAnchor.constraint(equalTo: handleBar.trailingAnchor),
            separator.bottomAnchor.constraint(equalTo: handleBar.bottomAnchor),
            separator.heightAnchor.constraint(equalToConstant: 1.0 / UIScreen.main.scale),
        ])

        // Pan gesture for resizing
        let panGesture = UIPanGestureRecognizer(target: self, action: #selector(handlePan(_:)))
        handleBar.addGestureRecognizer(panGesture)
    }

    // MARK: - Drag to Resize

    @objc private func handlePan(_ gesture: UIPanGestureRecognizer) {
        let translation = gesture.translation(in: view)
        let screenHeight = view.bounds.height
        let minHeight = screenHeight * minHeightRatio
        let maxHeight = screenHeight * maxHeightRatio

        switch gesture.state {
        case .began:
            dragStartHeight = containerHeightConstraint.constant
        case .changed:
            // Dragging up (negative translation) increases height
            let newHeight = dragStartHeight - translation.y
            containerHeightConstraint.constant = min(max(newHeight, minHeight), maxHeight)
        case .ended, .cancelled:
            // Snap: if dragged below minimum threshold, dismiss
            let velocity = gesture.velocity(in: view).y
            if velocity > 1000 || containerHeightConstraint.constant < minHeight * 0.8 {
                animateDismissal {
                    self.dismiss(animated: false) {
                        self.onClose()
                    }
                }
            } else {
                // Smooth settle
                UIView.animate(withDuration: 0.2, delay: 0, options: .curveEaseOut) {
                    self.view.layoutIfNeeded()
                }
            }
        default:
            break
        }
    }

    // MARK: - Animations

    private func animatePresentation() {
        UIView.animate(
            withDuration: 0.35,
            delay: 0,
            usingSpringWithDamping: 0.85,
            initialSpringVelocity: 0.5,
            options: .curveEaseOut
        ) {
            self.containerView.transform = .identity
            self.view.viewWithTag(999)?.alpha = 1
        }
    }

    private func animateDismissal(completion: @escaping () -> Void) {
        UIView.animate(withDuration: 0.25, delay: 0, options: .curveEaseIn, animations: {
            self.containerView.transform = CGAffineTransform(
                translationX: 0, y: self.containerView.bounds.height)
            self.view.viewWithTag(999)?.alpha = 0
        }) { _ in
            completion()
        }
    }

    // MARK: - WebView

    private func setupWebView() {
        let wv = WKWebView(frame: .zero, configuration: webViewConfiguration)
        wv.navigationDelegate = self
        wv.translatesAutoresizingMaskIntoConstraints = false
        wv.allowsBackForwardNavigationGestures = false
        wv.layer.cornerRadius = cornerRadius
        wv.layer.maskedCorners = [.layerMinXMinYCorner, .layerMaxXMinYCorner]
        wv.clipsToBounds = true
        if #available(iOS 16.4, *) {
            wv.isInspectable = true
        } else {
            // Fallback on earlier versions
        }
        containerView.addSubview(wv)

        NSLayoutConstraint.activate([
            wv.topAnchor.constraint(equalTo: containerView.topAnchor, constant: handleBarHeight),
            wv.leadingAnchor.constraint(equalTo: containerView.leadingAnchor),
            wv.trailingAnchor.constraint(equalTo: containerView.trailingAnchor),
            wv.bottomAnchor.constraint(equalTo: containerView.bottomAnchor),
        ])

        self.webView = wv
    }

    private func loadURL() {
        print("BreezeWebView: loading url=\(url.absoluteString)")
        webView?.load(URLRequest(url: url))
    }

    @objc private func closeTapped() {
        animateDismissal {
            self.dismiss(animated: false) {
                self.onClose()
            }
        }
    }

    // MARK: - Cleanup

    func cleanup() {
        guard let wv = webView else { return }
        wv.stopLoading()
        wv.navigationDelegate = nil

        let contentController = wv.configuration.userContentController
        contentController.removeAllUserScripts()
        for name in BreezeConstants.MessageHandler.all {
            contentController.removeScriptMessageHandler(forName: name)
        }

        webView = nil
    }

    // MARK: - WKNavigationDelegate

    func webView(
        _ webView: WKWebView,
        decidePolicyFor navigationAction: WKNavigationAction,
        decisionHandler: @escaping (WKNavigationActionPolicy) -> Void
    ) {
        guard let requestURL = navigationAction.request.url else {
            decisionHandler(.cancel)
            return
        }

        let scheme = requestURL.scheme?.lowercased() ?? ""
        print("BreezeWebView: navigating to \(requestURL.absoluteString)")

        // Allow normal web navigation
        if scheme == "http" || scheme == "https" || scheme == "about" {
            decisionHandler(.allow)
            return
        }

        // Intercept custom-scheme payment redirects
        decisionHandler(.cancel)

        let host = requestURL.host?.lowercased() ?? ""
        let path = requestURL.path

        if host == BreezeConstants.PaymentRedirect.host {
            if path == BreezeConstants.PaymentRedirect.successPath {
                onPaymentRedirect(.paymentSuccess)
            } else if path == BreezeConstants.PaymentRedirect.failurePath {
                onPaymentRedirect(.paymentFailure)
            } else {
                print("BreezeWebView: unknown payment redirect path: \(path)")
                onPaymentRedirect(.dismissed)
            }
            return
        }

        // Other custom schemes (tel:, mailto:, etc.) — open with system
        print("BreezeWebView: opening external URL: \(requestURL.absoluteString)")
        UIApplication.shared.open(requestURL, options: [:], completionHandler: nil)
    }

    func webView(
        _ webView: WKWebView,
        didFailProvisionalNavigation navigation: WKNavigation!,
        withError error: Error
    ) {
        let nsError = error as NSError
        let failingURL = nsError.userInfo[NSURLErrorFailingURLStringErrorKey] ?? webView.url?.absoluteString ?? "unknown"
        print("BreezeWebView: didFailProvisionalNavigation url=\(failingURL) error=\(nsError.code) \(nsError.localizedDescription)")
        if nsError.code == NSURLErrorCancelled { return }
        onLoadError(error)
    }

    func webView(
        _ webView: WKWebView,
        didFail navigation: WKNavigation!,
        withError error: Error
    ) {
        let nsError = error as NSError
        let failingURL = nsError.userInfo[NSURLErrorFailingURLStringErrorKey] ?? webView.url?.absoluteString ?? "unknown"
        print("BreezeWebView: didFail url=\(failingURL) error=\(nsError.code) \(nsError.localizedDescription)")
        if nsError.code == NSURLErrorCancelled { return }
        onLoadError(error)
    }
}
