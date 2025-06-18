using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace WabbajackDownloader.Http;
/// <summary>
/// Wrapper around System.Net.Http.HttpContent for easy creation
/// </summary>
internal class HttpRequestContent
{
    private HttpContent? httpContent;
    private readonly List<KeyValuePair<string, string>> kvps = new();

    /// <summary>
    /// Add key and value pair for FormUrl and Json content type. Key value pairs will not be used if StringData or Data is already set
    /// </summary>
    public string this[string key]
    {
        set { kvps.Add(new(key, value)); }
    }
    /// <summary>
    /// Set raw string content
    /// </summary>
    public string StringData
    {
        set { httpContent = new StringContent(value, Encoding.UTF8); }
    }
    /// <summary>
    /// Set HttpContent directly or retrieve it. HttpContent will be constructed from key value pairs if null
    /// </summary>
    public HttpContent? Data
    {
        get
        {
            BuildContent();
            return httpContent;
        }
        set { httpContent = value; }
    }
    /// <summary>
    /// Type is FormUrl by default. Make sure it matches the content provided by StringData, KVPs, and Data
    /// </summary>
    public ContentType Type;

    private void BuildContent()
    {
        if (httpContent == null)
        {
            if (kvps.Count == 0)
                return;

            if (Type == ContentType.Json)
            {
                var sb = new StringBuilder();
                sb.Append('{');
                foreach (var kvp in kvps)
                {
                    sb.Append('"');
                    sb.Append(kvp.Key);
                    sb.Append('"');
                    sb.Append(':');
                    sb.Append('"');
                    sb.Append(kvp.Value);
                    sb.Append('"');
                    sb.Append(',');
                }
                sb.Length--;
                sb.Append('}');
                httpContent = new StringContent(sb.ToString(), Encoding.UTF8);
            }
            else
            {
                httpContent = new FormUrlEncodedContent(kvps);
                Type = ContentType.FormUrl;
            }
        }
        httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Type.ToStringValue(), Encoding.UTF8.WebName);
        return;
    }
}

/// <summary>
/// Possible values for content type header
/// </summary>
internal enum ContentType
{
    FormUrl,
    Json,
    Xml,
    Text
}

/// <summary>
/// Give string value for ContentType
/// </summary>
file static class ContentTypeExtension
{
    public static string ToStringValue(this ContentType? type) => GetString(type.GetValueOrDefault());
    public static string ToStringValue(this ContentType type) => GetString(type);

    public static string GetString(ContentType type) => type switch
    {
        ContentType.FormUrl => "application/x-www-form-urlencoded",
        ContentType.Json => "application/json",
        ContentType.Xml => "application/xml",
        ContentType.Text => "text/plain",
        _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid content type")
    };
}
