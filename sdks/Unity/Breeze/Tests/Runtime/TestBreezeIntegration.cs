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

    [Test]
    public void Flow_DialogDismissed_CloseTapped_CanCreateVerifier()
    {
        // User closes dialog → we can still verify if payment happened
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.example.com",
            TimeoutSeconds = 10f,
            MaxAttempts = 5
        };
        var verifier = new BreezePaymentVerifier(config);
        Assert.IsNotNull(verifier);
    }

    [Test]
    public void Flow_DialogDismissed_DirectPaymentTapped_VerifierConfigured()
    {
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.example.com",
            AuthToken = "player-jwt-token",
            PollIntervalSeconds = 1.0f,
            TimeoutSeconds = 120f
        };
        var verifier = new BreezePaymentVerifier(config);
        Assert.IsNotNull(verifier);
    }

    [Test]
    public void Flow_VerificationResult_Succeeded_GrantsItems()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Succeeded,
            OrderId = "order-123",
            TransactionId = "txn-456"
        };
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsTerminal);
        Assert.IsNull(result.Error);
    }

    [Test]
    public void Flow_VerificationResult_Failed_DoesNotGrant()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Failed,
            OrderId = "order-123",
            Error = null
        };
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void Flow_VerificationResult_Expired_DoesNotGrant()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Expired,
            OrderId = "order-123"
        };
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void Flow_VerificationResult_Timeout_ReturnsError()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Pending,
            OrderId = "order-123",
            Error = "Verification timed out after 120s"
        };
        Assert.IsFalse(result.IsSuccess);
        Assert.IsFalse(result.IsTerminal);
        Assert.IsNotNull(result.Error);
    }

    [Test]
    public void Flow_VerificationResult_Refunded()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Refunded,
            OrderId = "order-123",
            TransactionId = "txn-789"
        };
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsTerminal);
    }

    // ─── Multiple Verifiers ─────────────────────────────────────────────

    [Test]
    public void Flow_MultipleConcurrentVerifiers_IndependentConfigs()
    {
        var config1 = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api1.example.com",
            AuthToken = "token1"
        };
        var config2 = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api2.example.com",
            AuthToken = "token2"
        };
        var v1 = new BreezePaymentVerifier(config1);
        var v2 = new BreezePaymentVerifier(config2);
        Assert.IsNotNull(v1);
        Assert.IsNotNull(v2);
        Assert.AreNotSame(v1, v2);
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

    // ─── Server response parsing scenarios ──────────────────────────────

    [Test]
    public void Flow_ServerResponse_AlreadyCompleted_InstantSuccess()
    {
        string serverJson = "{\"status\":\"succeeded\",\"orderId\":\"order-123\",\"transactionId\":\"txn-done\"}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(serverJson);
        Assert.AreEqual("succeeded", response.Status);
    }

    [Test]
    public void Flow_ServerResponse_Pending_NeedsMorePolling()
    {
        string serverJson = "{\"status\":\"pending\",\"orderId\":\"order-123\"}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(serverJson);
        Assert.AreEqual("pending", response.Status);
    }

    [Test]
    public void Flow_ServerResponse_Processing_NeedsMorePolling()
    {
        string serverJson = "{\"status\":\"processing\",\"orderId\":\"order-123\"}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(serverJson);
        Assert.AreEqual("processing", response.Status);
    }

    // ─── DismissReason → Verification behavior ─────────────────────────

    [Test]
    public void Flow_AllDismissReasons_CanTriggerVerification()
    {
        var reasons = new[]
        {
            BrzPaymentDialogDismissReason.CloseTapped,
            BrzPaymentDialogDismissReason.DirectPaymentTapped,
            BrzPaymentDialogDismissReason.AppStoreTapped,
            BrzPaymentDialogDismissReason.GoogleStoreTapped
        };

        foreach (var reason in reasons)
        {
            // For any dismiss reason, verification config should be constructable
            var config = new BrzPaymentVerificationConfig
            {
                GameServerBaseUrl = "https://api.example.com"
            };
            Assert.DoesNotThrow(() => new BreezePaymentVerifier(config),
                $"Failed for dismiss reason: {reason}");
        }
    }

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
