using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BreezeSdk.Runtime
{
    /// <summary>
    /// Request object passed to <see cref="Breeze.ShowPaymentOptionsDialog"/> to configure the payment options dialog.
    /// </summary>
    public class BrzShowPaymentOptionsDialogRequest
    {
        /// <summary>
        /// Optional title displayed at the top of the payment options dialog.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Display information for the product being purchased (name, price, icon, etc.).
        /// </summary>
        [JsonProperty("product")]
        public BrzProductDisplayInfo ProductDisplayInfo { get; set; }

        /// <summary>
        /// The deep-link URL that opens the Breeze payment web view directly.
        /// Used when the user selects the "Pay with Breeze" option.
        /// </summary>
        [JsonProperty("directPaymentUrl")]
        public string DirectPaymentUrl { get; set; }

        /// <summary>
        /// An optional opaque string payload forwarded to the <see cref="Breeze.OnPaymentOptionsDialogDismissed"/> event.
        /// Use this to carry through any context your game needs when handling the dismissal.
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// Controls the color theme of the payment options dialog.
        /// When <c>null</c>, the dialog follows the system appearance. Defaults to <c>null</c>.
        /// </summary>
        [JsonProperty("theme", NullValueHandling = NullValueHandling.Ignore)]
        public BrzPaymentOptionsTheme? Theme { get; set; }
    }

    /// <summary>
    /// Color theme applied to the payment options dialog UI.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrzPaymentOptionsTheme
    {
        /// <summary>Follows the current system-level light/dark appearance setting.</summary>
        [EnumMember(Value = "auto")]
        Auto,

        /// <summary>Forces the dialog to use a light color scheme.</summary>
        [EnumMember(Value = "light")]
        Light,

        /// <summary>Forces the dialog to use a dark color scheme.</summary>
        [EnumMember(Value = "dark")]
        Dark
    }

    /// <summary>
    /// Display metadata for the product shown inside the payment options dialog.
    /// </summary>
    public class BrzProductDisplayInfo
    {
        /// <summary>
        /// Human-readable name of the product shown to the user (e.g., <c>"1000 Gold Coins"</c>).
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// The regular (non-Breeze) price string shown as the original price (e.g., <c>"$9.99"</c>).
        /// </summary>
        [JsonProperty("originalPrice")]
        public string OriginalPrice { get; set; }

        /// <summary>
        /// The discounted Breeze price string (e.g., <c>"$8.99"</c>).
        /// </summary>
        [JsonProperty("breezePrice")]
        public string BreezePrice { get; set; }

        /// <summary>
        /// Short promotional label shown on the product tile, e.g. <c>"10% off"</c>.
        /// </summary>
        [JsonProperty("decoration")]
        public string Decoration { get; set; }

        /// <summary>
        /// Product icon image URL. Loaded and displayed inside the dialog at runtime.
        /// </summary>
        [JsonProperty("productIconUrl")]
        public string ProductIconUrl { get; set; }
    }

    /// <summary>
    /// Callback invoked by the native layer when the payment options dialog is dismissed.
    /// </summary>
    /// <param name="reason">The reason the dialog was dismissed.</param>
    /// <param name="data">The data passed from the dialog.</param>
    public delegate void BrzPaymentDialogDismissCallback(BrzPaymentDialogDismissReason reason, string data);

    /// <summary>
    /// Describes why the payment options dialog was dismissed.
    /// Received via <see cref="Breeze.OnPaymentOptionsDialogDismissed"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrzPaymentDialogDismissReason : Int32
    {
        /// <summary>The user tapped the close/cancel button without selecting a payment option.</summary>
        [EnumMember(Value = "CloseTapped")]
        CloseTapped = 0,

        /// <summary>The user selected the direct Breeze payment option.</summary>
        [EnumMember(Value = "DirectPaymentTapped")]
        DirectPaymentTapped = 1,

        /// <summary>The user tapped the Apple App Store in-app purchase option.</summary>
        [EnumMember(Value = "AppStoreTapped")]
        AppStoreTapped = 2,

        /// <summary>The user tapped the Google Play Store in-app purchase option.</summary>
        [EnumMember(Value = "GoogleStoreTapped")]
        GoogleStoreTapped = 3,
    }

    /// <summary>
    /// Result code returned by the native layer after calling ShowPaymentOptionsDialog.
    /// Used internally; games should rely on <see cref="BrzPaymentDialogDismissReason"/> for flow control.
    /// </summary>
    public enum BrzShowPaymentOptionsResultCode : Int32
    {
        /// <summary>The dialog was shown successfully.</summary>
        Success = 0,

        /// <summary>A null input parameter was provided.</summary>
        NullInput = 1,

        /// <summary>The request JSON contained invalid UTF-8 characters.</summary>
        InvalidUtf8 = 2,

        /// <summary>The request JSON could not be decoded.</summary>
        JsonDecodingFailed = 3
    }

    /// <summary>
    /// Request object passed to <see cref="Breeze.ShowPaymentWebview"/> to open the Breeze payment web view.
    /// </summary>
    public class BrzShowPaymentWebviewRequest
    {
        /// <summary>
        /// The deep-link URL that the payment web view will load.
        /// Typically provided by the Breeze backend or constructed via your game server.
        /// </summary>
        [JsonProperty("directPaymentUrl")]
        public string DirectPaymentUrl { get; set; }

        /// <summary>
        /// An optional opaque string payload forwarded to the <see cref="Breeze.OnPaymentWebviewDismissed"/> event.
        /// Use this to carry through any context your game needs when handling the dismissal.
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }
    }

    /// <summary>
    /// Callback invoked by the native layer when the payment web view is dismissed.
    /// </summary>
    /// <param name="reason">The reason the web view was dismissed.</param>
    /// <param name="data">Optional JSON payload passed from the native web view.</param>
    public delegate void BrzPaymentWebviewDismissCallback(BrzPaymentWebviewDismissReason reason, string data);

    /// <summary>
    /// Describes why the payment web view was dismissed.
    /// Received via <see cref="Breeze.OnPaymentWebviewDismissed"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrzPaymentWebviewDismissReason : Int32
    {
        /// <summary>The user dismissed the web view manually without completing a payment.</summary>
        [EnumMember(Value = "Dismissed")]
        Dismissed = 0,

        /// <summary>The payment was completed successfully.</summary>
        [EnumMember(Value = "PaymentSuccess")]
        PaymentSuccess = 1,

        /// <summary>The payment failed or was declined.</summary>
        [EnumMember(Value = "PaymentFailure")]
        PaymentFailure = 2,

        /// <summary>The web view failed to load the payment page (e.g., no network).</summary>
        [EnumMember(Value = "LoadError")]
        LoadError = 3,
    }

    /// <summary>
    /// Result code returned by the native layer after calling ShowPaymentWebview.
    /// Used internally; games should rely on <see cref="BrzPaymentWebviewDismissReason"/> for flow control.
    /// </summary>
    public enum BrzShowPaymentWebviewResultCode : Int32
    {
        /// <summary>The web view was shown successfully.</summary>
        Success = 0,

        /// <summary>A null input parameter was provided.</summary>
        NullInput = 1,

        /// <summary>The request JSON contained invalid UTF-8 characters.</summary>
        InvalidUtf8 = 2,

        /// <summary>The request JSON could not be decoded.</summary>
        JsonDecodingFailed = 3,

        /// <summary>The provided payment URL was malformed or invalid.</summary>
        InvalidUrl = 4,
    }


    /// <summary>
    /// JSON payload received from the native layer when the payment options dialog is dismissed.
    /// </summary>
    [Serializable]
    public class DialogDismissedPayload
    {
        /// <summary>
        /// The reason the payment options dialog was dismissed.
        /// </summary>
        [JsonProperty("reason")]
        public BrzPaymentDialogDismissReason Reason;

        /// <summary>
        /// Optional data passed back from the dialog.
        /// </summary>
        [JsonProperty("data")]
        public string Data;
    }

    /// <summary>
    /// JSON payload received from the native layer when the payment web view is dismissed.
    /// </summary>
    [Serializable]
    public class WebViewDismissedPayload
    {
        /// <summary>
        /// The reason the payment web view was dismissed.
        /// </summary>
        [JsonProperty("reason")]
        public BrzPaymentWebviewDismissReason Reason;

        /// <summary>
        /// Optional data passed back from the web view.
        /// </summary>
        [JsonProperty("data")]
        public string Data;
    }

}