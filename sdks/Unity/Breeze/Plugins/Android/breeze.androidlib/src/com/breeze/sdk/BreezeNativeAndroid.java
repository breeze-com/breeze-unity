package com.breeze.sdk;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

public class BreezeNativeAndroid {

    private static final String TAG = "BreezeNativeAndroid";

    // Result codes matching C# BrzShowPaymentOptionsResultCode
    public static final int RESULT_SUCCESS = 0;
    public static final int RESULT_NULL_INPUT = 1;
    public static final int RESULT_INVALID_UTF8 = 2;
    public static final int RESULT_JSON_DECODING_FAILED = 3;

    private static final String kClassNameCustomTabsIntent = "androidx.browser.customtabs.CustomTabsIntent";
    private static final String kClassNameCustomTabsIntent_Builder = "androidx.browser.customtabs.CustomTabsIntent$Builder";

    private static BreezeNativeAndroid instance = null;
    private BreezePaymentOptionsDialog currentDialog = null;
    private BreezeWebView currentWebView = null;

    public static BreezeNativeAndroid getInstance() {
        if (instance == null) {
            instance = new BreezeNativeAndroid();
        }
        return instance;
    }

    private BreezeNativeAndroid() {
        Log.i(TAG, "BreezeNativeAndroid instance created");
    }

    public String getDeviceUniqueId() {
        try {
            Activity activity = UnityPlayer.currentActivity;
            if (activity != null) {
                return android.provider.Settings.Secure.getString(
                    activity.getContentResolver(),
                    android.provider.Settings.Secure.ANDROID_ID
                );
            }
        } catch (Exception e) {
            Log.w(TAG, "Failed to get device ID: " + e.getMessage());
        }
        return "";
    }

