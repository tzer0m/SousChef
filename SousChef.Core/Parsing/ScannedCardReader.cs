using System.Text.RegularExpressions;
using SousChef.Core.Models;
using UglyToad.PdfPig;

namespace SousChef.Core.Parsing;

/// <summary>
/// Reads scanned recipe cards from their OCR text layer, using layout rather than fonts or colours to find each part.
/// </summary>
internal static partial class ScannedCardReader
{
    private const double MaxLineGap = 22;

    private static readonly char[] JunkPrefix = ['>', '+', '<', '?', ' ', '"', '\''];

    private static readonly HashSet<string> Bullets = ["e", "+", "°", "•", "«", "»", "©", "*", "o", "¢", "@", "-", "–", "·"];

    /// <summary>
    /// Reads both pages of a scanned card.
    /// </summary>
    /// <param name="document">The PDF document.</param>
    /// <param name="result">The result to fill in.</param>
    public static void Read(PdfDocument document, ParseResult result)
    {
        if (document.NumberOfPages >= 2)
        {
            using ScanPage method = ScanPage.Load(document.GetPage(document.NumberOfPages));
            ReadMethodPage(method, result);
        }
        using ScanPage front = ScanPage.Load(document.GetPage(1));
        ReadFrontPage(front, result);
    }

    /// <summary>
    /// Reads the title, tags, time and hero photo from the front page.
    /// </summary>
    /// <param name="scan">The front page.</param>
    /// <param name="result">The result to fill in.</param>
    private static void ReadFrontPage(ScanPage scan, ParseResult result)
    {
        Recipe recipe = result.Recipe;
        List<List<PdfWord>> lines = LineGrouper.Group(scan.Words);
        List<PdfWord>? tagLine = lines.FirstOrDefault(l => l[0].Y > scan.Height * 0.6 && CardText.TotalTimeRegex().IsMatch(CardText.LineText(l)));
        double tagY = tagLine?[0].Y ?? scan.Height * 0.83;
        if (tagLine != null)
        {
            ReadTagLine(tagLine, recipe);
        }
        List<List<PdfWord>> header = [.. lines.Where(l => l[0].Y > tagY + 3).Select(l => l.FindAll(w => w.X > scan.Width * 0.17 && w.X < scan.Width * 0.77)).Where(l => l.Count > 0)];
        int withIndex = header.FindIndex(l => l[0].Text.Equals("with", StringComparison.OrdinalIgnoreCase));
        recipe.Title = string.Join(" ", header.Take(withIndex > 0 ? withIndex : Math.Min(1, header.Count)).Select(CardText.LineText));
        recipe.Subtitle = withIndex >= 0 ? string.Join(" ", header.Skip(withIndex).Select(CardText.LineText)) : string.Empty;
        byte[]? hero = scan.CropJpeg(scan.Width * 0.017, scan.Height * 0.034, scan.Width * 0.79, tagY - 18);
        if (hero != null)
        {
            result.Images.Add(new RecipeImage { Key = "hero", ContentType = "image/jpeg", Data = hero });
            recipe.HeroImageKey = "hero";
        }
    }

    /// <summary>
    /// Reads the tags before the time, the time itself, and the extras after it.
    /// </summary>
    /// <param name="line">The tag line's words.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ReadTagLine(List<PdfWord> line, Recipe recipe)
    {
        string text = CardText.LineText(line);
        Match time = CardText.TotalTimeRegex().Match(text);
        List<List<PdfWord>> tags = [];
        int position = 0;
        PdfWord? previous = null;
        foreach (PdfWord word in line)
        {
            if (position >= time.Index)
            {
                break;
            }
            if (previous == null || previous.Colour != word.Colour || word.X - previous.Right > 8)
            {
                tags.Add([]);
            }
            tags[^1].Add(word);
            previous = word;
            position += word.Text.Length + 1;
        }
        foreach (string tag in tags.Select(t => string.Join(" ", t.Select(w => w.Text)).Trim()).Where(t => t.Length > 2))
        {
            AddTag(tag, recipe);
        }
        RecipeCardParser.TryReadTime(time.Value, recipe);
        string after = LeadingLettersRegex().Replace(text[(time.Index + time.Length)..], string.Empty);
        foreach (string part in ExtraSeparatorRegex().Split(after).Select(p => FiveADayRegex().Replace(p.Trim().TrimStart(JunkPrefix), "$1 a day")).Where(p => p.Length > 2))
        {
            if (SpiceRegex().IsMatch(part))
            {
                AddTag(part, recipe);
            }
            else
            {
                recipe.Extras.Add(part);
            }
        }
    }

    /// <summary>
    /// Adds a tag, treating tags such as "Very Hot" or "Mild Spice" as the spice level.
    /// </summary>
    /// <param name="tag">The tag text.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void AddTag(string tag, Recipe recipe)
    {
        recipe.Tags.Add(tag);
        if (SpiceRegex().IsMatch(tag))
        {
            recipe.SpiceLevel = tag;
        }
    }

