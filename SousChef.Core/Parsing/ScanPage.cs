using System.IO.Compression;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace SousChef.Core.Parsing;

/// <summary>
/// A scanned page: the OCR words turned upright, plus the scan image for colours and photos.
/// </summary>
/// <param name="bitmap">The decoded scan image, if there is one.</param>
/// <param name="imageBox">Where the scan image sits on the page.</param>
/// <param name="quarterTurns">How many quarter turns the text is rotated by (0 to 3).</param>
/// <param name="originalWidth">The page width before turning upright.</param>
/// <param name="originalHeight">The page height before turning upright.</param>
internal sealed class ScanPage(SKBitmap? bitmap, PdfRectangle imageBox, int quarterTurns, double originalWidth, double originalHeight) : IDisposable
{
    private const int MaxPhotoWidth = 1400;

    private (double R, double G, double B) _paper = (1, 1, 1);

    /// <summary>
    /// The upright page width.
    /// </summary>
    public double Width => quarterTurns % 2 == 1 ? originalHeight : originalWidth;

    /// <summary>
    /// The upright page height.
    /// </summary>
    public double Height => quarterTurns % 2 == 1 ? originalWidth : originalHeight;

    /// <summary>
    /// Whether the page had to be turned to make the text upright.
    /// </summary>
    public bool WasRotated => quarterTurns != 0;

    /// <summary>
    /// The OCR words in upright page coordinates, with colours sampled from the scan.
    /// </summary>
    public List<PdfWord> Words { get; private set; } = [];

    /// <summary>
    /// Loads a scanned page.
    /// </summary>
    /// <param name="page">The PDF page.</param>
    public static ScanPage Load(Page page)
    {
        List<Letter> letters = [.. page.Letters];
        int turns = GetQuarterTurns(letters);
        IPdfImage? image = page.GetImages().OrderByDescending(i => i.BoundingBox.Width * i.BoundingBox.Height).FirstOrDefault();
        SKBitmap? bitmap = image == null ? null : Decode(image);
        ScanPage scan = new(bitmap, image?.BoundingBox ?? new PdfRectangle(0, 0, page.Width, page.Height), turns, page.Width, page.Height);
        scan._paper = scan.MeasurePaper();
        scan.Words = scan.BuildWords(letters);
        return scan;
    }

    /// <summary>
    /// Crops part of the upright page from the scan as a JPEG, or returns null if there's no scan.
    /// </summary>
    /// <param name="left">The left edge in page coordinates.</param>
    /// <param name="bottom">The bottom edge in page coordinates.</param>
    /// <param name="right">The right edge in page coordinates.</param>
    /// <param name="top">The top edge in page coordinates.</param>
    public byte[]? CropJpeg(double left, double bottom, double right, double top)
    {
        if (bitmap == null)
        {
            return null;
        }
        SKRectI rect = ToPixelRect(left, bottom, right, top);
        if (rect.Width < 20 || rect.Height < 20)
        {
            return null;
        }
        using SKBitmap crop = new(rect.Width, rect.Height);
        if (!bitmap.ExtractSubset(crop, rect))
        {
            return null;
        }
        int width = quarterTurns % 2 == 1 ? crop.Height : crop.Width;
        int height = quarterTurns % 2 == 1 ? crop.Width : crop.Height;
        double scale = Math.Min(1, (double)MaxPhotoWidth / width);
        int outWidth = Math.Max(1, (int)(width * scale));
        int outHeight = Math.Max(1, (int)(height * scale));
        using SKBitmap output = new(outWidth, outHeight);
        using (SKCanvas canvas = new(output))
        {
            canvas.Translate(outWidth / 2f, outHeight / 2f);
            canvas.RotateDegrees(quarterTurns * 90);
            canvas.Scale((float)scale);
            canvas.Translate(-crop.Width / 2f, -crop.Height / 2f);
            using SKImage cropImage = SKImage.FromBitmap(crop);
            canvas.DrawImage(cropImage, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
        using SKImage result = SKImage.FromBitmap(output);
        using SKData data = result.Encode(SKEncodedImageFormat.Jpeg, 85);
        return data.ToArray();
    }

    /// <summary>
    /// Frees the scan image.
    /// </summary>
    public void Dispose()
    {
        bitmap?.Dispose();
    }

    /// <summary>
    /// Works out the text direction from the letters' baselines, in quarter turns.
    /// </summary>
    /// <param name="letters">The page's letters.</param>
    private static int GetQuarterTurns(List<Letter> letters)
    {
        int[] counts = new int[4];
        foreach (Letter letter in letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)))
        {
            double dx = letter.EndBaseLine.X - letter.StartBaseLine.X;
            double dy = letter.EndBaseLine.Y - letter.StartBaseLine.Y;
            if (Math.Abs(dx) + Math.Abs(dy) < 0.01)
            {
                continue;
            }
            double angle = ((Math.Atan2(dy, dx) * 180 / Math.PI) + 360) % 360;
            counts[(int)Math.Round(angle / 90) % 4]++;
        }
        return Array.IndexOf(counts, counts.Max());
    }

