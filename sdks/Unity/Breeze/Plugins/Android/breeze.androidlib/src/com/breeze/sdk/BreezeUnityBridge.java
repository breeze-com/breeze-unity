package com.breeze.sdk;

import android.util.Log;

import com.unity3d.player.UnityPlayer;
import org.json.JSONObject;

public class BreezeUnityBridge {

    private static final String TAG = "BreezeUnityBridge";
    private static final String UNITY_GAME_OBJECT = "BreezePay";

    // Unity Message Methods
    public static final String MSG_ON_DIALOG_DISMISSED = "OnAndroidDialogDismissed";

    public static void sendMessage(String methodName, String message) {
        try {
            UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, methodName, message != null ? message : "");
        } catch (Exception e) {
            Log.e(TAG, "Failed to send Unity message: " + methodName + " Error: " + e.getMessage());
        }
    }

    public static void sendDialogDismissed(BreezePaymentDialogDismissReason reason, String data) {
        try {
            JSONObject payload = new JSONObject();
            payload.put("reason", reason.getValue());
            payload.put("data", data != null ? data : "");
            String jsonPayload = payload.toString();
            sendMessage(MSG_ON_DIALOG_DISMISSED, jsonPayload);
        } catch (Exception e) {
            Log.e(TAG, "Failed to create dismiss payload: " + e.getMessage());
        }
    }
}
