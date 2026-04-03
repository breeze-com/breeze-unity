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
        /// Initializes the Breeze SDK singleton with the provided configuration.
        /// Must be called once before accessing <see cref="Instance"/> or invoking any payment methods.
        /// Call <see cref="Uninitialize"/> first if you need to re-initialize with a different configuration.
        /// </summary>
        /// <param name="configuration">The SDK configuration. <see cref="BreezeConfiguration.AppScheme"/> is required.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="configuration"/> is null or <see cref="BreezeConfiguration.AppScheme"/> is empty.</exception>
        public static void Initialize(BreezeConfiguration configuration)
        {
            if (_instance != null)
            {
                UnityEngine.Debug.LogWarning("BreezePayment already initialized. Call Uninitialize() first to re-initialize.");
                return;
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
        /// Programmatically dismisses any currently-visible payment page web view.
        /// Has no effect if the web view is not currently shown.
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
                throw new ArgumentException("AppScheme is required in BreezeConfiguration (e.g. 'yourgame')");
            }
        }
    }
}