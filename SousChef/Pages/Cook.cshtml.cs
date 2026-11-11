using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SousChef.Core.Cooking;
using SousChef.Core.Models;
using SousChef.Data;
using SousChef.Services;

namespace SousChef.Pages;

/// <summary>
/// Step-by-step cook mode with a prep checklist and timers.
/// </summary>
/// <param name="library">The recipe library.</param>
public class CookModel(RecipeLibrary library) : PageModel
{
    /// <summary>
    /// The stored recipe card.
    /// </summary>
    public RecipeRecord Record { get; set; } = new();

    /// <summary>
    /// The parsed recipe.
    /// </summary>
    public Recipe Recipe { get; set; } = new();

    /// <summary>
    /// The number of people being cooked for.
    /// </summary>
    public int Servings { get; set; }

    /// <summary>
    /// The method with scaled amounts.
    /// </summary>
    public List<AnnotatedStep> Steps { get; set; } = [];

    /// <summary>
    /// The ingredients with scaled amounts.
    /// </summary>
    public List<IngredientLine> Ingredients { get; set; } = [];

    /// <summary>
    /// Loads and scales the recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="servings">The number of people, if chosen.</param>
    public async Task<IActionResult> OnGetAsync(int id, int? servings)
    {
        RecipeRecord? record = await library.GetAsync(id);
        if (record == null)
        {
            return NotFound();
        }
        Record = record;
        Recipe = RecipeLibrary.Deserialize(record);
        Servings = ServingsPreference.Resolve(servings, HttpContext, Recipe);
        Steps = new StepAnnotator(Recipe, Servings).AnnotateAll();
        Ingredients = IngredientLine.Build(Recipe, Servings);
        return Page();
    }

    /// <summary>
    /// Records that the recipe was cooked and returns to the recipe page.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnPostCookedAsync(int id)
    {
        await library.MarkCookedAsync(id);
        return RedirectToPage("/Recipe", new { id });
    }
}