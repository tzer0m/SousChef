using System.Globalization;

namespace SousChef.Core.Cooking;

/// <summary>
/// Formats quantities for display, such as "1½ tbsp", "2 sachets" or "22.5 g".
/// </summary>
public static class QuantityFormatter
{
    private static readonly HashSet<string> MetricUnits = ["g", "kg", "ml", "l"];

    /// <summary>
    /// Formats a quantity with its unit.
    /// </summary>
    /// <param name="quantity">The quantity, or null if unknown.</param>
    /// <param name="unit">The unit as printed on the card.</param>
    public static string Format(decimal? quantity, string unit)
    {
        if (quantity == null)
        {
            return string.Empty;
        }
        string number = FormatNumber(quantity.Value, !MetricUnits.Contains(unit.Trim().ToLowerInvariant()));
        string displayUnit = FormatUnit(unit, quantity.Value);
        return displayUnit.Length == 0 ? number : $"{number} {displayUnit}";
    }

    /// <summary>
    /// Formats a quantity as a short amount for inline use, such as "15 g" or "×2" for plain counts.
    /// </summary>
    /// <param name="quantity">The quantity, or null if unknown.</param>
    /// <param name="unit">The unit as printed on the card.</param>
    public static string FormatAmount(decimal? quantity, string unit)
    {
        string formatted = Format(quantity, unit);
        return formatted.Length > 0 && FormatUnit(unit, quantity ?? 0).Length == 0 ? $"×{formatted}" : formatted;
    }

    /// <summary>
    /// Turns a card unit into a display unit, so "sachet(s)" becomes "sachet" or "sachets" and "unit(s)" disappears.
    /// </summary>
    /// <param name="unit">The unit as printed on the card.</param>
    /// <param name="quantity">The quantity, used for plurals.</param>
    public static string FormatUnit(string unit, decimal quantity)
    {
        string trimmed = unit.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith("unit", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }
        if (trimmed.EndsWith("(s)", StringComparison.Ordinal))
        {
            string stem = trimmed[..^3];
            return quantity > 1 ? stem + "s" : stem;
        }
        return trimmed;
    }

    /// <summary>
    /// Formats a number, using fraction characters for counts and spoons.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="useFractions">Whether to show quarters and halves as fractions.</param>
    private static string FormatNumber(decimal value, bool useFractions)
    {
        if (useFractions)
        {
            decimal whole = Math.Floor(value);
            string? fraction = (value - whole) switch
            {
                0.25m => "¼",
                0.5m => "½",
                0.75m => "¾",
                _ => null
            };
            if (fraction != null)
            {
                return whole > 0 ? $"{whole:0}{fraction}" : fraction;
            }
        }
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}