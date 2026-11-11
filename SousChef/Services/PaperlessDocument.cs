namespace SousChef.Services;

/// <summary>
/// A document in Paperless-ngx.
/// </summary>
/// <param name="Id">The document ID.</param>
/// <param name="Title">The document title.</param>
public record PaperlessDocument(int Id, string Title);