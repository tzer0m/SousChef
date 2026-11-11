using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SousChef.Core.Cooking;
using SousChef.Core.Models;
using SousChef.Data;
using SousChef.Services;

namespace SousChef.Pages;

/// <summary>
/// A recipe scaled to a number of people, with ingredients, method, nutrition and notes.
/// </summary>
/// <param name="library">The recipe library.</param>
public class RecipeModel(RecipeLibrary library) : PageModel
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
    /// The ingredients that come in the box.
    /// </summary>
    public List<IngredientLine> BoxIngredients => [.. Ingredients.Where(i => !i.Ingredient.IsPantry)];

    /// <summary>
    /// The pantry ingredients the card says to use.
    /// </summary>
    public List<IngredientLine> PantryIngredients => [.. Ingredients.Where(i => i.Ingredient.IsPantry)];

    /// <summary>
    /// How the quantities were worked out when the card doesn't have this serving size.
    /// </summary>
    public string ScalingNote { get; set; } = string.Empty;

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
        ScalingNote = ServingScaler.Describe(Recipe.ServingSizes, Servings);
        return Page();
    }

    /// <summary>
    /// Toggles the recipe's favourite flag.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnPostFavouriteAsync(int id)
    {
        await library.ToggleFavouriteAsync(id);
        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Saves personal notes.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="notes">The notes.</param>
    public async Task<IActionResult> OnPostNotesAsync(int id, string? notes)
    {
        await library.SaveNotesAsync(id, notes);
        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Deletes the recipe.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await library.DeleteAsync(id);
        return RedirectToPage("/Index");
    }
}