using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Services;

/// <summary>
/// Maps a recipe to and from the edit form, keeping the details the form doesn't show.
/// </summary>
public static class RecipeEditMapper
{
    private const int BlankIngredientRows = 3;

    /// <summary>
    /// Fills the edit form from a recipe, with a few blank ingredient rows for additions.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    public static RecipeEditForm ToForm(Recipe recipe)
    {
        List<int> sizes = recipe.ServingSizes;
        List<IngredientEditRow> ingredients = [.. recipe.Ingredients.Select(i => new IngredientEditRow { Name = i.Name, Unit = i.Unit, IsPantry = i.IsPantry, Quantities = [.. sizes.Select(s => i.Quantities.TryGetValue(s, out decimal q) ? q : (decimal?)null)] })];
        for (int i = 0; i < BlankIngredientRows; i++)
        {
            ingredients.Add(new IngredientEditRow { Quantities = [.. sizes.Select(_ => (decimal?)null)] });
        }
        return new RecipeEditForm
        {
            Title = recipe.Title,
            Subtitle = recipe.Subtitle,
            TimeMinMinutes = recipe.TimeMinMinutes,
            TimeMaxMinutes = recipe.TimeMaxMinutes,
            Tags = string.Join(", ", recipe.Tags),
            SpiceLevel = recipe.SpiceLevel,
            Tools = string.Join(", ", recipe.Tools),
            Ingredients = ingredients,
            Steps = [.. recipe.Steps.Select(s => new StepEditRow { Title = s.Title, Text = StepTextFormat.ToText(s) })]
        };
    }

    /// <summary>
    /// Builds the corrected recipe from the form, keeping images, allergens, nutrition and hot markings from the original.
    /// </summary>
    /// <param name="form">The submitted form.</param>
    /// <param name="original">The recipe before editing.</param>
    public static Recipe FromForm(RecipeEditForm form, Recipe original)
    {
        List<int> sizes = original.ServingSizes;
        List<Ingredient> ingredients = [];
        foreach ((IngredientEditRow row, int index) in form.Ingredients.Select((r, i) => (r, i)).Where(p => !string.IsNullOrWhiteSpace(p.r.Name)))
        {
            string name = row.Name!.Trim();
            Ingredient? before = original.Ingredients.FirstOrDefault(i => i.Name == name) ?? original.Ingredients.ElementAtOrDefault(index);
            Ingredient ingredient = new() { Name = name, Unit = row.Unit?.Trim() ?? string.Empty, IsPantry = row.IsPantry, NotIncluded = row.IsPantry && (before?.NotIncluded ?? true), StoreInFridge = before?.StoreInFridge ?? false, IsHot = before?.IsHot ?? false, AllergenCodes = before?.AllergenCodes ?? [], ImageKey = before?.ImageKey };
            for (int s = 0; s < sizes.Count && s < row.Quantities.Count; s++)
            {
                if (row.Quantities[s].HasValue)
                {
                    ingredient.Quantities[sizes[s]] = row.Quantities[s]!.Value;
                }
            }
            ingredients.Add(ingredient);
        }
        List<RecipeStep> steps = [];
        foreach ((StepEditRow row, int index) in form.Steps.Select((r, i) => (r, i)).Where(p => !string.IsNullOrWhiteSpace(p.r.Title) || !string.IsNullOrWhiteSpace(p.r.Text)))
        {
            RecipeStep? before = original.Steps.ElementAtOrDefault(index);
            steps.Add(new RecipeStep { Number = steps.Count + 1, Title = row.Title?.Trim() ?? string.Empty, SubSteps = StepTextFormat.FromText(row.Text ?? string.Empty, before), ImageKey = before?.ImageKey });
        }
        return new Recipe
        {
            Number = original.Number,
            ProductCode = original.ProductCode,
            WeekCode = original.WeekCode,
            Title = form.Title.Trim(),
            Subtitle = form.Subtitle?.Trim() ?? string.Empty,
            Tags = SplitList(form.Tags),
            SpiceLevel = string.IsNullOrWhiteSpace(form.SpiceLevel) ? null : form.SpiceLevel.Trim(),
            TimeMinMinutes = form.TimeMinMinutes,
            TimeMaxMinutes = form.TimeMaxMinutes ?? form.TimeMinMinutes,
            Extras = original.Extras,
            ServingSizes = sizes,
            Tools = SplitList(form.Tools),
            Ingredients = ingredients,
            ImpliedPantry = original.ImpliedPantry,
            Steps = steps,
            Nutrition = original.Nutrition,
            NutritionNote = original.NutritionNote,
            Allergens = original.Allergens,
            IsScan = original.IsScan,
            HeroImageKey = original.HeroImageKey
        };
    }

    /// <summary>
    /// Splits a comma-separated list, dropping blanks.
    /// </summary>
    /// <param name="text">The list text.</param>
    private static List<string> SplitList(string? text)
    {
        return [.. (text ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
    }
}