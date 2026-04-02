using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using UnityEngine.Networking;

/// <summary>
/// General-purpose URL and locale utilities used internally by the Breeze SDK.
/// </summary>
public static class BreezeHelper
{
    /// <summary>
    /// Constructs an absolute URL from a base URL, a path, and an optional set of query parameters.
    /// </summary>
    /// <param name="baseUrl">The scheme and host portion of the URL (e.g., <c>"https://pay.breeze.cash"</c>).</param>
    /// <param name="path">The URL path (e.g., <c>"/checkout"</c>).</param>
    /// <param name="queryParams">Query parameters to append. Pass <c>null</c> or an empty collection for no query string.</param>
    /// <returns>The fully-formed absolute URI string.</returns>
    public static string BuildRequestUrl(string baseUrl, string path, NameValueCollection queryParams)
    {
        UriBuilder builder = new UriBuilder(baseUrl);
        builder.Path = path;
        builder.Query = BuildQueryString(queryParams);
        return builder.Uri.AbsoluteUri;
    }

    /// <summary>
    /// Merges or appends query parameters into an existing URL.
    /// </summary>
    /// <param name="url">The base URL whose query string will be modified.</param>
    /// <param name="extraParams">The parameters to merge in.</param>
    /// <param name="appendOnly">
    /// When <c>true</c>, parameters are added without removing existing keys (multi-value allowed).
    /// When <c>false</c> (default), existing keys are replaced by the values in <paramref name="extraParams"/>.
    /// </param>
    /// <returns>The URL with the updated query string.</returns>
    public static string UpdateUrlQueryParams(string url, NameValueCollection extraParams, bool appendOnly = false)
    {
        UriBuilder builder = new UriBuilder(url);
        var queryParams = HttpUtility.ParseQueryString(builder.Query);

        if (appendOnly)
        {
            queryParams.Add(extraParams);
        }
        else
        {
            foreach (string key in extraParams.AllKeys)
            {
                queryParams.Remove(key);
                var extraValues = extraParams.GetValues(key);
                if (extraValues != null)
                {
                    foreach (string val in extraValues)
                    {
                        queryParams.Add(key, val);
                    }
                }
            }
        }

        builder.Query = BuildQueryString(queryParams);

        return builder.Uri.AbsoluteUri;
    }

    /// <summary>
    /// Encodes a <see cref="NameValueCollection"/> into a URL query string using Unity's URL escaping.
    /// Keys or values that are <c>null</c> are treated as empty strings.
    /// </summary>
    /// <param name="queryParams">The parameters to encode. Returns an empty string when <c>null</c> or empty.</param>
    /// <returns>A percent-encoded query string without a leading <c>?</c>, e.g. <c>"key1=val1&amp;key2=val2"</c>.</returns>
    public static string BuildQueryString(NameValueCollection queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (string key in queryParams.AllKeys)
        {
            var encodedKey = UnityWebRequest.EscapeURL(key);
            var values = queryParams.GetValues(key);

            if (values != null && values.Length > 0)
            {
                foreach (var value in values)
                {
                    var encodedValue = UnityWebRequest.EscapeURL(value ?? string.Empty);
                    parts.Add($"{encodedKey}={encodedValue}");
                }
            }
            else
            {
                // Key without value
                parts.Add(encodedKey);
            }
        }

        return string.Join("&", parts);
    }

    /// <summary>
    /// Returns the two-letter ISO 3166-1 alpha-2 country code for the given <see cref="CultureInfo"/>.
    /// Falls back to <c>"US"</c> if the culture cannot be mapped to a region (e.g., invariant or neutral cultures).
    /// </summary>
    /// <param name="culture">The culture to resolve to a region.</param>
    /// <returns>A two-letter uppercase country code such as <c>"US"</c>, <c>"GB"</c>, or <c>"JP"</c>.</returns>
    public static string GetCurrentTwoLetterIsoRegionName(CultureInfo culture)
    {
        var countryCode = "US"; // Default fallback
        try
        {
            var regionInfo = new RegionInfo(culture.Name);
            countryCode = regionInfo.TwoLetterISORegionName;
        }
        catch
        {
            // ignore
        }
        return countryCode;
    }
}

