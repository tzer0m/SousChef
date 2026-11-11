using System.Globalization;
using System.IO.Compression;
using SkiaSharp;
using SousChef.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SousChef.Core.Parsing;

/// <summary>
/// Reads downloaded recipe card PDFs, which have real fonts, colours and separate images.
/// </summary>
internal static class DigitalCardReader
{
    /// <summary>
    /// Reads both pages of a downloaded card.
    /// </summary>
    /// <param name="document">The PDF document.</param>
    /// <param name="result">The result to fill in.</param>
    public static void Read(PdfDocument document, ParseResult result)
    {
        if (document.NumberOfPages >= 2)
        {
            ReadMethodPage(document.GetPage(2), result);
        }
        ReadFrontPage(document.GetPage(1), result);
    }

    /// <summary>
    /// Reads the title, tags, time, recipe number and images from the front page.
    /// </summary>
    /// <param name="page">The front page.</param>
    /// <param name="result">The result to fill in.</param>
    private static void ReadFrontPage(Page page, ParseResult result)
    {
        Recipe recipe = result.Recipe;
        List<PdfWord> words = PdfWord.FromPage(page);
        List<PdfWord> headingWords = [.. words.Where(w => w.IsHeadingFont)];
        PdfWord? numberWord = headingWords.Where(w => int.TryParse(w.Text, out _)).OrderByDescending(w => w.Size).FirstOrDefault();
        if (numberWord != null)
        {
            recipe.Number = int.Parse(numberWord.Text, CultureInfo.InvariantCulture);
        }
        List<PdfWord> mediumWords = [.. headingWords.Where(w => !w.IsRegularWeight && w.Colour == TextColour.Black && !int.TryParse(w.Text, out _))];
        double titleSize = mediumWords.Select(w => w.Size).DefaultIfEmpty(0).Max();
        recipe.Title = LineGrouper.Join(mediumWords.Where(w => Math.Abs(w.Size - titleSize) < 0.5));
        recipe.Subtitle = LineGrouper.Join(mediumWords.Where(w => w.Size < titleSize - 0.5 && w.Size > titleSize * 0.5));
        ReadTagLine([.. headingWords.Where(w => w.IsRegularWeight)], recipe);
        ExtractFrontImages(page, words, result);
    }

    /// <summary>
    /// Reads the coloured tags, total time and extras such as "1 of your 5 a day".
    /// </summary>
    /// <param name="words">The words on the tag line.</param>
    /// <param name="recipe">The recipe to fill in.</param>
    private static void ReadTagLine(List<PdfWord> words, Recipe recipe)
    {
        List<string> plain = [];
        List<List<PdfWord>> tags = [];
        PdfWord? previous = null;
        foreach (PdfWord word in LineGrouper.Group(words).SelectMany(l => l))
        {
            if (word.Colour == TextColour.Black)
            {
                plain.Add(word.Text);
                previous = null;
                continue;
            }
            bool continues = previous != null && previous.Colour == word.Colour && word.X - previous.Right < 6;
            if (!continues)
            {
                tags.Add([]);
            }
            tags[^1].Add(word);
            previous = word;
        }
        foreach (List<PdfWord> tag in tags)
        {
            string text = string.Join(" ", tag.Select(w => w.Text));
            recipe.Tags.Add(text);
            if (tag[0].Colour == TextColour.Red)
            {
                recipe.SpiceLevel = text;
            }
        }
        foreach (string part in string.Join(" ", plain).Split('•', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!RecipeCardParser.TryReadTime(part, recipe))
            {
                recipe.Extras.Add(part);
            }
        }
    }

