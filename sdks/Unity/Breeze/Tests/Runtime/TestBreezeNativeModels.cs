using NUnit.Framework;
using Newtonsoft.Json;

namespace BreezeSdk.Runtime.Tests
{
    public class TestBreezeNativeModels
    {
        [Test]
        public void BrzShowPaymentOptionsDialogRequest_SerializesAndDeserializes()
        {
            var product = new BrzProductDisplayInfo
            {
                DisplayName = "Test Product",
                OriginalPrice = "$9.99",
                BreezePrice = "$7.99",
                Decoration = "20% off",
                ProductIconUrl = "https://example.com/icon.png"
            };
            var request = new BrzShowPaymentOptionsDialogRequest
            {
                Title = "Choose payment",
                ProductDisplayInfo = product,
                DirectPaymentUrl = "https://pay.example.com",
                Data = "{\"id\":1}",
                Theme = BrzPaymentOptionsTheme.Dark
            };

            string json = JsonConvert.SerializeObject(request);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Choose payment"));
            Assert.IsTrue(json.Contains("Test Product"));
            Assert.IsTrue(json.Contains("dark"));

            var roundTrip = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
            Assert.IsNotNull(roundTrip);
            Assert.AreEqual("Choose payment", roundTrip.Title);
            Assert.AreEqual("https://pay.example.com", roundTrip.DirectPaymentUrl);
            Assert.AreEqual("{\"id\":1}", roundTrip.Data);
            Assert.AreEqual(BrzPaymentOptionsTheme.Dark, roundTrip.Theme);
            Assert.IsNotNull(roundTrip.ProductDisplayInfo);
            Assert.AreEqual("Test Product", roundTrip.ProductDisplayInfo.DisplayName);
            Assert.AreEqual("$9.99", roundTrip.ProductDisplayInfo.OriginalPrice);
            Assert.AreEqual("$7.99", roundTrip.ProductDisplayInfo.BreezePrice);
            Assert.AreEqual("20% off", roundTrip.ProductDisplayInfo.Decoration);
            Assert.AreEqual("https://example.com/icon.png", roundTrip.ProductDisplayInfo.ProductIconUrl);
        }

        [Test]
        public void BrzShowPaymentOptionsDialogRequest_ThemeNull_OmittedFromJson()
        {
            var request = new BrzShowPaymentOptionsDialogRequest
            {
                Title = "Pay",
                Theme = null
            };
            string json = JsonConvert.SerializeObject(request);
            Assert.IsFalse(json.Contains("\"theme\""));
        }

        [Test]
        public void BrzPaymentOptionsTheme_SerializesToExpectedStrings()
        {
            Assert.AreEqual("\"light\"", JsonConvert.SerializeObject(BrzPaymentOptionsTheme.Light));
            Assert.AreEqual("\"dark\"", JsonConvert.SerializeObject(BrzPaymentOptionsTheme.Dark));
        }

        [Test]
        public void BrzPaymentOptionsTheme_DeserializesFromStrings()
        {
            Assert.AreEqual(BrzPaymentOptionsTheme.Light, JsonConvert.DeserializeObject<BrzPaymentOptionsTheme>("\"light\""));
            Assert.AreEqual(BrzPaymentOptionsTheme.Dark, JsonConvert.DeserializeObject<BrzPaymentOptionsTheme>("\"dark\""));
        }

        [Test]
        public void BrzProductDisplayInfo_SerializesAndDeserializes()
        {
            var product = new BrzProductDisplayInfo
            {
                DisplayName = "Gem Pack",
                OriginalPrice = "1.99",
                BreezePrice = "0.99",
                Decoration = "50% off",
                ProductIconUrl = "https://cdn.example.com/gem.png"
            };
            string json = JsonConvert.SerializeObject(product);
            var roundTrip = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
            Assert.IsNotNull(roundTrip);
            Assert.AreEqual("Gem Pack", roundTrip.DisplayName);
            Assert.AreEqual("1.99", roundTrip.OriginalPrice);
            Assert.AreEqual("0.99", roundTrip.BreezePrice);
            Assert.AreEqual("50% off", roundTrip.Decoration);
            Assert.AreEqual("https://cdn.example.com/gem.png", roundTrip.ProductIconUrl);
        }

        [Test]
        public void BrzProductDisplayInfo_HandlesNullOptionalFields()
        {
            string json = "{\"displayName\":\"Only Name\"}";
            var product = JsonConvert.DeserializeObject<BrzProductDisplayInfo>(json);
            Assert.IsNotNull(product);
            Assert.AreEqual("Only Name", product.DisplayName);
            Assert.IsNull(product.OriginalPrice);
            Assert.IsNull(product.BreezePrice);
            Assert.IsNull(product.Decoration);
            Assert.IsNull(product.ProductIconUrl);
        }

        [Test]
        public void BrzPaymentDialogDismissReason_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)BrzPaymentDialogDismissReason.CloseTapped);
            Assert.AreEqual(1, (int)BrzPaymentDialogDismissReason.DirectPaymentTapped);
            Assert.AreEqual(2, (int)BrzPaymentDialogDismissReason.AppStoreTapped);
        }

        [Test]
        public void BrzShowPaymentOptionsResultCode_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)BrzShowPaymentOptionsResultCode.Success);
            Assert.AreEqual(1, (int)BrzShowPaymentOptionsResultCode.NullInput);
            Assert.AreEqual(2, (int)BrzShowPaymentOptionsResultCode.InvalidUtf8);
            Assert.AreEqual(3, (int)BrzShowPaymentOptionsResultCode.JsonDecodingFailed);
        }

        [Test]
        public void BrzShowPaymentOptionsDialogRequest_DeserializesFromMinimalJson()
        {
            string json = "{\"title\":\"Pay\",\"directPaymentUrl\":\"https://example.com\",\"data\":\"\"}";
            var request = JsonConvert.DeserializeObject<BrzShowPaymentOptionsDialogRequest>(json);
            Assert.IsNotNull(request);
            Assert.AreEqual("Pay", request.Title);
            Assert.AreEqual("https://example.com", request.DirectPaymentUrl);
            Assert.AreEqual("", request.Data);
            Assert.IsNull(request.ProductDisplayInfo);
            Assert.IsNull(request.Theme);
        }
    }
}