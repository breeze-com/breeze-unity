package com.breeze.sdk;

public enum BreezePaymentDialogDismissReason {
    CloseTapped("CloseTapped", 0),
    DirectPaymentTapped("DirectPaymentTapped", 1),
    AppStoreTapped("AppStoreTapped", 2),
    GoogleStoreTapped("GoogleStoreTapped", 3);

    private final String value;
    private final int id;

    BreezePaymentDialogDismissReason(String value, int id) {
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
