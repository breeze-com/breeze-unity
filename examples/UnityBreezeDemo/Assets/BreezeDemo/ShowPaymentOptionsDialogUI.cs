using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

public class ShowPaymentOptionsDialogUI : MonoBehaviour
{
    const string GAME_PRODUCT_ID = "test-product-id-123456";
    const string IAP_PRODUCT_ID = "breezegoldcoin100";
    const string BREEZE_PRODUCT_ID = "prd_e1d721f06a0effe9";

    [SerializeField] RotatingCube rotatingCube;
    [SerializeField] YourGameClient gameClient;
    [SerializeField] IapManager iapManager;

    private Button btnTest;
    private Button btnShowDialog;
    private Button btnShowWebview;
    private ToggleButtonGroup themeButtonGroup;

    private bool isCreatingOrder = false;

    async Awaitable Start()
    {
        await this.IapInitializeAsync();

        Application.deepLinkActivated += this.OnPaymentPageResult;

        Breeze.Initialize(new BreezeConfiguration()
        {
            AppScheme = "breezedemo://",
            Environment = BreezeEnvironment.Production,
        });

        var root = GetComponent<UIDocument>().rootVisualElement;
        this.btnShowDialog = root.Q<Button>("show-dialog-button");
        this.btnShowWebview = root.Q<Button>("show-webview-button");
        this.btnTest = root.Q<Button>("test-button");
        this.themeButtonGroup = root.Q<ToggleButtonGroup>("theme-button-group");

        this.btnShowDialog.clicked += this.OnShowDialogClicked;
        this.btnShowWebview.clicked += this.OnShowWebviewClicked;
        this.btnTest.clicked += this.OnTestClicked;
    }

    void OnDestroy()
    {
        this.btnShowDialog.clicked -= this.OnShowDialogClicked;
        this.btnShowWebview.clicked -= this.OnShowWebviewClicked;
        this.btnTest.clicked -= this.OnTestClicked;
        Application.deepLinkActivated -= this.OnPaymentPageResult;

        this.IapUninitialize();
    }

    BrzPaymentOptionsTheme GetThemeFromGroup()
    {
        if (this.themeButtonGroup == null)
            return BrzPaymentOptionsTheme.Auto;
        var state = this.themeButtonGroup.value;
        int count = state.length;
        if (count <= 0)
            return BrzPaymentOptionsTheme.Auto;
        var buffer = new int[count];
        var active = state.GetActiveOptions(buffer.AsSpan());
        if (active.Length <= 0)
            return BrzPaymentOptionsTheme.Auto;
        return active[0] switch
        {
            0 => BrzPaymentOptionsTheme.Auto,
            1 => BrzPaymentOptionsTheme.Dark,
            2 => BrzPaymentOptionsTheme.Light,
            _ => BrzPaymentOptionsTheme.Auto,
        };
    }

    private async Awaitable IapInitializeAsync()
    {
        try
        {
            await this.iapManager.InitializeAsync(new List<ProductDefinition>() {
                new ProductDefinition(GAME_PRODUCT_ID, IAP_PRODUCT_ID, ProductType.Consumable)
            });
            this.iapManager.OnPurchaseSuccess += this.OnIAPPurchaseSuccess;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Initialize IAP failed: {ex}");
        }
    }

    private void IapUninitialize()
    {
        this.iapManager.OnPurchaseSuccess -= this.OnIAPPurchaseSuccess;
    }

