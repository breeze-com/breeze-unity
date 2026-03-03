using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Integration / E2E flow tests for the Breeze payment SDK.
/// These test the logical flow and data contracts between components.
/// Actual async network calls require Unity runtime mocking.
/// </summary>
public class TestBreezeIntegration
{
    [SetUp]
    public void SetUp()
    {
        Breeze.Uninitialize();
    }

    [TearDown]
    public void TearDown()
    {
        Breeze.Uninitialize();
    }

    // ─── Payment Dialog → Verification Flow ─────────────────────────────

    [Test]
    public void Flow_CreateDialogRequest_WithAllFields()
    {
        var request = new BrzShowPaymentOptionsDialogRequest
        {
            Title = "Buy Gems",
            ProductDisplayInfo = new BrzProductDisplayInfo
            {
                DisplayName = "100 Gems",
                OriginalPrice = "$9.99",
                BreezePrice = "$7.99",
                Decoration = "20% off",
                ProductIconUrl = "https://cdn.example.com/gems.png"
            },
            DirectPaymentUrl = "https://pay.breeze.com/checkout/order-123",
            Data = "{\"orderId\":\"order-123\",\"productId\":\"gems-100\"}",
            Theme = BrzPaymentOptionsTheme.Dark
        };

        string json = JsonConvert.SerializeObject(request);
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("order-123"));
    }


    // ─── Data round-trip: order creation → dialog → verification ────────

    [Test]
    public void Flow_OrderDataPassedThroughDialog()
    {
        string orderId = "order-" + Guid.NewGuid().ToString("N");
        var request = new BrzShowPaymentOptionsDialogRequest
        {
            Data = JsonConvert.SerializeObject(new { orderId = orderId, amount = 999 })
        };

        // Simulate serialization that native bridge would do
        string json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);

        // Extract orderId back from data
        var dataObj = JsonConvert.DeserializeAnonymousType(deserialized.Data, new { orderId = "", amount = 0 });
        Assert.AreEqual(orderId, dataObj.orderId);
        Assert.AreEqual(999, dataObj.amount);
    }

    // ─── DismissReason → Verification behavior ─────────────────────────

    [Test]
    public void Flow_AppStoreTapped_NonUSStorefront()
    {
        // When user is in a non-US region and taps App Store
        var culture = new System.Globalization.CultureInfo("ja-JP");
        string region = BreezeHelper.GetCurrentTwoLetterIsoRegionName(culture);
        Assert.AreEqual("JP", region);
        // SDK should route to App Store IAP — this is handled natively
    }
}
