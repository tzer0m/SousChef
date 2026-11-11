namespace SousChef.Core.Parsing;

/// <summary>
/// Groups words into lines of text by their baseline.
/// </summary>
internal static class LineGrouper
{
    /// <summary>
    /// Groups words into lines, top to bottom, each sorted left to right.
    /// </summary>
    /// <param name="words">The words to group.</param>
    /// <param name="tolerance">How far apart two baselines can be and still count as one line.</param>
    public static List<List<PdfWord>> Group(IEnumerable<PdfWord> words, double tolerance = 3)
    {
        List<List<PdfWord>> lines = [];
        List<PdfWord>? current = null;
        double lineY = 0;
        foreach (PdfWord word in words.OrderByDescending(w => w.Y))
        {
            if (current == null || Math.Abs(word.Y - lineY) > tolerance)
            {
                current = [];
                lines.Add(current);
                lineY = word.Y;
            }
            current.Add(word);
        }
        foreach (List<PdfWord> line in lines)
        {
            line.Sort((a, b) => a.X.CompareTo(b.X));
        }
        return lines;
    }

    /// <summary>
    /// Joins words into a single string in reading order.
    /// </summary>
    /// <param name="words">The words to join.</param>
    public static string Join(IEnumerable<PdfWord> words)
    {
        return string.Join(" ", Group(words).SelectMany(l => l).Select(w => w.Text)).Trim();
    }
}