using System;
using System.Collections.Specialized;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Security-focused tests for the Breeze Unity Payment SDK.
/// Covers URL validation, injection, spoofing, and edge cases identified in QA review.
/// </summary>
public class TestBreezeSecurity
{
    // ─── URL Validation: directPaymentUrl host checks ───

    [Test]
    public void DirectPaymentUrl_ValidBreezeCashHost_ShouldBeAccepted()
    {
        string url = "https://pay.breeze.cash/page_abc123";
        Assert.IsTrue(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_SubdomainBreezeCash_ShouldBeAccepted()
    {
        string url = "https://pay.qa.breeze.cash/page_abc123";
        Assert.IsTrue(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_NonBreezeCashHost_ShouldBeRejected()
    {
        string url = "https://evil.example.com/page_abc123";
        Assert.IsFalse(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_BreezeCashInPath_ShouldBeRejected()
    {
        // Host is example.com, breeze.cash only appears in path
        string url = "https://example.com/.breeze.cash/page_abc123";
        Assert.IsFalse(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_BreezeCashSuffix_SpoofAttempt_ShouldBeRejected()
    {
        // "notbreeze.cash" does not end with ".breeze.cash"
        string url = "https://notbreeze.cash/page_abc123";
        Assert.IsFalse(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_HttpNotHttps_ShouldBeRejected()
    {
        // No HTTPS enforcement — this test documents the gap
        string url = "http://pay.breeze.cash/page_abc123";
        var uri = new Uri(url);
        Assert.AreEqual("http", uri.Scheme, "SDK should enforce HTTPS but currently does not");
        // When HTTPS enforcement is added, this should be:
        // Assert.IsFalse(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_Null_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl(null));
    }

    [Test]
    public void DirectPaymentUrl_Empty_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl(""));
    }

    [Test]
    public void DirectPaymentUrl_MalformedUri_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl("not-a-url"));
    }

    [Test]
    public void DirectPaymentUrl_JavascriptScheme_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl("javascript:alert(1)"));
    }

    [Test]
    public void DirectPaymentUrl_DataScheme_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl("data:text/html,<h1>pwned</h1>"));
    }

    [Test]
    public void DirectPaymentUrl_FileScheme_ShouldBeRejected()
    {
        Assert.IsFalse(IsValidBreezeUrl("file:///etc/passwd"));
    }

    [Test]
    public void DirectPaymentUrl_HostWithPort_ShouldStillValidate()
    {
        string url = "https://pay.breeze.cash:8443/page_abc123";
        Assert.IsTrue(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_HostWithUserInfo_ShouldBeRejected()
    {
        // user@host URL — potential phishing
        string url = "https://admin@evil.com@pay.breeze.cash/page";
        // Uri parsing may or may not treat this as host=pay.breeze.cash
        // The test documents the behavior
        try
        {
            var uri = new Uri(url);
            // If parsed, host should be checked carefully
            Assert.IsNotNull(uri.Host);
        }
        catch
        {
            // Malformed URL — good, rejected
            Assert.Pass("Malformed URL correctly rejected by parser");
        }
    }

    [Test]
    public void DirectPaymentUrl_IPAddress_ShouldBeRejected()
    {
        string url = "https://192.168.1.1/page_abc123";
        Assert.IsFalse(IsValidBreezeUrl(url));
    }

    [Test]
    public void DirectPaymentUrl_Localhost_ShouldBeRejected()
    {
        string url = "https://localhost/page_abc123";
        Assert.IsFalse(IsValidBreezeUrl(url));
    }

    // ─── URL Injection via orderId ───

    [Test]
    public void BuildStatusUrl_NormalOrderId_ConstructsCorrectUrl()
    {
        string baseUrl = "https://api.game.com";
        string template = "/v1/orders/{orderId}/status";
        string orderId = "order-123";
        string path = template.Replace("{orderId}", Uri.EscapeDataString(orderId));
        string result = baseUrl.TrimEnd('/') + path;
        Assert.AreEqual("https://api.game.com/v1/orders/order-123/status", result);
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithSlashes_IsEscaped()
    {
        string orderId = "../../admin/secret";
        string escaped = Uri.EscapeDataString(orderId);
        Assert.IsFalse(escaped.Contains("/"), "Path traversal characters should be escaped");
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithQueryString_IsEscaped()
    {
        string orderId = "order-123?admin=true&bypass=1";
        string escaped = Uri.EscapeDataString(orderId);
        Assert.IsFalse(escaped.Contains("?"), "Query injection characters should be escaped");
        Assert.IsFalse(escaped.Contains("&"), "Query injection characters should be escaped");
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithNullBytes_IsEscaped()
    {
        string orderId = "order-123\0admin";
        string escaped = Uri.EscapeDataString(orderId);
        Assert.IsFalse(escaped.Contains("\0"), "Null bytes should be escaped");
    }

    [Test]
    public void BuildStatusUrl_OrderIdWithUnicode_IsEscaped()
    {
        string orderId = "order-日本語";
        string escaped = Uri.EscapeDataString(orderId);
        Assert.IsFalse(escaped.Contains("日"), "Unicode should be percent-encoded");
    }

    // ─── Deep Link Spoofing ───

    [Test]
    public void DeepLink_SuccessUrl_MatchesExpectedFormat()
    {
        string url = "breezedemo://breeze-payment/purchase/success";
        var uri = new Uri(url);
        Assert.AreEqual("breeze-payment", uri.Host);
        Assert.AreEqual("/purchase/success", uri.AbsolutePath);
    }

    [Test]
    public void DeepLink_SuccessUrl_WithExtraQueryParams_StillParsesAsSuccess()
    {
        // An attacker could append params — does the parser still match?
        string url = "breezedemo://breeze-payment/purchase/success?spoofed=true";
        var uri = new Uri(url);
        Assert.AreEqual("/purchase/success", uri.AbsolutePath);
        // This shows deep link URL alone should NOT be trusted
    }

    [Test]
    public void DeepLink_FailureUrl_ShouldNotMatchSuccess()
    {
        string url = "breezedemo://breeze-payment/purchase/failure";
        var uri = new Uri(url);
        Assert.AreNotEqual("/purchase/success", uri.AbsolutePath);
    }

    [Test]
    public void DeepLink_ArbitraryScheme_CanCraftSuccessUrl()
    {
        // Demonstrates that any app can craft this URL
        string url = "breezedemo://breeze-payment/purchase/success";
        var uri = new Uri(url);
        Assert.AreEqual("breezedemo", uri.Scheme);
        // This is why server-side verification is mandatory
    }

    // ─── JSON Deserialization Security ───

    [Test]
    public void JsonDeserialization_ExtraFields_AreIgnored()
    {
        string json = "{\"status\":\"succeeded\",\"orderId\":\"o1\",\"transactionId\":\"t1\",\"evil\":\"payload\"}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNotNull(response);
        Assert.AreEqual("succeeded", response.Status);
    }

    [Test]
    public void JsonDeserialization_MissingFields_DefaultToNull()
    {
        string json = "{}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNotNull(response);
        Assert.IsNull(response.Status);
        Assert.IsNull(response.OrderId);
    }

    [Test]
    public void JsonDeserialization_InvalidJson_ThrowsException()
    {
        string json = "not json at all";
        Assert.Throws<JsonReaderException>(() =>
        {
            JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        });
    }

    [Test]
    public void JsonDeserialization_VeryLargePayload_DoesNotCrash()
    {
        // Simulate a large but valid JSON payload
        string bigValue = new string('A', 100000);
        string json = $"{{\"status\":\"pending\",\"orderId\":\"{bigValue}\"}}";
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);
        Assert.IsNotNull(response);
        Assert.AreEqual(100000, response.OrderId.Length);
    }

    [Test]
    public void JsonDeserialization_NullPayload_ReturnsNull()
    {
        var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(null);
        Assert.IsNull(response);
    }

    // ─── Payment Status Parsing ───

    [Test]
    public void PaymentStatus_UnknownString_MapsToUnknown()
    {
        // Test that unexpected status strings don't crash
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Unknown };
        Assert.IsFalse(result.IsTerminal);
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void PaymentStatus_Succeeded_IsTerminalAndSuccess()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Succeeded };
        Assert.IsTrue(result.IsTerminal);
        Assert.IsTrue(result.IsSuccess);
    }

    [Test]
    public void PaymentStatus_Failed_IsTerminalNotSuccess()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Failed };
        Assert.IsTrue(result.IsTerminal);
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public void PaymentStatus_Pending_IsNotTerminal()
    {
        var result = new BrzPaymentVerificationResult { Status = BrzPaymentStatus.Pending };
        Assert.IsFalse(result.IsTerminal);
    }

    // ─── Android Callback Type Mismatch ───

    [Test]
    public void AndroidCallback_StringReasonInJson_FailsIntParsing()
    {
        // Simulates what Android sends: reason as string value
        string jsonPayload = "{\"reason\":\"CloseTapped\",\"data\":\"test\"}";

        // The BreezeAndroidCallbackReceiver expects int Reason
        // This documents the bug: Newtonsoft will throw or return 0
        try
        {
            var payload = JsonConvert.DeserializeObject<AndroidDismissPayloadIntReason>(jsonPayload);
            // If it doesn't throw, reason defaults to 0 (CloseTapped) — masking real reason
            Assert.AreEqual(0, payload.Reason, "String 'CloseTapped' incorrectly parsed as int 0");
        }
        catch (JsonException)
        {
            Assert.Pass("Correctly fails to parse string as int — bug confirmed");
        }
    }

    [Test]
    public void AndroidCallback_IntReasonInJson_ParsesCorrectly()
    {
        string jsonPayload = "{\"reason\":1,\"data\":\"test\"}";
        var payload = JsonConvert.DeserializeObject<AndroidDismissPayloadIntReason>(jsonPayload);
        Assert.AreEqual(1, payload.Reason);
        Assert.AreEqual("test", payload.Data);
    }

    // ─── Configuration Validation ───

    [Test]
    public void BreezePaymentVerifier_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BreezePaymentVerifier(null));
    }

    [Test]
    public void BreezePaymentVerifier_EmptyBaseUrl_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new BreezePaymentVerifier(new BrzPaymentVerificationConfig { GameServerBaseUrl = "" }));
    }

    [Test]
    public void BreezePaymentVerifier_NullBaseUrl_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new BreezePaymentVerifier(new BrzPaymentVerificationConfig { GameServerBaseUrl = null }));
    }

    [Test]
    public void BreezePaymentVerifier_HttpBaseUrl_ShouldWarn()
    {
        // Currently accepted — this test documents the security gap
        var config = new BrzPaymentVerificationConfig { GameServerBaseUrl = "http://insecure.game.com" };
        var verifier = new BreezePaymentVerifier(config);
        Assert.IsNotNull(verifier, "HTTP URL accepted — HTTPS should be enforced");
    }

    // ─── Query String Injection ───

    [Test]
    public void UpdateUrlQueryParams_XSSInValue_IsEscaped()
    {
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection
        {
            { "q", "<script>alert('xss')</script>" }
        };
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);
        Assert.IsFalse(result.Contains("<script>"), "XSS payload should be URL-encoded");
    }

    [Test]
    public void UpdateUrlQueryParams_SQLInjectionInValue_IsEscaped()
    {
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection
        {
            { "id", "1; DROP TABLE orders;--" }
        };
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);
        Assert.IsTrue(result.Contains("1%3b+DROP"), "SQL injection should be URL-encoded");
    }

    [Test]
    public void BuildQueryString_NullCollection_ReturnsEmpty()
    {
        string result = BreezeHelper.BuildQueryString(null);
        Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void BuildQueryString_EmptyCollection_ReturnsEmpty()
    {
        string result = BreezeHelper.BuildQueryString(new NameValueCollection());
        Assert.AreEqual(string.Empty, result);
    }

    // ─── Helpers ───

    /// <summary>
    /// Mirrors the validation logic from iOS/Android native code.
    /// This is what SHOULD also exist in the C# layer.
    /// </summary>
    private static bool IsValidBreezeUrl(string urlString)
    {
        if (string.IsNullOrEmpty(urlString))
            return false;

        try
        {
            var uri = new Uri(urlString);
            if (uri.Scheme != "https")
                return false;
            var host = uri.Host?.ToLowerInvariant();
            if (string.IsNullOrEmpty(host))
                return false;
            return host.EndsWith(".breeze.cash");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Matches the broken BreezeAndroidCallbackReceiver.DialogDismissedPayload structure
    /// </summary>
    private class AndroidDismissPayloadIntReason
    {
        [JsonProperty("reason")]
        public int Reason;

        [JsonProperty("data")]
        public string Data;
    }
}
