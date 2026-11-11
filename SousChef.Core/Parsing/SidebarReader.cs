using System.Globalization;
using System.Text.RegularExpressions;
using SousChef.Core.Models;

namespace SousChef.Core.Parsing;

/// <summary>
/// Reads the method page's left-hand column (tools, ingredients, nutrition, allergens), finding headings by their text.
/// </summary>
internal static class SidebarReader
{
    /// <summary>
    /// Reads the sidebar into the recipe.
    /// </summary>
    /// <param name="words">The words in the left-hand column.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    public static void Read(List<PdfWord> words, Recipe recipe)
    {
        List<List<PdfWord>> lines = LineGrouper.Group(words);
        List<PdfWord> toolsHeading = FindHeading(lines, "Cooking Tools") ?? FindHeading(lines, "Tools") ?? [];
        List<PdfWord>? ingredientsHeading = FindHeading(lines, "Ingredients");
        List<PdfWord>? nutritionHeading = FindHeading(lines, "Nutrition");
        List<PdfWord>? allergensHeading = FindHeading(lines, "Allergens");
        double ingredientsY = ingredientsHeading?[0].Y ?? 0;
        IEnumerable<List<PdfWord>> toolLines = toolsHeading.Count > 0 ? lines.Where(l => l[0].Y < toolsHeading[0].Y - 1 && l[0].Y > ingredientsY + 1) : lines.Where(l => l[0].Y > ingredientsY + 1 && CardText.ToolKeywordRegex().IsMatch(CardText.LineText(l)));
        string tools = string.Join(" ", toolLines.Select(CardText.LineText));
        recipe.Tools = [.. CardText.ToolSeparatorRegex().Split(tools).Select(t => t.Trim(' ', '.', ':')).Where(t => t.Length > 1)];
        ReadIngredientTable(lines, nutritionHeading?[0].Y, recipe);
        if (nutritionHeading != null)
        {
            ReadNutrition(lines, nutritionHeading[0].Y, allergensHeading?[0].Y ?? 0, recipe);
        }
        if (allergensHeading != null)
        {
            ReadAllergens(lines, allergensHeading[0].Y, recipe);
        }
    }

    /// <summary>
    /// Finds a heading line: one that is the heading text, or starts with it and has at most three words.
    /// </summary>
    /// <param name="lines">The sidebar lines.</param>
    /// <param name="heading">The heading text.</param>
    private static List<PdfWord>? FindHeading(List<List<PdfWord>> lines, string heading)
    {
        return lines.FirstOrDefault(l => l.Count <= 3 && CardText.LineText(l).StartsWith(heading, StringComparison.OrdinalIgnoreCase) && !l.Any(w => CardText.ServingHeaderRegex().IsMatch(w.Text)));
    }

