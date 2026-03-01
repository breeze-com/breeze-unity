using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class BrzShowPaymentOptionsDialogRequest
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("product")]
    public BrzProductDisplayInfo ProductDisplayInfo { get; set; }

    [JsonProperty("directPaymentUrl")]
    public string DirectPaymentUrl { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }

    [JsonProperty("theme", NullValueHandling = NullValueHandling.Ignore)]
    public BrzPaymentOptionsTheme? Theme { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum BrzPaymentOptionsTheme
{
    [EnumMember(Value = "auto")]
    Auto,

    [EnumMember(Value = "light")]
    Light,

    [EnumMember(Value = "dark")]
    Dark
}

public class BrzProductDisplayInfo
{
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("originalPrice")]
    public string OriginalPrice { get; set; }

    [JsonProperty("breezePrice")]
    public string BreezePrice { get; set; }

    /// <summary>
    /// e.g. "10% off"
    /// </summary>
    [JsonProperty("decoration")]
    public string Decoration { get; set; }

    /// <summary>
    /// Product icon image URL
    /// </summary>
    [JsonProperty("productIconUrl")]
    public string ProductIconUrl { get; set; }
}

/// <summary>
/// Callback for payment dialog dismissal.
/// </summary>
/// <param name="reason">The reason the dialog was dismissed.</param>
/// <param name="data">The data passed from the dialog.</param>
public delegate void BrzPaymentDialogDismissCallback(BrzPaymentDialogDismissReason reason, string data);

[JsonConverter(typeof(StringEnumConverter))]
public enum BrzPaymentDialogDismissReason : Int32
{
    [EnumMember(Value = "CloseTapped")]
    CloseTapped = 0,

    [EnumMember(Value = "DirectPaymentTapped")]
    DirectPaymentTapped = 1,

    [EnumMember(Value = "AppStoreTapped")]
    AppStoreTapped = 2,

    [EnumMember(Value = "GoogleStoreTapped")]
    GoogleStoreTapped = 3,
}

public enum BrzShowPaymentOptionsResultCode : Int32
{
    Success = 0,
    NullInput = 1,
    InvalidUtf8 = 2,
    JsonDecodingFailed = 3
}

public class BrzShowPaymentWebviewRequest
{
    [JsonProperty("directPaymentUrl")]
    public string DirectPaymentUrl { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }
}

public delegate void BrzPaymentWebviewDismissCallback(BrzPaymentWebviewDismissReason reason, string data);

[JsonConverter(typeof(StringEnumConverter))]
public enum BrzPaymentWebviewDismissReason : Int32
{
    [EnumMember(Value = "Dismissed")]
    Dismissed = 0,

    [EnumMember(Value = "PaymentSuccess")]
    PaymentSuccess = 1,

    [EnumMember(Value = "PaymentFailure")]
    PaymentFailure = 2,

    [EnumMember(Value = "LoadError")]
    LoadError = 3,
}

public enum BrzShowPaymentWebviewResultCode : Int32
{
    Success = 0,
    NullInput = 1,
    InvalidUtf8 = 2,
    JsonDecodingFailed = 3,
    InvalidUrl = 4,
}