    private void IapPurchase(string productId)
    {
        try
        {
            this.iapManager.PurchaseProduct(productId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Purchase IAP failed: {ex}");
        }
    }

    void OnIAPPurchaseSuccess()
    {
        PlayPaymentSuccessEffect();
    }

    void OnTestClicked()
    {
        string id = Breeze.Instance.GetDeviceUniqueId();
        Debug.Log($"device unique id is: {id}");
        PlayPaymentSuccessEffect();
    }

    void OnShowDialogClicked()
    {
        var theme = GetThemeFromGroup();
        StartCoroutine(CreateOrderAndShowPaymentDialog(theme));
    }

    void OnShowWebviewClicked()
    {
        StartCoroutine(CreateOrderAndShowPaymentWebview());
    }

    async Awaitable CreateOrderAndShowPaymentWebview()
    {
        Debug.Log("show webview clicked");

        if (this.isCreatingOrder)
        {
            Debug.LogWarning("already creating order, please wait");
            return;
        }

        if (this.gameClient == null)
        {
            Debug.LogError("gameClient is not set");
            return;
        }

        this.isCreatingOrder = true;
        CreateOrderResult result = null;
        try
        {
            result = await this.gameClient.CreateOrderAsync(new CreateOrderInput
            {
                ProductId = BREEZE_PRODUCT_ID,
                Quantity = 1,
                Context = Application.platform == RuntimePlatform.Android ? "android" : "iap",
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateOrderAsync failed. Error: {ex}");
        }
        finally
        {
            this.isCreatingOrder = false;
        }

        if (result == null)
        {
            Debug.LogError("failed to create order");
            return;
        }

        Debug.Log($"webview order result url: {result.Data.Url}");

        var request = new BrzShowPaymentWebviewRequest()
        {
            DirectPaymentUrl = result.Data.Url,
            Data = GAME_PRODUCT_ID,
        };

        Breeze.Instance.OnPaymentWebviewDismissed += this.OnPaymentWebviewDismissed;
        Breeze.Instance.ShowPaymentWebview(request);
    }

    void OnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason reason, string data)
    {
        Debug.Log($"UI Example, webview dismissed, reason: {reason}, data: {data}");
        Breeze.Instance.OnPaymentWebviewDismissed -= this.OnPaymentWebviewDismissed;

        if (reason == BrzPaymentWebviewDismissReason.PaymentSuccess)
        {
            PlayPaymentSuccessEffect();
        }
    }

    async Awaitable CreateOrderAndShowPaymentDialog(BrzPaymentOptionsTheme theme)
    {
        Debug.Log("show dialog clicked");

        if (this.isCreatingOrder)
        {
            Debug.LogWarning("already creating order, please wait");
            return;
        }

        if (this.gameClient == null)
        {
            Debug.LogError("gameClient is not set");
            return;
        }

        this.isCreatingOrder = true;
        CreateOrderResult result = null;
        try
        {
            result = await this.gameClient.CreateOrderAsync(new CreateOrderInput
            {
                ProductId = BREEZE_PRODUCT_ID,
                Quantity = 1,
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateOrderAsync failed. Error: {ex}");
        }
        finally
        {
            this.isCreatingOrder = false;
        }

        if (result == null)
        {
            Debug.LogError("failed to create order");
            return;
        }

        Debug.Log($"order result url: {result.Data.Url}");

        var request = new BrzShowPaymentOptionsDialogRequest()
        {
            Title = "Select payment method",
            ProductDisplayInfo = new BrzProductDisplayInfo()
            {
                DisplayName = "Essential Collection",
                OriginalPrice = "USD $5.99",
                BreezePrice = "USD $4.79",
                Decoration = "Save 20%",
                ProductIconUrl = "https://storage.googleapis.com/beamo-payments-production.appspot.com/logos/breeze_iconv2.png",
            },
            // DirectPaymentUrl = "https://pay.qa.breeze.cash/page_b5c5e6e99f5824fd/pcs_f85b8778a500135fbe75272628a737693d9c4b1a",
            // DirectPaymentUrl = "https://link.qa.breeze.cash/link/plink_22f296fdeecb92ff",
            DirectPaymentUrl = result.Data.Url,
            Data = GAME_PRODUCT_ID,
            Theme = theme,
        };

        Breeze.Instance.OnPaymentOptionsDialogDismissed += this.OnPaymentOptionsDialogDismissed;
        Breeze.Instance.ShowPaymentOptionsDialog(request);
    }

    void OnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason reason, string data)
    {
        Debug.Log($"UI Example, dialog dismissed, reason: {reason}, data: {data}");
        Breeze.Instance.OnPaymentOptionsDialogDismissed -= this.OnPaymentOptionsDialogDismissed;

        if (reason == BrzPaymentDialogDismissReason.AppStoreTapped)
        {
            this.IapPurchase(data);
        }
        else if (reason == BrzPaymentDialogDismissReason.GoogleStoreTapped)
        {
            this.IapPurchase(data);
        }
    }

    void OnPaymentPageResult(string url)
    {
        // dismiss the payment page view
        Breeze.Instance.DismissPaymentPageView();

        var absoluteUrl = Application.absoluteURL;
        Debug.Log($"breeze deep link activated: {url}, unity absolute url: {absoluteUrl}");
        // verify the result on server side
        Debug.Log("verify the payment result on your server");
        // play confetti effect if the payment is successful, make sure verify the result on server side first
        if (IsPaymentSuccessUrl(url))
        {
            PlayPaymentSuccessEffect();
        }
    }

    static bool IsPaymentSuccessUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }
        var uri = new Uri(url);
        // the url doesn't guarantee the payment is successful, make sure verify the result on server side first
        return uri.Host == "breeze-payment" && uri.AbsolutePath == "/purchase/success";
    }

    void PlayPaymentSuccessEffect()
    {
        var speed = 600f;
        var curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        var duration = 5f;
        var axis = (Vector3.left + Vector3.up).normalized;
        this.rotatingCube.StartRotating(speed, curve, duration, axis);
    }
}
