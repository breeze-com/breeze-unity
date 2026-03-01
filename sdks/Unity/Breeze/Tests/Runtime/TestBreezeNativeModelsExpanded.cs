using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Expanded tests for BreezeNativeModels — covers payment verification types,
/// edge cases, and additional serialization scenarios.
/// Does NOT duplicate tests from TestBreezeNativeModels.cs.
/// </summary>
public class TestBreezeNativeModelsExpanded
{
    // ─── BrzPaymentDialogDismissReason ──────────────────────────────────

    [Test]
    public void DismissReason_GoogleStoreTapped_HasValue3()
    {
        Assert.AreEqual(3, (int)BrzPaymentDialogDismissReason.GoogleStoreTapped);
    }

    [Test]
    public void DismissReason_CloseTapped_SerializesToString()
    {
        Assert.AreEqual("\"CloseTapped\"", JsonConvert.SerializeObject(BrzPaymentDialogDismissReason.CloseTapped));
    }

    [Test]
    public void DismissReason_DirectPaymentTapped_SerializesToString()
    {
        Assert.AreEqual("\"DirectPaymentTapped\"", JsonConvert.SerializeObject(BrzPaymentDialogDismissReason.DirectPaymentTapped));
    }

    [Test]
    public void DismissReason_AppStoreTapped_SerializesToString()
    {
        Assert.AreEqual("\"AppStoreTapped\"", JsonConvert.SerializeObject(BrzPaymentDialogDismissReason.AppStoreTapped));
    }

    [Test]
    public void DismissReason_GoogleStoreTapped_SerializesToString()
    {
        Assert.AreEqual("\"GoogleStoreTapped\"", JsonConvert.SerializeObject(BrzPaymentDialogDismissReason.GoogleStoreTapped));
    }

