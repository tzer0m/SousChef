using System.Globalization;
using System.Text.RegularExpressions;

namespace SousChef.Core.Parsing;

/// <summary>
/// Text helpers and patterns shared by the card readers.
/// </summary>
internal static partial class CardText
{
    /// <summary>
    /// Parses a quantity such as "22.5", "½", "1/2", or OCR'd "05" (meaning 0.5); returns null if it isn't one.
    /// </summary>
    /// <param name="text">The quantity text.</param>
    public static decimal? ParseQuantity(string text)
    {
        string value = text.Trim().Replace(',', '.');
        if (!QuantityRegex().IsMatch(value))
        {
            return null;
        }
        decimal fraction = value[^1] switch
        {
            '¼' => 0.25m,
            '½' => 0.5m,
            '¾' => 0.75m,
            _ => -1
        };
        if (fraction >= 0)
        {
            return value.Length > 1 && decimal.TryParse(value[..^1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal whole) ? whole + fraction : fraction;
        }
        if (value.Contains('/'))
        {
            string[] parts = value.Split('/');
            return decimal.TryParse(parts[0], CultureInfo.InvariantCulture, out decimal top) && decimal.TryParse(parts[1], CultureInfo.InvariantCulture, out decimal bottom) && bottom != 0 ? top / bottom : null;
        }
        if (value.Length >= 2 && value[0] == '0' && !value.Contains('.'))
        {
            value = "0." + value[1..];
        }
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) ? result : null;
    }

