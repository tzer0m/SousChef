using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SousChef.Services;

namespace SousChef.Pages;

/// <summary>
/// The recipe library, with search, filters and sorting.
/// </summary>
/// <param name="library">The recipe library.</param>
public class IndexModel(RecipeLibrary library) : PageModel
{
    /// <summary>
    /// The search text.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>
    /// Whether to show only favourites.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Favourites { get; set; }

    /// <summary>
    /// Whether to hide spicy recipes.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Mild { get; set; }

    /// <summary>
    /// The sort order.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public LibrarySort Sort { get; set; }

    /// <summary>
    /// The recipes to show.
    /// </summary>
    public List<RecipeSummary> Recipes { get; set; } = [];

    /// <summary>
    /// Whether any filter is applied.
    /// </summary>
    public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || Favourites || Mild;

    /// <summary>
    /// Loads the recipes.
    /// </summary>
    public async Task OnGetAsync()
    {
        Recipes = await library.GetSummariesAsync(Search, Favourites, Mild, Sort);
    }

    /// <summary>
    /// Toggles a recipe's favourite flag and returns to the same view.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    public async Task<IActionResult> OnPostFavouriteAsync(int id)
    {
        await library.ToggleFavouriteAsync(id);
        return RedirectToPage(new { Search, Favourites, Mild, Sort });
    }
}