    /// <summary>
    /// Decodes a scan image, unwrapping the zlib layer OCRmyPDF puts around JPEGs.
    /// </summary>
    /// <param name="image">The PDF image.</param>
    private static SKBitmap? Decode(IPdfImage image)
    {
        byte[] raw = [.. image.RawBytes];
        if (raw.Length > 2 && raw[0] == 0x78)
        {
            try
            {
                using MemoryStream input = new(raw);
                using ZLibStream zlib = new(input, CompressionMode.Decompress);
                using MemoryStream output = new();
                zlib.CopyTo(output);
                raw = output.ToArray();
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }
        if (image.TryGetPng(out byte[]? png) && png != null)
        {
            raw = png;
        }
        return SKBitmap.Decode(raw);
    }

    /// <summary>
    /// Rebuilds words from the OCR letter stream: each run of letters on one baseline is sorted left to right and split at spaces and gaps.
    /// </summary>
    /// <param name="letters">The page's letters in content-stream order.</param>
    private List<PdfWord> BuildWords(List<Letter> letters)
    {
        List<List<ScanLetter>> runs = [];
        List<ScanLetter> run = [];
        foreach (Letter letter in letters)
        {
            (double x0, double y0) = ToUpright(letter.StartBaseLine.X, letter.StartBaseLine.Y);
            (double x1, double y1) = ToUpright(letter.EndBaseLine.X, letter.EndBaseLine.Y);
            double height = quarterTurns % 2 == 1 ? Math.Abs(letter.BoundingBox.Width) : Math.Abs(letter.BoundingBox.Height);
            ScanLetter current = new(letter.Value, Math.Min(x0, x1), Math.Max(x0, x1), (y0 + y1) / 2, (y0 + y1) / 2, height > 0.5 ? height : letter.PointSize * 0.7);
            if (run.Count > 0 && Math.Abs(current.Y - run[^1].Y) > Math.Max(current.Height, run[^1].Height))
            {
                runs.Add(run);
                run = [];
            }
            run.Add(current);
        }
        runs.Add(run);
        double slope = MeasureSkew(runs);
        List<PdfWord> words = [];
        foreach (List<ScanLetter> line in runs)
        {
            List<ScanLetter> word = [];
            foreach (ScanLetter letter in line.Select(l => l with { Y = l.RawY - (slope * (l.X0 - (Width / 2))) }).OrderBy(l => l.X0))
            {
                bool isSpace = string.IsNullOrWhiteSpace(letter.Value);
                bool gap = word.Count > 0 && letter.X0 - word[^1].X1 > Math.Max(letter.Height, word[^1].Height) * 0.8;
                if ((isSpace || gap) && word.Count > 0)
                {
                    words.Add(ToWord(word));
                    word = [];
                }
                if (!isSpace)
                {
                    word.Add(letter);
                }
            }
            if (word.Count > 0)
            {
                words.Add(ToWord(word));
            }
        }
        return words;
    }

    /// <summary>
    /// Creates a word from its letters, sampling its colour from the scan.
    /// </summary>
    /// <param name="letters">The word's letters, left to right.</param>
    private PdfWord ToWord(List<ScanLetter> letters)
    {
        string text = TextCleaner.Clean(string.Concat(letters.Select(l => l.Value)));
        double x = letters.Min(l => l.X0);
        double right = letters.Max(l => l.X1);
        double y = letters.Select(l => l.Y).Order().ElementAt(letters.Count / 2);
        double rawY = letters.Select(l => l.RawY).Order().ElementAt(letters.Count / 2);
        double size = letters.Select(l => l.Height).Order().ElementAt(letters.Count / 2);
        return new PdfWord { Text = text, MainText = text, X = x, Right = right, Y = y, Size = size, Colour = SampleColour(x, right, rawY, size) };
    }

    /// <summary>
    /// Measures how much the scan is tilted, as the median baseline slope of the longer OCR lines.
    /// </summary>
    /// <param name="runs">The letter runs, one per OCR line.</param>
    private static double MeasureSkew(List<List<ScanLetter>> runs)
    {
        List<double> slopes = [];
        foreach (List<ScanLetter> run in runs.Where(r => r.Count >= 8))
        {
            double meanX = run.Average(l => l.X0);
            double meanY = run.Average(l => l.RawY);
            double spread = run.Sum(l => (l.X0 - meanX) * (l.X0 - meanX));
            if (run.Max(l => l.X0) - run.Min(l => l.X0) > 40 && spread > 0)
            {
                slopes.Add(run.Sum(l => (l.X0 - meanX) * (l.RawY - meanY)) / spread);
            }
        }
        if (slopes.Count < 5)
        {
            return 0;
        }
        double median = slopes.Order().ElementAt(slopes.Count / 2);
        return Math.Abs(median) < 0.08 ? median : 0;
    }

    /// <summary>
    /// Finds the ink colour inside a word's box: the average of pixels clearly darker than the paper around them.
    /// </summary>
    /// <param name="left">The word's left edge.</param>
    /// <param name="right">The word's right edge.</param>
    /// <param name="baseline">The word's baseline.</param>
    /// <param name="size">The word's letter height.</param>
    private TextColour SampleColour(double left, double right, double baseline, double size)
    {
        if (bitmap == null)
        {
            return TextColour.Black;
        }
        SKRectI rect = ToPixelRect(left, baseline - (size * 0.2), right, baseline + size);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return TextColour.Black;
        }
        int step = Math.Max(1, (int)Math.Sqrt(rect.Width * rect.Height / 1500.0));
        List<SKColor> pixels = [];
        for (int py = rect.Top; py < rect.Bottom; py += step)
        {
            for (int px = rect.Left; px < rect.Right; px += step)
            {
                pixels.Add(bitmap.GetPixel(px, py));
            }
        }
        if (pixels.Count == 0)
        {
            return TextColour.Black;
        }
        List<double> luminance = [.. pixels.Select(Luminance).Order()];
        double paper = luminance[(int)(luminance.Count * 0.9)];
        List<SKColor> ink = [.. pixels.Where(p => Luminance(p) < paper - 0.22)];
        if (ink.Count < 3)
        {
            return TextColour.Black;
        }
        double r = Math.Min(1, ink.Average(p => p.Red) / 255 / _paper.R);
        double g = Math.Min(1, ink.Average(p => p.Green) / 255 / _paper.G);
        double b = Math.Min(1, ink.Average(p => p.Blue) / 255 / _paper.B);
        return Classify(r, g, b);
    }

