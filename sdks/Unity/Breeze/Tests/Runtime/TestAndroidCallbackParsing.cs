using System;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Tests for Android callback JSON parsing — specifically the known bug where
/// the Android bridge sent dismiss reason as a string ("CloseTapped") but
/// the C# DialogDismissedPayload expected an int.
///
/// The fix: DialogDismissedPayload.Reason uses int, matching the int values
/// sent by the Java bridge. The StringEnumConverter on BrzPaymentDialogDismissReason
/// handles string↔enum for the public API; the internal bridge uses int codes.
/// </summary>
public class TestAndroidCallbackParsing
{
    // Mirrors the internal DialogDismissedPayload from BreezeNativeAndroid
    [Serializable]
    private class DialogDismissedPayload
    {
        [JsonProperty("reason")]
        public int Reason;

        [JsonProperty("data")]
        public string Data;
    }

    // ─── Int-based reason (correct protocol) ────────────────────────────

    [Test]
    public void IntReason_CloseTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":0,\"data\":\"d\"}");
        Assert.AreEqual(0, payload.Reason);
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped, (BrzPaymentDialogDismissReason)payload.Reason);
    }

    [Test]
    public void IntReason_DirectPaymentTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":1,\"data\":null}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped, (BrzPaymentDialogDismissReason)payload.Reason);
    }

    [Test]
    public void IntReason_AppStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":2}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.AppStoreTapped, (BrzPaymentDialogDismissReason)payload.Reason);
    }

    [Test]
    public void IntReason_GoogleStoreTapped_ParsesCorrectly()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":3}");
        Assert.AreEqual(BrzPaymentDialogDismissReason.GoogleStoreTapped, (BrzPaymentDialogDismissReason)payload.Reason);
    }

    // ─── String-based reason (the bug scenario) ─────────────────────────

    [Test]
    public void StringReason_FailsOrDefaultsToZero()
    {
        // Android bridge previously sent "CloseTapped" as string.
        // With int field, Newtonsoft either throws or returns 0.
        string json = "{\"reason\":\"CloseTapped\",\"data\":\"test\"}";
        try
        {
            var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>(json);
            // If it doesn't throw, reason defaults to 0 — masking DirectPaymentTapped etc.
            Assert.AreEqual(0, payload.Reason, "String reason silently becomes 0");
        }
        catch (JsonException)
        {
            // This is the correct behavior — string can't be parsed as int
            Assert.Pass("Correctly rejects string reason for int field");
        }
    }

    [Test]
    public void StringReason_DirectPaymentTapped_WouldBeMisinterpreted()
    {
        // If Android sent "DirectPaymentTapped" but C# expects int,
        // the reason would be wrong (0 instead of 1)
        string json = "{\"reason\":\"DirectPaymentTapped\",\"data\":\"url\"}";
        try
        {
            var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>(json);
            // Bug: reason=0 (CloseTapped) instead of 1 (DirectPaymentTapped)
            Assert.AreNotEqual(1, payload.Reason, "String 'DirectPaymentTapped' does NOT parse to int 1");
        }
        catch (JsonException)
        {
            Assert.Pass("Correctly rejects string reason");
        }
    }

    // ─── Enum-based deserialization (what the public API uses) ──────────

    [Test]
    public void EnumReason_StringDeserialization_Works()
    {
        // BrzPaymentDialogDismissReason uses StringEnumConverter
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"CloseTapped\""));
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("\"DirectPaymentTapped\""));
    }

    [Test]
    public void EnumReason_IntDeserialization_Works()
    {
        // StringEnumConverter also handles int values
        Assert.AreEqual(BrzPaymentDialogDismissReason.CloseTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("0"));
        Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped,
            JsonConvert.DeserializeObject<BrzPaymentDialogDismissReason>("1"));
    }

    // ─── Null/missing reason ────────────────────────────────────────────

    [Test]
    public void NullReason_DefaultsToZero()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"data\":\"test\"}");
        Assert.AreEqual(0, payload.Reason); // int default
    }

    [Test]
    public void EmptyPayload_DefaultsToZero()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{}");
        Assert.AreEqual(0, payload.Reason);
        Assert.IsNull(payload.Data);
    }

    // ─── Unknown reason values ──────────────────────────────────────────

    [Test]
    public void UnknownIntReason_CastsWithoutError()
    {
        var payload = JsonConvert.DeserializeObject<DialogDismissedPayload>("{\"reason\":99}");
        Assert.AreEqual(99, payload.Reason);
        // Casting to enum gives undefined value — doesn't crash
        var reason = (BrzPaymentDialogDismissReason)payload.Reason;
        Assert.AreEqual(99, (int)reason);
    }
}
