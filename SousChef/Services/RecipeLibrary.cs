using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SousChef.Core.Models;
using SousChef.Core.Parsing;
using SousChef.Data;

namespace SousChef.Services;

/// <summary>
/// Imports, stores and looks up recipe cards.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="parser">The recipe card parser.</param>
public class RecipeLibrary(SousChefDbContext db, RecipeCardParser parser)
{
    /// <summary>
    /// Parses and stores a card, updating the existing recipe if the card was imported before.
    /// </summary>
    /// <param name="fileName">The uploaded file name (or Paperless title).</param>
    /// <param name="pdf">The PDF bytes.</param>
    /// <param name="paperlessId">The Paperless document ID, if the card came from Paperless.</param>
    public async Task<ImportOutcome> ImportAsync(string fileName, byte[] pdf, int? paperlessId = null)
    {
        ParseResult result;
        try
        {
            result = parser.Parse(pdf, fileName);
        }
        catch (Exception ex)
        {
            return new ImportOutcome { FileName = fileName, Message = $"Couldn't read the PDF: {ex.Message}" };
        }
        Recipe recipe = result.Recipe;
        if (recipe.Title.Length == 0 && recipe.Ingredients.Count == 0)
        {
            return new ImportOutcome { FileName = fileName, Message = "This doesn't look like a HelloFresh recipe card.", Warnings = recipe.ParseWarnings };
        }
        RecipeRecord? record = await FindExistingAsync(recipe, paperlessId);
        if (record != null && record.IsEdited)
        {
            return new ImportOutcome { FileName = fileName, Succeeded = true, IsUpdate = true, RecipeId = record.Id, Title = record.Title, Message = "Already imported and corrected by hand, so your edits were kept." };
        }
        bool isNew = record == null;
        record ??= new RecipeRecord { ImportedAt = DateTime.UtcNow };
        record.PaperlessId = paperlessId ?? record.PaperlessId;
        Apply(record, result, fileName, pdf);
        if (isNew)
        {
            db.Recipes.Add(record);
        }
        await db.SaveChangesAsync();
        return new ImportOutcome { FileName = fileName, Succeeded = true, IsUpdate = !isNew, RecipeId = record.Id, Title = recipe.Title, Warnings = recipe.ParseWarnings };
    }

    /// <summary>
    /// Re-parses every stored card that hasn't been edited by hand, and returns how many were updated.
    /// </summary>
    public async Task<int> ReparseAllAsync()
    {
        List<int> ids = await db.Recipes.Where(r => !r.IsEdited).Select(r => r.Id).ToListAsync();
        foreach (int id in ids)
        {
            RecipeRecord record = await db.Recipes.Include(r => r.Images).SingleAsync(r => r.Id == id);
            Apply(record, parser.Parse(record.Pdf, record.SourceFileName), record.SourceFileName, record.Pdf);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
        return ids.Count;
    }

    /// <summary>
    /// Lists recipes for the library page.
    /// </summary>
    /// <param name="search">Text to search for in titles, tags and ingredients.</param>
    /// <param name="favouritesOnly">Whether to show only favourites.</param>
    /// <param name="mildOnly">Whether to hide spicy recipes.</param>
    /// <param name="sort">The sort order.</param>
    public async Task<List<RecipeSummary>> GetSummariesAsync(string? search, bool favouritesOnly, bool mildOnly, LibrarySort sort)
    {
        IQueryable<RecipeRecord> query = db.Recipes.AsNoTracking();
        foreach (string term in (search ?? string.Empty).ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Where(r => r.SearchText.Contains(term));
        }
        if (favouritesOnly)
        {
            query = query.Where(r => r.IsFavourite);
        }
        if (mildOnly)
        {
            query = query.Where(r => !r.IsSpicy);
        }
        query = sort switch
        {
            LibrarySort.Title => query.OrderBy(r => r.Title),
            LibrarySort.Quickest => query.OrderBy(r => r.TimeMaxMinutes ?? int.MaxValue).ThenBy(r => r.Title),
            LibrarySort.MostCooked => query.OrderByDescending(r => r.TimesCooked).ThenBy(r => r.Title),
            _ => query.OrderByDescending(r => r.ImportedAt)
        };
        return await query.Select(r => new RecipeSummary { Id = r.Id, Title = r.Title, Subtitle = r.Subtitle, Tags = r.Tags, IsSpicy = r.IsSpicy, TimeMaxMinutes = r.TimeMaxMinutes, IsFavourite = r.IsFavourite, TimesCooked = r.TimesCooked, HasWarnings = r.HasWarnings, HasHero = r.Images.Any(i => i.Key == "hero"), Version = r.UpdatedAt.Ticks }).ToListAsync();
    }

    /// <summary>
    /// Returns a stored recipe card, or null if it doesn't exist.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<RecipeRecord?> GetAsync(int id)
    {
        return await db.Recipes.FindAsync(id);
    }

    /// <summary>
    /// Returns the parsed recipe stored in a record.
    /// </summary>
    /// <param name="record">The stored recipe card.</param>
    public static Recipe Deserialize(RecipeRecord record)
    {
        return JsonSerializer.Deserialize<Recipe>(record.RecipeJson) ?? new Recipe();
    }

    /// <summary>
    /// Saves a recipe corrected by hand, clearing the parser's warnings.
    /// </summary>
    /// <param name="record">The stored recipe card.</param>
    /// <param name="recipe">The corrected recipe.</param>
    public async Task SaveEditAsync(RecipeRecord record, Recipe recipe)
    {
        recipe.ParseWarnings = [];
        ApplyRecipe(record, recipe);
        record.IsEdited = true;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Returns the Paperless document IDs already imported.
    /// </summary>
    public async Task<HashSet<int>> GetPaperlessIdsAsync()
    {
        List<int> ids = await db.Recipes.Where(r => r.PaperlessId != null).Select(r => r.PaperlessId!.Value).ToListAsync();
        return [.. ids];
    }

    /// <summary>
    /// Toggles whether a recipe is a favourite.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task ToggleFavouriteAsync(int id)
    {
        await db.Recipes.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsFavourite, r => !r.IsFavourite));
    }

