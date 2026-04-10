using System;
namespace BreezeSdk.Runtime
{

    internal static class BreezeNative
    {
        private static IBreezeNative _instance;

        public static IBreezeNative Instance
        {
            get
            {
                if (_instance == null)
                {
#if !UNITY_EDITOR && UNITY_IOS
                _instance = new BreezeNativeIos();
#elif !UNITY_EDITOR && UNITY_ANDROID
                _instance = new BreezeNativeAndroid();
#else
                    _instance = new BreezeNativeNoop();
#endif
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// Interface for platform-specific native Breeze SDK implementations.
    /// </summary>
    public interface IBreezeNative
    {
        /// <summary>
        /// Gets the unique device identifier.
        /// </summary>
        /// <returns>A string representing the unique device ID.</returns>
        public string GetDeviceUniqueId();

        /// <summary>
        /// Shows the payment options dialog.
        /// </summary>
        /// <param name="request">The request configuration for the payment options dialog.</param>
        /// <param name="onDismiss">Callback invoked when the dialog is dismissed.</param>
        /// <returns>A result code indicating whether the dialog was shown successfully.</returns>
        public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
            BrzShowPaymentOptionsDialogRequest request,
            BrzPaymentDialogDismissCallback onDismiss
        );

        /// <summary>
        /// Dismisses the currently displayed payment page view.
        /// </summary>
        public void DismissPaymentPageView();

        /// <summary>
        /// Shows the payment web view.
        /// </summary>
        /// <param name="request">The request configuration for the payment web view.</param>
        /// <param name="onDismiss">Callback invoked when the web view is dismissed.</param>
        /// <returns>A result code indicating whether the web view was shown successfully.</returns>
        public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
            BrzShowPaymentWebviewRequest request,
            BrzPaymentWebviewDismissCallback onDismiss
        );
    }
}