package com.breeze.sdk;

import android.app.Activity;
import android.app.Dialog;
import android.content.res.Configuration;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;
import android.widget.TextView;

import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class BreezePaymentOptionsDialog {

    private static final String TAG = "BreezePaymentDialog";

    public interface OnDismissListener {
        void onDismiss(BreezePaymentDialogDismissReason reason);
    }

    private final Activity activity;
    private final String title;
    private final String displayName;
    private final String originalPrice;
    private final String breezePrice;
    private final String decoration;
    private final String productIconUrl;
    private final String data;
    private final OnDismissListener listener;

    private Dialog dialog;
    private final ThemeColors colors;
    private boolean dismissed = false;

    public BreezePaymentOptionsDialog(
            Activity activity,
            String title,
            String displayName,
            String originalPrice,
            String breezePrice,
            String decoration,
            String productIconUrl,
            String directPaymentUrl,
            String data,
            String theme,
            OnDismissListener listener) {
        this.activity = activity;
        this.title = title;
        this.displayName = displayName;
        this.originalPrice = originalPrice;
        this.breezePrice = breezePrice;
        this.decoration = decoration;
        this.productIconUrl = productIconUrl;
        this.data = data;
        this.listener = listener;
        this.colors = resolveTheme(activity, theme);
    }

    public void show() {
        dialog = new Dialog(activity, android.R.style.Theme_Translucent_NoTitleBar);
        dialog.requestWindowFeature(Window.FEATURE_NO_TITLE);
        dialog.setCancelable(true);
        dialog.setOnCancelListener(d -> dismiss(BreezePaymentDialogDismissReason.CloseTapped));

        FrameLayout root = new FrameLayout(activity);
        root.setBackgroundColor(colors.overlay);
        root.setOnClickListener(v -> dismiss(BreezePaymentDialogDismissReason.CloseTapped));

        LinearLayout contentView = createContentView();
        contentView.setOnClickListener(v -> { /* consume click */ });

        FrameLayout.LayoutParams contentParams = new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.WRAP_CONTENT
        );
        contentParams.gravity = Gravity.BOTTOM;
        root.addView(contentView, contentParams);

        dialog.setContentView(root);

        Window window = dialog.getWindow();
        if (window != null) {
            window.setLayout(WindowManager.LayoutParams.MATCH_PARENT,
                    WindowManager.LayoutParams.MATCH_PARENT);
            window.setBackgroundDrawableResource(android.R.color.transparent);
        }

        dialog.show();
    }

    private void dismiss(BreezePaymentDialogDismissReason reason) {
        if (dismissed) return;
        dismissed = true;
        if (dialog != null && dialog.isShowing()) {
            dialog.dismiss();
        }
        if (listener != null) {
            listener.onDismiss(reason);
        }
    }

    private LinearLayout createContentView() {
        LinearLayout content = new LinearLayout(activity);
        content.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(20);
        content.setPadding(pad, pad, pad, pad);

        GradientDrawable bg = new GradientDrawable();
        bg.setColor(colors.contentBackground);
        float cornerRadius = dp(20);
        bg.setCornerRadii(new float[]{
                cornerRadius, cornerRadius,
                cornerRadius, cornerRadius,
                0, 0,
                0, 0
        });
        content.setBackground(bg);

        // Title bar
        content.addView(createTitleBar());

        // Product section
        LinearLayout.LayoutParams productParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        productParams.topMargin = dp(20);
        content.addView(createProductSection(), productParams);

        // Direct Payment button
        LinearLayout.LayoutParams directBtnParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dp(56)
        );
        directBtnParams.topMargin = dp(24);
        content.addView(createDirectPaymentButton(), directBtnParams);

        // Google Play Store button
        LinearLayout.LayoutParams storeBtnParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dp(56)
        );
        storeBtnParams.topMargin = dp(12);
        content.addView(createStoreButton(), storeBtnParams);

        // Footer
        LinearLayout.LayoutParams footerParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        footerParams.topMargin = dp(20);
        footerParams.gravity = Gravity.CENTER_HORIZONTAL;
        content.addView(createFooter(), footerParams);

        return content;
    }

    private RelativeLayout createTitleBar() {
        RelativeLayout container = new RelativeLayout(activity);
        container.setLayoutParams(new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(44)));

        TextView titleLabel = new TextView(activity);
        titleLabel.setText(title);
        titleLabel.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        titleLabel.setTypeface(Typeface.DEFAULT_BOLD);
        titleLabel.setTextColor(colors.titleText);
        RelativeLayout.LayoutParams titleParams = new RelativeLayout.LayoutParams(
                RelativeLayout.LayoutParams.WRAP_CONTENT,
                RelativeLayout.LayoutParams.WRAP_CONTENT
        );
        titleParams.addRule(RelativeLayout.ALIGN_PARENT_START);
        titleParams.addRule(RelativeLayout.CENTER_VERTICAL);
        container.addView(titleLabel, titleParams);

        TextView closeButton = new TextView(activity);
        closeButton.setText("\u2715");
        closeButton.setTextSize(TypedValue.COMPLEX_UNIT_SP, 24);
        closeButton.setTextColor(colors.closeButton);
        closeButton.setGravity(Gravity.CENTER);
        closeButton.setOnClickListener(v -> dismiss(BreezePaymentDialogDismissReason.CloseTapped));
        RelativeLayout.LayoutParams closeParams = new RelativeLayout.LayoutParams(dp(44), dp(44));
        closeParams.addRule(RelativeLayout.ALIGN_PARENT_END);
        closeParams.addRule(RelativeLayout.CENTER_VERTICAL);
        container.addView(closeButton, closeParams);

        return container;
    }

    private LinearLayout createProductSection() {
        LinearLayout container = new LinearLayout(activity);
        container.setOrientation(LinearLayout.HORIZONTAL);
        int pad = dp(16);
        container.setPadding(pad, pad, pad, pad);

        GradientDrawable bg = new GradientDrawable();
        bg.setColor(colors.productCardBackground);
        bg.setCornerRadius(dp(12));
        container.setBackground(bg);

        // Product image
        ImageView imageView = new ImageView(activity);
        GradientDrawable imageBg = new GradientDrawable();
        imageBg.setColor(colors.imagePlaceholder);
        imageBg.setCornerRadius(dp(8));
        imageView.setBackground(imageBg);
        imageView.setClipToOutline(true);
        imageView.setScaleType(ImageView.ScaleType.CENTER_CROP);
        LinearLayout.LayoutParams imageParams = new LinearLayout.LayoutParams(dp(80), dp(80));
        container.addView(imageView, imageParams);

        if (productIconUrl != null && !productIconUrl.isEmpty()) {
            loadImageAsync(productIconUrl, imageView);
        }

        // Text container
        LinearLayout textContainer = new LinearLayout(activity);
        textContainer.setOrientation(LinearLayout.VERTICAL);
        LinearLayout.LayoutParams textParams = new LinearLayout.LayoutParams(
                0, LinearLayout.LayoutParams.WRAP_CONTENT, 1
        );
        textParams.leftMargin = dp(16);
        textContainer.setGravity(Gravity.CENTER_VERTICAL);

        // Product name
        TextView nameLabel = new TextView(activity);
        nameLabel.setText(displayName);
        nameLabel.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        nameLabel.setTypeface(Typeface.DEFAULT_BOLD);
        nameLabel.setTextColor(colors.primaryText);
        textContainer.addView(nameLabel);

        // Original price (strikethrough)
        TextView originalPriceLabel = new TextView(activity);
        originalPriceLabel.setText(originalPrice);
        originalPriceLabel.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14);
        originalPriceLabel.setTextColor(colors.mutedText);
        originalPriceLabel.setPaintFlags(originalPriceLabel.getPaintFlags() | Paint.STRIKE_THRU_TEXT_FLAG);
        LinearLayout.LayoutParams opParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        opParams.topMargin = dp(8);
        textContainer.addView(originalPriceLabel, opParams);

        // Breeze price
        TextView breezePriceLabel = new TextView(activity);
        breezePriceLabel.setText(breezePrice);
        breezePriceLabel.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        breezePriceLabel.setTypeface(Typeface.DEFAULT_BOLD);
        breezePriceLabel.setTextColor(colors.primaryText);
        LinearLayout.LayoutParams bpParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        bpParams.topMargin = dp(4);
        textContainer.addView(breezePriceLabel, bpParams);

        container.addView(textContainer, textParams);

        return container;
    }

    private View createDirectPaymentButton() {
        FrameLayout container = new FrameLayout(activity);

        // Button
        TextView button = new TextView(activity);
        button.setText("Direct Payment " + breezePrice);
        button.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        button.setTypeface(Typeface.DEFAULT_BOLD);
        button.setTextColor(colors.primaryButtonText);
        button.setGravity(Gravity.CENTER);

        GradientDrawable btnBg = new GradientDrawable();
        btnBg.setColor(colors.primaryButtonBackground);
        btnBg.setCornerRadius(dp(12));
        button.setBackground(btnBg);
        button.setOnClickListener(v -> dismiss(BreezePaymentDialogDismissReason.DirectPaymentTapped));

        FrameLayout.LayoutParams btnParams = new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
        );
        container.addView(button, btnParams);

        // Badge (only if decoration is provided)
        if (decoration != null && !decoration.isEmpty()) {
            TextView badge = new TextView(activity);
            badge.setText(decoration);
            badge.setTextSize(TypedValue.COMPLEX_UNIT_SP, 12);
            badge.setTypeface(Typeface.DEFAULT_BOLD);
            badge.setTextColor(colors.badgeText);
            badge.setGravity(Gravity.CENTER);

            GradientDrawable badgeBg = new GradientDrawable();
            badgeBg.setColor(colors.badgeBackground);
            badgeBg.setCornerRadius(dp(8));
            badge.setBackground(badgeBg);

            FrameLayout.LayoutParams badgeParams = new FrameLayout.LayoutParams(dp(70), dp(24));
            badgeParams.gravity = Gravity.TOP | Gravity.END;
            badgeParams.rightMargin = dp(12);
            badgeParams.topMargin = dp(-4);
            container.addView(badge, badgeParams);
        }

        // Allow badge to overflow
        container.setClipChildren(false);
        container.setClipToPadding(false);

        return container;
    }

    private TextView createStoreButton() {
        TextView button = new TextView(activity);
        button.setText("Google Play Store " + originalPrice);
        button.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        button.setTextColor(colors.secondaryButtonText);
        button.setGravity(Gravity.CENTER);

        GradientDrawable bg = new GradientDrawable();
        bg.setColor(colors.secondaryButtonBackground);
        bg.setCornerRadius(dp(12));
        button.setBackground(bg);
        button.setOnClickListener(v -> dismiss(BreezePaymentDialogDismissReason.GoogleStoreTapped));

        return button;
    }

    private LinearLayout createFooter() {
        LinearLayout container = new LinearLayout(activity);
        container.setOrientation(LinearLayout.HORIZONTAL);

        TextView poweredBy = new TextView(activity);
        poweredBy.setText("Powered by");
        poweredBy.setTextSize(TypedValue.COMPLEX_UNIT_SP, 12);
        poweredBy.setTextColor(colors.footerMutedText);
        container.addView(poweredBy);

        TextView breeze = new TextView(activity);
        breeze.setText(" breeze");
        breeze.setTextSize(TypedValue.COMPLEX_UNIT_SP, 12);
        breeze.setTypeface(Typeface.DEFAULT_BOLD);
        breeze.setTextColor(colors.footerAccentText);
        container.addView(breeze);

        return container;
    }

    private void loadImageAsync(String urlString, ImageView imageView) {
        ExecutorService executor = Executors.newSingleThreadExecutor();
        Handler handler = new Handler(Looper.getMainLooper());

        executor.execute(() -> {
            try {
                URL url = new URL(urlString);
                HttpURLConnection connection = (HttpURLConnection) url.openConnection();
                connection.setDoInput(true);
                connection.setConnectTimeout(10000);
                connection.setReadTimeout(10000);
                connection.connect();

                InputStream input = connection.getInputStream();
                Bitmap bitmap = BitmapFactory.decodeStream(input);
                input.close();
                connection.disconnect();

                if (bitmap != null) {
                    handler.post(() -> {
                        imageView.setImageBitmap(bitmap);
                        imageView.setBackground(null);
                    });
                }
            } catch (Exception e) {
                Log.w(TAG, "Failed to load product icon: " + e.getMessage());
            }
        });
    }

    private int dp(int value) {
        return (int) TypedValue.applyDimension(
                TypedValue.COMPLEX_UNIT_DIP, value,
                activity.getResources().getDisplayMetrics()
        );
    }

    // --- Theme ---

    private static ThemeColors resolveTheme(Activity activity, String theme) {
        if ("light".equals(theme)) return ThemeColors.LIGHT;
        if ("dark".equals(theme)) return ThemeColors.DARK;

        // auto or null: check system dark mode
        int nightMode = activity.getResources().getConfiguration().uiMode
                & Configuration.UI_MODE_NIGHT_MASK;
        if (nightMode == Configuration.UI_MODE_NIGHT_YES) {
            return ThemeColors.DARK;
        }
        return ThemeColors.LIGHT;
    }

    static class ThemeColors {
        int overlay;
        int contentBackground;
        int titleText;
        int closeButton;
        int productCardBackground;
        int imagePlaceholder;
        int primaryText;
        int mutedText;
        int primaryButtonBackground;
        int primaryButtonText;
        int badgeText;
        int badgeBackground;
        int secondaryButtonBackground;
        int secondaryButtonText;
        int footerMutedText;
        int footerAccentText;

        static final ThemeColors LIGHT = new ThemeColors();
        static final ThemeColors DARK = new ThemeColors();

        static {
            LIGHT.overlay = Color.argb(128, 0, 0, 0);
            LIGHT.contentBackground = Color.WHITE;
            LIGHT.titleText = Color.rgb(51, 51, 51);
            LIGHT.closeButton = Color.rgb(102, 102, 102);
            LIGHT.productCardBackground = Color.rgb(242, 242, 242);
            LIGHT.imagePlaceholder = Color.rgb(255, 166, 0);
            LIGHT.primaryText = Color.rgb(51, 51, 51);
            LIGHT.mutedText = Color.rgb(153, 153, 153);
            LIGHT.primaryButtonBackground = Color.rgb(0, 122, 255);
            LIGHT.primaryButtonText = Color.WHITE;
            LIGHT.badgeText = Color.BLACK;
            LIGHT.badgeBackground = Color.rgb(255, 214, 0);
            LIGHT.secondaryButtonBackground = Color.rgb(230, 230, 230);
            LIGHT.secondaryButtonText = Color.rgb(51, 51, 51);
            LIGHT.footerMutedText = Color.rgb(153, 153, 153);
            LIGHT.footerAccentText = Color.rgb(51, 51, 51);

            DARK.overlay = Color.argb(153, 0, 0, 0);
            DARK.contentBackground = Color.rgb(38, 38, 43);
            DARK.titleText = Color.rgb(242, 242, 247);
            DARK.closeButton = Color.rgb(179, 179, 184);
            DARK.productCardBackground = Color.rgb(56, 56, 61);
            DARK.imagePlaceholder = Color.rgb(153, 102, 0);
            DARK.primaryText = Color.rgb(242, 242, 247);
            DARK.mutedText = Color.rgb(153, 153, 158);
            DARK.primaryButtonBackground = Color.rgb(0, 122, 255);
            DARK.primaryButtonText = Color.WHITE;
            DARK.badgeText = Color.rgb(38, 38, 43);
            DARK.badgeBackground = Color.rgb(255, 214, 0);
            DARK.secondaryButtonBackground = Color.rgb(89, 89, 94);
            DARK.secondaryButtonText = Color.rgb(242, 242, 247);
            DARK.footerMutedText = Color.rgb(153, 153, 158);
            DARK.footerAccentText = Color.rgb(242, 242, 247);
        }
    }
}
