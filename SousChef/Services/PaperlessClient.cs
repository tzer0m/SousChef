using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SousChef.Services;

/// <summary>
/// Reads tagged recipe cards from the Paperless-ngx REST API.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The Paperless settings.</param>
public class PaperlessClient(HttpClient http, IOptions<PaperlessOptions> options)
{
    private readonly PaperlessOptions _options = options.Value;

    /// <summary>
    /// Whether a Paperless URL and token have been configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl) && !string.IsNullOrWhiteSpace(_options.Token);

    /// <summary>
    /// The tag that marks recipe cards.
    /// </summary>
    public string Tag => _options.Tag;

    /// <summary>
    /// Lists every document with the recipe card tag.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<PaperlessDocument>> GetTaggedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        using JsonDocument tags = await GetJsonAsync($"api/tags/?name__iexact={Uri.EscapeDataString(_options.Tag)}", cancellationToken);
        JsonElement tag = tags.RootElement.GetProperty("results").EnumerateArray().FirstOrDefault();
        if (tag.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Paperless has no tag called \"{_options.Tag}\".");
        }
        List<PaperlessDocument> documents = [];
        string? next = $"api/documents/?tags__id__all={tag.GetProperty("id").GetInt32()}&page_size=100&fields=id,title";
        while (next != null)
        {
            using JsonDocument page = await GetJsonAsync(next, cancellationToken);
            documents.AddRange(page.RootElement.GetProperty("results").EnumerateArray().Select(d => new PaperlessDocument(d.GetProperty("id").GetInt32(), d.GetProperty("title").GetString() ?? string.Empty)));
            next = page.RootElement.TryGetProperty("next", out JsonElement link) && link.ValueKind == JsonValueKind.String ? PinToBaseHost(link.GetString()!) : null;
        }
        return documents;
    }

    /// <summary>
    /// Downloads a document's archived PDF, which includes Paperless's OCR text layer.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<byte[]> DownloadAsync(int id, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest($"api/documents/{id}/download/");
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Sends an authenticated GET and parses the JSON response.
    /// </summary>
    /// <param name="url">A path relative to the Paperless base URL, or an absolute "next page" link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Points a "next page" link at the configured Paperless host, so the token is never sent anywhere else.
    /// </summary>
    /// <param name="link">The link from the API response.</param>
    private string PinToBaseHost(string link)
    {
        Uri baseUri = new(_options.BaseUrl!);
        UriBuilder builder = new(link) { Scheme = baseUri.Scheme, Host = baseUri.Host, Port = baseUri.Port };
        return builder.Uri.ToString();
    }

    /// <summary>
    /// Creates a GET request with the API token.
    /// </summary>
    /// <param name="url">A path relative to the Paperless base URL, or an absolute URL.</param>
    private HttpRequestMessage CreateRequest(string url)
    {
        Uri baseUri = new(_options.BaseUrl!.TrimEnd('/') + "/");
        HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, url));
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.Token);
        return request;
    }
}