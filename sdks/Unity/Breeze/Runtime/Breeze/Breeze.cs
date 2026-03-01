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
            UnityEngine.Debug.LogWarning("BreezePayment already initialized. Call Uninitialize() first to re-initialize.");
            return;
        }
        _instance = new Breeze(configuration);
    }

    public static void Uninitialize()
    {
        if (_instance != null)
        {
            _instance.Destroy();
        }
        _instance = null;
    }

    private BreezeConfiguration configuration;

    public event Action<BrzPaymentDialogDismissReason, string> OnPaymentOptionsDialogDismissed;

    private GameObject messenger;

    private Breeze(BreezeConfiguration configuration)
    {
        this.configuration = configuration;
        this.ValidateConfiguration();
        this.messenger = this.InstantiateBridgeObject();
    }

    private GameObject InstantiateBridgeObject()
    {
        GameObject go = GameObject.Find("/BreezePay");
        if (go == null)
        {
            go = new GameObject("BreezePay");
        }
        if (go.GetComponent<BreezeBridgeMessenger>() == null)
        {
            go.AddComponent<BreezeBridgeMessenger>();
        }
        GameObject.DontDestroyOnLoad(go);
        return go;
    }

    private void Destroy()
    {
        if (messenger != null)
        {
            GameObject.Destroy(this.messenger);
            this.messenger = null;
        }
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
