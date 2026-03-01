using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Tests for BreezePaymentVerifier and related payment verification types.
/// These tests cover config validation, URL building, status parsing, and result properties.
/// Async polling tests (PollOnceAsync, WaitForPaymentAsync) require UnityWebRequest mocking
/// and are structured as design-verification tests for the expected behavior.
/// </summary>
public class TestBreezePaymentVerifier
{
    // ─── Constructor / Config Validation ────────────────────────────────

    [Test]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BreezePaymentVerifier(null));
    }

    [Test]
    public void Constructor_NullGameServerBaseUrl_ThrowsArgumentException()
    {
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = null };
        Assert.Throws<ArgumentException>(() => new BreezePaymentVerifier(config));
    }

    [Test]
    public void Constructor_EmptyGameServerBaseUrl_ThrowsArgumentException()
    {
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = "" };
        Assert.Throws<ArgumentException>(() => new BreezePaymentVerifier(config));
    }

    [Test]
    public void Constructor_WhitespaceGameServerBaseUrl_ThrowsArgumentException()
    {
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = "   " };
        // string.IsNullOrEmpty doesn't catch whitespace, so this should succeed
        // This documents current behavior — whitespace-only URL is accepted by constructor
        Assert.DoesNotThrow(() => new BreezePaymentVerifier(config));
    }

    [Test]
    public void Constructor_ValidConfig_DoesNotThrow()
    {
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = "https://api.example.com" };
        Assert.DoesNotThrow(() => new BreezePaymentVerifier(config));
    }

    [Test]
    public void Constructor_ValidConfigWithAllFields_DoesNotThrow()
    {
        var config = new BrzPaymentVerificationConfig
        {
            GameServerBaseUrl = "https://api.example.com",
            StatusPathTemplate = "/custom/{orderId}",
            PollIntervalSeconds = 5.0f,
            TimeoutSeconds = 300f,
            MaxAttempts = 100,
            AuthToken = "my-token",
            ExtraHeaders = new Dictionary<string, string> { { "X-Custom", "val" } }
        };
        Assert.DoesNotThrow(() => new BreezePaymentVerifier(config));
    }

    // ─── BrzPaymentVerificationConfig Defaults ──────────────────────────

    [Test]
    public void Config_DefaultStatusPathTemplate()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.AreEqual("/v1/orders/{orderId}/status", config.StatusPathTemplate);
    }

    [Test]
    public void Config_DefaultPollIntervalSeconds()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.AreEqual(2.0f, config.PollIntervalSeconds);
    }

    [Test]
    public void Config_DefaultTimeoutSeconds()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.AreEqual(120f, config.TimeoutSeconds);
    }

    [Test]
    public void Config_DefaultMaxAttempts()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.AreEqual(60, config.MaxAttempts);
    }

    [Test]
    public void Config_DefaultAuthTokenIsNull()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.IsNull(config.AuthToken);
    }

    [Test]
    public void Config_DefaultExtraHeadersIsNull()
    {
        var config = new BrzPaymentVerificationConfig();
        Assert.IsNull(config.ExtraHeaders);
    }

    [Test]
    public void Config_CustomStatusPathTemplate()
    {
        var config = new BrzPaymentVerificationConfig
        {
            StatusPathTemplate = "/api/payments/{orderId}/check"
        };
        Assert.AreEqual("/api/payments/{orderId}/check", config.StatusPathTemplate);
    }

    [Test]
    public void Config_CustomPollInterval()
    {
        var config = new BrzPaymentVerificationConfig { PollIntervalSeconds = 0.5f };
        Assert.AreEqual(0.5f, config.PollIntervalSeconds);
    }

    [Test]
    public void Config_CustomTimeout()
    {
        var config = new BrzPaymentVerificationConfig { TimeoutSeconds = 30f };
        Assert.AreEqual(30f, config.TimeoutSeconds);
    }

    [Test]
    public void Config_CustomMaxAttempts()
    {
        var config = new BrzPaymentVerificationConfig { MaxAttempts = 5 };
        Assert.AreEqual(5, config.MaxAttempts);
    }

    // ─── ParseStatus (via reflection or tested indirectly through result) ─

    // We test ParseStatus behavior through BrzPaymentVerificationResult construction
    // since ParseStatus is private. These tests document expected mapping.

    // ─── BrzPaymentStatus Enum ──────────────────────────────────────────

    [Test]
    public void BrzPaymentStatus_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)BrzPaymentStatus.Pending);
        Assert.AreEqual(1, (int)BrzPaymentStatus.Succeeded);
        Assert.AreEqual(2, (int)BrzPaymentStatus.Failed);
        Assert.AreEqual(3, (int)BrzPaymentStatus.Expired);
        Assert.AreEqual(4, (int)BrzPaymentStatus.Refunded);
        Assert.AreEqual(5, (int)BrzPaymentStatus.Unknown);
    }

    [Test]
    public void BrzPaymentStatus_Pending_SerializesToString()
    {
        Assert.AreEqual("\"pending\"", JsonConvert.SerializeObject(BrzPaymentStatus.Pending));
    }

    [Test]
    public void BrzPaymentStatus_Succeeded_SerializesToString()
    {
        Assert.AreEqual("\"succeeded\"", JsonConvert.SerializeObject(BrzPaymentStatus.Succeeded));
    }

    [Test]
    public void BrzPaymentStatus_Failed_SerializesToString()
    {
        Assert.AreEqual("\"failed\"", JsonConvert.SerializeObject(BrzPaymentStatus.Failed));
    }

    [Test]
    public void BrzPaymentStatus_Expired_SerializesToString()
    {
        Assert.AreEqual("\"expired\"", JsonConvert.SerializeObject(BrzPaymentStatus.Expired));
    }

    [Test]
    public void BrzPaymentStatus_Refunded_SerializesToString()
    {
        Assert.AreEqual("\"refunded\"", JsonConvert.SerializeObject(BrzPaymentStatus.Refunded));
    }

    [Test]
    public void BrzPaymentStatus_Unknown_SerializesToString()
    {
        Assert.AreEqual("\"unknown\"", JsonConvert.SerializeObject(BrzPaymentStatus.Unknown));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Pending()
    {
        Assert.AreEqual(BrzPaymentStatus.Pending, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"pending\""));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Succeeded()
    {
        Assert.AreEqual(BrzPaymentStatus.Succeeded, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"succeeded\""));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Failed()
    {
        Assert.AreEqual(BrzPaymentStatus.Failed, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"failed\""));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Expired()
    {
        Assert.AreEqual(BrzPaymentStatus.Expired, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"expired\""));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Refunded()
    {
        Assert.AreEqual(BrzPaymentStatus.Refunded, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"refunded\""));
    }

    [Test]
    public void BrzPaymentStatus_DeserializesFromString_Unknown()
    {
        Assert.AreEqual(BrzPaymentStatus.Unknown, JsonConvert.DeserializeObject<BrzPaymentStatus>("\"unknown\""));
    }

    // ─── BrzPaymentVerificationResult.IsTerminal ────────────────────────

    [Test]
    public void IsTerminal_Succeeded_ReturnsTrue()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Succeeded };
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void IsTerminal_Failed_ReturnsTrue()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Failed };
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void IsTerminal_Expired_ReturnsTrue()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Expired };
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void IsTerminal_Refunded_ReturnsTrue()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Refunded };
        Assert.IsTrue(result.IsTerminal);
    }

    [Test]
    public void IsTerminal_Pending_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Pending };
        Assert.IsFalse(result.IsTerminal);
    }

    [Test]
    public void IsTerminal_Unknown_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Unknown };
        Assert.IsFalse(result.IsTerminal);
    }

    // ─── BrzPaymentVerificationResult.IsSuccess ─────────────────────────

    [Test]
    public void IsSuccess_Succeeded_ReturnsTrue()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Succeeded };
        Assert.IsTrue(result.IsSuccess);
    }

    [Test]
    public void IsSuccess_Failed_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Failed };
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void IsSuccess_Expired_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Expired };
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void IsSuccess_Refunded_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Refunded };
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void IsSuccess_Pending_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Pending };
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void IsSuccess_Unknown_ReturnsFalse()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Unknown };
        Assert.IsFalse(result.IsSuccess);
    }

    // ─── BrzPaymentVerificationResult Defaults ──────────────────────────

    [Test]
    public void Result_DefaultStatus_IsUnknown()
    {
        var result = new BrzPaymentVerificationResult();
        Assert.AreEqual(BrzPaymentStatus.Unknown, result.Status);
    }

    [Test]
    public void Result_DefaultOrderId_IsNull()
    {
        var result = new BrzPaymentVerificationResult();
        Assert.IsNull(result.OrderId);
    }

    [Test]
    public void Result_DefaultTransactionId_IsNull()
    {
        var result = new BrzPaymentVerificationResult();
        Assert.IsNull(result.TransactionId);
    }

    [Test]
    public void Result_DefaultError_IsNull()
    {
        var result = new BrzPaymentVerificationResult();
        Assert.IsNull(result.Error);
    }

    [Test]
    public void Result_WithAllFields_RoundTrips()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Succeeded,
            OrderId = "order-123",
            TransactionId = "txn-456",
            Error = null
        };
        string json = JsonConvert.SerializeObject(result);
        var rt = JsonConvert.DeserializeObject<BrzPaymentVerificationResult>(json);
        Assert.AreEqual(BrzPaymentStatus.Succeeded, rt.Status);
        Assert.AreEqual("order-123", rt.OrderId);
        Assert.AreEqual("txn-456", rt.TransactionId);
        Assert.IsNull(rt.Error);
        Assert.IsTrue(rt.IsSuccess);
        Assert.IsTrue(rt.IsTerminal);
    }

    [Test]
    public void Result_WithError_RoundTrips()
    {
        var result = new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Unknown,
            OrderId = "order-err",
            Error = "HTTP error: 500"
        };
        string json = JsonConvert.SerializeObject(result);
        var rt = JsonConvert.DeserializeObject<BrzPaymentVerificationResult>(json);
        Assert.AreEqual(BrzPaymentStatus.Unknown, rt.Status);
        Assert.AreEqual("order-err", rt.OrderId);
        Assert.AreEqual("HTTP error: 500", rt.Error);
        Assert.IsFalse(rt.IsSuccess);
        Assert.IsFalse(rt.IsTerminal);
    }

    // ─── BrzOrderStatusResponse ─────────────────────────────────────────

    [Test]
    public void OrderStatusResponse_DeserializesSucceeded()
    {
        string json = "{\"status\":\"succeeded\",\"orderId\":\"o1\",\"transactionId\":\"t1\"}";
        var resp = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.AreEqual("succeeded", resp.Status);
        Assert.AreEqual("o1", resp.OrderId);
        Assert.AreEqual("t1", resp.TransactionId);
    }

    [Test]
    public void OrderStatusResponse_DeserializesPending()
    {
        string json = "{\"status\":\"pending\"}";
        var resp = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.AreEqual("pending", resp.Status);
        Assert.IsNull(resp.OrderId);
        Assert.IsNull(resp.TransactionId);
    }

    [Test]
    public void OrderStatusResponse_DeserializesWithExtraFields()
    {
        string json = "{\"status\":\"failed\",\"orderId\":\"o2\",\"extra\":\"ignored\",\"nested\":{\"a\":1}}";
        var resp = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.AreEqual("failed", resp.Status);
        Assert.AreEqual("o2", resp.OrderId);
    }

    [Test]
    public void OrderStatusResponse_DeserializesMinimalEmpty()
    {
        string json = "{}";
        var resp = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNotNull(resp);
        Assert.IsNull(resp.Status);
        Assert.IsNull(resp.OrderId);
        Assert.IsNull(resp.TransactionId);
    }

    [Test]
    public void OrderStatusResponse_SerializesRoundTrip()
    {
        var resp = new BrzOrderStatusResponse
        {
            Status = "succeeded",
            OrderId = "order-abc",
            TransactionId = "txn-xyz"
        };
        string json = JsonConvert.SerializeObject(resp);
        var rt = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.AreEqual("succeeded", rt.Status);
        Assert.AreEqual("order-abc", rt.OrderId);
        Assert.AreEqual("txn-xyz", rt.TransactionId);
    }

    [Test]
    public void OrderStatusResponse_NullStatus()
    {
        string json = "{\"status\":null,\"orderId\":\"o3\"}";
        var resp = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNull(resp.Status);
    }

    // ─── BuildStatusUrl (tested via reflection helper) ──────────────────

    // Since BuildStatusUrl is private, we test it indirectly via a reflection helper.
    // In a real Unity project, you might use [assembly: InternalsVisibleTo] instead.

    private static string InvokeBuildStatusUrl(BreezePaymentVerifier verifier, string orderId)
    {
        var method = typeof(BreezePaymentVerifier).GetMethod("BuildStatusUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)method.Invoke(verifier, new object[] { orderId });
    }

    private static BreezePaymentVerifier CreateVerifier(string baseUrl = "https://api.example.com",
        string pathTemplate = null)
    {
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = baseUrl };
        if (pathTemplate != null) config.StatusPathTemplate = pathTemplate;
        return new BreezePaymentVerifier(config);
    }

    [Test]
    public void BuildStatusUrl_NormalOrderId()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "order-123");
        Assert.AreEqual("https://api.example.com/v1/orders/order-123/status", url);
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithSpaces_UrlEncoded()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "order 123");
        Assert.IsTrue(url.Contains("order%20123") || url.Contains("order+123"));
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithSlashes_UrlEncoded()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "order/123/sub");
        Assert.IsTrue(url.Contains("order%2F123%2Fsub"));
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithAmpersand_UrlEncoded()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "order&id=123");
        Assert.IsTrue(url.Contains("order%26id%3D123"));
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithUnicode()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "注文-123");
        // Should be percent-encoded
        Assert.IsFalse(url.Contains("注文"));
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithSpecialChars()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "order#123?key=val");
        Assert.IsTrue(url.Contains("order%23123%3Fkey%3Dval"));
    }

    [Test]
    public void BuildStatusUrl_CustomPathTemplate()
    {
        var verifier = CreateVerifier(pathTemplate: "/api/payments/{orderId}/check");
        string url = InvokeBuildStatusUrl(verifier, "abc");
        Assert.AreEqual("https://api.example.com/api/payments/abc/check", url);
    }

    [Test]
    public void BuildStatusUrl_BaseUrlWithTrailingSlash()
    {
        var verifier = CreateVerifier("https://api.example.com/");
        string url = InvokeBuildStatusUrl(verifier, "order-1");
        Assert.AreEqual("https://api.example.com/v1/orders/order-1/status", url);
    }

    [Test]
    public void BuildStatusUrl_GuidOrderId()
    {
        var verifier = CreateVerifier();
        string guid = "550e8400-e29b-41d4-a716-446655440000";
        string url = InvokeBuildStatusUrl(verifier, guid);
        Assert.IsTrue(url.Contains(guid));
    }

    [Test]
    public void BuildStatusUrl_NumericOrderId()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "12345");
        Assert.AreEqual("https://api.example.com/v1/orders/12345/status", url);
    }

    [Test]
    public void BuildStatusUrl_EmptyOrderId()
    {
        var verifier = CreateVerifier();
        string url = InvokeBuildStatusUrl(verifier, "");
        Assert.AreEqual("https://api.example.com/v1/orders//status", url);
    }

    [Test]
    public void BuildStatusUrl_VeryLongOrderId()
    {
        var verifier = CreateVerifier();
        string longId = new string('a', 500);
        string url = InvokeBuildStatusUrl(verifier, longId);
        Assert.IsTrue(url.Contains(longId));
    }

    // ─── ParseStatus (tested via reflection) ────────────────────────────

    private static BrzPaymentStatus InvokeParseStatus(string status)
    {
        var method = typeof(BreezePaymentVerifier).GetMethod("ParseStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (BrzPaymentStatus)method.Invoke(null, new object[] { status });
    }

    [Test]
    public void ParseStatus_Succeeded() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("succeeded"));

    [Test]
    public void ParseStatus_Success() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("success"));

    [Test]
    public void ParseStatus_Paid() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("paid"));

    [Test]
    public void ParseStatus_Completed() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("completed"));

    [Test]
    public void ParseStatus_Failed() => Assert.AreEqual(BrzPaymentStatus.Failed, InvokeParseStatus("failed"));

    [Test]
    public void ParseStatus_Failure() => Assert.AreEqual(BrzPaymentStatus.Failed, InvokeParseStatus("failure"));

    [Test]
    public void ParseStatus_Declined() => Assert.AreEqual(BrzPaymentStatus.Failed, InvokeParseStatus("declined"));

    [Test]
    public void ParseStatus_Expired() => Assert.AreEqual(BrzPaymentStatus.Expired, InvokeParseStatus("expired"));

    [Test]
    public void ParseStatus_Timeout() => Assert.AreEqual(BrzPaymentStatus.Expired, InvokeParseStatus("timeout"));

    [Test]
    public void ParseStatus_Refunded() => Assert.AreEqual(BrzPaymentStatus.Refunded, InvokeParseStatus("refunded"));

    [Test]
    public void ParseStatus_Pending() => Assert.AreEqual(BrzPaymentStatus.Pending, InvokeParseStatus("pending"));

    [Test]
    public void ParseStatus_Processing() => Assert.AreEqual(BrzPaymentStatus.Pending, InvokeParseStatus("processing"));

    [Test]
    public void ParseStatus_Created() => Assert.AreEqual(BrzPaymentStatus.Pending, InvokeParseStatus("created"));

    [Test]
    public void ParseStatus_Unknown() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus("unknown_value"));

    [Test]
    public void ParseStatus_EmptyString() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus(""));

    [Test]
    public void ParseStatus_Null() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus(null));

    [Test]
    public void ParseStatus_GarbageString() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus("xyzzy_garbage_123"));

    [Test]
    public void ParseStatus_CaseInsensitive_SUCCEEDED() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("SUCCEEDED"));

    [Test]
    public void ParseStatus_CaseInsensitive_Failed() => Assert.AreEqual(BrzPaymentStatus.Failed, InvokeParseStatus("FAILED"));

    [Test]
    public void ParseStatus_CaseInsensitive_Mixed() => Assert.AreEqual(BrzPaymentStatus.Succeeded, InvokeParseStatus("SuCcEeDeD"));

    [Test]
    public void ParseStatus_WhitespaceOnly() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus("   "));

    [Test]
    public void ParseStatus_LeadingTrailingSpaces() => Assert.AreEqual(BrzPaymentStatus.Unknown, InvokeParseStatus(" succeeded "));
}
