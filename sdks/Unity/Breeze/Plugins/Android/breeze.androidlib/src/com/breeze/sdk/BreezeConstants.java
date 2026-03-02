package com.breeze.sdk;

public final class BreezeConstants {

    private BreezeConstants() {}

    public static final String SDK_VERSION = "0.1.0";
    public static final String SDK_PLATFORM = "android";

    /** Domains allowed for webview navigation and JS bridge injection. */
    public static final String[] ALLOWED_HOSTS = {".breeze.cash", ".breeze.com"};

    /** Custom-scheme payment redirect constants. */
    public static final String PAYMENT_REDIRECT_HOST = "breeze-payment";
    public static final String PAYMENT_REDIRECT_SUCCESS_PATH = "/purchase/success";
    public static final String PAYMENT_REDIRECT_FAILURE_PATH = "/purchase/failure";

    /** JavaScript interface name exposed to the web page. */
    public static final String JS_INTERFACE_NAME = "_breezeNative";
}