    /// <summary>
    /// Parses a table cell that may carry its unit, such as "150g", "2sachets" or OCR'd "ltsp" (1 tsp).
    /// </summary>
    /// <param name="text">The cell text.</param>
    /// <param name="quantity">The quantity, if parsed.</param>
    /// <param name="unit">The unit in card form (e.g. "sachet(s)"), or empty if the cell has none.</param>
    public static bool TryParseCell(string text, out decimal quantity, out string unit)
    {
        quantity = 0;
        unit = string.Empty;
        string value = text.Trim().TrimEnd('.', ',');
        value = LeadingOneRegex().Replace(value, "1");
        value = value.Replace("%", "½");
        Match match = CellRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }
        decimal? parsed = ParseQuantity(match.Groups["number"].Value);
        if (!parsed.HasValue)
        {
            return false;
        }
        quantity = parsed.Value;
        unit = NormaliseUnit(match.Groups["unit"].Value);
        return true;
    }

    /// <summary>
    /// Puts a unit written in a cell into the card's table form, so "sachets" becomes "sachet(s)".
    /// </summary>
    /// <param name="unit">The unit as written.</param>
    public static string NormaliseUnit(string unit)
    {
        string lower = unit.Trim().ToLowerInvariant();
        string stem = lower.EndsWith('s') && lower.Length > 4 ? lower[..^1] : lower;
        return stem switch
        {
            "" => string.Empty,
            "sachet" or "pot" or "pack" or "tin" or "bag" or "punnet" or "pouch" or "jar" => $"{stem}(s)",
            "mi" or "mil" => "ml",
            _ => lower
        };
    }

    /// <summary>
    /// Removes one pair of surrounding parentheses, so "(unit(s))" becomes "unit(s)".
    /// </summary>
    /// <param name="text">The text to strip.</param>
    public static string StripParentheses(string text)
    {
        string trimmed = text.Trim();
        return trimmed.StartsWith('(') && trimmed.EndsWith(')') ? trimmed[1..^1].Trim() : trimmed;
    }

    /// <summary>
    /// Lower-cases text and removes everything but letters, for loose name comparison.
    /// </summary>
    /// <param name="text">The text to normalise.</param>
    public static string Normalise(string text)
    {
        return new string([.. text.ToLowerInvariant().Where(char.IsLetter)]);
    }

    /// <summary>
    /// Joins a line's words into text.
    /// </summary>
    /// <param name="line">The words of one line.</param>
    public static string LineText(List<PdfWord> line)
    {
        return string.Join(" ", line.Select(w => w.Text));
    }

    /// <summary>
    /// Matches a quantity, including fractions and OCR'd commas.
    /// </summary>
    [GeneratedRegex(@"^(\d+(\.\d+)?|\d*[¼½¾]|\d+/\d+)$")]
    public static partial Regex QuantityRegex();

    /// <summary>
    /// Matches a table cell: a quantity optionally followed by a unit, such as "150g" or "1½".
    /// </summary>
    [GeneratedRegex(@"^(?<number>\d+(?:[.,]\d+)?|\d*[¼½¾]|\d+/\d+)(?<unit>[A-Za-z]{1,8})?$")]
    public static partial Regex CellRegex();

    /// <summary>
    /// Matches an OCR'd "1" read as "l" or "I" before a unit, such as "lsachet".
    /// </summary>
    [GeneratedRegex(@"^[lI](?=[A-Za-z]{2,}$|$)")]
    public static partial Regex LeadingOneRegex();

    /// <summary>
    /// Matches a serving-size column header such as "2P" (OCR sometimes reads "4p").
    /// </summary>
    [GeneratedRegex(@"^\d+[Pp]$")]
    public static partial Regex ServingHeaderRegex();

    /// <summary>
    /// Matches an allergen code such as "13)".
    /// </summary>
    [GeneratedRegex(@"(\d+)\)")]
    public static partial Regex AllergenCodeRegex();

    /// <summary>
    /// Matches one entry of the allergen legend, such as "13) Cereals containing gluten".
    /// </summary>
    [GeneratedRegex(@"(?<code>\d+)\)\s*(?<name>.+?)(?=\s+\d+\)|$)")]
    public static partial Regex AllergenEntryRegex();

    /// <summary>
    /// Matches a unit in parentheses at the end of an ingredient name, such as "(g)" or "(unit(s))".
    /// </summary>
    [GeneratedRegex(@"\((?<unit>[A-Za-z]+(?:\(s\))?)\)")]
    public static partial Regex UnitRegex();

    /// <summary>
    /// Matches a unit in parentheses at the end of a label, such as "(kJ/kcal)".
    /// </summary>
    [GeneratedRegex(@"\((?<unit>[^()]+)\)$")]
    public static partial Regex TrailingParenthesesRegex();

    /// <summary>
    /// Matches the first line of the nutrition table, for cards where the ingredient table has no footnote.
    /// </summary>
    [GeneratedRegex(@"^(Energy|Typical\s+Values|Per\s+serving|Nutrition)\b", RegexOptions.IgnoreCase)]
    public static partial Regex NutritionStartRegex();

    /// <summary>
    /// Matches words that only appear in the nutrition table, to stop the ingredient table there.
    /// </summary>
    [GeneratedRegex(@"\b(serving|uncooked|kJ|kcal|Energy|Typical)\b", RegexOptions.IgnoreCase)]
    public static partial Regex NutritionWordRegex();

    /// <summary>
    /// Matches a nutrition value such as "14.9", "400g" or "2467/590".
    /// </summary>
    [GeneratedRegex(@"^[\d.,/]+[a-zA-Z]*$")]
    public static partial Regex NutritionValueRegex();

    /// <summary>
    /// Splits a tools list on commas and "and".
    /// </summary>
    [GeneratedRegex(@",|\s+and\s+")]
    public static partial Regex ToolSeparatorRegex();

    /// <summary>
    /// Matches a line that lists kitchen equipment.
    /// </summary>
    [GeneratedRegex(@"\b(Pans?|Saucepans?|Bowls?|Dish|Tray|Sieve|Colander|Grater|Kettle|Press|Lids?|Peeler|Jug|Pin)\b", RegexOptions.IgnoreCase)]
    public static partial Regex ToolKeywordRegex();

    /// <summary>
    /// Matches a sub-step letter such as "a)".
    /// </summary>
    [GeneratedRegex(@"^[a-h]\)$")]
    public static partial Regex SubStepLetterRegex();

    /// <summary>
    /// Matches the menu week code in the footer, such as "2026-W46".
    /// </summary>
    [GeneratedRegex(@"^\d{4}-W\d{1,2}$")]
    public static partial Regex WeekCodeRegex();

    /// <summary>
    /// Matches the total cooking time, such as "20 - 25 Minutes" (OCR may garble everything after "Mi").
    /// </summary>
    [GeneratedRegex(@"(?<min>\d+)(?:\s*(?:-|–|to)\s*(?<max>\d+))?\s*Mi", RegexOptions.IgnoreCase)]
    public static partial Regex TotalTimeRegex();

    /// <summary>
    /// Matches a HelloFresh product code in a file name, such as "QR3208-79".
    /// </summary>
    [GeneratedRegex(@"[A-Z]{2}\d{3,6}-\d+")]
    public static partial Regex ProductCodeRegex();

    /// <summary>
    /// Matches a Paperless-style file name such as "2026-11-11 HelloFresh 1 Quick Butter Chicken Masala".
    /// </summary>
    [GeneratedRegex(@"HelloFresh\s+(?<number>\d+)\s+(?<title>.+?)(?:\s+\d+)?(?:_[\dP_]+)?(?:_web)?$", RegexOptions.IgnoreCase)]
    public static partial Regex FileTitleRegex();

    /// <summary>
    /// Matches runs of whitespace.
    /// </summary>
    [GeneratedRegex(@"\s+")]
    public static partial Regex WhitespaceRegex();

    /// <summary>
    /// Matches a word made of letters.
    /// </summary>
    [GeneratedRegex(@"[A-Za-z]+")]
    public static partial Regex WordRegex();
}