    /// <summary>
    /// Measures the paper colour (the brightest fifth of a sparse sample) so ink colours can be white-balanced.
    /// </summary>
    private (double R, double G, double B) MeasurePaper()
    {
        if (bitmap == null)
        {
            return (1, 1, 1);
        }
        List<SKColor> sample = [];
        int stepX = Math.Max(1, bitmap.Width / 160);
        int stepY = Math.Max(1, bitmap.Height / 120);
        for (int y = stepY / 2; y < bitmap.Height; y += stepY)
        {
            for (int x = stepX / 2; x < bitmap.Width; x += stepX)
            {
                sample.Add(bitmap.GetPixel(x, y));
            }
        }
        List<SKColor> brightest = [.. sample.OrderByDescending(Luminance).Take(Math.Max(1, sample.Count / 5))];
        return (Math.Max(0.5, brightest.Average(p => p.Red) / 255), Math.Max(0.5, brightest.Average(p => p.Green) / 255), Math.Max(0.5, brightest.Average(p => p.Blue) / 255));
    }

    /// <summary>
    /// Classifies an ink colour by hue and chroma (not saturation, which exaggerates the tint of dark scanned ink).
    /// </summary>
    /// <param name="r">Red, 0 to 1.</param>
    /// <param name="g">Green, 0 to 1.</param>
    /// <param name="b">Blue, 0 to 1.</param>
    private static TextColour Classify(double r, double g, double b)
    {
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        if (max - min < 0.17)
        {
            return max < 0.45 ? TextColour.Black : TextColour.Grey;
        }
        double hue;
        if (max == r)
        {
            hue = 60 * (((g - b) / (max - min)) % 6);
        }
        else if (max == g)
        {
            hue = 60 * (((b - r) / (max - min)) + 2);
        }
        else
        {
            hue = 60 * (((r - g) / (max - min)) + 4);
        }
        hue = (hue + 360) % 360;
        if (hue < 25 || hue > 335)
        {
            return TextColour.Red;
        }
        return hue is >= 75 and <= 165 ? TextColour.Green : TextColour.Other;
    }

