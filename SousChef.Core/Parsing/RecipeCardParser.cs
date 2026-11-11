using System.Globalization;
using System.Text.RegularExpressions;
using SousChef.Core.Cooking;
using SousChef.Core.Models;
using UglyToad.PdfPig;

namespace SousChef.Core.Parsing;

/// <summary>
/// Parses HelloFresh recipe card PDFs, either downloaded or scanned with an OCR text layer.
/// </summary>
public class RecipeCardParser
{
    private static readonly string[] ImpliedPantryWords = ["oil", "salt", "pepper", "water", "butter", "sugar", "flour", "vinegar"];

    /// <summary>
    /// Parses a recipe card.
    /// </summary>
    /// <param name="pdf">The PDF bytes.</param>
    /// <param name="fileName">The original file name or document title, used for the product code, number and title.</param>
    public ParseResult Parse(byte[] pdf, string? fileName = null)
    {
        ParseResult result = new();
        Recipe recipe = result.Recipe;
        string name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        Match code = CardText.ProductCodeRegex().Match(name);
        if (code.Success)
        {
            recipe.ProductCode = code.Value;
        }
        using PdfDocument document = PdfDocument.Open(pdf);
        if (document.NumberOfPages < 2)
        {
            recipe.ParseWarnings.Add($"Expected 2 pages but found {document.NumberOfPages}.");
        }
        if (IsDownloadedCard(document))
        {
            DigitalCardReader.Read(document, result);
        }
        else
        {
            recipe.IsScan = true;
            ScannedCardReader.Read(document, result);
        }
        ApplyFileName(name, recipe);
        FinishRecipe(recipe);
        return result;
    }

    /// <summary>
    /// Reads a total time such as "20 - 25 Minutes" into the recipe, returning whether the text was a time.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    internal static bool TryReadTime(string text, Recipe recipe)
    {
        Match time = CardText.TotalTimeRegex().Match(text);
        if (!time.Success)
        {
            return false;
        }
        int min = int.Parse(time.Groups["min"].Value, CultureInfo.InvariantCulture);
        int max = time.Groups["max"].Success ? int.Parse(time.Groups["max"].Value, CultureInfo.InvariantCulture) : min;
        recipe.TimeMinMinutes = min < 5 || min > max ? max : min;
        recipe.TimeMaxMinutes = max;
        return true;
    }

    /// <summary>
    /// Checks for the card's own font, which only downloaded cards have.
    /// </summary>
    /// <param name="document">The PDF document.</param>
    private static bool IsDownloadedCard(PdfDocument document)
    {
        return document.GetPages().Any(p => p.Letters.Any(l => l.FontName != null && l.FontName.Contains("Agrandir", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Uses a Paperless-style name ("2026-11-11 HelloFresh 1 Quick Butter Chicken Masala") for the number, and for the title of scans.
    /// </summary>
    /// <param name="name">The file name without extension.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ApplyFileName(string name, Recipe recipe)
    {
        Match match = CardText.FileTitleRegex().Match(name);
        if (!match.Success)
        {
            return;
        }
        recipe.Number ??= int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
        string title = match.Groups["title"].Value.Trim();
        if (recipe.IsScan || recipe.Title.Length == 0)
        {
            recipe.Title = title;
        }
    }

    /// <summary>
    /// Works out hot ingredients and implied pantry items, and checks the essentials were found.
    /// </summary>
    /// <param name="recipe">The parsed recipe.</param>
    private static void FinishRecipe(Recipe recipe)
    {
        List<SubStep> subSteps = [.. recipe.Steps.SelectMany(s => s.SubSteps)];
        foreach (SubStep subStep in subSteps)
        {
            foreach (TextRange range in subStep.HotRanges.Where(r => r.End <= subStep.Text.Length))
            {
                foreach (IngredientMention mention in IngredientMatcher.FindMentions(subStep.Text.Substring(range.Start, range.Length), recipe.Ingredients))
                {
                    recipe.Ingredients[mention.IngredientIndex].IsHot = true;
                }
            }
        }
        HashSet<string> methodWords = [.. subSteps.SelectMany(s => CardText.WordRegex().Matches(s.Text)).Select(m => m.Value.ToLowerInvariant())];
        HashSet<string> ingredientWords = [.. recipe.Ingredients.SelectMany(i => CardText.WordRegex().Matches(i.Name)).Select(m => m.Value.ToLowerInvariant())];
        recipe.ImpliedPantry = [.. ImpliedPantryWords.Where(w => methodWords.Contains(w) && !ingredientWords.Contains(w)).Select(w => char.ToUpperInvariant(w[0]) + w[1..])];
        if (recipe.Title.Length == 0)
        {
            recipe.ParseWarnings.Add("No title found.");
        }
        if (recipe.Ingredients.Count == 0)
        {
            recipe.ParseWarnings.Add("No ingredients found.");
        }
        if (recipe.Steps.Count == 0)
        {
            recipe.ParseWarnings.Add("No steps found.");
        }
        foreach (RecipeStep step in recipe.Steps.Where(s => s.SubSteps.Count == 0))
        {
            recipe.ParseWarnings.Add($"Step {step.Number} has no instructions.");
        }
    }
}