    /**
     * @param requestJson JSON string of BrzShowPaymentOptionsDialogRequest
     * @return int value of enum BrzShowPaymentOptionsResultCode
     */
    public int showPaymentOptionsDialog(String requestJson) {
        if (requestJson == null || requestJson.isEmpty()) {
            return RESULT_NULL_INPUT;
        }

        try {
            JSONObject json = new JSONObject(requestJson);
            String title = json.optString("title", "Select payment method");
            String directPaymentUrl = json.optString("directPaymentUrl", null);
            String data = json.optString("data", null);
            String themeStr = json.optString("theme", "auto");

            JSONObject productJson = json.optJSONObject("product");
            String displayName = "";
            String originalPrice = "";
            String breezePrice = "";
            String decoration = null;
            String productIconUrl = null;

            if (productJson != null) {
                displayName = productJson.optString("displayName", "");
                originalPrice = productJson.optString("originalPrice", "");
                breezePrice = productJson.optString("breezePrice", "");
                decoration = productJson.optString("decoration", null);
                productIconUrl = productJson.optString("productIconUrl", null);
            }

            // Validate directPaymentUrl host ends with .breeze.cash
            if (directPaymentUrl == null || directPaymentUrl.isEmpty()) {
                Log.w(TAG, "Direct payment URL is missing");
                sendDismiss(BreezePaymentDialogDismissReason.CloseTapped, data);
                return RESULT_SUCCESS;
            }

            try {
                Uri uri = Uri.parse(directPaymentUrl);
                String host = uri.getHost();
                if (host == null) {
                    Log.w(TAG, "Direct payment URL has no host");
                    sendDismiss(BreezePaymentDialogDismissReason.CloseTapped, data);
                    return RESULT_SUCCESS;
                }
                String lower = host.toLowerCase();
                boolean allowed = false;
                for (String suffix : BreezeConstants.ALLOWED_HOSTS) {
                    if (lower.endsWith(suffix)) {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed) {
                    Log.w(TAG, "Direct payment URL host not allowed: " + host);
                    sendDismiss(BreezePaymentDialogDismissReason.CloseTapped, data);
                    return RESULT_SUCCESS;
                }
            } catch (Exception e) {
                Log.w(TAG, "Invalid direct payment URL: " + e.getMessage());
                sendDismiss(BreezePaymentDialogDismissReason.CloseTapped, data);
                return RESULT_SUCCESS;
            }

            // Show dialog on UI thread
            final String fTitle = title;
            final String fDirectPaymentUrl = directPaymentUrl;
            final String fData = data;
            final String fThemeStr = themeStr;
            final String fDisplayName = displayName;
            final String fOriginalPrice = originalPrice;
            final String fBreezePrice = breezePrice;
            final String fDecoration = decoration;
            final String fProductIconUrl = productIconUrl;

            Activity activity = UnityPlayer.currentActivity;
            activity.runOnUiThread(() -> {
                currentDialog = new BreezePaymentOptionsDialog(
                        activity,
                        fTitle,
                        fDisplayName,
                        fOriginalPrice,
                        fBreezePrice,
                        fDecoration,
                        fProductIconUrl,
                        fDirectPaymentUrl,
                        fData,
                        fThemeStr,
                        (reason) -> {
                            currentDialog = null;
                            if (reason == BreezePaymentDialogDismissReason.DirectPaymentTapped) {
                                openWithBrowser(fDirectPaymentUrl, activity);
                            }
                            sendDismiss(reason, fData);
                        }
                );
                currentDialog.show();
            });

            return RESULT_SUCCESS;
        } catch (Exception e) {
            Log.e(TAG, "Failed to parse request JSON: " + e.getMessage());
            return RESULT_JSON_DECODING_FAILED;
        }
    }

    public void dismissPaymentPageView() {
        Log.d(TAG, "dismissPaymentPageView called (no-op on Android)");
    }

    /**
     * @param requestJson JSON string of BrzShowPaymentWebviewRequest
     * @return int value of enum BrzShowPaymentWebviewResultCode
     */
    public int showPaymentWebview(String requestJson) {
        if (requestJson == null || requestJson.isEmpty()) {
            return 1; // NullInput
        }

        try {
            JSONObject json = new JSONObject(requestJson);
            String directPaymentUrl = json.optString("directPaymentUrl", null);
            String data = json.optString("data", null);

            if (directPaymentUrl == null || directPaymentUrl.isEmpty()) {
                Log.w(TAG, "directPaymentUrl is missing");
                return 4; // InvalidUrl
            }

            // Validate URL host
            try {
                Uri uri = Uri.parse(directPaymentUrl);
                String host = uri.getHost();
                if (host == null) {
                    Log.w(TAG, "directPaymentUrl has no host");
                    return 4; // InvalidUrl
                }
                String lower = host.toLowerCase();
                boolean allowed = false;
                for (String suffix : BreezeConstants.ALLOWED_HOSTS) {
                    if (lower.endsWith(suffix)) {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed) {
                    Log.w(TAG, "directPaymentUrl host not allowed: " + host);
                    return 4; // InvalidUrl
                }
            } catch (Exception e) {
                Log.w(TAG, "Invalid directPaymentUrl: " + e.getMessage());
                return 4; // InvalidUrl
            }

            Activity activity = UnityPlayer.currentActivity;
            final String fUrl = directPaymentUrl;
            final String fData = data;

            activity.runOnUiThread(() -> {
                if (currentWebView != null) {
                    currentWebView.dismiss();
                }
                currentWebView = new BreezeWebView(activity, fUrl, fData, (reason, cbData) -> {
                    currentWebView = null;
                    BreezeUnityBridge.sendWebViewDismissed(reason, cbData);
                });
                currentWebView.show();
            });

            return 0; // Success
        } catch (Exception e) {
            Log.e(TAG, "Failed to parse webview request JSON: " + e.getMessage());
            return 3; // JsonDecodingFailed
        }
    }

    public void dismissPaymentWebview() {
        Log.d(TAG, "dismissPaymentWebview called");
        if (currentWebView != null) {
            currentWebView.dismiss();
        }
    }

    private void sendDismiss(BreezePaymentDialogDismissReason reason, String data) {
        BreezeUnityBridge.sendDialogDismissed(reason, data);
    }

    private void openWithBrowser(String url, Activity activity) {
        try {
            if (isChromeCustomTabsAvailable()) {
                Log.d(TAG, "Opening URL with Chrome Custom Tabs");
                openWithReflectionChromeCustomTabs(url, activity);
            } else {
                Log.w(TAG, "Chrome Custom Tabs not available. Falling back to default browser.");
                openWithDefaultBrowser(url, activity);
            }
        } catch (Exception e) {
            Log.e(TAG, "Failed to open browser: " + e.getMessage());
            try {
                openWithDefaultBrowser(url, activity);
            } catch (Exception fallbackException) {
                Log.e(TAG, "Failed to open default browser: " + fallbackException.getMessage());
            }
        }
    }

    private boolean isChromeCustomTabsAvailable() {
        try {
            Class.forName(kClassNameCustomTabsIntent);
            return true;
        } catch (ClassNotFoundException e) {
            return false;
        }
    }

    private void openWithReflectionChromeCustomTabs(String url, Activity activity) throws Exception {
        Class<?> builderClass = Class.forName(kClassNameCustomTabsIntent_Builder);
        Object builder = builderClass.newInstance();

        java.lang.reflect.Method setShowTitle = builderClass.getMethod("setShowTitle", boolean.class);
        setShowTitle.invoke(builder, true);

        java.lang.reflect.Method build = builderClass.getMethod("build");
        Object customTabsIntent = build.invoke(builder);

        Class<?> customTabsIntentClass = Class.forName(kClassNameCustomTabsIntent);
        java.lang.reflect.Method launchUrl = customTabsIntentClass.getMethod("launchUrl",
                android.content.Context.class, Uri.class);
        launchUrl.invoke(customTabsIntent, activity, Uri.parse(url));
    }

    private void openWithDefaultBrowser(String url, Activity activity) {
        Intent browserIntent = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
        browserIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        activity.startActivity(browserIntent);
    }
}
