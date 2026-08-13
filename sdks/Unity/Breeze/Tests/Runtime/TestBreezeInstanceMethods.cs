using System;
using NUnit.Framework;
using Newtonsoft.Json;

namespace BreezeSdk.Runtime.Tests
{
    /// <summary>
    /// Tests for Breeze instance properties and methods not covered elsewhere:
    /// SuccessReturnUrl, FailureReturnUrl, AppScheme, IsPaymentSuccessUrl,
    /// NotifyOnPaymentWebviewDismissed, BrzShowPaymentWebviewRequest serialization,
    /// BrzShowPaymentWebviewResultCode, and BrzPaymentWebviewDismissReason.
    /// Does NOT duplicate tests from TestBreezeSingleton.cs.
    /// </summary>
    public class TestBreezeInstanceMethods
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

        // ─── SuccessReturnUrl / FailureReturnUrl / AppScheme ────────────────

        [Test]
        public void SuccessReturnUrl_ContainsAppSchemeAndSuccessPath()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.AreEqual("mygame://breeze-payment/purchase/success", Breeze.Instance.SuccessReturnUrl);
        }

        [Test]
        public void FailureReturnUrl_ContainsAppSchemeAndFailurePath()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.AreEqual("mygame://breeze-payment/purchase/failure", Breeze.Instance.FailureReturnUrl);
        }

        [Test]
        public void AppScheme_ReflectsConfiguredValue()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.AreEqual("mygame", Breeze.Instance.AppScheme);
        }

        [Test]
        public void SuccessAndFailureUrls_ShareSchemeAndHost_DifferInPath()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "game" });
            var success = new Uri(Breeze.Instance.SuccessReturnUrl);
            var failure = new Uri(Breeze.Instance.FailureReturnUrl);
            Assert.AreEqual(success.Scheme, failure.Scheme);
            Assert.AreEqual(success.Host, failure.Host);
            Assert.AreNotEqual(success.AbsolutePath, failure.AbsolutePath);
        }

        // ─── IsPaymentSuccessUrl ────────────────────────────────────────────

        [Test]
        public void IsPaymentSuccessUrl_Null_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl(null));
        }

        [Test]
        public void IsPaymentSuccessUrl_Empty_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl(string.Empty));
        }

        [Test]
        public void IsPaymentSuccessUrl_ExactSuccessUrl_ReturnsTrue()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsTrue(Breeze.Instance.IsPaymentSuccessUrl(Breeze.Instance.SuccessReturnUrl));
        }

        [Test]
        public void IsPaymentSuccessUrl_FailureUrl_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl(Breeze.Instance.FailureReturnUrl));
        }

        [Test]
        public void IsPaymentSuccessUrl_WrongScheme_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("othergame://breeze-payment/purchase/success"));
        }

        [Test]
        public void IsPaymentSuccessUrl_WrongHost_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("mygame://wrong-host/purchase/success"));
        }

        [Test]
        public void IsPaymentSuccessUrl_WrongPath_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("mygame://breeze-payment/other/path"));
        }

        [Test]
        public void IsPaymentSuccessUrl_ExtraPathSegment_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("mygame://breeze-payment/purchase/success/extra"));
        }

        [Test]
        public void IsPaymentSuccessUrl_WithQueryParams_ReturnsTrue()
        {
            // Uri.AbsolutePath excludes the query string so the path check still matches.
            // Deep links must always be verified server-side regardless of this result.
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsTrue(Breeze.Instance.IsPaymentSuccessUrl("mygame://breeze-payment/purchase/success?token=abc"));
        }

        [Test]
        public void IsPaymentSuccessUrl_HttpsScheme_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("https://breeze-payment/purchase/success"));
        }

        [Test]
        public void IsPaymentSuccessUrl_DifferentAppScheme_ReturnsFalse()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            Assert.IsFalse(Breeze.Instance.IsPaymentSuccessUrl("anothergame://breeze-payment/purchase/success"));
        }

        // ─── NotifyOnPaymentWebviewDismissed ────────────────────────────────

        [Test]
        public void NotifyOnPaymentWebviewDismissed_WhenNoInstance_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                Breeze.NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason.Dismissed, null));
        }

        [Test]
        public void OnPaymentWebviewDismissed_CanSubscribeAndReceivesEvent()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            bool called = false;
            Breeze.Instance.OnPaymentWebviewDismissed += (reason, data) => { called = true; };
            Breeze.NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason.PaymentSuccess, null);
            Assert.IsTrue(called);
        }

        [Test]
        public void NotifyOnPaymentWebviewDismissed_PassesCorrectReason()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            BrzPaymentWebviewDismissReason? received = null;
            Breeze.Instance.OnPaymentWebviewDismissed += (reason, data) => { received = reason; };
            Breeze.NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason.PaymentFailure, null);
            Assert.AreEqual(BrzPaymentWebviewDismissReason.PaymentFailure, received);
        }

        [Test]
        public void NotifyOnPaymentWebviewDismissed_PassesCorrectData()
        {
            Breeze.Initialize(new BreezeConfiguration { AppScheme = "mygame" });
            string receivedData = null;
            Breeze.Instance.OnPaymentWebviewDismissed += (reason, data) => { receivedData = data; };
            Breeze.NotifyOnPaymentWebviewDismissed(BrzPaymentWebviewDismissReason.PaymentSuccess, "{\"receipt\":\"abc\"}");
            Assert.AreEqual("{\"receipt\":\"abc\"}", receivedData);
        }

        // ─── BrzShowPaymentWebviewRequest ───────────────────────────────────

        [Test]
        public void WebviewRequest_SerializesAndDeserializes()
        {
            var request = new BrzShowPaymentWebviewRequest
            {
                DirectPaymentUrl = "https://pay.breeze.cash/checkout/order-xyz",
                Data = "{\"orderId\":\"xyz\"}"
            };
            string json = JsonConvert.SerializeObject(request);
            Assert.IsTrue(json.Contains("order-xyz"));
            var rt = JsonConvert.DeserializeObject<BrzShowPaymentWebviewRequest>(json);
            Assert.IsNotNull(rt);
            Assert.AreEqual("https://pay.breeze.cash/checkout/order-xyz", rt.DirectPaymentUrl);
            Assert.AreEqual("{\"orderId\":\"xyz\"}", rt.Data);
        }

        [Test]
        public void WebviewRequest_NullFields_Serializes()
        {
            var request = new BrzShowPaymentWebviewRequest();
            string json = JsonConvert.SerializeObject(request);
            var rt = JsonConvert.DeserializeObject<BrzShowPaymentWebviewRequest>(json);
            Assert.IsNotNull(rt);
            Assert.IsNull(rt.DirectPaymentUrl);
            Assert.IsNull(rt.Data);
        }

        [Test]
        public void WebviewRequest_DataField_PassedThrough()
        {
            string payload = "{\"order\":\"123\",\"amount\":500}";
            var request = new BrzShowPaymentWebviewRequest { Data = payload };
            string json = JsonConvert.SerializeObject(request);
            var rt = JsonConvert.DeserializeObject<BrzShowPaymentWebviewRequest>(json);
            Assert.AreEqual(payload, rt.Data);
        }

        // ─── BrzShowPaymentWebviewResultCode ────────────────────────────────

        [Test]
        public void WebviewResultCode_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)BrzShowPaymentWebviewResultCode.Success);
            Assert.AreEqual(1, (int)BrzShowPaymentWebviewResultCode.NullInput);
            Assert.AreEqual(2, (int)BrzShowPaymentWebviewResultCode.InvalidUtf8);
            Assert.AreEqual(3, (int)BrzShowPaymentWebviewResultCode.JsonDecodingFailed);
            Assert.AreEqual(4, (int)BrzShowPaymentWebviewResultCode.InvalidUrl);
        }

        [Test]
        public void WebviewResultCode_AllValues_Count()
        {
            Assert.AreEqual(5, Enum.GetValues(typeof(BrzShowPaymentWebviewResultCode)).Length);
        }

        // ─── BrzPaymentWebviewDismissReason ─────────────────────────────────

        [Test]
        public void WebviewDismissReason_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)BrzPaymentWebviewDismissReason.Dismissed);
            Assert.AreEqual(1, (int)BrzPaymentWebviewDismissReason.PaymentSuccess);
            Assert.AreEqual(2, (int)BrzPaymentWebviewDismissReason.PaymentFailure);
            Assert.AreEqual(3, (int)BrzPaymentWebviewDismissReason.LoadError);
        }

        [Test]
        public void WebviewDismissReason_AllValues_Count()
        {
            Assert.AreEqual(4, Enum.GetValues(typeof(BrzPaymentWebviewDismissReason)).Length);
        }

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
    }
}
