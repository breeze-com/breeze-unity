package com.breeze.sdk;

import android.animation.Animator;
import android.animation.AnimatorListenerAdapter;
import android.animation.ValueAnimator;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.util.Log;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.DecelerateInterpolator;
import android.webkit.JavascriptInterface;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.Map;

/**
 * Bottom-sheet style webview dialog with draggable resize handle, rounded corners,
 * and close button. Mirrors the iOS BreezeWebView implementation.
 */
public class BreezeWebView {

    private static final String TAG = "BreezeWebView";

    // Layout constants
    private static final float CORNER_RADIUS_DP = 16f;
    private static final int HANDLE_BAR_HEIGHT_DP = 48;
    private static final float DEFAULT_HEIGHT_RATIO = 0.90f;
    private static final float MIN_HEIGHT_RATIO = 0.35f;
    private static final float MAX_HEIGHT_RATIO = 0.95f;
    private static final int ANIMATION_DURATION_MS = 300;
    private static final int DISMISS_ANIMATION_DURATION_MS = 200;
    private static final float DISMISS_VELOCITY_THRESHOLD = 1500f;

    public interface OnDismissListener {
        void onDismiss(BreezeWebViewDismissReason reason, String data);
    }

    private final Activity activity;
    private final String url;
    private final String data;
    private final OnDismissListener onDismissListener;

    private FrameLayout rootOverlay;
    private View dimBackground;
    private FrameLayout containerView;
    private WebView webView;

    private boolean didInvokeDismiss = false;
    private int containerHeight;
    private float dragStartY;
    private int dragStartHeight;

    public BreezeWebView(Activity activity, String url, String data, OnDismissListener onDismissListener) {
        this.activity = activity;
        this.url = url;
        this.data = data;
        this.onDismissListener = onDismissListener;
    }

    // ---- Public API ----

    public void show() {
        activity.runOnUiThread(this::buildAndShow);
    }

    public void dismiss() {
        activity.runOnUiThread(() -> animateDismissal(BreezeWebViewDismissReason.Dismissed));
    }

    // ---- Build UI ----

    @SuppressLint({"ClickableViewAccessibility", "SetJavaScriptEnabled"})
    private void buildAndShow() {
        int screenHeight = activity.getResources().getDisplayMetrics().heightPixels;
        float density = activity.getResources().getDisplayMetrics().density;
        containerHeight = (int) (screenHeight * DEFAULT_HEIGHT_RATIO);
        int handleBarHeightPx = (int) (HANDLE_BAR_HEIGHT_DP * density);
        int cornerRadiusPx = (int) (CORNER_RADIUS_DP * density);

        // Root overlay that covers the full screen
        rootOverlay = new FrameLayout(activity);
        rootOverlay.setLayoutParams(new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT));

