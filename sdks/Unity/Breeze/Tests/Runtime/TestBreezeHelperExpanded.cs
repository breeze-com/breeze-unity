using System;
using System.Collections.Specialized;
using NUnit.Framework;

namespace BreezeSdk.Runtime.Tests
{
    /// <summary>
    /// Expanded tests for BreezeHelper, BreezeBase64Helper, and BreezeDateTimeExtensions.
    /// Does NOT duplicate tests from TestBreezeHelper.cs.
    /// </summary>
    public class TestBreezeHelperExpanded
    {
        // ─── BuildRequestUrl ────────────────────────────────────────────────

        [Test]
        public void BuildRequestUrl_BasicUrl()
        {
            var qp = new NameValueCollection { { "a", "1" } };
            string result = BreezeHelper.BuildRequestUrl("https://example.com", "/api/test", qp);
            Assert.IsTrue(result.StartsWith("https://example.com/api/test"));
            Assert.IsTrue(result.Contains("a=1"));
        }

        [Test]
        public void BuildRequestUrl_NoQueryParams()
        {
            string result = BreezeHelper.BuildRequestUrl("https://example.com", "/path", new NameValueCollection());
            Assert.AreEqual("https://example.com/path", result);
        }

        [Test]
        public void BuildRequestUrl_MultipleParams()
        {
            var qp = new NameValueCollection { { "x", "1" }, { "y", "2" }, { "z", "3" } };
            string result = BreezeHelper.BuildRequestUrl("https://example.com", "/", qp);
            Assert.IsTrue(result.Contains("x=1"));
            Assert.IsTrue(result.Contains("y=2"));
            Assert.IsTrue(result.Contains("z=3"));
        }

        // ─── UpdateUrlQueryParams edge cases ────────────────────────────────

        [Test]
        public void UpdateUrlQueryParams_UrlWithPort()
        {
            var extra = new NameValueCollection { { "key", "val" } };
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com:9090/path", extra);
            Assert.IsTrue(result.Contains(":9090"));
            Assert.IsTrue(result.Contains("key=val"));
        }

        [Test]
        public void UpdateUrlQueryParams_UrlWithFragment()
        {
            var extra = new NameValueCollection { { "key", "val" } };
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com/path#section", extra);
            Assert.IsTrue(result.Contains("key=val"));
        }

        [Test]
        public void UpdateUrlQueryParams_VeryLongQueryString()
        {
            var extra = new NameValueCollection();
            for (int i = 0; i < 100; i++)
            {
                extra.Add($"key{i}", $"value{i}");
            }
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com/path", extra);
            Assert.IsTrue(result.Contains("key0=value0"));
            Assert.IsTrue(result.Contains("key99=value99"));
        }

        [Test]
        public void UpdateUrlQueryParams_SpecialCharsInKey()
        {
            var extra = new NameValueCollection { { "key=special&chars", "value" } };
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com/path", extra);
            Assert.IsNotNull(result);
            // Key should be encoded
            Assert.IsFalse(result.Contains("key=special&chars=value"));
        }

        [Test]
        public void UpdateUrlQueryParams_SpecialCharsInValue()
        {
            var extra = new NameValueCollection { { "key", "val=ue&more" } };
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com/path", extra);
            Assert.IsNotNull(result);
        }

        [Test]
        public void UpdateUrlQueryParams_NullValueInParam()
        {
            var extra = new NameValueCollection { { "key", null } };
            string result = BreezeHelper.UpdateUrlQueryParams("https://example.com/path", extra);
            Assert.IsNotNull(result);
        }

        [Test]
        public void UpdateUrlQueryParams_HttpUrl()
        {
            var extra = new NameValueCollection { { "k", "v" } };
            string result = BreezeHelper.UpdateUrlQueryParams("http://example.com/path", extra);
            Assert.IsTrue(result.StartsWith("http://"));
        }

        [Test]
        public void UpdateUrlQueryParams_ReplaceMultipleParams()
        {
            string url = "https://example.com?a=1&b=2&c=3";
            var extra = new NameValueCollection { { "a", "10" }, { "c", "30" } };
            string result = BreezeHelper.UpdateUrlQueryParams(url, extra);
            Assert.IsTrue(result.Contains("a=10"));
            Assert.IsTrue(result.Contains("b=2"));
            Assert.IsTrue(result.Contains("c=30"));
        }

        // ─── BuildQueryString ───────────────────────────────────────────────

