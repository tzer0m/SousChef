using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SousChef.Core.Models;
using SousChef.Data;
using SousChef.Services;

namespace SousChef.Pages;

/// <summary>
/// Corrects a parsed recipe by hand, mainly for OCR slips on scanned cards.
/// </summary>
/// <param name="library">The recipe library.</param>
public class EditModel(RecipeLibrary library) : PageModel
{
    /// <summary>
    /// The form fields.
    /// </summary>
    [BindProperty]
    public RecipeEditForm Form { get; set; } = new();

    /// <summary>
    /// The stored recipe card.
    /// </summary>
    public RecipeRecord Record { get; set; } = new();

    /// <summary>
    /// The recipe as currently stored.
    /// </summary>
    public Recipe Recipe { get; set; } = new();

    /// <summary>
    /// Shows the form filled in from the stored recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
        {
            return NotFound();
        }
        Form = RecipeEditMapper.ToForm(Recipe);
        return Page();
    }

    /// <summary>
    /// Saves the corrections and returns to the recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id))
        {
            return NotFound();
        }
        if (string.IsNullOrWhiteSpace(Form.Title))
        {
            ModelState.AddModelError("Form.Title", "The recipe needs a title.");
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }
        await library.SaveEditAsync(Record, RecipeEditMapper.FromForm(Form, Recipe));
        return RedirectToPage("/Recipe", new { id });
    }

    /// <summary>
    /// Loads the stored recipe, returning false if it doesn't exist.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    private async Task<bool> LoadAsync(int id)
    {
        RecipeRecord? record = await library.GetAsync(id);
        if (record == null)
        {
            return false;
        }
        Record = record;
        Recipe = RecipeLibrary.Deserialize(record);
        return true;
    }
}