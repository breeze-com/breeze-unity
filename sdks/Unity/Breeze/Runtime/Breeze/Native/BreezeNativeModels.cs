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

// ─── Payment Verification (Option C: Server webhook + client poll) ──────────

/// <summary>
/// Payment status returned by the game server's order status endpoint.
/// The game server updates this based on Breeze webhook events.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum BrzPaymentStatus
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "succeeded")]
    Succeeded,

    [EnumMember(Value = "failed")]
    Failed,

    [EnumMember(Value = "expired")]
    Expired,

    [EnumMember(Value = "refunded")]
    Refunded,

    [EnumMember(Value = "unknown")]
    Unknown,
}

/// <summary>
/// Result of polling the game server for payment status.
/// </summary>
public class BrzPaymentVerificationResult
{
    [JsonProperty("status")]
    public BrzPaymentStatus Status { get; set; } = BrzPaymentStatus.Unknown;

    [JsonProperty("orderId")]
    public string OrderId { get; set; }

    [JsonProperty("transactionId")]
    public string TransactionId { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    /// <summary>
    /// Whether the payment is in a terminal state (succeeded, failed, expired, refunded).
    /// </summary>
    public bool IsTerminal =>
        Status == BrzPaymentStatus.Succeeded ||
        Status == BrzPaymentStatus.Failed ||
        Status == BrzPaymentStatus.Expired ||
        Status == BrzPaymentStatus.Refunded;

    /// <summary>
    /// Whether the payment was successful.
    /// </summary>
    public bool IsSuccess => Status == BrzPaymentStatus.Succeeded;
}

/// <summary>
/// Configuration for payment verification polling.
/// </summary>
public class BrzPaymentVerificationConfig
{
    /// <summary>
    /// Base URL of your game server (e.g. "https://api.yourgame.com").
    /// </summary>
    public string GameServerBaseUrl { get; set; }

    /// <summary>
    /// Path template for checking order status. Use {orderId} as placeholder.
    /// Default: "/v1/orders/{orderId}/status"
    /// </summary>
    public string StatusPathTemplate { get; set; } = "/v1/orders/{orderId}/status";

    /// <summary>
    /// Interval between poll attempts in seconds. Default: 2.0
    /// </summary>
    public float PollIntervalSeconds { get; set; } = 2.0f;

    /// <summary>
    /// Maximum time to poll before giving up, in seconds. Default: 120 (2 minutes)
    /// </summary>
    public float TimeoutSeconds { get; set; } = 120f;

    /// <summary>
    /// Maximum number of poll attempts. Default: 60.
    /// Whichever limit is hit first (timeout or max attempts) stops polling.
    /// </summary>
    public int MaxAttempts { get; set; } = 60;

    /// <summary>
    /// Optional auth token to include as Bearer token in poll requests.
    /// </summary>
    public string AuthToken { get; set; }

    /// <summary>
    /// Optional additional headers to include in poll requests.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, string> ExtraHeaders { get; set; }
}

/// <summary>
/// Response format expected from the game server's order status endpoint.
/// Game server should return JSON matching this shape.
/// </summary>
public class BrzOrderStatusResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("orderId")]
    public string OrderId { get; set; }

    [JsonProperty("transactionId")]
    public string TransactionId { get; set; }
}
