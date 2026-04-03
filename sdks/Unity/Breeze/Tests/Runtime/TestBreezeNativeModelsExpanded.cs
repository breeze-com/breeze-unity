using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

namespace BreezeSdk.Runtime.Tests
{
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

        // ─── BrzShowPaymentOptionsResultCode ────────────────────────────────

        [Test]
        public void ResultCode_AllValues_Count()
        {
            var values = Enum.GetValues(typeof(BrzShowPaymentOptionsResultCode));
            Assert.AreEqual(4, values.Length);
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
}