    /// <summary>
    /// Reads the sidebar and the step columns from the method page.
    /// </summary>
    /// <param name="scan">The method page.</param>
    /// <param name="result">The result to fill in.</param>
    private static void ReadMethodPage(ScanPage scan, ParseResult result)
    {
        Recipe recipe = result.Recipe;
        List<PdfWord> words = [.. scan.Words];
        PdfWord? week = words.FirstOrDefault(w => CardText.WeekCodeRegex().IsMatch(w.Text));
        if (week != null)
        {
            recipe.WeekCode = week.Text;
            words.Remove(week);
        }
        List<double> columns = FindColumns(words, scan.Width);
        if (columns.Count == 0)
        {
            recipe.ParseWarnings.Add("Couldn't find the method steps on this scan.");
        }
        double leftLimit = columns.Count > 0 ? columns[0] - 6 : scan.Width * 0.25;
        SidebarReader.Read([.. words.Where(w => w.X < leftLimit)], recipe);
        List<(int Column, StepBlock Block)> blocks = [];
        for (int i = 0; i < columns.Count; i++)
        {
            double left = columns[i] - 6;
            double right = i + 1 < columns.Count ? columns[i + 1] - 6 : scan.Width;
            blocks.AddRange(ReadColumn(LineGrouper.Group(words.Where(w => w.X >= left && w.X < right)), columns[i]).Select(b => (i, b)));
        }
        int row = 0;
        double? rowStart = null;
        List<(int Row, int Column, StepBlock Block)> ordered = [];
        foreach ((int column, StepBlock block) in blocks.OrderByDescending(b => b.Block.StartY))
        {
            if (rowStart.HasValue && rowStart.Value - block.StartY > 30)
            {
                row++;
                rowStart = block.StartY;
            }
            rowStart ??= block.StartY;
            ordered.Add((row, column, block));
        }
        int number = 1;
        foreach ((int _, int column, StepBlock block) in ordered.OrderBy(o => o.Row).ThenBy(o => o.Column))
        {
            RecipeStep step = new() { Number = number++, Title = block.Title, SubSteps = SubStepReader.Read(block.Lines) };
            double left = columns[column] - 4;
            double right = (column + 1 < columns.Count ? columns[column + 1] : scan.Width) - 12;
            double photoBottom = block.TitleY + 10;
            double photoTop = Math.Min(Math.Min(photoBottom + ((right - left) / 1.79), block.UpperLimitY - 6), scan.Height - 15);
            byte[]? photo = photoTop - photoBottom > 40 ? scan.CropJpeg(left, photoBottom, right, photoTop) : null;
            if (photo != null)
            {
                step.ImageKey = $"step-{step.Number}";
                result.Images.Add(new RecipeImage { Key = step.ImageKey, ContentType = "image/jpeg", Data = photo });
            }
            recipe.Steps.Add(step);
        }
    }

    /// <summary>
    /// Finds the left edge of each step column from where many lines start (text is left-aligned in each column).
    /// </summary>
    /// <param name="words">The page's words.</param>
    /// <param name="width">The page width.</param>
    private static List<double> FindColumns(List<PdfWord> words, double width)
    {
        List<PdfWord> candidates = [.. words.Where(w => w.X > width * 0.235)];
        List<double> lineStarts = [.. candidates.Where(w => !candidates.Any(o => o != w && Math.Abs(o.Y - w.Y) < 2.5 && o.Right <= w.X + 1 && o.Right > w.X - 12)).Select(w => w.X).Order()];
        List<List<double>> clusters = [];
        foreach (double x in lineStarts)
        {
            if (clusters.Count == 0 || x - clusters[^1][0] > 6)
            {
                clusters.Add([]);
            }
            clusters[^1].Add(x);
        }
        List<double> columns = [];
        foreach (List<double> strong in clusters.Where(c => c.Count >= 6))
        {
            if (columns.Count == 0 || strong[0] - columns[^1] > 100)
            {
                columns.Add(clusters.Where(c => c.Count >= 3 && c[0] >= strong[0] - 15 && c[0] <= strong[0]).Min(c => c[0]));
            }
        }
        return columns;
    }