    /// <summary>
    /// Extracts the hero photo and the captioned ingredient thumbnails from the front page.
    /// </summary>
    /// <param name="page">The front page.</param>
    /// <param name="words">The words on the front page.</param>
    /// <param name="result">The result to add images to.</param>
    private static void ExtractFrontImages(Page page, List<PdfWord> words, ParseResult result)
    {
        List<IPdfImage> images = [.. page.GetImages()];
        IPdfImage? hero = images.OrderByDescending(i => i.BoundingBox.Width * i.BoundingBox.Height).FirstOrDefault();
        if (hero != null && AddImage(result, "hero", hero))
        {
            result.Recipe.HeroImageKey = "hero";
        }
        List<PdfWord> captionWords = [.. words.Where(w => !w.IsHeadingFont && w.Size <= 8 && w.Colour == TextColour.Black)];
        int index = 0;
        foreach (IPdfImage image in images.Where(i => i != hero && i.BoundingBox.Width < 60 && i.BoundingBox.Height < 60))
        {
            List<PdfWord> caption = [.. captionWords.Where(w => w.CentreX >= image.BoundingBox.Left - 20 && w.CentreX <= image.BoundingBox.Right + 20 && w.Y < image.BoundingBox.Bottom && w.Y > image.BoundingBox.Bottom - 24)];
            if (caption.Count == 0)
            {
                continue;
            }
            string key = CardText.Normalise(LineGrouper.Join(caption));
            Ingredient? ingredient = result.Recipe.Ingredients.FirstOrDefault(i => CardText.Normalise(i.Name) == key) ?? result.Recipe.Ingredients.FirstOrDefault(i => CardText.Normalise(i.Name).Contains(key, StringComparison.Ordinal) || key.Contains(CardText.Normalise(i.Name), StringComparison.Ordinal));
            if (ingredient == null || ingredient.ImageKey != null)
            {
                continue;
            }
            string imageKey = $"ingredient-{index++}";
            if (AddImage(result, imageKey, image))
            {
                ingredient.ImageKey = imageKey;
            }
        }
    }

    /// <summary>
    /// Reads the sidebar and the step grid from the method page.
    /// </summary>
    /// <param name="page">The method page.</param>
    /// <param name="result">The result to fill in.</param>
    private static void ReadMethodPage(Page page, ParseResult result)
    {
        Recipe recipe = result.Recipe;
        List<PdfWord> words = PdfWord.FromPage(page);
        PdfWord? week = words.FirstOrDefault(w => CardText.WeekCodeRegex().IsMatch(w.Text));
        if (week != null)
        {
            recipe.WeekCode = week.Text;
            words.Remove(week);
        }
        List<PdfWord> anchors = [.. words.Where(w => !w.IsHeadingFont && w.Colour == TextColour.Green && w.Size >= 10 && int.TryParse(w.Text, out _)).OrderBy(w => int.Parse(w.Text, CultureInfo.InvariantCulture))];
        if (anchors.Count == 0)
        {
            recipe.ParseWarnings.Add("No step numbers found on the method page.");
        }
        double leftLimit = anchors.Count > 0 ? anchors.Min(a => a.X) - 10 : page.Width * 0.25;
        SidebarReader.Read([.. words.Where(w => w.X < leftLimit)], recipe);
        ReadSteps(page, [.. words.Where(w => w.X >= leftLimit)], anchors, result);
    }

