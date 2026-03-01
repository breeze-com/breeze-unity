package com.breeze.sdk;

/**
 * Matches C# BrzPaymentWebviewDismissReason enum values.
 */
public enum BreezeWebViewDismissReason {
    Dismissed("Dismissed", 0),
    PaymentSuccess("PaymentSuccess", 1),
    PaymentFailure("PaymentFailure", 2),
    LoadError("LoadError", 3);

    private final String value;
    private final int id;

    BreezeWebViewDismissReason(String value, int id) {
        this.value = value;
        this.id = id;
    }

    @Override
    public String toString() {
        return value;
    }

    public String getValue() {
        return this.value;
    }

    public int getId() {
        return this.id;
    }
}
