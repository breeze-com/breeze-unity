using Newtonsoft.Json;
using UnityEngine;

public class BreezeBridgeMessenger : MonoBehaviour
{
    public void OnAndroidDialogDismissed(string jsonPaylod)
    {
        Debug.Log($"received OnAndroidDialogDismissed: {jsonPaylod}");
        AndroidDialogDismissedPayload payload = JsonConvert.DeserializeObject<AndroidDialogDismissedPayload>(jsonPaylod);
        if (payload != null)
        {
            Breeze.NotifyOnPaymentOptionsDialogDismissed(payload.Reason, payload.Data);
        }
    }
}

public class AndroidDialogDismissedPayload
{
    [JsonProperty("reason")]
    public BrzPaymentDialogDismissReason Reason { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }
}