    /// <summary>
    /// Returns a pixel's perceived brightness, 0 to 1.
    /// </summary>
    /// <param name="colour">The pixel colour.</param>
    private static double Luminance(SKColor colour)
    {
        return ((0.299 * colour.Red) + (0.587 * colour.Green) + (0.114 * colour.Blue)) / 255;
    }

    /// <summary>
    /// Converts a rectangle in upright page coordinates to a pixel rectangle on the scan.
    /// </summary>
    /// <param name="left">The left edge.</param>
    /// <param name="bottom">The bottom edge.</param>
    /// <param name="right">The right edge.</param>
    /// <param name="top">The top edge.</param>
    private SKRectI ToPixelRect(double left, double bottom, double right, double top)
    {
        (double X, double Y)[] corners = [ToPixel(left, bottom), ToPixel(left, top), ToPixel(right, bottom), ToPixel(right, top)];
        int x0 = Math.Clamp((int)corners.Min(c => c.X), 0, bitmap!.Width);
        int x1 = Math.Clamp((int)Math.Ceiling(corners.Max(c => c.X)), 0, bitmap.Width);
        int y0 = Math.Clamp((int)corners.Min(c => c.Y), 0, bitmap.Height);
        int y1 = Math.Clamp((int)Math.Ceiling(corners.Max(c => c.Y)), 0, bitmap.Height);
        return new SKRectI(x0, y0, x1, y1);
    }

    /// <summary>
    /// Converts a point in upright page coordinates to a pixel on the scan.
    /// </summary>
    /// <param name="x">The upright X position.</param>
    /// <param name="y">The upright Y position.</param>
    private (double X, double Y) ToPixel(double x, double y)
    {
        (double ox, double oy) = ToOriginal(x, y);
        return ((ox - imageBox.Left) / imageBox.Width * bitmap!.Width, (imageBox.Top - oy) / imageBox.Height * bitmap.Height);
    }

    /// <summary>
    /// Turns a point on the original page into upright coordinates.
    /// </summary>
    /// <param name="x">The original X position.</param>
    /// <param name="y">The original Y position.</param>
    private (double X, double Y) ToUpright(double x, double y)
    {
        return quarterTurns switch
        {
            1 => (y, originalWidth - x),
            2 => (originalWidth - x, originalHeight - y),
            3 => (originalHeight - y, x),
            _ => (x, y)
        };
    }

    /// <summary>
    /// Turns a point in upright coordinates back onto the original page.
    /// </summary>
    /// <param name="x">The upright X position.</param>
    /// <param name="y">The upright Y position.</param>
    private (double X, double Y) ToOriginal(double x, double y)
    {
        return quarterTurns switch
        {
            1 => (originalWidth - y, x),
            2 => (originalWidth - x, originalHeight - y),
            3 => (y, originalHeight - x),
            _ => (x, y)
        };
    }
}