/// <summary>
/// Utilities for converting between standard Base64 and URL-safe Base64url encoding (RFC 4648 §5).
/// </summary>
public static class BreezeBase64Helper
{
    /// <summary>
    /// Converts a Base64url-encoded string to a standard Base64 string by replacing URL-safe characters
    /// and re-adding padding.
    /// </summary>
    /// <param name="base64UrlString">The Base64url input string.</param>
    /// <returns>A standard Base64 string with <c>+</c>, <c>/</c>, and <c>=</c> padding characters.</returns>
    public static string ConvertBase64UrlToBase64(string base64UrlString)
    {
        var base64String = base64UrlString
            .Replace('-', '+')
            .Replace('_', '/');
        switch (base64String.Length % 4)
        {
            case 0: break;
            case 2: base64String += "=="; break;
            case 3: base64String += "="; break;
        }
        return base64String;
    }

    /// <summary>
    /// Converts a standard Base64 string to a Base64url string by replacing non-URL-safe characters
    /// and stripping padding.
    /// </summary>
    /// <param name="base64String">The standard Base64 input string.</param>
    /// <returns>A Base64url string safe for use in URLs and filenames.</returns>
    public static string ConvertBase64ToBase64Url(string base64String)
    {
        return base64String
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decodes a Base64url-encoded string to a UTF-8 string.
    /// </summary>
    /// <param name="base64UrlString">The Base64url-encoded input.</param>
    /// <returns>The decoded UTF-8 string.</returns>
    public static string DecodeBase64UrlToString(string base64UrlString)
    {
        var base64String = ConvertBase64UrlToBase64(base64UrlString);
        return DecodeBase64ToString(base64String);
    }

    /// <summary>
    /// Decodes a standard Base64 string to a byte array.
    /// </summary>
    /// <param name="base64String">The Base64-encoded input.</param>
    /// <returns>The decoded byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] DecodeBase64ToBytes(string base64String)
    {
        return Convert.FromBase64String(base64String);
    }

    /// <summary>
    /// Decodes a Base64url-encoded string to a byte array.
    /// </summary>
    /// <param name="base64UrlString">The Base64url-encoded input.</param>
    /// <returns>The decoded byte array.</returns>
    public static byte[] DecodeBase64UrlToBytes(string base64UrlString)
    {
        var base64String = ConvertBase64UrlToBase64(base64UrlString);
        return Convert.FromBase64String(base64String);
    }

    /// <summary>
    /// Decodes a standard Base64 string to a UTF-8 string.
    /// </summary>
    /// <param name="base64String">The Base64-encoded input.</param>
    /// <returns>The decoded UTF-8 string.</returns>
    public static string DecodeBase64ToString(string base64String)
    {
        byte[] bytes = Convert.FromBase64String(base64String);
        return Encoding.UTF8.GetString(bytes);
    }
}

/// <summary>
/// Extension methods for converting between <see cref="DateTime"/> and Unix timestamps.
/// </summary>
public static class BreezeDateTimeExtensions
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> to a Unix timestamp in milliseconds (UTC).
    /// </summary>
    /// <param name="now">The date/time value to convert.</param>
    /// <returns>Milliseconds elapsed since 1970-01-01T00:00:00Z.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixTimeMilliseconds(this DateTime now)
    {
        return new DateTimeOffset(now).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a Unix timestamp in seconds (UTC).
    /// </summary>
    /// <param name="now">The date/time value to convert.</param>
    /// <returns>Seconds elapsed since 1970-01-01T00:00:00Z.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixTimeSeconds(this DateTime now)
    {
        return new DateTimeOffset(now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a Unix timestamp in milliseconds to a <see cref="DateTime"/> (UTC).
    /// </summary>
    /// <param name="unixTimestampInMilliseconds">Milliseconds since the Unix epoch.</param>
    /// <returns>The corresponding UTC <see cref="DateTime"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTimeFromUnixTimeMilliseconds(this long unixTimestampInMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampInMilliseconds).UtcDateTime;
    }

    /// <summary>
    /// Converts a Unix timestamp in seconds to a <see cref="DateTime"/> (UTC).
    /// </summary>
    /// <param name="unixTimestampInSeconds">Seconds since the Unix epoch.</param>
    /// <returns>The corresponding UTC <see cref="DateTime"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTimeFromUnixTimeSeconds(this long unixTimestampInSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestampInSeconds).UtcDateTime;
    }
}