    [Test]
    public void DismissReason_DeserializesFromString_CloseTapped()
    {
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"CloseTapped\""));
    }

    [Test]
    public void DismissReason_DeserializesFromString_DirectPaymentTapped()
    {
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"DirectPaymentTapped\""));
    }

    [Test]
    public void DismissReason_DeserializesFromInt()
    {
        Assert.AreEqual(BrzPaymentDialogDismissReason.AppStoreTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("2"));
    }

    [Test]
    public void DismissReason_AllValues_Count()
    {
        var values = Enum.GetValues(typeof(BrzPaymentDialogDismissReason));
        Assert.AreEqual(4, values.Length);
    }

    // ─── BrzPaymentOptionsTheme ─────────────────────────────────────────

    [Test]
    public void Theme_Auto_SerializesToString()
    {
        Assert.AreEqual("\"auto\"", JsonConvert.SerializeObject(BrzPaymentOptionsTheme.Auto));
    }

    [Test]
    public void Theme_DeserializesFromString_Auto()
    {
        Assert.AreEqual(BrzPaymentOptionsTheme.Auto,
            JsonConvert.DeserializeObject<BrzPaymentOptionsTheme>("\"auto\""));
    }

    [Test]
    public void Theme_AllValues_Count()
    {
        var values = Enum.GetValues(typeof(BrzPaymentOptionsTheme));
        Assert.AreEqual(3, values.Length);
    }

    [Test]
    public void Theme_IntValues()
    {
        Assert.AreEqual(0, (int)BrzPaymentOptionsTheme.Auto);
        Assert.AreEqual(1, (int)BrzPaymentOptionsTheme.Light);
        Assert.AreEqual(2, (int)BrzPaymentOptionsTheme.Dark);
    }

    // ─── BrzShowPaymentOptionsDialogRequest edge cases ──────────────────

    [Test]
    public void Request_AllFieldsNull_Serializes()
    {
        var request = new BrzShowPaymentOptionsDialogRequest();
        string json = JsonConvert.SerializeObject(request);
        Assert.IsNotNull(json);
        var rt = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
        Assert.IsNull(rt.Title);
        Assert.IsNull(rt.ProductDisplayInfo);
        Assert.IsNull(rt.DirectPaymentUrl);
        Assert.IsNull(rt.Data);
        Assert.IsNull(rt.Theme);
    }

    [Test]
    public void Request_ThemeAuto_IncludedInJson()
    {
        var request = new BrzShowPaymentOptionsDialogRequest { Theme = BrzPaymentOptionsTheme.Auto };
        string json = JsonConvert.SerializeObject(request);
        Assert.IsTrue(json.Contains("\"theme\":\"auto\""));
    }

    [Test]
    public void Request_WithUnicodeTitle_Serializes()
    {
        var request = new BrzShowPaymentOptionsDialogRequest { Title = "支払いオプション 🎮" };
        string json = JsonConvert.SerializeObject(request);
        var rt = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
        Assert.AreEqual("支払いオプション 🎮", rt.Title);
    }

    [Test]
    public void Request_WithEmptyData_Serializes()
    {
        var request = new BrzShowPaymentOptionsDialogRequest { Data = "" };
        string json = JsonConvert.SerializeObject(request);
        var rt = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
        Assert.AreEqual("", rt.Data);
    }

    [Test]
    public void Request_DeserializesExtraFieldsIgnored()
    {
        string json = "{\"title\":\"T\",\"unknownField\":42,\"nested\":{\"deep\":true}}";
        var request = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
        Assert.AreEqual("T", request.Title);
    }

    [Test]
    public void Request_DeserializesWrongTypeForTheme_Throws()
    {
        // Integer instead of string for theme — should still work due to enum converter
        string json = "{\"theme\":0}";
        var request = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
        Assert.AreEqual(BrzPaymentOptionsTheme.Auto, request.Theme);
    }

    // ─── BrzProductDisplayInfo edge cases ───────────────────────────────

    [Test]
    public void Product_UnicodeEmoji_Serializes()
    {
        var product = new BrzProductDisplayInfo
        {
            DisplayName = "💎 Gem Pack 💎",
            OriginalPrice = "¥1,000",
            BreezePrice = "¥800",
            Decoration = "🔥 20% off 🔥"
        };
        string json = JsonConvert.SerializeObject(product);
        var rt = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
        Assert.AreEqual("💎 Gem Pack 💎", rt.DisplayName);
        Assert.AreEqual("🔥 20% off 🔥", rt.Decoration);
    }

    [Test]
    public void Product_VeryLongDisplayName()
    {
        string longName = new string('X', 10000);
        var product = new BrzProductDisplayInfo { DisplayName = longName };
        string json = JsonConvert.SerializeObject(product);
        var rt = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
        Assert.AreEqual(10000, rt.DisplayName.Length);
    }

    [Test]
    public void Product_EmptyStrings()
    {
        var product = new BrzProductDisplayInfo
        {
            DisplayName = "",
            OriginalPrice = "",
            BreezePrice = "",
            Decoration = "",
            ProductIconUrl = ""
        };
        string json = JsonConvert.SerializeObject(product);
        var rt = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
        Assert.AreEqual("", rt.DisplayName);
        Assert.AreEqual("", rt.ProductIconUrl);
    }

    [Test]
    public void Product_SpecialCharsInStrings()
    {
        var product = new BrzProductDisplayInfo
        {
            DisplayName = "Test \"Product\" with <html> & 'quotes'",
        };
        string json = JsonConvert.SerializeObject(product);
        var rt = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
        Assert.AreEqual("Test \"Product\" with <html> & 'quotes'", rt.DisplayName);
    }

    // ─── BrzPaymentVerificationConfig serialization ─────────────────────

    [Test]
    public void VerificationConfig_AllFields_RoundTrips()
    {
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.test.com",
            StatusPathTemplate = "/custom/{orderId}",
            PollIntervalSeconds = 3.5f,
            TimeoutSeconds = 60f,
            MaxAttempts = 30,
            AuthToken = "bearer-token-123",
            ExtraHeaders = new Dictionary<string, string>
            {
                { "X-Game-Id", "game-1" },
                { "X-Version", "2.0" }
            }
        };
        string json = JsonConvert.SerializeObject(config);
        var rt = JsonConvert.DeserializeObject<BrzPaymentVerificationConfig>(json);
        Assert.AreEqual("https://api.test.com", rt.GameServerBaseUrl);
        Assert.AreEqual("/custom/{orderId}", rt.StatusPathTemplate);
        Assert.AreEqual(3.5f, rt.PollIntervalSeconds);
        Assert.AreEqual(60f, rt.TimeoutSeconds);
        Assert.AreEqual(30, rt.MaxAttempts);
        Assert.AreEqual("bearer-token-123", rt.AuthToken);
        Assert.AreEqual(2, rt.ExtraHeaders.Count);
        Assert.AreEqual("game-1", rt.ExtraHeaders["X-Game-Id"]);
    }

    [Test]
    public void VerificationConfig_NullExtraHeaders_Serializes()
    {
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.test.com"
        };
        string json = JsonConvert.SerializeObject(config);
        var rt = JsonConvert.DeserializeObject<BrzPaymentVerificationConfig>(json);
        Assert.IsNull(rt.ExtraHeaders);
    }

    [Test]
    public void VerificationConfig_EmptyExtraHeaders()
    {
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.test.com",
            ExtraHeaders = new Dictionary<string, string>()
        };
        string json = JsonConvert.SerializeObject(config);
        var rt = JsonConvert.DeserializeObject<BrzPaymentVerificationConfig>(json);
        Assert.IsNotNull(rt.ExtraHeaders);
        Assert.AreEqual(0, rt.ExtraHeaders.Count);
    }

    // ─── BrzOrderStatusResponse edge cases ──────────────────────────────

    [Test]
    public void OrderStatusResponse_WithNullValues_Serializes()
    {
        var resp = new BrzOrderStatusResponse
        {
            Status = null,
            OrderId = null,
            TransactionId = null
        };
        string json = JsonConvert.SerializeObject(resp);
        var rt = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNull(rt.Status);
        Assert.IsNull(rt.OrderId);
    }

    [Test]
    public void OrderStatusResponse_CasePreserved()
    {
        var resp = new BrzOrderStatusResponse { Status = "SUCCEEDED" };
        string json = JsonConvert.SerializeObject(resp);
        Assert.IsTrue(json.Contains("SUCCEEDED"));
    }

    [Test]
    public void OrderStatusResponse_UnicodeOrderId()
    {
        var resp = new BrzOrderStatusResponse { OrderId = "注文-abc-123" };
        string json = JsonConvert.SerializeObject(resp);
        var rt = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.AreEqual("注文-abc-123", rt.OrderId);
    }

    // ─── BrzShowPaymentOptionsResultCode ────────────────────────────────

    [Test]
    public void ResultCode_AllValues_Count()
    {
        var values = Enum.GetValues(typeof(BrzShowPaymentOptionsResultCode));
        Assert.AreEqual(4, values.Length);
    }

    // ─── BrzPaymentStatus enum completeness ─────────────────────────────

    [Test]
    public void PaymentStatus_AllValues_Count()
    {
        var values = Enum.GetValues(typeof(BrzPaymentStatus));
        Assert.AreEqual(6, values.Length);
    }

    [Test]
    public void PaymentStatus_CastFromInt()
    {
        Assert.AreEqual(BrzPaymentStatus.Pending, (BrzPaymentStatus)0);
        Assert.AreEqual(BrzPaymentStatus.Succeeded, (BrzPaymentStatus)1);
        Assert.AreEqual(BrzPaymentStatus.Failed, (BrzPaymentStatus)2);
        Assert.AreEqual(BrzPaymentStatus.Expired, (BrzPaymentStatus)3);
        Assert.AreEqual(BrzPaymentStatus.Refunded, (BrzPaymentStatus)4);
        Assert.AreEqual(BrzPaymentStatus.Unknown, (BrzPaymentStatus)5);
    }

    // ─── BreezeEnvironment ──────────────────────────────────────────────

    [Test]
    public void BreezeEnvironment_Production_IsZero()
    {
        Assert.AreEqual(0, (int)BreezeEnvironment.Production);
    }

    [Test]
    public void BreezeEnvironment_Development_IsOne()
    {
        Assert.AreEqual(1, (int)BreezeEnvironment.Development);
    }

    [Test]
    public void BreezeEnvironment_AllValues_Count()
    {
        Assert.AreEqual(2, Enum.GetValues(typeof(BreezeEnvironment)).Length);
    }
}
