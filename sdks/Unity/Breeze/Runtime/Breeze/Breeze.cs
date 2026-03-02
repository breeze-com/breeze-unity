using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class Breeze
{
    private static Breeze _instance;
    public static Breeze Instance => _instance;

    public static void Initialize(BreezeConfiguration configuration)
    {
        if (_instance != null)
        {
            UnityEngine.Debug.LogWarning("BreezePayment already initialized");
        }
        _instance = new Breeze(configuration);
    }

    public static void Uninitialize()
    {
        _instance = null;
    }

    private BreezeConfiguration configuration;

    public event Action<BrzPaymentDialogDismissReason, string> OnPaymentOptionsDialogDismissed;
    public event Action<BrzPaymentWebviewDismissReason, string> OnPaymentWebviewDismissed;

    private Breeze(BreezeConfiguration configuration)
    {
        this.configuration = configuration;
        this.ValidateConfiguration();
    }

    public string GetDeviceUniqueId()
    {
        return BreezeNative.Instance.GetDeviceUniqueId();
    }

    public void ShowPaymentOptionsDialog(BrzShowPaymentOptionsDialogRequest request)
    {
        BreezeNative.Instance.ShowPaymentOptionsDialog(request, NotifyOnPaymentOptionsDialogDismissed);
    }

    public void DismissPaymentPageView()
    {
        BreezeNative.Instance.DismissPaymentPageView();
    }

    public void ShowPaymentWebview(BrzShowPaymentWebviewRequest request)
    {
        BreezeNative.Instance.ShowPaymentWebview(request, NotifyOnPaymentWebviewDismissed);
    }

    [AOT.MonoPInvokeCallback(typeof(BrzPaymentDialogDismissCallback))]
    public static void NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason reason, string data)
    {
        UnityEngine.Debug.Log($"dialog dismissed, reason: {reason}, data: {data}");

        if (_instance != null)
        {
            _instance.OnPaymentOptionsDialogDismissed?.Invoke(reason, data);
        }
    }

    [AOT.MonoPInvokeCallback(typeof(BrzPaymentWebviewDismissCallback))]
    public static void NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason reason, string data)
    {
        UnityEngine.Debug.Log($"webview dismissed, reason: {reason}, data: {data}");

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
    }
}
