using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Colors;

namespace SousChef.Core.Parsing;

/// <summary>
/// A word on a PDF page with the layout details the parser needs.
/// </summary>
internal class PdfWord
{
    /// <summary>
    /// The word text, with lost ligatures restored.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// The letters that aren't grey (grey letters are units).
    /// </summary>
    public string MainText { get; init; } = string.Empty;

    /// <summary>
    /// The grey letters, such as "(g)".
    /// </summary>
    public string GreyText { get; init; } = string.Empty;

    /// <summary>
    /// The X position of the start of the word's baseline.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// The X position of the end of the word's baseline.
    /// </summary>
    public double Right { get; init; }

    /// <summary>
    /// The baseline Y position (PDF coordinates, so larger is higher up).
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// The font size in points.
    /// </summary>
    public double Size { get; init; }

    /// <summary>
    /// The colour of the first letter.
    /// </summary>
    public TextColour Colour { get; init; }

    /// <summary>
    /// Whether the word uses the card's heading font (Agrandir).
    /// </summary>
    public bool IsHeadingFont { get; init; }

    /// <summary>
    /// Whether the word uses the regular (not medium) weight of the heading font.
    /// </summary>
    public bool IsRegularWeight { get; init; }

    /// <summary>
    /// The horizontal centre of the word.
    /// </summary>
    public double CentreX => (X + Right) / 2;

    /// <summary>
    /// Reads all non-empty words from a page.
    /// </summary>
    /// <param name="page">The page to read.</param>
    public static List<PdfWord> FromPage(Page page)
    {
        return [.. page.GetWords().Where(w => w.Letters.Count > 0 && !string.IsNullOrWhiteSpace(w.Text)).Select(FromWord)];
    }

    /// <summary>
    /// Converts a PdfPig word.
    /// </summary>
    /// <param name="word">The PdfPig word.</param>
    public static PdfWord FromWord(Word word)
    {
        StringBuilder main = new();
        StringBuilder grey = new();
        foreach (Letter letter in word.Letters)
        {
            string value = TextCleaner.Clean(letter.Value);
            if (Classify(letter.Color) == TextColour.Grey)
            {
                grey.Append(value);
            }
            else
            {
                main.Append(value);
            }
        }
        Letter first = word.Letters[0];
        Letter last = word.Letters[^1];
        string fontName = first.FontName ?? string.Empty;
        return new PdfWord
        {
            Text = TextCleaner.Clean(word.Text),
            MainText = main.ToString(),
            GreyText = grey.ToString(),
            X = first.StartBaseLine.X,
            Right = last.EndBaseLine.X,
            Y = first.StartBaseLine.Y,
            Size = first.PointSize,
            Colour = Classify(first.Color),
            IsHeadingFont = fontName.Contains("Agrandir", StringComparison.OrdinalIgnoreCase),
            IsRegularWeight = fontName.Contains("Regular", StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Maps an RGB colour to one of the card's meaningful colours.
    /// </summary>
    /// <param name="colour">The PDF colour.</param>
    private static TextColour Classify(IColor? colour)
    {
        if (colour == null)
        {
            return TextColour.Black;
        }
        (double r, double g, double b) = colour.ToRGBValues();
        if (r > 0.9 && g > 0.9 && b > 0.9)
        {
            return TextColour.White;
        }
        if (r > 0.6 && g < 0.4 && b < 0.4)
        {
            return TextColour.Red;
        }
        if (g > 0.5 && r < 0.4 && b < 0.6)
        {
            return TextColour.Green;
        }
        if (Math.Abs(r - g) < 0.05 && Math.Abs(g - b) < 0.05 && r >= 0.4 && r <= 0.7)
        {
            return TextColour.Grey;
        }
        if (r < 0.3 && g < 0.3 && b < 0.3)
        {
            return TextColour.Black;
        }
        return TextColour.Other;
    }
}