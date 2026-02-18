using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using UnityEngine.Networking;

public static class BreezeHelper
{
    public static string BuildRequestUrl(string baseUrl, string path, NameValueCollection queryParams)
    {
        UriBuilder builder = new UriBuilder(baseUrl);
        builder.Path = path;
        builder.Query = BuildQueryString(queryParams);
        return builder.Uri.ToString();
    }

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

        return builder.Uri.ToString();
    }

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

public static class BreezeBase64Helper
{
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

    public static string ConvertBase64ToBase64Url(string base64String)
    {
        return base64String
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string DecodeBase64UrlToString(string base64UrlString)
    {
        var base64String = ConvertBase64UrlToBase64(base64UrlString);
        return DecodeBase64ToString(base64String);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] DecodeBase64ToBytes(string base64String)
    {
        return Convert.FromBase64String(base64String);
    }

    public static byte[] DecodeBase64UrlToBytes(string base64UrlString)
    {
        var base64String = ConvertBase64UrlToBase64(base64UrlString);
        return Convert.FromBase64String(base64String);
    }

    public static string DecodeBase64ToString(string base64String)
    {
        byte[] bytes = Convert.FromBase64String(base64String);
        return Encoding.UTF8.GetString(bytes);
    }
}

public static class BreezeDateTimeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixTimeMilliseconds(this DateTime now)
    {
        return new DateTimeOffset(now).ToUnixTimeMilliseconds();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixTimeSeconds(this DateTime now)
    {
        return new DateTimeOffset(now).ToUnixTimeSeconds();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTimeFromUnixTimeMilliseconds(this long unixTimestampInMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampInMilliseconds).UtcDateTime;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTimeFromUnixTimeSeconds(this long unixTimestampInSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestampInSeconds).UtcDateTime;
    }
}