    /// <summary>
    /// Reads the ingredient and pantry tables, using the column headers to tell quantities from names and allergen codes.
    /// </summary>
    /// <param name="lines">The sidebar lines.</param>
    /// <param name="nutritionY">The Y position of the nutrition heading, if found.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ReadIngredientTable(List<List<PdfWord>> lines, double? nutritionY, Recipe recipe)
    {
        List<List<PdfWord>> headers = [.. lines.Where(l => l.Count(w => CardText.ServingHeaderRegex().IsMatch(w.Text)) >= 2)];
        if (headers.Count == 0)
        {
            recipe.ParseWarnings.Add("No ingredient table header (2P, 3P, 4P) found.");
            return;
        }
        Dictionary<int, double> columns = [];
        foreach (PdfWord word in headers[0].Where(w => CardText.ServingHeaderRegex().IsMatch(w.Text)))
        {
            columns[int.Parse(word.Text[..^1], CultureInfo.InvariantCulture)] = word.CentreX;
        }
        recipe.ServingSizes = [.. columns.Keys.Order()];
        HashSet<PdfWord> headerWords = [.. headers.SelectMany(h => h)];
        double? pantryY = headers.Count > 1 ? headers[1][0].Y : null;
        double top = headers[0][0].Y;
        List<PdfWord>? footnote = lines.FirstOrDefault(l => l[0].Y < top && l[0].Text.StartsWith("*Not", StringComparison.OrdinalIgnoreCase));
        List<PdfWord>? nutritionStart = lines.FirstOrDefault(l => l[0].Y < top && CardText.NutritionStartRegex().IsMatch(CardText.LineText(l)));
        double?[] bottoms = [footnote?[0].Y, nutritionY, nutritionStart?[0].Y];
        double bottom = bottoms.Max() ?? 0;
        List<PdfWord> quantityWords = [];
        Dictionary<PdfWord, (int Servings, decimal Quantity, string Unit)> quantities = [];
        List<PdfWord> nameWords = [];
        foreach (PdfWord word in lines.SelectMany(l => l).Where(w => !headerWords.Contains(w) && w.Y < top - 1 && w.Y > bottom + 1))
        {
            KeyValuePair<int, double> column = columns.MinBy(c => Math.Abs(c.Value - word.CentreX));
            bool inColumn = Math.Abs(column.Value - word.CentreX) < 16;
            if (inColumn && CardText.TryParseCell(word.Text, out decimal quantity, out string cellUnit))
            {
                quantityWords.Add(word);
                quantities[word] = (column.Key, quantity, cellUnit);
            }
            else if (!inColumn || word.Text.Length > 4)
            {
                nameWords.Add(word);
            }
        }
        List<(double Y, Ingredient Ingredient, List<PdfWord> Names)> rows = [];
        foreach (List<PdfWord> line in LineGrouper.Group(quantityWords))
        {
            Ingredient ingredient = new();
            foreach (PdfWord word in line)
            {
                ingredient.Quantities[quantities[word].Servings] = quantities[word].Quantity;
            }
            ingredient.Unit = line.Select(w => quantities[w].Unit).Where(u => u.Length > 0).GroupBy(u => u).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? string.Empty;
            rows.Add((line.Average(w => w.Y), ingredient, []));
        }
        if (rows.Count == 0)
        {
            recipe.ParseWarnings.Add("No ingredient quantities found.");
            return;
        }
        foreach (PdfWord word in nameWords)
        {
            (double Y, Ingredient Ingredient, List<PdfWord> Names) row = rows.MinBy(r => Math.Abs(r.Y - word.Y));
            if (Math.Abs(row.Y - word.Y) <= 8)
            {
                row.Names.Add(word);
            }
        }
        foreach ((double y, Ingredient ingredient, List<PdfWord> names) in rows.OrderByDescending(r => r.Y))
        {
            List<PdfWord> ordered = [.. LineGrouper.Group(names).SelectMany(l => l)];
            string name = string.Join(" ", ordered.Select(w => w.MainText).Where(t => t.Length > 0));
            if (CardText.NutritionWordRegex().IsMatch(name))
            {
                break;
            }
            string unit = string.Concat(ordered.Select(w => w.GreyText)).Trim();
            if (unit.Length == 0)
            {
                Match unitMatch = CardText.UnitRegex().Matches(name).LastOrDefault() ?? Match.Empty;
                if (unitMatch.Success)
                {
                    unit = unitMatch.Groups["unit"].Value;
                    name = name.Remove(unitMatch.Index, unitMatch.Length);
                }
            }
            ingredient.AllergenCodes = [.. CardText.AllergenCodeRegex().Matches(name).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))];
            name = CardText.AllergenCodeRegex().Replace(name, string.Empty);
            ingredient.StoreInFridge = name.Contains("**", StringComparison.Ordinal);
            ingredient.NotIncluded = !ingredient.StoreInFridge && name.Contains('*', StringComparison.Ordinal);
            ingredient.Name = CardText.WhitespaceRegex().Replace(name.Replace("*", string.Empty), " ").Trim(' ', ',', '.');
            if (unit.Length > 0)
            {
                ingredient.Unit = CardText.StripParentheses(unit);
            }
            ingredient.IsPantry = pantryY.HasValue && y < pantryY.Value;
            if (ingredient.Name.Length == 0)
            {
                recipe.ParseWarnings.Add($"An ingredient row at y={y:F0} has no name.");
                continue;
            }
            List<int> missing = [.. recipe.ServingSizes.Where(s => !ingredient.Quantities.ContainsKey(s))];
            if (missing.Count > 0)
            {
                recipe.ParseWarnings.Add($"{ingredient.Name} has no quantity for {string.Join(", ", missing.Select(s => $"{s}P"))}.");
            }
            RestoreLostDecimals(ingredient);
            List<decimal> amounts = [.. ingredient.Quantities.OrderBy(q => q.Key).Select(q => q.Value)];
            if (amounts.Zip(amounts.Skip(1)).Any(p => p.Second < p.First))
            {
                recipe.ParseWarnings.Add($"{ingredient.Name} quantities look wrong ({string.Join(" / ", amounts.Select(q => q.ToString(CultureInfo.InvariantCulture)))}).");
            }
            recipe.Ingredients.Add(ingredient);
        }
    }

    /// <summary>
    /// Fixes quantities where OCR dropped the decimal point (e.g. 1 / 15 / 2 becomes 1 / 1.5 / 2), when dividing by ten makes the sizes increase in order.
    /// </summary>
    /// <param name="ingredient">The ingredient to fix.</param>
    private static void RestoreLostDecimals(Ingredient ingredient)
    {
        List<int> sizes = [.. ingredient.Quantities.Keys.Order()];
        for (int i = 0; i < sizes.Count; i++)
        {
            decimal value = ingredient.Quantities[sizes[i]];
            decimal? previous = i > 0 ? ingredient.Quantities[sizes[i - 1]] : null;
            decimal? next = i + 1 < sizes.Count ? ingredient.Quantities[sizes[i + 1]] : null;
            bool tooBig = (next.HasValue && value > next.Value) || (previous.HasValue && !next.HasValue && value > previous.Value * 4);
            decimal shrunk = value / 10;
            if (tooBig && value >= 10 && (!previous.HasValue || shrunk >= previous.Value) && (!next.HasValue || shrunk <= next.Value))
            {
                ingredient.Quantities[sizes[i]] = shrunk;
            }
        }
    }

    /// <summary>
    /// Reads the nutrition table rows and the note under it.
    /// </summary>
    /// <param name="lines">The sidebar lines.</param>
    /// <param name="top">The Y position of the nutrition heading.</param>
    /// <param name="bottom">The Y position of the next heading.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ReadNutrition(List<List<PdfWord>> lines, double top, double bottom, Recipe recipe)
    {
        List<string> notes = [];
        foreach (List<PdfWord> line in lines.Where(l => l[0].Y < top - 1 && l[0].Y > bottom + 1))
        {
            string text = CardText.LineText(line);
            if (text.Contains("serving", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (line.Count >= 3 && CardText.NutritionValueRegex().IsMatch(line[^1].Text) && CardText.NutritionValueRegex().IsMatch(line[^2].Text) && !CardText.NutritionValueRegex().IsMatch(line[0].Text))
            {
                List<PdfWord> label = [.. line.Take(line.Count - 2)];
                string name = string.Join(" ", label.Select(w => w.MainText).Where(t => t.Length > 0));
                string unit = CardText.StripParentheses(string.Concat(label.Select(w => w.GreyText)));
                if (unit.Length == 0)
                {
                    Match unitMatch = CardText.TrailingParenthesesRegex().Match(name);
                    if (unitMatch.Success)
                    {
                        unit = unitMatch.Groups["unit"].Value;
                        name = name.Remove(unitMatch.Index).Trim();
                    }
                }
                if (name.StartsWith("for uncooked", StringComparison.OrdinalIgnoreCase))
                {
                    name = "Weight";
                }
                recipe.Nutrition.Add(new NutritionValue { Name = name, Unit = unit, PerServing = line[^2].Text, Per100g = line[^1].Text });
            }
            else
            {
                notes.Add(text);
            }
        }
        recipe.NutritionNote = notes.Count > 0 ? string.Join(" ", notes) : null;
    }

    /// <summary>
    /// Reads the allergen legend, such as "11) Soya 13) Cereals containing gluten".
    /// </summary>
    /// <param name="lines">The sidebar lines.</param>
    /// <param name="top">The Y position of the allergens heading.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ReadAllergens(List<List<PdfWord>> lines, double top, Recipe recipe)
    {
        List<string> legend = [.. lines.Where(l => l[0].Y < top - 1).Take(3).Select(CardText.LineText).TakeWhile(t => !t.StartsWith("Please", StringComparison.OrdinalIgnoreCase) && CardText.AllergenCodeRegex().IsMatch(t))];
        foreach (Match match in CardText.AllergenEntryRegex().Matches(string.Join(" ", legend)))
        {
            recipe.Allergens.Add(new Allergen { Code = int.Parse(match.Groups["code"].Value, CultureInfo.InvariantCulture), Name = match.Groups["name"].Value.Trim(' ', ',', '.') });
        }
    }
}