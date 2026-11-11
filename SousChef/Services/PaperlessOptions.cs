namespace SousChef.Services;

/// <summary>
/// Settings for syncing recipe cards from Paperless-ngx.
/// </summary>
public class PaperlessOptions
{
    /// <summary>
    /// The Paperless base URL, such as "https://paperless.example.com".
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The Paperless API token (keep in user secrets).
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The tag that marks recipe cards.
    /// </summary>
    public string Tag { get; set; } = "HelloFresh";
}