    /// <summary>
    /// Splits one column into steps: blocks of text separated by photos, each starting with its title.
    /// </summary>
    /// <param name="lines">The column's lines, top to bottom.</param>
    /// <param name="columnX">The column's left edge.</param>
    private static List<StepBlock> ReadColumn(List<List<PdfWord>> lines, double columnX)
    {
        List<List<List<PdfWord>>> chunks = [];
        double previousY = double.MaxValue;
        foreach (List<PdfWord> line in lines.Where(l => !IsGarbage(l)))
        {
            if (chunks.Count == 0 || previousY - line[0].Y > MaxLineGap)
            {
                chunks.Add([]);
            }
            chunks[^1].Add(line);
            previousY = line[0].Y;
        }
        List<StepBlock> blocks = [];
        double upperLimit = double.MaxValue;
        List<PdfWord>? loneLine = null;
        foreach (List<List<PdfWord>> chunk in chunks)
        {
            if (blocks.Count > 0 && SwapNoteRegex().IsMatch(CardText.LineText(chunk[0])))
            {
                blocks[^1].Lines.AddRange(chunk);
                upperLimit = chunk[^1][0].Y;
                continue;
            }
            if (chunk.Count < 2)
            {
                loneLine = chunk[0];
                continue;
            }
            int firstMarker = chunk.FindIndex(l => IsMarker(l[0], columnX));
            int titleLines = firstMarker >= 0 ? Math.Min(firstMarker, 2) : 1;
            string title = string.Join(" ", chunk.Take(titleLines).Select(CardText.LineText));
            double titleY = chunk[0][0].Y;
            if (titleLines == 0 && loneLine != null && loneLine[0].Y - chunk[0][0].Y <= 35)
            {
                title = CardText.LineText(loneLine);
                titleY = loneLine[0].Y;
            }
            else if (loneLine != null)
            {
                upperLimit = loneLine[0].Y;
            }
            loneLine = null;
            StepBlock block = new(title, titleY, chunk[0][0].Y, upperLimit);
            block.Lines.AddRange(MarkSubSteps([.. chunk.Skip(firstMarker >= 0 ? firstMarker : titleLines)], columnX));
            blocks.Add(block);
            upperLimit = chunk[^1][0].Y;
        }
        return blocks;
    }

    /// <summary>
    /// Makes sure each sub-step starts with a letter marker: keeps "a)" markers, turns bullets into letters, or splits paragraphs at wider line gaps.
    /// </summary>
    /// <param name="lines">The step's body lines.</param>
    /// <param name="columnX">The column's left edge.</param>
    private static List<List<PdfWord>> MarkSubSteps(List<List<PdfWord>> lines, double columnX)
    {
        if (lines.Count == 0 || lines.Any(l => CardText.SubStepLetterRegex().IsMatch(l[0].Text)))
        {
            return lines;
        }
        char letter = 'a';
        if (lines.Any(l => IsMarker(l[0], columnX)))
        {
            return [.. lines.Select(l => IsMarker(l[0], columnX) ? (List<PdfWord>)[Marker(letter++, l[0]), .. l.Skip(1)] : l)];
        }
        List<double> gaps = [.. lines.Zip(lines.Skip(1)).Select(p => p.First[0].Y - p.Second[0].Y).Order()];
        double typical = gaps.Count > 0 ? gaps[gaps.Count / 2] : 0;
        List<List<PdfWord>> marked = [];
        for (int i = 0; i < lines.Count; i++)
        {
            bool newParagraph = i == 0 || (lines[i - 1][0].Y - lines[i][0].Y > Math.Max(typical * 1.25, typical + 2));
            marked.Add(newParagraph ? [Marker(letter++, lines[i][0]), .. lines[i]] : lines[i]);
        }
        return marked;
    }

    /// <summary>
    /// Checks whether a word is a sub-step marker: "a)" or a bullet at the column's left edge.
    /// </summary>
    /// <param name="word">The first word of a line.</param>
    /// <param name="columnX">The column's left edge.</param>
    private static bool IsMarker(PdfWord word, double columnX)
    {
        return CardText.SubStepLetterRegex().IsMatch(word.Text) || (Bullets.Contains(word.Text) && Math.Abs(word.X - columnX) < 8);
    }

    /// <summary>
    /// Creates a synthetic "a)" marker word in place of a bullet or paragraph start.
    /// </summary>
    /// <param name="letter">The sub-step letter.</param>
    /// <param name="near">The word the marker goes before.</param>
    private static PdfWord Marker(char letter, PdfWord near)
    {
        return new PdfWord { Text = $"{letter})", X = near.X - 8, Right = near.X - 1, Y = near.Y, Size = near.Size };
    }

    /// <summary>
    /// Checks whether a line is OCR noise from a photo rather than text (too few letters or real words).
    /// </summary>
    /// <param name="line">The line's words.</param>
    private static bool IsGarbage(List<PdfWord> line)
    {
        string text = CardText.LineText(line);
        int letters = text.Count(char.IsLetter);
        int visible = text.Count(c => !char.IsWhiteSpace(c));
        int realWords = line.Count(w => w.Text.Count(char.IsLetter) >= 3);
        return letters < 3 || letters < visible * 0.6 || (line.Count >= 3 && realWords < line.Count * 0.35);
    }

    [GeneratedRegex(@"^\W*Swap\b", RegexOptions.IgnoreCase)]
    private static partial Regex SwapNoteRegex();

    [GeneratedRegex(@"^[A-Za-z]*")]
    private static partial Regex LeadingLettersRegex();

    [GeneratedRegex(@"[•«»·©@:|\-–]+")]
    private static partial Regex ExtraSeparatorRegex();

    [GeneratedRegex(@"(\d)\s*a\s*day", RegexOptions.IgnoreCase)]
    private static partial Regex FiveADayRegex();

    [GeneratedRegex(@"\b(Hot|Spicy|Spice|Mild)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SpiceRegex();
}