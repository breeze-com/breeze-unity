#if UNITY_ANDROID

using System;
using Newtonsoft.Json;
using UnityEngine;

public class BreezeNativeAndroid : IBreezeNative
{
    private AndroidJavaClass androidPlugin;
    private AndroidJavaObject androidPluginInstance;

    private static BrzPaymentDialogDismissCallback pendingDismissCallback;
    private static BrzPaymentWebviewDismissCallback pendingWebViewDismissCallback;

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

    public string GetDeviceUniqueId()
    {
        EnsureAndroidPluginInitialized();
        string id = androidPluginInstance.Call<string>("getDeviceUniqueId");
        return id;
    }

    public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
        BrzShowPaymentOptionsDialogRequest request,
        BrzPaymentDialogDismissCallback onDismiss)
    {
        EnsureAndroidPluginInitialized();
        EnsureCallbackReceiverExists();

        pendingDismissCallback = onDismiss;

        string requestJson = JsonConvert.SerializeObject(request);
        Debug.Log($"brz_show_payment_options_dialog: request = {requestJson}");
        int code = androidPluginInstance.Call<int>("showPaymentOptionsDialog", requestJson);
        return (BrzShowPaymentOptionsResultCode)code;
    }

    public void DismissPaymentPageView()
    {
        Debug.Log("BreezeNativeAndroid: DismissPaymentPageView called (no-op on Android)");
    }

    public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
        BrzShowPaymentWebviewRequest request,
        BrzPaymentWebviewDismissCallback onDismiss)
    {
        EnsureAndroidPluginInitialized();
        EnsureCallbackReceiverExists();

        pendingWebViewDismissCallback = onDismiss;

        string requestJson = JsonConvert.SerializeObject(request);
        Debug.Log($"BreezeNativeAndroid: showPaymentWebview request = {requestJson}");
        int code = androidPluginInstance.Call<int>("showPaymentWebview", requestJson);
        return (BrzShowPaymentWebviewResultCode)code;
    }

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
        Debug.Log($"BreezeAndroidCallbackReceiver: OnAndroidDialogDismissed: {jsonPayload}");
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
        Debug.Log($"BreezeAndroidCallbackReceiver: OnAndroidWebViewDismissed: {jsonPayload}");
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

    [Serializable]
    private class DialogDismissedPayload
    {
        [JsonProperty("reason")]
        public BrzPaymentDialogDismissReason Reason;

        [JsonProperty("data")]
        public string Data;
    }

    [Serializable]
    private class WebViewDismissedPayload
    {
        [JsonProperty("reason")]
        public BrzPaymentWebviewDismissReason Reason;

        [JsonProperty("data")]
        public string Data;
    }
}

#endif
