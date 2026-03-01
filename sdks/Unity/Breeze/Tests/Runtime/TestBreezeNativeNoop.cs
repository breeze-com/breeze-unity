using System;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// Tests for BreezeNativeNoop — the editor/no-op implementation of IBreezeNative.
/// Covers the fixed bug where DismissPaymentPageView threw NotImplementedException.
/// </summary>
public class TestBreezeNativeNoop
{
    private BreezeNativeNoop _noop;

    [SetUp]
    public void SetUp()
    {
        _noop = new BreezeNativeNoop();
    }

    // ─── DismissPaymentPageView (was throwing NotImplementedException) ───

    [Test]
    public void DismissPaymentPageView_DoesNotThrow()
    {
        // Previously threw NotImplementedException — fixed to be a no-op
        Assert.DoesNotThrow(() => _noop.DismissPaymentPageView());
    }

    [Test]
    public void DismissPaymentPageView_CalledTwice_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            _noop.DismissPaymentPageView();
            _noop.DismissPaymentPageView();
        });
    }

    // ─── ShowPaymentOptionsDialog ───────────────────────────────────────

    [Test]
    public void ShowPaymentOptionsDialog_ReturnsSuccess()
    {
        var request = new BrzShowPaymentOptionsDialogRequest
        {
            Title = "Test",
            DirectPaymentUrl = "https://pay.breeze.cash/test"
        };
        var result = _noop.ShowPaymentOptionsDialog(request, null);
        Assert.AreEqual(BrzShowPaymentOptionsResultCode.Success, result);
    }

    [Test]
    public void ShowPaymentOptionsDialog_NullRequest_ReturnsSuccess()
    {
        // Noop implementation serializes null → "null" JSON, doesn't crash
        var result = _noop.ShowPaymentOptionsDialog(null, null);
        Assert.AreEqual(BrzShowPaymentOptionsResultCode.Success, result);
    }

    [Test]
    public void ShowPaymentOptionsDialog_WithCallback_ReturnsSuccess()
    {
        bool called = false;
        BrzPaymentDialogDismissCallback cb = (reason, data) => { called = true; };
        var request = new BrzShowPaymentOptionsDialogRequest { Title = "T" };
        var result = _noop.ShowPaymentOptionsDialog(request, cb);
        Assert.AreEqual(BrzShowPaymentOptionsResultCode.Success, result);
        // Noop does NOT invoke the callback
        Assert.IsFalse(called);
    }

    // ─── GetDeviceUniqueId ──────────────────────────────────────────────

    [Test]
    public void GetDeviceUniqueId_ReturnsNonEmpty()
    {
        string id = _noop.GetDeviceUniqueId();
        Assert.IsFalse(string.IsNullOrEmpty(id));
    }

    [Test]
    public void GetDeviceUniqueId_ReturnsValidGuid()
    {
        string id = _noop.GetDeviceUniqueId();
        Assert.IsTrue(Guid.TryParse(id, out _), $"Expected GUID format, got: {id}");
    }

    [Test]
    public void GetDeviceUniqueId_ReturnsSameValueOnSecondCall()
    {
        // Should return the same ID (persisted in PlayerPrefs)
        string id1 = _noop.GetDeviceUniqueId();
        string id2 = _noop.GetDeviceUniqueId();
        Assert.AreEqual(id1, id2);
    }

    // ─── Implements IBreezeNative ───────────────────────────────────────

    [Test]
    public void ImplementsIBreezeNative()
    {
        Assert.IsInstanceOf<IBreezeNative>(_noop);
    }
}
