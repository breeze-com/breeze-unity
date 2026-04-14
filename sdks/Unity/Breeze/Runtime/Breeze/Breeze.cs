using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BreezeSdk.Runtime
{

    /// <summary>
    /// Main entry point for the Breeze Payment SDK.
    /// Use <see cref="Initialize"/> to create the singleton instance before calling any other methods.
    /// </summary>
    public sealed class Breeze
    {
        private static Breeze _instance;

        /// <summary>
        /// Gets the initialized singleton instance of <see cref="Breeze"/>.
        /// Returns <c>null</c> if <see cref="Initialize"/> has not been called yet.
        /// </summary>
        public static Breeze Instance => _instance;

        /// <summary>
        /// Initializes the Breeze SDK singleton using settings configured in the Breeze Setup editor window.
        /// The <see cref="BreezeRuntimeSettings"/> asset must exist (created automatically when you save in <c>Breeze &gt; Setup</c>).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the runtime settings asset is missing or <c>AppScheme</c> is empty.</exception>
        public static void Initialize()
        {
            Initialize(new BreezeConfiguration());
        }

        /// <summary>
        /// Initializes the Breeze SDK singleton with the provided configuration.
        /// If <see cref="BreezeConfiguration.AppScheme"/> is not set, it is read automatically from the
        /// <see cref="BreezeRuntimeSettings"/> asset created by the Breeze Setup editor window.
        /// Must be called once before accessing <see cref="Instance"/> or invoking any payment methods.
        /// Call <see cref="Uninitialize"/> first if you need to re-initialize with a different configuration.
        /// </summary>
        /// <param name="configuration">The SDK configuration.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="configuration"/> is null or <c>AppScheme</c> cannot be resolved.</exception>
        public static void Initialize(BreezeConfiguration configuration)
        {
            if (_instance != null)
            {
                UnityEngine.Debug.LogWarning("BreezePayment already initialized. Call Uninitialize() first to re-initialize.");
                return;
            }

            if (configuration != null && string.IsNullOrEmpty(configuration.AppScheme))
            {
                var runtimeSettings = BreezeRuntimeSettings.Load();
                if (runtimeSettings == null || string.IsNullOrEmpty(runtimeSettings.AppScheme))
                {
                    throw new ArgumentException(
                        "AppScheme is not configured. Open the Breeze Setup window (Breeze > Setup) " +
                        "in the Unity Editor to set your URL scheme, then click Save Settings.");
                }
                configuration.AppScheme = runtimeSettings.AppScheme;
            }

            _instance = new Breeze(configuration);
        }

        /// <summary>
        /// Tears down the Breeze SDK singleton, releasing all associated resources.
        /// After calling this method, <see cref="Instance"/> returns <c>null</c> until <see cref="Initialize"/> is called again.
        /// </summary>
        public static void Uninitialize()
        {
            _instance = null;
        }

        private BreezeConfiguration configuration;

        /// <summary>
        /// Raised when the payment options dialog is dismissed.
        /// The first argument is the <see cref="BrzPaymentDialogDismissReason"/> indicating why it was closed;
        /// the second argument is an optional JSON data payload from the dialog.
        /// </summary>
        public event Action<BrzPaymentDialogDismissReason, string> OnPaymentOptionsDialogDismissed;

        /// <summary>
        /// Raised when the payment web view is dismissed.
        /// The first argument is the <see cref="BrzPaymentWebviewDismissReason"/> indicating why it was closed;
        /// the second argument is an optional JSON data payload from the web view.
        /// </summary>
        public event Action<BrzPaymentWebviewDismissReason, string> OnPaymentWebviewDismissed;

        private Breeze(BreezeConfiguration configuration)
        {
            this.configuration = configuration;
            this.ValidateConfiguration();
        }

        private const string DeepLinkHost = "breeze-payment";
        private const string SuccessPath = "/purchase/success";
        private const string FailurePath = "/purchase/failure";

        /// <summary>
        /// Returns this AppScheme
        /// </summary>
        public string AppScheme => this.configuration.AppScheme;

        /// <summary>
        /// The deep-link URL that the Breeze payment page redirects to on a successful purchase.
        /// For example: <c>yourgame://breeze-payment/purchase/success</c>.
        /// Pass this as <c>SuccessReturnUrl</c> when creating an order on your game server.
        /// </summary>
        public string SuccessReturnUrl => $"{this.configuration.AppScheme}://{DeepLinkHost}{SuccessPath}";

        /// <summary>
        /// The deep-link URL that the Breeze payment page redirects to on a failed or cancelled purchase.
        /// For example: <c>yourgame://breeze-payment/purchase/failure</c>.
        /// Pass this as <c>FailReturnUrl</c> when creating an order on your game server.
        /// </summary>
        public string FailureReturnUrl => $"{this.configuration.AppScheme}://{DeepLinkHost}{FailurePath}";

        /// <summary>
        /// Checks whether the given deep-link URL represents a successful payment redirect.
        /// The URL alone does not guarantee the payment succeeded — always verify the result on your server.
        /// </summary>
        /// <param name="url">The deep-link URL received via <c>Application.deepLinkActivated</c>.</param>
        /// <returns><c>true</c> if the URL matches the Breeze payment success pattern; otherwise <c>false</c>.</returns>
        public bool IsPaymentSuccessUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            var uri = new Uri(url);
            return uri.Scheme == this.configuration.AppScheme
                && uri.Host == DeepLinkHost
                && uri.AbsolutePath == SuccessPath;
        }

        /// <summary>
        /// Returns a stable, device-unique identifier suitable for correlating sessions.
        /// On iOS this is the vendor identifier (IDFV); on Android it is the Android ID.
        /// </summary>
        /// <returns>A non-empty string containing the device unique identifier.</returns>
        public string GetDeviceUniqueId()
        {
            return BreezeNative.Instance.GetDeviceUniqueId();
        }

        /// <summary>
        /// Presents the Breeze payment options dialog over the current Unity view.
        /// When the user dismisses the dialog, <see cref="OnPaymentOptionsDialogDismissed"/> is raised on the main thread.
        /// </summary>
        /// <param name="request">The request describing the product and payment options to display.</param>
        public void ShowPaymentOptionsDialog(BrzShowPaymentOptionsDialogRequest request)
        {
            BreezeNative.Instance.ShowPaymentOptionsDialog(request, NotifyOnPaymentOptionsDialogDismissed);
        }

        /// <summary>
        /// Programmatically dismisses any currently-visible payment page web view (iOS only).
        /// 
        /// On Android this is a no-op because Chrome Custom Tabs run in a separate process
        /// and are automatically removed from the back stack when the deep link returns.
        /// </summary>
        public void DismissPaymentPageView()
        {
            BreezeNative.Instance.DismissPaymentPageView();
        }

        /// <summary>
        /// Presents the Breeze payment web view over the current Unity view, loading the given payment URL.
        /// When the user dismisses the web view, <see cref="OnPaymentWebviewDismissed"/> is raised on the main thread.
        /// </summary>
        /// <param name="request">The request containing the direct payment URL and optional data payload.</param>
        public void ShowPaymentWebview(BrzShowPaymentWebviewRequest request)
        {
            BreezeNative.Instance.ShowPaymentWebview(request, NotifyOnPaymentWebviewDismissed);
        }

        /// <summary>
        /// Native-to-managed callback invoked by the platform layer when the payment options dialog is dismissed.
        /// Forwards the event to <see cref="OnPaymentOptionsDialogDismissed"/> on the active instance.
        /// </summary>
        /// <param name="reason">The reason the dialog was dismissed.</param>
        /// <param name="data">Optional JSON payload passed from the native dialog.</param>
        [AOT.MonoPInvokeCallback(typeof(BrzPaymentDialogDismissCallback))]
        public static void NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason reason, string data)
        {
#if BREEZE_DEBUG
        UnityEngine.Debug.Log($"dialog dismissed, reason: {reason}, data: {data}");
#endif

            if (_instance != null)
            {
                _instance.OnPaymentOptionsDialogDismissed?.Invoke(reason, data);
            }
        }

        /// <summary>
        /// Native-to-managed callback invoked by the platform layer when the payment web view is dismissed.
        /// Forwards the event to <see cref="OnPaymentWebviewDismissed"/> on the active instance.
        /// </summary>
        /// <param name="reason">The reason the web view was dismissed.</param>
        /// <param name="data">Optional JSON payload passed from the native web view.</param>
        [AOT.MonoPInvokeCallback(typeof(BrzPaymentWebviewDismissCallback))]
        public static void NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason reason, string data)
        {
#if BREEZE_DEBUG
        UnityEngine.Debug.Log($"webview dismissed, reason: {reason}, data: {data}");
#endif
            if (_instance != null)
            {
                _instance.OnPaymentWebviewDismissed?.Invoke(reason, data);
            }
        }

        // when redirected back from the payment page, verify the redirect url is valid
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateConfiguration()
        {
            if (this.configuration == null)
            {
                throw new ArgumentException("Configuration is required");
            }
            if (string.IsNullOrEmpty(this.configuration.AppScheme))
            {
                throw new ArgumentException(
                    "AppScheme is required. Open the Breeze Setup window (Breeze > Setup) " +
                    "in the Unity Editor to set your URL scheme, then click Save Settings.");
            }
        }
    }
}