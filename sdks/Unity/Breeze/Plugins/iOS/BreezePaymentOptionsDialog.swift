import Foundation
import StoreKit
import UIKit

// MARK: - Theme

enum PaymentDialogTheme: Int32 {
    case light = 0
    case dark = 1

    var colors: PaymentDialogThemeColors {
        switch self {
        case .light: return .light
        case .dark: return .dark
        }
    }
}

struct PaymentDialogThemeColors {
    let overlay: UIColor
    let contentBackground: UIColor
    let titleText: UIColor
    let closeButton: UIColor
    let productCardBackground: UIColor
    let imagePlaceholder: UIColor
    let primaryText: UIColor
    let mutedText: UIColor
    let primaryButtonBackground: UIColor
    let primaryButtonText: UIColor
    let badgeText: UIColor
    let badgeBackground: UIColor
    let secondaryButtonBackground: UIColor
    let secondaryButtonText: UIColor
    let footerMutedText: UIColor
    let footerAccentText: UIColor

    static let light = PaymentDialogThemeColors(
        overlay: UIColor.black.withAlphaComponent(0.5),
        contentBackground: .white,
        titleText: UIColor(red: 0.2, green: 0.2, blue: 0.2, alpha: 1.0),
        closeButton: UIColor(red: 0.4, green: 0.4, blue: 0.4, alpha: 1.0),
        productCardBackground: UIColor(red: 0.95, green: 0.95, blue: 0.95, alpha: 1.0),
        imagePlaceholder: UIColor(red: 1.0, green: 0.65, blue: 0.0, alpha: 1.0),
        primaryText: UIColor(red: 0.2, green: 0.2, blue: 0.2, alpha: 1.0),
        mutedText: UIColor(red: 0.6, green: 0.6, blue: 0.6, alpha: 1.0),
        primaryButtonBackground: UIColor(red: 0.0, green: 0.48, blue: 1.0, alpha: 1.0),
        primaryButtonText: .white,
        badgeText: .black,
        badgeBackground: UIColor(red: 1.0, green: 0.84, blue: 0.0, alpha: 1.0),
        secondaryButtonBackground: UIColor(red: 0.9, green: 0.9, blue: 0.9, alpha: 1.0),
        secondaryButtonText: UIColor(red: 0.2, green: 0.2, blue: 0.2, alpha: 1.0),
        footerMutedText: UIColor(red: 0.6, green: 0.6, blue: 0.6, alpha: 1.0),
        footerAccentText: UIColor(red: 0.2, green: 0.2, blue: 0.2, alpha: 1.0)
    )

    static let dark = PaymentDialogThemeColors(
        overlay: UIColor.black.withAlphaComponent(0.6),
        contentBackground: UIColor(red: 0.15, green: 0.15, blue: 0.17, alpha: 1.0),
        titleText: UIColor(red: 0.95, green: 0.95, blue: 0.97, alpha: 1.0),
        closeButton: UIColor(red: 0.7, green: 0.7, blue: 0.72, alpha: 1.0),
        productCardBackground: UIColor(red: 0.22, green: 0.22, blue: 0.24, alpha: 1.0),
        imagePlaceholder: UIColor(red: 0.6, green: 0.4, blue: 0.0, alpha: 1.0),
        primaryText: UIColor(red: 0.95, green: 0.95, blue: 0.97, alpha: 1.0),
        mutedText: UIColor(red: 0.6, green: 0.6, blue: 0.62, alpha: 1.0),
        primaryButtonBackground: UIColor(red: 0.0, green: 0.48, blue: 1.0, alpha: 1.0),
        primaryButtonText: .white,
        badgeText: UIColor(red: 0.15, green: 0.15, blue: 0.17, alpha: 1.0),
        badgeBackground: UIColor(red: 1.0, green: 0.84, blue: 0.0, alpha: 1.0),
        secondaryButtonBackground: UIColor(red: 0.35, green: 0.35, blue: 0.37, alpha: 1.0),
        secondaryButtonText: UIColor(red: 0.95, green: 0.95, blue: 0.97, alpha: 1.0),
        footerMutedText: UIColor(red: 0.6, green: 0.6, blue: 0.62, alpha: 1.0),
        footerAccentText: UIColor(red: 0.95, green: 0.95, blue: 0.97, alpha: 1.0)
    )
}