    /// <summary>
    /// Saves personal notes for a recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="notes">The notes, or null to clear them.</param>
    public async Task SaveNotesAsync(int id, string? notes)
    {
        string? value = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await db.Recipes.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Notes, value));
    }

    /// <summary>
    /// Records that a recipe has been cooked.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task MarkCookedAsync(int id)
    {
        DateTime now = DateTime.UtcNow;
        await db.Recipes.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.TimesCooked, r => r.TimesCooked + 1).SetProperty(r => r.LastCookedAt, now));
    }

    /// <summary>
    /// Deletes a recipe and its images.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task DeleteAsync(int id)
    {
        await db.Images.Where(i => i.RecipeRecordId == id).ExecuteDeleteAsync();
        await db.Recipes.Where(r => r.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Finds a previously imported copy of a recipe, by Paperless ID, product code, or else title and week.
    /// </summary>
    /// <param name="recipe">The newly parsed recipe.</param>
    /// <param name="paperlessId">The Paperless document ID, if any.</param>
    private async Task<RecipeRecord?> FindExistingAsync(Recipe recipe, int? paperlessId)
    {
        IQueryable<RecipeRecord> query = db.Recipes.Include(r => r.Images);
        RecipeRecord? fromPaperless = paperlessId.HasValue ? await query.FirstOrDefaultAsync(r => r.PaperlessId == paperlessId) : null;
        if (fromPaperless != null)
        {
            return fromPaperless;
        }
        if (recipe.ProductCode != null)
        {
            return await query.FirstOrDefaultAsync(r => r.ProductCode == recipe.ProductCode);
        }
        return await query.FirstOrDefaultAsync(r => r.Title == recipe.Title && r.WeekCode == recipe.WeekCode);
    }

    /// <summary>
    /// Copies a parse result onto a record, updating images in place.
    /// </summary>
    /// <param name="record">The record to update.</param>
    /// <param name="result">The parse result.</param>
    /// <param name="fileName">The source file name.</param>
    /// <param name="pdf">The PDF bytes.</param>
    private static void Apply(RecipeRecord record, ParseResult result, string fileName, byte[] pdf)
    {
        ApplyRecipe(record, result.Recipe);
        record.SourceFileName = fileName;
        record.Pdf = pdf;
        record.UpdatedAt = DateTime.UtcNow;
        HashSet<string> keys = [.. result.Images.Select(i => i.Key)];
        record.Images.RemoveAll(i => !keys.Contains(i.Key));
        foreach (RecipeImage image in result.Images)
        {
            RecipeImageRecord? existing = record.Images.FirstOrDefault(i => i.Key == image.Key);
            if (existing == null)
            {
                record.Images.Add(new RecipeImageRecord { Key = image.Key, ContentType = image.ContentType, Data = image.Data });
            }
            else
            {
                existing.ContentType = image.ContentType;
                existing.Data = image.Data;
            }
        }
    }

    /// <summary>
    /// Copies a recipe's searchable details and JSON onto a record.
    /// </summary>
    /// <param name="record">The record to update.</param>
    /// <param name="recipe">The recipe.</param>
    private static void ApplyRecipe(RecipeRecord record, Recipe recipe)
    {
        record.ProductCode = recipe.ProductCode;
        record.Number = recipe.Number;
        record.Title = recipe.Title;
        record.Subtitle = recipe.Subtitle;
        record.WeekCode = recipe.WeekCode;
        record.Tags = string.Join("|", recipe.Tags);
        record.IsSpicy = recipe.SpiceLevel != null;
        record.TimeMaxMinutes = recipe.TimeMaxMinutes;
        record.SearchText = string.Join(" ", [recipe.Title, recipe.Subtitle, .. recipe.Tags, .. recipe.Ingredients.Select(i => i.Name)]).ToLowerInvariant();
        record.RecipeJson = JsonSerializer.Serialize(recipe);
        record.HasWarnings = recipe.ParseWarnings.Count > 0;
    }
}