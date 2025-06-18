using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WabbajackDownloader.Http;
/// <summary>
/// A wrapper around HttpRequestMessage with readable instantiation
/// Designed to be used with object initializer syntax
/// </summary>
internal class HttpRequest
{
    private readonly List<KeyValuePair<string, string>> headers = new();

    /// <summary>
    /// Base url without query. E.g. https://www.example.com/my_example
    /// </summary>
    public string? BaseUrl;
    /// <summary>
    /// Query string following the base url
    /// </summary>
    public HttpRequestQuery? Query;
    /// <summary>
    /// Request type. Default is GET
    /// </summary>
    public HttpMethod Method = HttpMethod.Get;
    /// <summary>
    /// Payload content used in POST requests
    /// </summary>
    public HttpRequestContent? Content;
    /// <summary>
    /// Add headers by declaring [header] = value
    /// </summary>
    public string this[string header]
    {
        set { headers.Add(new(header, value)); }
    }

    /// <summary>
    /// Send this request using the specified Http client
    /// </summary>
    /// <param name="client">The client sending the request</param>
    /// <returns>Response content</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<HttpContent> SendAsync(HttpClient client, bool ensureSuccess = true, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(BaseUrl))
            throw new ArgumentException("BaseUrl cannot be null or empty");

        var uriString = Query is null ? BaseUrl : BaseUrl + Query.Data;
        var uri = new Uri(uriString);

        using var message = new HttpRequestMessage(Method, uri);
        // Add content
        if (Content != null)
            message.Content = Content.Data;
        // Add headers
        foreach (var header in headers)
        {
            message.Headers.Add(header.Key, header.Value);
        }

        // Send message and await response
        var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);

        if (ensureSuccess)
            response.EnsureSuccessStatusCode();

        return response.Content;
    }
}
