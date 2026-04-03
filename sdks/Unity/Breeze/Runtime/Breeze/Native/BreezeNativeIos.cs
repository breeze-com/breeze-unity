#if UNITY_IOS

using System;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace BreezeSdk.Runtime
{
    public class BreezeNativeIos : IBreezeNative
    {
        private const string LIB_NAME = "__Internal";

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 brz_get_device_id([Out] byte[] buffer, UInt32 bufferLength);

        public string GetDeviceUniqueId()
        {
            UInt32 requiredLength = brz_get_device_id(null, 0);
            byte[] buffer = new byte[requiredLength];
            brz_get_device_id(buffer, (UInt32)buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern BrzShowPaymentOptionsResultCode brz_show_payment_options_dialog(
            string requestJson,
            BrzPaymentDialogDismissCallback onDismiss
        );

        public BrzShowPaymentOptionsResultCode ShowPaymentOptionsDialog(
            BrzShowPaymentOptionsDialogRequest request,
            BrzPaymentDialogDismissCallback onDismiss
        )
        {
            string requestJson = JsonConvert.SerializeObject(request);
    #if BREEZE_DEBUG
            UnityEngine.Debug.Log($"brz_show_payment_options_dialog: request = {requestJson}");
    #endif
            return brz_show_payment_options_dialog(requestJson, onDismiss);
        }

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void brz_dismiss_payment_page_view();

        public void DismissPaymentPageView()
        {
    #if BREEZE_DEBUG
            UnityEngine.Debug.Log($"brz_dismiss_payment_page_view called");
    #endif
            brz_dismiss_payment_page_view();
        }

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern BrzShowPaymentWebviewResultCode brz_show_payment_webview(
            string requestJson,
            BrzPaymentWebviewDismissCallback onDismiss
        );

        public BrzShowPaymentWebviewResultCode ShowPaymentWebview(
            BrzShowPaymentWebviewRequest request,
            BrzPaymentWebviewDismissCallback onDismiss
        )
        {
            string requestJson = JsonConvert.SerializeObject(request);
    #if BREEZE_DEBUG
            UnityEngine.Debug.Log($"brz_show_payment_webview: request = {requestJson}");
    #endif
            return brz_show_payment_webview(requestJson, onDismiss);
        }
    }
}
#endif
