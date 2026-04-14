#if UNITY_ANDROID

using System;
using Newtonsoft.Json;
using UnityEngine;
namespace BreezeSdk.Runtime
{
    /// <summary>
    /// Android native implementation of <see cref="IBreezeNative"/>.
    /// </summary>
    public class BreezeNativeAndroid : IBreezeNative
    {
        private AndroidJavaClass androidPlugin;
        private AndroidJavaObject androidPluginInstance;

        private static BrzPaymentDialogDismissCallback pendingDismissCallback;
        private static BrzPaymentWebviewDismissCallback pendingWebViewDismissCallback;

        /// <summary>
        /// Initializes the Android native plugin and ensures the callback receiver exists.
        /// </summary>
        public BreezeNativeAndroid()
        {
            this.InitializeAndroidPlugin();
            EnsureCallbackReceiverExists();
        }

        private void InitializeAndroidPlugin()
        {
            if (androidPlugin == null)
            {
                try
                {
                    androidPlugin = new AndroidJavaClass("com.breeze.sdk.BreezeNativeAndroid");
                    androidPluginInstance = androidPlugin.CallStatic<AndroidJavaObject>("getInstance");
                }
                catch (Exception e)
                {
                    Debug.LogError($"BreezeNativeAndroid: Failed to initialize Android plugin: {e.Message}");
                }
            }
        }

        private void EnsureAndroidPluginInitialized()
        {
            if (androidPluginInstance == null)
            {
                throw new Exception($"BreezeNativeAndroid: Android plugin should be initialized.");
            }
        }

        private static void EnsureCallbackReceiverExists()
        {
            if (BreezeAndroidCallbackReceiver.Instance == null)
            {
                var go = new GameObject("BreezePay");
                go.AddComponent<BreezeAndroidCallbackReceiver>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
        }

        /// <inheritdoc />
        public string GetDeviceUniqueId()
        {
            EnsureAndroidPluginInitialized();
            string id = androidPluginInstance.Call<string>("getDeviceUniqueId");
            return id;
        }

        /// <inheritdoc />
        public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
            BrzShowPaymentOptionsDialogRequest request,
            BrzPaymentDialogDismissCallback onDismiss)
        {
            EnsureAndroidPluginInitialized();
            EnsureCallbackReceiverExists();

            pendingDismissCallback = onDismiss;

            string requestJson = JsonConvert.SerializeObject(request);
#if BREEZE_DEBUG
            Debug.Log($"brz_show_payment_options_dialog: request = {requestJson}");
#endif
            int code = androidPluginInstance.Call<int>("showPaymentOptionsDialog", requestJson);
            return (BrzShowPaymentOptionsResultCode)code;
        }

        /// <inheritdoc />
        public void DismissPaymentPageView()
        {
            // On Android this is a no-op because Chrome Custom Tabs run in a separate process
            // and are automatically removed from the back stack when the deep link returns.
            // check the <inheritdoc /> for details
#if BREEZE_DEBUG
            Debug.Log("BreezeNativeAndroid: DismissPaymentPageView called (no-op on Android)");
#endif
        }

        /// <inheritdoc />
        public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
            BrzShowPaymentWebviewRequest request,
            BrzPaymentWebviewDismissCallback onDismiss)
        {
            EnsureAndroidPluginInitialized();
            EnsureCallbackReceiverExists();

            pendingWebViewDismissCallback = onDismiss;

            string requestJson = JsonConvert.SerializeObject(request);
#if BREEZE_DEBUG
            Debug.Log($"BreezeNativeAndroid: showPaymentWebview request = {requestJson}");
#endif
            int code = androidPluginInstance.Call<int>("showPaymentWebview", requestJson);
            return (BrzShowPaymentWebviewResultCode)code;
        }

        /// <summary>
        /// Dismisses the currently displayed payment web view.
        /// </summary>
        public void DismissPaymentWebview()
        {
            EnsureAndroidPluginInitialized();
            androidPluginInstance.Call("dismissPaymentWebview");
        }

        internal static void HandleDialogDismissed(BrzPaymentDialogDismissReason reason, string data)
        {
            var callback = pendingDismissCallback;
            pendingDismissCallback = null;
            callback?.Invoke(reason, data);
        }

        internal static void HandleWebViewDismissed(BrzPaymentWebviewDismissReason reason, string data)
        {
            var callback = pendingWebViewDismissCallback;
            pendingWebViewDismissCallback = null;
            callback?.Invoke(reason, data);
        }
    }

    /// <summary>
    /// MonoBehaviour that receives UnitySendMessage callbacks from Java.
    /// Must live on a GameObject named "BreezePay" (matching BreezeUnityBridge.UNITY_GAME_OBJECT).
    /// </summary>
    public class BreezeAndroidCallbackReceiver : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the callback receiver.
        /// </summary>
        public static BreezeAndroidCallbackReceiver Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Called by Unity's UnitySendMessage from Java BreezeUnityBridge.
        /// Payload JSON format: {"reason": "", "data": "..."}
        /// </summary>
        public void OnAndroidDialogDismissed(string jsonPayload)
        {
#if BREEZE_DEBUG
            Debug.Log($"BreezeAndroidCallbackReceiver: OnAndroidDialogDismissed: {jsonPayload}");
#endif
            try
            {
                var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>(jsonPayload);
                var reason = (BrzPaymentDialogDismissReason)(payload?.Reason ?? 0);
                var data = payload?.Data;
                BreezeNativeAndroid.HandleDialogDismissed(reason, data);
            }
            catch (Exception e)
            {
                Debug.LogError($"BreezeAndroidCallbackReceiver: Failed to parse dismiss payload: {e.Message}");
                BreezeNativeAndroid.HandleDialogDismissed(BrzPaymentDialogDismissReason.CloseTapped, null);
            }
        }

        /// <summary>
        /// Called by Unity's UnitySendMessage from Java BreezeUnityBridge.
        /// Payload JSON format: {"reason": "", "data": "..."}
        /// </summary>
        public void OnAndroidWebViewDismissed(string jsonPayload)
        {
#if BREEZE_DEBUG
            Debug.Log($"BreezeAndroidCallbackReceiver: OnAndroidWebViewDismissed: {jsonPayload}");
#endif
            try
            {
                var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>(jsonPayload);
                var reason = payload?.Reason ?? BrzPaymentWebviewDismissReason.Dismissed;
                var data = payload?.Data;
                BreezeNativeAndroid.HandleWebViewDismissed(reason, data);
            }
            catch (Exception e)
            {
                Debug.LogError($"BreezeAndroidCallbackReceiver: Failed to parse webview dismiss payload: {e.Message}");
                BreezeNativeAndroid.HandleWebViewDismissed(BrzPaymentWebviewDismissReason.Dismissed, null);
            }
        }
    }
}
#endif
