using System;
using Newtonsoft.Json;
using UnityEngine;

namespace BreezeSdk.Runtime
{
    /// <summary>
    /// No-op implementation of <see cref="IBreezeNative"/> used in the Unity Editor and unsupported platforms.
    /// </summary>
    public class BreezeNativeNoop : IBreezeNative
    {
        private const string DEVICE_UNIQUE_ID_KEY = "BreezeDeviceUniqueId";

        /// <inheritdoc />
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

        /// <inheritdoc />
        public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
            BrzShowPaymentOptionsDialogRequest request,
            BrzPaymentDialogDismissCallback onDismiss
        )
        {
            string requestJson = JsonConvert.SerializeObject(request);
#if BREEZE_DEBUG
        Debug.Log($"ShowPaymentOptionsDialog: Not implemented, request: {requestJson}");
#endif
            return BrzShowPaymentOptionsResultCode.Success;
        }

        /// <inheritdoc />
        public void DismissPaymentPageView()
        {
#if BREEZE_DEBUG
        Debug.Log("BreezeNativeNoop: DismissPaymentPageView (no-op in editor)");
#endif
        }

        /// <inheritdoc />
        public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
            BrzShowPaymentWebviewRequest request,
            BrzPaymentWebviewDismissCallback onDismiss
        )
        {
            string requestJson = JsonConvert.SerializeObject(request);
#if BREEZE_DEBUG
        Debug.Log($"ShowPaymentWebview: Not implemented, request: {requestJson}");
#endif
            return BrzShowPaymentWebviewResultCode.Success;
        }
    }
}