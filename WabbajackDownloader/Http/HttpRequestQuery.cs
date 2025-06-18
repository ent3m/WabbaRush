using System.Collections.Generic;
using System.Text;

namespace WabbajackDownloader.Http;
/// <summary>
/// Provides an easy way to create request query with object initializer syntax
/// </summary>
internal class HttpRequestQuery
{
    private string? data;
    private readonly List<KeyValuePair<string, string>> kvps = new();

    /// <summary>
    /// Add key and value pairs for query payload. Key value pairs will not be used if Data is already set
    /// </summary>
    public string this[string key]
    {
        set { kvps.Add(new(key, value)); }
    }
    /// <summary>
    /// Set the query directly with string value or retrieve it
    /// </summary>
    public string Data
    {
        get { return data ?? FormQuery(); }
        set { data = value; }
    }

    /// <summary>
    /// Create query from key value pairs
    /// </summary>
    private string FormQuery()
    {
        if (kvps.Count == 0)
            return string.Empty;

        var query = new StringBuilder();
        query.Append('?');
        foreach (var pair in kvps)
        {
            query.Append(pair.Key);
            query.Append('=');
            query.Append(pair.Value);
            query.Append('&');
        }
        query.Length--;
        return query.ToString();
    }
}
