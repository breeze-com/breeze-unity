using System;
using Newtonsoft.Json;
using UnityEngine;

public class BreezeNativeNoop : IBreezeNative
{
    private const string DEVICE_UNIQUE_ID_KEY = "BreezeDeviceUniqueId";

    public string GetDeviceUniqueId()
    {
        // Check if UUID already exists in PlayerPrefs
        string existingId = PlayerPrefs.GetString(DEVICE_UNIQUE_ID_KEY, string.Empty);

        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        // Generate a new UUID
        string newId = Guid.NewGuid().ToString();

        // Save to PlayerPrefs
        PlayerPrefs.SetString(DEVICE_UNIQUE_ID_KEY, newId);
        PlayerPrefs.Save();

        return newId;
    }

    public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
        BrzShowPaymentOptionsDialogRequest request,
        BrzPaymentDialogDismissCallback onDismiss
    )
    {
        string requestJson = JsonConvert.SerializeObject(request);
        Debug.Log($"ShowPaymentOptionsDialog: Not implemented, request: {requestJson}");
        return BrzShowPaymentOptionsResultCode.Success;
    }

    public void DismissPaymentPageView()
    {
        throw new NotImplementedException();
    }
}
