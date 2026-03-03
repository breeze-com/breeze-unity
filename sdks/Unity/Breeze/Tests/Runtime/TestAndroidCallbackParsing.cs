using System;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Tests for Android callback JSON parsing.
/// DialogDismissedPayload.Reason and WebViewDismissedPayload.Reason are enums
/// with StringEnumConverter, so they support both int and string deserialization.
/// </summary>
public class TestAndroidCallbackParsing
{
    // ─── DialogDismissedPayload: Int-based reason ────────────────────────

    [Test]
    public void IntReason_CloseTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":0,\"data\":\"d\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped, payload.Reason);
    }

    [Test]
    public void IntReason_DirectPaymentTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":1,\"data\":null}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped, payload.Reason);
    }

    [Test]
    public void IntReason_AppStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":2}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.AppStoreTapped, payload.Reason);
    }

    [Test]
    public void IntReason_GoogleStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":3}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.GoogleStoreTapped, payload.Reason);
    }

    // ─── DialogDismissedPayload: String-based reason ─────────────────────

    [Test]
    public void StringReason_CloseTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":\"CloseTapped\",\"data\":\"test\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped, payload.Reason);
    }

    [Test]
    public void StringReason_DirectPaymentTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":\"DirectPaymentTapped\",\"data\":\"url\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped, payload.Reason);
    }

    [Test]
    public void StringReason_AppStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":\"AppStoreTapped\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.AppStoreTapped, payload.Reason);
    }

    [Test]
    public void StringReason_GoogleStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":\"GoogleStoreTapped\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.GoogleStoreTapped, payload.Reason);
    }

    // ─── DialogDismissedPayload: Enum deserialization (standalone) ───────

    [Test]
    public void EnumReason_StringDeserialization_Works()
    {
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"CloseTapped\""));
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"DirectPaymentTapped\""));
    }

    [Test]
    public void EnumReason_IntDeserialization_Works()
    {
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("0"));
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("1"));
    }

    // ─── DialogDismissedPayload: Null/missing reason ─────────────────────

    [Test]
    public void NullReason_DefaultsToCloseTapped()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"data\":\"test\"}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped, payload.Reason);
    }

    [Test]
    public void EmptyPayload_DefaultsToCloseTapped()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped, payload.Reason);
        Assert.IsNull(payload.Data);
    }

    // ─── DialogDismissedPayload: Unknown reason values ───────────────────

    [Test]
    public void UnknownIntReason_CastsWithoutError()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":99}");
        Assert.AreEqual(99, (int)payload.Reason);
    }

    // ─── WebViewDismissedPayload: Int-based reason ───────────────────────

    [Test]
    public void WebView_IntReason_Dismissed_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":0,\"data\":\"d\"}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed, payload.Reason);
    }

    [Test]
    public void WebView_IntReason_PaymentSuccess_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":1}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentSuccess, payload.Reason);
    }

    [Test]
    public void WebView_IntReason_PaymentFailure_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":2}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentFailure, payload.Reason);
    }

    [Test]
    public void WebView_IntReason_LoadError_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":3}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.LoadError, payload.Reason);
    }

    // ─── WebViewDismissedPayload: String-based reason ────────────────────

    [Test]
    public void WebView_StringReason_Dismissed_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":\"Dismissed\"}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed, payload.Reason);
    }

    [Test]
    public void WebView_StringReason_PaymentSuccess_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":\"PaymentSuccess\",\"data\":\"receipt\"}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentSuccess, payload.Reason);
        Assert.AreEqual("receipt", payload.Data);
    }

    [Test]
    public void WebView_StringReason_PaymentFailure_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":\"PaymentFailure\",\"data\":\"error_msg\"}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentFailure, payload.Reason);
        Assert.AreEqual("error_msg", payload.Data);
    }

    [Test]
    public void WebView_StringReason_LoadError_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":\"LoadError\"}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.LoadError, payload.Reason);
    }

    // ─── WebViewDismissedPayload: Null/missing/unknown ───────────────────

    [Test]
    public void WebView_EmptyPayload_DefaultsToDismissed()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{}");
        Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed, payload.Reason);
        Assert.IsNull(payload.Data);
    }

    [Test]
    public void WebView_UnknownIntReason_CastsWithoutError()
    {
        var payload = JsonConvert.DeserializeObject<WebViewDismissedPayload>("{\"reason\":99}");
        Assert.AreEqual(99, (int)payload.Reason);
    }
}