// Custom dialog view controller
class PaymentDialogViewController: UIViewController {
    private let request: BrzShowPaymentOptionsDialogRequest
    private var contentView: UIView!
    private let onDismiss: BrzPaymentDialogDismissCallback?
    private let theme: PaymentDialogTheme

    init(
        request: BrzShowPaymentOptionsDialogRequest,
        onDismiss: BrzPaymentDialogDismissCallback? = nil,
    ) {
        self.request = request
        self.onDismiss = onDismiss
        self.theme = Self.resolveTheme(requestTheme: request.theme)
        super.init(nibName: nil, bundle: nil)
        modalPresentationStyle = .overFullScreen
        modalTransitionStyle = .crossDissolve
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    /// Resolves the request theme to a concrete dialog theme. For `.auto` or `nil`, uses system user interface style.
    private static func resolveTheme(requestTheme: BrzPaymentOptionsTheme?) -> PaymentDialogTheme {
        switch requestTheme {
        case .dark: return .dark
        case .light: return .light
        case .auto, .none:
            let style = currentSystemUserInterfaceStyle()
            switch style {
            case .dark: return .dark
            case .light, .unspecified: return .light
            @unknown default: return .light
            }
        }
    }

    /// Returns the current system user interface style (key window when available, otherwise main screen).
    private static func currentSystemUserInterfaceStyle() -> UIUserInterfaceStyle {
        if let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
           let keyWindow = windowScene.windows.first(where: { $0.isKeyWindow }) {
            return keyWindow.traitCollection.userInterfaceStyle
        }
        return UIScreen.main.traitCollection.userInterfaceStyle
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        setupUI()
    }

    private func setupUI() {
        let colors = theme.colors
        view.backgroundColor = colors.overlay

        // Main content container
        contentView = UIView()
        contentView.backgroundColor = colors.contentBackground
        contentView.layer.cornerRadius = 20
        contentView.layer.maskedCorners = [.layerMinXMinYCorner, .layerMaxXMinYCorner]
        contentView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(contentView)

        // Title bar
        let titleBar = createTitleBar(colors: colors)
        contentView.addSubview(titleBar)

        // Product info section
        let productSection = createProductSection(colors: colors)
        contentView.addSubview(productSection)

        // Payment buttons
        let directPaymentButton = createDirectPaymentButton(colors: colors)
        contentView.addSubview(directPaymentButton)

        let appStoreButton = createAppStoreButton(colors: colors)
        contentView.addSubview(appStoreButton)

        // Footer
        let footer = createFooter(colors: colors)
        contentView.addSubview(footer)

        // Layout constraints
        NSLayoutConstraint.activate([
            contentView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            contentView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            contentView.bottomAnchor.constraint(equalTo: view.bottomAnchor),

            titleBar.topAnchor.constraint(equalTo: contentView.topAnchor, constant: 20),
            titleBar.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 20),
            titleBar.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -20),
            titleBar.heightAnchor.constraint(equalToConstant: 44),

            productSection.topAnchor.constraint(equalTo: titleBar.bottomAnchor, constant: 20),
            productSection.leadingAnchor.constraint(
                equalTo: contentView.leadingAnchor, constant: 20),
            productSection.trailingAnchor.constraint(
                equalTo: contentView.trailingAnchor, constant: -20),

            directPaymentButton.topAnchor.constraint(
                equalTo: productSection.bottomAnchor, constant: 24),
            directPaymentButton.leadingAnchor.constraint(
                equalTo: contentView.leadingAnchor, constant: 20),
            directPaymentButton.trailingAnchor.constraint(
                equalTo: contentView.trailingAnchor, constant: -20),
            directPaymentButton.heightAnchor.constraint(equalToConstant: 56),

            appStoreButton.topAnchor.constraint(
                equalTo: directPaymentButton.bottomAnchor, constant: 12),
            appStoreButton.leadingAnchor.constraint(
                equalTo: contentView.leadingAnchor, constant: 20),
            appStoreButton.trailingAnchor.constraint(
                equalTo: contentView.trailingAnchor, constant: -20),
            appStoreButton.heightAnchor.constraint(equalToConstant: 56),

            footer.topAnchor.constraint(equalTo: appStoreButton.bottomAnchor, constant: 20),
            footer.centerXAnchor.constraint(equalTo: contentView.centerXAnchor),
            footer.bottomAnchor.constraint(
                equalTo: contentView.safeAreaLayoutGuide.bottomAnchor, constant: -20),
        ])
    }

    private func createTitleBar(colors: PaymentDialogThemeColors) -> UIView {
        let container = UIView()
        container.translatesAutoresizingMaskIntoConstraints = false

        let titleLabel = UILabel()
        titleLabel.text = request.title
        titleLabel.font = UIFont.boldSystemFont(ofSize: 18)
        titleLabel.textColor = colors.titleText
        titleLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(titleLabel)

        let closeButton = UIButton(type: .system)
        closeButton.setTitle("✕", for: .normal)
        closeButton.titleLabel?.font = UIFont.systemFont(ofSize: 24, weight: .regular)
        closeButton.setTitleColor(colors.closeButton, for: .normal)
        closeButton.addTarget(self, action: #selector(closeTapped), for: .touchUpInside)
        closeButton.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(closeButton)

        NSLayoutConstraint.activate([
            titleLabel.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            titleLabel.centerYAnchor.constraint(equalTo: container.centerYAnchor),

            closeButton.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            closeButton.centerYAnchor.constraint(equalTo: container.centerYAnchor),
            closeButton.widthAnchor.constraint(equalToConstant: 44),
            closeButton.heightAnchor.constraint(equalToConstant: 44),
        ])

        return container
    }

    private func createProductSection(colors: PaymentDialogThemeColors) -> UIView {
        let container = UIView()
        container.backgroundColor = colors.productCardBackground
        container.layer.cornerRadius = 12
        container.translatesAutoresizingMaskIntoConstraints = false

        // Product image
        let imageView = UIImageView()
        imageView.backgroundColor = colors.imagePlaceholder
        imageView.layer.cornerRadius = 8
        imageView.clipsToBounds = true
        imageView.contentMode = .scaleAspectFill
        imageView.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(imageView)

        // Load product icon from URL if provided
        if let iconUrlString = request.product.productIconUrl,
            let iconUrl = URL(string: iconUrlString)
        {
            loadImage(from: iconUrl, into: imageView)
        }

        // Product name
        let nameLabel = UILabel()
        nameLabel.text = request.product.displayName
        nameLabel.font = UIFont.boldSystemFont(ofSize: 16)
        nameLabel.textColor = colors.primaryText
        nameLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(nameLabel)

        // Original price (strikethrough)
        let originalPriceLabel = UILabel()
        originalPriceLabel.text = request.product.originalPrice
        originalPriceLabel.font = UIFont.systemFont(ofSize: 14)
        originalPriceLabel.textColor = colors.mutedText
        let attributedString = NSMutableAttributedString(string: request.product.originalPrice)
        attributedString.addAttribute(
            NSAttributedString.Key.strikethroughStyle, value: 1,
            range: NSRange(location: 0, length: request.product.originalPrice.count))
        originalPriceLabel.attributedText = attributedString
        originalPriceLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(originalPriceLabel)

        // Breeze price
        let breezePriceLabel = UILabel()
        breezePriceLabel.text = request.product.breezePrice
        breezePriceLabel.font = UIFont.boldSystemFont(ofSize: 18)
        breezePriceLabel.textColor = colors.primaryText
        breezePriceLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(breezePriceLabel)

        NSLayoutConstraint.activate([
            imageView.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 16),
            imageView.topAnchor.constraint(equalTo: container.topAnchor, constant: 16),
            imageView.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -16),
            imageView.widthAnchor.constraint(equalToConstant: 80),
            imageView.heightAnchor.constraint(equalToConstant: 80),

            nameLabel.leadingAnchor.constraint(equalTo: imageView.trailingAnchor, constant: 16),
            nameLabel.topAnchor.constraint(equalTo: container.topAnchor, constant: 20),
            nameLabel.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -16),

            originalPriceLabel.leadingAnchor.constraint(
                equalTo: imageView.trailingAnchor, constant: 16),
            originalPriceLabel.topAnchor.constraint(equalTo: nameLabel.bottomAnchor, constant: 8),

            breezePriceLabel.leadingAnchor.constraint(
                equalTo: imageView.trailingAnchor, constant: 16),
            breezePriceLabel.topAnchor.constraint(
                equalTo: originalPriceLabel.bottomAnchor, constant: 4),
            breezePriceLabel.trailingAnchor.constraint(
                equalTo: container.trailingAnchor, constant: -16),
        ])

        return container
    }

    private func loadImage(from url: URL, into imageView: UIImageView) {
        Task {
            do {
                let (data, _) = try await URLSession.shared.data(from: url)
                if let image = UIImage(data: data) {
                    await MainActor.run {
                        imageView.image = image
                        imageView.backgroundColor = .clear
                    }
                }
            } catch {
                print("Failed to load product icon image: \(error)")
                // Keep the placeholder background color if loading fails
            }
        }
    }

    private func createDirectPaymentButton(colors: PaymentDialogThemeColors) -> UIView {
        let container = UIView()
        container.translatesAutoresizingMaskIntoConstraints = false

        let button = UIButton(type: .system)
        button.backgroundColor = colors.primaryButtonBackground
        button.setTitle("Direct Payment \(request.product.breezePrice)", for: .normal)
        button.setTitleColor(colors.primaryButtonText, for: .normal)
        button.titleLabel?.font = UIFont.boldSystemFont(ofSize: 16)
        button.layer.cornerRadius = 12
        button.addTarget(self, action: #selector(directPaymentTapped), for: .touchUpInside)
        button.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(button)

        var constraints: [NSLayoutConstraint] = [
            button.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            button.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            button.topAnchor.constraint(equalTo: container.topAnchor),
            button.bottomAnchor.constraint(equalTo: container.bottomAnchor),
        ]

        // Save badge – only render when decoration is non-null and non-empty
        if let decoration = request.product.decoration, !decoration.isEmpty {
            let badge = UILabel()
            badge.text = decoration
            badge.font = UIFont.boldSystemFont(ofSize: 12)
            badge.textColor = colors.badgeText
            badge.backgroundColor = colors.badgeBackground
            badge.textAlignment = .center
            badge.layer.cornerRadius = 8
            badge.layer.masksToBounds = true
            badge.translatesAutoresizingMaskIntoConstraints = false
            container.addSubview(badge)
            constraints.append(contentsOf: [
                badge.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -12),
                badge.topAnchor.constraint(equalTo: container.topAnchor, constant: -8),
                badge.widthAnchor.constraint(equalToConstant: 70),
                badge.heightAnchor.constraint(equalToConstant: 24),
            ])
        }

        NSLayoutConstraint.activate(constraints)

        return container
    }

    private func createAppStoreButton(colors: PaymentDialogThemeColors) -> UIButton {
        let button = UIButton(type: .system)
        button.backgroundColor = colors.secondaryButtonBackground
        button.setTitle("Apple App Store \(request.product.originalPrice)", for: .normal)
        button.setTitleColor(colors.secondaryButtonText, for: .normal)
        button.titleLabel?.font = UIFont.systemFont(ofSize: 16)
        button.layer.cornerRadius = 12
        button.addTarget(self, action: #selector(appStoreTapped), for: .touchUpInside)
        button.translatesAutoresizingMaskIntoConstraints = false
        return button
    }

    private func createFooter(colors: PaymentDialogThemeColors) -> UIView {
        let container = UIView()
        container.translatesAutoresizingMaskIntoConstraints = false

        let poweredByLabel = UILabel()
        poweredByLabel.text = "Powered by"
        poweredByLabel.font = UIFont.systemFont(ofSize: 12)
        poweredByLabel.textColor = colors.footerMutedText
        poweredByLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(poweredByLabel)

        let breezeLabel = UILabel()
        breezeLabel.text = "breeze"
        breezeLabel.font = UIFont.boldSystemFont(ofSize: 12)
        breezeLabel.textColor = colors.footerAccentText
        breezeLabel.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(breezeLabel)

        NSLayoutConstraint.activate([
            poweredByLabel.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            poweredByLabel.centerYAnchor.constraint(equalTo: container.centerYAnchor),

            breezeLabel.leadingAnchor.constraint(
                equalTo: poweredByLabel.trailingAnchor, constant: 4),
            breezeLabel.centerYAnchor.constraint(equalTo: container.centerYAnchor),
            breezeLabel.trailingAnchor.constraint(equalTo: container.trailingAnchor),
        ])

        return container
    }

    @objc private func closeTapped() {
        dismiss(animated: true) {
            self.onDismiss?(.closeTapped, self.request.data)
        }
    }

    @objc private func directPaymentTapped() {
        print("Direct Payment tapped")
        dismiss(animated: true) {
            // Open URL in Safari View Controller (in-app).
            // NOTE: Do NOT fire onDismiss here — only fire it once when the Safari view
            // is dismissed (either by user or programmatically). This prevents the game
            // from receiving two callbacks for one payment flow.
            if let urlString = self.request.directPaymentUrl, let url = URL(string: urlString) {
                let safariView = BreezeSafariView(
                    url: url,
                    onDismiss: {
                        // Single callback when the payment browser is closed
                        self.onDismiss?(.directPaymentTapped, self.request.data)
                    })
                safariView.show()
            } else {
                print("Warning: No directPaymentUrl provided")
                self.onDismiss?(.closeTapped, self.request.data)
            }
        }
    }

    @objc private func appStoreTapped() {
        print("Apple App Store tapped")
        dismiss(animated: true) {
            self.onDismiss?(.appStoreTapped, self.request.data)
        }
    }
}