    /// <summary>
    /// Reads each step cell of the grid, using the green step numbers as anchors.
    /// </summary>
    /// <param name="page">The method page.</param>
    /// <param name="words">The words to the right of the sidebar.</param>
    /// <param name="anchors">The step number words.</param>
    /// <param name="result">The result to fill in.</param>
    private static void ReadSteps(Page page, List<PdfWord> words, List<PdfWord> anchors, ParseResult result)
    {
        List<IPdfImage> photos = [.. page.GetImages().Where(i => i.BoundingBox.Width > 100)];
        foreach (PdfWord anchor in anchors)
        {
            double left = anchor.X - 10;
            double right = anchors.Where(a => Math.Abs(a.Y - anchor.Y) < 10 && a.X > anchor.X + 10).Select(a => a.X - 10).DefaultIfEmpty(double.MaxValue).Min();
            double bottom = anchors.Where(a => a.Y < anchor.Y - 10).Select(a => a.Y + 15).DefaultIfEmpty(double.MinValue).Max();
            List<PdfWord> cell = [.. words.Where(w => w != anchor && w.X >= left && w.X < right && w.Y < anchor.Y + 5 && w.Y > bottom)];
            RecipeStep step = new() { Number = int.Parse(anchor.Text, CultureInfo.InvariantCulture), Title = LineGrouper.Join(cell.Where(w => w.IsHeadingFont && w.Size >= 10)) };
            step.SubSteps = SubStepReader.Read(LineGrouper.Group(cell.Where(w => !w.IsHeadingFont)));
            IPdfImage? photo = photos.Where(p => p.BoundingBox.Left >= left - 5 && p.BoundingBox.Left < right && p.BoundingBox.Top <= anchor.Y + 30 && p.BoundingBox.Top > bottom).OrderByDescending(p => p.BoundingBox.Top).FirstOrDefault();
            string key = $"step-{step.Number}";
            if (photo != null && AddImage(result, key, photo))
            {
                step.ImageKey = key;
            }
            result.Recipe.Steps.Add(step);
        }
    }

    /// <summary>
    /// Adds an image to the result as PNG or JPEG.
    /// </summary>
    /// <param name="result">The result to add to.</param>
    /// <param name="key">The image key.</param>
    /// <param name="image">The PDF image.</param>
    private static bool AddImage(ParseResult result, string key, IPdfImage image)
    {
        if (image.TryGetPng(out byte[]? png) && png != null)
        {
            result.Images.Add(new RecipeImage { Key = key, ContentType = "image/png", Data = png });
            return true;
        }
        byte[] raw = [.. image.RawBytes];
        if (raw.Length > 2 && raw[0] == 0x78)
        {
            raw = Inflate(raw);
        }
        if (raw.Length > 2 && raw[0] == 0xFF && raw[1] == 0xD8)
        {
            result.Images.Add(new RecipeImage { Key = key, ContentType = "image/jpeg", Data = image.MaskImage == null ? raw : FlattenOnWhite(raw, image.MaskImage) });
            return true;
        }
        return false;
    }

    /// <summary>
    /// Applies an image's soft mask over a white background, so transparent corners don't come out black.
    /// </summary>
    /// <param name="jpeg">The JPEG bytes.</param>
    /// <param name="mask">The PDF soft mask (one greyscale alpha sample per pixel).</param>
    private static byte[] FlattenOnWhite(byte[] jpeg, IPdfImage mask)
    {
        if (!mask.TryGetBytesAsMemory(out Memory<byte> alphaMemory) || alphaMemory.Length < mask.WidthInSamples * mask.HeightInSamples)
        {
            return jpeg;
        }
        using SKBitmap? bitmap = SKBitmap.Decode(jpeg);
        if (bitmap == null)
        {
            return jpeg;
        }
        Span<byte> alpha = alphaMemory.Span;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                int a = alpha[(y * mask.HeightInSamples / bitmap.Height * mask.WidthInSamples) + (x * mask.WidthInSamples / bitmap.Width)];
                SKColor colour = bitmap.GetPixel(x, y);
                bitmap.SetPixel(x, y, new SKColor((byte)(((colour.Red * a) + (255 * (255 - a))) / 255), (byte)(((colour.Green * a) + (255 * (255 - a))) / 255), (byte)(((colour.Blue * a) + (255 * (255 - a))) / 255)));
            }
        }
        using SKImage flattened = SKImage.FromBitmap(bitmap);
        using SKData data = flattened.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    /// <summary>
    /// Unwraps zlib-compressed image data (the cards store JPEGs behind a Flate filter).
    /// </summary>
    /// <param name="data">The compressed bytes.</param>
    private static byte[] Inflate(byte[] data)
    {
        try
        {
            using MemoryStream input = new(data);
            using ZLibStream zlib = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return data;
        }
    }
}