        [Test]
        public void BuildQueryString_NullCollection_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, BreezeHelper.BuildQueryString(null));
        }

        [Test]
        public void BuildQueryString_EmptyCollection_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, BreezeHelper.BuildQueryString(new NameValueCollection()));
        }

        [Test]
        public void BuildQueryString_SingleParam()
        {
            var nvc = new NameValueCollection { { "key", "value" } };
            string result = BreezeHelper.BuildQueryString(nvc);
            Assert.AreEqual("key=value", result);
        }

        // ─── GetCurrentTwoLetterIsoRegionName ───────────────────────────────

        [Test]
        public void GetRegionName_EnUS_ReturnsUS()
        {
            var culture = new System.Globalization.CultureInfo("en-US");
            Assert.AreEqual("US", BreezeHelper.GetCurrentTwoLetterIsoRegionName(culture));
        }

        [Test]
        public void GetRegionName_JaJP_ReturnsJP()
        {
            var culture = new System.Globalization.CultureInfo("ja-JP");
            Assert.AreEqual("JP", BreezeHelper.GetCurrentTwoLetterIsoRegionName(culture));
        }

        [Test]
        public void GetRegionName_InvalidCulture_FallsBackToUS()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            Assert.AreEqual("US", BreezeHelper.GetCurrentTwoLetterIsoRegionName(culture));
        }

        // ─── BreezeBase64Helper ─────────────────────────────────────────────

        [Test]
        public void Base64UrlToBase64_ReplacesChars()
        {
            string b64url = "SGVsbG8-V29ybGQ_";
            string b64 = BreezeBase64Helper.ConvertBase64UrlToBase64(b64url);
            Assert.IsTrue(b64.Contains('+'));
            Assert.IsTrue(b64.Contains('/'));
        }

        [Test]
        public void Base64ToBase64Url_ReplacesChars()
        {
            string b64 = "SGVsbG8+V29ybGQ/";
            string b64url = BreezeBase64Helper.ConvertBase64ToBase64Url(b64);
            Assert.IsFalse(b64url.Contains('+'));
            Assert.IsFalse(b64url.Contains('/'));
            Assert.IsFalse(b64url.Contains('='));
        }

        [Test]
        public void Base64Url_Roundtrip()
        {
            string original = "SGVsbG8gV29ybGQ="; // "Hello World"
            string b64url = BreezeBase64Helper.ConvertBase64ToBase64Url(original);
            string back = BreezeBase64Helper.ConvertBase64UrlToBase64(b64url);
            Assert.AreEqual(original, back);
        }

        [Test]
        public void DecodeBase64ToString_HelloWorld()
        {
            string result = BreezeBase64Helper.DecodeBase64ToString("SGVsbG8gV29ybGQ=");
            Assert.AreEqual("Hello World", result);
        }

        [Test]
        public void DecodeBase64UrlToString_Works()
        {
            string b64url = BreezeBase64Helper.ConvertBase64ToBase64Url("SGVsbG8gV29ybGQ=");
            string result = BreezeBase64Helper.DecodeBase64UrlToString(b64url);
            Assert.AreEqual("Hello World", result);
        }

        [Test]
        public void DecodeBase64ToBytes_ReturnsCorrectLength()
        {
            byte[] bytes = BreezeBase64Helper.DecodeBase64ToBytes("AQID"); // [1,2,3]
            Assert.AreEqual(3, bytes.Length);
            Assert.AreEqual(1, bytes[0]);
            Assert.AreEqual(2, bytes[1]);
            Assert.AreEqual(3, bytes[2]);
        }

        [Test]
        public void ConvertBase64UrlToBase64_PaddingMod2()
        {
            // Length % 4 == 2 → should add ==
            string result = BreezeBase64Helper.ConvertBase64UrlToBase64("QQ");
            Assert.IsTrue(result.EndsWith("=="));
        }

        [Test]
        public void ConvertBase64UrlToBase64_PaddingMod3()
        {
            // Length % 4 == 3 → should add =
            string result = BreezeBase64Helper.ConvertBase64UrlToBase64("QUI");
            Assert.IsTrue(result.EndsWith("="));
        }

        // ─── BreezeDateTimeExtensions ───────────────────────────────────────

        [Test]
        public void ToUnixTimeMilliseconds_Epoch_ReturnsZero()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, epoch.ToUnixTimeMilliseconds());
        }

        [Test]
        public void ToUnixTimeSeconds_Epoch_ReturnsZero()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, epoch.ToUnixTimeSeconds());
        }

        [Test]
        public void ToDateTimeFromUnixTimeMilliseconds_Zero_ReturnsEpoch()
        {
            var result = 0L.ToDateTimeFromUnixTimeMilliseconds();
            Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        }

        [Test]
        public void ToDateTimeFromUnixTimeSeconds_Roundtrip()
        {
            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            long unix = now.ToUnixTimeSeconds();
            var back = unix.ToDateTimeFromUnixTimeSeconds();
            Assert.AreEqual(now, back);
        }

        // ─── BreezeConstants ────────────────────────────────────────────────

        [Test]
        public void SdkVersion_IsNotNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(BreezeConstants.SDK_VERSION));
        }

        [Test]
        public void SdkVersion_HasExpectedFormat()
        {
            // Should be semver-like
            var parts = BreezeConstants.SDK_VERSION.Split('.');
            Assert.GreaterOrEqual(parts.Length, 2);
        }
    }
}