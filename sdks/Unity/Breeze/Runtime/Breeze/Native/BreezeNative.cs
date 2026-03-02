using System;

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

public interface IBreezeNative
{
    public string GetDeviceUniqueId();

    public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
        BrzShowPaymentOptionsDialogRequest request,
        BrzPaymentDialogDismissCallback onDismiss
    );

    public void DismissPaymentPageView();

    public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
        BrzShowPaymentWebviewRequest request,
        BrzPaymentWebviewDismissCallback onDismiss
    );
}