func showPaymentOptionsDialog(
    request: BrzShowPaymentOptionsDialogRequest,
    onDismiss: BrzPaymentDialogDismissCallback? = nil,
    theme: PaymentDialogTheme = .light
) {
    DispatchQueue.main.async {
        Task {
            guard let storefront = await Storefront.current else {
                print("Warning: Could not get current storefront")
                onDismiss?(.appStoreTapped, request.data)
                return
            }

            let countryCode = storefront.countryCode.lowercased()
            if countryCode != "usa" {
                print("Warning: Storefront country code is \(countryCode), not USA")
                onDismiss?(.appStoreTapped, request.data)
                return
            }

            // check if the payment page url host ends with .breeze.cash, if not dismiss with close
            if let urlString = request.directPaymentUrl,
                let url = URL(string: urlString),
                let host = url.host
            {
                if !host.lowercased().hasSuffix(".breeze.cash") {
                    print("Warning: Direct payment URL host is not .breeze.cash")
                    onDismiss?(.closeTapped, request.data)
                    return
                }
            } else {
                // If URL is invalid or missing, dismiss with close
                print("Warning: Direct payment URL is invalid or missing")
                onDismiss?(.closeTapped, request.data)
                return
            }

            // show the dialog
            guard let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
                let rootViewController = windowScene.windows.first?.rootViewController
            else {
                print("Error: Could not find root view controller")
                onDismiss?(.closeTapped, request.data)
                return
            }

            let dialogViewController = PaymentDialogViewController(
                request: request, onDismiss: onDismiss)
            rootViewController.present(dialogViewController, animated: true, completion: nil)
        }
    }
}
