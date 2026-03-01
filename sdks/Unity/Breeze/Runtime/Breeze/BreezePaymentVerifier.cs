using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Polls the game server for payment status after a Breeze payment flow.
///
/// Flow:
/// 1. Game creates order on game server → game server creates Breeze payment page
/// 2. Player pays via Breeze (SDK opens browser)
/// 3. Breeze sends webhook to game server → game server marks order as paid
/// 4. This verifier polls game server until order status is terminal
///
/// Usage:
///   var config = new BrzPaymentVerificationConfig {
///       GameServerBaseUrl = "https://api.yourgame.com",
///       AuthToken = playerAuthToken
///   };
///   var verifier = new BreezePaymentVerifier(config);
///   var result = await verifier.WaitForPaymentAsync("order-123");
///   if (result.IsSuccess) { GrantItems(); }
/// </summary>
public class BreezePaymentVerifier
{
    private readonly BrzPaymentVerificationConfig _config;

    public BreezePaymentVerifier(BrzPaymentVerificationConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrEmpty(config.GameServerBaseUrl))
            throw new ArgumentException("GameServerBaseUrl is required", nameof(config));
        if (!config.GameServerBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("GameServerBaseUrl must use HTTPS", nameof(config));

        _config = config;
    }

    /// <summary>
    /// Poll the game server for payment status until it reaches a terminal state
    /// (succeeded, failed, expired, refunded) or the timeout/max attempts is reached.
    /// </summary>
    /// <param name="orderId">The order ID to check (same as clientReferenceId sent to Breeze).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Payment verification result.</returns>
    public async Awaitable<BrzPaymentVerificationResult> WaitForPaymentAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return new BrzPaymentVerificationResult
            {
                Status = BrzPaymentStatus.Unknown,
                OrderId = orderId,
                Error = "Order ID is required"
            };
        }

        string statusUrl = BuildStatusUrl(orderId);
        float startTime = Time.realtimeSinceStartup;
        int attempts = 0;

        Debug.Log($"[BreezeVerifier] Starting payment verification for {orderId}");

        while (!cancellationToken.IsCancellationRequested)
        {
            attempts++;

            // Check limits
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed >= _config.TimeoutSeconds)
            {
                Debug.LogWarning($"[BreezeVerifier] Timeout after {elapsed:F1}s for {orderId}");
                return new BrzPaymentVerificationResult
                {
                    Status = BrzPaymentStatus.Pending,
                    OrderId = orderId,
                    Error = $"Verification timed out after {_config.TimeoutSeconds}s"
                };
            }

            if (attempts > _config.MaxAttempts)
            {
                Debug.LogWarning($"[BreezeVerifier] Max attempts ({_config.MaxAttempts}) reached for {orderId}");
                return new BrzPaymentVerificationResult
                {
                    Status = BrzPaymentStatus.Pending,
                    OrderId = orderId,
                    Error = $"Max poll attempts ({_config.MaxAttempts}) reached"
                };
            }

            // Poll
            var result = await PollOnceAsync(statusUrl, orderId);

            if (result.IsTerminal)
            {
                Debug.Log($"[BreezeVerifier] Payment {orderId} reached terminal state: {result.Status}");
                return result;
            }

            Debug.Log($"[BreezeVerifier] Attempt {attempts}: {orderId} status={result.Status}, waiting {_config.PollIntervalSeconds}s...");

            // Wait before next poll
            await Awaitable.WaitForSecondsAsync(_config.PollIntervalSeconds);
        }

        // Cancelled
        return new BrzPaymentVerificationResult
        {
            Status = BrzPaymentStatus.Unknown,
            OrderId = orderId,
            Error = "Verification cancelled"
        };
    }

    /// <summary>
    /// Single poll attempt to check payment status.
    /// </summary>
    public async Awaitable<BrzPaymentVerificationResult> PollOnceAsync(string orderId)
    {
        string statusUrl = BuildStatusUrl(orderId);
        return await PollOnceAsync(statusUrl, orderId);
    }

    private async Awaitable<BrzPaymentVerificationResult> PollOnceAsync(string statusUrl, string orderId)
    {
        try
        {
            using var request = UnityWebRequest.Get(statusUrl);

            // Auth header
            if (!string.IsNullOrEmpty(_config.AuthToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_config.AuthToken}");
            }

            // Extra headers
            if (_config.ExtraHeaders != null)
            {
                foreach (var kvp in _config.ExtraHeaders)
                {
                    request.SetRequestHeader(kvp.Key, kvp.Value);
                }
            }

            request.timeout = 10; // 10s per request

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BreezeVerifier] Poll failed: {request.error}");
                return new BrzPaymentVerificationResult
                {
                    Status = BrzPaymentStatus.Unknown,
                    OrderId = orderId,
                    Error = $"HTTP error: {request.error}"
                };
            }

            string json = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<BrzOrderStatusResponse>(json);

            if (response == null)
            {
                return new BrzPaymentVerificationResult
                {
                    Status = BrzPaymentStatus.Unknown,
                    OrderId = orderId,
                    Error = "Invalid response from server"
                };
            }

            BrzPaymentStatus status = ParseStatus(response.Status);

            return new BrzPaymentVerificationResult
            {
                Status = status,
                OrderId = response.OrderId ?? orderId,
                TransactionId = response.TransactionId,
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BreezeVerifier] Exception polling {orderId}: {ex.Message}");
            return new BrzPaymentVerificationResult
            {
                Status = BrzPaymentStatus.Unknown,
                OrderId = orderId,
                Error = ex.Message
            };
        }
    }

    private string BuildStatusUrl(string orderId)
    {
        string path = _config.StatusPathTemplate.Replace("{orderId}", Uri.EscapeDataString(orderId));
        return _config.GameServerBaseUrl.TrimEnd('/') + path;
    }

    private static BrzPaymentStatus ParseStatus(string status)
    {
        if (string.IsNullOrEmpty(status))
            return BrzPaymentStatus.Unknown;

        return status.ToLowerInvariant() switch
        {
            "succeeded" or "success" or "paid" or "completed" => BrzPaymentStatus.Succeeded,
            "failed" or "failure" or "declined" => BrzPaymentStatus.Failed,
            "expired" or "timeout" => BrzPaymentStatus.Expired,
            "refunded" => BrzPaymentStatus.Refunded,
            "pending" or "processing" or "created" => BrzPaymentStatus.Pending,
            _ => BrzPaymentStatus.Unknown,
        };
    }
}