        // Dim background
        dimBackground = new View(activity);
        dimBackground.setBackgroundColor(Color.argb(102, 0, 0, 0)); // 0.4 alpha
        dimBackground.setLayoutParams(new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT));
        dimBackground.setAlpha(0f);
        dimBackground.setOnClickListener(v -> animateDismissal(BreezeWebViewDismissReason.Dismissed));
        rootOverlay.addView(dimBackground);

        // Container (bottom sheet)
        containerView = new FrameLayout(activity);
        FrameLayout.LayoutParams containerParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, containerHeight);
        containerParams.gravity = Gravity.BOTTOM;
        containerView.setLayoutParams(containerParams);

        GradientDrawable containerBg = new GradientDrawable();
        containerBg.setColor(resolveBackgroundColor());
        containerBg.setCornerRadii(new float[]{
                cornerRadiusPx, cornerRadiusPx,  // top-left
                cornerRadiusPx, cornerRadiusPx,  // top-right
                0, 0, 0, 0                        // bottom corners
        });
        containerView.setBackground(containerBg);
        containerView.setClipToOutline(true);
        containerView.setElevation(16 * density);

        // Start off-screen for animation
        containerView.setTranslationY(screenHeight);
        rootOverlay.addView(containerView);

        // Handle bar (FrameLayout so pill is absolutely centered)
        FrameLayout handleBar = new FrameLayout(activity);
        handleBar.setBackgroundColor(resolveHandleBarColor());
        FrameLayout.LayoutParams handleParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, handleBarHeightPx);
        handleBar.setLayoutParams(handleParams);

        // Drag indicator pill — centered in handle bar
        View pill = new View(activity);
        GradientDrawable pillBg = new GradientDrawable();
        pillBg.setColor(resolvePillColor());
        pillBg.setCornerRadius(2.5f * density);
        pill.setBackground(pillBg);
        FrameLayout.LayoutParams pillParams = new FrameLayout.LayoutParams(
                (int) (36 * density), (int) (5 * density));
        pillParams.gravity = Gravity.CENTER;
        pill.setLayoutParams(pillParams);
        handleBar.addView(pill);

        // Close button (xmark) — right-aligned
        ImageView closeButton = new ImageView(activity);
        closeButton.setImageResource(android.R.drawable.ic_menu_close_clear_cancel);
        closeButton.setColorFilter(resolveSecondaryTextColor());
        closeButton.setPadding(
                (int) (8 * density), (int) (8 * density),
                (int) (8 * density), (int) (8 * density));
        FrameLayout.LayoutParams closeParams = new FrameLayout.LayoutParams(
                (int) (40 * density), (int) (40 * density));
        closeParams.gravity = Gravity.CENTER_VERTICAL | Gravity.END;
        closeParams.rightMargin = (int) (8 * density);
        closeButton.setLayoutParams(closeParams);
        closeButton.setOnClickListener(v -> animateDismissal(BreezeWebViewDismissReason.Dismissed));
        handleBar.addView(closeButton);

        // Separator line at bottom of handle bar
        View separator = new View(activity);
        separator.setBackgroundColor(resolveSeparatorColor());
        FrameLayout.LayoutParams sepParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, 1);
        sepParams.gravity = Gravity.BOTTOM;
        handleBar.addView(separator, sepParams);

        containerView.addView(handleBar);

        // Drag gesture on handle bar
        handleBar.setOnTouchListener((v, event) -> {
            handleDrag(event, screenHeight);
            return true;
        });

        // WebView
        webView = new WebView(activity);
        FrameLayout.LayoutParams webViewParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        webViewParams.topMargin = handleBarHeightPx;
        webView.setLayoutParams(webViewParams);

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);

        settings.setLoadWithOverviewMode(true);                     // fit content to screen width
        settings.setUseWideViewPort(true);                          // proper viewport for iframes
        settings.setSupportMultipleWindows(false);                  // prevent iframe popup issues
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_COMPATIBILITY_MODE); // allow mixed content in iframes
        settings.setLoadWithOverviewMode(true);
        settings.setLayoutAlgorithm(WebSettings.LayoutAlgorithm.TEXT_AUTOSIZING);

        // Enable third-party cookies for cross-origin payment iframes
        android.webkit.CookieManager cookieManager = android.webkit.CookieManager.getInstance();
        cookieManager.setAcceptCookie(true);
        cookieManager.setAcceptThirdPartyCookies(webView, true);

        webView.setWebViewClient(new BreezeWebViewClient());
        webView.addJavascriptInterface(new BreezeJsInterface(), BreezeConstants.JS_INTERFACE_NAME);

        containerView.addView(webView);

        // Inject JS bridge before loading
        injectJsBridge();

        // Add to window
        ViewGroup decorView = (ViewGroup) activity.getWindow().getDecorView();
        decorView.addView(rootOverlay);

        // Animate in
        animatePresentation();

        // Load URL
        Log.i(TAG, "BreezeWebView: loading url=" + url);
        webView.loadUrl(url);
    }

    // ---- JS Bridge ----

    private void injectJsBridge() {
        JSONArray allowedSuffixes = new JSONArray();
        for (String host : BreezeConstants.ALLOWED_HOSTS) {
            allowedSuffixes.put(host);
        }

        String deviceInfoJSON = getDeviceInfoJSON();

        String bridgeJs = "(function() {" +
                "var allowedSuffixes = " + allowedSuffixes.toString() + ";" +
                "var hostname = window.location.hostname.toLowerCase();" +
                "var allowed = allowedSuffixes.some(function(suffix) {" +
                "    return hostname.endsWith(suffix);" +
                "});" +
                "if (!allowed) return;" +
                "window._breeze = {" +
                "    version: '" + BreezeConstants.SDK_VERSION + "'," +
                "    platform: '" + BreezeConstants.SDK_PLATFORM + "'," +
                "    getDeviceInfo: function() {" +
                "        return " + deviceInfoJSON + ";" +
                "    }," +
                "    onPaymentSuccess: function(data) {" +
                "        " + BreezeConstants.JS_INTERFACE_NAME + ".onPaymentSuccess(data !== undefined ? String(data) : '');" +
                "    }," +
                "    onPaymentFailure: function(data) {" +
                "        " + BreezeConstants.JS_INTERFACE_NAME + ".onPaymentFailure(data !== undefined ? String(data) : '');" +
                "    }," +
                "    dismiss: function() {" +
                "        " + BreezeConstants.JS_INTERFACE_NAME + ".dismiss();" +
                "    }" +
                "};" +
                "})();";

        webView.evaluateJavascript(bridgeJs, null);
    }

    private String getDeviceInfoJSON() {
        try {
            JSONObject info = new JSONObject();
            info.put("deviceId", BreezeNativeAndroid.getInstance().getDeviceUniqueId());
            return info.toString();
        } catch (Exception e) {
            return "{}";
        }
    }

    /**
     * JavaScript interface exposed to web pages via {@code window._breezeNative}.
     */
    private class BreezeJsInterface {

        @JavascriptInterface
        public void onPaymentSuccess(String jsData) {
            Log.d(TAG, "JS: onPaymentSuccess");
            activity.runOnUiThread(() ->
                    animateDismissal(BreezeWebViewDismissReason.PaymentSuccess, jsData));
        }

        @JavascriptInterface
        public void onPaymentFailure(String jsData) {
            Log.d(TAG, "JS: onPaymentFailure");
            activity.runOnUiThread(() ->
                    animateDismissal(BreezeWebViewDismissReason.PaymentFailure, jsData));
        }

        @JavascriptInterface
        public void dismiss() {
            Log.d(TAG, "JS: dismiss");
            activity.runOnUiThread(() ->
                    animateDismissal(BreezeWebViewDismissReason.Dismissed));
        }
    }

    // ---- WebViewClient ----

    private class BreezeWebViewClient extends WebViewClient {

        /**
         * Intercept iframe requests from the payment provider and inject a CSS fix
         * for a WebView rendering bug where {@code background: transparent} (alpha 0)
         * on input elements causes the font to render at an incorrect (tiny) size.
         * Setting alpha to 0.01 is visually identical but avoids the bug.
         */
        @Override
        public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
            String url = request.getUrl().toString();
            String urlPath = url.contains("?") ? url.substring(0, url.indexOf("?")) : url;

            if (urlPath.startsWith("https://js.basistheory.com/")
                    && urlPath.contains("/hosted-elements/")
                    && urlPath.endsWith(".html")) {
                try {
                    HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
                    conn.setRequestMethod("GET");
                    Map<String, String> headers = request.getRequestHeaders();
                    if (headers != null) {
                        for (Map.Entry<String, String> entry : headers.entrySet()) {
                            conn.setRequestProperty(entry.getKey(), entry.getValue());
                        }
                    }

                    InputStream is = conn.getInputStream();
                    BufferedReader reader = new BufferedReader(new InputStreamReader(is, "utf-8"));
                    StringBuilder sb = new StringBuilder();
                    String line;
                    while ((line = reader.readLine()) != null) {
                        sb.append(line).append("\n");
                    }
                    reader.close();

                    String html = sb.toString();

                    // Fix: override fully-transparent backgrounds with near-transparent (alpha 0.01)
                    // to work around Android WebView font rendering bug.
                    String cssFix = "<style>input,textarea,select,div[contenteditable]"
                            + "{background-color:rgba(255,255,255,0.01)!important}</style>";

                    if (html.contains("</head>")) {
                        html = html.replace("</head>", cssFix + "</head>");
                    } else {
                        html = cssFix + html;
                    }

                    ByteArrayInputStream modifiedStream =
                            new ByteArrayInputStream(html.getBytes("utf-8"));
                    return new WebResourceResponse("text/html", "utf-8", modifiedStream);
                } catch (Exception e) {
                    Log.w(TAG, "BreezeWebView: failed to intercept iframe for CSS fix: "
                            + e.getMessage());
                }
            }

            return super.shouldInterceptRequest(view, request);
        }

        @Override
        public void onPageFinished(WebView view, String url) {
            super.onPageFinished(view, url);
            // Re-inject the bridge after each page load
            injectJsBridge();
        }

        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            Uri requestUri = request.getUrl();
            String scheme = requestUri.getScheme() != null ? requestUri.getScheme().toLowerCase() : "";
            Log.d(TAG, "BreezeWebView: navigating to " + requestUri);

            // Allow normal web navigation
            if ("http".equals(scheme) || "https".equals(scheme) || "about".equals(scheme)) {
                return false; // let WebView handle it
            }

            // Intercept custom-scheme payment redirects
            String host = requestUri.getHost() != null ? requestUri.getHost().toLowerCase() : "";
            String path = requestUri.getPath() != null ? requestUri.getPath() : "";

            if (BreezeConstants.PAYMENT_REDIRECT_HOST.equals(host)) {
                if (BreezeConstants.PAYMENT_REDIRECT_SUCCESS_PATH.equals(path)) {
                    activity.runOnUiThread(() ->
                            animateDismissal(BreezeWebViewDismissReason.PaymentSuccess));
                } else if (BreezeConstants.PAYMENT_REDIRECT_FAILURE_PATH.equals(path)) {
                    activity.runOnUiThread(() ->
                            animateDismissal(BreezeWebViewDismissReason.PaymentFailure));
                } else {
                    Log.w(TAG, "BreezeWebView: unknown payment redirect path: " + path);
                    activity.runOnUiThread(() ->
                            animateDismissal(BreezeWebViewDismissReason.Dismissed));
                }
                return true;
            }

            // Other custom schemes (tel:, mailto:, etc.) — open with system
            Log.d(TAG, "BreezeWebView: opening external URL: " + requestUri);
            try {
                Intent intent = new Intent(Intent.ACTION_VIEW, requestUri);
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                activity.startActivity(intent);
            } catch (Exception e) {
                Log.w(TAG, "BreezeWebView: failed to open external URL: " + e.getMessage());
            }
            return true;
        }

        @Override
        public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
            // Only handle main frame errors
            if (request.isForMainFrame()) {
                Log.e(TAG, "BreezeWebView: load error code=" + error.getErrorCode()
                        + " desc=" + error.getDescription()
                        + " url=" + request.getUrl());
                activity.runOnUiThread(() ->
                        animateDismissal(BreezeWebViewDismissReason.LoadError));
            }
        }
    }

    // ---- Drag to Resize ----

    private void handleDrag(MotionEvent event, int screenHeight) {
        int minHeight = (int) (screenHeight * MIN_HEIGHT_RATIO);
        int maxHeight = (int) (screenHeight * MAX_HEIGHT_RATIO);

        switch (event.getAction()) {
            case MotionEvent.ACTION_DOWN:
                dragStartY = event.getRawY();
                dragStartHeight = containerHeight;
                break;

            case MotionEvent.ACTION_MOVE:
                float deltaY = event.getRawY() - dragStartY;
                // Dragging down (positive delta) decreases height
                int newHeight = (int) (dragStartHeight - deltaY);
                newHeight = Math.min(Math.max(newHeight, minHeight), maxHeight);
                containerHeight = newHeight;
                updateContainerHeight(newHeight);
                break;

            case MotionEvent.ACTION_UP:
            case MotionEvent.ACTION_CANCEL:
                float velocityY = event.getRawY() - dragStartY;
                long dt = event.getEventTime() - event.getDownTime();
                float velocity = dt > 0 ? (velocityY / dt) * 1000f : 0;

                if (velocity > DISMISS_VELOCITY_THRESHOLD
                        || containerHeight < minHeight * 0.8f) {
                    animateDismissal(BreezeWebViewDismissReason.Dismissed);
                }
                break;
        }
    }

    private void updateContainerHeight(int height) {
        ViewGroup.LayoutParams params = containerView.getLayoutParams();
        params.height = height;
        containerView.setLayoutParams(params);
    }

    // ---- Animations ----

    private void animatePresentation() {
        containerView.animate()
                .translationY(0)
                .setDuration(ANIMATION_DURATION_MS)
                .setInterpolator(new DecelerateInterpolator())
                .start();

        dimBackground.animate()
                .alpha(1f)
                .setDuration(ANIMATION_DURATION_MS)
                .start();
    }

    private void animateDismissal(BreezeWebViewDismissReason reason) {
        animateDismissal(reason, null);
    }

    private void animateDismissal(BreezeWebViewDismissReason reason, String jsData) {
        if (didInvokeDismiss) return;

        containerView.animate()
                .translationY(containerView.getHeight())
                .setDuration(DISMISS_ANIMATION_DURATION_MS)
                .setInterpolator(new DecelerateInterpolator())
                .setListener(new AnimatorListenerAdapter() {
                    @Override
                    public void onAnimationEnd(Animator animation) {
                        cleanup();
                        invokeDismissCallback(reason, jsData);
                    }
                })
                .start();

        dimBackground.animate()
                .alpha(0f)
                .setDuration(DISMISS_ANIMATION_DURATION_MS)
                .start();
    }

    // ---- Dismiss / Cleanup ----

    private void invokeDismissCallback(BreezeWebViewDismissReason reason, String jsData) {
        if (didInvokeDismiss) return;
        didInvokeDismiss = true;

        String callbackData = jsData != null ? jsData : data;
        if (onDismissListener != null) {
            onDismissListener.onDismiss(reason, callbackData);
        }
    }

    private void cleanup() {
        if (webView != null) {
            webView.stopLoading();
            webView.removeJavascriptInterface(BreezeConstants.JS_INTERFACE_NAME);
            webView.setWebViewClient(null);
            webView.destroy();
            webView = null;
        }

        if (rootOverlay != null) {
            ViewGroup parent = (ViewGroup) rootOverlay.getParent();
            if (parent != null) {
                parent.removeView(rootOverlay);
            }
            rootOverlay = null;
        }
    }

    // ---- Theme Colors ----

    private int resolveBackgroundColor() {
        return isDarkMode() ? Color.parseColor("#26262B") : Color.WHITE;
    }

    private int resolveHandleBarColor() {
        return isDarkMode() ? Color.parseColor("#1C1C1E") : Color.parseColor("#F2F2F7");
    }

    private int resolvePillColor() {
        return isDarkMode() ? Color.parseColor("#636366") : Color.parseColor("#C7C7CC");
    }

    private int resolveSecondaryTextColor() {
        return isDarkMode() ? Color.parseColor("#8E8E93") : Color.parseColor("#8E8E93");
    }

    private int resolveSeparatorColor() {
        return isDarkMode() ? Color.parseColor("#38383A") : Color.parseColor("#C6C6C8");
    }

    private boolean isDarkMode() {
        int nightMode = activity.getResources().getConfiguration().uiMode
                & android.content.res.Configuration.UI_MODE_NIGHT_MASK;
        return nightMode == android.content.res.Configuration.UI_MODE_NIGHT_YES;
    }
}
