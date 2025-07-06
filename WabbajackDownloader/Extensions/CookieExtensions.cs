using System;
using System.Net;
using System.Text;
using Xilium.CefGlue;

namespace WabbajackDownloader.Extensions;

internal static class CookieExtensions
{
    public static Cookie ConvertCookie(this CefCookie cefCookie)
    {
        var cookie = new Cookie()
        {
            Name = cefCookie.Name,
            Value = cefCookie.Value,
            Domain = cefCookie.Domain,
            Path = cefCookie.Path,
            Secure = cefCookie.Secure,
            HttpOnly = cefCookie.HttpOnly,
        };
        if (cefCookie.Expires.HasValue)
        {
            cookie.Expires = cefCookie.Expires.Value.ConvertBaseTime();
        }
        return cookie;
    }

    public static string ToString(this Cookie cookie)
    {
        var sb = new StringBuilder();
        sb.Append($"Name: {cookie.Name}");
        sb.AppendLine($"Value: {cookie.Value}");
        sb.AppendLine($"Domain: {cookie.Domain}");
        sb.AppendLine($"Path: {cookie.Path}");
        sb.AppendLine($"Secure: {cookie.Secure}");
        sb.AppendLine($"HttpOnly: {cookie.HttpOnly}");
        sb.AppendLine($"Creation: {cookie.TimeStamp}");
        if (cookie.Expires != default)
            sb.AppendLine($"Expires: {cookie.Expires}");
        return sb.ToString();
    }

    public static string ToString(this CefCookie cookie)
    {
        var sb = new StringBuilder();
        sb.Append($"Name: {cookie.Name}");
        sb.AppendLine($"Value: {cookie.Value}");
        sb.AppendLine($"Domain: {cookie.Domain}");
        sb.AppendLine($"Path: {cookie.Path}");
        sb.AppendLine($"Secure: {cookie.Secure}");
        sb.AppendLine($"HttpOnly: {cookie.HttpOnly}");
        sb.AppendLine($"Creation: {cookie.Creation.ConvertBaseTime()}");
        if (cookie.Expires.HasValue)
            sb.AppendLine($"Expires: {cookie.Expires.Value.ConvertBaseTime()}");
        return sb.ToString();
    }

    // CefBaseTime represents a wall clock time in UTC. Time is stored internally as microseconds since the Windows epoch (1601).
    public static DateTime ConvertBaseTime(this CefBaseTime cefTime)
    {
        // Treat a zero value as no time set.
        if (cefTime.Ticks == 0)
            return default;

        // Multiply by 10 to convert microseconds to 100-nanosecond ticks, then converts a Windows FILETIME (ticks since 1601) into a DateTime.
        return DateTime.FromFileTime(cefTime.Ticks * 10);
    }
}
