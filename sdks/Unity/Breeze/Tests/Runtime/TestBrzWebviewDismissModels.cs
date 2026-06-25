using System;
using NUnit.Framework;
using Newtonsoft.Json;

namespace BreezeSdk.Runtime.Tests
{
    /// <summary>
    /// Tests for BrzPaymentWebviewDismissReason and WebViewDismissedPayload serialization.
    /// Complements TestAndroidCallbackParsing.cs (deserialization only) with serialization
    /// and standalone enum coverage.
    /// </summary>
    public class TestBrzWebviewDismissModels
    {
        // ─── BrzPaymentWebviewDismissReason: int values ──────────────────────

        [Test]
        public void WebviewDismissReason_Dismissed_IsZero()
        {
            Assert.AreEqual(0, (int)BrzPaymentWebviewDismissReason.Dismissed);
        }

        [Test]
        public void WebviewDismissReason_PaymentSuccess_IsOne()
        {
            Assert.AreEqual(1, (int)BrzPaymentWebviewDismissReason.PaymentSuccess);
        }

        [Test]
        public void WebviewDismissReason_PaymentFailure_IsTwo()
        {
            Assert.AreEqual(2, (int)BrzPaymentWebviewDismissReason.PaymentFailure);
        }

        [Test]
        public void WebviewDismissReason_LoadError_IsThree()
        {
            Assert.AreEqual(3, (int)BrzPaymentWebviewDismissReason.LoadError);
        }

        [Test]
        public void WebviewDismissReason_AllValues_Count()
        {
            var values = Enum.GetValues(typeof(BrzPaymentWebviewDismissReason));
            Assert.AreEqual(4, values.Length);
        }

        // ─── BrzPaymentWebviewDismissReason: serialization ───────────────────

        [Test]
        public void WebviewDismissReason_Dismissed_SerializesToString()
        {
            Assert.AreEqual("\"Dismissed\"", JsonConvert.SerializeObject(BrzPaymentWebviewDismissReason.Dismissed));
        }

        [Test]
        public void WebviewDismissReason_PaymentSuccess_SerializesToString()
        {
            Assert.AreEqual("\"PaymentSuccess\"", JsonConvert.SerializeObject(BrzPaymentWebviewDismissReason.PaymentSuccess));
        }

        [Test]
        public void WebviewDismissReason_PaymentFailure_SerializesToString()
        {
            Assert.AreEqual("\"PaymentFailure\"", JsonConvert.SerializeObject(BrzPaymentWebviewDismissReason.PaymentFailure));
        }

        [Test]
        public void WebviewDismissReason_LoadError_SerializesToString()
        {
            Assert.AreEqual("\"LoadError\"", JsonConvert.SerializeObject(BrzPaymentWebviewDismissReason.LoadError));
        }

        // ─── BrzPaymentWebviewDismissReason: deserialization ─────────────────

        [Test]
        public void WebviewDismissReason_DeserializesFromString_Dismissed()
        {
            Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed,
                JsonConvert.DeserializeObject<BrzPaymentWebviewDismissReason>("\"Dismissed\""));
        }

        [Test]
        public void WebviewDismissReason_DeserializesFromString_PaymentSuccess()
        {
            Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentSuccess,
                JsonConvert.DeserializeObject<BrzPaymentWebviewDismissReason>("\"PaymentSuccess\""));
        }

        [Test]
        public void WebviewDismissReason_DeserializesFromString_PaymentFailure()
        {
            Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentFailure,
                JsonConvert.DeserializeObject<BrzPaymentWebviewDismissReason>("\"PaymentFailure\""));
        }

        [Test]
        public void WebviewDismissReason_DeserializesFromString_LoadError()
        {
            Assert.AreEqual(BrzPaymentWebviewDismissReason.LoadError,
                JsonConvert.DeserializeObject<BrzPaymentWebviewDismissReason>("\"LoadError\""));
        }

        [Test]
        public void WebviewDismissReason_DeserializesFromInt()
        {
            Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentSuccess,
                JsonConvert.DeserializeObject<BrzPaymentWebviewDismissReason>("1"));
        }

        // ─── WebViewDismissedPayload: serialization ──────────────────────────

        [Test]
        public void WebViewPayload_Serializes_WithReasonAndData()
        {
            var payload = new WebViewDismissedPayload
            {
                Reason = BrzPaymentWebviewDismissReason.PaymentSuccess,
                Data = "{\"receiptId\":\"abc123\"}"
            };
            string json = JsonConvert.SerializeObject(payload);
            Assert.IsTrue(json.Contains("\"PaymentSuccess\""));
            Assert.IsTrue(json.Contains("abc123"));
        }

        [Test]
        public void WebViewPayload_Roundtrip_PreservesAllFields()
        {
            var payload = new WebViewDismissedPayload
            {
                Reason = BrzPaymentWebviewDismissReason.PaymentFailure,
                Data = "error details"
            };
            string json = JsonConvert.SerializeObject(payload);
            var rt = JsonConvert.DeserializeObject<WebViewDismissedPayload>(json);
            Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentFailure, rt.Reason);
            Assert.AreEqual("error details", rt.Data);
        }

        [Test]
        public void WebViewPayload_Serializes_WithNullData()
        {
            var payload = new WebViewDismissedPayload
            {
                Reason = BrzPaymentWebviewDismissReason.Dismissed,
                Data = null
            };
            string json = JsonConvert.SerializeObject(payload);
            var rt = JsonConvert.DeserializeObject<WebViewDismissedPayload>(json);
            Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed, rt.Reason);
            Assert.IsNull(rt.Data);
        }

        [Test]
        public void WebViewPayload_Serializes_LoadError()
        {
            var payload = new WebViewDismissedPayload
            {
                Reason = BrzPaymentWebviewDismissReason.LoadError
            };
            string json = JsonConvert.SerializeObject(payload);
            Assert.IsTrue(json.Contains("\"LoadError\""));
        }

        [Test]
        public void WebViewPayload_Roundtrip_Dismissed_WithEmptyData()
        {
            var payload = new WebViewDismissedPayload
            {
                Reason = BrzPaymentWebviewDismissReason.Dismissed,
                Data = ""
            };
            string json = JsonConvert.SerializeObject(payload);
            var rt = JsonConvert.DeserializeObject<WebViewDismissedPayload>(json);
            Assert.AreEqual(BrzPaymentWebviewDismissReason.Dismissed, rt.Reason);
            Assert.AreEqual("", rt.Data);
        }
    }
}
