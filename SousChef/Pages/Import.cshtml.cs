using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SousChef.Services;

namespace SousChef.Pages;

/// <summary>
/// Uploads recipe card PDFs, syncs them from Paperless, and re-parses stored cards.
/// </summary>
/// <param name="library">The recipe library.</param>
/// <param name="paperless">The Paperless client.</param>
/// <param name="logger">The logger.</param>
[RequestSizeLimit(250_000_000)]
[RequestFormLimits(MultipartBodyLengthLimit = 250_000_000)]
public class ImportModel(RecipeLibrary library, PaperlessClient paperless, ILogger<ImportModel> logger) : PageModel
{
    /// <summary>
    /// The uploaded PDF files.
    /// </summary>
    [BindProperty]
    public List<IFormFile> Files { get; set; } = [];

    /// <summary>
    /// The result of each upload or synced document.
    /// </summary>
    public List<ImportOutcome> Outcomes { get; set; } = [];

    /// <summary>
    /// A status message after syncing or re-parsing.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// The result of a Paperless sync.
    /// </summary>
    public string? SyncMessage { get; set; }

    /// <summary>
    /// Whether Paperless sync is set up.
    /// </summary>
    public bool PaperlessConfigured => paperless.IsConfigured;

    /// <summary>
    /// The Paperless tag that marks recipe cards.
    /// </summary>
    public string PaperlessTag => paperless.Tag;

    /// <summary>
    /// Shows the upload form.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Imports each uploaded PDF.
    /// </summary>
    public async Task OnPostAsync()
    {
        foreach (IFormFile file in Files)
        {
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                Outcomes.Add(new ImportOutcome { FileName = file.FileName, Message = "Not a PDF." });
                continue;
            }
            using MemoryStream stream = new();
            await file.CopyToAsync(stream);
            Outcomes.Add(await library.ImportAsync(Path.GetFileName(file.FileName), stream.ToArray()));
        }
    }

    /// <summary>
    /// Imports any tagged Paperless documents that haven't been imported yet.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnPostSyncAsync(CancellationToken cancellationToken)
    {
        if (!paperless.IsConfigured)
        {
            SyncMessage = "Paperless isn't set up yet: add Paperless:BaseUrl and Paperless:Token to the settings.";
            return;
        }
        try
        {
            List<PaperlessDocument> documents = await paperless.GetTaggedDocumentsAsync(cancellationToken);
            HashSet<int> imported = await library.GetPaperlessIdsAsync();
            List<PaperlessDocument> pending = [.. documents.Where(d => !imported.Contains(d.Id))];
            foreach (PaperlessDocument document in pending)
            {
                byte[] pdf = await paperless.DownloadAsync(document.Id, cancellationToken);
                Outcomes.Add(await library.ImportAsync($"{document.Title}.pdf", pdf, document.Id));
            }
            SyncMessage = pending.Count == 0 ? $"Paperless has {documents.Count} \"{paperless.Tag}\" document{(documents.Count == 1 ? string.Empty : "s")}, all already imported." : $"Synced {pending.Count} new card{(pending.Count == 1 ? string.Empty : "s")} from Paperless.";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Paperless sync failed");
            SyncMessage = $"Couldn't sync from Paperless: {ex.Message}";
        }
    }

    /// <summary>
    /// Re-parses every stored card with the current parser.
    /// </summary>
    public async Task<IActionResult> OnPostReparseAsync()
    {
        int count = await library.ReparseAllAsync();
        StatusMessage = $"Re-parsed {count} recipe card{(count == 1 ? string.Empty : "s")} (hand-edited recipes were left alone).";
        return RedirectToPage();
    }
}